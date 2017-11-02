namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public sealed class NamespaceAnalyzerFixture : CodeFixVerifier
    {
        [Fact]
        public void ch0004_flags_multiple_namespaces_in_same_file()
        {
            var source = @"
namespace Foo
{
}

namespace Bar
{
    namespace Baz
    {
    }
}

namespace Fiz
{
}";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "CH0004",
                    Message = "Separate the code into multiple files.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 6, 1)
                    }
                },
                new DiagnosticResult
                {
                    Id = "CH0004",
                    Message = "Separate the code into multiple files.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 8, 5)
                    }
                },
                new DiagnosticResult
                {
                    Id = "CH0004",
                    Message = "Separate the code into multiple files.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 13, 1)
                    }
                }
            };
            this.VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ch0004_does_not_flag_multiple_namespaces_in_different_files()
        {
            var source1 = @"
namespace Foo
{
}";
            var source2 = @"
namespace Bar
{
}";
            this.VerifyCSharpDiagnostic(new[] { source1, source2 }, new DiagnosticResult[0]);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new NamespaceDiagnosticAnalyzer();
    }
}