namespace CodeHeroes.CodeAnalysis.Style
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;

    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsingDirectiveFix))]
    public sealed class UsingDirectiveFix : CodeFixProvider
    {
        private const string ch0002Title = "Move using directive";
        private const string ch0003Title = "Sort using directives";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UsingDirectiveAnalyzer.UsingsWithinNamespaceDiagnosticId, UsingDirectiveAnalyzer.UsingsSortedCorrectlyDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.Single();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            if (diagnostic.Id == UsingDirectiveAnalyzer.UsingsWithinNamespaceDiagnosticId)
            {
                var usingDirectiveNode = root.FindNode(diagnosticSpan);
                var namespaceNode = root
                    .DescendantNodes(node => !node.IsKind(SyntaxKind.NamespaceDeclaration))
                    .Where(node => node.IsKind(SyntaxKind.NamespaceDeclaration))
                    .FirstOrDefault();

                if (namespaceNode != null)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: ch0002Title,
                            createChangedSolution: ct => MoveUsingDirectiveAsync(context.Document, (UsingDirectiveSyntax)usingDirectiveNode, (NamespaceDeclarationSyntax)namespaceNode, ct),
                            equivalenceKey: ch0002Title),
                        diagnostic);
                }
            }
            else if (diagnostic.Id == UsingDirectiveAnalyzer.UsingsSortedCorrectlyDiagnosticId)
            {
                var node = root.FindNode(diagnosticSpan);
                var parentNode = node.Parent;

                if (parentNode != null)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: ch0003Title,
                            createChangedSolution: ct => SortUsingDirectivesAsync(context.Document, parentNode, ct),
                            equivalenceKey: ch0003Title),
                        diagnostic);
                }
            }
        }

        private async Task<Solution> MoveUsingDirectiveAsync(Document document, UsingDirectiveSyntax usingDirectiveNode, NamespaceDeclarationSyntax namespaceNode, CancellationToken cancellationToken)
        {
            var usings = namespaceNode.Usings;
            var insertPosition = GetInsertPositionFor(usingDirectiveNode, usings);
            var newUsings = usings.Insert(insertPosition, usingDirectiveNode);

            var root = await document.GetSyntaxRootAsync();
            var trackedRoot = root.TrackNodes(namespaceNode, usingDirectiveNode);
            trackedRoot = trackedRoot.RemoveNode(trackedRoot.GetCurrentNode(usingDirectiveNode), SyntaxRemoveOptions.KeepNoTrivia);
            trackedRoot = trackedRoot.ReplaceNode(trackedRoot.GetCurrentNode(namespaceNode), namespaceNode.WithUsings(newUsings));

            var formattedRoot = Formatter.Format(trackedRoot, document.Project.Solution.Workspace);

            var newDocument = document.WithSyntaxRoot(formattedRoot);

            return newDocument.Project.Solution;
        }

        private static int GetInsertPositionFor(UsingDirectiveSyntax usingDirectiveSyntax, SyntaxList<UsingDirectiveSyntax> usings)
        {
            var index = 0;

            while (usings.Count > index && UsingDirectiveComparer.Instance.Compare(usingDirectiveSyntax, usings[index]) > 0)
            {
                ++index;
            }

            return index;
        }

        private async Task<Solution> SortUsingDirectivesAsync(Document document, SyntaxNode parentNode, CancellationToken cancellationToken)
        {
            var isCompilationUnit = parentNode.IsKind(SyntaxKind.CompilationUnit);
            SyntaxList<UsingDirectiveSyntax> usingDirectives;

            if (isCompilationUnit)
            {
                usingDirectives = ((CompilationUnitSyntax)parentNode).Usings;
            }
            else
            {
                usingDirectives = ((NamespaceDeclarationSyntax)parentNode).Usings;
            }

            var usingDirectivesList = new List<UsingDirectiveSyntax>(usingDirectives);
            usingDirectivesList.Sort(UsingDirectiveComparer.Instance);
            usingDirectives = new SyntaxList<UsingDirectiveSyntax>().AddRange(usingDirectivesList);

            SyntaxNode newParentNode;

            if (isCompilationUnit)
            {
                newParentNode = ((CompilationUnitSyntax)parentNode).WithUsings(usingDirectives);
            }
            else
            {
                newParentNode = ((NamespaceDeclarationSyntax)parentNode).WithUsings(usingDirectives);
            }

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(parentNode, newParentNode);

            var formattedRoot = Formatter.Format(newRoot, document.Project.Solution.Workspace);

            var newDocument = document.WithSyntaxRoot(formattedRoot);

            return newDocument.Project.Solution;
        }
    }
}