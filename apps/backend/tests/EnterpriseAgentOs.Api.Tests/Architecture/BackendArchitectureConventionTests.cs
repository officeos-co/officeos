using System.Text.RegularExpressions;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Architecture;

public sealed class BackendArchitectureConventionTests
{
    private static readonly Regex DtoTypeRegex = new(@"\b(?:public|internal)\s+(?:sealed\s+)?(?:record|class|struct)\s+\w*Dto\b", RegexOptions.Compiled);
    private static readonly Regex RepositoryServiceInjectionRegex = new(@"\[Service\]\s+I\w+Repository\b|\bI\w+Repository\s+\w+", RegexOptions.Compiled);

    [Fact]
    public void Domain_does_not_define_dto_types()
    {
        var offenders = DomainFiles()
            .Where(file =>
                file.Name.EndsWith("Dto.cs", StringComparison.Ordinal)
                || DtoTypeRegex.IsMatch(File.ReadAllText(file.FullName)))
            .Select(RelativePath)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Feature_layers_do_not_use_broad_types_files()
    {
        var offenders = FeaturesRoot()
            .EnumerateFiles("*Types.cs", SearchOption.AllDirectories)
            .Select(RelativePath)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Api_mutations_do_not_inject_repositories()
    {
        var offenders = FeaturesRoot()
            .EnumerateFiles("*Mutations.cs", SearchOption.AllDirectories)
            .Where(file => IsInLayer(file, "Api"))
            .Where(file => RepositoryServiceInjectionRegex.IsMatch(File.ReadAllText(file.FullName)))
            .Select(RelativePath)
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Domain_does_not_depend_on_api_or_infrastructure_namespaces()
    {
        var forbidden = new[]
        {
            "EnterpriseAgentOs.Api.",
            "EnterpriseAgentOs.Infrastructure.",
            "EnterpriseAgentOs.Database.",
            "Microsoft.EntityFrameworkCore",
            "HotChocolate",
        };

        var offenders = DomainFiles()
            .Where(file =>
            {
                var text = File.ReadAllText(file.FullName);
                return forbidden.Any(text.Contains);
            })
            .Select(RelativePath)
            .ToList();

        Assert.Empty(offenders);
    }

    private static IEnumerable<FileInfo> DomainFiles() =>
        FeaturesRoot()
            .EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .Where(file => IsInLayer(file, "Domain"));

    private static bool IsInLayer(FileInfo file, string layer)
    {
        var marker = $"{Path.DirectorySeparatorChar}{layer}{Path.DirectorySeparatorChar}";
        return file.FullName.Contains(marker, StringComparison.Ordinal);
    }

    private static DirectoryInfo FeaturesRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var features = Path.Combine(current.FullName, "src", "Features");
            if (Directory.Exists(features))
                return new DirectoryInfo(features);
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find src/Features from test output directory.");
    }

    private static string RelativePath(FileInfo file)
    {
        var root = FeaturesRoot().Parent!.Parent!.FullName;
        return Path.GetRelativePath(root, file.FullName);
    }
}
