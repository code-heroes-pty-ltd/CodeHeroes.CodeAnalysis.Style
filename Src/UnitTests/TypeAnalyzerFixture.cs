namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public sealed class TypeAnalyzerFixture : CodeFixVerifier
    {
        [Fact]
        public void ch0005_flags_multiple_top_level_classes_in_same_file()
        {
            var source = @"
class Foo { }

class Bar { }
";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "CH0005",
                    Message = "Separate the code into multiple files, or move top-level types to inner types.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 4, 1)
                    }
                }
            };
            this.VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ch0005_flags_multiple_top_level_enumerations_in_same_file()
        {
            var source = @"
enum Foo { }

enum Bar { }
";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "CH0005",
                    Message = "Separate the code into multiple files, or move top-level types to inner types.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 4, 1)
                    }
                }
            };
            this.VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ch0005_flags_multiple_top_level_interfaces_in_same_file()
        {
            var source = @"
interface Foo { }

interface Bar { }
";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "CH0005",
                    Message = "Separate the code into multiple files, or move top-level types to inner types.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 4, 1)
                    }
                }
            };
            this.VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ch0005_flags_multiple_top_level_structures_in_same_file()
        {
            var source = @"
struct Foo { }

struct Bar { }
";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "CH0005",
                    Message = "Separate the code into multiple files, or move top-level types to inner types.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 4, 1)
                    }
                }
            };
            this.VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ch0005_does_not_flag_multiple_types_in_same_file_if_all_bar_one_type_are_inner_types()
        {
            var source = @"
class Bar
{
    class Foo {}

    interface IFoo {}

    struct SFoo {}

    enum EFoo {}
}
";
            this.VerifyCSharpDiagnostic(source, new DiagnosticResult[0]);
        }

        [Fact]
        public void ch0005_does_not_flag_multiple_top_level_types_in_same_file_if_all_bar_one_type_are_delegates()
        {
            var source = @"
delegate void SomeDelegate1(int i);

delegate void SomeDelegate2(float f);

class Bar { }
";
            this.VerifyCSharpDiagnostic(source, new DiagnosticResult[0]);
        }

        [Fact]
        public void ch0005_does_not_flag_multiple_top_level_types_in_different_files()
        {
            var source1 = @"
class Foo
{
}";
            var source2 = @"
class Bar
{
}";
            this.VerifyCSharpDiagnostic(new[] { source1, source2 }, new DiagnosticResult[0]);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new TypeDiagnosticAnalyzer();
    }
}