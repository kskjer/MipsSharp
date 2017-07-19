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
using System.Globalization;

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
            EucJpStrings,
            AsmPatch
        }

        enum ZeldaMode
        {
            NotSet = 0,
            Extract,
            DisassembleOverlay,
            DisassembleAllOverlays,
            GenerateOverlayRelocs,
            DumpRelocations
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

        private class AsmPatchOptions
        {
            public static string PatchSource;
            public static string OutputRom;
            public static string InputRom;
        }

        static readonly OptionSet _operatingModes = new OptionSet
        {
            { "asm-patch"        , "Assemble patch and apply to ROM",                v => _mode = Mode.AsmPatch          },
            { "eucjp-strings"    , "Find and dump EUC-JP encoded strings in input",  v => _mode = Mode.EucJpStrings     },
            { "import-signatures", "Import function signatures from .a file",        v => _mode = Mode.ImportSignatures },
            { "jekyll-gen"       , "Generate Jekyll collections",                    v => _mode = Mode.JekyllGen        },
            { "signatures"       , "Identify function signatures",                   v => _mode = Mode.Signatures       },
            { "zelda64"          , "Operations pertaining to Zelda 64 ROMs",         v => _mode = Mode.Zelda64          },
            { "help"             , "Show this message and exit",                     v => _mode = Mode.ShowHelp         },
        };

        static readonly Overlay.Options _overlayOptions = new Overlay.Options();

        static int Verbosity;

        static readonly OptionSet _globalOptions = new OptionSet
        {
            { "v", "Increase verbosity", v => Verbosity++ },
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
            { "O|elf-to-ovl", "Generate overlay relocations .c file", v => _zeldaMode = ZeldaMode.GenerateOverlayRelocs },
            { "e|extract", "Extract the contents of a Zelda 64 ROM", v => _zeldaMode = ZeldaMode.Extract },
            { "D|disassemble-overlay", "Disassemble an overlay file", v => _zeldaMode = ZeldaMode.DisassembleOverlay },
            { "A|disassemble-all", "Disassemble all overlay files in ROM", v => _zeldaMode = ZeldaMode.DisassembleAllOverlays },
            { "R|dump-relocs", "Dump relocation table for overlay", v => _zeldaMode = ZeldaMode.DumpRelocations }
        };

        static readonly OptionSet _disasmOptions = new OptionSet
        {
            { "P|pc", "Print PC and raw instruction next to disasembly", v => DisassemblyOptions.DebugPc = true },
            { "n|number", "Use a symbol's index instead of its address for name", v => _overlayOptions.NumberSymbols = true }
        };

        static readonly OptionSet _eucJpOptions = new OptionSet
        {
            { "json", "Output results as JSON", v => EucJpOptions.Json = true },
            { "only-foreign", "Only show strings that have EUC-JP chars", v => EucJpOptions.OnlyForeign = true },
            { "min-length=", "Minimum length of strings to display", v => EucJpOptions.MinStringLength = int.Parse(v) }
        };

        static readonly OptionSet _asmPatchOptions = new OptionSet
        {
            { "i|input=", "Input ROM", v => AsmPatchOptions.InputRom = v },
            { "o|output=", "Output ROM", v => AsmPatchOptions.OutputRom = v },
            { "s|source=", "Source code of patch (should be MIPS assembly)", v => AsmPatchOptions.PatchSource = v }
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
        

        static int Main(string[] args)
        {
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

                MainReal(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);

                return -1;
            }

            return 0;
        }

        static void MainReal(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var extra = _globalOptions.Parse(_operatingModes.Parse(args));

            switch(_mode)
            {
                case Mode.ShowHelp:
                    Console.WriteLine("Usage: {0} [MODE] [OPTIONS]+ file", Path.GetFileNameWithoutExtension(typeof(Program).GetTypeInfo().Assembly.Location));
                    Console.WriteLine();

                    Console.WriteLine("The application supports the following modes:");
                    _operatingModes.WriteOptionDescriptions(Console.Out);
                    
                    Console.WriteLine();
                    Console.WriteLine("Global options:");
                    _globalOptions.WriteOptionDescriptions(Console.Out);

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

                    Console.WriteLine();
                    Console.WriteLine("Options for assembly patches:");
                    _asmPatchOptions.WriteOptionDescriptions(Console.Out);
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
                                    var ovl = new Overlay(o.VmaStart, o.File.Contents, _overlayOptions);

                                    Directory.CreateDirectory(p);

                                    File.WriteAllLines(
                                        Path.Combine(p, $"{o.File.Name}.S"),
                                        ovl.Disassemble(DisassemblyOptions.DebugPc)
                                    );

                                    File.WriteAllText(
                                        Path.Combine(p, "conf.ld"),
                                        ovl.LinkerScript
                                    );

                                    File.WriteAllText(
                                        Path.Combine(p, "Makefile"),
                                        OverlayCreator.GenerateMakefileForOvl(AppContext.BaseDirectory, o.File.Name, o.VmaStart)
                                    );

                                    File.WriteAllBytes(
                                        Path.Combine(p, $"{o.File.Name}.ovl.orig"),
                                        o.File.Contents.ToArray()
                                    );
                                }
                                break;

                            case ZeldaMode.GenerateOverlayRelocs:
                                Console.WriteLine(
                                    OverlayCreator.CreateCSourceFromOverlayRelocations(
                                        "__relocations",
                                        OverlayCreator.GetOverlayRelocationsFromElf(File.ReadAllBytes(files[0]))
                                    )
                                );
                                break;

                            case ZeldaMode.DumpRelocations:
                                Console.WriteLine(
                                    string.Join(
                                        Environment.NewLine,
                                        new[]
                                        {
                                            "ID      Sec. loc   Abs. addr    Sym value   Type        Section       Disassembly",
                                            //"    0  [0x0000001C/0x808FDEAC] (0x8090EE50) R_MIPS_HI16 .text         lui         $at,0x8091"
                                        }.Concat(
                                            LoadOverlay(files)
                                                .Relocations
                                                .Select((x, i) => string.Format("{0,5}  {1}", i, x))
                                        )
                                    )
                                );
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

                case Mode.AsmPatch:
                    {
                        _asmPatchOptions.Parse(extra);

                        var config = Toolchain.Configuration.FromEnvironment();

                        config.CFLAGS += $" \"-I{AppContext.BaseDirectory}/dist\"";


                        if (Verbosity > 0)
                            config.CommandDebugger = Console.Error.WriteLine;
                        var assembled = RomAssembler.AssembleSource(config, File.ReadAllText(AsmPatchOptions.PatchSource)).ToArray();
                        var input = File.ReadAllBytes(AsmPatchOptions.InputRom);

                        foreach (var a in assembled)
                            Utilities.WriteU32(a.Instruction, input, (int)a.RomAddress);

                        Rom.ApplyCrcs(input, Rom.RecalculateCrc(input));

                        File.WriteAllBytes(AsmPatchOptions.OutputRom, input);

                        if (Verbosity > 1)
                        {
                            Console.Error.WriteLine("Raw dump of patched values:");

                            foreach (var x in assembled)
                                Console.Error.WriteLine("  " + x);
                        }

                        Console.Error.WriteLine("Patched {0} bytes.", assembled.Length * 4);
                    }
                    break;

                default:
                    goto case Mode.ShowHelp;
            }
        }

        private static Overlay LoadOverlay(IReadOnlyList<string> args, Overlay.Options options = null)
        {
            var contents = File.ReadAllBytes(args[0]);

            var entryPoint = args.Count >= 2
                ? Convert.ToUInt32(args[1], 16)
                : Overlay.InferEntryPoint(contents);

            return new Overlay(entryPoint, contents, options);
        }

        static void MainDis(string[] args)
        {
            var file = File.ReadAllBytes(args[0]);
            var test = LoadOverlay(args, _overlayOptions);

            var disasm = new Disassembler(new DefaultDisassemblyFormatter(test.Symbols));

            Console.WriteLine(string.Join(Environment.NewLine, test.Disassemble(DisassemblyOptions.DebugPc)));
        }
    }
}
