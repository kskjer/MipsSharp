using MipsSharp.Mips;
using System;
using System.Collections.Generic;
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
