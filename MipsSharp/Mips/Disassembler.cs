using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp.Mips
{
    public class DisassemblerOutput
    {
        public string Opcode { get; private set; }
        public string Operands { get; private set; }



        public DisassemblerOutput(string opcode, string operands)
        {
            Opcode = opcode;
            Operands = operands;
        }

        public DisassemblerOutput(string opcode)
        {
            Opcode = opcode;
        }


        public override string ToString() =>
            Operands != null
            ? $"        {Opcode.PadRight(12)}{Operands}"
            : $"        {Opcode}";
    }

    public interface IDisassemblyFormatter
    {
        string FormatBranchTarget(UInt32 pc, Instruction insn);
        string FormatHi16(UInt32 pc, Instruction insn);
        string FormatLo16(UInt32 pc, Instruction insn);
        string FormatImmediate(UInt32 pc, Instruction insn);
        string FormatImmediate(short rawValue);
        string FormatGpr(uint register);
        string FormatFpr(uint register);
        string FormatCop0(uint register);
        string FormatOpcode(string name);
        string FormatFloatOpcode(string name, string fmt);
        string FormatFloatOpcode(string name, string fmt_to, string fmt_from);
        string FormatFloatCompareOpcode(string name, string condition, string fmt);
        string FormatAddress(UInt32 addr);
        string FormatHex32(UInt32 h);
        string FormatHex16(UInt32 h);
        string FormatLoadLocation(UInt32 pc, Instruction insn, uint register);
        string FormatShiftAmount(uint amount);
        string FormatJumpTarget(UInt32 pc, Instruction insn);
        string FormatOperands(params string[] operands);
        string FormatHex(uint v);
    }

    public interface ISymbolRepository
    {
        Symbol Lookup(UInt32 address);

        /// <summary>
        /// Lookup the symbol with the specified address.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        string LookupName(UInt32 address);

        /// <summary>
        /// Look up if a symbol is used at the specified PC. The disassembler only works on one 
        /// instruction at a time, and doesn't have the ability to do forward lookups.
        /// </summary>
        /// <param name="pc">The address of an instruction where a symbol might be referenced.</param>
        /// <returns></returns>
        string LookupReferencedSymbolNameAt(UInt32 pc);
    }

    public class EmptySymbolRepository : ISymbolRepository
    {
        public Symbol Lookup(uint address) => null;
        public string LookupName(uint address) => null;
        public string LookupReferencedSymbolNameAt(uint pc) => null;
    }

    public class DefaultDisassemblyFormatter : IDisassemblyFormatter
    {
        public static IReadOnlyList<string> Gpr { get; } =
            new string[]
            {
                "$zero", "$at", "v0",   "v1",   "a0",   "a1",   "a2",   "a3",
                "t0",   "t1",   "t2",   "t3",   "t4",   "t5",   "t6",   "t7",
                "s0",   "s1",   "s2",   "s3",   "s4",   "s5",   "s6",   "s7",
                "t8",   "t9",   "k0",   "k1",   "$gp",  "$sp",  "s8",   "$ra"
            };

        public static IReadOnlyList<string> Cop0 { get; } =
            new string[]
            {
                "C0_INX",      "C0_RAND",     "C0_ENTRYLO0",  "C0_ENTRYLO1",
                "C0_CONTEXT",  "C0_PAGEMASK", "C0_WIRED",     "$7",
                "C0_BADVADDR", "C0_COUNT",    "C0_ENTRYHI",   "C0_COMPARE",
                "C0_SR",       "C0_CAUSE",    "C0_EPC",       "C0_PRID",
                "C0_CONFIG",   "C0_LLADDR",   "C0_WATCHLO",   "C0_WATCHHI",
                "$20",         "$21",         "$22",          "$23",
                "$24",         "$25",         "C0_ECC",       "C0_CACHE_ERR",
                "C0_TAGLO",    "C0_TAGHI",    "C0_ERROR_EPC", "$30"
            };

        private readonly ISymbolRepository _symbols;

        public DefaultDisassemblyFormatter(ISymbolRepository symbols)
        {
            _symbols = symbols;
        }

        public string FormatAddress(uint addr) =>
            string.Format("0x{0:X8}", addr);

        public string FormatBranchTarget(uint pc, Instruction insn)
        {
            var target = (UInt32)(pc + insn.BranchOffset);

            return _symbols.LookupName(target) ?? FormatAddress(target);
        }

        public string FormatCop0(uint register) =>
            Cop0[(int)register & 0x1F];

        public string FormatFloatCompareOpcode(string name, string condition, string fmt) =>
            $"{name}.{condition}.{fmt}";

        public string FormatFloatOpcode(string name, string fmt) =>
            $"{name}.{fmt}";

        public string FormatFloatOpcode(string name, string fmt_to, string fmt_from) =>
            $"{name}.{fmt_to}.{fmt_from}";

        public string FormatFpr(uint register) =>
            string.Format("$f{0}", register & 0x1F);

        public string FormatGpr(uint register) =>
            Gpr[(int)register & 0x1F];

        public string FormatHex16(uint h) =>
            string.Format("0x{0:x}", h);

        public string FormatHex32(uint h) =>
            string.Format("0x{0:X8}", h);

        public string FormatHi16(uint pc, Instruction insn)
        {
            var sym = _symbols.LookupReferencedSymbolNameAt(pc);

            if (sym == null)
                return FormatHex16(insn.Immediate);

            return $"%hi({sym})";
        }

        public string FormatImmediate(uint pc, Instruction insn) =>
            insn.ImmediateSigned.ToString();

        public string FormatImmediate(short rawValue) =>
            rawValue.ToString();

        public string FormatJumpTarget(UInt32 pc, Instruction insn)
        {
            var fullTarget = insn.FullTarget(pc);

            return _symbols.LookupName(fullTarget) ?? FormatAddress(fullTarget);
        }

        public string FormatLo16(uint pc, Instruction insn)
        {
            var sym = _symbols.LookupReferencedSymbolNameAt(pc);

            if (sym == null)
                return insn.ImmediateSigned.ToString();

            return $"%lo({sym})";
        }

        public string FormatLoadLocation(uint pc, Instruction insn, uint register) =>
            $"{FormatLo16(pc, insn)}({FormatGpr(register)})";

        public string FormatOpcode(string name) =>
            name;

        public string FormatShiftAmount(uint amount) =>
            amount.ToString();

        public string FormatOperands(params string[] operands) =>
            string.Join(",", operands);

        public string FormatHex(uint v) =>
            string.Format("0x{0:X}", v);
    }

    public class Disassembler : IDisassembler
    {
        public static IDisassembler Default { get; } =
            new Disassembler(new DefaultDisassemblyFormatter(new EmptySymbolRepository()));

        public static IDisassembler DefaultWithoutPc { get; } =
            new Disassembler(new PcAgnosticDisassemblyFormatter());

        private readonly Interpreter<DisassemblerOutput> _interpreter =
            new Interpreter<DisassemblerOutput>(() => null);

        private readonly IDisassemblyFormatter _formatter;

        public Disassembler(IDisassemblyFormatter formatter)
        {
            _formatter = formatter;

            _interpreter.Handlers.ADD = (pc, insn) =>
                new DisassemblerOutput(
                    _formatter.FormatOpcode("add"),
                    _formatter.FormatOperands(
                        _formatter.FormatGpr(insn.GprRd),
                        _formatter.FormatGpr(insn.GprRs),
                        _formatter.FormatGpr(insn.GprRt)
                    )
                );

            _interpreter.Handlers.ADD = (pc, insn) =>
           {
               return new DisassemblerOutput(_formatter.FormatOpcode("add"),
               _formatter.FormatOperands(
                   _formatter.FormatGpr(insn.GprRd),
                   _formatter.FormatGpr(insn.GprRs),
                   _formatter.FormatGpr(insn.GprRt)
               ));
           };


            _interpreter.Handlers.ADDI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("addi"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.ADDIU = (pc, insn) =>
            {
                if (insn.GprRs != 0)
                {
                    return new DisassemblerOutput(_formatter.FormatOpcode("addiu"),
                    _formatter.FormatOperands(
                        _formatter.FormatGpr(insn.GprRt),
                        _formatter.FormatGpr(insn.GprRs),
                        _formatter.FormatLo16(pc, insn)
                    ));
                }
                else
                {
                    return new DisassemblerOutput(_formatter.FormatOpcode("li"),
                    _formatter.FormatOperands(
                        _formatter.FormatGpr(insn.GprRt),
                        _formatter.FormatImmediate(pc, insn)
                    ));
                };
            };


            _interpreter.Handlers.ADDU = (pc, insn) =>
            {
                if (insn.GprRt != 0)
                {
                    return new DisassemblerOutput(_formatter.FormatOpcode("addu"),
                    _formatter.FormatOperands(
                        _formatter.FormatGpr(insn.GprRd),
                        _formatter.FormatGpr(insn.GprRs),
                        _formatter.FormatGpr(insn.GprRt)
                    ));
                }
                else
                {
                    return new DisassemblerOutput(_formatter.FormatOpcode("move"),
                    _formatter.FormatOperands(
                        _formatter.FormatGpr(insn.GprRd),
                        _formatter.FormatGpr(insn.GprRs)
                    ));
                };
            };


            _interpreter.Handlers.AND = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("and"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.ANDI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("andi"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatHex16(insn.Immediate)
                ));
            };


            _interpreter.Handlers.BC1F = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bc1f"),
                _formatter.FormatOperands(
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BC1FL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bc1fl"),
                _formatter.FormatOperands(
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BC1T = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bc1t"),
                _formatter.FormatOperands(
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BC1TL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bc1tl"),
                _formatter.FormatOperands(
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BEQ = (pc, insn) =>
            {
                if (insn.GprRs == 0 && insn.GprRt == 0)
                    return new DisassemblerOutput(
                        _formatter.FormatOpcode("b"),
                        _formatter.FormatBranchTarget(pc, insn)
                    );

                return new DisassemblerOutput(_formatter.FormatOpcode("beq"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BEQL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("beql"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BGEZ = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bgez"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BGEZAL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bgezal"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BGEZALL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bgezall"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BGEZL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bgezl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BGTZ = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bgtz"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BGTZL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bgtzl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BLEZ = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("blez"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BLEZL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("blezl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BLTZ = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bltz"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BLTZAL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bltzal"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BLTZALL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bltzall"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BLTZL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bltzl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BNE = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bne"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BNEL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("bnel"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatBranchTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.BREAK = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("break"),
                _formatter.FormatOperands(
                    _formatter.FormatHex((insn.Word >> 6) & ((1 << 10) - 1))
                ));
            };


            _interpreter.Handlers.CACHE = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("cache"),
                _formatter.FormatOperands(
                    _formatter.FormatImmediate((short)((insn.Word >> 16) & 0x1F)),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.CFC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("cfc1"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatFpr(insn.GprRd)
                ));
            };


            _interpreter.Handlers.CTC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("ctc1"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatFpr(insn.GprRd)
                ));
            };


            _interpreter.Handlers.DADD = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dadd"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DADDI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("daddi"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.DADDIU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("daddiu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.DADDU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("daddu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DDIV = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("ddiv"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(0),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DDIVU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("ddivu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(0),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DIV = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("div"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(0),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DIVU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("divu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(0),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DMFC0 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dmfc0"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatCop0(insn.GprRd)
                ));
            };


            _interpreter.Handlers.DMFC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dmfc1"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatFpr(insn.GprRd)
                ));
            };


            _interpreter.Handlers.DMTC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dmtc1"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatFpr(insn.GprRd)
                ));
            };


            _interpreter.Handlers.DMULT = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dmult"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DMULTU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dmultu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DSLL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsll"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.DSLLV = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsllv"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DSLL32 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsll"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.DSRA = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsra"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.DSRAV = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsrav"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DSRA32 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsra32"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.DSRL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsrl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.DSRLV = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsrlv"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DSRL32 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsrl32"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.DSUB = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsub"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.DSUBU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("dsubu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.ERET = (pc, insn) =>
                new DisassemblerOutput(_formatter.FormatOpcode("eret"));


            _interpreter.Handlers.J = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("j"),
                _formatter.FormatOperands(
                    _formatter.FormatJumpTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.JAL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("jal"),
                _formatter.FormatOperands(
                    _formatter.FormatJumpTarget(pc, insn)
                ));
            };


            _interpreter.Handlers.JALR = (pc, insn) =>
                insn.GprRd == 31
                    ? new DisassemblerOutput(_formatter.FormatOpcode("jalr"), _formatter.FormatOperands(_formatter.FormatGpr(insn.GprRs)))
                    : new DisassemblerOutput(_formatter.FormatOpcode("jalr"), _formatter.FormatOperands(_formatter.FormatGpr(insn.GprRd), _formatter.FormatGpr(insn.GprRs)));


            _interpreter.Handlers.JR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("jr"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs)
                ));
            };


            _interpreter.Handlers.LB = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lb"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LBU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lbu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LD = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("ld"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LDC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("ldc1"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LDL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("ldl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LDR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("ldr"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LH = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lh"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LHU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lhu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("ll"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LLD = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lld"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LUI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lui"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatHi16(pc, insn)
                ));
            };


            _interpreter.Handlers.LW = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lw"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LWC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lwc1"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LWL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lwl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LWR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lwr"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.LWU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("lwu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.MFC0 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mfc0"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatCop0(insn.GprRd)
                ));
            };


            _interpreter.Handlers.MFC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mfc1"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatFpr(insn.GprRd)
                ));
            };


            _interpreter.Handlers.MFHI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mfhi"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd)
                ));
            };


            _interpreter.Handlers.MFLO = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mflo"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd)
                ));
            };


            _interpreter.Handlers.MTC0 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mtc0"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatCop0(insn.GprRd)
                ));
            };


            _interpreter.Handlers.MTC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mtc1"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatFpr(insn.GprRd)
                ));
            };


            _interpreter.Handlers.MTHI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mthi"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs)
                ));
            };


            _interpreter.Handlers.MTLO = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mthi"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs)
                ));
            };


            _interpreter.Handlers.MULT = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("mult"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.MULTU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("multu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.NOR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("nor"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.OR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("or"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.ORI = (pc, insn) =>
            {
                if (insn.GprRs == 0)
                    return new DisassemblerOutput(
                        _formatter.FormatOpcode("li"),
                        _formatter.FormatOperands(
                            _formatter.FormatGpr(insn.GprRt),
                            _formatter.FormatHex16(insn.Immediate)
                        )
                    );

                return new DisassemblerOutput(_formatter.FormatOpcode("ori"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatHex16(insn.Immediate)
                ));
            };


            _interpreter.Handlers.SB = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sb"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SC = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sc"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SCD = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("scd"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SD = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sd"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SDC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sdc1"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SDL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sdl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SDR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sdr"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SH = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sh"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SLL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sll"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.SLLV = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sllv"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs)
                ));
            };


            _interpreter.Handlers.SLT = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("slt"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.SLTI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("slti"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.SLTIU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sltiu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.SLTU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sltu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.SRA = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sra"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.SRAV = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("srav"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs)
                ));
            };


            _interpreter.Handlers.SRL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("srl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatShiftAmount(insn.ShAmt)
                ));
            };


            _interpreter.Handlers.SRLV = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("srlv"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs)
                ));
            };


            _interpreter.Handlers.SUB = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sub"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.SUBU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("subu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.SW = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("sw"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SWC1 = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("swc1"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SWL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("swl"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SWR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("swr"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatLoadLocation(
                        pc,
                        insn,
                        insn.GprBase
                    )
                ));
            };


            _interpreter.Handlers.SYNC = (pc, insn) =>
                new DisassemblerOutput(_formatter.FormatOpcode("sync"));


            _interpreter.Handlers.SYSCALL = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("syscall"),
                _formatter.FormatOperands(
                    _formatter.FormatHex((insn.Word >> 6) & ((1 << 20) - 1))
                ));
            };


            _interpreter.Handlers.TEQ = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("teq"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.TEQI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("teqi"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.TGE = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tge"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.TGEI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tgei"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.TGEIU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tgeiu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.TGEU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tgeu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.TLBP = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tlbp"));
            };


            _interpreter.Handlers.TLBR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tlbr"));

            };


            _interpreter.Handlers.TLBWI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tlbwi"));

            };


            _interpreter.Handlers.TLBWR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tlbwr"));

            };


            _interpreter.Handlers.TLT = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tlt"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.TLTI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tlti"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.TLTIU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tltiu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.TLTU = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tltu"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.TNE = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tne"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.TNEI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("tnei"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatImmediate(pc, insn)
                ));
            };


            _interpreter.Handlers.XOR = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("xor"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRd),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatGpr(insn.GprRt)
                ));
            };


            _interpreter.Handlers.XORI = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatOpcode("xori"),
                _formatter.FormatOperands(
                    _formatter.FormatGpr(insn.GprRt),
                    _formatter.FormatGpr(insn.GprRs),
                    _formatter.FormatHex16(insn.Immediate)
                ));
            };


            /*
             * Single precision floating point instructions
             */

            _interpreter.Handlers.ADD_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("add", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.SUB_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("sub", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.MUL_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("mul", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.DIV_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("div", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.SQRT_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("sqrt", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.ABS_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("abs", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.MOV_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("mov", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.NEG_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("neg", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.ROUND_L_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("round", "l", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.TRUNC_L_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("trunc", "l", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CEIL_L_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("ceil", "l", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };

            _interpreter.Handlers.FLOOR_L_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("floor", "l", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.ROUND_W_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("round", "w", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.TRUNC_W_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("trunc", "w", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CEIL_W_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("ceil", "w", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.FLOOR_W_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("floor", "w", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_D_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "d", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_W_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "w", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_L_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "l", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.C_F_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "f", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_UN_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "un", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_EQ_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "eq", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_UEQ_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ueq", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_OLT_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "olt", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_ULT_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ult", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_OLE_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ole", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_ULE_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ule", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_SF_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "sf", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_NGLE_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ngle", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_SEQ_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "seq", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_NGL_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ngl", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_LT_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "lt", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_NGE_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "nge", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_LE_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "le", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_NGT_S = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ngt", "s"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            /*
             * Double precision floating point instructions
             */

            _interpreter.Handlers.ADD_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("add", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.SUB_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("sub", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.MUL_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("mul", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.DIV_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("div", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.SQRT_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("sqrt", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.ABS_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("abs", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.MOV_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("mov", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.NEG_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("neg", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.ROUND_L_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("round", "l", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.TRUNC_L_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("trunc", "l", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CEIL_L_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("ceil", "l", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };

            _interpreter.Handlers.FLOOR_L_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("floor", "l", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.ROUND_W_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("round", "w", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.TRUNC_W_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("trunc", "w", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CEIL_W_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("ceil", "w", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.FLOOR_W_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("floor", "w", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_S_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "s", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_W_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "w", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_L_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "l", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.C_F_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "f", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_UN_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "un", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_EQ_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "eq", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_UEQ_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ueq", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_OLT_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "olt", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_ULT_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ult", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_OLE_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ole", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_ULE_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ule", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_SF_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "sf", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_NGLE_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ngle", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_SEQ_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "seq", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_NGL_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ngl", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_LT_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "lt", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_NGE_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "nge", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_LE_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "le", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            _interpreter.Handlers.C_NGT_D = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatCompareOpcode("c", "ngt", "d"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFs),
                    _formatter.FormatFpr(insn.FprFt)
                ));
            };


            /*
             * Long and word floating point instructions
             * (long = 64-bit, word = 32-bit)
             */

            _interpreter.Handlers.CVT_S_L = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "s", "l"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_D_L = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "d", "l"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_S_W = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "s", "w"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };


            _interpreter.Handlers.CVT_D_W = (pc, insn) =>
            {
                return new DisassemblerOutput(_formatter.FormatFloatOpcode("cvt", "d", "w"),
                _formatter.FormatOperands(
                    _formatter.FormatFpr(insn.FprFd),
                    _formatter.FormatFpr(insn.FprFs)
                ));
            };
        }

        public DisassemblerOutput Disassemble(UInt32 pc, Instruction insn) =>
            insn == 0
            ? new DisassemblerOutput("nop")
            : _interpreter.Execute(pc, insn);
    }
}
