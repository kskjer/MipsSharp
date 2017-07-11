using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Mips
{
    [Flags]
    public enum TypeHint
    {
        Function = 1 << 0,
        BranchTarget = 1 << 10,
        Single = 1 << 1,
        Double = 1 << 2,
        Byte = 1 << 3,
        ByteUnsigned = 1 << 4,
        HalfWord = 1 << 5,
        HalfWordUnsigned = 1 << 6,
        Word = 1 << 7,
        WordUnsigned = 1 << 8,
        DoubleWord = 1 << 9
    }
}
