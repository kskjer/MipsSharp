namespace MipsSharp.Mips
{
    public interface IDisassembler
    {
        DisassemblerOutput Disassemble(uint pc, Instruction insn);
    }
}