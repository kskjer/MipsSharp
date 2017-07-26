using MipsSharp.Nintendo64;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp
{
    public static class EnumerableGamesharkCodeExtensions
    {
        public static IEnumerable<GamesharkCode> Optimize(this IEnumerable<GamesharkCode> codes)
        {
            var xxx = codes
                .ExpandRepeaters()
                .OrderBy(g => g.Address)
                .GroupByContiguousVariable(x => x.Code)
                .Select(x => new
                {
                    codes = x.ToArray(),
                    valDiffs = x
                        .Select(y => (GamesharkCode?)y)
                        .SelectWithNext(
                            (cur, next, prev) => new
                            {
                                code = cur,
                                valDiff = next.HasValue
                                    ? (next.Value.Value - cur.Value.Value)
                                    : (cur.Value.Value - prev.Value.Value)
                            }
                        )
                        .ToArray()
                })
                .SelectMany(x =>
                    x.codes.Length > 2 &&
                    x.codes[1].Address - x.codes[0].Address <= 0xFF &&
                    x.valDiffs.All(y => y.valDiff == x.valDiffs[0].valDiff)
                    ? x.codes
                        .Select((y, i) => new { y, i })
                        .GroupBy(y => y.i / 255)
                        .SelectMany((y, i) =>
                        {
                            var offset = i * 255;

                            return new[]
                            {
                                new GamesharkCode.Repeater(
                                    (byte)y.Count(),
                                    (byte)(x.codes[offset + 1].Address - x.codes[offset].Address),
                                    (short)x.valDiffs[offset].valDiff
                                ).Code,
                                x.codes[offset]
                            };
                        })
                    : x.codes
                )
                .ToArray();

            return xxx;
        }

        public static IEnumerable<GamesharkCode> ExpandRepeaters(this IEnumerable<GamesharkCode> codes)
        {
            GamesharkCode? preceding = null, 
                           repeater = null;

            foreach (var code in codes)
            {
                if (repeater.HasValue)
                {
                    var x = new GamesharkCode.Repeater(repeater.Value);
                    var canBeRepeated = preceding?.IsConditional == true;

                    for (var i = 0; i < x.Count; i++)
                    {
                        if (canBeRepeated)
                            yield return preceding.Value;

                        yield return new GamesharkCode(
                            code.CodeType,
                            (UInt32)(code.Address + i * x.AddressStep),
                            (UInt16)(code.Value + i * x.ValueStep)
                        );
                    }
                    
                    preceding = null;
                    repeater = null;

                    continue;
                }
                else if (code.CodeType == GamesharkCode.Type.Repeater)
                {
                    repeater = code;
                    continue;
                }

                if (!code.IsConditional)
                    yield return code;

                preceding = code;
            }
        }

        public static IEnumerable<GamesharkCode> NopOptimize(this IEnumerable<GamesharkCode> self, Func<GamesharkCode, bool> isCode)
        {
            GamesharkCode? previous = null;

            bool isAligned(GamesharkCode? gs) =>
                (gs.Value.Address & 3) == 0;

            foreach (var c in self)
            {
                if (previous.HasValue &&
                    isAligned(previous) &&
                    c.Value == 0 &&
                    c.Address - previous.Value.Address == 2)
                {
                    yield return new GamesharkCode(previous.Value.Code, 0x2400);

                    previous = null;
                }
                else
                {
                    if (isAligned(c) && c.Value == 0 && c.IsWrite16)
                        previous = c;
                    else
                        yield return c;
                }
            }
        }
    }
}
