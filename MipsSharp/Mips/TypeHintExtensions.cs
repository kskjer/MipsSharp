using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Mips
{
    public static class TypeHintExtensions
    {
        public static bool HasFlags(this TypeHint t, TypeHint flags) =>
            (t & flags) != 0;
    }
}
