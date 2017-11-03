namespace CodeHeroes.CodeAnalysis.Style
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UsingDirectiveAnalyzer : DiagnosticAnalyzer
    {
        public const string UsingsWithinNamespaceDiagnosticId = "CH0002";
        public const string UsingsSortedCorrectlyDiagnosticId = "CH0003";

        private static DiagnosticDescriptor UsingsWithinNamespaceRule = new DiagnosticDescriptor(
            UsingsWithinNamespaceDiagnosticId,
            "Using directives must be within the namespace scope",
            "Move the using directive within the namespace scope.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor UsingsSortedCorrectlyRule = new DiagnosticDescriptor(
            UsingsSortedCorrectlyDiagnosticId,
            "Using directives must be sorted correctly",
            "Sort using directives in alphabetical order, with System usings first.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UsingsWithinNamespaceRule, UsingsSortedCorrectlyRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(EnsureUsingDirectiveIsWithinNamespaceDeclaration, ImmutableArray.Create(SyntaxKind.UsingDirective));
            context.RegisterSyntaxNodeAction(EnsureUsingDirectivesAreSortedCorrectly, ImmutableArray.Create(SyntaxKind.CompilationUnit, SyntaxKind.NamespaceDeclaration));
        }

        private static void EnsureUsingDirectiveIsWithinNamespaceDeclaration(SyntaxNodeAnalysisContext context)
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

        private static void EnsureUsingDirectivesAreSortedCorrectly(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            SyntaxList<UsingDirectiveSyntax> usingDirectives;

            if (node.IsKind(SyntaxKind.CompilationUnit))
            {
                usingDirectives = ((CompilationUnitSyntax)node).Usings;
            }
            else
            {
                usingDirectives = ((NamespaceDeclarationSyntax)node).Usings;
            }

            for (var i = 0; i < usingDirectives.Count - 1; ++i)
            {
                var compare = UsingDirectiveComparer.Instance.Compare(usingDirectives[i], usingDirectives[i + 1]);

                if (compare > 0)
                {
                    var diagnostic = Diagnostic.Create(
                        UsingsSortedCorrectlyRule,
                        usingDirectives[i].GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}