using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MipsSharp.Mips;
using System.Linq;
using static MipsSharp.Mips.SignatureDatabase;
using System.Collections.Generic;

namespace MipsSharp.Tests
{
    [TestClass]
    public class SignaturesTest
    {
        [TestMethod]
        public void TestDetection()
        {
            return;

            var signatures = new SignatureDatabase(@"..\..\..\signatures.db");

            var toCompare = signatures.Imports
                .SelectMany(i => i.Variants)
                .Select(v => new {
                    Variant = v,
                    ZeroedInsns = v.Instructions
                        .WithPc(0)
                        .ZeroRelocatedValues()
                        .ToList(),
                    RegularInsns = v.Instructions
                        .WithPc(0)
                        .ToList() });

            var faults = new List<Tuple<Variant, List<InstructionWithPc>>>();

            foreach(var c in toCompare)
            {
                var result = c.ZeroedInsns.SequenceEqual(c.RegularInsns);

                if (!result)
                    faults.Add(Tuple.Create(c.Variant, c.ZeroedInsns));
                    //$"{c.Variant} didn't work well with our PIC zeroing!";
            }

            if(faults.Count > 0 )
            {
                throw new Exception(string.Join(Environment.NewLine, new[] {
                    $"The following {faults.Count} variants were mangled by our algorithm:" }
                    .Concat(faults.Select(f => $" - {f.Item1}")))
                );
            }
        }
    }
}
