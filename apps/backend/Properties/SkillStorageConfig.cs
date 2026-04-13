namespace EnterpriseAgentOs.Api.Properties;

public sealed class SkillStorageConfig
{
    /// <summary>MinIO/S3 endpoint URL, e.g. <c>http://eaos-minio:9000</c>.</summary>
    public string Endpoint { get; init; } = "http://localhost:9000";
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    /// <summary>Bucket name. Skill bundles stored under <c>skills/{name}.zip</c>.</summary>
    public string Bucket { get; init; } = "skills";
}
