namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using System.Collections.Generic;
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public sealed class TypeAnalyzerFixture : CodeFixVerifier
    {
        [Theory]
        [MemberData(nameof(CH0005Diagnostics))]
        public void ch0005_diagnoses_correctly(string[] sources, DiagnosticResult[] diagnosticResults)
        {
            this.VerifyCSharpDiagnostic(sources, diagnosticResults);
        }

        public static IEnumerable<object[]> CH0005Diagnostics =>
            GetDataForDiagnosticVerification(TypeAnalyzer.DiagnosticId);

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new TypeAnalyzer();
    }
}