namespace CodeHeroes.CodeAnalysis.Style
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NamespaceDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CH0004";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Only a single namespace should reside in any given file",
            "Separate the code into multiple files.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxTreeAction(AnalyzeNamespaces);
        }

        private static void AnalyzeNamespaces(SyntaxTreeAnalysisContext context)
        {
            var walker = new NamespaceWalker(context);
            walker.Visit(context.Tree.GetRoot());
        }

        private sealed class NamespaceWalker : CSharpSyntaxWalker
        {
            private readonly SyntaxTreeAnalysisContext context;
            private int namespaceDeclarationCount;

            public NamespaceWalker(SyntaxTreeAnalysisContext context)
                : base(SyntaxWalkerDepth.Node)
            {
                this.context = context;
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                base.VisitNamespaceDeclaration(node);
                ++this.namespaceDeclarationCount;

                if (this.namespaceDeclarationCount > 1)
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        node.GetLocation());
                    this.context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}