using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using MipsSharp.Mips;

namespace MipsSharp.Nintendo64
{
    public class RomAssembler
    {
        private static readonly Regex _originTagsRegex = 
            new Regex(
                @"((\.((ram|rom)_origin\s+(0x[0-9a-fA-F]+))\s+)(\.((ram|rom)_origin\s+(0x[0-9a-fA-F]+))))", 
                RegexOptions.Compiled | RegexOptions.Singleline
            );

        public static ImmutableArray<string> GetCommonLines(IEnumerable<string> input) =>
            input
                .TakeWhile(x => 
                    string.IsNullOrWhiteSpace(x) || 
                    x.Trim().StartsWith("#") || 
                    x.Trim().StartsWith(".set"))
                .ToImmutableArray();

        public class Chunk
        {
            public UInt32 RomAddress { get; }
            public UInt32 RamAddress { get; }
            public string Assembly { get; }

            public Chunk(UInt32 romaddr, UInt32 ramaddr, string asm)
            {
                RomAddress = romaddr;
                RamAddress = ramaddr;
                Assembly = asm;
            }
        }

        public static ImmutableArray<Chunk> GetChunks(string input) =>
            _originTagsRegex.Matches(input)
                .Cast<Match>()
                .SelectWithNext((cur, next) => 
                {
                    var patternEnd = cur.Index + cur.Length;
                    var contentSize = (next != null ? next.Index : input.Length) - patternEnd;

                    var romFirst = cur.Groups[4].Value == "rom";

                    return new Chunk(
                        Convert.ToUInt32(cur.Groups[romFirst ? 5 : 9].Value, 16),
                        Convert.ToUInt32(cur.Groups[romFirst ? 9 : 5].Value, 16), 
                        input.Substring(patternEnd, contentSize)
                    );
                })
                .ToImmutableArray();

        public static ImmutableArray<Chunk> GetChunksWithCommon(string input)
        {
            var lines = input.Split(new[] { "\n", Environment.NewLine }, StringSplitOptions.None);
            var common = string.Join(Environment.NewLine, GetCommonLines(lines));

            return GetChunks(input)
                .Select(x => new Chunk(x.RomAddress, x.RamAddress, common + x.Assembly))
                .ToImmutableArray();
        }


        public class AssembledInstruction
        {
            public UInt32 RomAddress { get; }
            public UInt32 RamAddress { get; }
            public Instruction Instruction { get; }

            public AssembledInstruction(UInt32 romaddr, UInt32 ramaddr, Instruction insn)
            {
                RomAddress = romaddr;
                RamAddress = ramaddr;
                Instruction = insn;
            }

            public override string ToString() =>
                string.Format("0x{0:X8} 0x{1:X8} {2}", RomAddress, RamAddress, Instruction);
        }

        public static IEnumerable<AssembledInstruction> AssembleSource(Toolchain.Configuration tcConfig, string source) =>
            GetChunksWithCommon(source)
                .SelectMany(x =>
                {
                    using (var asmObj = Toolchain.Assemble(tcConfig, x.Assembly))
                    using (var elf = Toolchain.Link(tcConfig, new[] { asmObj.Path }, Toolchain.GenerateLinkerScript(x.RamAddress)))
                    {
                        return Toolchain.ToBinary(tcConfig, elf.Path, new[] { "--remove-section", ".MIPS.abiflags" })
                            .ToInstructions()
                            .Select((y, i) => new AssembledInstruction(x.RomAddress + (uint)i * 4, x.RamAddress + (uint)i * 4, y));
                    }
                });
    }
}
