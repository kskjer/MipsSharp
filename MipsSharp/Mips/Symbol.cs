using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Mips
{
    public enum SymbolType
    {
        Internal,
        External
    }

    public class Symbol
    {
        public UInt32 Address { get; }
        public string Name { get; }
        public TypeHint TypeHint { get; }

        public override string ToString() =>
            string.Format("{0:X8} {1}", Address, Name) +
            (TypeHint != 0 ? $" ({TypeHint})" : "");

        public static string HintToName(TypeHint hint) =>
            hint.HasFlags(TypeHint.Function)         ? "func" :
            hint.HasFlags(TypeHint.Double)           ? "f64"  :
            hint.HasFlags(TypeHint.Single)           ? "f32"  :
            hint.HasFlags(TypeHint.Byte)             ? "i8"   :
            hint.HasFlags(TypeHint.ByteUnsigned)     ? "u8"   :
            hint.HasFlags(TypeHint.HalfWord)         ? "i16"  :
            hint.HasFlags(TypeHint.HalfWordUnsigned) ? "u16"  :
            hint.HasFlags(TypeHint.Word)             ? "i32"  :
            hint.HasFlags(TypeHint.WordUnsigned)     ? "u32"  :
            hint.HasFlags(TypeHint.DoubleWord)       ? "i64"  : "data";

        private static string GenName(UInt32 address, TypeHint hint) =>
            string.Format("{0}_{1:X8}", HintToName(hint), address);

        public Symbol(UInt32 address, TypeHint hint)
            : this(address, GenName(address, hint), hint)
        {

        }

        public Symbol(UInt32 address, string name, TypeHint typeHint)
            : this(address, name, typeHint, SymbolType.Internal)
        {

        }

        public Symbol(UInt32 address, string name, TypeHint typeHint, SymbolType type)
        {
            Address = address;
            Name = name;
            TypeHint = typeHint;
        }
    }
}
