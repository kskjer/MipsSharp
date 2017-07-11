using MipsSharp.Binutils;
using MipsSharp.Mips;
using MipsSharp.Nintendo64;
using MipsSharp.Zelda64;
using MipsSharp.Text;
using Mono.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using static MipsSharp.Mips.SignatureDatabase;
using System.Reflection;

namespace MipsSharp
{
    class Program
    {
        enum Mode
        {
            NotSet = 0,
            Signatures,
            ImportSignatures,
            Zelda64,
            ShowHelp,
            JekyllGen,
            EucJpStrings
        }

        enum ZeldaMode
        {
            NotSet = 0,
            Extract,
            DisassembleOverlay,
            DisassembleAllOverlays,
            ElfToOverlay
        }

        static Mode _mode;
        static ZeldaMode _zeldaMode;

        private class SignaturesOptions
        {
            public static bool IndentJson = false;
            public static string DatabasePath = "signatures.db";
            public static bool ShortMode = false;
            internal static bool CleanDatabase;
            internal static bool ShowAll;

            public static bool SlowMode { get; internal set; }
        }

        private class DisassemblyOptions
        {
            public static bool DebugPc = false;
        }

        private class EucJpOptions
        {
            public static bool Json = false;
            public static bool OnlyForeign = false;
            public static int? MinStringLength;
        }

        static readonly OptionSet _operatingModes = new OptionSet
        {
            { "eucjp-strings"    , "Find and dump EUC-JP encoded strings in input",  v => _mode = Mode.EucJpStrings     },
            { "import-signatures", "Import function signatures from .a file",        v => _mode = Mode.ImportSignatures },
            { "jekyll-gen"       , "Generate Jekyll collections",                    v => _mode = Mode.JekyllGen        },
            { "signatures"       , "Identify function signatures",                   v => _mode = Mode.Signatures       },
            { "zelda64"          , "Operations pertaining to Zelda 64 ROMs",         v => _mode = Mode.Zelda64          },
            { "help"             , "Show this message and exit",                     v => _mode = Mode.ShowHelp         },
        };

        static readonly OptionSet _signatureOptions = new OptionSet
        {
            { "i|indent", "Format/beautify output JSON", v => SignaturesOptions.IndentJson = true },
            { "d|database=", "Specify signature database location", v => SignaturesOptions.DatabasePath = v },
            { "s|short", "Short mode. Only 1 address and symbol name per line. If multiple matches found, names are separated with a pipe.", v => SignaturesOptions.ShortMode = true },
            { "S|slow", "Use the brute force method of identifying functions", v => SignaturesOptions.SlowMode = true },
            { "v|all", "Show all found extra symbols. Some are useless (e.g., \".text\", \".bss\". These are hidden by default.", v => SignaturesOptions.ShowAll = true }
        };

        static readonly OptionSet _importSignatureOptions = new OptionSet
        {
            { "d|database=", "Specify signature database location", v => SignaturesOptions.DatabasePath = v },
            { "c|clean",     "Clean database before import", v => SignaturesOptions.CleanDatabase = true }
        };

        static readonly OptionSet _zelda64Modes = new OptionSet
        {
            { "O|elf-to-ovl", "Convert ELF file to overlay", v => _zeldaMode = ZeldaMode.ElfToOverlay },
            { "e|extract", "Extract the contents of a Zelda 64 ROM", v => _zeldaMode = ZeldaMode.Extract },
            { "D|disassemble-overlay", "Disassemble an overlay file", v => _zeldaMode = ZeldaMode.DisassembleOverlay },
            { "A|disassemble-all", "Disassemble all overlay files in ROM", v => _zeldaMode = ZeldaMode.DisassembleAllOverlays }
        };

        static readonly OptionSet _disasmOptions = new OptionSet
        {
            { "P|pc", "Print PC and raw instruction next to disasembly", v => DisassemblyOptions.DebugPc = true }
        };

        static readonly OptionSet _eucJpOptions = new OptionSet
        {
            { "json", "Output results as JSON", v => EucJpOptions.Json = true },
            { "only-foreign", "Only show strings that have EUC-JP chars", v => EucJpOptions.OnlyForeign = true },
            { "min-length=", "Minimum length of strings to display", v => EucJpOptions.MinStringLength = int.Parse(v) }
        };

        static void MainSignatures(string[] args)
        {
            var signatures = new SignatureDatabase("signatures.db");
            var rom = new Rom(args[0]);

            Console.WriteLine(
                JsonConvert.SerializeObject(
                    signatures.Identify(rom.GetExecutable())
                )
            );
        }

        static void MainExtract(string[] args)
        {
            var rom = new Z64Rom(new Rom(args[0]));

            foreach (var file in rom.Files.Select((f, i) => new { f, i }))
                File.WriteAllBytes(string.Format("{0:0000} {1}", file.i, file.f.Name), file.f.Contents.ToArray());
        }

        static void MainDumbDis(string[] args)
        {
            var file = new Overlay(Convert.ToUInt32(args[1], 16), File.ReadAllBytes(args[0]));
            var disasm = Disassembler.Default;

            var lines = file.Sections[0].Data
                .ToInstructions()
                .Select((insn, i) => new { i, insn });

            foreach(var l in lines)
            {
                var pc = l.i * 4 + file.EntryPoint;

                Console.WriteLine("{0:X8}: {1}", pc, disasm.Disassemble((uint)pc, l.insn));

                var relo = file.Relocations
                    .SingleOrDefault(r => r.Location == l.i * 4);

                if (relo != null)
                    Console.WriteLine("\t{0}", relo);
            }
            
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var extra = _operatingModes.Parse(args);

            switch(_mode)
            {
                case Mode.ShowHelp:
                    Console.WriteLine("Usage: {0} [MODE] [OPTIONS]+ file", Path.GetFileNameWithoutExtension(typeof(Program).GetTypeInfo().Assembly.Location));
                    Console.WriteLine();

                    Console.WriteLine("The application supports the following modes:");
                    _operatingModes.WriteOptionDescriptions(Console.Out);

                    Console.WriteLine();
                    Console.WriteLine("Options for identifying function signatures:");
                    _signatureOptions.WriteOptionDescriptions(Console.Out);

                    Console.WriteLine();
                    Console.WriteLine("Options for importing function signatures:");
                    _importSignatureOptions.WriteOptionDescriptions(Console.Out);

                    Console.WriteLine();
                    Console.WriteLine("Options for Zelda 64 operations:");
                    _zelda64Modes.WriteOptionDescriptions(Console.Out);

                    Console.WriteLine();
                    Console.WriteLine("Options for disassembly operations:");
                    _disasmOptions.WriteOptionDescriptions(Console.Out);

                    Console.WriteLine();
                    Console.WriteLine("Options for EUC-JP string identification:");
                    _eucJpOptions.WriteOptionDescriptions(Console.Out);
                    break;

                case Mode.Signatures:
                    {
                        var files = _signatureOptions.Parse(extra);
                        var sigs = new SignatureDatabase(SignaturesOptions.DatabasePath);

                        var action =
                            SignaturesOptions.SlowMode
                            ? new Func<IEnumerable<InstructionWithPc>, IReadOnlyList<SignatureMatch>>(sigs.IdentifySlow)
                            : sigs.Identify;

                        var results = files
                            .Select(f => action(new Rom(f).GetExecutable()))
                            .First();

                        if (!SignaturesOptions.ShortMode)
                            Console.WriteLine(JsonConvert.SerializeObject(results, SignaturesOptions.IndentJson ? Formatting.Indented : Formatting.None));
                        else
                        {
                            Console.WriteLine(
                                string.Join(
                                    Environment.NewLine,
                                    results
                                        .ToShortForm(SignaturesOptions.ShowAll)
                                        .Select(g => string.Format("0x{0:x8},{1}", g.Key, string.Join("|", g.Value)))
                                )
                            );
                        }
                    }
                    break;

                case Mode.ImportSignatures:
                    var arFiles = _importSignatureOptions.Parse(extra);
                    var stats = SignatureDatabase.ImportArchiveToDatabase(
                        SignaturesOptions.DatabasePath, SignaturesOptions.CleanDatabase, arFiles.ToArray()
                    );

                    Console.Error.WriteLine("{0} imported, {1} variants already present, {2} archives already present", stats.CountImported, stats.CountExisting, stats.ArchivesExisting);

                    break;

                case Mode.JekyllGen:
                    {
                        var sigs = new SignatureDatabase(SignaturesOptions.DatabasePath);

                        var which = new DirectoryInfo(@"Z:\[N64]\Extracted")
                            .EnumerateFiles()
                            .OrderBy(f => f.Name)
                            .Where(f => f.Name.ToLower().EndsWith(".z64") && f.Name.ToLower().Contains("zelda"));

                        var source = new DirectoryInfo(@"Z:\[N64]\Extracted")
                            .EnumerateFiles()
                            .OrderBy(f => f.Name)
                            .Where(f => f.Name.ToLower().EndsWith(".z64"))
                            .Select(f => { Console.Error.WriteLine("{0}...", f.Name); return f; })
                            .Select(f => new Rom(f.FullName))
                            .Select(r => new {
                                Name = Path.GetFileNameWithoutExtension(r.FilePath),
                                Header = new {
                                    ImageName = r.Header.Name.Trim(),
                                    EntryPoint = string.Format("0x{0:X8}", r.Header.EntryPoint),
                                    Crc = string.Format("{0:X8} {1:X2}", r.Header.Crc[0], r.Header.Crc[1])
                                },
                                Signatures = sigs.IdentifySlow(r.GetExecutable()).ToShortForm()
                                        .Select(g => new { Address = string.Format("0x{0:x8}", g.Key), Sym = g.Value.ToArray() })
                                        .ToArray()
                            });

                        var opts = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 1.0))
                        };

                        var output = new SortedList<string, object>();

                        Parallel.ForEach(
                            which.Select(w => w.FullName),
                            opts,
                            (path) =>
                            {
                                Console.Error.WriteLine("{0}...", Path.GetFileName(path));

                                try
                                {
                                    var r = new Rom(path);

                                    var result = new
                                    {
                                        Name = Path.GetFileNameWithoutExtension(r.FilePath),
                                        Header = new
                                        {
                                            ImageName = r.Header.Name.Trim(),
                                            EntryPoint = string.Format("0x{0:X8}", r.Header.EntryPoint),
                                            Crc = string.Format("{0:X8} {1:X2}", r.Header.Crc[0], r.Header.Crc[1])
                                        },
                                        Signatures = sigs.IdentifySlow(r.GetExecutable()).ToShortForm()
                                            .OrderBy(s => s.Key)
                                            .Select(g => new { Address = string.Format("0x{0:x8}", g.Key), Sym = g.Value.ToArray() })
                                            .ToArray()
                                    };

                                    if (path.ToLower().Contains("zelda"))
                                    {
                                        var zrom = new Z64Rom(r);

                                        var result2 = new
                                        {
                                            result.Name,
                                            result.Header,
                                            result.Signatures,
                                            Zelda = new
                                            {
                                                Files = zrom.Files
                                                    .Select(f => new {
                                                        f.Name,
                                                        f.PhysicalEnd,
                                                        f.PhysicalStart,
                                                        f.PhysicalSize,
                                                        f.VirtualEnd,
                                                        f.VirtualStart,
                                                        f.Size,
                                                        f.Type
                                                    }),
                                                Overlays = zrom.Overlays
                                                    .Select(o => new {
                                                        o.VirtualStart,
                                                        o.VirtualEnd,
                                                        o.VmaStart,
                                                        o.VmaEnd
                                                    })
                                            }
                                        };

                                        lock (output)
                                            output.Add(path, result2);
                                    }
                                    else
                                    { 
                                        lock (output)
                                            output.Add(path, result);
                                    }
                                }
                                catch(Exception e )
                                {
                                    Console.Error.WriteLine(
                                        new Exception($"Error while processing \"{path}\"", e)
                                    );
                                }
                            }
                        );

                        Console.WriteLine(JsonConvert.SerializeObject(output.Values));
                    }
                    break;

                case Mode.Zelda64:
                    {
                        var files = _disasmOptions.Parse(_zelda64Modes.Parse(extra));

                        switch(_zeldaMode)
                        {
                            case ZeldaMode.Extract:
                                MainExtract(files.ToArray());
                                break;

                            case ZeldaMode.DisassembleOverlay:
                                MainDis(files.ToArray());
                                break;

                            case ZeldaMode.DisassembleAllOverlays:
                                var rom = new Rom(files[0]);
                                var zrom = new Z64Rom(rom);

                                foreach (var o in zrom.Overlays)
                                {
                                    var p = o.File.Name;
                                    var ovl = new Overlay(o.VmaStart, o.File.Contents);

                                    Directory.CreateDirectory(p);

                                    File.WriteAllLines(
                                        Path.Combine(p, $"{o.File.Name}.S"),
                                        ovl.Disassemble(DisassemblyOptions.DebugPc)
                                    );
                                    File.WriteAllText(
                                        Path.Combine(p, "conf.ld"),
                                        ovl.LinkerScript
                                    );
                                }
                                break;

                            case ZeldaMode.ElfToOverlay:
                                OverlayCreator.CreateFromElf(files[0]);
                                break;
                        }
                    }
                    break;

                case Mode.EucJpStrings:
                    {
                        var files = _eucJpOptions.Parse(extra);

                        var data = files
                            .Select(x => new { x, data = File.ReadAllBytes(x) })
                            .Select(x => new
                            {
                                x.x,
                                strings = EucJp.ExtractEucJpStrings(x.data, EucJp.AsciiColorCode, EucJp.UnknownExtraChars)
                                    .Where(y => !EucJpOptions.MinStringLength.HasValue || y.String.Length >= EucJpOptions.MinStringLength.Value)
                                    .Where(y => !EucJpOptions.OnlyForeign || y.ContainsEucJpCharacters)
                                    .Select(y => y.String)
                            });

                        if (!EucJpOptions.Json)
                        {
                            foreach (var x in data.Select(y => y.strings))
                                Console.WriteLine(x);
                        }
                        else
                        {
                            Console.WriteLine(
                                JsonConvert.SerializeObject(
                                    data.Select(x => new { Filename = x.x, Strings = x.strings }),
                                    Formatting.Indented
                                )
                            );
                        }
                    }
                    break;

                default:
                    goto case Mode.ShowHelp;
            }
        }


        static void MainDis(string[] args)
        {
            var file = File.ReadAllBytes(args[0]);
            var test = new Overlay(Convert.ToUInt32(args[1], 16), file); // 0x80832210

            var disasm = new Disassembler(new DefaultDisassemblyFormatter(test.Symbols));

            Console.WriteLine(string.Join(Environment.NewLine, test.Disassemble(DisassemblyOptions.DebugPc)));
        }
    }
}
