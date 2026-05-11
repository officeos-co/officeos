namespace OffceOs.Infrastructure.Features.Management;

internal sealed class UserRepository : IUserRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public UserRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<UserRecord> UpsertByGoogleSubjectAsync(
        string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct)
    {
        email = NormalizeEmail(email);
        var subjectEntity = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.GoogleSubjectId == googleSubjectId, ct);
        var emailEntity = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var entity = ResolveProviderUser(subjectEntity, emailEntity, clearProviderSubject: user => user.GoogleSubjectId = null);
        if (entity is null)
        {
            entity = new UserEntity
            {
                Id = Guid.NewGuid(),
                GoogleSubjectId = googleSubjectId,
                Email = email,
                Name = name,
                AvatarUrl = avatarUrl,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };
            _eaosDbContext.Users.Add(entity);
        }
        else
        {
            entity.GoogleSubjectId = googleSubjectId;
            entity.Email = email;
            entity.Name = name;
            entity.AvatarUrl = avatarUrl;
            entity.LastLoginAt = DateTime.UtcNow;
        }
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToUserRecord(entity);
    }

    public async Task<UserRecord> UpsertByGitHubSubjectAsync(
        string gitHubSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct)
    {
        email = NormalizeEmail(email);
        var subjectEntity = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.GitHubSubjectId == gitHubSubjectId, ct);
        var emailEntity = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var entity = ResolveProviderUser(subjectEntity, emailEntity, clearProviderSubject: user => user.GitHubSubjectId = null);
        if (entity is null)
        {
            entity = new UserEntity
            {
                Id = Guid.NewGuid(),
                GitHubSubjectId = gitHubSubjectId,
                Email = email,
                Name = name,
                AvatarUrl = avatarUrl,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };
            _eaosDbContext.Users.Add(entity);
        }
        else
        {
            entity.GitHubSubjectId = gitHubSubjectId;
            entity.Email = email;
            entity.Name = name;
            entity.AvatarUrl = avatarUrl;
            entity.LastLoginAt = DateTime.UtcNow;
        }
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToUserRecord(entity);
    }

    public async Task<UserRecord?> GetByAsync(UserFilter filter, CancellationToken ct = default)
    {
        var query = _eaosDbContext.Users.AsNoTracking().AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(u => u.Id == filter.Id.Value);

        if (!string.IsNullOrEmpty(filter.Email))
            query = query.Where(u => u.Email == NormalizeEmail(filter.Email));

        if (!string.IsNullOrEmpty(filter.GoogleSubjectId))
            query = query.Where(u => u.GoogleSubjectId == filter.GoogleSubjectId);

        if (!string.IsNullOrEmpty(filter.GitHubSubjectId))
            query = query.Where(u => u.GitHubSubjectId == filter.GitHubSubjectId);

        var entity = await query.FirstOrDefaultAsync(ct);
        return entity is null ? null : ToUserRecord(entity);
    }

    public async Task<UserRecord> UpdateProfileAsync(
        Guid id,
        string? name,
        string? displayName,
        string? timezone,
        string? notificationPrefsJson,
        string? preferences,
        CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new InvalidOperationException($"user {id} not found");
        if (name is not null) entity.Name = name;
        if (displayName is not null) entity.DisplayName = displayName;
        if (timezone is not null) entity.Timezone = timezone;
        if (notificationPrefsJson is not null) entity.NotificationPrefsJson = notificationPrefsJson;
        if (preferences is not null) entity.Preferences = preferences;
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToUserRecord(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _eaosDbContext.Users.Where(u => u.Id == id).ExecuteDeleteAsync(ct);
    }

    // ── Mapping ──────────────────────────────────────────────────────

    internal static UserRecord ToUserRecord(UserEntity e) => new()
    {
        Id = e.Id,
        Email = e.Email,
        Name = e.Name,
        AvatarUrl = e.AvatarUrl,
        GoogleSubjectId = e.GoogleSubjectId,
        GitHubSubjectId = e.GitHubSubjectId,
        CreatedAt = e.CreatedAt,
        LastLoginAt = e.LastLoginAt,
        DisplayName = e.DisplayName,
        Timezone = e.Timezone,
        NotificationPrefsJson = e.NotificationPrefsJson,
        Preferences = e.Preferences,
        CurrentWorkspaceId = e.CurrentWorkspaceId,
    };

    private static UserEntity ToUserEntity(UserRecord r) => new()
    {
        Id = r.Id,
        Email = r.Email,
        Name = r.Name,
        AvatarUrl = r.AvatarUrl,
        GoogleSubjectId = r.GoogleSubjectId,
        GitHubSubjectId = r.GitHubSubjectId,
        CreatedAt = r.CreatedAt,
        LastLoginAt = r.LastLoginAt,
        DisplayName = r.DisplayName,
        Timezone = r.Timezone,
        NotificationPrefsJson = r.NotificationPrefsJson,
        Preferences = r.Preferences,
        CurrentWorkspaceId = r.CurrentWorkspaceId,
    };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserEntity? ResolveProviderUser(
        UserEntity? subjectEntity,
        UserEntity? emailEntity,
        Action<UserEntity> clearProviderSubject)
    {
        if (subjectEntity is null || emailEntity is null || subjectEntity.Id == emailEntity.Id)
            return subjectEntity ?? emailEntity;

        clearProviderSubject(subjectEntity);
        return emailEntity;
    }
}
