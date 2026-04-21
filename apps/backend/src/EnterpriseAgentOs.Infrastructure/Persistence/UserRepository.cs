namespace EnterpriseAgentOs.Infrastructure.Persistence;

internal sealed class UserRepository : IUserRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public UserRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<UserRecord> UpsertByGoogleSubjectAsync(
        string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct)
    {
        var user = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.GoogleSubjectId == googleSubjectId, ct);
        if (user is null)
        {
            user = new UserRecord
            {
                GoogleSubjectId = googleSubjectId,
                Email = email,
                Name = name,
                AvatarUrl = avatarUrl,
            };
            _eaosDbContext.Users.Add(user);
        }
        else
        {
            user.Email = email;
            user.Name = name;
            user.AvatarUrl = avatarUrl;
            user.LastLoginAt = DateTime.UtcNow;
        }
        await _eaosDbContext.SaveChangesAsync(ct);
        return user;
    }

    public async Task<UserRecord?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _eaosDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<UserRecord> UpdateProfileAsync(
        Guid id,
        string? name,
        string? displayName,
        string? timezone,
        string? notificationPrefsJson,
        string? preferences,
        CancellationToken ct = default)
    {
        var user = await _eaosDbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new InvalidOperationException($"user {id} not found");
        if (name is not null) user.Name = name;
        if (displayName is not null) user.DisplayName = displayName;
        if (timezone is not null) user.Timezone = timezone;
        if (notificationPrefsJson is not null) user.NotificationPrefsJson = notificationPrefsJson;
        if (preferences is not null) user.Preferences = preferences;
        await _eaosDbContext.SaveChangesAsync(ct);
        return user;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _eaosDbContext.Users.Where(u => u.Id == id).ExecuteDeleteAsync(ct);
    }
}
