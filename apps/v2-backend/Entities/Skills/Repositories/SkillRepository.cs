using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Entities.Skills.Interfaces;
using EnterpriseAgentOs.Api.Entities.Skills.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Api.Entities.Skills.Repositories;

public sealed class SkillRepository : ISkillRepository
{
    private readonly EaosDbContext _db;

    public SkillRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SkillRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Skills.AsNoTracking().OrderBy(s => s.DisplayName).ToListAsync(ct);
    }
}
