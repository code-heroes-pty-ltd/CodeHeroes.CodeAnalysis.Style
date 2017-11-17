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

    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassFix))]
    public sealed class ClassFix : CodeFixProvider
    {
        private const string ch0008Title = "Seal class";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ClassAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.Single();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var classDeclarationNode = root.FindNode(diagnosticSpan) as ClassDeclarationSyntax;

            if (classDeclarationNode == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var namedType = semanticModel.GetDeclaredSymbol(classDeclarationNode, context.CancellationToken);

            if (namedType == null || namedType.IsAbstract)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: ch0008Title,
                    createChangedSolution: ct => SealClassAsync(context.Document, classDeclarationNode, ct),
                    equivalenceKey: ch0008Title),
                diagnostic);
        }

        private async Task<Solution> SealClassAsync(Document document, ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
        {
            var newClassDeclarationSyntax = classDeclarationSyntax
                .WithModifiers(classDeclarationSyntax.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword)));

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(classDeclarationSyntax, newClassDeclarationSyntax);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument.Project.Solution;
        }
    }
}