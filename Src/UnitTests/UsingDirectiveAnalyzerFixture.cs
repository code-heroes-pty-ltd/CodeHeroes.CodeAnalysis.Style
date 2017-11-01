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
        public void using_directives_outside_namespace_are_flagged()
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
        [MemberData(nameof(GetSources))]
        public void using_directives_outside_namespace_can_be_fixed(string input, string output)
        {
            this.VerifyCSharpFix(input, output);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
            new UsingDirectiveDiagnosticAnalyzer();

        protected override CodeFixProvider GetCSharpCodeFixProvider() =>
            new UsingDirectiveCodeFixProvider();

        public static IEnumerable<object[]> GetSources()
        {
            foreach (var resourceNamePrefix in ResourceNamePrefixes)
            {
                var prefix = "CodeHeroes.CodeAnalysis.Style.UnitTests.UsingDirectiveAnalyzerResources." + resourceNamePrefix;

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

        private static IEnumerable<string> ResourceNamePrefixes
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
    }
}