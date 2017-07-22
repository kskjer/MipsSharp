﻿using MipsSharp.Binutils;
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
using static MipsSharp.Mips.SignatureDatabase;
using System.Reflection;
using System.Globalization;
using static MipsSharp.Nintendo64.RomAssembler;
using ELFSharp.ELF;

namespace MipsSharp
{
    class Program
    {
        private class CliException : Exception
        {
            public CliException(string message)
                : base(message)
            {

            }
        }

        enum Mode
        {
            NotSet = 0,
            Signatures,
            ImportSignatures,
            Zelda64,
            ShowHelp,
            EucJpStrings,
            AsmPatch,
            CreateProject,
            ElfPatch
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
            public static string ElfLocation;
            public static bool Gameshark;
            public static string BsdiffPath;
        }

        private class CreateProjectOptions
        {
            internal static string Type;
            internal static string Rom;
            internal static string Destination;
            internal static string Name;
        }

        private class ElfPatchOptions
        {
            public static string ElfFile;
            public static string InputRom;
            public static string OutputRom;
        }

        static readonly OptionSet _operatingModes = new OptionSet
        {
            { "asm-patch"        , "Assemble patch and apply to ROM",                v => _mode = Mode.AsmPatch         },
            { "create-project"   , "Set up Makefiles for mips-elf project",          v => _mode = Mode.CreateProject    },
            { "elf-patch"        , "Patch elf file into a ROM",                      v => _mode = Mode.ElfPatch         },
            { "eucjp-strings"    , "Find and dump EUC-JP encoded strings in input",  v => _mode = Mode.EucJpStrings     },
            { "import-signatures", "Import function signatures from .a file",        v => _mode = Mode.ImportSignatures },
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
            { "g|gameshark", "Build Gameshark code instead of writing to ROM", v => AsmPatchOptions.Gameshark = true },
            { "o|output=", "Output ROM", v => AsmPatchOptions.OutputRom = v },
            { "p|patch=", "Filename of BSDIFF patch to generate", v => AsmPatchOptions.BsdiffPath = v },
            { "e|elf=", "Write resulting ELF file here after linking. This can be useful for debugging.", v => AsmPatchOptions.ElfLocation = v },
            { "s|source=", "Source code of patch (should be MIPS assembly)", v => AsmPatchOptions.PatchSource = v }
        };

        static readonly OptionSet _createProjectOptions = new OptionSet
        {
            { "MipsSharp --create-project [FLAGS] [CHUNK SPECIFICATION]..." },
            { "" },
            { "This utility sets up a folder to contain a project. It sets up Makefiles and example sources." },
            { "" },
            { "The chunk specification has the following format: RAM address, ROM address, and chunk name. " +
              "These should be written as a comma separated list. Each specification should be one argument. " +
              "For example:" },
            { "" },
            { "  MipsSharp                       \\\n      --create-project            \\\n      --type=rom-hack             \\\n      --rom='Zelda64.z64'         \\\n      --destination=./new-folder  \\\n      0x80000400,0x00001000,hook  \\\n      0x800A8000,0x000A0000,main" },
            { "" },
            { "Options:" },
            { "t|type=", "The type of project to create. Currently only `rom-hack' is supported.", v => CreateProjectOptions.Type = v },
            { "r|rom=", "The path of the ROM to be patched, if ROM hack.", v => CreateProjectOptions.Rom = v },
            { "d|destination=", "Target folder to place the project files in.", v => CreateProjectOptions.Destination = v },
            { "n|name=", "Name of the project. Used in the generated files.", v => CreateProjectOptions.Name = v }
        };

        static readonly OptionSet _elfPatchOptions = new OptionSet
        {
            { "MipsSharp --elf-patch [OPTIONS]" },
            { "" },
            { "This utility patches a specially linked ELF file into a specified N64 ROM. " +
                "It will use the LMA (load address) of the section as the patch location. " +
                "Note that LMA is separate and less frequently encountered than the VMA " +
                "(virtual memory address). LMA is where in ROM something may reside, while " +
                "VMA is the location in RAM, and also the address at which the code will " +
                "execute." },
            { "" },
            { "CRCs in the output ROM are updated after patching." },
            { "" },
            { "e|elf=", "ELF file to patch", v => ElfPatchOptions.ElfFile = v },
            { "i|input=", "Input ROM", v => ElfPatchOptions.InputRom = v },
            { "o|output=", "Output filename of new ROM", v => ElfPatchOptions.OutputRom = v }
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
            catch (CliException e)
            {
                Console.Error.WriteLine(e.Message);

                return -1;
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

                    Console.WriteLine();
                    Console.WriteLine("Options for project creation:");
                    _createProjectOptions.WriteOptionDescriptions(Console.Out);

                    Console.WriteLine();
                    Console.WriteLine("Options for ELF file patching:");
                    _elfPatchOptions.WriteOptionDescriptions(Console.Out);
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

                        var assembled = BuildAsmPatch();

                        if (!AsmPatchOptions.Gameshark)
                        {
                            byte[] input = ApplyAssembledInstructionsToRom(
                                assembled,
                                AsmPatchOptions.InputRom,
                                AsmPatchOptions.OutputRom
                            );

                            DumpAssembledValues(assembled);

                            Console.Error.WriteLine("Patched {0} bytes.", assembled.Length * 4);

                            if (string.IsNullOrWhiteSpace(AsmPatchOptions.BsdiffPath))
                                break;

                            using (var patchOutput = File.OpenWrite(AsmPatchOptions.BsdiffPath))
                                deltaq.BsDiff.BsDiff.Create(File.ReadAllBytes(AsmPatchOptions.InputRom), input, patchOutput);

                            Console.Error.WriteLine("`{0}` of size {1} bytes created.", AsmPatchOptions.BsdiffPath, new FileInfo(AsmPatchOptions.BsdiffPath).Length);
                        }
                        else
                        {
                            var codes = assembled
                                .SelectMany(x => new[]
                                {
                                    (addr: x.RamAddress | 0x01000000,     hw: x.Instruction >> 16),
                                    (addr: x.RamAddress | 0x01000000 + 2, hw: x.Instruction & 0xFFFF)
                                })
                                .ToArray();

                            foreach (var c in codes)
                                Console.WriteLine("{0:X8} {1:X4}", c.addr, c.hw);
                        }
                    }
                    break;

                case Mode.CreateProject:
                    {
                        var chunks = _createProjectOptions.Parse(extra)
                            .Select(x =>
                            {
                                var arr = x.Split(',');

                                return (
                                    ramAddress: Convert.ToUInt32(arr[0], 16),
                                    romAddress: Convert.ToUInt32(arr[1], 16),
                                    name: arr[2],
                                    libs: arr.Skip(3)
                                );
                            })
                            .ToArray();

                        var dir = CreateProjectOptions.Destination;

                        Directory.CreateDirectory(dir);

                        switch (CreateProjectOptions.Type)
                        {
                            case "rom-hack":
                                var mkfile = new[]
                                {
                                   $"MIPSSHARP = dotnet \"{Path.Combine(AppContext.BaseDirectory, "MipsSharp.dll")}\"",
                                    "",
                                    "CC = mips-elf-gcc",
                                    "LD = mips-elf-ld",
                                    "",
                                   $"CPPFLAGS = \"-I{Path.Combine(AppContext.BaseDirectory, "dist")}\"",
                                    "ASFLAGS  = -mtune=vr4300 -march=vr4300",
                                    "CFLAGS   = -Os -G 0 -mabi=32 -mtune=vr4300 -march=vr4300",
                                    "",
                                   $"default: {CreateProjectOptions.Name}.elf",
                                    "",
                                    string.Join(
                                        Environment.NewLine,
                                        chunks
                                            .SelectMany(x => new[]
                                            {
                                                $"{x.name}.o: {x.name}-example.o",
                                                "\t$(LD) -r -o $@ $^",
                                                ""
                                            })
                                    ),
                                   $"{CreateProjectOptions.Name}.elf: {string.Join(" ", chunks.Select(x => x.name + ".o"))}",
                                   $"\t$(LD) -T {CreateProjectOptions.Name}.ld -o $@ {string.Join(" ", chunks.SelectMany(x => x.libs).Select(x => $"-l{x}"))} $^",
                                    "",
                                   $"patch: {CreateProjectOptions.Name}.z64",
                                    "",
                                   $"{CreateProjectOptions.Name}.z64: {CreateProjectOptions.Name}.elf",
                                   $"\t$(MIPSSHARP) --elf-patch -e $^ -i \"{Path.GetFileName(CreateProjectOptions.Rom)}\" -o $@",
                                    "",
                                    "clean:",
                                   $"\trm -vf *.elf *.o {CreateProjectOptions.Name}.z64"
                                };

                                File.WriteAllText(
                                    Path.Combine(dir, "Makefile"),
                                    string.Join(Environment.NewLine, mkfile)
                                );

                                File.WriteAllText(
                                    Path.Combine(dir, $"{CreateProjectOptions.Name}.ld"),
                                    Toolchain.GenerateLinkerScript(
                                        chunks
                                            .Select(x => (x.name, new[] { x.name + ".o" }.Concat(x.libs.Select(y => $"lib{y}.a")), x.ramAddress, x.romAddress))
                                    )
                                    .script
                                );

                                var examples = chunks
                                    .Select(x => x.name.Contains("hook")
                                    ? new
                                    {
                                        Filename = $"{x.name}-example.S",
                                        Lines = new[]
                                        {
                                            "#include <mips.h>",
                                            "",
                                            "\t.set\t\tnoreorder",
                                            "\t.set\t\tnoat",
                                            "",
                                            $"\t.global\t\t{x.name}_example",
                                            $"{x.name}_example:",
                                            $"\tjal\t\t" + (chunks.Length > 1 ? $"{chunks.First(y => y.name != x.name).name}_example" : "0x400"),
                                            "\tli\t\ta0,4"
                                        }
                                    }
                                    : new
                                    {
                                        Filename = $"{x.name}-example.c",
                                        Lines = new[]
                                        {
                                            $"#include \"{CreateProjectOptions.Name}.h\"",
                                            "",
                                            "int",
                                            $"{x.name}_example ( void )",
                                            "{",
                                            $"\tchar *ptr = &__{x.name}_bss_start;",
                                            "",
                                            $"\twhile (ptr < &__{x.name}_bss_end)",
                                            "\t\t*(ptr++) = 0;",
                                            "}",
                                            ""
                                        }
                                    });

                                foreach (var x in examples)
                                    File.WriteAllText(Path.Combine(dir, x.Filename), string.Join(Environment.NewLine, x.Lines));

                                var guardDef = $"__{CreateProjectOptions.Name.ToUpper()}_H__";

                                var header = new[]
                                {
                                    $"#ifndef {guardDef}",
                                    $"#define {guardDef}",
                                    "",
                                    string.Join(
                                        Environment.NewLine,
                                        chunks
                                            .SelectMany(x => new[] { "text", "data", "rodata", "bss" }.Select(y => new { x, y }))
                                            .SelectMany(x => new[]
                                            {
                                                $"extern char __{x.x.name}_{x.y}_start;",
                                                $"extern char __{x.x.name}_{x.y}_end;",
                                                $"extern char __{x.x.name}_rom_{x.y}_start;",
                                                $"extern char __{x.x.name}_rom_{x.y}_end;",
                                            })
                                            .Where(x => !x.Contains("_rom_bss"))
                                    ),
                                    "",
                                    $"#endif /* {guardDef} */",
                                    ""
                                };

                                File.WriteAllText(Path.Combine(dir, CreateProjectOptions.Name + ".h"), string.Join(Environment.NewLine, header));
                                File.Copy(CreateProjectOptions.Rom, Path.Combine(dir, Path.GetFileName(CreateProjectOptions.Rom)), true);
                                break;

                            default:
                                Console.Error.WriteLine("Project type `{0}' not supported.", CreateProjectOptions.Type);
                                break;
                        }
                    }
                    break;

                case Mode.ElfPatch:
                    {
                        _elfPatchOptions.Parse(extra);

                        var data = ELFReader.Load<uint>(ElfPatchOptions.ElfFile)
                            .GetAllocatableData()
                            .ToArray();

                        ApplyAssembledInstructionsToRom(
                            data,
                            ElfPatchOptions.InputRom,
                            ElfPatchOptions.OutputRom
                        );

                        DumpAssembledValues(data);

                        Console.Error.WriteLine(
                            string.Join(
                                Environment.NewLine,
                                data
                                    .GroupByContiguousAddresses(x => x.RamAddress)
                                    .Select(x => $"Patched {x.Count() * 4} bytes at 0x{x.First().RomAddress.ToString("X8")}.")
                            )
                        );

                        Console.Error.WriteLine("New ROM written to `{0}'.", ElfPatchOptions.OutputRom);
                    }
                    break;

                default:
                    goto case Mode.ShowHelp;
            }
        }

        private static void DumpAssembledValues(AssembledInstruction[] assembled)
        {
            if (Verbosity > 0)
            {
                Console.Error.WriteLine("Raw dump of patched values:");

                var lines = assembled
                    .SelectWithNext(
                        (cur, next) =>
                            next != null && next.RamAddress - cur.RamAddress > 4
                            ? new[] { $"    {cur}", "--" }
                            : new[] { $"    {cur}" }
                    )
                    .SelectMany(x => x);

                foreach (var x in lines)
                    Console.Error.WriteLine(x);
            }
        }

        private static byte[] ApplyAssembledInstructionsToRom(IEnumerable<AssembledInstruction> assembled, string inputRom, string outputRom)
        {
            var input = File.ReadAllBytes(inputRom);

            foreach (var a in assembled)
                Utilities.WriteU32(a.Instruction, input, (int)a.RomAddress);

            Rom.ApplyCrcs(input, Rom.RecalculateCrc(input));

            File.WriteAllBytes(outputRom, input);

            return input;
        }

        private static bool _alreadyUsedStdin = false;

        private static string ReadAllText(string pathFromCli)
        {
            if (pathFromCli != "-")
                return File.ReadAllText(pathFromCli);

            if (_alreadyUsedStdin)
                throw new CliException("Can't use stdin as an input argument more than once.");

            _alreadyUsedStdin = true;

            return Console.In.ReadToEnd();
        }

        private static AssembledInstruction[] BuildAsmPatch()
        {
            var config = Toolchain.Configuration.FromEnvironment();

            config.CFLAGS += $" \"-I{AppContext.BaseDirectory}/dist\"";

            if (Verbosity > 1)
                config.CommandDebugger = Console.Error.WriteLine;

            return RomAssembler.AssembleSource(
                config,
                ReadAllText(AsmPatchOptions.PatchSource),
                new AssembleSourceOptions
                {
                    PreserveElfAt = AsmPatchOptions.ElfLocation
                }
            ).ToArray();
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
