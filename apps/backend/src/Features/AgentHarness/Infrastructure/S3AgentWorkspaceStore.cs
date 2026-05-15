namespace OffceOs.Infrastructure.Features.AgentHarness;

internal sealed class S3AgentWorkspaceStore : IAgentWorkspaceStore
{
    private const string ArchivePath = "/tmp/eaos-workspace.tar.gz";
    private readonly WorkspaceStorageConfig _workspaceStorageConfig;
    private readonly PodExecutorClient _podExecutorClient;
    private readonly IAmazonS3 _amazonS3;
    private readonly ILogger<S3AgentWorkspaceStore> _logger;
    private bool _bucketChecked;

    public S3AgentWorkspaceStore(
        WorkspaceStorageConfig config,
        PodExecutorClient executor,
        ILogger<S3AgentWorkspaceStore> logger)
    {
        _workspaceStorageConfig = config;
        _podExecutorClient = executor;
        _logger = logger;
        _amazonS3 = new AmazonS3Client(
            _workspaceStorageConfig.AccessKey,
            _workspaceStorageConfig.SecretKey,
            new AmazonS3Config
            {
                ServiceURL = _workspaceStorageConfig.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName,
            });
    }

    public async Task RestoreAsync(string sandboxId, string serviceUrl, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);
        var key = ObjectKey(sandboxId);
        if (!await ObjectExistsAsync(key, ct))
        {
            _logger.LogDebug("No workspace checkpoint found for sandbox {SandboxId}", sandboxId);
            return;
        }

        using var response = await _amazonS3.GetObjectAsync(_workspaceStorageConfig.Bucket, key, ct);
        var upload = await _podExecutorClient.UploadFileStreamAsync(
            sandboxId,
            serviceUrl,
            ArchivePath,
            response.ResponseStream,
            "workspace.tar.gz",
            ct);
        if (upload.IsFailure)
            throw new InvalidOperationException(upload.Error.Message);

        var extract = await _podExecutorClient.ExecuteAsync(
            sandboxId,
            serviceUrl,
            $"mkdir -p {KubernetesAgentSandbox.WorkspacePath} && find {KubernetesAgentSandbox.WorkspacePath} -mindepth 1 -maxdepth 1 -exec rm -rf -- {{}} + && tar -C {KubernetesAgentSandbox.WorkspacePath} -xzf {ArchivePath} && rm -f {ArchivePath}",
            TimeSpan.FromMinutes(2),
            ct);
        if (extract.IsFailure)
            throw new InvalidOperationException(extract.Error.Message);
        if (extract.Value.ExitCode != 0)
            throw new InvalidOperationException($"Workspace restore failed: {extract.Value.Output}");

        _logger.LogInformation("Restored workspace checkpoint {ObjectKey} for sandbox {SandboxId}", key, sandboxId);
    }

    public async Task CheckpointAsync(string sandboxId, string serviceUrl, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);
        var pack = await _podExecutorClient.ExecuteAsync(
            sandboxId,
            serviceUrl,
            $"tar -C {KubernetesAgentSandbox.WorkspacePath} -czf {ArchivePath} .",
            TimeSpan.FromMinutes(2),
            ct);
        if (pack.IsFailure)
            throw new InvalidOperationException(pack.Error.Message);
        if (pack.Value.ExitCode != 0)
            throw new InvalidOperationException($"Workspace checkpoint packing failed: {pack.Value.Output}");

        var download = await _podExecutorClient.DownloadFileStreamAsync(sandboxId, serviceUrl, ArchivePath, ct);
        if (download.IsFailure)
            throw new InvalidOperationException(download.Error.Message);

        await using var archiveStream = download.Value;
        var key = ObjectKey(sandboxId);
        await _amazonS3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _workspaceStorageConfig.Bucket,
            Key = key,
            InputStream = archiveStream,
            ContentType = "application/gzip",
        }, ct);

        await _podExecutorClient.ExecuteAsync(
            sandboxId,
            serviceUrl,
            $"rm -f {ArchivePath}",
            TimeSpan.FromSeconds(10),
            ct);

        _logger.LogInformation("Checkpointed workspace for sandbox {SandboxId} to {ObjectKey}", sandboxId, key);
    }

    internal static string ObjectKey(string sandboxId) => $"workspaces/{sandboxId}/workspace.tar.gz";

    private async Task<bool> ObjectExistsAsync(string key, CancellationToken ct)
    {
        try
        {
            await _amazonS3.GetObjectMetadataAsync(_workspaceStorageConfig.Bucket, key, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketChecked)
            return;

        var buckets = await _amazonS3.ListBucketsAsync(ct);
        if (!buckets.Buckets.Any(bucket => string.Equals(bucket.BucketName, _workspaceStorageConfig.Bucket, StringComparison.Ordinal)))
        {
            await _amazonS3.PutBucketAsync(new PutBucketRequest
            {
                BucketName = _workspaceStorageConfig.Bucket,
            }, ct);
        }

        _bucketChecked = true;
    }
}
