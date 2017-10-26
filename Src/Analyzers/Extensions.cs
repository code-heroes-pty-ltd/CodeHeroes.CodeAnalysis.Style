namespace CodeHeroes.CodeAnalysis.Style.Analyzers
{
    using System.Diagnostics;

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
    }
}