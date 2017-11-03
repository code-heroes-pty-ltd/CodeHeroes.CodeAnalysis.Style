namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using System.Collections.Generic;
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public sealed class UsingDirectiveAnalyzerFixture : CodeFixVerifier
    {
        [Theory]
        [MemberData(nameof(CH0002Diagnostics))]
        public void ch0002_diagnoses_correctly(string[] sources, DiagnosticResult[] diagnosticResults)
        {
            this.VerifyCSharpDiagnostic(sources, diagnosticResults);
        }

        [Theory]
        [MemberData(nameof(CH0002Fixes))]
        public void ch0002_fixes_correctly(string input, string output)
        {
            this.VerifyCSharpFix(input, output);
        }

        [Theory]
        [MemberData(nameof(CH0003Diagnostics))]
        public void ch0003_diagnoses_correctly(string[] sources, DiagnosticResult[] diagnosticResults)
        {
            this.VerifyCSharpDiagnostic(sources, diagnosticResults);
        }

        [Theory]
        [MemberData(nameof(CH0003Fixes))]
        public void ch0003_fixes_correctly(string input, string output)
        {
            this.VerifyCSharpFix(input, output);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new UsingDirectiveAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider() =>
            new UsingDirectiveFix();

        public static IEnumerable<object[]> CH0002Diagnostics =>
            GetDataForDiagnosticVerification(UsingDirectiveAnalyzer.UsingsWithinNamespaceDiagnosticId);

        public static IEnumerable<object[]> CH0002Fixes =>
            GetDataForFixVerification(UsingDirectiveAnalyzer.UsingsWithinNamespaceDiagnosticId);

        public static IEnumerable<object[]> CH0003Diagnostics =>
            GetDataForDiagnosticVerification(UsingDirectiveAnalyzer.UsingsSortedCorrectlyDiagnosticId);

        public static IEnumerable<object[]> CH0003Fixes =>
            GetDataForFixVerification(UsingDirectiveAnalyzer.UsingsSortedCorrectlyDiagnosticId);
    }
}