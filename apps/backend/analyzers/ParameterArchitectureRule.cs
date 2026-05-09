using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OffceOs.Architecture.Analyzers;

internal sealed class ParameterArchitectureRule : IArchitectureRule
{
    public void Register(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var filePath = ArchitecturePaths.Normalize(context.Node.SyntaxTree.FilePath);
        if (!ArchitecturePaths.IsInLayer(filePath, "Api"))
            return;

        var parameter = (ParameterSyntax)context.Node;
        if (parameter.Type is null)
            return;

        var typeSymbol = context.SemanticModel.GetTypeInfo(parameter.Type, context.CancellationToken).Type;
        if (typeSymbol is null)
            return;

        if (Path.GetFileName(filePath).EndsWith("Mutations.cs", StringComparison.Ordinal) && IsRepositoryType(typeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ArchitectureDiagnostics.ApiMutationRepositoryInjectionRule,
                parameter.GetLocation(),
                parameter.Identifier.ValueText,
                typeSymbol.Name));
        }

        if (IsApiBoundaryParameter(context, parameter)
            && (typeSymbol.Name.EndsWith("Request", StringComparison.Ordinal)
            || typeSymbol.Name.EndsWith("Result", StringComparison.Ordinal))
            && !IsFrameworkParameter(typeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ArchitectureDiagnostics.ApiBoundaryRequestRule,
                parameter.GetLocation(),
                parameter.Identifier.ValueText,
                typeSymbol.Name));
        }
    }

    private static bool IsApiBoundaryParameter(SyntaxNodeAnalysisContext context, ParameterSyntax parameter)
    {
        var method = parameter.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is null)
            return false;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken);
        if (methodSymbol?.DeclaredAccessibility is not Accessibility.Public)
            return false;

        return methodSymbol.ContainingType?.DeclaredAccessibility is Accessibility.Public;
    }

    private static bool IsFrameworkParameter(ITypeSymbol typeSymbol) =>
        typeSymbol.ContainingNamespace.ToDisplayString().StartsWith("Microsoft.", StringComparison.Ordinal)
        || typeSymbol.ContainingNamespace.ToDisplayString().StartsWith("System.", StringComparison.Ordinal);

    private static bool IsRepositoryType(ITypeSymbol typeSymbol)
    {
        var name = typeSymbol.Name;
        if (name.StartsWith("I", StringComparison.Ordinal) && name.EndsWith("Repository", StringComparison.Ordinal))
            return true;

        return typeSymbol.AllInterfaces.Any(interfaceSymbol =>
            interfaceSymbol.Name.StartsWith("I", StringComparison.Ordinal)
            && interfaceSymbol.Name.EndsWith("Repository", StringComparison.Ordinal));
    }
}
