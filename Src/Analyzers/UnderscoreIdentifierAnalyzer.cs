﻿namespace CodeHeroes.CodeAnalysis.Style
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnderscoreIdentifierAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CH0006";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Identifiers whose names consist solely of underscore characters are reserved for discard semantics",
            "Do not use the identifier, or rename it.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxTreeAction(AnalyzeIdentifiers);
        }

        private static void AnalyzeIdentifiers(SyntaxTreeAnalysisContext context)
        {
            var walker = new IdentifierWalker(context);
            walker.Visit(context.Tree.GetRoot());
        }

        private sealed class IdentifierWalker : CSharpSyntaxWalker
        {
            private readonly SyntaxTreeAnalysisContext context;

            public IdentifierWalker(SyntaxTreeAnalysisContext context)
                : base(SyntaxWalkerDepth.Node)
            {
                this.context = context;
            }

            public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
            {
                base.VisitVariableDeclaration(node);

                foreach (var variable in node.Variables)
                {
                    if (IsInvalidName(variable.Identifier.ToString()))
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            variable.GetLocation());
                        this.context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                base.VisitMemberAccessExpression(node);

                if (!(node.Expression is IdentifierNameSyntax))
                {
                    return;
                }

                var identifierNameSyntax = (IdentifierNameSyntax)node.Expression;

                if (IsInvalidName(identifierNameSyntax.Identifier.ToString()))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        identifierNameSyntax.Identifier.GetLocation());
                    this.context.ReportDiagnostic(diagnostic);
                }
            }

            private static bool IsInvalidName(string name)
            {
                for (var i = 0; i < name.Length; ++i)
                {
                    if (name[i] != '_')
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}