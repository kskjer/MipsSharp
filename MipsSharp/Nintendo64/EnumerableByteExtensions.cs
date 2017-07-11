using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Nintendo64
{
    public static class EnumerableByteExtensions
    {
        public static IEnumerable<byte> Swap(this IEnumerable<byte> input, Endians mode)
        {
            if (mode == Endians.Big)
            {
                foreach (var b in input)
                    yield return b;

                yield break;
            }

            var index = 0;

            if (mode == Endians.V64 )
            {
                UInt16 hw = 0;

                foreach (var b in input)
                {
                    hw |= (UInt16)(b << ((index++ % 2) * 8));

                    if( index % 2 == 0 )
                    {
                        yield return (byte)(hw & 0xFF);
                        yield return (byte)(hw >> 8);
                    }
                }
            }
            else
            {
                UInt32 w = 0;

                foreach(var b in input)
                {
                    w |= (uint)(b << ((index++ % 4) * 8));

                    if( index % 4 == 0 )
                    {
                        yield return (byte)(w & 0xFF);
                        yield return (byte)(w >> 8);
                        yield return (byte)(w >> 16);
                        yield return (byte)(w >> 24);
                    }
                }
            }
        }
    }
}
