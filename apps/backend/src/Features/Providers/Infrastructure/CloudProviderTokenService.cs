using OffceOs.Features.Providers.Domain;

namespace OffceOs.Features.Providers.Infrastructure;

internal sealed class CloudProviderTokenService : ICloudProviderTokenService
{
    public Task<ProviderAuthResult> GetAwsCredentialsAsync(ProviderAuthResult auth, CancellationToken ct = default)
    {
        if (auth.Kind is ProviderAuthKind.AwsAccessKey or ProviderAuthKind.AwsIam)
            return Task.FromResult(auth);

        Amazon.Runtime.AWSCredentials credentials = auth.Kind switch
        {
            ProviderAuthKind.AwsEnvironment => Amazon.Runtime.FallbackCredentialsFactory.GetCredentials(),
            ProviderAuthKind.AwsProfile => GetProfileCredentials(
                auth.Get("awsProfile") ?? throw new InvalidOperationException("AWS profile is required.")),
            _ => throw new InvalidOperationException($"Authentication kind '{auth.Kind.ToStorageString()}' is not an AWS credential-chain flow."),
        };

        var immutable = credentials.GetCredentials();
        var values = new Dictionary<string, string>(auth.Credentials, StringComparer.OrdinalIgnoreCase)
        {
            ["authKind"] = ProviderAuthKind.AwsAccessKey.ToStorageString(),
            ["awsAccessKeyId"] = immutable.AccessKey,
            ["awsSecretAccessKey"] = immutable.SecretKey,
        };
        if (!string.IsNullOrWhiteSpace(immutable.Token))
            values["awsSessionToken"] = immutable.Token;

        return Task.FromResult(new ProviderAuthResult(ProviderAuthKind.AwsAccessKey, values));
    }

    public async Task<string> GetGoogleAccessTokenAsync(ProviderAuthResult auth, CancellationToken ct = default)
    {
        GoogleCredential credential = auth.Kind switch
        {
            ProviderAuthKind.GoogleServiceAccountFile => FromCredentialFile(
                auth.Get("credentialsPath") ?? throw new InvalidOperationException("Google credentials path is required.")),
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

    private static GoogleCredential FromCredentialFile(string path)
    {
#pragma warning disable CS0618
        return GoogleCredential.FromFile(path);
#pragma warning restore CS0618
    }

    public async Task<string> GetAzureAccessTokenAsync(ProviderAuthResult auth, CancellationToken ct = default)
    {
        TokenCredential credential = auth.Kind switch
        {
            ProviderAuthKind.AzureDefaultCredential => new DefaultAzureCredential(),
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

    private static Amazon.Runtime.AWSCredentials GetProfileCredentials(string profileName)
    {
        var chain = new Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain();
        if (!chain.TryGetAWSCredentials(profileName, out var credentials))
            throw new InvalidOperationException($"AWS profile '{profileName}' could not be loaded.");

        return credentials;
    }
}
