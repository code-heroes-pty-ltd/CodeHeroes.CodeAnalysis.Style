namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using System.Collections.Generic;
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public sealed class NamespaceAnalyzerFixture : CodeFixVerifier
    {
        [Theory]
        [MemberData(nameof(CH0004Diagnostics))]
        public void ch0004_diagnoses_correctly(string[] sources, DiagnosticResult[] diagnosticResults)
        {
            this.VerifyCSharpDiagnostic(sources, diagnosticResults);
        }

        public static IEnumerable<object[]> CH0004Diagnostics =>
            GetDataForDiagnosticVerification(NamespaceAnalyzer.DiagnosticId);

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new NamespaceAnalyzer();
    }
}