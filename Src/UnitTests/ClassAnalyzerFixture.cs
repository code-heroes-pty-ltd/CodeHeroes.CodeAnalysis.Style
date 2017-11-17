namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using System.Collections.Generic;
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public sealed class ClassAnalyzerFixture : CodeFixVerifier
    {
        [Theory]
        [MemberData(nameof(CH0008Diagnostics))]
        public void ch0008_diagnoses_correctly(string[] sources, DiagnosticResult[] diagnosticResults)
        {
            this.VerifyCSharpDiagnostic(sources, diagnosticResults);
        }

        [Theory]
        [MemberData(nameof(CH0008Fixes))]
        public void ch0008_fixes_correctly(string input, string output)
        {
            this.VerifyCSharpFix(input, output);
        }

        public static IEnumerable<object[]> CH0008Diagnostics =>
            GetDataForDiagnosticVerification(ClassAnalyzer.DiagnosticId);

        public static IEnumerable<object[]> CH0008Fixes =>
            GetDataForFixVerification(ClassAnalyzer.DiagnosticId);

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new ClassAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider() =>
            new ClassFix();
    }
}