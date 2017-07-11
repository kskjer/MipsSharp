using MipsSharp.Mips;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Zelda64
{
    public class OverlayCreator
    {
        public static IReadOnlyList<byte> CreateFromElf(string pathToElf)
        {
            var elf = ELFReader.Load(pathToElf);
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
                    .First(s => s.s.LoadAddress >= addr && s.s.Size <= addr - s.s.LoadAddress)
                    .i;


            var sectionsData = ours
                .SelectMany(s => s.GetContents())
                .ToInstructions()
                .ToArray();
                
            var relocs = sections.Take(3).Select(s => $".rel{s}").ToArray();
            var relSecs = elf.Sections
                .Where(s => relocs.Contains(s.Name))
                .ToArray();

            var relContents = relSecs
                .SelectMany((s, i) => s
                    .GetContents()
                    .ToWordGroups(2)
                    .Select(g => g[0] | ((g[1] & 0x3F) << 24) | (uint)((i + 1) << 30))
                )
                //.Select(g => new { Data = sectionsData[idxFromAddr(g[0])], Info = g })
                .ToArray();

            //var bssRelocs = relContents
            //    .Where(r => r[0] >= ours[3].LoadAddress && r[0] < ours[3].LoadAddress + ours[3].Size)
            //    .ToArray();


            File.WriteAllText(
                Path.Combine(Path.GetDirectoryName(pathToElf), "ovl-relocations.c"),
                string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        "static const unsigned int",
                        "relocations[] = ",
                        "{"
                    }.Concat(
                        relContents
                            .Select((r, i) => new { r = string.Format("0x{0:X8}U, ", r), i })
                            .GroupBy(r => r.i / 4)
                            .Select(g => "\t" + string.Join("", g.Select(k => k.r)))
                    )
                    .Concat(new[] { "};" })
                )
            );

            return null;
        }
    }
}
