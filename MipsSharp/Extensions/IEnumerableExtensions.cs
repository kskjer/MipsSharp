using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        public static IReadOnlyList<T> GetSegment<T>(this IReadOnlyList<T> list, int start, int count) =>
            new ListSegment<T>(list, start, count);

        public static IReadOnlyList<T> GetSegment<T>(this IReadOnlyList<T> list, int start) =>
            new ListSegment<T>(list, start, list.Count - start);
    }
}
