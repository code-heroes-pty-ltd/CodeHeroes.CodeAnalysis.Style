namespace CodeHeroes.CodeAnalysis.Style
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;

    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TrailingWhitespaceCodeFixProvider))]
    public sealed class TrailingWhitespaceCodeFixProvider : CodeFixProvider
    {
        private const string title = "Remove trailing whitespace";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TrailingWhitespaceDiagnosticAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.Single();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var nearestToken = root.FindToken(diagnosticSpan.Start);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: ct => RemoveWhitespaceAsync(context.Document, nearestToken, ct),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> RemoveWhitespaceAsync(Document document, SyntaxToken nearestToken, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync();
            var updatedRoot = root.ReplaceToken(nearestToken, StripSuperfluousWhitespace(nearestToken));
            var newDocument = document.WithSyntaxRoot(updatedRoot);

            return newDocument.Project.Solution;
        }

        private static SyntaxToken StripSuperfluousWhitespace(SyntaxToken token)
        {
            var result = token;
            var isEndOfFileToken = token.IsKind(SyntaxKind.EndOfFileToken);

            if (token.HasLeadingTrivia)
            {
                RemoveTrailingWhitespaceIn(() => token.LeadingTrivia, isEndOfFileToken);
            }

            if (token.HasTrailingTrivia)
            {
                RemoveTrailingWhitespaceIn(() => token.TrailingTrivia, isEndOfFileToken);
            }

            return token;

            void RemoveTrailingWhitespaceIn(Func<SyntaxTriviaList> getTriviaList, bool isForEndOfFileToken)
            {
                var leadingWhitespaceWillBeInvalid = isForEndOfFileToken;
                var triviaList = getTriviaList();

                for (var i = triviaList.Count - 1; i >= 0; --i)
                {
                    var trivia = triviaList[i];

                    switch (trivia.Kind())
                    {
                        case SyntaxKind.EndOfLineTrivia:
                        case SyntaxKind.EndOfFileToken:
                            leadingWhitespaceWillBeInvalid = true;
                            break;
                        case SyntaxKind.WhitespaceTrivia:
                            if (leadingWhitespaceWillBeInvalid)
                            {
                                token = token.ReplaceTrivia(trivia, new SyntaxTrivia());

                                // Make sure changes are reflect in the list we're working against.
                                triviaList = getTriviaList();
                            }

                            break;
                        case SyntaxKind.SingleLineCommentTrivia:
                            if (RemoveTrailingWhitespaceInSingleLineComment(trivia))
                            {
                                // Make sure changes are reflect in the list we're working against.
                                triviaList = getTriviaList();
                            }

                            leadingWhitespaceWillBeInvalid = false;
                            break;
                        case SyntaxKind.MultiLineCommentTrivia:
                            if (RemoveTrailingWhitespaceInMultiLineComment(trivia))
                            {
                                // Make sure changes are reflect in the list we're working against.
                                triviaList = getTriviaList();
                            }

                            leadingWhitespaceWillBeInvalid = false;
                            break;
                        default:
                            leadingWhitespaceWillBeInvalid = false;
                            break;
                    }
                }
            }

            bool RemoveTrailingWhitespaceInSingleLineComment(SyntaxTrivia trivia)
            {
                var comment = trivia.ToFullString();
                var whitespaceCharacters = comment.GetTrailingWhitespaceCount();

                if (whitespaceCharacters > 0)
                {
                    var newTrivia = SyntaxFactory.Comment(comment.Substring(0, comment.Length - whitespaceCharacters));
                    token = token.ReplaceTrivia(trivia, newTrivia);
                    return true;
                }

                return false;
            }

            bool RemoveTrailingWhitespaceInMultiLineComment(SyntaxTrivia trivia)
            {
                var comment = trivia.ToFullString();
                StringBuilder modifiedComment = null;
                var startOfLine = 0;

                for (var i = 0; i < comment.Length; ++i)
                {
                    var ch = comment[i];

                    if (ch == '\r' || ch == '\n')
                    {
                        var whitespaceCharacters = comment.GetTrailingWhitespaceCount(startOfLine, i - startOfLine);

                        if (whitespaceCharacters > 0)
                        {
                            if (modifiedComment == null)
                            {
                                modifiedComment = new StringBuilder(comment, 0, startOfLine, comment.Length);
                            }

                            modifiedComment.Append(comment, startOfLine, i - startOfLine - whitespaceCharacters);
                            modifiedComment.Append(ch);
                        }

                        if (ch == '\r' && i + 1 < comment.Length && comment[i + 1] == '\n')
                        {
                            // Consume any subsequent \n character so that \r and \r\n are treated the same.
                            modifiedComment?.Append('\n');
                            ++i;
                        }

                        startOfLine = i + 1;
                    }
                }

                if (modifiedComment == null)
                {
                    return false;
                }

                modifiedComment.Append(comment, startOfLine, comment.Length - startOfLine);

                var newTrivia = SyntaxFactory.Comment(modifiedComment.ToString());
                token = token.ReplaceTrivia(trivia, newTrivia);

                return true;
            }
        }
    }
}