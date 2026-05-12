namespace OffceOs.Architecture.Analyzers;

internal static class ArchitecturePaths
{
    public static string Normalize(string filePath) =>
        filePath.Replace('\\', '/').ToLowerInvariant();

    public static string? GetLayer(string filePath)
    {
        if (IsInLayer(filePath, "Domain"))
            return "Domain";
        if (IsInLayer(filePath, "Application"))
            return "Application";
        if (IsInLayer(filePath, "Api"))
            return "Api";
        if (IsInLayer(filePath, "Infrastructure"))
            return "Infrastructure";
        if (IsInLayer(filePath, "EventHandlers"))
            return "EventHandlers";

        return null;
    }

    public static bool IsDomainFile(string filePath) =>
        IsInLayer(filePath, "Domain");

    public static bool IsFeatureFile(string filePath) =>
        filePath.Contains("/src/features/");

    public static bool IsBackendSourceFile(string filePath) =>
        filePath.Contains("/src/");

    public static bool IsGlobalUsingsFile(string filePath) =>
        filePath.EndsWith("/src/globalusings.cs", StringComparison.Ordinal);

    public static bool IsDatabaseMigrationFile(string filePath) =>
        filePath.IndexOf("/src/database/migrations/", StringComparison.Ordinal) >= 0;

    public static bool IsInLayer(string filePath, string layer) =>
        filePath.Contains($"/{layer.ToLowerInvariant()}/");
}
