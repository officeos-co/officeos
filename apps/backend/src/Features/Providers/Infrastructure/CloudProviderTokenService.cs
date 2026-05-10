namespace OffceOs.Infrastructure.Features.Providers;

internal sealed class CloudProviderTokenService : ICloudProviderTokenService
{
    public async Task<string> GetGoogleAccessTokenAsync(ProviderAuthResult auth, CancellationToken ct = default)
    {
        GoogleCredential credential = auth.Kind switch
        {
            ProviderAuthKind.GoogleServiceAccount => FromServiceAccountJson(
                auth.Get("serviceAccountJson") ?? throw new InvalidOperationException("Google service account JSON is required.")),
            ProviderAuthKind.GoogleApplicationDefault => await GoogleCredential.GetApplicationDefaultAsync(ct),
            _ => throw new InvalidOperationException($"Authentication kind '{auth.Kind.ToStorageString()}' is not a Google auth flow."),
        };

        if (credential.IsCreateScopedRequired)
            credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");

        return await ((ITokenAccess)credential.UnderlyingCredential).GetAccessTokenForRequestAsync(cancellationToken: ct);
    }

    private static GoogleCredential FromServiceAccountJson(string serviceAccountJson)
    {
#pragma warning disable CS0618
        return GoogleCredential.FromJson(serviceAccountJson);
#pragma warning restore CS0618
    }

    public async Task<string> GetAzureAccessTokenAsync(ProviderAuthResult auth, CancellationToken ct = default)
    {
        TokenCredential credential = auth.Kind switch
        {
            ProviderAuthKind.AzureEntraClientSecret => new ClientSecretCredential(
                auth.Get("tenantId") ?? throw new InvalidOperationException("Azure tenant ID is required."),
                auth.Get("clientId") ?? throw new InvalidOperationException("Azure client ID is required."),
                auth.Get("clientSecret") ?? throw new InvalidOperationException("Azure client secret is required.")),
            ProviderAuthKind.AzureManagedIdentity => string.IsNullOrWhiteSpace(auth.Get("clientId"))
                ? new DefaultAzureCredential()
                : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(auth.Get("clientId")!)),
            _ => throw new InvalidOperationException($"Authentication kind '{auth.Kind.ToStorageString()}' is not an Azure auth flow."),
        };

        var token = await credential.GetTokenAsync(
            new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]),
            ct);
        return token.Token;
    }
}
