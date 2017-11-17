namespace CodeHeroes.CodeAnalysis.Style
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ClassAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CH0008";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Non-static classes must be sealed, or must have derived classes",
            "Seal the class, or derive another from it.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(ReportUnsealedClasses);
        }

        private static void ReportUnsealedClasses(CompilationStartAnalysisContext context)
        {
            var unsealedClasses = new HashSet<INamedTypeSymbol>();
            var inheritedClasses = new HashSet<INamedTypeSymbol>();

            context.RegisterSymbolAction(
                analysisContext =>
                {
                    var symbol = analysisContext.Symbol;

                    if (!(symbol is INamedTypeSymbol namedTypeSymbol))
                    {
                        return;
                    }

                    if (namedTypeSymbol.TypeKind != TypeKind.Class)
                    {
                        return;
                    }

                    if (namedTypeSymbol.IsStatic)
                    {
                        return;
                    }

                    if (!namedTypeSymbol.IsSealed)
                    {
                        unsealedClasses.Add(namedTypeSymbol);
                    }

                    inheritedClasses.Add(namedTypeSymbol.BaseType);
                },
                SymbolKind.NamedType);

            context.RegisterCompilationEndAction(
                analysisContext =>
                {
                    var uninheritedClasses = unsealedClasses.Except(inheritedClasses).ToList();

                    foreach (var uninheritedClass in uninheritedClasses)
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            uninheritedClass.Locations.First());
                        analysisContext.ReportDiagnostic(diagnostic);
                    }
                });
        }
    }
}