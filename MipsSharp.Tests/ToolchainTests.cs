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
    }
}
