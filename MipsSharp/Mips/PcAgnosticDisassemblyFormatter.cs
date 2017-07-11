using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Mips
{
    public class PcAgnosticDisassemblyFormatter : IDisassemblyFormatter
    {
        private readonly IDisassemblyFormatter _inner =
            new DefaultDisassemblyFormatter(new EmptySymbolRepository());

        public string FormatAddress(uint addr)
        {
            return _inner.FormatAddress(addr);
        }

        public string FormatBranchTarget(uint pc, Instruction insn)
        {
            var offset = insn.BranchOffset;

            return (offset >= 0 ? "+" : "") + offset;
        }

        public string FormatCop0(uint register)
        {
            return _inner.FormatCop0(register);
        }

        public string FormatFloatCompareOpcode(string name, string condition, string fmt)
        {
            return _inner.FormatFloatCompareOpcode(name, condition, fmt);
        }

        public string FormatFloatOpcode(string name, string fmt)
        {
            return _inner.FormatFloatOpcode(name, fmt);
        }

        public string FormatFloatOpcode(string name, string fmt_to, string fmt_from)
        {
            return _inner.FormatFloatOpcode(name, fmt_to, fmt_from);
        }

        public string FormatFpr(uint register)
        {
            return _inner.FormatFpr(register);
        }

        public string FormatGpr(uint register)
        {
            return _inner.FormatGpr(register);
        }

        public string FormatHex(uint v)
        {
            return _inner.FormatHex(v);
        }

        public string FormatHex16(uint h)
        {
            return _inner.FormatHex16(h);
        }

        public string FormatHex32(uint h)
        {
            return _inner.FormatHex32(h);
        }

        public string FormatHi16(uint pc, Instruction insn)
        {
            return _inner.FormatHi16(pc, insn);
        }

        public string FormatImmediate(short rawValue)
        {
            return _inner.FormatImmediate(rawValue);
        }

        public string FormatImmediate(uint pc, Instruction insn)
        {
            return _inner.FormatImmediate(pc, insn);
        }

        public string FormatJumpTarget(uint pc, Instruction insn)
        {
            return _inner.FormatJumpTarget(pc, insn);
        }

        public string FormatLo16(uint pc, Instruction insn)
        {
            return _inner.FormatLo16(pc, insn);
        }

        public string FormatLoadLocation(uint pc, Instruction insn, uint register)
        {
            return _inner.FormatLoadLocation(pc, insn, register);
        }

        public string FormatOpcode(string name)
        {
            return _inner.FormatOpcode(name);
        }

        public string FormatOperands(params string[] operands)
        {
            return _inner.FormatOperands(operands);
        }

        public string FormatShiftAmount(uint amount)
        {
            return _inner.FormatShiftAmount(amount);
        }
    }
}
