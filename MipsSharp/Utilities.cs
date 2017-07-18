using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp
{
    internal class Utilities
    {
        public static uint swapEndianness(uint x)
        {
            return ((x & 0x000000ff) << 24) +  // First byte
                   ((x & 0x0000ff00) << 8) +   // Second byte
                   ((x & 0x00ff0000) >> 8) +   // Third byte
                   ((x & 0xff000000) >> 24);   // Fourth byte
        }

        public static UInt32 ReadU32(IReadOnlyList<byte> data, int pos)
        {
            UInt32 result = 0;

            for (var i = 0; i < sizeof(UInt32); i++)
                result = (result << 8) | (UInt32)data[pos + i];

            return result;
        }

        public static UInt64 ReadU64(IReadOnlyList<byte> data, int pos)
        {
            UInt64 result = 0;

            for (var i = 0; i < sizeof(UInt64); i++)
                result = (result << 8) | (UInt64)data[pos + i];

            return result;
        }

        public static void WriteU32(UInt32 word, IList<byte> target, int pos)
        {
            target[pos + 0] = (byte)(word >> 24);
            target[pos + 1] = (byte)(word >> 16);
            target[pos + 2] = (byte)(word >> 8);
            target[pos + 3] = (byte)(word >> 0);
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
