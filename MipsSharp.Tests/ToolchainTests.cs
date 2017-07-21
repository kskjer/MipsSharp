using Microsoft.VisualStudio.TestTools.UnitTesting;
using MipsSharp.Mips;
using System;
using System.Collections.Generic;
using System.Text;

namespace MipsSharp.Tests
{
    [TestClass]
    public class ToolchainTests
    {
        [TestMethod]
        public void TestAs()
        {
            var config = Toolchain.Configuration.FromEnvironment();

            using (var test = Toolchain.Assemble(config, "la $4,ENTRY_POINT"))
            using (var elf = Toolchain.Link(config, new[] { test.Path }, Toolchain.GenerateLinkerScript(0x80809999)))
            {
                var binary = Toolchain.ToBinary(
                    config,
                    elf.Path
                );
            }
        }

        [TestMethod]
        public void TestDefaultPrefix()
        {
            Assert.AreEqual("mips-elf-ld", Toolchain.Configuration.FromEnvironment().LdPath);
        }

        [TestMethod]
        public void TestTest()
        {
            var sss = Toolchain.GenerateLinkerScript(
                new[]
                {
                    ("hook", new[] { "hook.o" } as IEnumerable<string>, 0x800A0000, 0x00002240U),
                    ("main", new[] { "main.o" }, 0x80200000, 0x00040000U)
                }
            );
        }
    }
}
