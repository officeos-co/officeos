namespace OffceOs.Domain.Features.Providers;

public interface ICloudProviderTokenService
{
    Task<ProviderAuthResult> GetAwsCredentialsAsync(ProviderAuthResult auth, CancellationToken ct = default);
    Task<string> GetGoogleAccessTokenAsync(ProviderAuthResult auth, CancellationToken ct = default);
    Task<string> GetAzureAccessTokenAsync(ProviderAuthResult auth, CancellationToken ct = default);
}
