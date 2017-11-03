namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using System.Collections.Generic;
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public sealed class UnderscoreIdentifierAnalyzerFixture : CodeFixVerifier
    {
        [Theory]
        [MemberData(nameof(CH0006Diagnostics))]
        public void ch0006_diagnoses_correctly(string[] sources, DiagnosticResult[] diagnosticResults)
        {
            this.VerifyCSharpDiagnostic(sources, diagnosticResults);
        }

        public static IEnumerable<object[]> CH0006Diagnostics =>
            GetDataForDiagnosticVerification(UnderscoreIdentifierAnalyzer.DiagnosticId);

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new UnderscoreIdentifierAnalyzer();
    }
}