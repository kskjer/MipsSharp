using MipsSharp.Binutils;
using Dapper;
using ELFSharp.ELF.Sections;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using static MipsSharp.EnumerableInstructionExtensions;

namespace MipsSharp.Mips
{
    public sealed class SignatureDatabase
    {
        private const string _sqlSchema = @"
            BEGIN TRANSACTION;
            CREATE TABLE fingerprints
            (
                fingerprint_id       INTEGER PRIMARY KEY,
                fingerprint_md5      TEXT UNIQUE,
                fingerprint_length   INTEGER
            );
            CREATE TABLE instructions
            (
                instruction_id    INTEGER PRIMARY KEY,
                instruction_word  UNSIGNED INTEGER,
                fingerprint_id    INTEGER
            );
            CREATE TABLE masks
            (
                mask_id           INTEGER PRIMARY KEY,
                mask_word         UNSIGNED INTEGER,
                fingerprint_id    INTEGER
            );
            CREATE TABLE imports (import_file_md5 TEXT UNIQUE, import_id INTEGER PRIMARY KEY, import_file TEXT, import_mtime TEXT);
            CREATE TABLE variants (fingerprint_id INTEGER, variant_id INTEGER PRIMARY KEY, variant_name TEXT, variant_source TEXT, import_id INTEGER);
            CREATE TABLE relocations (relocation_address UNSIGNED INTEGER, relocation_id INTEGER PRIMARY KEY, relocation_sym TEXT, relocation_value INTEGER, relocation_type INTEGER, variant_id INTEGER);
            CREATE INDEX idx_masks ON masks(fingerprint_id DESC);
            CREATE INDEX idx_instructions ON instructions(fingerprint_id DESC);
            CREATE INDEX idx_variants ON variants(fingerprint_id DESC);
            COMMIT;";


        public class ImportStats
        {
            public int CountExisting { get; set; }
            public int CountImported { get; set; }
            public int ArchivesExisting { get; set; }
        }

        private static void ImportArFile(DbConnection db, string arPath, ImportStats stats = null)
        {
            Console.Error.WriteLine($"{arPath}...");


            // Dummy value in case none provided
            stats = stats ?? new ImportStats();

            var ar = new Archive(arPath);
            var constructed = ar.Files
                .Select(a => new { elf = a.ToElf(), a })
                .Where(e => e.elf.Machine == ELFSharp.ELF.Machine.MIPS && e.elf.Endianess == ELFSharp.ELF.Endianess.BigEndian)
                .Select(e => new { e.elf, e.a, syms = ((ISymbolTable)e.elf.GetSection(".symtab")).Entries.ToArray() })
                .Select(e => new {
                    e.a,
                    e.syms,
                    e.elf,
                    text = e.elf.Sections.FirstOrDefault(s => s.Name == ".text")?.GetContents(),
                    fns = e.syms
                        .Select(s => s as SymbolEntry<uint>)
                        .Where(s => s != null && s.Binding == SymbolBinding.Global && s.Size != 0 && s.Type == ELFSharp.ELF.Sections.SymbolType.Function)
                        .Select(s => new { s.Name, s.Size, Location = s.Value })
                })
                .Select(e => new {
                    e.a,
                    e.syms,
                    e.fns,
                    e.text,
                    e.elf,
                    relocs = e.elf.Sections
                        .Where(s => s.Type == ELFSharp.ELF.Sections.SectionType.Relocation && s.Name.Contains(".text"))
                        .Select(s => new {
                            s,
                            data = s.GetContents().ToWordGroups(2)
                                .Select(g => new {
                                    Type = (RelocationType)(g[1] & 0xFF),
                                    Address = g[0],
                                    T = e.syms[(int)g[1] >> 8]
                                })
                                //.Where(g => {
                                //    if (string.IsNullOrWhiteSpace(g.T?.Name))
                                //        Console.WriteLine("Hit!");

                                //    return true;
                                //})
                                .Where(g => e.fns.Any(f => g.Address >= f.Location && g.Address < f.Location + f.Size))
                                .Select(g => {
                                    
                                    var sym = g.T as SymbolEntry<uint>;
                                    var name = sym.Name;
                                    
                                    if (string.IsNullOrWhiteSpace(name))
                                    {
                                        name = sym.PointedSection?.Name;

                                        //var preceding = e.syms
                                        //    .Select(y => y as SymbolEntry<uint>)
                                        //    .Where(y => y.Binding == SymbolBinding.Global)
                                        //    .Where(y => y.Type == ELFSharp.ELF.Sections.SymbolType.Function)
                                        //    .FirstOrDefault(y => y.Value < g.Address);

                                        //var offset = sym.Value - sym.PointedSection?.LoadAddress;

                                        //var insn = e.text
                                        //    .Skip((int)g.Address)
                                        //    .Take(4)
                                        //    .ToInstructions()
                                        //    .First();

                                        //switch (g.Type)
                                        //{
                                        //    case RelocationType.R_MIPS_26:
                                        //        offset = insn.Target;
                                        //        break;

                                        //    case RelocationType.R_MIPS_LO16:
                                        //        offset = insn.Immediate;
                                        //        break;
                                        //}

                                        //name = $"{e.a.Filename}({preceding?.Name}+{offset})";
                                    }

                                    return new {
                                        g.Type,
                                        g.Address,
                                        Name = name
                                    };
                                })
                                .ToList(),
                            insns = s.Name.Contains(".text")
                                ? e.elf.Sections.First(z => z.Name == s.Name.Replace(".rel", "")).GetContents().ToInstructions()
                                : null
                        })
                        .FirstOrDefault()
                        ?.data
                        ?.ToDictionary(d => d.Address, d => new { d.Name, d.Type })
                })
                .Where(e => e.relocs?.Values?.All(
                    r => r.Type == RelocationType.R_MIPS_26   ||
                         r.Type == RelocationType.R_MIPS_HI16 ||
                         r.Type == RelocationType.R_MIPS_LO16
                ) != false)
                .Select(e => new {
                    e.a,
                    e.elf,
                    fns = e.fns
                        .Select(f => new {
                            f.Name,
                            f.Size,
                            f.Location,
                            Masks = Enumerable.Range(0, (int)f.Size / 4)
                                .Select(i => {
                                    var ptr = (uint)(i * 4) + f.Location;

                                    if (!(e.relocs?.ContainsKey(ptr)).GetValueOrDefault())
                                        return 0xFFFFFFFFU;

                                    switch (e.relocs[ptr].Type)
                                    {
                                        case RelocationType.R_MIPS_26:
                                            return ~((1U << 26) - 1);

                                        case RelocationType.R_MIPS_HI16:
                                        case RelocationType.R_MIPS_LO16:
                                            return 0xFFFF0000U;

                                        default:
                                            throw new NotImplementedException();
                                    }
                                })
                                .ToArray()
                        })
                        .Select(f => new {
                            f.Name,
                            f.Size,
                            f.Location,
                            f.Masks,
                            Instructions = e.text.GetSegment((int)f.Location, (int)f.Size)
                                .ToInstructions()
                                .Zip(f.Masks, (i, m) => new Instruction(i & m))
                                .ToArray(),
                            Relocs = e.relocs
                                ?.Where(r => r.Key >= f.Location && r.Key < f.Location + f.Size)
                                ?.Select(r => new { Address = r.Key - f.Location, r.Value.Type, r.Value.Name })
                                ?.ToArray()
                        })
                        .Select(f => new {
                            f.Name, f.Size, f.Location, f.Masks, f.Instructions, f.Relocs,
                            Hash = CreateMd5Hash(
                                f.Instructions
                                    .RemoveTrailingNops()
                                    .ToBytes()
                                    .ToArray()
                            )
                        })
                        .ToArray()
                })
                .Where(e => e.fns.Length > 0)
                .ToList();

            var info = new FileInfo(arPath);

            int importId = 0;

            try
            {
                importId = db.Query<int>(@"
                    INSERT INTO imports ( import_file_md5, import_file, import_mtime ) VALUES (
                        @md5,
                        @name,
                        @mtime
                    );
                    SELECT last_insert_rowid()",
                    new
                    {
                        md5 = CreateMd5Hash(File.ReadAllBytes(arPath)),
                        name = info.Name,
                        mtime = info.LastWriteTimeUtc.ToString("o")
                    }
                ).Single();
            }
            catch
            {
                stats.ArchivesExisting++;
                return;
            }

            var import = db.Query<sqlImport>("SELECT * FROM imports WHERE import_id = @importId", new { importId })
                .Single();

            foreach(var f in constructed.SelectMany(c => c.fns.Select(f => new { c, fn = f })))
            {
                var needInsnsAndMasks = false;

                reload:
                var existing =
                    db.Query<sqlFingerprint>("SELECT * FROM fingerprints WHERE fingerprint_md5 = @md5", new { md5 = f.fn.Hash })
                    .SingleOrDefault();


                if( existing == null )
                {
                    db.Execute(@"
                        INSERT INTO fingerprints ( fingerprint_md5, fingerprint_length ) VALUES (
                            @md5, @length
                        );",
                        new { md5 = f.fn.Hash, length = f.fn.Size }
                    );

                    needInsnsAndMasks = true;

                    goto reload;
                }

                if (needInsnsAndMasks)
                {
                    // Also insert instructions and masks only once
                    var list = f.fn.Masks.Select(m => new { word = m, t = "mask" })
                        .Concat(f.fn.Instructions.Select(i => new { word = i.Word, t = "instruction" }));

                    foreach (var l in list)
                        db.Execute(
                            $"INSERT INTO {l.t}s ( fingerprint_id, {l.t}_word ) VALUES ( @fpid, @word )",
                            new { fpid = existing.fingerprint_id, word = (UInt64)l.word }
                        );
                }

                // Check that variant does not already exist
                var countExisting = db.Query<int>(
                    "SELECT COUNT(*) FROM variants WHERE fingerprint_id = @fpid AND variant_name = @name",
                    new { fpid = existing.fingerprint_id, f.fn.Name }
                ).Single();

                if (countExisting > 0)
                {
                    stats.CountExisting++;
                    continue;
                }
                else
                {
                    stats.CountImported++;
                }

                var variant = new sqlVariant
                {
                    fingerprint_id = existing.fingerprint_id,
                    import_id = import.import_id,
                    variant_name = f.fn.Name,
                    variant_source = f.c.a.Filename
                };

                variant.variant_id = db.Query<int>(@"
                    INSERT INTO variants ( fingerprint_id, import_id, variant_name, variant_source ) VALUES (
                        @fingerprint_id,
                        @import_id,
                        @variant_name,
                        @variant_source
                    );
                    SELECT last_insert_rowid()",
                    new { variant.fingerprint_id, variant.import_id, variant.variant_name, variant.variant_source }
                ).Single();

                if (f.fn.Relocs == null)
                    continue;

                foreach(var r in f.fn.Relocs )
                {
                    db.Execute(@"
                        INSERT INTO relocations ( 
                            relocation_address, 
                            relocation_sym, 
                            relocation_value, 
                            relocation_type, 
                            variant_id
                        ) VALUES (
                            @Address,
                            @Name,
                            @value,
                            @Type,
                            @variant_id
                        )",
                        new { r.Address, r.Name, value = 0, r.Type, variant.variant_id }
                    );
                }
            }

            foreach (var c in constructed)
                c.elf.Dispose();
        }

        public static ImportStats ImportArchiveToDatabase(string dbPath, bool cleanDatabase, params string[] arFiles)
        {
            var existing = File.Exists(dbPath);
            var stats = new ImportStats();

            using (var db = new SqliteConnection($"Data Source={dbPath};Version=3;"))
            {
                if (!existing)
                    db.Execute(_sqlSchema);

                db.Execute("BEGIN");

                if (cleanDatabase)
                {
                    db.Execute(@"
                        DELETE FROM fingerprints;
                        DELETE FROM instructions;
                        DELETE FROM masks;
                        DELETE FROM imports;
                        DELETE FROM variants;
                        DELETE FROM relocations;"
                    );
                }

                foreach (var ar in arFiles)
                    ImportArFile(db, ar, stats);

                db.Execute("COMMIT");
            }

            return stats;
        }

        private static string CreateMd5Hash(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                return string.Join(
                    "",
                    md5.ComputeHash(data)
                        .Select(b => b.ToString("x2"))
                );
            }
        }

        private static string CalculateFunctionHash(EnumerableInstructionExtensions.DiscoveredFunction df)
        {
            var bytes = df.Instructions
                .RemoveTrailingNops()
                .ZeroRelocatedValues()
                .ToBytes()
                .ToArray();

            return CreateMd5Hash(bytes);
        }

        public class VariantMatch
        {
            public string Name { get; }
            public IReadOnlyDictionary<UInt32, string> Symbols { get; }

            public VariantMatch(string n, IReadOnlyDictionary<UInt32, string> dict)
            {
                Name = n;
                Symbols = dict;
            }
        }

        public class SignatureMatch
        {
            public UInt32 Start => _function.StartAddress;
            public int Size => (int)_function.Size;
            private readonly DiscoveredFunction _function;
            public IReadOnlyList<VariantMatch> Matches { get; }

            public SignatureMatch(DiscoveredFunction fn, IReadOnlyList<VariantMatch> matches)
            {
                _function = fn;
                Matches = matches;
            }

            public override string ToString() =>
                string.Format("{0:X8}: {1} ({2} symbols)", _function.StartAddress, string.Join(" | ", Matches.Select(m => m.Name)), Matches.Sum(m => m.Symbols.Count));
        }

        public IReadOnlyList<SignatureMatch> IdentifySlow(IEnumerable<InstructionWithPc> instructions)
        {
            var haystack = Signatures
                .Select(s => new {
                    masks = s.Masks.ToArray(),
                    insns = s.Variants.First().Instructions.ToArray(),
                    s
                })
                .ToArray();

            return instructions
                .DiscoverFunctions()
                .Select(f => new {
                    f,
                    Trimmed = f.Instructions.RemoveTrailingNops().ToArray()
                })
                .Select(f => new {
                    f,
                    match = haystack
                        .Where(h => h.insns.Length == f.Trimmed.Length)
                        .FirstOrDefault(s =>
                        {
                            //0x80004780,osCreatePiManager
                            //if (f.f.StartAddress == 0x80004780U && s.s.Variants.Any(v => v.Name == "osCreatePiManager"))
                            //    Console.WriteLine("Jes");

                            return f.Trimmed
                                .Select((insn, idx) => insn.Instruction & s.masks[idx])
                                .SequenceEqual(s.insns.Select(i => i.Word));
                        })
                })
                .Where(f => f.match != null)
                .Select(f => new SignatureMatch(
                    f.f.f, 
                    f.match.s.Variants
                        .Select(v => new VariantMatch(
                            v.Name,
                            v.GetSymbolsReferencedByRelocations(f.f.Trimmed)
                        ))
                        .ToArray()
                ))
                .ToArray();
        }

        public IReadOnlyList<SignatureMatch> Identify(IEnumerable<InstructionWithPc> instructions)
        {
            return instructions
                .DiscoverFunctions()
                .Where(f => f.Instructions.Count > 1)
                // Drop functions which are just jr $ra; nop
                .Where(f => !(f.Instructions[0].Instruction == 0x03e00008 && f.Instructions[1].Instruction == 0))
                .Select(f => new { f, hash = CalculateFunctionHash(f) })
                .Where(f => _signatureLookup.ContainsKey(f.hash))
                .Select(f => new SignatureMatch(
                    f.f,
                    _signatureLookup[f.hash]
                        .Variants
                        .GroupBy(v => v.Name)
                        .Select(g => g.First())
                        .Select(v => new VariantMatch( 
                            v.Name,
                            v.GetSymbolsReferencedByRelocations(f.f.Instructions)
                        ))
                        .ToList()
                ))
                .ToList();
        }

        public IReadOnlyList<Import> Imports { get; }
        public IReadOnlyList<Signature> Signatures { get; }
        private readonly IReadOnlyDictionary<string, Signature> _signatureLookup;

        public SignatureDatabase(string dbPath)
        {
            using (var db = new SqliteConnection($"Data Source={dbPath};Version=3;"))
            {
                var masks        = db.Query<sqlMask>       ("SELECT * FROM masks");
                var instructions = db.Query<sqlInstruction>("SELECT * FROM instructions");
                var imports      = db.Query<sqlImport>     ("SELECT * FROM imports");
                var fingerprints = db.Query<sqlFingerprint>("SELECT * FROM fingerprints");
                var variants     = db.Query<sqlVariant>    ("SELECT * FROM variants");
                var relocations  = db.Query<sqlRelocation> ("SELECT * FROM relocations");

                var insnDict = instructions
                    .GroupBy(i => i.fingerprint_id)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(v => new Instruction((UInt32)v.instruction_word))
                            .ToList()
                    );

                var reloDict = relocations
                    .GroupBy(r => r.variant_id)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(r => new Relocation(
                                r.relocation_address,
                                r.relocation_sym,
                                r.relocation_type,
                                r.relocation_value
                            ))
                            .ToList()
                    );

                var masksDict = masks
                    .GroupBy(m => m.fingerprint_id)
                    .ToDictionary(g => g.Key, g => g.Select(m => (UInt32)m.mask_word).ToList());

                var variantsDict = variants
                    .GroupBy(v => v.import_id)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .Select(v => new Variant(
                                v.fingerprint_id,
                                v.variant_name,
                                v.variant_source,
                                reloDict.ContainsKey(v.variant_id) ? reloDict[v.variant_id] : new List<Relocation>(),
                                insnDict[v.fingerprint_id]
                            ))
                            .ToList()
                    );

                var variantsByFpDict = variantsDict.Values.SelectMany(v => v)
                    .GroupBy(v => (v as IVariant).FingerprintId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );

                Imports = imports
                    .Where(i => variantsDict.ContainsKey(i.import_id))
                    .Select(m => new Import(
                        m.import_id,
                        m.import_file,
                        m.import_file_md5,
                        m.import_mtime,
                        variantsDict[m.import_id]
                    ))
                    .ToList();

                Signatures = fingerprints
                    .Select(f => new Signature(
                        f.fingerprint_id,
                        f.fingerprint_length,
                        f.fingerprint_md5,
                        masksDict[f.fingerprint_id],
                        variantsByFpDict[f.fingerprint_id]
                    ))
                    .ToList();

                _signatureLookup = Signatures
                    .ToDictionary(s => s.Md5, s => s);
            }
        }

        public class Import
        {
            private int Id { get; }
            public string Filename { get; }
            public string FileMd5 { get; }
            public string LastModified { get; }

            public IReadOnlyList<Variant> Variants { get; }

            public Import(int id, string fn, string md5, string mtime, IReadOnlyList<Variant> variants)
            {
                Id = id;
                Filename = fn;
                FileMd5 = md5;
                LastModified = mtime;
                Variants = variants;
            }
        }

        public class Signature
        {
            public int Id { get; }
            public int Length { get; }
            public string Md5 { get; }

            public IReadOnlyList<UInt32> Masks { get; }
            public IReadOnlyList<Variant> Variants { get; }

            public Signature(int id, int len, string md5, IReadOnlyList<UInt32> mask, IReadOnlyList<Variant> v)
            {
                Id = id;
                Length = len;
                Md5 = md5;
                Masks = mask;
                Variants = v;
            }

            public override string ToString() =>
                string.Join(" | ", Variants.Select(v => v.Name).Distinct()) + $" ({Length} bytes)";

        }

        private interface IVariant
        {
            int FingerprintId { get; set; }
        }

        public class Variant : IVariant
        {
            public string Name { get; }
            public string Source { get; }

            public IReadOnlyList<Relocation> Relocations { get; }
            public IReadOnlyList<Instruction> Instructions { get; }

            public override string ToString() =>
                string.Format("{0}({1}, {2} insns)", Name, Source, Instructions.Count);

            public IReadOnlyDictionary<UInt32, string> GetSymbolsReferencedByRelocations<T>(IReadOnlyList<T> instructions)
                where T : struct, IHasInstruction
            {
                var list = new List<Tuple<UInt32, string>>();
                var hi16Index = new UInt32[32];

                foreach(var relo in Relocations)
                {
                    var insn = instructions[(int)(relo.Address / 4)].Instruction;

                    switch ((RelocationType)relo.Type)
                    {
                        case RelocationType.R_MIPS_26:
                            list.Add(
                                Tuple.Create(
                                    insn.FullTarget(0x80000000),
                                    relo.Symbol
                                )
                            );
                            break;

                        case RelocationType.R_MIPS_HI16:
                            hi16Index[insn.GprRt] = insn.Immediate << 16;
                            break;

                        case RelocationType.R_MIPS_LO16:
                            list.Add(
                                Tuple.Create(
                                    (UInt32)(hi16Index[insn.GprRs] + insn.ImmediateSigned),
                                    relo.Symbol
                                )
                            );
                            break;
                    }
                }

                return list
                    .GroupBy(l => l.Item1)
                    .Select(g => g.First())
                    .ToDictionary(g => g.Item1, g => g.Item2);
            }

            int IVariant.FingerprintId { get; set; }

            public Variant(int fpId, string name, string source, IReadOnlyList<Relocation> relo, IReadOnlyList<Instruction> insns)
            {
                Name = name;
                Source = source;
                Relocations = relo;
                Instructions = insns;

                ((IVariant)this).FingerprintId = fpId;
            } 
        }

        public class Relocation
        {
            public UInt32 Address { get; }
            public string Symbol { get; }
            public int Type { get; }
            public int Value { get; }

            public Relocation(UInt32 address, string sym, int type, int value)
            {
                Address = address;
                Symbol = sym;
                Type = type;
                Value = value;
            }
        }


        private class sqlMask
        {
            public int mask_id { get; set; }
            public Int64 mask_word { get; set; }
            public int fingerprint_id { get; set; }
        }

        private class sqlInstruction
        {
            public int instruction_id { get; set; }
            public Int64 instruction_word { get; set; }
            public int fingerprint_id { get; set; }
        }

        private class sqlImport
        {
            public string import_file_md5 { get; set; }
            public int import_id { get; set; }
            public string import_file { get; set; }
            public string import_mtime { get; set; }
        }

        private class sqlFingerprint
        {
            public int fingerprint_id { get; set; }
            public string fingerprint_md5 { get; set; }
            public int fingerprint_length { get; set; }
        }

        private class sqlVariant
        {
            public int fingerprint_id { get; set; }
            public int variant_id { get; set; }
            public string variant_name { get; set; }
            public string variant_source { get; set; }
            public int import_id { get; set; }
        }

        private class sqlRelocation
        {
            public UInt32 relocation_address { get; set; }
            public int relocation_id { get; set; }
            public string relocation_sym { get; set; }
            public int relocation_value { get; set; }
            public int relocation_type { get; set; }
            public int variant_id { get; set; }
        }
    }

    public static class SignatureMatchExtensions
    {
        public static IReadOnlyDictionary<UInt32, IReadOnlyList<string>> ToShortForm(
            this IReadOnlyList<SignatureDatabase.SignatureMatch> results,
            bool showSections = false)
        {

            Predicate<string> checker = s => true;

            if (!showSections)
                checker = s => !s.StartsWith(".");

            return results
                .SelectMany(r => r.Matches.Select(m => new { Address = r.Start, Name = m.Name }))
                .Concat(results.SelectMany(r => r.Matches.SelectMany(m => m.Symbols))
                    .Select(r => new { Address = r.Key, Name = r.Value }))
                .GroupBy(r => r.Address)
                .Where(g => g.Any(x => checker(x.Name)))
                .ToDictionary(
                    g => g.Key, 
                    g => g
                        .Select(x => x.Name)
                        .Distinct()
                        .Where(x => checker(x))
                        .OrderBy(x => x)
                        .ToList() as IReadOnlyList<string>
                );
                //.Select(g => string.Format("0x{0:x8},{1}", g.Key, string.Join("|", g.Select(x => x.Name).Where(s => checker(s)).Distinct().OrderBy(x => x))))
        }
    }
}
