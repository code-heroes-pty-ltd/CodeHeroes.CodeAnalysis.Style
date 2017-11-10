namespace CodeHeroes.CodeAnalysis.Style
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DiscardIdentifierAnalyzer : DiagnosticAnalyzer
    {
        public const string DiscardIdentifiersAreUnusedDiagnosticId = "CH0006";

        public const string UnusedLambdaParametersHaveDiscardIdentifiersDiagnosticId = "CH0007";

        private static DiagnosticDescriptor DiscardIdentifiersAreUnusedRule = new DiagnosticDescriptor(
            DiscardIdentifiersAreUnusedDiagnosticId,
            "Identifiers whose names consist solely of underscore characters are reserved for discard semantics",
            "Do not use the identifier, or rename it.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static DiagnosticDescriptor UnusedLambdaParametersHaveDiscardIdentifiersRule = new DiagnosticDescriptor(
            UnusedLambdaParametersHaveDiscardIdentifiersDiagnosticId,
            "Unused lambda parameters should be given discard names consisting solely of underscore characters",
            "Use the identifier, or rename it.",
            "Style",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiscardIdentifiersAreUnusedRule,
            UnusedLambdaParametersHaveDiscardIdentifiersRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxTreeAction(ReportUsedDiscardIdentifiers);

            context.RegisterCompilationAction(ReportUnusedLambdaParametersWithoutDiscardIdentifiers);
        }

        private static void ReportUsedDiscardIdentifiers(SyntaxTreeAnalysisContext context)
        {
            var identifierWalker = new ReferencedIdentifierWalker(
                token =>
                {
                    if (IsDiscardName(token.ToString()))
                    {
                        var diagnostic = Diagnostic.Create(
                            DiscardIdentifiersAreUnusedRule,
                            token.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                });
            identifierWalker.Visit(context.Tree.GetRoot());
        }

        private static void ReportUnusedLambdaParametersWithoutDiscardIdentifiers(CompilationAnalysisContext context)
        {
            var usedIdentifiers = new HashSet<ISymbol>();
            var usedIdentifierWalker = new ReferencedIdentifierWalker(
                token =>
                {
                    var semanticModel = context.Compilation.GetSemanticModel(token.SyntaxTree);
                    var symbol = semanticModel.GetSymbolInfo(token.Parent).Symbol;

                    if (symbol == null)
                    {
                        return;
                    }

                    usedIdentifiers.Add(symbol);
                });
            usedIdentifierWalker.Visit(context.Compilation.SyntaxTrees.First().GetRoot());

            var declaredLambdaParameters = new HashSet<ISymbol>();
            var identifierWalker = new IdentifierWalker(
                token =>
                {
                    if (!token.Parent.IsKind(SyntaxKind.Parameter))
                    {
                        return;
                    }

                    var semanticModel = context.Compilation.GetSemanticModel(token.SyntaxTree);
                    var declaredSymbol = semanticModel.GetDeclaredSymbol(token.Parent);

                    if (declaredSymbol == null)
                    {
                        return;
                    }

                    if (!(declaredSymbol.ContainingSymbol is IMethodSymbol methodSymbol))
                    {
                        return;
                    }

                    if (methodSymbol == null || methodSymbol.MethodKind != MethodKind.LambdaMethod)
                    {
                        return;
                    }

                    declaredLambdaParameters.Add(declaredSymbol);
                });
            identifierWalker.Visit(context.Compilation.SyntaxTrees.First().GetRoot());

            var unusedLambdaParameters = declaredLambdaParameters.Except(usedIdentifiers);

            foreach (var unusedLambdaParameter in unusedLambdaParameters)
            {
                if (!IsDiscardName(unusedLambdaParameter.Name))
                {
                    var diagnostic = Diagnostic.Create(
                        UnusedLambdaParametersHaveDiscardIdentifiersRule,
                        unusedLambdaParameter.Locations.First());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsDiscardName(string name)
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

        // Report all referenced identifiers.
        private class ReferencedIdentifierWalker : CSharpSyntaxWalker
        {
            private readonly Action<SyntaxToken> callback;

            public ReferencedIdentifierWalker(Action<SyntaxToken> callback)
                : base(SyntaxWalkerDepth.Node)
            {
                this.callback = callback;
            }

            public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
            {
                base.VisitVariableDeclaration(node);

                foreach (var variable in node.Variables)
                {
                    this.VisitName(variable.Identifier);
                }
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                base.VisitMemberAccessExpression(node);
                this.VisitExpression(node.Expression);
            }

            public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
            {
                base.VisitConditionalAccessExpression(node);
                this.VisitExpression(node.Expression);
            }

            public override void VisitArgument(ArgumentSyntax node)
            {
                base.VisitArgument(node);
                this.VisitExpression(node.Expression);
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                base.VisitBinaryExpression(node);
                this.VisitExpression(node.Left);
                this.VisitExpression(node.Right);
            }

            private void VisitExpression(ExpressionSyntax node)
            {
                if (!(node is IdentifierNameSyntax))
                {
                    return;
                }

                var identifierNameSyntax = (IdentifierNameSyntax)node;
                this.VisitName(identifierNameSyntax.Identifier);
            }

            protected void VisitName(SyntaxToken name) =>
                this.callback(name);
        }

        // Report all identifiers.
        private sealed class IdentifierWalker : ReferencedIdentifierWalker
        {
            public IdentifierWalker(Action<SyntaxToken> callback)
                : base(callback)
            {
            }

            public override void VisitParameter(ParameterSyntax node)
            {
                base.VisitParameter(node);
                this.VisitName(node.Identifier);
            }

            public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                base.VisitParenthesizedLambdaExpression(node);
            }

            public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                base.VisitSimpleLambdaExpression(node);
            }
        }
    }
}