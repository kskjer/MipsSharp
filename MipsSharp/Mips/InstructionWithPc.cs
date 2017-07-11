using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Mips
{
    public struct InstructionWithPc : IHasInstruction
    {
        public UInt32 Pc { get; }
        public Instruction Instruction { get; }

        public InstructionWithPc(UInt32 pc, Instruction insn)
        {
            Pc = pc;
            Instruction = insn;
        }

        public override string ToString()
        {
            return string.Format("{0:X8}: {1}", Pc, Disassembler.Default.Disassemble(Pc, Instruction));
        }

        public static implicit operator Instruction(InstructionWithPc w) => 
            w.Instruction;
    }
}
