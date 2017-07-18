using MipsSharp.Mips;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Nintendo64
{
    public class Rom
    {
        public string FilePath { get; }
        public Endians Endian { get; }
        public IReadOnlyList<byte> Data { get; }
        public IHeader Header { get; }

        private readonly byte[] _rawData;


        public Rom(string filename)
        {
            FilePath = filename;
            Data = _rawData = File.ReadAllBytes(filename);

            switch(Utilities.ReadU32(Data, 0))
            {
                case HeaderInfo.Magic:
                    break;

                case HeaderInfo.MagicLE:
                    Data = Data.Swap(Endians.Little).ToList();
                    break;

                case HeaderInfo.MagicV64:
                    Data = Data.Swap(Endians.V64).ToList();
                    break;

                default:
                    throw new ArgumentException("Invalid N64 ROM");
            }

            Header = new HeaderImpl(this);
        }


        public IReadOnlyList<byte> GetSegment(int start, int size) =>
            new ArraySegment<byte>(_rawData, start, size);

        public IReadOnlyList<byte> GetSegment(int start) =>
            new ArraySegment<byte>(_rawData, start, _rawData.Length - start);

        public IEnumerable<InstructionWithPc> GetExecutable()
        {
            var pc = Header.EntryPoint;
            var insns = GetSegment(0x1000, Math.Max(_rawData.Length - 0x1000, 2 * 1024 * 1024)).ToInstructions();

            foreach(var i in insns)
            {
                yield return new InstructionWithPc(pc, i);

                pc += 4;
            }
        }

        public static class BootCode
        {
            public const UInt32 CIC6102 = 0xF8CA4DDC;
            public const UInt32 CIC6103 = 0xA3886759;
            public const UInt32 CIC6105 = 0xDF26F436;
            public const UInt32 CIC6106 = 0x1FEA617A;

            public static int IdentifyFromRom(IReadOnlyList<byte> romContents)
            {
                var toCheck = romContents.GetSegment(0x40, 0x1000 - 0x40);

                switch (CalculateCrc(toCheck))
                {
                    case 0x6170A4A1: return 6101;
                    case 0x90BB6CB5: return 6102;
                    case 0x0B050EE0: return 6103;
                    case 0x98BC2C86: return 6105;
                    case 0xACC8580A: return 6106;
                }

                return 6105;
            }
        }


        private static readonly UInt32[] _crcTable = new UInt32[256];

        static Rom()
        {
            uint crc, poly;
            int i, j;

            poly = 0xEDB88320;

            for (i = 0; i < 256; i++)
            {
                crc = (uint)i;

                for (j = 8; j > 0; j--)
                {
                    if ((crc & 1) > 0)
                        crc = (crc >> 1) ^ poly;
                    else
                        crc >>= 1;
                }

                _crcTable[i] = crc;
            }
        }

        private static UInt32 CalculateCrc(IReadOnlyList<byte> data)
        {
            UInt32 crc = 0xFFFFFFFF;
            int i;

            for (i = 0; i < data.Count; i++)
            {
                crc = (crc >> 8) ^ _crcTable[(crc ^ ((uint)data[i])) & 0xFF];
            }

            return ~crc;
        }


        public static ImmutableArray<UInt32> RecalculateCrc(IReadOnlyList<byte> romContents)
        {
            UInt32 ROL(UInt32 x, int b) => 
                (((x) << (b)) | ((x) >> (32 - (b))));

            int i, bootcode;
            UInt32 seed;

            UInt32 t1, t2, t3;
            UInt32 t4, t5, t6;
            UInt32 r, d;

            UInt32[] crc = new UInt32[2];

            switch ((bootcode = BootCode.IdentifyFromRom(romContents)))
            {
                case 6101:
                case 6102:
                    seed = BootCode.CIC6102;
                    break;

                case 6103:
                    seed = BootCode.CIC6103;
                    break;

                case 6105:
                    seed = BootCode.CIC6105;
                    break;

                case 6106:
                    seed = BootCode.CIC6106;
                    break;

                default:
                    throw new Exception(String.Format("Unknown bootcode {0}", bootcode));
            }

            t1 = t2 = t3 = t4 = t5 = t6 = seed;

            var csum1 = romContents.GetSegment(0x1000, 0x100000);

            for (i = 0; i < csum1.Count; i += 4)
            {
                UInt32 word = Utilities.ReadU32(csum1, i);

                d = word;

                if ((t6 + d) < t6)
                    t4++;

                t6 += d;
                t3 ^= d;
                r = ROL(d, (int)(d & 0x1F));

                t5 += r;

                if (t2 > d)
                    t2 ^= r;
                else
                    t2 ^= t6 ^ d;

                if (bootcode == 6105)
                    t1 += Utilities.ReadU32(romContents, 0x40 + 0x0710 + (i & 0xFF)) ^ d;
                else
                    t1 += t5 ^ d;
            }

            switch (bootcode)
            {
                case 6103:
                    crc[0] = (t6 ^ t4) + t3;
                    crc[1] = (t5 ^ t2) + t1;
                    break;

                case 6106:
                    crc[0] = (t6 * t4) + t3;
                    crc[1] = (t5 * t2) + t1;
                    break;

                default:
                    crc[0] = t6 ^ t4 ^ t3;
                    crc[1] = t5 ^ t2 ^ t1;
                    break;
            }

            return crc.ToImmutableArray();
        }

        public static void ApplyCrcs(IList<byte> romContents, IReadOnlyList<UInt32> newCrcs)
        {
            Utilities.WriteU32(newCrcs[0], romContents, 16);
            Utilities.WriteU32(newCrcs[1], romContents, 20);
        }

        public interface IHeader
        {
            UInt32 EntryPoint { get; }
            UInt32[] Crc { get; }
            string Name { get; }
        }

        private class HeaderImpl : IHeader
        {
            private readonly Rom _rom;

            public HeaderImpl(Rom r)
            {
                _rom = r;

                Crc = new UInt32[2]
                {
                    Utilities.ReadU32(_rom.Data, 16),
                    Utilities.ReadU32(_rom.Data, 20)
                };

                EntryPoint = Utilities.ReadU32(_rom.Data, 8);
                Name = Encoding.ASCII.GetString(_rom.Data
                    .Skip(32)
                    .Take(20)
                    .ToArray()
                );
            }

            public uint[] Crc { get; }
            public uint EntryPoint { get; }
            public string Name { get; }
        }

        private static class HeaderInfo
        {
            public const uint PI_BSB_DOM1_LAT_REG  = 0x80;
            public const uint PI_BSB_DOM1_PGS_REG  = 0x37;
            public const uint PI_BSB_DOM1_PWD_REG  = 0x12;
            public const uint PI_BSB_DOM1_PGS_REG2 = 0x40;

            /// <summary>
            /// First four bytes identifying N64 ROMs.
            /// </summary>
            public const UInt32 Magic = (
                (PI_BSB_DOM1_LAT_REG  << 24) |
                (PI_BSB_DOM1_PGS_REG  << 16) |
                (PI_BSB_DOM1_PWD_REG  << 8)  |
                 PI_BSB_DOM1_PGS_REG2
            );

            /// <summary>
            /// 16-bit little endian magic
            /// </summary>
            public const UInt32 MagicV64 = (
                (PI_BSB_DOM1_PGS_REG  << 24) |
                (PI_BSB_DOM1_LAT_REG  << 16) |
                (PI_BSB_DOM1_PGS_REG2 << 8)  |
                 PI_BSB_DOM1_PWD_REG
            );

            /// <summary>
            /// 32-bit little endian format.
            /// </summary>
            public const UInt32 MagicLE = (
                (PI_BSB_DOM1_PGS_REG2 << 24) |
                (PI_BSB_DOM1_PWD_REG  << 16) |
                (PI_BSB_DOM1_PGS_REG  << 8)  |
                 PI_BSB_DOM1_LAT_REG
           );
        }
    }
}
