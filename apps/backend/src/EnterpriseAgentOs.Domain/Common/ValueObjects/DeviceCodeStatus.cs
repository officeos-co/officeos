namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum DeviceCodeStatus
{
    Pending,
    Authorized,
    Expired,
}

public static class DeviceCodeStatusExtensions
{
    public static string ToStorageString(this DeviceCodeStatus status) => status switch
    {
        DeviceCodeStatus.Pending => "pending",
        DeviceCodeStatus.Authorized => "authorized",
        DeviceCodeStatus.Expired => "expired",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static DeviceCodeStatus ToDeviceCodeStatus(this string value) => value switch
    {
        "pending" => DeviceCodeStatus.Pending,
        "authorized" => DeviceCodeStatus.Authorized,
        "expired" => DeviceCodeStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown device code status: {value}"),
    };
}
