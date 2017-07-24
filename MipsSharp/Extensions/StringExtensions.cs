using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp
{
    public static class StringExtensions
    {
        /// <summary>
        /// Duplicates a string <paramref name="count"/> times.
        /// </summary>
        public static string Dup(this string input, int count) =>
            string.Join("", Enumerable.Range(0, count).Select(x => input));

        public static string Join(this IEnumerable<string> input, string separator) =>
            string.Join(separator, input);
    }
}
