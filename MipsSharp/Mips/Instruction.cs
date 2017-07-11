using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp.Mips
{
    public struct Instruction : IHasInstruction
    {
        public UInt32 Word { get; private set; }

        public Instruction(UInt32 word)
        {
            Word = word;

        }

        #region Static stuff

        private static readonly Interpreter<bool> _branchInterp =
            new Interpreter<bool>(() => false);

        private static readonly Interpreter<bool> _loadStoreInterp =
            new Interpreter<bool>(() => false);

        private static readonly Interpreter<MipsInstruction> _insnInterp =
            new Interpreter<MipsInstruction>(() => MipsInstruction.Invalid);

        private static readonly IDisassembler _disassembler =
            Disassembler.DefaultWithoutPc;


        static Instruction()
        {
            _branchInterp.MetaHandlers.Branch = (pc, insn) => 
                true;

            _loadStoreInterp.MetaHandlers.LoadStore = (pc, insn) =>
                true;

            _insnInterp.Handlers.ABS_D     = (p, i) => MipsInstruction.ABS_D;
            _insnInterp.Handlers.ABS_S     = (p, i) => MipsInstruction.ABS_S;
            _insnInterp.Handlers.ADD       = (p, i) => MipsInstruction.ADD;
            _insnInterp.Handlers.ADD_D     = (p, i) => MipsInstruction.ADD_D;
            _insnInterp.Handlers.ADD_S     = (p, i) => MipsInstruction.ADD_S;
            _insnInterp.Handlers.ADDI      = (p, i) => MipsInstruction.ADDI;
            _insnInterp.Handlers.ADDIU     = (p, i) => MipsInstruction.ADDIU;
            _insnInterp.Handlers.ADDU      = (p, i) => MipsInstruction.ADDU;
            _insnInterp.Handlers.AND       = (p, i) => MipsInstruction.AND;
            _insnInterp.Handlers.ANDI      = (p, i) => MipsInstruction.ANDI;
            _insnInterp.Handlers.BC1F      = (p, i) => MipsInstruction.BC1F;
            _insnInterp.Handlers.BC1FL     = (p, i) => MipsInstruction.BC1FL;
            _insnInterp.Handlers.BC1T      = (p, i) => MipsInstruction.BC1T;
            _insnInterp.Handlers.BC1TL     = (p, i) => MipsInstruction.BC1TL;
            _insnInterp.Handlers.BEQ       = (p, i) => MipsInstruction.BEQ;
            _insnInterp.Handlers.BEQL      = (p, i) => MipsInstruction.BEQL;
            _insnInterp.Handlers.BGEZ      = (p, i) => MipsInstruction.BGEZ;
            _insnInterp.Handlers.BGEZAL    = (p, i) => MipsInstruction.BGEZAL;
            _insnInterp.Handlers.BGEZALL   = (p, i) => MipsInstruction.BGEZALL;
            _insnInterp.Handlers.BGEZL     = (p, i) => MipsInstruction.BGEZL;
            _insnInterp.Handlers.BGTZ      = (p, i) => MipsInstruction.BGTZ;
            _insnInterp.Handlers.BGTZL     = (p, i) => MipsInstruction.BGTZL;
            _insnInterp.Handlers.BLEZ      = (p, i) => MipsInstruction.BLEZ;
            _insnInterp.Handlers.BLEZL     = (p, i) => MipsInstruction.BLEZL;
            _insnInterp.Handlers.BLTZ      = (p, i) => MipsInstruction.BLTZ;
            _insnInterp.Handlers.BLTZAL    = (p, i) => MipsInstruction.BLTZAL;
            _insnInterp.Handlers.BLTZALL   = (p, i) => MipsInstruction.BLTZALL;
            _insnInterp.Handlers.BLTZL     = (p, i) => MipsInstruction.BLTZL;
            _insnInterp.Handlers.BNE       = (p, i) => MipsInstruction.BNE;
            _insnInterp.Handlers.BNEL      = (p, i) => MipsInstruction.BNEL;
            _insnInterp.Handlers.BREAK     = (p, i) => MipsInstruction.BREAK;
            _insnInterp.Handlers.C_EQ_D    = (p, i) => MipsInstruction.C_EQ_D;
            _insnInterp.Handlers.C_EQ_S    = (p, i) => MipsInstruction.C_EQ_S;
            _insnInterp.Handlers.C_F_D     = (p, i) => MipsInstruction.C_F_D;
            _insnInterp.Handlers.C_F_S     = (p, i) => MipsInstruction.C_F_S;
            _insnInterp.Handlers.C_LE_D    = (p, i) => MipsInstruction.C_LE_D;
            _insnInterp.Handlers.C_LE_S    = (p, i) => MipsInstruction.C_LE_S;
            _insnInterp.Handlers.C_LT_D    = (p, i) => MipsInstruction.C_LT_D;
            _insnInterp.Handlers.C_LT_S    = (p, i) => MipsInstruction.C_LT_S;
            _insnInterp.Handlers.C_NGE_D   = (p, i) => MipsInstruction.C_NGE_D;
            _insnInterp.Handlers.C_NGE_S   = (p, i) => MipsInstruction.C_NGE_S;
            _insnInterp.Handlers.C_NGL_D   = (p, i) => MipsInstruction.C_NGL_D;
            _insnInterp.Handlers.C_NGL_S   = (p, i) => MipsInstruction.C_NGL_S;
            _insnInterp.Handlers.C_NGLE_D  = (p, i) => MipsInstruction.C_NGLE_D;
            _insnInterp.Handlers.C_NGLE_S  = (p, i) => MipsInstruction.C_NGLE_S;
            _insnInterp.Handlers.C_NGT_D   = (p, i) => MipsInstruction.C_NGT_D;
            _insnInterp.Handlers.C_NGT_S   = (p, i) => MipsInstruction.C_NGT_S;
            _insnInterp.Handlers.C_OLE_D   = (p, i) => MipsInstruction.C_OLE_D;
            _insnInterp.Handlers.C_OLE_S   = (p, i) => MipsInstruction.C_OLE_S;
            _insnInterp.Handlers.C_OLT_D   = (p, i) => MipsInstruction.C_OLT_D;
            _insnInterp.Handlers.C_OLT_S   = (p, i) => MipsInstruction.C_OLT_S;
            _insnInterp.Handlers.C_SEQ_D   = (p, i) => MipsInstruction.C_SEQ_D;
            _insnInterp.Handlers.C_SEQ_S   = (p, i) => MipsInstruction.C_SEQ_S;
            _insnInterp.Handlers.C_SF_D    = (p, i) => MipsInstruction.C_SF_D;
            _insnInterp.Handlers.C_SF_S    = (p, i) => MipsInstruction.C_SF_S;
            _insnInterp.Handlers.C_UEQ_D   = (p, i) => MipsInstruction.C_UEQ_D;
            _insnInterp.Handlers.C_UEQ_S   = (p, i) => MipsInstruction.C_UEQ_S;
            _insnInterp.Handlers.C_ULE_D   = (p, i) => MipsInstruction.C_ULE_D;
            _insnInterp.Handlers.C_ULE_S   = (p, i) => MipsInstruction.C_ULE_S;
            _insnInterp.Handlers.C_ULT_D   = (p, i) => MipsInstruction.C_ULT_D;
            _insnInterp.Handlers.C_ULT_S   = (p, i) => MipsInstruction.C_ULT_S;
            _insnInterp.Handlers.C_UN_D    = (p, i) => MipsInstruction.C_UN_D;
            _insnInterp.Handlers.C_UN_S    = (p, i) => MipsInstruction.C_UN_S;
            _insnInterp.Handlers.CACHE     = (p, i) => MipsInstruction.CACHE;
            _insnInterp.Handlers.CEIL_L_D  = (p, i) => MipsInstruction.CEIL_L_D;
            _insnInterp.Handlers.CEIL_L_S  = (p, i) => MipsInstruction.CEIL_L_S;
            _insnInterp.Handlers.CEIL_W_D  = (p, i) => MipsInstruction.CEIL_W_D;
            _insnInterp.Handlers.CEIL_W_S  = (p, i) => MipsInstruction.CEIL_W_S;
            _insnInterp.Handlers.CFC1      = (p, i) => MipsInstruction.CFC1;
            _insnInterp.Handlers.CTC1      = (p, i) => MipsInstruction.CTC1;
            _insnInterp.Handlers.CVT_D_L   = (p, i) => MipsInstruction.CVT_D_L;
            _insnInterp.Handlers.CVT_D_S   = (p, i) => MipsInstruction.CVT_D_S;
            _insnInterp.Handlers.CVT_D_W   = (p, i) => MipsInstruction.CVT_D_W;
            _insnInterp.Handlers.CVT_L_D   = (p, i) => MipsInstruction.CVT_L_D;
            _insnInterp.Handlers.CVT_L_S   = (p, i) => MipsInstruction.CVT_L_S;
            _insnInterp.Handlers.CVT_S_D   = (p, i) => MipsInstruction.CVT_S_D;
            _insnInterp.Handlers.CVT_S_L   = (p, i) => MipsInstruction.CVT_S_L;
            _insnInterp.Handlers.CVT_S_W   = (p, i) => MipsInstruction.CVT_S_W;
            _insnInterp.Handlers.CVT_W_D   = (p, i) => MipsInstruction.CVT_W_D;
            _insnInterp.Handlers.CVT_W_S   = (p, i) => MipsInstruction.CVT_W_S;
            _insnInterp.Handlers.DADD      = (p, i) => MipsInstruction.DADD;
            _insnInterp.Handlers.DADDI     = (p, i) => MipsInstruction.DADDI;
            _insnInterp.Handlers.DADDIU    = (p, i) => MipsInstruction.DADDIU;
            _insnInterp.Handlers.DADDU     = (p, i) => MipsInstruction.DADDU;
            _insnInterp.Handlers.DDIV      = (p, i) => MipsInstruction.DDIV;
            _insnInterp.Handlers.DDIVU     = (p, i) => MipsInstruction.DDIVU;
            _insnInterp.Handlers.DIV       = (p, i) => MipsInstruction.DIV;
            _insnInterp.Handlers.DIV_D     = (p, i) => MipsInstruction.DIV_D;
            _insnInterp.Handlers.DIV_S     = (p, i) => MipsInstruction.DIV_S;
            _insnInterp.Handlers.DIVU      = (p, i) => MipsInstruction.DIVU;
            _insnInterp.Handlers.DMFC1     = (p, i) => MipsInstruction.DMFC1;
            _insnInterp.Handlers.DMTC1     = (p, i) => MipsInstruction.DMTC1;
            _insnInterp.Handlers.DMULT     = (p, i) => MipsInstruction.DMULT;
            _insnInterp.Handlers.DMULTU    = (p, i) => MipsInstruction.DMULTU;
            _insnInterp.Handlers.DSLL      = (p, i) => MipsInstruction.DSLL;
            _insnInterp.Handlers.DSLL32    = (p, i) => MipsInstruction.DSLL32;
            _insnInterp.Handlers.DSLLV     = (p, i) => MipsInstruction.DSLLV;
            _insnInterp.Handlers.DSRA      = (p, i) => MipsInstruction.DSRA;
            _insnInterp.Handlers.DSRA32    = (p, i) => MipsInstruction.DSRA32;
            _insnInterp.Handlers.DSRAV     = (p, i) => MipsInstruction.DSRAV;
            _insnInterp.Handlers.DSRL      = (p, i) => MipsInstruction.DSRL;
            _insnInterp.Handlers.DSRL32    = (p, i) => MipsInstruction.DSRL32;
            _insnInterp.Handlers.DSRLV     = (p, i) => MipsInstruction.DSRLV;
            _insnInterp.Handlers.DSUB      = (p, i) => MipsInstruction.DSUB;
            _insnInterp.Handlers.DSUBU     = (p, i) => MipsInstruction.DSUBU;
            _insnInterp.Handlers.ERET      = (p, i) => MipsInstruction.ERET;
            _insnInterp.Handlers.FLOOR_L_D = (p, i) => MipsInstruction.FLOOR_L_D;
            _insnInterp.Handlers.FLOOR_L_S = (p, i) => MipsInstruction.FLOOR_L_S;
            _insnInterp.Handlers.FLOOR_W_D = (p, i) => MipsInstruction.FLOOR_W_D;
            _insnInterp.Handlers.FLOOR_W_S = (p, i) => MipsInstruction.FLOOR_W_S;
            _insnInterp.Handlers.J         = (p, i) => MipsInstruction.J;
            _insnInterp.Handlers.JAL       = (p, i) => MipsInstruction.JAL;
            _insnInterp.Handlers.JALR      = (p, i) => MipsInstruction.JALR;
            _insnInterp.Handlers.JR        = (p, i) => MipsInstruction.JR;
            _insnInterp.Handlers.LB        = (p, i) => MipsInstruction.LB;
            _insnInterp.Handlers.LBU       = (p, i) => MipsInstruction.LBU;
            _insnInterp.Handlers.LD        = (p, i) => MipsInstruction.LD;
            _insnInterp.Handlers.LDC1      = (p, i) => MipsInstruction.LDC1;
            _insnInterp.Handlers.LDL       = (p, i) => MipsInstruction.LDL;
            _insnInterp.Handlers.LDR       = (p, i) => MipsInstruction.LDR;
            _insnInterp.Handlers.LH        = (p, i) => MipsInstruction.LH;
            _insnInterp.Handlers.LHU       = (p, i) => MipsInstruction.LHU;
            _insnInterp.Handlers.LL        = (p, i) => MipsInstruction.LL;
            _insnInterp.Handlers.LUI       = (p, i) => MipsInstruction.LUI;
            _insnInterp.Handlers.LW        = (p, i) => MipsInstruction.LW;
            _insnInterp.Handlers.LWC1      = (p, i) => MipsInstruction.LWC1;
            _insnInterp.Handlers.LWL       = (p, i) => MipsInstruction.LWL;
            _insnInterp.Handlers.LWR       = (p, i) => MipsInstruction.LWR;
            _insnInterp.Handlers.LWU       = (p, i) => MipsInstruction.LWU;
            _insnInterp.Handlers.MFC0      = (p, i) => MipsInstruction.MFC0;
            _insnInterp.Handlers.MFC1      = (p, i) => MipsInstruction.MFC1;
            _insnInterp.Handlers.MFHI      = (p, i) => MipsInstruction.MFHI;
            _insnInterp.Handlers.MFLO      = (p, i) => MipsInstruction.MFLO;
            _insnInterp.Handlers.MOV_D     = (p, i) => MipsInstruction.MOV_D;
            _insnInterp.Handlers.MOV_S     = (p, i) => MipsInstruction.MOV_S;
            _insnInterp.Handlers.MTC0      = (p, i) => MipsInstruction.MTC0;
            _insnInterp.Handlers.MTC1      = (p, i) => MipsInstruction.MTC1;
            _insnInterp.Handlers.MTHI      = (p, i) => MipsInstruction.MTHI;
            _insnInterp.Handlers.MTLO      = (p, i) => MipsInstruction.MTLO;
            _insnInterp.Handlers.MUL_D     = (p, i) => MipsInstruction.MUL_D;
            _insnInterp.Handlers.MUL_S     = (p, i) => MipsInstruction.MUL_S;
            _insnInterp.Handlers.MULT      = (p, i) => MipsInstruction.MULT;
            _insnInterp.Handlers.MULTU     = (p, i) => MipsInstruction.MULTU;
            _insnInterp.Handlers.NEG_D     = (p, i) => MipsInstruction.NEG_D;
            _insnInterp.Handlers.NEG_S     = (p, i) => MipsInstruction.NEG_S;
            _insnInterp.Handlers.NOR       = (p, i) => MipsInstruction.NOR;
            _insnInterp.Handlers.OR        = (p, i) => MipsInstruction.OR;
            _insnInterp.Handlers.ORI       = (p, i) => MipsInstruction.ORI;
            _insnInterp.Handlers.ROUND_L_D = (p, i) => MipsInstruction.ROUND_L_D;
            _insnInterp.Handlers.ROUND_L_S = (p, i) => MipsInstruction.ROUND_L_S;
            _insnInterp.Handlers.ROUND_W_D = (p, i) => MipsInstruction.ROUND_W_D;
            _insnInterp.Handlers.ROUND_W_S = (p, i) => MipsInstruction.ROUND_W_S;
            _insnInterp.Handlers.SB        = (p, i) => MipsInstruction.SB;
            _insnInterp.Handlers.SC        = (p, i) => MipsInstruction.SC;
            _insnInterp.Handlers.SD        = (p, i) => MipsInstruction.SD;
            _insnInterp.Handlers.SDC1      = (p, i) => MipsInstruction.SDC1;
            _insnInterp.Handlers.SDL       = (p, i) => MipsInstruction.SDL;
            _insnInterp.Handlers.SDR       = (p, i) => MipsInstruction.SDR;
            _insnInterp.Handlers.SH        = (p, i) => MipsInstruction.SH;
            _insnInterp.Handlers.SLL       = (p, i) => MipsInstruction.SLL;
            _insnInterp.Handlers.SLLV      = (p, i) => MipsInstruction.SLLV;
            _insnInterp.Handlers.SLT       = (p, i) => MipsInstruction.SLT;
            _insnInterp.Handlers.SLTI      = (p, i) => MipsInstruction.SLTI;
            _insnInterp.Handlers.SLTIU     = (p, i) => MipsInstruction.SLTIU;
            _insnInterp.Handlers.SLTU      = (p, i) => MipsInstruction.SLTU;
            _insnInterp.Handlers.SQRT_D    = (p, i) => MipsInstruction.SQRT_D;
            _insnInterp.Handlers.SQRT_S    = (p, i) => MipsInstruction.SQRT_S;
            _insnInterp.Handlers.SRA       = (p, i) => MipsInstruction.SRA;
            _insnInterp.Handlers.SRAV      = (p, i) => MipsInstruction.SRAV;
            _insnInterp.Handlers.SRL       = (p, i) => MipsInstruction.SRL;
            _insnInterp.Handlers.SRLV      = (p, i) => MipsInstruction.SRLV;
            _insnInterp.Handlers.SUB       = (p, i) => MipsInstruction.SUB;
            _insnInterp.Handlers.SUB_D     = (p, i) => MipsInstruction.SUB_D;
            _insnInterp.Handlers.SUB_S     = (p, i) => MipsInstruction.SUB_S;
            _insnInterp.Handlers.SUBU      = (p, i) => MipsInstruction.SUBU;
            _insnInterp.Handlers.SW        = (p, i) => MipsInstruction.SW;
            _insnInterp.Handlers.SWC1      = (p, i) => MipsInstruction.SWC1;
            _insnInterp.Handlers.SWL       = (p, i) => MipsInstruction.SWL;
            _insnInterp.Handlers.SWR       = (p, i) => MipsInstruction.SWR;
            _insnInterp.Handlers.SYNC      = (p, i) => MipsInstruction.SYNC;
            _insnInterp.Handlers.SYSCALL   = (p, i) => MipsInstruction.SYSCALL;
            _insnInterp.Handlers.TEQ       = (p, i) => MipsInstruction.TEQ;
            _insnInterp.Handlers.TLBP      = (p, i) => MipsInstruction.TLBP;
            _insnInterp.Handlers.TLBR      = (p, i) => MipsInstruction.TLBR;
            _insnInterp.Handlers.TLBWI     = (p, i) => MipsInstruction.TLBWI;
            _insnInterp.Handlers.TLBWR     = (p, i) => MipsInstruction.TLBWR;
            _insnInterp.Handlers.TRUNC_L_D = (p, i) => MipsInstruction.TRUNC_L_D;
            _insnInterp.Handlers.TRUNC_L_S = (p, i) => MipsInstruction.TRUNC_L_S;
            _insnInterp.Handlers.TRUNC_W_D = (p, i) => MipsInstruction.TRUNC_W_D;
            _insnInterp.Handlers.TRUNC_W_S = (p, i) => MipsInstruction.TRUNC_W_S;
            _insnInterp.Handlers.XOR       = (p, i) => MipsInstruction.XOR;
            _insnInterp.Handlers.XORI      = (p, i) => MipsInstruction.XORI;
            _insnInterp.Handlers.LLD       = (p, i) => MipsInstruction.LLD;
            _insnInterp.Handlers.DMFC0     = (p, i) => MipsInstruction.DMFC0;
            _insnInterp.Handlers.SCD       = (p, i) => MipsInstruction.SCD;
            _insnInterp.Handlers.TEQI      = (p, i) => MipsInstruction.TEQI;
            _insnInterp.Handlers.TGE       = (p, i) => MipsInstruction.TGE;
            _insnInterp.Handlers.TGEI      = (p, i) => MipsInstruction.TGEI;
            _insnInterp.Handlers.TGEIU     = (p, i) => MipsInstruction.TGEIU;
            _insnInterp.Handlers.TGEU      = (p, i) => MipsInstruction.TGEU;
            _insnInterp.Handlers.TLT       = (p, i) => MipsInstruction.TLT;
            _insnInterp.Handlers.TLTI      = (p, i) => MipsInstruction.TLTI;
            _insnInterp.Handlers.TLTIU     = (p, i) => MipsInstruction.TLTIU;
            _insnInterp.Handlers.TLTU      = (p, i) => MipsInstruction.TLTU;
            _insnInterp.Handlers.TNE       = (p, i) => MipsInstruction.TNE;
            _insnInterp.Handlers.TNEI      = (p, i) => MipsInstruction.TNEI;
        }

        #endregion

        #region Analytical methods

        public bool IsBranch =>
            _branchInterp.Execute(0x80000000, this);

        public bool IsLoadStore =>
            _loadStoreInterp.Execute(0x80000000, this);

        public string Disassembly =>
            _disassembler.Disassemble(0x80000000, this).ToString();

        public MipsInstruction Id =>
            _insnInterp.Execute(0x80000000, this);

        #endregion



        private const uint OpMask        = 0x3F;
        private const uint RegMask       = 0x1F;
        private const uint ImmediateMask = 0xFFFF;
        private const uint TargetMask    = 0x03FFFFFF;
        private const uint CopMask       = 0x3;

        //
        // Instruction ingredients
        //

        #region Instruction ingredients

        public uint Opcode
        {
            get { return (Word >> 26) & OpMask; }
            set { Word &= ~(OpMask << 26); Word |= (value & OpMask) << 26; }
        }

        public uint GprRs
        {
            get { return (Word >> 21) & RegMask; }
            set { Word &= ~(RegMask << 21); Word |= (value & RegMask) << 21; }
        }

        public uint GprRt
        {
            get { return (Word >> 16) & RegMask; }
            set { Word &= ~(RegMask << 16); Word |= (value & RegMask) << 16; }
        }

        public uint GprRd
        {
            get { return (Word >> 11) & RegMask; }
            set { Word &= ~(RegMask << 11); Word |= (value & RegMask) << 11; }
        }


        public uint FprFs
        {
            get { return GprRd; }
            set { GprRd = value; }
        }

        public uint FprFt
        {
            get { return GprRt; }
            set { GprRt = value; }
        }

        public uint FprFd
        {
            get { return (Word >> 6) & RegMask; }
            set { Word &= ~(RegMask << 6); Word |= (value & RegMask) << 6; }
        }

        public uint GprBase
        {
            get { return GprRs; }
            set { GprRs = value; }
        }

        public uint Immediate
        {
            get { return Word & ImmediateMask; }
            set { Word &= ~ImmediateMask; Word |= (value & ImmediateMask); }
        }

        public short ImmediateSigned =>
            (short)Immediate;

        public uint Offset
        {
            get { return Immediate; }
            set { Immediate = value; }
        }

        public int BranchOffset
        {
            get { return (int)((Int16)Immediate) * 4 + 4; }
            set
            {
                Int16 dst = (Int16)(value / 4);
                Word &= ~ImmediateMask;
                Word |= (UInt16)dst;
            }
        }

        private uint TargetRaw
        {
            get { return Word & TargetMask; }
            set { Word &= ~TargetMask; Word |= (value & TargetMask); }
        }

        public uint Target
        {
            get { return TargetRaw << 2; }
            set { TargetRaw = value >> 2; }
        }

        public uint FullTarget(uint pc) =>
            ((pc >> 2) & ~TargetMask) << 2 | Target;

        public uint Func
        {
            get { return Word & OpMask; }
            set { Word &= ~OpMask; Word |= (value & OpMask); }
        }

        public uint ShAmt
        {
            get { return FprFd; }
            set { FprFd = value; }
        }

        public uint Fmt
        {
            get { return GprRs; }
            set { GprRs = value; }
        }

        public uint CoProc
        {
            get { return (Word >> 26) & CopMask; }
            set { Word &= ~(CopMask << 26); Word |= (value & CopMask) << 26; }
        }

        Instruction IHasInstruction.Instruction => this;

        #endregion // Instruction ingredients


        public static bool operator ==(Instruction i1, Instruction i2) => i1.Word == i2.Word;
        public static bool operator !=(Instruction i1, Instruction i2) => i1.Word != i2.Word;

        public static implicit operator Instruction(UInt32 w) => new Instruction(w);
        public static implicit operator UInt32(Instruction i) => i.Word;

        public override bool Equals(object obj) => Word == ((Instruction)obj).Word;
        public override int GetHashCode() => Word.GetHashCode();
        public override string ToString() => Disassembly;
    }
}
