using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp.Tests
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void TestSelectWithNext()
        {
            var datum = new int?[]
            {
                1,
                2,
                3,
                4,
            };

            Assert.IsTrue(
                datum
                    .SelectWithNext((x, next) => new { x, next })
                    .SequenceEqual(
                        new[]
                        {
                            new { x = (int?)1, next = (int?)2 },
                            new { x = (int?)2, next = (int?)3 },
                            new { x = (int?)3, next = (int?)4 },
                            new { x = (int?)4, next = (int?)null }
                        }
                    )
            );

            Assert.IsTrue(
                datum
                    .SelectWithNext((x, next, prev) => new { x, next, prev })
                    .SequenceEqual(
                        new[]
                        {
                            new { x = (int?)1, next = (int?)2, prev = (int?)null },
                            new { x = (int?)2, next = (int?)3, prev = (int?)1 },
                            new { x = (int?)3, next = (int?)4, prev = (int?)2 },
                            new { x = (int?)4, next = (int?)null, prev = (int?)3 }
                        }
                    )
            );
        }

        [TestMethod]
        public void TestContiguousAddressVariableGrouping()
        {
            var input = new UInt32[]
            {
                0,
                4,
                8,
                0x100,
                0x108,
                0x110,
                0x2000,
                0x2001,
                0x2002,
                0x2003
            };

            var result = input
                .GroupByContiguousVariable(x => x)
                .ToArray();

            Assert.IsTrue(
                result[0]
                    .SequenceEqual(new uint[] { 0, 4, 8 })
            );

            Assert.IsTrue(
                result[1]
                    .SequenceEqual(new uint[] { 0x100, 0x108, 0x110 })
            );
        }
    }
}
