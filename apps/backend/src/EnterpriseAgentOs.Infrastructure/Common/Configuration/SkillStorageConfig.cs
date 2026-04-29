namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class SkillStorageConfig
{
    /// <summary>MinIO/S3 endpoint URL, e.g. <c>http://eaos-minio:9000</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
}
