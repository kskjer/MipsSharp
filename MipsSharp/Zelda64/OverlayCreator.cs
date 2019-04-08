using MipsSharp.Mips;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MipsSharp.Binutils;
using System.Collections.Immutable;

namespace MipsSharp.Zelda64
{
    public class OverlayCreator
    {
        public static string CreateCSourceFromOverlayRelocations(string symbolName, IEnumerable<uint> overlayRelocations)
        {
            return string.Join(
                Environment.NewLine,
                new[]
                {
                    "const unsigned int",
                    $"{symbolName}[] = ",
                    "{"
                }.Concat(
                    overlayRelocations
                        .Select((r, i) => new { r = string.Format("0x{0:X8}U, ", r), i })
                        .GroupBy(r => r.i / 4)
                        .Select(g => "\t" + string.Join("", g.Select(k => k.r)))
                )
                .Concat(new[] { "};" })
            );
        }


        public static ImmutableArray<uint> GetOverlayRelocationsFromElf(IReadOnlyList<byte> elfContents)
        {
            var elf = Elf.LoadFromBytes(elfContents);
            var sections = new[] { ".text", ".data", ".rodata", ".bss" };
            var ours = elf.Sections
                .Where(s => sections.Contains(s.Name))
                .Select(s => s as Section<uint>)
                .ToArray();

            Func<UInt32, int> idxFromAddr = (addr) =>
                (int)((addr - ours[0].LoadAddress) / 4);

            Func<UInt32, int> findSectionId = (addr) =>
                ours
                    .Select((s, i) => new { s, i })
                    .First(s => s.s.LoadAddress <= addr && s.s.Size > addr - s.s.LoadAddress)
                    .i;

            var sectionsData = ours
                .SelectMany(s => s.GetContents())
                .ToInstructions()
                .ToArray();

            var relocs = sections.Take(3).Select((s, i) => new { Idx = i, Name = $".rel{s}" }).ToArray();
            var relSecs = elf.Sections
                .Where(s => relocs.Any(x => x.Name == s.Name))
                .Select(s => new { s, Idx = relocs.First(x => x.Name == s.Name).Idx })
                .ToArray();

            var relContents = relSecs
                .SelectMany(s => s.s
                    .GetContents()
                    .ToWordGroups(2)
                    .Select(x =>
                    {
                        var sectionIdx = findSectionId(x[0]);
                        var section = ours[sectionIdx];

                        return new uint[] { x[0] - section.LoadAddress, x[1] };
                    })
                    .Where(x =>
                    {
                        // Make sure we don't get relocs for JALs that are outside our image
                        if (s.Idx != 0)
                            return true;

                        if ((x[1] & 0x3F) == (uint)RelocationType.R_MIPS_26)
                        {
                            var insn = ours[0].GetContents()
                                .ToInstructions()
                                .Skip((int)(x[0] / 4))
                                .First();

                            if (insn.FullTarget(ours[0].LoadAddress) < ours[0].LoadAddress)
                                return false;
                        }

                        return true;
                    })
                    .Select(g => g[0] | ((g[1] & 0x3F) << 24) | (uint)((s.Idx + 1) << 30))
                )
                .ToImmutableArray();

            return relContents;
        }

        private static string MakefileEscape(string s) =>
            s.Replace("\\", "\\\\")
             .Replace(" ", "\\ ");

        public static string GenerateMakefileForOvl(string mipsSharpPath, string ovlName, UInt32 ovlEntryPoint)
        {
            return string.Join(
                Environment.NewLine,
                new[]
                {
                   $"MIPSSHARP_PATH = {MakefileEscape(mipsSharpPath)}",
                    "",
                   $"OVL_ADDR = 0x{ovlEntryPoint.ToString("X8")}",
                   $"OVL_NAME = {ovlName}",
                    "PARTS    = $(OVL_NAME).o",
                    "TARGET   = $(OVL_NAME).elf",
                    "",
                   $"include {Path.Combine(MakefileEscape(mipsSharpPath), "dist", "z64-ovl.mk")}"
                }
            );
        }
    }
}
