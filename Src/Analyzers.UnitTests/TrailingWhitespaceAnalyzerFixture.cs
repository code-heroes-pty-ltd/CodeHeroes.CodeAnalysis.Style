namespace Analyzers.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using TestHelper;
    using Xunit;

    // TODO: ADD SUPPORT FOR DOC COMMENTS (STARTING WITH /** and ///)

    public sealed class TrailingWhitespaceAnalyzerFixture : CodeFixVerifier
    {
        private const string source = @"

using System;

public class Foo : Bar
{
    public string Greeting=> ""Hello"";

    public string Name
    {
        get => ""Fred"";
        set => {}
    }

    // single-line comment
    public int Baz(int param1, int     param2)
    {
        {}
        {    }

        return 42;
    }

    /*
     * Multi-line comment
     */
    public float Fiz() =>
        42f;
}


";

        [Theory]
        [MemberData(nameof(GetPersonFromDataGenerator))]
        public void trailing_whitespace_is_flagged_and_can_be_removed(string whitespace, string newLine)
        {
            var lineInfos = source
                .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select((line, index) => (text: line, lineNumber: index + 1));
            var normalizedSource = lineInfos
                .Select(lineInfo => lineInfo.text)
                .Join(newLine);
            var rewrittenSource = lineInfos
                .Select(lineInfo => lineInfo.text + whitespace)
                .Join(newLine);
            var expected = lineInfos
                .Select(
                    lineInfo =>
                        new DiagnosticResult
                        {
                            Id = "TrailingWhitespace",
                            Message = "Remove trailing whitespace. Whilst this analyzer includes a code fix provider, it is recommended you install the Trailing Whitespace Visualizer add-in to automatically remove all trailing whitespace. See https://marketplace.visualstudio.com/items?itemName=MadsKristensen.TrailingWhitespaceVisualizer.",
                            Severity = DiagnosticSeverity.Warning,
                            Locations = new[]
                            {
                                new DiagnosticResultLocation("Test0.cs", lineInfo.lineNumber, lineInfo.text.Length + 1)
                            }
                        })
                .ToArray();

            this.VerifyCSharpDiagnostic(rewrittenSource, expected);

            this.VerifyCSharpFix(rewrittenSource, normalizedSource);
        }

        public static IEnumerable<object[]> GetPersonFromDataGenerator()
        {
            var whitespaces = new[]
            {
                " ",
                "    ",
                "\t",
                " \t\t     \t"
            };
            var newLines = new[]
            {
                "\r\n",
                "\r",
                "\n"
            };

            foreach (var whitespace in whitespaces)
            {
                foreach (var newLine in newLines)
                {
                    yield return new[] { whitespace, newLine };
                }
            }
        }

        // Useful for quick, standalone tests.
        [Fact]
        public void test_bed()
        {
            var source = @"// hello  ";
            var expected = @"// hello";

            this.VerifyCSharpFix(source, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new TrailingWhitespaceDiagnosticAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider() =>
            new TrailingWhitespaceCodeFixProvider();
    }
}