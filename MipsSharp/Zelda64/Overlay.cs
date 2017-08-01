using MipsSharp.Mips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Text.RegularExpressions;
using MipsSharp.Sys;
using MipsSharp.Exceptions;

namespace MipsSharp.Zelda64
{
    public class Overlay
    {

        public class SectionInformation
        {
            public int Id { get; }
            public string Name { get; }
            public UInt32 StartAddress { get; }
            public UInt32 EndAddress => StartAddress + Size;
            public UInt32 Size { get; }
            public IReadOnlyList<byte> Data { get; }

            public override string ToString() =>
                string.Format("0x{0:X8} {1} ({2:0.00} kB)", StartAddress, Name, Size / 1024.0f);

            public SectionInformation(int id, string name, IReadOnlyList<byte> data, UInt32 startAddress, UInt32 size)
            {
                Id = id;
                Name = name;
                Size = size;
                StartAddress = startAddress;
                Data = data;
            }
        }

        private interface IInternalRelocation
        {
            Relocation Sibling { get; set; }
            UInt32 Address { set; }
        }


        public class Relocation : IInternalRelocation
        {
            public int Id { get; }
            public UInt32 Word { get; }

            public int SectionId => (int)((Word >> 30) - 1);
            public RelocationType Type => (RelocationType)(Word >> 24 & 0x3F);

            /// <summary>
            /// The section relative location of the data to be relocated.
            /// </summary>
            public UInt32 Location => Word & 0x00FFFFFF;
            public UInt32 AbsoluteLocation => Location + Section.StartAddress;

            private static string PadRelocName(RelocationType type) =>
                type.ToString().PadRight(nameof(RelocationType.R_MIPS_HI16).Length);

            public override string ToString()
            {
                try
                {
                    return string.Format("[0x{0:X8}/0x{5:X8}] (0x{4:X8}) {1} {2}{3}", Location, PadRelocName(Type), SectionNames[SectionId].PadRight(14), Disassembly.Trim(), Address, AbsoluteLocation);
                }
                catch
                {
                    return $" {("0x" + Location.ToString("X8")).PadRight(35)} {PadRelocName(Type)} {SectionNames[SectionId].PadRight(14)}{Disassembly.Trim()}";
                }
            }

            private static readonly Regex _spacingRegex = new Regex(@"\s+", RegexOptions.Compiled);

            public string ToShortString()
            {
                return _spacingRegex.Replace(ToString().Trim(), " ");
            }

            public SectionInformation Section =>
                _parent.Sections[SectionId];

            public UInt32 Data => 
                Utilities.ReadU32(Section.Data, (int)Location);

            public string Disassembly =>
                Type == RelocationType.R_MIPS_32
                ? string.Format("0x{0:X8}", Data)
                : Disassembler.Default.Disassemble(Section.StartAddress + Location, Data).ToString();

            private UInt32? _address;
            private Relocation _sibling;

            private void SetSibling()
            {
                if (_sibling != null)
                    return;

                switch(Type)
                {
                    case RelocationType.R_MIPS_HI16:
                        break;

                    case RelocationType.R_MIPS_LO16:
                        _sibling = _parent.Relocations
                            .Reverse()
                            .SkipWhile(r => r.Location > Location)
                            .First(r => r.Type == RelocationType.R_MIPS_HI16 &&
                                        new Instruction(Data).GprBase == new Instruction(r.Data).GprRt);

                        _sibling._sibling = this;
                        break;

                    default:
                        throw new Exception();
                }
            }


            public UInt32 Address
            {
                get
                {
                    if (_address.HasValue)
                        return _address.Value;

                    switch(Type)
                    {
                        case RelocationType.R_MIPS_32:
                            _address = Data;
                            break;

                        case RelocationType.R_MIPS_26:
                            _address = new Instruction(Data).FullTarget(Section.StartAddress);
                            break;

                        case RelocationType.R_MIPS_HI16:
                        case RelocationType.R_MIPS_LO16:

                            SetSibling();

                            var hilo = Type == RelocationType.R_MIPS_HI16
                                ? new[] { this, _sibling }
                                : new[] { _sibling, this };

                            try
                            {
                                var sum =
                                    (new Instruction(hilo[0].Data).Immediate << 16) + new Instruction(hilo[1].Data).ImmediateSigned;

                                _address = (UInt32)sum;
                            }
                            catch (Exception)
                            {

                                throw;
                            }

                            break;

                        default:
                            throw new Exception();
                    }

                    return _address.Value;
                }
            }

            private TypeHint? _cachedHint;

            public TypeHint TypeHint =>
                _cachedHint ?? (_cachedHint =
                    SectionId == 0
                    ? new Instruction(Data).TypeHint
                    : 0
                ).Value;

            private readonly Overlay _parent;

            public Relocation(UInt32 word, Overlay parent, int id)
            {
                Id = id;
                Word = word;
                _parent = parent;
            }


            private IEnumerable<string> _context =>
                _parent.Relocations
                    .Where(r => r.Location >= Location - 4 * 4 && r.Location < Location + 4 * 4)
                    .Select(i => $"{i.AbsoluteLocation.ToString("X8")}: {i.Disassembly}" + (i.Location == Location ? "    <" : ""));

            Relocation IInternalRelocation.Sibling
            {
                get { return _sibling; }
                set { _sibling = value; }
            }

            uint IInternalRelocation.Address
            {
                set { _address = value; }
            }
        }

        public IReadOnlyList<SectionInformation> Sections { get; }
        public IOverlayRelocations Relocations { get; }

        private SectionInformation FindSection(UInt32 address) =>
            Sections.Single(s => address >= s.StartAddress && address < s.EndAddress);

        public const int SectionCount = 4;
        public const int HeaderSize = sizeof(UInt32) * SectionCount + sizeof(UInt32);
        public static IReadOnlyList<string> SectionNames { get; } = new[] { ".text", ".data", ".rodata", ".bss" };

        public UInt32 EntryPoint { get; }
        public UInt32 Size => (UInt32)Sections.Sum(s => s.Size) + HeaderOffset;
        public UInt32 EndAddress => EntryPoint + Size;

        public string LinkerScript { get; }

        private readonly IEnumerable<KeyValuePair<UInt32, string>> _extraSymbols;


        public UInt32 HeaderOffset { get; }

        public class Options
        {
            public bool NumberSymbols { get; set; }
        }


        public Overlay(UInt32 entryPoint, IReadOnlyList<byte> overlayData, Options options = null, IEnumerable<KeyValuePair<UInt32, string>> extraSymbols = null)
        {
            options = options ?? new Options();

            EntryPoint = entryPoint;
            _extraSymbols = extraSymbols ?? new KeyValuePair<UInt32, string>[0];

            var headerOffset = HeaderOffset = Utilities.ReadU32(overlayData, overlayData.Count - sizeof(UInt32));
            var headerStart = overlayData.Count - (int)headerOffset;
            var sectionPtr = 0u;

            var sections = Enumerable.Range(0, SectionCount)
                .Select((i, idx) =>
                {
                    var size = Utilities.ReadU32(overlayData, headerStart + i * 4);
                    var rval = new SectionInformation(
                        i,
                        SectionNames[i],
                        new ListSegment<byte>(overlayData, (int)sectionPtr, (int)size),
                        entryPoint + sectionPtr +
                        // The BSS section appears to be offset by the size of the header / relocations 
                        // section. Here, we add the size of the header to the start of the bss.
                        (uint)(idx == 3 ? headerOffset : 0),
                        size 
                    );

                    sectionPtr += size;

                    return rval;
                })
                .ToList();

            var relocationCount = Utilities.ReadU32(overlayData, headerStart + SectionCount * 4);

            var relocations = Enumerable.Range(0, (int)relocationCount)
                .Select((i, _) => new Relocation(Utilities.ReadU32(overlayData, headerStart + HeaderSize + i * 4), this, 0))
                .OrderBy(r => r.SectionId)
                .ToList();

            Sections = sections;
            Relocations = new OverlayRelocations(relocations);



            var hiLoRelocs = relocations
                .Where(r => r.Type == RelocationType.R_MIPS_HI16 || r.Type == RelocationType.R_MIPS_LO16)
                .ToArray();

            Relocation lastHi16 = null;

            foreach (var r in hiLoRelocs)
            {
                if (r.Type == RelocationType.R_MIPS_HI16)
                {
                    lastHi16 = r;
                }
                else
                {
                    if (lastHi16 == null)
                        throw new RelocationException(
                            $"Relocation `{r.ToShortString()}` has no preceding R_MIPS_HI16.",
                            relocations,
                            r.Location
                        );


                    var address = ((IInternalRelocation)r).Address =
                        (UInt32)((new Instruction(lastHi16.Data).Immediate << 16) + new Instruction(r.Data).ImmediateSigned);

                    if (((IInternalRelocation)lastHi16).Sibling == null)
                    {
                        ((IInternalRelocation)lastHi16).Sibling = r;
                        ((IInternalRelocation)lastHi16).Address = address;
                    }

                    ((IInternalRelocation)r).Sibling = lastHi16;
                }
            }

            var textInsns = Sections[0].Data
                    .ToInstructions()
                    .ToArray();

            var fns = textInsns
                .DiscoverFunctions(EntryPoint)
                .ToArray();

            IEnumerable<Symbol> GenerateSymbols() =>
                Relocations
                    .OrderBy(r => r.Address)
                    .GroupBy(r => r.Address)
                    .Select((g, i) => {

                        var typeHint = g
                            .Select(t => t.TypeHint)
                            .Aggregate((t1, t2) => t1 | t2);

                        return options.NumberSymbols
                            ? new Symbol(g.Key, $"{Symbol.HintToName(typeHint)}_{i}", typeHint)
                            : new Symbol(g.Key, typeHint);
                    })
                    .Concat(
                        textInsns
                            .DiscoverBranchTargets(EntryPoint)
                            .GroupBy(t => fns.First(f => t >= f.StartAddress && t < f.EndAddress).StartAddress)
                            .SelectMany((g, outerI) => g.Select((b, i) => new Symbol(b, $"$L{outerI}_{i}", TypeHint.BranchTarget)))
                    )
                    .Concat(
                        textInsns
                            .DiscoverFunctionCalls(EntryPoint)
                            .Where(f => f < EntryPoint)
                            .Select(f =>
                                f < EntryPoint
                                ? new Symbol(f, string.Format("external_func_{0:X8}", f), TypeHint.Function, SymbolType.External)
                                : new Symbol(f, string.Format("func_{0:X8}", f), TypeHint.Function, SymbolType.External))
                    )
                    .Concat(
                        _extraSymbols
                            .Select(s => new Symbol(s.Key, s.Value, 0, SymbolType.External))
                    );

            Symbols =
                new OverlaySymbols(
                    this,
                    GenerateSymbols(),
                    Sections[0]
                );

            var extra = GenerateExtraSymbols(this, Symbols);

            Symbols = new OverlaySymbols(
                this,
                GenerateSymbols()
                    .Select(x =>
                    {
                        if (extra.offsetSymbols.ContainsKey(x.Name))
                            return new Symbol(x.Address, extra.offsetSymbols[x.Name], x.TypeHint);

                        return x;
                    }),
                Sections[0]
            );

            try
            {
                for (var i = Relocations.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var o = Relocations[i].Address;
                    }
                    catch (Exception e)
                    {
                        throw new RelocationException(
                            $"Error evaluating address for relocation {i} ({Relocations[i].ToShortString()})",
                            e,
                            relocations,
                            Relocations[i].Location
                        );
                    }
                }

                LinkerScript = string.Join(
                    Environment.NewLine, 
                    new[] 
                    {
                        string.Format("ADDRESS_START = 0x{0:X8};", EntryPoint),
                        "ENTRY_POINT = ADDRESS_START;"
                    }.Concat(
                        extra.list
                    )
                );
            }
            catch (RelocationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new RelocationException("Error while evaluating relocation addresses and generating linker script", e, relocations);
            }
        }



        private static (Dictionary<string, string> offsetSymbols, IEnumerable<string> list) GenerateExtraSymbols(Overlay ovl, IOverlaySymbols syms)
        {
            var sortedSyms = syms
                .OrderBy(x => x.Address)
                .ToArray();

            var orphans = sortedSyms
                .Where(s => !ovl.Sections.Any(x => s.Address >= x.StartAddress && s.Address < x.EndAddress))
                .ToArray();

            bool checker(Symbol s) => 
                s.Address >= ovl.EntryPoint && s.Address <= ovl.EndAddress;

            var offsetSymbols = new Dictionary<string, string>();

            var list = orphans
                .Where(s => !checker(s))
                .Select(s => string.Format("{0} = 0x{1:X8};", s.Name, s.Address));

            foreach (var x in orphans.Where(s => checker(s)))      
            {
                var nearest = sortedSyms.TakeWhile(y => y.Address < x.Address).Last();

                offsetSymbols.Add(x.Name, $"{nearest.Name} + {x.Address - nearest.Address}");
            }

            return (offsetSymbols, list);
        }
        
        

        public interface IOverlaySymbols : ISymbolRepository, IEnumerable<Symbol>
        {
            
        }

        public interface IOverlayRelocations : IReadOnlyList<Relocation>
        {
            Relocation Lookup(UInt32 pc);
        }

        private class OverlayRelocations : IOverlayRelocations
        {
            private readonly Dictionary<UInt32, Relocation> _lookup;
            private readonly IReadOnlyList<Relocation> _relocations;

            public OverlayRelocations(IReadOnlyList<Relocation> input)
            {
                _relocations = input;
                _lookup = input.ToDictionary(r => r.Section.StartAddress + r.Location, r => r);
            }


            public Relocation this[int index]
            {
                get
                {
                    return _relocations[index];
                }
            }

            public int Count
            {
                get
                {
                    return _relocations.Count;
                }
            }


            public IEnumerator<Relocation> GetEnumerator()
            {
                return _relocations.GetEnumerator();
            }

            public Relocation Lookup(uint pc)
            {
                if (!_lookup.ContainsKey(pc))
                    return null;

                return _lookup[pc];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _relocations.GetEnumerator();
            }
        }

        private List<string> _oddities = new List<string>();


        private class OverlaySymbols : IOverlaySymbols
        {
            private readonly SortedDictionary<UInt32, Symbol> _symbols;
            private readonly Dictionary<UInt32, UInt32> _pairLookup;

            public OverlaySymbols(Overlay parent, IEnumerable<Symbol> symbols, SectionInformation textSection)
            {
                symbols = symbols.ToList();

                _symbols = new SortedDictionary<uint, Symbol>(
                    symbols
                        .GroupBy(s => s.Address)
                        .Select(g => g
                            .OrderByDescending(s => s.TypeHint == TypeHint.Function)
                            .First())
                        .ToDictionary(s => s.Address, s => s)
                );

                _pairLookup = parent.Relocations
                    .Where(r => r.Type == RelocationType.R_MIPS_HI16 || r.Type == RelocationType.R_MIPS_LO16)
                    .Select(r => new { Pc = r.AbsoluteLocation, SymbolAddress = r.Address })
                    .ToDictionary(p => p.Pc, p => p.SymbolAddress);

                // Find strange symbols in .text section that are not 4-byte aligned
                var malignedTextSymbols = symbols
                    .Where(s => s.Address >= textSection.StartAddress && s.Address < textSection.EndAddress)
                    .Where(s => s.Address % 4 != 0)
                    .Select(s => new {
                        New = new Symbol((uint)(s.Address & ~3), string.Format("data_{0:X8} + {1}", s.Address & ~3, s.Address - (s.Address & ~3)), s.TypeHint),
                        Old = s
                    })
                    .ToArray();

                foreach(var x in malignedTextSymbols)
                {
                    _symbols.Remove(x.Old.Address);
                    _symbols.Add(x.New.Address, x.New);

                    var pcToRemove = _pairLookup
                        .Where(p => p.Value == x.Old.Address)
                        .Select(p => p.Key)
                        .ToArray();

                    foreach (var pc in pcToRemove)
                        _pairLookup[pc] = x.New.Address;
                }

                parent._oddities.AddRange(
                    malignedTextSymbols.Select(s => $"{s.Old.Name} in .text section but not 4-byte aligned")
                );
            }

            public Symbol Lookup(uint address) =>
                _symbols.ContainsKey(address)
                ? _symbols[address]
                : null;

            public Symbol LookupReferencedSymbolAt(uint pc) =>
                _pairLookup.ContainsKey(pc)
                ? Lookup(_pairLookup[pc])
                : null;

            public string LookupReferencedSymbolNameAt(uint pc) =>
                LookupReferencedSymbolAt(pc)?.Name;

            public string LookupName(uint address) =>
                Lookup(address)?.Name;

            public IEnumerator<Symbol> GetEnumerator() =>
                _symbols.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();
        }
        
        public IOverlaySymbols Symbols { get; }



        public IEnumerable<string> Disassemble(bool debugAddress = false)
        {
            var disassembler = new Disassembler(new DefaultDisassemblyFormatter(Symbols));

            Func<string, string, string> formatPair = (left, right) =>
                right == null
                ? $"\t{left}"
                : $"\t{left.PadRight(16)}{right}";

            Func<UInt32, Instruction, DisassemblerOutput, string> formatDisasm = (pc, insn, output) =>
                !debugAddress
                ? formatPair(output.Opcode, output.Operands)
                : formatPair(output.Opcode, (output.Operands ?? "").PadRight(32) + string.Format("# 0x{0:X8}  0x{2:X8}  {1}", pc, Disassembler.DefaultWithoutPc.Disassemble(0, insn), insn.Word));

            yield return "#include \"mips.h\"";
            yield return "#define s8 $fp";
            yield return "";

            var x = 0;

            yield return formatPair("#", null);

            foreach (var s in Sections)
            {
                yield return formatPair($"# {s.Name}", string.Format("0x{0:X8} - 0x{1:X8} ({2,6:0.00} kb)", s.StartAddress, s.EndAddress, s.Size / 1024.0));

                if( x++ == 2 )
                    yield return formatPair("# [relocs]", string.Format("0x{0:X8} - 0x{1:X8} ({2,6:0.00} kb)", Sections[2].EndAddress, Sections[2].EndAddress + HeaderOffset, HeaderOffset / 1024.0));
            }

            if(_oddities.Count > 0 )
            {
                yield return formatPair("#", null);
                yield return formatPair("# Oddities:", null);

                foreach (var o in _oddities)
                    yield return formatPair($"#   - {o}", null);
            }

            yield return formatPair("#", null);

            yield return "";

            yield return formatPair(".set", "noreorder");
            yield return formatPair(".set", "noat");
            yield return "";

            for (var j = 0; j < Sections.Count; j++)
            {
                var section = Sections[j];

                if (section.Size == 0)
                    continue;

                UInt32 pc = section.StartAddress;

                if(section.Id >= 1)
                {
                    yield return "";
                    yield return "";
                }

                if (section.Id > 1)
                    yield return formatPair(".section", $"{section.Name}");
                else
                    yield return formatPair(section.Id == 1 ? ".data" : ".text", null);
                yield return "";

                switch (section.Id)
                {

                    case 0:
                        foreach (var insn in section.Data.ToInstructions())
                        {
                            var symbol = Symbols.Lookup(pc);

                            if (symbol != null)
                            {
                                if (symbol.TypeHint.HasFlags(TypeHint.Function))
                                {
                                    yield return "";
                                    yield return formatPair(".global", symbol.Name);
                                    yield return formatPair(".type", $"{symbol.Name}, @function");
                                    yield return "";
                                    yield return symbol.Name + ":";
                                }
                                else
                                {
                                    var pattern = @" \+ \d+$";
                                    var name = symbol.Name;

                                    if (Regex.IsMatch(name, pattern))
                                        name = Regex.Replace(name, pattern, "");

                                    yield return name + ":";
                                }
                            }

                            yield return formatDisasm(pc, insn, disassembler.Disassemble(pc, insn));
                            pc += 4;
                        }
                        break;

                    case 1:
                    case 2:
                        for( var i = 0; i < section.Data.Count; i++ )
                        {
                            var symbol = Symbols.Lookup(pc + (UInt32)i);
                            

                            if( symbol != null )
                            {
                                var stuffs = new List<string>
                                {
                                    "",
                                    formatPair(".type", $"{symbol.Name}, @object"),
                                    symbol.Name + ":"
                                };
                                

                                var reloc = Relocations.Lookup(pc + (UInt32)i);
                                var doByte = false;

                                Action<int> size = (s) =>
                                    stuffs.Insert(2, formatPair(".size", $"{symbol.Name}, {s}"));

                                bool checkNoSymbolsForNextNbytes(int length) =>
                                    Enumerable.Range(1, length - 1)
                                        .Select(y => Symbols.Lookup(pc + (UInt32)(i + y)))
                                        .All(y => y == null);

                                if (reloc != null && checkNoSymbolsForNextNbytes(4))
                                {
                                    size(4);
                                    stuffs.Add(formatPair(".word", Symbols.Lookup(reloc.Address).Name));
                                    i += 3;
                                }
                                else if (symbol.TypeHint.HasFlags(TypeHint.HalfWord | TypeHint.HalfWordUnsigned) && checkNoSymbolsForNextNbytes(2))
                                {
                                    size(2);
                                    stuffs.Add(formatPair(".short", string.Format("0x{0:X4}", section.Data[i] << 8 | section.Data[i + 1])));
                                    i++;
                                }
                                else if (symbol.TypeHint.HasFlags(TypeHint.Word | TypeHint.WordUnsigned) && checkNoSymbolsForNextNbytes(4))
                                {
                                    size(4);
                                    stuffs.Add(formatPair(".word", string.Format("0x{0:X8}", Utilities.ReadU32(section.Data, i))));
                                    i += 3;
                                }
                                else if (symbol.TypeHint.HasFlags(TypeHint.DoubleWord) && checkNoSymbolsForNextNbytes(8))
                                {
                                    size(8);
                                    stuffs.Add(formatPair(".quad", string.Format("0x{0:X16}", Utilities.ReadU32(section.Data, i))));
                                    i += 7;
                                }
                                else if (symbol.TypeHint.HasFlags(TypeHint.Single) && checkNoSymbolsForNextNbytes(4))
                                {
                                    size(4);

                                    var flt = BitConverter.ToSingle(BitConverter.GetBytes(Utilities.ReadU32(section.Data, i)), 0);

                                    stuffs.AddRange(Float.GenerateAssemblyLine(flt).Select(y => formatPair(y.left, y.right)));

                                    i += 3;
                                }
                                else if (symbol.TypeHint.HasFlags(TypeHint.Double) && checkNoSymbolsForNextNbytes(8))
                                {
                                    size(8);

                                    var flt = BitConverter.ToDouble(BitConverter.GetBytes(Utilities.ReadU64(section.Data, i)), 0);

                                    stuffs.AddRange(Float.GenerateAssemblyLine(flt).Select(y => formatPair(y.left, y.right)));

                                    i += 7;
                                }
                                else
                                {
                                    doByte = true;
                                }

                                foreach (var q in stuffs)
                                    yield return q;

                                if (!doByte)
                                    continue;
                            }
                            else
                            {
                                var reloc = Relocations.Lookup(pc + (UInt32)i);

                                if (reloc != null)
                                {
                                    yield return formatPair(".word", Symbols.Lookup(reloc.Address).Name);
                                    i += 3;

                                    continue;
                                }
                            }

                            yield return formatPair(".byte", string.Format("0x{0:X2}", section.Data[i]));
                        }
                        break;

                    case 3:
                        var bssSyms = Symbols
                            .Where(s => s.Address >= section.StartAddress && s.Address < section.EndAddress)
                            .OrderBy(s => s.Address)
                            .ToArray();

                        var symIdx = 0;

                        if (bssSyms.Length == 0)
                        {
                            yield return formatPair(".space", $"{Sections[3].Size}");
                            break;
                        }

                        for(var ptr = 0; ptr < section.Size; )
                        {
                            long curAddr;
                            var sym = bssSyms[symIdx];

                            if ( (curAddr = section.StartAddress + ptr) < sym.Address )
                            {
                                var spaceSize = bssSyms[symIdx].Address - curAddr;

                                yield return formatPair(".space", $"{spaceSize}");

                                ptr += (int)spaceSize;
                            }
                            else
                            {
                                var size = (symIdx + 1 == bssSyms.Length
                                    ? section.EndAddress
                                    : bssSyms[symIdx + 1].Address) - sym.Address;

                                var right = $"{sym.Name},{size},1";

                                if (debugAddress)
                                    right = right.PadRight(24) + string.Format("# 0x{0:X8}", ptr + section.StartAddress);

                                yield return formatPair(".local", sym.Name);
                                yield return formatPair(".comm", right);

                                ptr += (int)size;
                                symIdx++;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Detect a valid overlay given the provided data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool Detect(IReadOnlyList<byte> data)
        {
            var headerOffset = Utilities.ReadU32(data, data.Count - 4);

            if (headerOffset == 0 || (headerOffset & 3) != 0 || headerOffset >= data.Count)
                return false;

            var sizes = Enumerable.Range(0, 4)
                .Select(i => Utilities.ReadU32(data, (int)(data.Count - headerOffset + i * 4)))
                .ToArray();

            if (sizes.Take(3).Sum(s => s) + headerOffset != data.Count)
                return false;

            return true;
        }

        public static UInt32 InferEntryPoint(IReadOnlyList<byte> overlay) =>
            InferEntryPointFromRelocs(
                new Overlay(0, overlay)
                    .Relocations
                    .Select(r => (r.Type, r.Address))
            );

        public static UInt32 InferEntryPointFromRelocs(IEnumerable<(RelocationType relocType, UInt32 symValue)> relocs) =>
            relocs
                .Where(x => x.relocType == RelocationType.R_MIPS_26 || x.relocType == RelocationType.R_MIPS_32)
                // JAL/J does not contain the upper 4 bits of the address, but it is always 8 for overlays
                .Select(x => x.symValue | 0x80000000)
                .OrderBy(x => x)
                .First();
    }
}
