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

        public delegate TResult SelectWithNextDelegate<TInput, TResult>(TInput current, TInput next);
        public delegate TResult SelectWithNextAndPreviousDelegate<TInput, TResult>(TInput current, TInput next, TInput previous);

        public static IEnumerable<TResult> SelectWithNext<TInput, TResult>(this IEnumerable<TInput> self, SelectWithNextDelegate<TInput, TResult> selector)
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

        public static IEnumerable<TResult> SelectWithNext<TInput, TResult>(this IEnumerable<TInput> self, SelectWithNextAndPreviousDelegate<TInput, TResult> selector)
        {
            TInput previous = default(TInput);

            var data = self.SelectWithNext((x, next) => new { x, next });

            foreach (var d in data)
            {
                yield return selector(d.x, d.next, previous);

                previous = d.x;
            }
        }

        public static IEnumerable<IGrouping<int, T>> 
        GroupByContiguousAddresses<T>(this IEnumerable<T> input, Func<T, UInt32> selector, UInt32 diff = 4)
        {
            var ptr = 0;

            return input
                .SelectWithNext((cur, next) => new { cur, next })
                .Select(x => new { x.cur, x.next, grp = x.next == null ? ptr : (selector(x.next) - selector(x.cur) > diff ? ptr++ : ptr) })
                .GroupBy(x => x.grp, x => x.cur)
                .ToArray();
        }
    }
}
