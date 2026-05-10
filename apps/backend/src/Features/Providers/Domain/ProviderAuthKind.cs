namespace OffceOs.Domain.Features.Providers;

public enum ProviderAuthKind
{
    ApiKey,
    Gateway,
    AwsEnvironment,
    AwsProfile,
    AwsAccessKey,
    AwsIam,
    AwsBedrockApiKey,
    GoogleServiceAccountFile,
    GoogleServiceAccount,
    GoogleApplicationDefault,
    AzureDefaultCredential,
    AzureEntraClientSecret,
    AzureManagedIdentity,
    AzureApiKey,
}

public static class ProviderAuthKinds
{
    public static string ToStorageString(this ProviderAuthKind kind) => kind switch
    {
        ProviderAuthKind.ApiKey => "api_key",
        ProviderAuthKind.Gateway => "gateway",
        ProviderAuthKind.AwsEnvironment => "aws_environment",
        ProviderAuthKind.AwsProfile => "aws_profile",
        ProviderAuthKind.AwsAccessKey => "aws_access_key",
        ProviderAuthKind.AwsIam => "aws_iam",
        ProviderAuthKind.AwsBedrockApiKey => "aws_bedrock_api_key",
        ProviderAuthKind.GoogleServiceAccountFile => "google_service_account_file",
        ProviderAuthKind.GoogleServiceAccount => "google_service_account",
        ProviderAuthKind.GoogleApplicationDefault => "google_application_default",
        ProviderAuthKind.AzureDefaultCredential => "azure_default_credential",
        ProviderAuthKind.AzureEntraClientSecret => "azure_entra_client_secret",
        ProviderAuthKind.AzureManagedIdentity => "azure_managed_identity",
        ProviderAuthKind.AzureApiKey => "azure_api_key",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static ProviderAuthKind ToProviderAuthKind(this string value) => value switch
    {
        "api_key" => ProviderAuthKind.ApiKey,
        "gateway" => ProviderAuthKind.Gateway,
        "aws_environment" => ProviderAuthKind.AwsEnvironment,
        "aws_profile" => ProviderAuthKind.AwsProfile,
        "aws_access_key" => ProviderAuthKind.AwsAccessKey,
        "aws_iam" => ProviderAuthKind.AwsIam,
        "aws_bedrock_api_key" => ProviderAuthKind.AwsBedrockApiKey,
        "google_service_account_file" => ProviderAuthKind.GoogleServiceAccountFile,
        "google_service_account" => ProviderAuthKind.GoogleServiceAccount,
        "google_application_default" => ProviderAuthKind.GoogleApplicationDefault,
        "azure_default_credential" => ProviderAuthKind.AzureDefaultCredential,
        "azure_entra_client_secret" => ProviderAuthKind.AzureEntraClientSecret,
        "azure_managed_identity" => ProviderAuthKind.AzureManagedIdentity,
        "azure_api_key" => ProviderAuthKind.AzureApiKey,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown provider auth kind: {value}"),
    };
}
