using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp.Extensions
{
    public static class IReadOnlyListExtensions
    {
        public static T[] ToArray<T>(this IReadOnlyList<T> input)
        {
            if (input is T[])
                return input as T[];

            return (input as IEnumerable<T>).ToArray();
        }
    }
}
