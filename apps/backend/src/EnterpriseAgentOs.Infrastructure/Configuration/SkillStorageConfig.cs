namespace EnterpriseAgentOs.Infrastructure.Configuration;

public sealed class SkillStorageConfig
{
    /// <summary>MinIO/S3 endpoint URL, e.g. <c>http://eaos-minio:9000</c>.</summary>
    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    /// <summary>Bucket name. Skill bundles stored under <c>skills/{name}.zip</c>.</summary>
    public string Bucket { get; set; } = "skills";
}
