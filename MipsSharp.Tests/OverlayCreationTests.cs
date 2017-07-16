using Microsoft.VisualStudio.TestTools.UnitTesting;
using MipsSharp.Binutils;
using MipsSharp.Tests.Static;
using MipsSharp.Zelda64;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MipsSharp.Tests
{
    [TestClass]
    public class OverlayCreationTests
    {
        [TestMethod]
        public void TestRelocationExtraction()
        {
            ImmutableArray<uint> data;

            Assert.IsTrue(
                (data = OverlayCreator.GetOverlayRelocationsFromElf(ovl_En_Vase.Contents))
                    .SequenceEqual(new UInt32[] { 0x82000010, 0x82000014, 0x8200001C })
            );
        }

        [TestMethod]
        public void TestCSourceGenerationOfRelocationData()
        {
            var test = OverlayCreator.CreateCSourceFromOverlayRelocations(
                "__ovl_relocations",
                OverlayCreator.GetOverlayRelocationsFromElf(ovl_En_Vase.Contents)
            );

            Assert.AreEqual(
                @"const unsigned int
__ovl_relocations[] = 
{
	0x82000010U, 0x82000014U, 0x8200001CU, 
};",
                test
            );
        }

        [TestMethod]
        public void TestMakefileGeneration()
        {
            Assert.AreEqual(
                @"MIPSSHARP_PATH = ../..

OVL_ADDR = 0x80000000
OVL_NAME = ovl_En_Vase
PARTS    = $(OVL_NAME).o
TARGET   = $(OVL_NAME).elf

include ../../dist/z64-ovl.mk",
                OverlayCreator.GenerateMakefileForOvl("../..", "ovl_En_Vase", 0x80000000)
            );
        }
    }
}
