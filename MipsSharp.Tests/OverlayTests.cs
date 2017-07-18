using Microsoft.VisualStudio.TestTools.UnitTesting;
using MipsSharp.Mips;
using MipsSharp.Zelda64;
using System;
using System.Collections.Generic;
using System.Text;

namespace MipsSharp.Tests
{
    [TestClass]
    public class OverlayTests
    {
        [TestMethod]
        public void TestEntryPointInferral()
        {
            Assert.AreEqual(
                0x80800000,
                Overlay.InferEntryPointFromRelocs(
                    new[]
                    {
                        (RelocationType.R_MIPS_32, 0x80800040U),
                        (RelocationType.R_MIPS_26, 0x00800000U)
                    }
                )
            );

            Assert.AreEqual(
                0x80800000,
                Overlay.InferEntryPointFromRelocs(
                    new[]
                    {
                        (RelocationType.R_MIPS_32, 0x80800000U),
                        (RelocationType.R_MIPS_26, 0x00800040U)
                    }
                )
            );
        }
    }
}
