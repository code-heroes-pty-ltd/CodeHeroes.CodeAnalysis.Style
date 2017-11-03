namespace CodeHeroes.CodeAnalysis.Style
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TypeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CH0005";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Only a single top-level type should reside in any given file (delegates excepted)",
            "Separate the code into multiple files, or move top-level types to inner types.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxTreeAction(AnalyzeTypes);
        }

        private static void AnalyzeTypes(SyntaxTreeAnalysisContext context)
        {
            var walker = new NamespaceWalker(context);
            walker.Visit(context.Tree.GetRoot());
        }

        private sealed class NamespaceWalker : CSharpSyntaxWalker
        {
            private readonly SyntaxTreeAnalysisContext context;
            private int topLevelTypeDeclarationCount;

            public NamespaceWalker(SyntaxTreeAnalysisContext context)
                : base(SyntaxWalkerDepth.Node)
            {
                this.context = context;
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                base.VisitClassDeclaration(node);
                this.Diagnose(node);
            }

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
            {
                base.VisitEnumDeclaration(node);
                this.Diagnose(node);
            }

            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                base.VisitInterfaceDeclaration(node);
                this.Diagnose(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                base.VisitStructDeclaration(node);
                this.Diagnose(node);
            }

            private void Diagnose(SyntaxNode node)
            {
                if (IsTopLevel(node))
                {
                    ++this.topLevelTypeDeclarationCount;

                    if (this.topLevelTypeDeclarationCount > 1)
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            node.GetLocation());
                        this.context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            private static bool IsTopLevel(SyntaxNode declarationNode) =>
                declarationNode.Parent.IsKind(SyntaxKind.NamespaceDeclaration) || declarationNode.Parent.IsKind(SyntaxKind.CompilationUnit);
        }
    }
}