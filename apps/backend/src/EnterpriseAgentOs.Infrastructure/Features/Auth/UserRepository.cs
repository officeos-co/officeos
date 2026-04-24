namespace EnterpriseAgentOs.Infrastructure.Features.Auth;

internal sealed class UserRepository : IUserRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public UserRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<UserRecord> UpsertByGoogleSubjectAsync(
        string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct)
    {
        var entity = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.GoogleSubjectId == googleSubjectId, ct);
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
            entity.Email = email;
            entity.Name = name;
            entity.AvatarUrl = avatarUrl;
            entity.LastLoginAt = DateTime.UtcNow;
        }
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToUserRecord(entity);
    }

    public async Task<UserRecord?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _eaosDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
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
        CreatedAt = e.CreatedAt,
        LastLoginAt = e.LastLoginAt,
        DisplayName = e.DisplayName,
        Timezone = e.Timezone,
        NotificationPrefsJson = e.NotificationPrefsJson,
        Preferences = e.Preferences,
    };

    private static UserEntity ToUserEntity(UserRecord r) => new()
    {
        Id = r.Id,
        Email = r.Email,
        Name = r.Name,
        AvatarUrl = r.AvatarUrl,
        GoogleSubjectId = r.GoogleSubjectId,
        CreatedAt = r.CreatedAt,
        LastLoginAt = r.LastLoginAt,
        DisplayName = r.DisplayName,
        Timezone = r.Timezone,
        NotificationPrefsJson = r.NotificationPrefsJson,
        Preferences = r.Preferences,
    };
}
