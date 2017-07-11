using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Mips
{
    public enum RelocationType
    {
        R_MIPS_32 = 2,
        R_MIPS_26 = 4,
        R_MIPS_HI16 = 5,
        R_MIPS_LO16 = 6
    }
}
