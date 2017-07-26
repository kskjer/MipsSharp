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
    }
}
