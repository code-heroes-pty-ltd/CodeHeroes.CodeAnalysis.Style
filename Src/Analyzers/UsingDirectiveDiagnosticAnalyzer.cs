namespace CodeHeroes.CodeAnalysis.Style
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UsingDirectiveDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string UsingsWithinNamespaceDiagnosticId = "CH0002";

        private static DiagnosticDescriptor UsingsWithinNamespaceRule = new DiagnosticDescriptor(
            UsingsWithinNamespaceDiagnosticId,
            "Using directives must be within the namespace scope",
            "Move the using directive within the namespace scope.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UsingsWithinNamespaceRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, ImmutableArray.Create(SyntaxKind.UsingDirective));
        }

        private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var parent = node.Parent;

            if (!parent.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                var diagnostic = Diagnostic.Create(
                    UsingsWithinNamespaceRule,
                    node.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}