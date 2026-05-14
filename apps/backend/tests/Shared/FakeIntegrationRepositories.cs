using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;
using System.Text.Json;

namespace OffceOs.Tests.Shared;

public sealed class FakeIntegrationDefinitionRepository : IIntegrationDefinitionRepository
{
    private readonly Dictionary<(Guid OwnerId, Guid? WorkspaceId, string Name), IntegrationDefinitionRecord> _servers = new();

    public Task<IReadOnlyList<IntegrationDefinitionRecord>> ListAsync(Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IntegrationDefinitionRecord>>(
            _servers.Where(kvp => kvp.Key.OwnerId == ownerId && kvp.Key.WorkspaceId == workspaceId).Select(kvp => kvp.Value).ToList());

    public Task<IntegrationDefinitionRecord?> GetByNameAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult(_servers.GetValueOrDefault((ownerId, workspaceId, name)));

    public Task<IntegrationDefinitionRecord> UpsertAsync(Guid ownerId, Guid workspaceId, IntegrationDefinitionRecord server, CancellationToken ct = default)
    {
        var saved = server with { OwnerId = ownerId, WorkspaceId = workspaceId, IsBuiltin = false };
        _servers[(ownerId, workspaceId, server.Name)] = saved;
        return Task.FromResult(saved);
    }

    public Task DeleteAsync(Guid ownerId, string name, Guid? workspaceId = null, CancellationToken ct = default)
    {
        _servers.Remove((ownerId, workspaceId, name));
        return Task.CompletedTask;
    }
}

public sealed class FakeAgentIntegrationRepository : IAgentIntegrationRepository
{
    private readonly Dictionary<Guid, HashSet<string>> _assigned = new();

    public IReadOnlyList<string> AssignedIntegrationNames => _assigned.Values.SelectMany(v => v).ToList();

    public Task<IReadOnlyList<string>> ListIntegrationNamesForAgentAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(
            _assigned.TryGetValue(agentId, out var names) ? names.ToList() : []);

    public Task AssignAsync(Guid agentId, string integrationName, CancellationToken ct = default)
    {
        if (!_assigned.TryGetValue(agentId, out var names))
            _assigned[agentId] = names = new(StringComparer.OrdinalIgnoreCase);

        names.Add(integrationName);
        return Task.CompletedTask;
    }

    public Task UnassignAsync(Guid agentId, string integrationName, CancellationToken ct = default)
    {
        if (_assigned.TryGetValue(agentId, out var names))
            names.Remove(integrationName);

        return Task.CompletedTask;
    }

    public Task UnassignIntegrationFromOwnerAgentsAsync(Guid ownerId, string integrationName, CancellationToken ct = default)
    {
        foreach (var names in _assigned.Values)
            names.Remove(integrationName);

        return Task.CompletedTask;
    }
}

public sealed class FakeIntegrationCredentialRepository : IIntegrationCredentialRepository
{
    private readonly Dictionary<(Guid OwnerId, Guid? WorkspaceId, string IntegrationName), IntegrationCredentialRecord> _credentials = new();

    public Task<IReadOnlyList<IntegrationCredentialRecord>> ListAsync(IntegrationCredentialFilter filter, CancellationToken ct = default)
    {
        var rows = _credentials.Values.AsEnumerable();
        if (filter.Id.HasValue)
            rows = rows.Where(credential => credential.Id == filter.Id.Value);
        if (filter.OwnerId.HasValue)
            rows = rows.Where(credential => credential.OwnerId == filter.OwnerId.Value);
        if (filter.WorkspaceId.HasValue)
            rows = rows.Where(credential => credential.WorkspaceId == filter.WorkspaceId.Value);
        if (!string.IsNullOrWhiteSpace(filter.IntegrationName))
            rows = rows.Where(credential => credential.IntegrationName == filter.IntegrationName);
        if (!filter.IncludeArchived)
            rows = rows.Where(credential => credential.State != IntegrationCredentialState.Archived);

        return Task.FromResult<IReadOnlyList<IntegrationCredentialRecord>>(rows.ToList());
    }

    public Task<IntegrationCredentialRecord?> GetByAsync(IntegrationCredentialFilter filter, CancellationToken ct = default)
    {
        return Task.FromResult(ListAsync(filter, ct).Result.FirstOrDefault());
    }

    public Task UpsertAsync(IntegrationCredentialRecord credential, CancellationToken ct = default)
    {
        _credentials[(credential.OwnerId, credential.WorkspaceId, credential.IntegrationName)] = credential;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid ownerId, string integrationName, Guid? workspaceId = null, CancellationToken ct = default)
    {
        _credentials.Remove((ownerId, workspaceId, integrationName));
        return Task.CompletedTask;
    }

    public Task ArchiveAsync(Guid workspaceId, string integrationName, CancellationToken ct = default)
    {
        foreach (var (key, credential) in _credentials.ToList())
        {
            if (credential.WorkspaceId != workspaceId || credential.IntegrationName != integrationName)
                continue;
            _credentials[key] = credential.CopyForFake(
                state: IntegrationCredentialState.Archived,
                archivedAt: DateTime.UtcNow,
                updatedAt: DateTime.UtcNow);
        }

        return Task.CompletedTask;
    }

    public Task MarkUsedAsync(Guid id, DateTime usedAt, CancellationToken ct = default)
    {
        foreach (var (key, credential) in _credentials.ToList())
        {
            if (credential.Id != id) continue;
            _credentials[key] = credential.CopyForFake(lastUsedAt: usedAt, updatedAt: usedAt);
        }

        return Task.CompletedTask;
    }
}

internal static class FakeIntegrationCredentialRecordExtensions
{
    public static IntegrationCredentialRecord CopyForFake(
        this IntegrationCredentialRecord credential,
        IntegrationCredentialState? state = null,
        DateTime? lastUsedAt = null,
        DateTime? archivedAt = null,
        DateTime? updatedAt = null) =>
        new()
        {
            Id = credential.Id,
            OwnerId = credential.OwnerId,
            WorkspaceId = credential.WorkspaceId,
            IntegrationName = credential.IntegrationName,
            AuthKind = credential.AuthKind,
            State = state ?? credential.State,
            EncryptedSecretEnvelope = credential.EncryptedSecretEnvelope,
            PublicAuthMetadataJson = credential.PublicAuthMetadataJson,
            ScopesJson = credential.ScopesJson,
            ExpiresAtUtc = credential.ExpiresAtUtc,
            ValidatedAt = credential.ValidatedAt,
            LastUsedAt = lastUsedAt ?? credential.LastUsedAt,
            ArchivedAt = archivedAt ?? credential.ArchivedAt,
            CreatedBy = credential.CreatedBy,
            ConfiguredAt = credential.ConfiguredAt,
            UpdatedAt = updatedAt ?? credential.UpdatedAt,
        };
}

public sealed class FakeOAuthTokenRepository : IOAuthTokenRepository
{
    public Task<OAuthTokenRecord?> GetByAsync(OAuthTokenFilter filter, CancellationToken ct = default) =>
        Task.FromResult<OAuthTokenRecord?>(null);

    public Task UpsertAsync(OAuthTokenRecord token, CancellationToken ct = default) => Task.CompletedTask;

    public Task<bool> DeleteAsync(OAuthTokenFilter filter, CancellationToken ct = default) =>
        Task.FromResult(false);
}

public sealed class FakeIntegrationCredentialEncryptionService : IIntegrationCredentialEncryptionService
{
    public Task<string> ProtectAsync(Dictionary<string, string> secret, CancellationToken ct = default) =>
        Task.FromResult(JsonSerializer.Serialize(secret));

    public Task<Dictionary<string, string>> UnprotectAsync(string envelope, CancellationToken ct = default) =>
        Task.FromResult(JsonSerializer.Deserialize<Dictionary<string, string>>(envelope) ?? new());
}

public sealed class FakeIntegrationDeploymentRepository : IIntegrationDeploymentRepository
{
    public Task<IReadOnlyList<IntegrationDeploymentRecord>> ListAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<IntegrationDeploymentRecord>>([]);

    public Task<IntegrationDeploymentRecord?> GetByAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default) =>
        Task.FromResult<IntegrationDeploymentRecord?>(null);

    public Task<IntegrationDeploymentRecord> UpsertAsync(IntegrationDeploymentRecord record, CancellationToken ct = default) =>
        Task.FromResult(record);

    public Task<bool> DeleteAsync(IntegrationDeploymentFilter filter, CancellationToken ct = default) =>
        Task.FromResult(false);
}

public sealed class FakeWorkspaceRepository : IWorkspaceRepository
{
    public Task<IReadOnlyList<WorkspaceRecord>> ListAsync(WorkspaceFilter filter, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<WorkspaceRecord>>([]);

    public Task<IReadOnlyList<WorkspaceRecord>> ListAccessibleAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<WorkspaceRecord>>([]);

    public Task<WorkspaceRecord?> GetByAsync(WorkspaceFilter filter, CancellationToken ct = default) =>
        Task.FromResult<WorkspaceRecord?>(new WorkspaceRecord
        {
            Id = filter.Id ?? TestIds.WorkspaceId,
            OwnerKind = WorkspaceOwnerKind.Personal,
            OwnerUserId = TestIds.OwnerId,
            Name = "Personal",
        });

    public Task<WorkspaceRecord?> GetAccessibleAsync(Guid userId, Guid workspaceId, CancellationToken ct = default) =>
        GetByAsync(new WorkspaceFilter { Id = workspaceId }, ct);

    public Task<WorkspaceRecord> SaveAsync(WorkspaceRecord record, CancellationToken ct = default) => Task.FromResult(record);

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);

    public Task<WorkspaceRecord> EnsurePersonalDefaultAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(WorkspaceRecord.CreatePersonal(userId, "Default", true));

    public Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default) =>
        EnsurePersonalDefaultAsync(userId, ct);

    public Task SetCurrentAsync(Guid userId, Guid workspaceId, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class FakeWorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly Dictionary<(Guid WorkspaceId, Guid UserId), WorkspaceMemberRecord> _members = new();

    public Task<IReadOnlyList<WorkspaceMemberRecord>> ListAsync(WorkspaceMemberFilter filter, CancellationToken ct = default)
    {
        var rows = _members.Values.AsEnumerable();
        if (filter.WorkspaceId.HasValue)
            rows = rows.Where(member => member.WorkspaceId == filter.WorkspaceId.Value);
        if (filter.UserId.HasValue)
            rows = rows.Where(member => member.UserId == filter.UserId.Value);
        return Task.FromResult<IReadOnlyList<WorkspaceMemberRecord>>(rows.ToList());
    }

    public Task<WorkspaceMemberRecord?> GetByAsync(WorkspaceMemberFilter filter, CancellationToken ct = default)
    {
        if (filter.WorkspaceId.HasValue && filter.UserId.HasValue)
            return Task.FromResult(_members.GetValueOrDefault((filter.WorkspaceId.Value, filter.UserId.Value)));
        return Task.FromResult<WorkspaceMemberRecord?>(null);
    }

    public Task<WorkspaceMemberRecord> UpsertAsync(WorkspaceMemberRecord record, CancellationToken ct = default)
    {
        _members[(record.WorkspaceId, record.UserId)] = record;
        return Task.FromResult(record);
    }

    public Task<bool> DeleteAsync(WorkspaceMemberFilter filter, CancellationToken ct = default)
    {
        if (filter.WorkspaceId.HasValue && filter.UserId.HasValue)
            return Task.FromResult(_members.Remove((filter.WorkspaceId.Value, filter.UserId.Value)));
        return Task.FromResult(false);
    }
}
