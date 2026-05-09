using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EnterpriseAgentOs.Architecture.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BackendArchitectureAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DomainDtoTypeRule = new(
        "EAOS001",
        "Domain must not define DTO types",
        "Domain type '{0}' uses the DTO suffix; use a business name such as *Record, *Filter, or a stable domain contract name",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor BroadTypesFileRule = new(
        "EAOS002",
        "Feature layers must not use broad Types files",
        "Feature file '{0}' uses the broad *Types.cs naming pattern; use a specific name such as *Input, *Payload, *Record, *Projection, or *Contracts",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ApiMutationRepositoryInjectionRule = new(
        "EAOS003",
        "API mutations must not inject repositories",
        "API mutation parameter '{0}' injects repository '{1}'; call an Application service instead",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DomainForbiddenDependencyRule = new(
        "EAOS004",
        "Domain must not depend on outer layers or transport/infrastructure frameworks",
        "Domain file references forbidden namespace '{0}'",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FeatureDtoTypeRule = new(
        "EAOS005",
        "Feature API/Application layers must not define DTO types",
        "Feature type '{0}' uses the DTO suffix; use API *Input/*Payload or Application *Request/*Result/*Projection naming",
        "EnterpriseAgentOs.Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly string[] ForbiddenDomainNamespaces =
    [
        "EnterpriseAgentOs.Api",
        "EnterpriseAgentOs.Infrastructure",
        "EnterpriseAgentOs.Database",
        "Microsoft.EntityFrameworkCore",
        "HotChocolate",
        "Microsoft.AspNetCore",
        "KubernetesClient",
        "Stripe",
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DomainDtoTypeRule,
            BroadTypesFileRule,
            ApiMutationRepositoryInjectionRule,
            DomainForbiddenDependencyRule,
            FeatureDtoTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxTreeAction(AnalyzeFileName);
        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    private static void AnalyzeFileName(SyntaxTreeAnalysisContext context)
    {
        var filePath = NormalizePath(context.Tree.FilePath);
        if (!IsFeatureFile(filePath))
            return;

        var fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith("Types.cs", StringComparison.Ordinal))
            ReportFileDiagnostic(context, BroadTypesFileRule, fileName);

        if (IsDomainFile(filePath) && fileName.EndsWith("Dto.cs", StringComparison.Ordinal))
            ReportFileDiagnostic(context, DomainDtoTypeRule, Path.GetFileNameWithoutExtension(fileName));
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var filePath = NormalizePath(context.Node.SyntaxTree.FilePath);
        if (!IsDomainFile(filePath))
            return;

        var usingDirective = (UsingDirectiveSyntax)context.Node;
        var namespaceName = usingDirective.Name?.ToString();
        if (namespaceName is null)
            return;

        var forbiddenNamespace = ForbiddenDomainNamespaces.FirstOrDefault(forbidden =>
            namespaceName.Equals(forbidden, StringComparison.Ordinal)
            || namespaceName.StartsWith(forbidden + ".", StringComparison.Ordinal));

        if (forbiddenNamespace is null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DomainForbiddenDependencyRule,
            usingDirective.GetLocation(),
            forbiddenNamespace));
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        var declaringSyntax = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (declaringSyntax is null)
            return;

        var filePath = NormalizePath(declaringSyntax.SyntaxTree.FilePath);
        if (!IsFeatureFile(filePath) || !typeSymbol.Name.EndsWith("Dto", StringComparison.Ordinal))
            return;

        var location = declaringSyntax.GetSyntax(context.CancellationToken).GetLocation();
        if (IsDomainFile(filePath))
        {
            context.ReportDiagnostic(Diagnostic.Create(DomainDtoTypeRule, location, typeSymbol.Name));
            return;
        }

        if (IsInLayer(filePath, "Api") || IsInLayer(filePath, "Application"))
            context.ReportDiagnostic(Diagnostic.Create(FeatureDtoTypeRule, location, typeSymbol.Name));
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var filePath = NormalizePath(context.Node.SyntaxTree.FilePath);
        if (!IsInLayer(filePath, "Api") || !Path.GetFileName(filePath).EndsWith("Mutations.cs", StringComparison.Ordinal))
            return;

        var parameter = (ParameterSyntax)context.Node;
        if (parameter.Type is null)
            return;

        var typeSymbol = context.SemanticModel.GetTypeInfo(parameter.Type, context.CancellationToken).Type;
        if (typeSymbol is null || !IsRepositoryType(typeSymbol))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ApiMutationRepositoryInjectionRule,
            parameter.GetLocation(),
            parameter.Identifier.ValueText,
            typeSymbol.Name));
    }

    private static bool IsRepositoryType(ITypeSymbol typeSymbol)
    {
        var name = typeSymbol.Name;
        if (name.StartsWith("I", StringComparison.Ordinal) && name.EndsWith("Repository", StringComparison.Ordinal))
            return true;

        return typeSymbol.AllInterfaces.Any(interfaceSymbol =>
            interfaceSymbol.Name.StartsWith("I", StringComparison.Ordinal)
            && interfaceSymbol.Name.EndsWith("Repository", StringComparison.Ordinal));
    }

    private static void ReportFileDiagnostic(SyntaxTreeAnalysisContext context, DiagnosticDescriptor descriptor, string argument)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var location = root.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, argument));
    }

    private static bool IsDomainFile(string filePath) =>
        IsInLayer(filePath, "Domain");

    private static bool IsFeatureFile(string filePath) =>
        filePath.Contains("/src/features/");

    private static bool IsInLayer(string filePath, string layer) =>
        filePath.Contains($"/{layer.ToLowerInvariant()}/");

    private static string NormalizePath(string filePath) =>
        filePath.Replace('\\', '/').ToLowerInvariant();
}
