namespace CodeHeroes.CodeAnalysis.Style
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TrailingWhitespaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CH0001";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Trailing whitespace should be removed",
            "Remove trailing whitespace. Whilst this analyzer includes a code fix provider, it is recommended you install the Trailing Whitespace Visualizer add-in to automatically remove all trailing whitespace. See https://marketplace.visualstudio.com/items?itemName=MadsKristensen.TrailingWhitespaceVisualizer.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxTreeAction(AnalyzeWhitespace);
        }

        private static void AnalyzeWhitespace(SyntaxTreeAnalysisContext context)
        {
            var walker = new WhitespaceWalker(context);
            walker.Visit(context.Tree.GetRoot());
        }

        private sealed class WhitespaceWalker : CSharpSyntaxWalker
        {
            private readonly SyntaxTreeAnalysisContext context;

            public WhitespaceWalker(SyntaxTreeAnalysisContext context)
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                this.context = context;
            }

            public override void VisitToken(SyntaxToken token)
            {
                base.VisitToken(token);
                FindSuperfluousWhitespace(token, this.ReportDiagnosticFor);
            }

            private void ReportDiagnosticFor(Location location)
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    location);
                this.context.ReportDiagnostic(diagnostic);
            }
        }

        private static void FindSuperfluousWhitespace(SyntaxToken token, Action<Location> handler)
        {
            var isEndOfFileToken = token.IsKind(SyntaxKind.EndOfFileToken);

            if (token.HasLeadingTrivia)
            {
                FlagTrailingWhitespaceIn(token.LeadingTrivia, isEndOfFileToken);
            }

            if (token.HasTrailingTrivia)
            {
                FlagTrailingWhitespaceIn(token.TrailingTrivia, isEndOfFileToken);
            }

            void FlagTrailingWhitespaceIn(SyntaxTriviaList triviaList, bool isForEndOfFileToken)
            {
                // If we find whitespace before the current position, this flag indicates whether it will be considered invalid or not.
                var leadingWhitespaceWillBeInvalid = isForEndOfFileToken;

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
                                handler(trivia.GetLocation());
                            }

                            break;
                        case SyntaxKind.SingleLineCommentTrivia:
                            FlagTrailingWhitespaceInSingleLineComment(trivia);
                            leadingWhitespaceWillBeInvalid = false;
                            break;
                        case SyntaxKind.MultiLineCommentTrivia:
                            HandleMultiLineComment(trivia);
                            leadingWhitespaceWillBeInvalid = false;
                            break;
                        default:
                            leadingWhitespaceWillBeInvalid = false;
                            break;
                    }
                }
            }

            // Easiest to split this out after hitting a compiler bug. See https://twitter.com/kent_boogaart/status/923330354626183168.
            void FlagTrailingWhitespaceInSingleLineComment(SyntaxTrivia trivia)
            {
                var comment = trivia.ToFullString();
                var whitespaceCharacters = comment.GetTrailingWhitespaceCount();

                if (whitespaceCharacters > 0)
                {
                    var textSpan = new TextSpan(trivia.FullSpan.End - whitespaceCharacters, whitespaceCharacters);
                    var whitespaceLocation = Location.Create(
                        trivia.SyntaxTree,
                        textSpan);
                    handler(whitespaceLocation);
                }
            }

            void HandleMultiLineComment(SyntaxTrivia trivia)
            {
                var comment = trivia.ToFullString();
                var startOfLine = 0;

                for (var i = 0; i < comment.Length; ++i)
                {
                    var ch = comment[i];

                    if (ch == '\r' || ch == '\n')
                    {
                        var whitespaceCharacters = comment.GetTrailingWhitespaceCount(startOfLine, i - startOfLine);

                        if (whitespaceCharacters > 0)
                        {
                            var textSpan = new TextSpan(trivia.FullSpan.Start + i - whitespaceCharacters, whitespaceCharacters);
                            var whitespaceLocation = Location.Create(
                                trivia.SyntaxTree,
                                textSpan);
                            handler(whitespaceLocation);
                        }

                        if (ch == '\r' && i + 1 < comment.Length && comment[i + 1] == '\n')
                        {
                            // Consume any subsequent \n character so that \r and \r\n are treated the same.
                            ++i;
                        }

                        startOfLine = i + 1;
                    }
                }
            }
        }
    }
}