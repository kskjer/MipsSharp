using Microsoft.VisualStudio.TestTools.UnitTesting;
using MipsSharp.Mips;
using MipsSharp.Nintendo64;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp.Tests
{
    [TestClass]
    public class RomAssemblerTests
    {
        private static readonly string _source = @"#include <mips.h>
	.set		noreorder
	.set		noat

	.ram_origin	0x800431B0
	.rom_origin	0x00043db0

	lui             a0,0x8010
	jal             0x000AC440
	addiu           a0,a0,-20288

	.ram_origin 	0x800AC440
	.rom_origin	    0x000ad040

        addiu   $sp,$sp,-24
        sw      $16,16($sp)
        sw      $31,20($sp)
        jal     0x0097DD4
        move    $16,$4

        lhu     $2,0($16)
        nop
        andi    $2,$2,0x10
        beq     $2,$0,$L1
        nop

        lbu     $2,3($16)
        nop
        subu    $2,$0,$2
        sb      $2,3($16)
$L1:
        lw      $31,20($sp)
        lw      $16,16($sp)
        jr      $31
        addiu   $sp,$sp,24";

        [TestMethod]
        public void TestChunkExtraction()
        {
            var chunks = RomAssembler.GetChunks(_source).ToArray();

            Assert.AreEqual(0x800431b0U, chunks[0].RamAddress);
            Assert.AreEqual(0x00043db0U, chunks[0].RomAddress);
            Assert.AreEqual(0x800ac440U, chunks[1].RamAddress);
            Assert.AreEqual(0x000ad040U, chunks[1].RomAddress);
            Assert.AreEqual("\r\n\r\n\tlui             a0,0x8010\r\n\tjal             0x000AC440\r\n\taddiu           a0,a0,-20288\r\n\r\n\t", chunks[0].Assembly);
            Assert.AreEqual("\r\n\r\n        addiu   $sp,$sp,-24\r\n        sw      $16,16($sp)\r\n        sw      $31,20($sp)\r\n        jal     0x0097DD4\r\n        move    $16,$4\r\n\r\n        lhu     $2,0($16)\r\n        nop\r\n        andi    $2,$2,0x10\r\n        beq     $2,$0,$L1\r\n        nop\r\n\r\n        lbu     $2,3($16)\r\n        nop\r\n        subu    $2,$0,$2\r\n        sb      $2,3($16)\r\n$L1:\r\n        lw      $31,20($sp)\r\n        lw      $16,16($sp)\r\n        jr      $31\r\n        addiu   $sp,$sp,24", chunks[1].Assembly);
        }


        [TestMethod]
        public void TestCommonExtraction()
        {
            var common = string.Join("\n", RomAssembler.GetCommonLines(_source.Split(new[] { Environment.NewLine }, StringSplitOptions.None)));

            Assert.AreEqual("#include <mips.h>\n\t.set\t\tnoreorder\n\t.set\t\tnoat\n", common);
        }
    }
}
