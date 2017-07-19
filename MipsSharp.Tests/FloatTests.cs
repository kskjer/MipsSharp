using Microsoft.VisualStudio.TestTools.UnitTesting;
using MipsSharp.Sys;
using MipsSharp.Zelda64;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MipsSharp.Tests
{
    [TestClass]
    public class FloatTests
    {
        public FloatTests()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        }

        [TestMethod]
        public void TestRegexApproximation()
        {
            Assert.AreEqual("0.13", Float.FloatRoundDecimalUp(0.129999995231628f).Result);
            Assert.AreEqual("0.015", Float.FloatRoundDecimalUp(0.014999999664723f).Result);
            Assert.AreEqual("3.6", Float.FloatRoundDecimalUp(3.59999990463257f).Result);
            Assert.AreEqual("0.9", Float.FloatRoundDecimalUp(0.899999976158142f).Result);
            Assert.AreEqual("0.072", Float.FloatRoundDecimalUp(0.0719999969005585f).Result);
            Assert.AreEqual("-0.072", Float.FloatRoundDecimalUp(-0.0719999969005585f).Result);
        }

        [TestMethod]
        public void TestApproximation()
        {
            Assert.AreEqual("-0.1", Float.GetClosestApproximation(-0.100000001490116f).Approximation);
            Assert.AreEqual("0.08", Float.GetClosestApproximation(0.0799999982118607f).Approximation);
            Assert.AreEqual("0", Float.GetClosestApproximation(0.0f).Approximation);
            Assert.AreEqual("2", Float.GetClosestApproximation(2f).Approximation);
            Assert.AreEqual("1", Float.GetClosestApproximation(1f).Approximation);
        }

        [TestMethod]
        public void TestAssemblyLine()
        {
            Assert.IsTrue(
                Float.GenerateAssemblyLine(0.200000002980232f)
                    .Last().Equals(
                        (".single", "0.2")
                    )
            );
            
            var x = Float.GenerateAssemblyLine(0x402E000007800000UL).ToArray();
            Assert.IsTrue(
                x.Last().Equals(
                    (".quad", "0x402E000007800000")
                )
            );

            var y = Float.GenerateAssemblyLine(0.199999988079071f).ToArray();
            Assert.IsTrue(
                y.Last().Equals(
                    (".word", "0x3E4CCCCC")
                )
            );

            var z = Float.GenerateAssemblyLine(15.0).ToArray();
            Assert.IsTrue(
                z.Last().Equals(
                    (".double", "15")
                )
            );

        }
    }
}
