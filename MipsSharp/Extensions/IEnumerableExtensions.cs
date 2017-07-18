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

        public static IEnumerable<TResult> SelectWithNext<TInput, TResult>(this IEnumerable<TInput> self, Func<TInput, TInput, TResult> selector)
        {
            var buffer = new TInput[2];
            var bufferIdx = 0;

            var enumerator = self.GetEnumerator();

            TInput last = default(TInput);

            while (enumerator.MoveNext())
            {
                last = buffer[bufferIdx++ % 2] = enumerator.Current;

                if (bufferIdx < 2)
                    continue;
                
                yield return selector(buffer[(bufferIdx - 2) % 2], buffer[(bufferIdx - 1) % 2]);
            }

            if (bufferIdx >= 1)
                yield return selector(last, default(TInput));
        }
    }
}
