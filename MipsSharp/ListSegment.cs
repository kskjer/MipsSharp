using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp
{
    public sealed class ListSegment<T> : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> _inner;
        private readonly int _offset;
        private readonly int _count;

        public int Count => _count;

        public T this[int index] => _inner[_offset + index];

        public ListSegment(IReadOnlyList<T> inner, int offset, int count)
        {
            _inner = inner;
            _offset = offset;
            _count = count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = _offset; i < _offset + _count; i++)
                yield return _inner[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
