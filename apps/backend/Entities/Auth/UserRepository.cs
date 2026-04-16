namespace EnterpriseAgentOs.Api.Entities.Auth;

public sealed class UserRepository : IUserRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public UserRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db) => _db = db;

    public async Task<EnterpriseAgentOs.Api.Database.Models.UserRecord> UpsertByGoogleSubjectAsync(
        string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleSubjectId == googleSubjectId, ct);
        if (user is null)
        {
            user = new EnterpriseAgentOs.Api.Database.Models.UserRecord
            {
                GoogleSubjectId = googleSubjectId,
                Email = email,
                Name = name,
                AvatarUrl = avatarUrl,
            };
            _db.Users.Add(user);
        }
        else
        {
            user.Email = email;
            user.Name = name;
            user.AvatarUrl = avatarUrl;
            user.LastLoginAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.UserRecord?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
}
