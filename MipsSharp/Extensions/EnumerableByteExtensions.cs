using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp
{
    public static class EnumerableByteExtensions
    {
        public static IEnumerable<UInt32> ToWords(this IEnumerable<byte> bytes)
        {
            var i = 0;
            UInt32 w = 0;

            foreach (var b in bytes)
            {
                w |= (UInt32)b << (i++ * 8);

                if ((i % 4) == 0)
                {
                    yield return w;
                    w = 0;
                }
            }
        }

        public static IEnumerable<UInt32[]> ToWordGroups(this IEnumerable<byte> bytes, int wordsPerGroup)
        {
            UInt32 word = 0;
            int index = 0;
            int groupIndex = 0;
            UInt32[] group = new UInt32[wordsPerGroup];

            foreach (var b in bytes)
            {
                word |= (uint)b << (24 - (index % 4) * 8);
                index++;


                if (index > 0 && (index % 4) == 0)
                {
                    group[groupIndex++ % wordsPerGroup] = word;
                    
                    if(groupIndex % wordsPerGroup == 0)
                    {
                        yield return group;
                        group = new UInt32[wordsPerGroup];
                    }

                    word = 0;
                }
            }
        }

        public static IEnumerable<string> ExtractStrings(this IEnumerable<byte> bytes, int count)
        {
            int i = 0;
            string current = "";

            foreach(var b in bytes)
            {
                if (i >= count)
                    yield break;

                if(b == 0)
                {
                    if (!string.IsNullOrEmpty(current))
                    {
                        yield return current;
                        current = "";
                        i++;
                    }

                    continue;
                }

                if (b < 0x20 || b > 0x7F)
                {
                    if (!string.IsNullOrEmpty(current))
                        yield return current;

                    yield break;
                }

                current += Encoding.ASCII.GetString(new[] { b });
            }
        }

        public static IEnumerable<IReadOnlyList<byte>> GetGroupsOfBytesSeparatedBy(this IEnumerable<byte> bytes, byte separator)
        {
            var current = new List<byte>();

            foreach (var b in bytes)
            {
                if (b == separator)
                {
                    if (current.Count > 0)
                    {
                        yield return current;
                        current = new List<byte>();
                    }
                }
                else
                {
                    current.Add(b);
                }
            }

            if (current.Count > 0)
                yield return current;
        }
    }
}
