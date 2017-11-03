namespace CodeHeroes.CodeAnalysis.Style
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public sealed class UsingDirectiveComparer : IComparer<UsingDirectiveSyntax>
    {
        public static readonly UsingDirectiveComparer Instance = new UsingDirectiveComparer();

        private UsingDirectiveComparer()
        {
        }

        public int Compare(UsingDirectiveSyntax x, UsingDirectiveSyntax y)
        {
            var xName = x.Name.ToString();
            var yName = y.Name.ToString();
            var xIsAliased = IsAliased(x);
            var yIsAliased = IsAliased(y);

            if (xIsAliased != yIsAliased)
            {
                return xIsAliased.CompareTo(yIsAliased);
            }

            var xIsStatic = IsStatic(x);
            var yIsStatic = IsStatic(y);

            if (xIsStatic != yIsStatic)
            {
                return xIsStatic.CompareTo(yIsStatic);
            }

            var xSegment = IsSystem(xName) ? 0 : 1;
            var ySegment = IsSystem(yName) ? 0 : 1;

            if (xSegment != ySegment)
            {
                return xSegment.CompareTo(ySegment);
            }

            if (xIsAliased)
            {
                return x.Alias.Name.ToString().CompareTo(y.Alias.Name.ToString());
            }
            else
            {
                return xName.CompareTo(yName);
            }
        }

        private static bool IsAliased(UsingDirectiveSyntax usingDirectiveSyntax) =>
            usingDirectiveSyntax.Alias != null;

        private static bool IsStatic(UsingDirectiveSyntax usingDirectiveSyntax) =>
            !usingDirectiveSyntax.StaticKeyword.IsKind(SyntaxKind.None);

        private static bool IsSystem(string usingDirective) =>
            usingDirective.StartsWith("System", StringComparison.Ordinal);
    }
}