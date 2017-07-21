using MipsSharp.Host;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MipsSharp.Mips
{
    public class ToolchainException : Exception
    {
        public ToolchainException(string message)
            : base(message)
        {

        }

        public ToolchainException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }

    public class Toolchain
    {
        public class Configuration
        {
            private const string DefaultPrefix = "mips-elf-";

            public string AsPath { get; set; }
            public string CcPath { get; set; }
            public string LdPath { get; set; }
            public string ObjcopyPath { get; set; }

            public string CFLAGS { get; set; }
            public string LDFLAGS { get; set; }

            public Action<string> CommandDebugger { get; set; }

            public Configuration()
                : this(DefaultPrefix)
            {

            }

            private Configuration(string prefix)
            {
                AsPath = $"{prefix}as";
                CcPath = $"{prefix}gcc";
                LdPath = $"{prefix}ld";
                ObjcopyPath = $"{prefix}objcopy";
            }

            public static Configuration FromEnvironment()
            {
                var prefix = Environment.GetEnvironmentVariable("MIPS_TOOLCHAIN_PREFIX");

                if (string.IsNullOrWhiteSpace(prefix))
                    prefix = DefaultPrefix;

                return new Configuration(prefix)
                {
                    CFLAGS = Environment.GetEnvironmentVariable("MIPS_CFLAGS"),
                    LDFLAGS = Environment.GetEnvironmentVariable("MIPS_LDFLAGS"),
                };
            }
        }


        private static void Execute(Configuration config, string path, string strArgs, IEnumerable<string> arrayArgs, Action<StreamWriter> withStdin = null)
        {
            string args;

            var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args = strArgs + " " + string.Join(
                        " ",
                        arrayArgs.Select(x => $"\"{x}\"")
                    ),
                    UseShellExecute = false,
                    RedirectStandardInput = withStdin != null,
                    RedirectStandardError = true,
                }
            );

            if (withStdin != null)
            {
                var ms = new MemoryStream();

                using (var tmp = new StreamWriter(ms))
                    withStdin(tmp);

                var writtenStr = Encoding.UTF8.GetString(ms.ToArray());
                var escapedStr = StrToPrintf(writtenStr);

                config.CommandDebugger?.Invoke(
                    $"{escapedStr} | {path} {args}"
                );

                process.StandardInput.Write(writtenStr);
                process.StandardInput.Dispose();
            }
            else
            {
                config.CommandDebugger?.Invoke(
                    $"{path} {args}"
                );
            }

            process.WaitForExit();

            var stderr = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
                throw new ToolchainException($"`{Path.GetFileName(path)} {args}` exited with code {process.ExitCode}: {stderr}");
        }

        private static string StrToPrintf(string writtenStr)
        {
            return "printf '" + writtenStr
                .Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("'", "'\"'\"'")
                .Replace("%", "%%") + "'";
        }
        public static TemporaryFile Assemble(Configuration config, string source, IEnumerable<string> extraArgs = null)
        {
            extraArgs = extraArgs ?? new string[0];

            var outFile = new TemporaryFile();

            try
            {
                Execute(
                    config,
                    config.CcPath,
                    config.CFLAGS,
                    extraArgs
                        .Concat(new[] { "-x", "assembler-with-cpp", "-c", "-o", outFile.Path, "-" }),
                    (stdin) =>
                        stdin.Write(source)
                );
            }
            catch (ToolchainException e)
            {
                throw new ToolchainException(
                    "Assemble failed. Source code:" + Environment.NewLine + source,
                    e
                );
            }

            return outFile;
        }

        public static TemporaryFile Link(Configuration config, IEnumerable<string> inputObjects, string linkerScript, IEnumerable<string> extraArgs = null)
        {
            extraArgs = extraArgs ?? new string[0];

            var outFile = new TemporaryFile();

            using (var linkerScriptFile = new TemporaryFile())
            {
                File.WriteAllText(linkerScriptFile.Path, linkerScript);

                config.CommandDebugger?.Invoke(
                    $"{StrToPrintf(linkerScript)} > {linkerScriptFile.Path}"
                );

                Execute(
                    config,
                    config.LdPath,
                    config.LDFLAGS,
                    extraArgs
                        .Concat(new[] { "-T", linkerScriptFile.Path, "-o", outFile.Path })
                        .Concat(inputObjects)
                );
            }

            return outFile;
        }

        public static ImmutableArray<byte> ToBinary(Configuration config, string inputObject, IEnumerable<string> extraArgs = null)
        {
            extraArgs = extraArgs ?? new string[0];

            using (var outFile = new TemporaryFile())
            {
                Execute(
                    config,
                    config.ObjcopyPath,
                    null,
                    extraArgs
                        .Concat(new[] { "-O", "binary", inputObject, outFile.Path })
                );

                return File.ReadAllBytes(outFile.Path).ToImmutableArray();
            }
        }

        public static string GenerateLinkerScript(UInt32 entryPoint) =>
            @"
            ENTRY_POINT = 0x" + entryPoint.ToString("X8") + @";

            OUTPUT_ARCH( mips )
            OUTPUT_FORMAT( ""elf32-bigmips"", ""elf32-bigmips"", ""elf32-littlemips"" )

            ENTRY( ENTRY_POINT )


            SECTIONS
            {
	            . = ENTRY_POINT;
                .text :   { *(.text)   *(.text.*) }
                .data :   { *(.data)   *(.data.*) }
                .rodata : { *(.rodata) *(.rodata.*) }
                .bss (NOLOAD) : { *(.bss) *(.sbss) *(.scommon) }
            };";

        public static (string script, IEnumerable<string[]> chunkFilter) GenerateLinkerScript(IEnumerable<(string path, UInt32 entryPoint)> parts)
        {
            var tmp = parts.ToArray();
            var sections = new[] { ".text", ".data", ".rodata", ".bss" };

            string hexAddr(UInt32 x) =>
                "0x" + x.ToString("X8");

            IEnumerable<string> makeParts() =>
                tmp
                    .SelectMany((x, i) =>
                        new[] { $". = {hexAddr(x.entryPoint)};" }
                            .Concat(
                                sections.Select(y => $".chunk{i}{y.PadRight(sections.Max(z => z.Length))} : {{ KEEP(\"{x.path}\"({y} {y}.*)); }}")
                            )
                    );

            var script = string.Join(
                Environment.NewLine,
                new[]
                {
                    "OUTPUT_ARCH( mips )",
                    "OUTPUT_FORMAT( \"elf32-bigmips\" )",
                    "",
                   $"ENTRY_POINT = {hexAddr(tmp[0].entryPoint)};",
                    "",
                    "ENTRY( ENTRY_POINT )",
                    "",
                    "SECTIONS",
                    "{",
                    string.Join(
                        Environment.NewLine,
                        makeParts()
                            .Select(x => $"    {x}")
                    ),
                    "}"
                }
            );

            return (
                script,
                tmp.Select((x, i) => sections.Select(y => $".chunk{i}{y}").ToArray()).ToArray()
            );
        }

        public static (string script, IEnumerable<string[]> chunkFilter) GenerateLinkerScript(
            IEnumerable<(string name, IEnumerable<string> path, UInt32 entryPoint, UInt32 loadAddress)> parts)
        {
            var tmp = parts.ToArray();
            var sections = new[] { ".text", ".data", ".rodata", ".bss" };

            string hexAddr(UInt32 x) =>
                "0x" + x.ToString("X8");

            IEnumerable<string> makeParts() =>
                tmp
                    .SelectMany((x, i) =>
                        new[] { $". = {hexAddr(x.entryPoint)};" }
                            .Concat(sections.SelectMany(y =>
                            {
                                var seccname = $".chunk{i}{y}";
                                var z = y.TrimStart('.');

                                var keep = string.Join(
                                    " ",
                                    x.path
                                        .Select(w =>
                                        {
                                            var k = $"KEEP(\"{w}\"({y} {y}.*";

                                            if (z == "bss")
                                                k += " .sbss .scommon";

                                            k += "));";

                                            return k;
                                        })
                                );

                                var loadAddr = string.Join(
                                    " + ",
                                    new[] { hexAddr(x.loadAddress) }
                                    .Concat(sections
                                        .TakeWhile(w => w != y)
                                        .Select(w => $"SIZEOF(.chunk{i}{w})"))
                                );

                                loadAddr = $"AT({loadAddr})";

                                if (z == "bss")
                                    loadAddr = "";

                                return new[]
                                {
                                    $"__{x.name}_{z}_start = .;",
                                    z != "bss" ? $"__{x.name}_rom_{z}_start = LOADADDR({seccname});" : "",
                                    $".chunk{i}{y.PadRight(sections.Max(w => w.Length))} : {loadAddr} {{ {keep} }}",
                                    $"__{x.name}_{z}_end = .;",
                                    z != "bss" ? $"__{x.name}_rom_{z}_end = __{x.name}_rom_{z}_start + SIZEOF({seccname});" : ""
                                }
                                .Where(w => !string.IsNullOrWhiteSpace(w));
                            }))
                    );

            var script = string.Join(
                Environment.NewLine,
                new[]
                {
                    "OUTPUT_ARCH( mips )",
                    "OUTPUT_FORMAT( \"elf32-bigmips\" )",
                    "",
                   $"ENTRY_POINT = {hexAddr(tmp[0].entryPoint)};",
                    "",
                    "ENTRY( ENTRY_POINT )",
                    "",
                    "SECTIONS",
                    "{",
                    string.Join(
                        Environment.NewLine,
                        makeParts()
                            .Concat(
                                new[]
                                {
                                    "\"/DISCARD/\": { *(.MIPS.abiflags); }"
                                }
                            )
                            .Select(x => $"    {x}")
                    ),
                    "}"
                }
            );

            return (
                script,
                tmp.Select((x, i) => sections.Select(y => $".chunk{i}{y}").ToArray()).ToArray()
            );
        }
    }
}
