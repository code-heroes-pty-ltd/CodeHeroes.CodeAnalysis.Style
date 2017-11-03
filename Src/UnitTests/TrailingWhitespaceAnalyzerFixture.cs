namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using System.Collections.Generic;
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    // TODO: ADD SUPPORT FOR DOC COMMENTS (STARTING WITH /** and ///)

    public sealed class TrailingWhitespaceAnalyzerFixture : CodeFixVerifier
    {
        [Theory]
        [MemberData(nameof(CH0001Diagnostics))]
        public void ch0001_diagnoses_correctly(string[] sources, DiagnosticResult[] diagnosticResults)
        {
            this.VerifyCSharpDiagnostic(sources, diagnosticResults);
        }

        [Theory]
        [MemberData(nameof(CH0001Fixes))]
        public void ch0001_fixes_correctly(string input, string output)
        {
            Assert.NotEqual(input, output);
            this.VerifyCSharpFix(input, output);
        }

        public static IEnumerable<object[]> CH0001Diagnostics =>
            GetDataForDiagnosticVerification(TrailingWhitespaceDiagnosticAnalyzer.DiagnosticId);

        public static IEnumerable<object[]> CH0001Fixes =>
            GetDataForFixVerification(TrailingWhitespaceDiagnosticAnalyzer.DiagnosticId);

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new TrailingWhitespaceDiagnosticAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider() =>
            new TrailingWhitespaceCodeFixProvider();
    }
}