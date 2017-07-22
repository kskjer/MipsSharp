using ELFSharp.ELF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using static MipsSharp.Nintendo64.RomAssembler;

namespace MipsSharp
{
    public static class ElfExtensions
    {
        public static IEnumerable<AssembledInstruction> GetAllocatableData(this ELF<uint> self)
        {
            var properSegments = self.Segments
                .Select((x, i) =>
                {
                    var delta = self.EntryPoint - x.Address;

                    if (i == 0)
                        return new { Address = x.Address + delta, LoadAddress = x.PhysicalAddress + delta, Size = x.Size - delta };

                    return new { x.Address, LoadAddress = x.PhysicalAddress, Size = x.Size };
                })
                .ToArray();

            var withData = properSegments
                .SelectMany(x => self.Sections
                    .Where(y => y.Type == ELFSharp.ELF.Sections.SectionType.ProgBits)
                    .Where(y => y.Flags.HasFlag(ELFSharp.ELF.Sections.SectionFlags.Allocatable))
                    .Where(y => y.LoadAddress >= x.Address && y.LoadAddress < x.Address + x.Size)
                    .SelectMany(y => y.GetContents().ToWords()
                        .Select(z => (uint)IPAddress.NetworkToHostOrder((int)z))
                        .Select((z, i) => new { Ram = y.LoadAddress + (uint)i * 4, Rom = y.LoadAddress - x.Address + x.LoadAddress + (uint)i * 4, Word = z })
                        ))
                .ToArray();

            return withData
                .Select(x => new AssembledInstruction(x.Rom, x.Ram, x.Word));
        }
    }
}
