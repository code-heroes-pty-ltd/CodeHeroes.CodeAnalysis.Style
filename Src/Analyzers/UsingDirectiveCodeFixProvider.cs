namespace CodeHeroes.CodeAnalysis.Style
{
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsingDirectiveCodeFixProvider))]
    public sealed class UsingDirectiveCodeFixProvider : CodeFixProvider
    {
        private const string title = "Move using directive";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UsingDirectiveDiagnosticAnalyzer.UsingsWithinNamespaceDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.Single();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var usingDirectiveNode = root.FindNode(diagnosticSpan);
            var namespaceNode = root
                .DescendantNodes(node => !node.IsKind(SyntaxKind.NamespaceDeclaration))
                .Where(node => node.IsKind(SyntaxKind.NamespaceDeclaration))
                .FirstOrDefault();

            if (namespaceNode != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: ct => MoveUsingDirectiveAsync(context.Document, (UsingDirectiveSyntax)usingDirectiveNode, (NamespaceDeclarationSyntax)namespaceNode, ct),
                        equivalenceKey: title),
                    diagnostic);
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
    }
}