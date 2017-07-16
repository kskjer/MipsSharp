using Microsoft.VisualStudio.TestTools.UnitTesting;
using MipsSharp.Mips;
using System;
using System.Collections.Generic;
using System.Text;

namespace MipsSharp.Tests
{
    [TestClass]
    public class DisassemblerTests
    {
        [TestMethod]
        public void TestBreakDisassembly()
        {
            // BREAK has a 20 bit field for a code, but it seems that only the upper 10 bits are 
            // supported by GAS.

            Assert.AreEqual(
                "0x3ff",
                Disassembler.DefaultWithoutPc.Disassemble(
                    0x80000000,
                    0x03ff000d
                )
                .Operands
                .ToLower()
            );
        }

        [TestMethod]
        public void TestLiViaOriDisassembly()
        {
            Assert.AreEqual(
                "t5,4",
                Disassembler.DefaultWithoutPc.Disassemble(
                    0,
                    0x240D0004
                )
                .Operands
            );
        }
    }
}
