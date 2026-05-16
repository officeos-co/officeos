using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Features.Management.Domain;
namespace OffceOs.Features.Management.Infrastructure;

internal sealed class DeviceCodeRepository : IDeviceCodeRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public DeviceCodeRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task AddAsync(DeviceCodeRecord record, CancellationToken ct = default)
    {
        _eaosDbContext.DeviceCodes.Add(ToEntity(record));
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    public async Task<DeviceCodeRecord?> GetByDeviceCodeAsync(string deviceCode, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.DeviceCodes.AsNoTracking()
            .Include(code => code.User)
            .FirstOrDefaultAsync(code => code.DeviceCode == deviceCode, ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<DeviceCodeRecord?> GetByUserCodeAsync(string userCode, CancellationToken ct = default)
    {
        var normalized = NormalizeUserCode(userCode);
        var entity = await _eaosDbContext.DeviceCodes.AsNoTracking()
            .Include(code => code.User)
            .FirstOrDefaultAsync(code => code.UserCode == normalized, ct);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task UpdateAsync(DeviceCodeRecord record, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.DeviceCodes.FirstOrDefaultAsync(code => code.Id == record.Id, ct);
        if (entity is null) return;

        entity.DeviceCode = record.DeviceCode;
        entity.UserCode = NormalizeUserCode(record.UserCode);
        entity.UserId = record.UserId;
        entity.Status = record.Status.ToStorageString();
        entity.RunnerName = record.RunnerName;
        entity.ExpiresAt = record.ExpiresAt;
        entity.CreatedAt = record.CreatedAt;
        entity.LastPolledAt = record.LastPolledAt;
        await _eaosDbContext.SaveChangesAsync(ct);
    }

    private static DeviceCodeRecord ToRecord(DeviceCodeEntity entity) => new()
    {
        Id = entity.Id,
        DeviceCode = entity.DeviceCode,
        UserCode = entity.UserCode,
        UserId = entity.UserId,
        Status = entity.Status.ToDeviceCodeStatus(),
        RunnerName = entity.RunnerName,
        ExpiresAt = entity.ExpiresAt,
        CreatedAt = entity.CreatedAt,
        LastPolledAt = entity.LastPolledAt,
        User = entity.User is null ? null : UserRepository.ToUserRecord(entity.User),
    };

    private static DeviceCodeEntity ToEntity(DeviceCodeRecord record) => new()
    {
        Id = record.Id,
        DeviceCode = record.DeviceCode,
        UserCode = NormalizeUserCode(record.UserCode),
        UserId = record.UserId,
        Status = record.Status.ToStorageString(),
        RunnerName = record.RunnerName,
        ExpiresAt = record.ExpiresAt,
        CreatedAt = record.CreatedAt,
        LastPolledAt = record.LastPolledAt,
    };

    private static string NormalizeUserCode(string userCode) =>
        userCode.Replace("-", "", StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
}
