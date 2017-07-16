using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MipsSharp.Sys
{
    public class Float
    {
        private static readonly Regex _roundUpParts =
            new Regex(@"(\d+)\.(\d+?)(9{4,})(\d+)", RegexOptions.Compiled);

        private static readonly Regex _truncateParts =
            new Regex(@"(\-?\d\.\d+?)(0{4,}(\d+))", RegexOptions.Compiled);


        public static bool BinaryCompare(float a, float b) =>
            BitConverter.GetBytes(a).SequenceEqual(BitConverter.GetBytes(b));

        public static bool BinaryCompare(double a, double b) =>
            BitConverter.GetBytes(a).SequenceEqual(BitConverter.GetBytes(b));


        public static (string Result, bool Success) FloatRoundDecimalUp(double input)
        {
            var hit = false;
            var str = _roundUpParts.Replace(
                input.ToString(),
                m =>
                {
                    hit = true;

                    return string.Join(
                        "",
                        m.Groups[1].Value,
                        ".",
                        ((int.Parse(m.Groups[2].Value) + 1) + "").PadLeft(m.Groups[2].Value.Length, '0')
                    );
                }
            );

            return (str, hit);
        }

        public static (string Result, bool Success) FloatTruncateDecimal(double input)
        {
            var hit = false;
            var str = _truncateParts.Replace(
                input.ToString(),
                m =>
                {
                    hit = true;

                    return m.Groups[1].Value;
                }
            );

            return (str, hit);
        }


        public static (string Approximation, bool Successful) GetClosestApproximation(float input)
        {
            var truncatedAsString = FloatTruncateDecimal(input);
            var truncated = float.Parse(truncatedAsString.Result);

            if (truncatedAsString.Success && BinaryCompare(truncated, input))
                return (truncatedAsString.Result, true);

            var strApprox = FloatRoundDecimalUp(input);

            if (strApprox.Success && BinaryCompare(float.Parse(strApprox.Result), input))
                return (strApprox.Result, true);

            return (
                input.ToString(), 
                BinaryCompare(float.Parse(input.ToString()), input)
            );
        }


        public static IEnumerable<(string left, string right)> GenerateAssemblyLine(float input)
        {
            var approx = GetClosestApproximation(input);

            if (!approx.Successful)
            {
                yield return ($"/* Couldn't approximate 32-bit float. Was: {input} */", null);
                yield return (
                    ".word",
                    string.Format("0x{0:X8}", BitConverter.ToUInt32(BitConverter.GetBytes(input), 0))
                );
            }
            else
            {
                yield return (".single", approx.Approximation);
            }
        }


        public static IEnumerable<(string left, string right)> GenerateAssemblyLine(double input)
        {
            if (BinaryCompare(double.Parse(input.ToString()), input))
            {
                yield return (".double", input.ToString());
            }
            else
            {
                yield return ($"/* Couldn't approximate 64-bit float. Was: {input} */", null);
                yield return (
                    ".quad",
                    string.Format("0x{0:X16}", BitConverter.ToUInt64(BitConverter.GetBytes(input), 0))
                );
            }
        }


        public static IEnumerable<(string left, string right)> GenerateAssemblyLine(UInt64 input) =>
            GenerateAssemblyLine(BitConverter.ToDouble(BitConverter.GetBytes(input), 0));
    }
}
