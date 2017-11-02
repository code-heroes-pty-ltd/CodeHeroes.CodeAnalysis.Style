namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using System.Collections.Generic;
    using CodeHeroes.CodeAnalysis.Style.UnitTests.TestHelper;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public sealed class UsingDirectiveAnalyzerFixture : CodeFixVerifier
    {
        [Fact]
        public void ch0002_flags_using_directives_outside_namespaces()
        {
            var source = @"using System;
using System.Collections;

namespace Foo
{
    using System.Linq;
}";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "CH0002",
                    Message = "Move the using directive within the namespace scope.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 1, 1)
                    }
                },
                new DiagnosticResult
                {
                    Id = "CH0002",
                    Message = "Move the using directive within the namespace scope.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 2, 1)
                    }
                }
            };
            this.VerifyCSharpDiagnostic(source, expected);
        }

        [Theory]
        [MemberData(nameof(CH0002Sources))]
        public void ch0002_allows_using_directives_outside_namespace_to_be_fixed(string input, string output)
        {
            this.VerifyCSharpFix(input, output);
        }

        [Fact]
        public void ch0003_flags_incorrectly_order_using_directives()
        {
            var source = @"namespace Foo
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Diagnostics;
    using Genesis.Ensure;
    using System.IO;
    using Genesis.Join;
    using Genesis.AsyncInitializationGuard;
}";
            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "CH0003",
                    Message = "Sort using directives in alphabetical order, with System usings first.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 4, 5)
                    }
                },
                new DiagnosticResult
                {
                    Id = "CH0003",
                    Message = "Sort using directives in alphabetical order, with System usings first.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 7, 5)
                    }
                },
                new DiagnosticResult
                {
                    Id = "CH0003",
                    Message = "Sort using directives in alphabetical order, with System usings first.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 9, 5)
                    }
                }
            };
            this.VerifyCSharpDiagnostic(source, expected);
        }

        [Theory]
        [MemberData(nameof(CH0003Sources))]
        public void ch0003_allows_using_directive_sort_order_to_be_fixed(string input, string output)
        {
            this.VerifyCSharpFix(input, output);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new UsingDirectiveDiagnosticAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider() =>
            new UsingDirectiveCodeFixProvider();

        public static IEnumerable<object[]> CH0002Sources =>
            GetInputsAndOutputsFromResources(
                UsingDirectiveDiagnosticAnalyzer.UsingsWithinNamespaceDiagnosticId,
                "Degenerate",
                "MisplacedNonSystemUsing",
                "MisplacedSystemUsing",
                "MisplacedSystemUsing2",
                "MultipleNamespaces",
                "NoNamespace");

        public static IEnumerable<object[]> CH0003Sources =>
            GetInputsAndOutputsFromResources(
                UsingDirectiveDiagnosticAnalyzer.UsingsSortedCorrectlyDiagnosticId,
                "Degenerate",
                "SystemUsings",
                "SystemUsingsInCompilationUnit",
                "NonSystemUsings");
    }
}