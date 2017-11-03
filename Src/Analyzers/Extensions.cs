namespace CodeHeroes.CodeAnalysis.Style
{
    using System.Diagnostics;
    using Microsoft.CodeAnalysis;

    public static class Extensions
    {
        public static int GetTrailingWhitespaceCount(this string @this, int? start = null, int? length = null)
        {
            Debug.Assert(@this != null);

            var actualStart = start ?? 0;
            var actualLength = length ?? @this.Length;
            var count = 0;

            for (var i = actualStart + actualLength - 1; i >= actualStart; --i)
            {
                var ch = @this[i];

                if (char.IsWhiteSpace(ch))
                {
                    ++count;
                }
                else
                {
                    break;
                }
            }

            return count;
        }

        public static SyntaxNode FindRoot(this SyntaxNode @this)
        {
            Debug.Assert(@this != null);

            while (@this.Parent != null)
            {
                @this = @this.Parent;
            }

            return @this;
        }
    }
}