namespace CodeHeroes.CodeAnalysis.Style.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
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
        [MemberData(nameof(GetCH0002Sources))]
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
        [MemberData(nameof(GetCH0003Sources))]
        public void ch0003_allows_using_directive_sort_order_to_be_fixed(string input, string output)
        {
            this.VerifyCSharpFix(input, output);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new UsingDirectiveDiagnosticAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider() =>
            new UsingDirectiveCodeFixProvider();

        public static IEnumerable<object[]> GetCH0002Sources() =>
            GetArgumentsFor("CH0002", CH0002ResourceNamePrefixes);

        private static IEnumerable<string> CH0002ResourceNamePrefixes
        {
            get
            {
                yield return "Degenerate";
                yield return "MisplacedNonSystemUsing";
                yield return "MisplacedSystemUsing";
                yield return "MisplacedSystemUsing2";
                yield return "MultipleNamespaces";
                yield return "NoNamespace";
            }
        }

        public static IEnumerable<object[]> GetCH0003Sources() =>
            GetArgumentsFor("CH0003", CH0003ResourceNamePrefixes);

        private static IEnumerable<string> CH0003ResourceNamePrefixes
        {
            get
            {
                yield return "Degenerate";
                yield return "SystemUsings";
                yield return "SystemUsingsInCompilationUnit";
                yield return "NonSystemUsings";
            }
        }

        private static IEnumerable<object[]> GetArgumentsFor(string id, IEnumerable<string> resourceNamePrefixes)
        {
            foreach (var resourceNamePrefix in resourceNamePrefixes)
            {
                var prefix = "CodeHeroes.CodeAnalysis.Style.UnitTests.Resources." + id + "." + resourceNamePrefix;

                using (var inputStream = typeof(UsingDirectiveAnalyzerFixture).GetTypeInfo().Assembly.GetManifestResourceStream(prefix + ".Input.txt"))
                using (var outputStream = typeof(UsingDirectiveAnalyzerFixture).GetTypeInfo().Assembly.GetManifestResourceStream(prefix + ".Output.txt"))
                using (var inputStreamReader = new StreamReader(inputStream))
                using (var outputStreamReader = new StreamReader(outputStream))
                {
                    var input = inputStreamReader.ReadToEnd();
                    var output = outputStreamReader.ReadToEnd();
                    var normalizedInput = input
                        .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                        .Join("\r\n");
                    var normalizedOutput = output
                        .Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                        .Join("\r\n");

                    yield return new object[] { normalizedInput, normalizedOutput };
                }
            }
        }
    }
}