namespace OffceOs.Features.Management.Domain;

public interface IDeviceCodeRepository
{
    Task AddAsync(DeviceCodeRecord record, CancellationToken ct = default);
    Task<DeviceCodeRecord?> GetByDeviceCodeAsync(string deviceCode, CancellationToken ct = default);
    Task<DeviceCodeRecord?> GetByUserCodeAsync(string userCode, CancellationToken ct = default);
    Task UpdateAsync(DeviceCodeRecord record, CancellationToken ct = default);
}
