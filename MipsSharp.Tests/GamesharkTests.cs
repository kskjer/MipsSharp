using Microsoft.VisualStudio.TestTools.UnitTesting;
using MipsSharp.Nintendo64;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp.Tests
{
    [TestClass]
    public class GamesharkTests
    {
        [TestMethod]
        public void TestCode()
        {
            var gs = new GamesharkCode(0x81002340, 0x2400);

            Assert.AreEqual(0x2400, gs.Value);
            Assert.AreEqual(0x2340U, gs.Address);
            Assert.AreEqual(GamesharkCode.Type.Write16, gs.CodeType);
            Assert.AreEqual(0x81002340U, gs.Code);
        }

        [TestMethod]
        public void TestCtorWithType()
        {
            Assert.AreEqual(0x81001234, new GamesharkCode(GamesharkCode.Type.Write16, 0x1234, 0).Code);
        }

        [TestMethod]
        public void TestRepeater()
        {
            var gs = new GamesharkCode(0x50002040, 0xFFF0);
            var repeater = new GamesharkCode.Repeater(gs);

            Assert.AreEqual(0x20, repeater.Count);
            Assert.AreEqual(0x40, repeater.AddressStep);
            Assert.AreEqual(-16, repeater.ValueStep);
        }

        [TestMethod]
        public void TestCodeExpansion()
        {
            var repeated = new[]
            {
                new GamesharkCode(0x50000204, 0x0001),
                new GamesharkCode(0x81000000, 0x0000)
            }
            .ExpandRepeaters()
            .ToArray();

            Assert.IsTrue(
                repeated
                    .Select(x => x.Code)
                    .SequenceEqual(new[]
                    {
                        0x81000000U,
                        0x81000004U,
                    })
            );

            Assert.IsTrue(
                repeated
                    .Select(x => x.Value)
                    .SequenceEqual(new ushort[]
                    {
                        0x0000,
                        0x0001,
                    })
            );

            var noRepeat = new[]
            {
                new GamesharkCode(0x81000000, 0),
                new GamesharkCode(0x81000004, 0),
                new GamesharkCode(0x81000008, 0),
                new GamesharkCode(0x8100000C, 0),
                new GamesharkCode(0x81000010, 0),
                new GamesharkCode(0x81000014, 0),
                new GamesharkCode(0x81000018, 0),
                new GamesharkCode(0x8100001C, 0),
            };

            Assert.IsTrue(
                noRepeat
                    .ExpandRepeaters()
                    .Select(x => x.Code)
                    .SequenceEqual(noRepeat.Select(x => x.Code))
            );
        }

        [TestMethod]
        public void TestCodeExpansionWithConditional()
        {
            var repeated = new[]
            {
                new GamesharkCode(0xD1000000, 0x0000),
                new GamesharkCode(0x50000204, 0x0001),
                new GamesharkCode(0x81000000, 0x0000)
            }
            .ExpandRepeaters()
            .ToArray();

            Assert.IsTrue(
                repeated
                    .Select(x => x.Code)
                    .SequenceEqual(new[]
                    {
                        0xD1000000U,
                        0x81000000U,
                        0xD1000000U,
                        0x81000004U,
                    })
            );

            Assert.IsTrue(
                repeated
                    .Select(x => x.Value)
                    .SequenceEqual(new ushort[]
                    {
                        0x0000,
                        0x0000,
                        0x0000,
                        0x0001,
                    })
            );
        }

        [TestMethod]
        public void TestOptimizer()
        {
            var xxx = new[]
            {
                new GamesharkCode(0x81000000, 3),
                new GamesharkCode(0x81000004, 6),
                new GamesharkCode(0x81000008, 9),
                new GamesharkCode(0x8100000C, 12),
                new GamesharkCode(0x81000010, 15),
                new GamesharkCode(0x81000014, 18),
                new GamesharkCode(0x81000018, 21),
                new GamesharkCode(0x8100001C, 24),
                new GamesharkCode(0x81001010, 0),
                new GamesharkCode(0x81001014, 0),
                new GamesharkCode(0x81001018, 0),
                new GamesharkCode(0x8100101C, 0),
            };

            var og = xxx
                .Optimize()
                .ToArray();

            Assert.IsTrue(
                new[]
                {
                    new GamesharkCode(0x50000804, 0x0003),
                    new GamesharkCode(0x81000000, 0x0003),
                    new GamesharkCode(0x50000404, 0x0000),
                    new GamesharkCode(0x81001010, 0x0000)
                }
                .SequenceEqual(og)
            );
        }

        [TestMethod]
        public void TestOptimizerLargeCodes()
        {
            var q = Enumerable.Range(0, 255 * 2)
                .Select(x => new GamesharkCode(0x81000000U + (uint)x * 4, (ushort)x))
                .Optimize()
                .ToArray();

            Assert.IsTrue(
                q.SequenceEqual(new[]
                {
                    new GamesharkCode(0x5000FF04, 0x0001),
                    new GamesharkCode(0x81000000, 0x0000),
                    new GamesharkCode(0x5000FF04, 0x0001),
                    new GamesharkCode(0x810003FC, 0x00FF),
                })
            );
        }

        [TestMethod]
        public void TestNopOptimization()
        {
            var Ept = Enumerable.Range(0, 4)
                .Select(x => new GamesharkCode(0x81000000 + (uint)x * 2, 0))
                .NopOptimize(x => true)
                .ToArray();
        }
    }
}
