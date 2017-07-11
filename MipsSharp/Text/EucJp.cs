using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Text
{
    public class EucJp
    {
        public struct IsEucJpCharReturnValue
        {
            public bool IsValid
            {
                get
                {
                    return (_flags & 1) == 1;
                }
                set
                {
                    _flags &= ~(1 << 0);
                    _flags |= value ? 1 : 0;
                }
            }

            public bool IsEucJp
            {
                get
                {
                    return ((_flags >> 1) & 1) == 1;
                }
                set
                {
                    _flags &= ~(1 << 1);
                    _flags |= (value ? 1 : 0) << 1;
                }
            }

            public int NextIndex { get; set; }

            private int _flags;

            public IsEucJpCharReturnValue WithNextIndex(int nextIndex) =>
                new IsEucJpCharReturnValue
                {
                    _flags = _flags,
                    NextIndex = nextIndex
                };
        }


        /*
         * A character from the lower half of JIS-X-0201 (ASCII, code set 0) is represented by one byte, in the range 0x21 – 0x7E.
         * A character from the upper half of JIS-X-0201 (half-width kana, code set 2) is represented by two bytes, the first being 0x8E, the second in the range 0xA1 – 0xDF.
         * A character from JIS-X-0208 (code set 1) is represented by two bytes, both in the range 0xA1 – 0xFE.
         * A character from JIS-X-0212 (code set 3) is represented by three bytes, the first being 0x8F, the following two in the range 0xA1 – 0xFE.
         */

        private static bool SatisfiesRange(int num, int minInclusive, int maxInclusive) =>
            num >= minInclusive && num <= maxInclusive;

        private static bool IsAscii(byte input)
        {
            switch (input)
            {
                // Whitespace
                case 0x09:
                case 0x0A:
                case 0x0D:
                    return true;
            }

            return input >= 0x20 && input <= 0x7E;
        }


        public static IsEucJpCharReturnValue IsValidChar(IReadOnlyList<byte> data, int index, IEnumerable<byte> alsoAllow = null)
        {
            var rval = new IsEucJpCharReturnValue { IsValid = true };

            if (index >= data.Count)
                throw new IndexOutOfRangeException();

            if (IsAscii(data[index]) || alsoAllow?.Contains(data[index]) == true)
                return rval.WithNextIndex(index + 1);

            if (index + 2 > data.Count)
                goto fail;

            rval.IsEucJp = true;

            // upper half of JIS-X-0201
            if (data[index] == 0x8E && SatisfiesRange(data[index + 1], 0xA1, 0xDF))
                return rval.WithNextIndex(index + 2);

            // JIS-X-0208 (code set 1) 
            if (SatisfiesRange(data[index], 0xA1, 0xFE) && SatisfiesRange(data[index + 1], 0xA1, 0xFE))
                return rval.WithNextIndex(index + 2);

            if (index + 3 > data.Count)
                goto fail;

            // JIS-X-0212 (code set 3)
            if (data[index] == 0x8F && SatisfiesRange(data[index + 1], 0xA1, 0xFE) && SatisfiesRange(data[index + 2], 0xA1, 0xFE))
                return rval.WithNextIndex(index + 3);

        fail:
            rval.IsEucJp = false;
            rval.IsValid = false;

            return rval.WithNextIndex(index + 1);
        }


        static EucJp()
        {
            _eucJpEncoding = CodePagesEncodingProvider.Instance.GetEncoding(20932);
        }

        private static readonly Encoding _eucJpEncoding;
        
        public static string EucJpToString(IEnumerable<byte> input) => 
            Encoding.UTF8.GetString(
                Encoding.Convert(_eucJpEncoding, Encoding.UTF8, input.ToArray())
            );


        /// <summary>
        /// Used as a part of an escape sequence for setting text color.
        /// </summary>
        public static IEnumerable<byte> AsciiColorCode { get; } = new byte[] { 0x1b };

        /// <summary>
        /// Extra EUC JP bytes found in the map select list. Couldn't find any reference to them on
        /// the internet.
        /// </summary>
        public static IEnumerable<byte> UnknownExtraChars { get; } = new byte[] { 0x8C, 0x8D };


        public struct EucJpStringResult
        {
            public string String { get; set; }
            public bool ContainsEucJpCharacters
            {
                get
                {
                    return (_flags & 1) == 1;
                }
                set
                {
                    _flags &= ~1;
                    _flags |= value ? 1 : 0;
                }
            }

            private int _flags;
        }


        public static IEnumerable<EucJpStringResult> ExtractEucJpStrings(IReadOnlyList<byte> source, IEnumerable<byte> allow = null, IEnumerable<byte> allowAndDiscard = null)
        {
            var buffer = new List<byte>();

            allow = allow ?? new byte[0];
            allowAndDiscard = allowAndDiscard ?? new byte[0];

            var localAllow = allow.Concat(allowAndDiscard).Distinct();
            var localAllowAndDiscard = allowAndDiscard.ToArray();

            bool anyEucJpChars = false;

            for (int idx = 0; idx < source.Count; )
            {
                var rval = IsValidChar(source, idx, localAllow);

                anyEucJpChars |= rval.IsEucJp;

                if (rval.IsValid)
                {
                    for (int i = 0; i < rval.NextIndex - idx; i++)
                    {
                        if (rval.NextIndex - idx == 1 && localAllowAndDiscard.Contains(source[idx + i]))
                            continue;
            
                        buffer.Add(source[idx + i]);
                    }
                }
                else if (buffer.Count > 0 && source[idx] == 0)
                {
                    yield return new EucJpStringResult
                    {
                        String = EucJpToString(buffer),
                        ContainsEucJpCharacters = anyEucJpChars
                    };

                    buffer.Clear();
                    anyEucJpChars = false;
                }
                else if (buffer.Count > 0)
                {
                    buffer.Clear();
                    anyEucJpChars = false;
                }

                idx = rval.NextIndex;
            }
        }
    }
}
