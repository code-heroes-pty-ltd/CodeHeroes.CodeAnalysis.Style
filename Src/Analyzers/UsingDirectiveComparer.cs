namespace CodeHeroes.CodeAnalysis.Style
{
    using System;
    using System.Collections.Generic;
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
            var xSegment = IsSystem(xName) ? 0 : 1;
            var ySegment = IsSystem(yName) ? 0 : 1;

            if (xSegment != ySegment)
            {
                return xSegment.CompareTo(ySegment);
            }

            return xName.CompareTo(yName);
        }

        private static bool IsSystem(string usingDirective) =>
            usingDirective.StartsWith("System", StringComparison.Ordinal);
    }
}