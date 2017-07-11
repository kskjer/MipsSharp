using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp.Mips
{
    public class UnhandledInstructionException : Exception
    {
        public UInt32 Pc { get; set; }
        public Instruction Instruction { get; set; }

        public UnhandledInstructionException(string message, UInt32 pc, Instruction instruction)
            : base(message)
        {
            Pc = pc;
            Instruction = instruction;
        }
    }

    public enum MipsInstruction
    {
        ABS_D,
        ABS_S,
        ADD,
        ADD_D,
        ADD_S,
        ADDI,
        ADDIU,
        ADDU,
        AND,
        ANDI,
        BC1F,
        BC1FL,
        BC1T,
        BC1TL,
        BEQ,
        BEQL,
        BGEZ,
        BGEZAL,
        BGEZALL,
        BGEZL,
        BGTZ,
        BGTZL,
        BLEZ,
        BLEZL,
        BLTZ,
        BLTZAL,
        BLTZALL,
        BLTZL,
        BNE,
        BNEL,
        BREAK,
        C_EQ_D,
        C_EQ_S,
        C_F_D,
        C_F_S,
        C_LE_D,
        C_LE_S,
        C_LT_D,
        C_LT_S,
        C_NGE_D,
        C_NGE_S,
        C_NGL_D,
        C_NGL_S,
        C_NGLE_D,
        C_NGLE_S,
        C_NGT_D,
        C_NGT_S,
        C_OLE_D,
        C_OLE_S,
        C_OLT_D,
        C_OLT_S,
        C_SEQ_D,
        C_SEQ_S,
        C_SF_D,
        C_SF_S,
        C_UEQ_D,
        C_UEQ_S,
        C_ULE_D,
        C_ULE_S,
        C_ULT_D,
        C_ULT_S,
        C_UN_D,
        C_UN_S,
        CACHE,
        CEIL_L_D,
        CEIL_L_S,
        CEIL_W_D,
        CEIL_W_S,
        CFC1,
        CTC1,
        CVT_D_L,
        CVT_D_S,
        CVT_D_W,
        CVT_L_D,
        CVT_L_S,
        CVT_S_D,
        CVT_S_L,
        CVT_S_W,
        CVT_W_D,
        CVT_W_S,
        DADD,
        DADDI,
        DADDIU,
        DADDU,
        DDIV,
        DDIVU,
        DIV,
        DIV_D,
        DIV_S,
        DIVU,
        DMFC1,
        DMTC1,
        DMULT,
        DMULTU,
        DSLL,
        DSLL32,
        DSLLV,
        DSRA,
        DSRA32,
        DSRAV,
        DSRL,
        DSRL32,
        DSRLV,
        DSUB,
        DSUBU,
        ERET,
        FLOOR_L_D,
        FLOOR_L_S,
        FLOOR_W_D,
        FLOOR_W_S,
        J,
        JAL,
        JALR,
        JR,
        LB,
        LBU,
        LD,
        LDC1,
        LDL,
        LDR,
        LH,
        LHU,
        LL,
        LUI,
        LW,
        LWC1,
        LWL,
        LWR,
        LWU,
        MFC0,
        MFC1,
        MFHI,
        MFLO,
        MOV_D,
        MOV_S,
        MTC0,
        MTC1,
        MTHI,
        MTLO,
        MUL_D,
        MUL_S,
        MULT,
        MULTU,
        NEG_D,
        NEG_S,
        NOR,
        OR,
        ORI,
        ROUND_L_D,
        ROUND_L_S,
        ROUND_W_D,
        ROUND_W_S,
        SB,
        SC,
        SD,
        SDC1,
        SDL,
        SDR,
        SH,
        SLL,
        SLLV,
        SLT,
        SLTI,
        SLTIU,
        SLTU,
        SQRT_D,
        SQRT_S,
        SRA,
        SRAV,
        SRL,
        SRLV,
        SUB,
        SUB_D,
        SUB_S,
        SUBU,
        SW,
        SWC1,
        SWL,
        SWR,
        SYNC,
        SYSCALL,
        TEQ,
        TLBP,
        TLBR,
        TLBWI,
        TLBWR,
        TRUNC_L_D,
        TRUNC_L_S,
        TRUNC_W_D,
        TRUNC_W_S,
        XOR,
        XORI,
        LLD,
        DMFC0,
        SCD,
        TEQI,
        TGE,
        TGEI,
        TGEIU,
        TGEU,
        TLT,
        TLTI,
        TLTIU,
        TLTU,
        TNE,
        TNEI,

        Invalid = -1
    }

    public sealed class Interpreter<T>
    {
        public delegate T InstructionHandler(UInt32 pc, Instruction insn);

        public interface IHandlers
        {
            InstructionHandler ABS_D     { set; }
            InstructionHandler ABS_S     { set; }
            InstructionHandler ADD       { set; }
            InstructionHandler ADD_D     { set; }
            InstructionHandler ADD_S     { set; }
            InstructionHandler ADDI      { set; }
            InstructionHandler ADDIU     { set; }
            InstructionHandler ADDU      { set; }
            InstructionHandler AND       { set; }
            InstructionHandler ANDI      { set; }
            InstructionHandler BC1F      { set; }
            InstructionHandler BC1FL     { set; }
            InstructionHandler BC1T      { set; }
            InstructionHandler BC1TL     { set; }
            InstructionHandler BEQ       { set; }
            InstructionHandler BEQL      { set; }
            InstructionHandler BGEZ      { set; }
            InstructionHandler BGEZAL    { set; }
            InstructionHandler BGEZALL   { set; }
            InstructionHandler BGEZL     { set; }
            InstructionHandler BGTZ      { set; }
            InstructionHandler BGTZL     { set; }
            InstructionHandler BLEZ      { set; }
            InstructionHandler BLEZL     { set; }
            InstructionHandler BLTZ      { set; }
            InstructionHandler BLTZAL    { set; }
            InstructionHandler BLTZALL   { set; }
            InstructionHandler BLTZL     { set; }
            InstructionHandler BNE       { set; }
            InstructionHandler BNEL      { set; }
            InstructionHandler BREAK     { set; }
            InstructionHandler C_EQ_D    { set; }
            InstructionHandler C_EQ_S    { set; }
            InstructionHandler C_F_D     { set; }
            InstructionHandler C_F_S     { set; }
            InstructionHandler C_LE_D    { set; }
            InstructionHandler C_LE_S    { set; }
            InstructionHandler C_LT_D    { set; }
            InstructionHandler C_LT_S    { set; }
            InstructionHandler C_NGE_D   { set; }
            InstructionHandler C_NGE_S   { set; }
            InstructionHandler C_NGL_D   { set; }
            InstructionHandler C_NGL_S   { set; }
            InstructionHandler C_NGLE_D  { set; }
            InstructionHandler C_NGLE_S  { set; }
            InstructionHandler C_NGT_D   { set; }
            InstructionHandler C_NGT_S   { set; }
            InstructionHandler C_OLE_D   { set; }
            InstructionHandler C_OLE_S   { set; }
            InstructionHandler C_OLT_D   { set; }
            InstructionHandler C_OLT_S   { set; }
            InstructionHandler C_SEQ_D   { set; }
            InstructionHandler C_SEQ_S   { set; }
            InstructionHandler C_SF_D    { set; }
            InstructionHandler C_SF_S    { set; }
            InstructionHandler C_UEQ_D   { set; }
            InstructionHandler C_UEQ_S   { set; }
            InstructionHandler C_ULE_D   { set; }
            InstructionHandler C_ULE_S   { set; }
            InstructionHandler C_ULT_D   { set; }
            InstructionHandler C_ULT_S   { set; }
            InstructionHandler C_UN_D    { set; }
            InstructionHandler C_UN_S    { set; }
            InstructionHandler CACHE     { set; }
            InstructionHandler CEIL_L_D  { set; }
            InstructionHandler CEIL_L_S  { set; }
            InstructionHandler CEIL_W_D  { set; }
            InstructionHandler CEIL_W_S  { set; }
            InstructionHandler CFC1      { set; }
            InstructionHandler CTC1      { set; }
            InstructionHandler CVT_D_L   { set; }
            InstructionHandler CVT_D_S   { set; }
            InstructionHandler CVT_D_W   { set; }
            InstructionHandler CVT_L_D   { set; }
            InstructionHandler CVT_L_S   { set; }
            InstructionHandler CVT_S_D   { set; }
            InstructionHandler CVT_S_L   { set; }
            InstructionHandler CVT_S_W   { set; }
            InstructionHandler CVT_W_D   { set; }
            InstructionHandler CVT_W_S   { set; }
            InstructionHandler DADD      { set; }
            InstructionHandler DADDI     { set; }
            InstructionHandler DADDIU    { set; }
            InstructionHandler DADDU     { set; }
            InstructionHandler DDIV      { set; }
            InstructionHandler DDIVU     { set; }
            InstructionHandler DIV       { set; }
            InstructionHandler DIV_D     { set; }
            InstructionHandler DIV_S     { set; }
            InstructionHandler DIVU      { set; }
            InstructionHandler DMFC1     { set; }
            InstructionHandler DMTC1     { set; }
            InstructionHandler DMULT     { set; }
            InstructionHandler DMULTU    { set; }
            InstructionHandler DSLL      { set; }
            InstructionHandler DSLL32    { set; }
            InstructionHandler DSLLV     { set; }
            InstructionHandler DSRA      { set; }
            InstructionHandler DSRA32    { set; }
            InstructionHandler DSRAV     { set; }
            InstructionHandler DSRL      { set; }
            InstructionHandler DSRL32    { set; }
            InstructionHandler DSRLV     { set; }
            InstructionHandler DSUB      { set; }
            InstructionHandler DSUBU     { set; }
            InstructionHandler ERET      { set; }
            InstructionHandler FLOOR_L_D { set; }
            InstructionHandler FLOOR_L_S { set; }
            InstructionHandler FLOOR_W_D { set; }
            InstructionHandler FLOOR_W_S { set; }
            InstructionHandler J         { set; }
            InstructionHandler JAL       { set; }
            InstructionHandler JALR      { set; }
            InstructionHandler JR        { set; }
            InstructionHandler LB        { set; }
            InstructionHandler LBU       { set; }
            InstructionHandler LD        { set; }
            InstructionHandler LDC1      { set; }
            InstructionHandler LDL       { set; }
            InstructionHandler LDR       { set; }
            InstructionHandler LH        { set; }
            InstructionHandler LHU       { set; }
            InstructionHandler LL        { set; }
            InstructionHandler LUI       { set; }
            InstructionHandler LW        { set; }
            InstructionHandler LWC1      { set; }
            InstructionHandler LWL       { set; }
            InstructionHandler LWR       { set; }
            InstructionHandler LWU       { set; }
            InstructionHandler MFC0      { set; }
            InstructionHandler MFC1      { set; }
            InstructionHandler MFHI      { set; }
            InstructionHandler MFLO      { set; }
            InstructionHandler MOV_D     { set; }
            InstructionHandler MOV_S     { set; }
            InstructionHandler MTC0      { set; }
            InstructionHandler MTC1      { set; }
            InstructionHandler MTHI      { set; }
            InstructionHandler MTLO      { set; }
            InstructionHandler MUL_D     { set; }
            InstructionHandler MUL_S     { set; }
            InstructionHandler MULT      { set; }
            InstructionHandler MULTU     { set; }
            InstructionHandler NEG_D     { set; }
            InstructionHandler NEG_S     { set; }
            InstructionHandler NOR       { set; }
            InstructionHandler OR        { set; }
            InstructionHandler ORI       { set; }
            InstructionHandler ROUND_L_D { set; }
            InstructionHandler ROUND_L_S { set; }
            InstructionHandler ROUND_W_D { set; }
            InstructionHandler ROUND_W_S { set; }
            InstructionHandler SB        { set; }
            InstructionHandler SC        { set; }
            InstructionHandler SD        { set; }
            InstructionHandler SDC1      { set; }
            InstructionHandler SDL       { set; }
            InstructionHandler SDR       { set; }
            InstructionHandler SH        { set; }
            InstructionHandler SLL       { set; }
            InstructionHandler SLLV      { set; }
            InstructionHandler SLT       { set; }
            InstructionHandler SLTI      { set; }
            InstructionHandler SLTIU     { set; }
            InstructionHandler SLTU      { set; }
            InstructionHandler SQRT_D    { set; }
            InstructionHandler SQRT_S    { set; }
            InstructionHandler SRA       { set; }
            InstructionHandler SRAV      { set; }
            InstructionHandler SRL       { set; }
            InstructionHandler SRLV      { set; }
            InstructionHandler SUB       { set; }
            InstructionHandler SUB_D     { set; }
            InstructionHandler SUB_S     { set; }
            InstructionHandler SUBU      { set; }
            InstructionHandler SW        { set; }
            InstructionHandler SWC1      { set; }
            InstructionHandler SWL       { set; }
            InstructionHandler SWR       { set; }
            InstructionHandler SYNC      { set; }
            InstructionHandler SYSCALL   { set; }
            InstructionHandler TEQ       { set; }
            InstructionHandler TLBP      { set; }
            InstructionHandler TLBR      { set; }
            InstructionHandler TLBWI     { set; }
            InstructionHandler TLBWR     { set; }
            InstructionHandler TRUNC_L_D { set; }
            InstructionHandler TRUNC_L_S { set; }
            InstructionHandler TRUNC_W_D { set; }
            InstructionHandler TRUNC_W_S { set; }
            InstructionHandler XOR       { set; }
            InstructionHandler XORI      { set; }

            InstructionHandler LLD       { set; }
            InstructionHandler DMFC0     { set; }
            InstructionHandler SCD       { set; }
            InstructionHandler TEQI      { set; }
            InstructionHandler TGE       { set; }
            InstructionHandler TGEI      { set; }
            InstructionHandler TGEIU     { set; }
            InstructionHandler TGEU      { set; }
            InstructionHandler TLT       { set; }
            InstructionHandler TLTI      { set; }
            InstructionHandler TLTIU     { set; }
            InstructionHandler TLTU      { set; }
            InstructionHandler TNE       { set; }
            InstructionHandler TNEI      { set; }
        }
        public interface IMetaHandlers
        {
            InstructionHandler AllValid  { set; }
            InstructionHandler Load      { set; }
            InstructionHandler Store     { set; }
            InstructionHandler LoadStore { set; }
            InstructionHandler Branch    { set; }
        }

        private class HandlersImpl : IHandlers
        {
            private readonly Interpreter<T> _parent;

            public HandlersImpl(Interpreter<T> parent)
            {
                _parent = parent;
            }

            public InstructionHandler ABS_D     { set { _parent.ops_cop1_d[5]   = value; } }
            public InstructionHandler ABS_S     { set { _parent.ops_cop1_s[5]   = value; } }
            public InstructionHandler ADD       { set { _parent.ops_special[32] = value; } }
            public InstructionHandler ADD_D     { set { _parent.ops_cop1_d[0]   = value; } }
            public InstructionHandler ADD_S     { set { _parent.ops_cop1_s[0]   = value; } }
            public InstructionHandler ADDI      { set { _parent.ops_main[8]     = value; } }
            public InstructionHandler ADDIU     { set { _parent.ops_main[9]     = value; } }
            public InstructionHandler ADDU      { set { _parent.ops_special[33] = value; } }
            public InstructionHandler AND       { set { _parent.ops_special[36] = value; } }
            public InstructionHandler ANDI      { set { _parent.ops_main[12]    = value; } }
            public InstructionHandler BC1F      { set { _parent.ops_cop1_bc[0]  = value; } }
            public InstructionHandler BC1FL     { set { _parent.ops_cop1_bc[2]  = value; } }
            public InstructionHandler BC1T      { set { _parent.ops_cop1_bc[1]  = value; } }
            public InstructionHandler BC1TL     { set { _parent.ops_cop1_bc[3]  = value; } }
            public InstructionHandler BEQ       { set { _parent.ops_main[4]     = value; } }
            public InstructionHandler BEQL      { set { _parent.ops_main[20]    = value; } }
            public InstructionHandler BGEZ      { set { _parent.ops_regimm[1]   = value; } }
            public InstructionHandler BGEZAL    { set { _parent.ops_regimm[17]  = value; } }
            public InstructionHandler BGEZALL   { set { _parent.ops_regimm[19]  = value; } }
            public InstructionHandler BGEZL     { set { _parent.ops_regimm[3]   = value; } }
            public InstructionHandler BGTZ      { set { _parent.ops_main[7]     = value; } }
            public InstructionHandler BGTZL     { set { _parent.ops_main[23]    = value; } }
            public InstructionHandler BLEZ      { set { _parent.ops_main[6]     = value; } }
            public InstructionHandler BLEZL     { set { _parent.ops_main[22]    = value; } }
            public InstructionHandler BLTZ      { set { _parent.ops_regimm[0]   = value; } }
            public InstructionHandler BLTZAL    { set { _parent.ops_regimm[16]  = value; } }
            public InstructionHandler BLTZALL   { set { _parent.ops_regimm[18]  = value; } }
            public InstructionHandler BLTZL     { set { _parent.ops_regimm[2]   = value; } }
            public InstructionHandler BNE       { set { _parent.ops_main[5]     = value; } }
            public InstructionHandler BNEL      { set { _parent.ops_main[21]    = value; } }
            public InstructionHandler BREAK     { set { _parent.ops_special[13] = value; } }
            public InstructionHandler C_EQ_D    { set { _parent.ops_cop1_d[50]  = value; } }
            public InstructionHandler C_EQ_S    { set { _parent.ops_cop1_s[50]  = value; } }
            public InstructionHandler C_F_D     { set { _parent.ops_cop1_d[48]  = value; } }
            public InstructionHandler C_F_S     { set { _parent.ops_cop1_s[48]  = value; } }
            public InstructionHandler C_LE_D    { set { _parent.ops_cop1_d[62]  = value; } }
            public InstructionHandler C_LE_S    { set { _parent.ops_cop1_s[62]  = value; } }
            public InstructionHandler C_LT_D    { set { _parent.ops_cop1_d[60]  = value; } }
            public InstructionHandler C_LT_S    { set { _parent.ops_cop1_s[60]  = value; } }
            public InstructionHandler C_NGE_D   { set { _parent.ops_cop1_d[61]  = value; } }
            public InstructionHandler C_NGE_S   { set { _parent.ops_cop1_s[61]  = value; } }
            public InstructionHandler C_NGL_D   { set { _parent.ops_cop1_d[59]  = value; } }
            public InstructionHandler C_NGL_S   { set { _parent.ops_cop1_s[59]  = value; } }
            public InstructionHandler C_NGLE_D  { set { _parent.ops_cop1_d[57]  = value; } }
            public InstructionHandler C_NGLE_S  { set { _parent.ops_cop1_s[57]  = value; } }
            public InstructionHandler C_NGT_D   { set { _parent.ops_cop1_d[63]  = value; } }
            public InstructionHandler C_NGT_S   { set { _parent.ops_cop1_s[63]  = value; } }
            public InstructionHandler C_OLE_D   { set { _parent.ops_cop1_d[54]  = value; } }
            public InstructionHandler C_OLE_S   { set { _parent.ops_cop1_s[54]  = value; } }
            public InstructionHandler C_OLT_D   { set { _parent.ops_cop1_d[52]  = value; } }
            public InstructionHandler C_OLT_S   { set { _parent.ops_cop1_s[52]  = value; } }
            public InstructionHandler C_SEQ_D   { set { _parent.ops_cop1_d[58]  = value; } }
            public InstructionHandler C_SEQ_S   { set { _parent.ops_cop1_s[58]  = value; } }
            public InstructionHandler C_SF_D    { set { _parent.ops_cop1_d[56]  = value; } }
            public InstructionHandler C_SF_S    { set { _parent.ops_cop1_s[56]  = value; } }
            public InstructionHandler C_UEQ_D   { set { _parent.ops_cop1_d[51]  = value; } }
            public InstructionHandler C_UEQ_S   { set { _parent.ops_cop1_s[51]  = value; } }
            public InstructionHandler C_ULE_D   { set { _parent.ops_cop1_d[55]  = value; } }
            public InstructionHandler C_ULE_S   { set { _parent.ops_cop1_s[55]  = value; } }
            public InstructionHandler C_ULT_D   { set { _parent.ops_cop1_d[53]  = value; } }
            public InstructionHandler C_ULT_S   { set { _parent.ops_cop1_s[53]  = value; } }
            public InstructionHandler C_UN_D    { set { _parent.ops_cop1_d[49]  = value; } }
            public InstructionHandler C_UN_S    { set { _parent.ops_cop1_s[49]  = value; } }
            public InstructionHandler CACHE     { set { _parent.ops_main[47]    = value; } }
            public InstructionHandler CEIL_L_D  { set { _parent.ops_cop1_d[10]  = value; } }
            public InstructionHandler CEIL_L_S  { set { _parent.ops_cop1_s[10]  = value; } }
            public InstructionHandler CEIL_W_D  { set { _parent.ops_cop1_d[14]  = value; } }
            public InstructionHandler CEIL_W_S  { set { _parent.ops_cop1_s[14]  = value; } }
            public InstructionHandler CFC1      { set { _parent.ops_cop1[2]     = value; } }
            public InstructionHandler CTC1      { set { _parent.ops_cop1[6]     = value; } }
            public InstructionHandler CVT_D_L   { set { _parent.ops_cop1_l[33]  = value; } }
            public InstructionHandler CVT_D_S   { set { _parent.ops_cop1_s[33]  = value; } }
            public InstructionHandler CVT_D_W   { set { _parent.ops_cop1_w[33]  = value; } }
            public InstructionHandler CVT_L_D   { set { _parent.ops_cop1_d[37]  = value; } }
            public InstructionHandler CVT_L_S   { set { _parent.ops_cop1_s[37]  = value; } }
            public InstructionHandler CVT_S_D   { set { _parent.ops_cop1_d[32]  = value; } }
            public InstructionHandler CVT_S_L   { set { _parent.ops_cop1_l[32]  = value; } }
            public InstructionHandler CVT_S_W   { set { _parent.ops_cop1_w[32]  = value; } }
            public InstructionHandler CVT_W_D   { set { _parent.ops_cop1_d[36]  = value; } }
            public InstructionHandler CVT_W_S   { set { _parent.ops_cop1_s[36]  = value; } }
            public InstructionHandler DADD      { set { _parent.ops_special[44] = value; } }
            public InstructionHandler DADDI     { set { _parent.ops_main[24]    = value; } }
            public InstructionHandler DADDIU    { set { _parent.ops_main[25]    = value; } }
            public InstructionHandler DADDU     { set { _parent.ops_special[45] = value; } }
            public InstructionHandler DDIV      { set { _parent.ops_special[30] = value; } }
            public InstructionHandler DDIVU     { set { _parent.ops_special[31] = value; } }
            public InstructionHandler DIV       { set { _parent.ops_special[26] = value; } }
            public InstructionHandler DIV_D     { set { _parent.ops_cop1_d[3]   = value; } }
            public InstructionHandler DIV_S     { set { _parent.ops_cop1_s[3]   = value; } }
            public InstructionHandler DIVU      { set { _parent.ops_special[27] = value; } }
            public InstructionHandler DMFC1     { set { _parent.ops_cop1[1]     = value; } }
            public InstructionHandler DMTC1     { set { _parent.ops_cop1[5]     = value; } }
            public InstructionHandler DMULT     { set { _parent.ops_special[28] = value; } }
            public InstructionHandler DMULTU    { set { _parent.ops_special[29] = value; } }
            public InstructionHandler DSLL      { set { _parent.ops_special[56] = value; } }
            public InstructionHandler DSLL32    { set { _parent.ops_special[60] = value; } }
            public InstructionHandler DSLLV     { set { _parent.ops_special[20] = value; } }
            public InstructionHandler DSRA      { set { _parent.ops_special[59] = value; } }
            public InstructionHandler DSRA32    { set { _parent.ops_special[63] = value; } }
            public InstructionHandler DSRAV     { set { _parent.ops_special[23] = value; } }
            public InstructionHandler DSRL      { set { _parent.ops_special[58] = value; } }
            public InstructionHandler DSRL32    { set { _parent.ops_special[62] = value; } }
            public InstructionHandler DSRLV     { set { _parent.ops_special[22] = value; } }
            public InstructionHandler DSUB      { set { _parent.ops_special[46] = value; } }
            public InstructionHandler DSUBU     { set { _parent.ops_special[47] = value; } }
            public InstructionHandler ERET      { set { _parent.ops_tlb[24]     = value; } }
            public InstructionHandler FLOOR_L_D { set { _parent.ops_cop1_d[11]  = value; } }
            public InstructionHandler FLOOR_L_S { set { _parent.ops_cop1_s[11]  = value; } }
            public InstructionHandler FLOOR_W_D { set { _parent.ops_cop1_d[15]  = value; } }
            public InstructionHandler FLOOR_W_S { set { _parent.ops_cop1_s[15]  = value; } }
            public InstructionHandler J         { set { _parent.ops_main[2]     = value; } }
            public InstructionHandler JAL       { set { _parent.ops_main[3]     = value; } }
            public InstructionHandler JALR      { set { _parent.ops_special[9]  = value; } }
            public InstructionHandler JR        { set { _parent.ops_special[8]  = value; } }
            public InstructionHandler LB        { set { _parent.ops_main[32]    = value; } }
            public InstructionHandler LBU       { set { _parent.ops_main[36]    = value; } }
            public InstructionHandler LD        { set { _parent.ops_main[55]    = value; } }
            public InstructionHandler LDC1      { set { _parent.ops_main[53]    = value; } }
            public InstructionHandler LDL       { set { _parent.ops_main[26]    = value; } }
            public InstructionHandler LDR       { set { _parent.ops_main[27]    = value; } }
            public InstructionHandler LH        { set { _parent.ops_main[33]    = value; } }
            public InstructionHandler LHU       { set { _parent.ops_main[37]    = value; } }
            public InstructionHandler LL        { set { _parent.ops_main[48]    = value; } }
            public InstructionHandler LUI       { set { _parent.ops_main[15]    = value; } }
            public InstructionHandler LW        { set { _parent.ops_main[35]    = value; } }
            public InstructionHandler LWC1      { set { _parent.ops_main[49]    = value; } }
            public InstructionHandler LWL       { set { _parent.ops_main[34]    = value; } }
            public InstructionHandler LWR       { set { _parent.ops_main[38]    = value; } }
            public InstructionHandler LWU       { set { _parent.ops_main[39]    = value; } }
            public InstructionHandler MFC0      { set { _parent.ops_cop0[0]     = value; } }
            public InstructionHandler MFC1      { set { _parent.ops_cop1[0]     = value; } }
            public InstructionHandler MFHI      { set { _parent.ops_special[16] = value; } }
            public InstructionHandler MFLO      { set { _parent.ops_special[18] = value; } }
            public InstructionHandler MOV_D     { set { _parent.ops_cop1_d[6]   = value; } }
            public InstructionHandler MOV_S     { set { _parent.ops_cop1_s[6]   = value; } }
            public InstructionHandler MTC0      { set { _parent.ops_cop0[4]     = value; } }
            public InstructionHandler MTC1      { set { _parent.ops_cop1[4]     = value; } }
            public InstructionHandler MTHI      { set { _parent.ops_special[17] = value; } }
            public InstructionHandler MTLO      { set { _parent.ops_special[19] = value; } }
            public InstructionHandler MUL_D     { set { _parent.ops_cop1_d[2]   = value; } }
            public InstructionHandler MUL_S     { set { _parent.ops_cop1_s[2]   = value; } }
            public InstructionHandler MULT      { set { _parent.ops_special[24] = value; } }
            public InstructionHandler MULTU     { set { _parent.ops_special[25] = value; } }
            public InstructionHandler NEG_D     { set { _parent.ops_cop1_d[7]   = value; } }
            public InstructionHandler NEG_S     { set { _parent.ops_cop1_s[7]   = value; } }
            public InstructionHandler NOR       { set { _parent.ops_special[39] = value; } }
            public InstructionHandler OR        { set { _parent.ops_special[37] = value; } }
            public InstructionHandler ORI       { set { _parent.ops_main[13]    = value; } }
            public InstructionHandler ROUND_L_D { set { _parent.ops_cop1_d[8]   = value; } }
            public InstructionHandler ROUND_L_S { set { _parent.ops_cop1_s[8]   = value; } }
            public InstructionHandler ROUND_W_D { set { _parent.ops_cop1_d[12]  = value; } }
            public InstructionHandler ROUND_W_S { set { _parent.ops_cop1_s[12]  = value; } }
            public InstructionHandler SB        { set { _parent.ops_main[40]    = value; } }
            public InstructionHandler SC        { set { _parent.ops_main[56]    = value; } }
            public InstructionHandler SD        { set { _parent.ops_main[63]    = value; } }
            public InstructionHandler SDC1      { set { _parent.ops_main[61]    = value; } }
            public InstructionHandler SDL       { set { _parent.ops_main[44]    = value; } }
            public InstructionHandler SDR       { set { _parent.ops_main[45]    = value; } }
            public InstructionHandler SH        { set { _parent.ops_main[41]    = value; } }
            public InstructionHandler SLL       { set { _parent.ops_special[0]  = value; } }
            public InstructionHandler SLLV      { set { _parent.ops_special[4]  = value; } }
            public InstructionHandler SLT       { set { _parent.ops_special[42] = value; } }
            public InstructionHandler SLTI      { set { _parent.ops_main[10]    = value; } }
            public InstructionHandler SLTIU     { set { _parent.ops_main[11]    = value; } }
            public InstructionHandler SLTU      { set { _parent.ops_special[43] = value; } }
            public InstructionHandler SQRT_D    { set { _parent.ops_cop1_d[4]   = value; } }
            public InstructionHandler SQRT_S    { set { _parent.ops_cop1_s[4]   = value; } }
            public InstructionHandler SRA       { set { _parent.ops_special[3]  = value; } }
            public InstructionHandler SRAV      { set { _parent.ops_special[7]  = value; } }
            public InstructionHandler SRL       { set { _parent.ops_special[2]  = value; } }
            public InstructionHandler SRLV      { set { _parent.ops_special[6]  = value; } }
            public InstructionHandler SUB       { set { _parent.ops_special[34] = value; } }
            public InstructionHandler SUB_D     { set { _parent.ops_cop1_d[1]   = value; } }
            public InstructionHandler SUB_S     { set { _parent.ops_cop1_s[1]   = value; } }
            public InstructionHandler SUBU      { set { _parent.ops_special[35] = value; } }
            public InstructionHandler SW        { set { _parent.ops_main[43]    = value; } }
            public InstructionHandler SWC1      { set { _parent.ops_main[57]    = value; } }
            public InstructionHandler SWL       { set { _parent.ops_main[42]    = value; } }
            public InstructionHandler SWR       { set { _parent.ops_main[46]    = value; } }
            public InstructionHandler SYNC      { set { _parent.ops_special[15] = value; } }
            public InstructionHandler SYSCALL   { set { _parent.ops_special[12] = value; } }
            public InstructionHandler TEQ       { set { _parent.ops_special[52] = value; } }
            public InstructionHandler TLBP      { set { _parent.ops_tlb[8]      = value; } }
            public InstructionHandler TLBR      { set { _parent.ops_tlb[1]      = value; } }
            public InstructionHandler TLBWI     { set { _parent.ops_tlb[2]      = value; } }
            public InstructionHandler TLBWR     { set { _parent.ops_tlb[6]      = value; } }
            public InstructionHandler TRUNC_L_D { set { _parent.ops_cop1_d[9]   = value; } }
            public InstructionHandler TRUNC_L_S { set { _parent.ops_cop1_s[9]   = value; } }
            public InstructionHandler TRUNC_W_D { set { _parent.ops_cop1_d[13]  = value; } }
            public InstructionHandler TRUNC_W_S { set { _parent.ops_cop1_s[13]  = value; } }
            public InstructionHandler XOR       { set { _parent.ops_special[38] = value; } }
            public InstructionHandler XORI      { set { _parent.ops_main[14]    = value; } }

            public InstructionHandler LLD
            {
                set
                {
                    _parent.ops_main[52] = value;
                }
            }

            public InstructionHandler DMFC0
            {
                set
                {
                    _parent.ops_cop0[1] = value;
                }
            }

            public InstructionHandler SCD
            {
                set
                {
                    _parent.ops_main[60] = value;
                }
            }

            public InstructionHandler TEQI
            {
                set
                {
                    _parent.ops_regimm[12] = value;
                }
            }

            public InstructionHandler TGE
            {
                set
                {
                    _parent.ops_special[48] = value;
                }
            }

            public InstructionHandler TGEI
            {
                set
                {
                    _parent.ops_regimm[8] = value;
                }
            }

            public InstructionHandler TGEIU
            {
                set
                {
                    _parent.ops_regimm[9] = value;
                }
            }

            public InstructionHandler TGEU
            {
                set
                {
                    _parent.ops_special[49] = value;
                }
            }

            public InstructionHandler TLT
            {
                set
                {
                    _parent.ops_special[50] = value;
                }
            }

            public InstructionHandler TLTI
            {
                set
                {
                    _parent.ops_regimm[10] = value;
                }
            }

            public InstructionHandler TLTIU
            {
                set
                {
                    _parent.ops_regimm[11] = value;
                }
            }

            public InstructionHandler TLTU
            {
                set
                {
                    _parent.ops_special[51] = value;
                }
            }

            public InstructionHandler TNE
            {
                set
                {
                    _parent.ops_special[54] = value;
                }
            }

            public InstructionHandler TNEI
            {
                set
                {
                    _parent.ops_regimm[14] = value;
                }
            }
        }
        private class MetaHandlersImpl : IMetaHandlers
        {
            private readonly IHandlers _handlers;

            public MetaHandlersImpl(IHandlers handlers)
            {
                _handlers = handlers;
            }

            public InstructionHandler AllValid
            {
                set
                {
                    _handlers.ABS_D     = value;
                    _handlers.ABS_S     = value;
                    _handlers.ADD       = value;
                    _handlers.ADD_D     = value;
                    _handlers.ADD_S     = value;
                    _handlers.ADDI      = value;
                    _handlers.ADDIU     = value;
                    _handlers.ADDU      = value;
                    _handlers.AND       = value;
                    _handlers.ANDI      = value;
                    _handlers.BC1F      = value;
                    _handlers.BC1FL     = value;
                    _handlers.BC1T      = value;
                    _handlers.BC1TL     = value;
                    _handlers.BEQ       = value;
                    _handlers.BEQL      = value;
                    _handlers.BGEZ      = value;
                    _handlers.BGEZAL    = value;
                    _handlers.BGEZALL   = value;
                    _handlers.BGEZL     = value;
                    _handlers.BGTZ      = value;
                    _handlers.BGTZL     = value;
                    _handlers.BLEZ      = value;
                    _handlers.BLEZL     = value;
                    _handlers.BLTZ      = value;
                    _handlers.BLTZAL    = value;
                    _handlers.BLTZALL   = value;
                    _handlers.BLTZL     = value;
                    _handlers.BNE       = value;
                    _handlers.BNEL      = value;
                    _handlers.BREAK     = value;
                    _handlers.C_EQ_D    = value;
                    _handlers.C_EQ_S    = value;
                    _handlers.C_F_D     = value;
                    _handlers.C_F_S     = value;
                    _handlers.C_LE_D    = value;
                    _handlers.C_LE_S    = value;
                    _handlers.C_LT_D    = value;
                    _handlers.C_LT_S    = value;
                    _handlers.C_NGE_D   = value;
                    _handlers.C_NGE_S   = value;
                    _handlers.C_NGL_D   = value;
                    _handlers.C_NGL_S   = value;
                    _handlers.C_NGLE_D  = value;
                    _handlers.C_NGLE_S  = value;
                    _handlers.C_NGT_D   = value;
                    _handlers.C_NGT_S   = value;
                    _handlers.C_OLE_D   = value;
                    _handlers.C_OLE_S   = value;
                    _handlers.C_OLT_D   = value;
                    _handlers.C_OLT_S   = value;
                    _handlers.C_SEQ_D   = value;
                    _handlers.C_SEQ_S   = value;
                    _handlers.C_SF_D    = value;
                    _handlers.C_SF_S    = value;
                    _handlers.C_UEQ_D   = value;
                    _handlers.C_UEQ_S   = value;
                    _handlers.C_ULE_D   = value;
                    _handlers.C_ULE_S   = value;
                    _handlers.C_ULT_D   = value;
                    _handlers.C_ULT_S   = value;
                    _handlers.C_UN_D    = value;
                    _handlers.C_UN_S    = value;
                    _handlers.CACHE     = value;
                    _handlers.CEIL_L_D  = value;
                    _handlers.CEIL_L_S  = value;
                    _handlers.CEIL_W_D  = value;
                    _handlers.CEIL_W_S  = value;
                    _handlers.CFC1      = value;
                    _handlers.CTC1      = value;
                    _handlers.CVT_D_L   = value;
                    _handlers.CVT_D_S   = value;
                    _handlers.CVT_D_W   = value;
                    _handlers.CVT_L_D   = value;
                    _handlers.CVT_L_S   = value;
                    _handlers.CVT_S_D   = value;
                    _handlers.CVT_S_L   = value;
                    _handlers.CVT_S_W   = value;
                    _handlers.CVT_W_D   = value;
                    _handlers.CVT_W_S   = value;
                    _handlers.DADD      = value;
                    _handlers.DADDI     = value;
                    _handlers.DADDIU    = value;
                    _handlers.DADDU     = value;
                    _handlers.DDIV      = value;
                    _handlers.DDIVU     = value;
                    _handlers.DIV       = value;
                    _handlers.DIV_D     = value;
                    _handlers.DIV_S     = value;
                    _handlers.DIVU      = value;
                    _handlers.DMFC1     = value;
                    _handlers.DMTC1     = value;
                    _handlers.DMULT     = value;
                    _handlers.DMULTU    = value;
                    _handlers.DSLL      = value;
                    _handlers.DSLL32    = value;
                    _handlers.DSLLV     = value;
                    _handlers.DSRA      = value;
                    _handlers.DSRA32    = value;
                    _handlers.DSRAV     = value;
                    _handlers.DSRL      = value;
                    _handlers.DSRL32    = value;
                    _handlers.DSRLV     = value;
                    _handlers.DSUB      = value;
                    _handlers.DSUBU     = value;
                    _handlers.ERET      = value;
                    _handlers.FLOOR_L_D = value;
                    _handlers.FLOOR_L_S = value;
                    _handlers.FLOOR_W_D = value;
                    _handlers.FLOOR_W_S = value;
                    _handlers.J         = value;
                    _handlers.JAL       = value;
                    _handlers.JALR      = value;
                    _handlers.JR        = value;
                    _handlers.LB        = value;
                    _handlers.LBU       = value;
                    _handlers.LD        = value;
                    _handlers.LDC1      = value;
                    _handlers.LDL       = value;
                    _handlers.LDR       = value;
                    _handlers.LH        = value;
                    _handlers.LHU       = value;
                    _handlers.LL        = value;
                    _handlers.LUI       = value;
                    _handlers.LW        = value;
                    _handlers.LWC1      = value;
                    _handlers.LWL       = value;
                    _handlers.LWR       = value;
                    _handlers.LWU       = value;
                    _handlers.MFC0      = value;
                    _handlers.MFC1      = value;
                    _handlers.MFHI      = value;
                    _handlers.MFLO      = value;
                    _handlers.MOV_D     = value;
                    _handlers.MOV_S     = value;
                    _handlers.MTC0      = value;
                    _handlers.MTC1      = value;
                    _handlers.MTHI      = value;
                    _handlers.MTLO      = value;
                    _handlers.MUL_D     = value;
                    _handlers.MUL_S     = value;
                    _handlers.MULT      = value;
                    _handlers.MULTU     = value;
                    _handlers.NEG_D     = value;
                    _handlers.NEG_S     = value;
                    _handlers.NOR       = value;
                    _handlers.OR        = value;
                    _handlers.ORI       = value;
                    _handlers.ROUND_L_D = value;
                    _handlers.ROUND_L_S = value;
                    _handlers.ROUND_W_D = value;
                    _handlers.ROUND_W_S = value;
                    _handlers.SB        = value;
                    _handlers.SC        = value;
                    _handlers.SD        = value;
                    _handlers.SDC1      = value;
                    _handlers.SDL       = value;
                    _handlers.SDR       = value;
                    _handlers.SH        = value;
                    _handlers.SLL       = value;
                    _handlers.SLLV      = value;
                    _handlers.SLT       = value;
                    _handlers.SLTI      = value;
                    _handlers.SLTIU     = value;
                    _handlers.SLTU      = value;
                    _handlers.SQRT_D    = value;
                    _handlers.SQRT_S    = value;
                    _handlers.SRA       = value;
                    _handlers.SRAV      = value;
                    _handlers.SRL       = value;
                    _handlers.SRLV      = value;
                    _handlers.SUB       = value;
                    _handlers.SUB_D     = value;
                    _handlers.SUB_S     = value;
                    _handlers.SUBU      = value;
                    _handlers.SW        = value;
                    _handlers.SWC1      = value;
                    _handlers.SWL       = value;
                    _handlers.SWR       = value;
                    _handlers.SYNC      = value;
                    _handlers.SYSCALL   = value;
                    _handlers.TEQ       = value;
                    _handlers.TLBP      = value;
                    _handlers.TLBR      = value;
                    _handlers.TLBWI     = value;
                    _handlers.TLBWR     = value;
                    _handlers.TRUNC_L_D = value;
                    _handlers.TRUNC_L_S = value;
                    _handlers.TRUNC_W_D = value;
                    _handlers.TRUNC_W_S = value;
                    _handlers.XOR       = value;
                    _handlers.XORI      = value;

                    _handlers.LLD       = value;
                    _handlers.DMFC0     = value;
                    _handlers.SCD       = value;
                    _handlers.TEQI      = value;
                    _handlers.TGE       = value;
                    _handlers.TGEI      = value;
                    _handlers.TGEIU     = value;
                    _handlers.TGEU      = value;
                    _handlers.TLT       = value;
                    _handlers.TLTI      = value;
                    _handlers.TLTIU     = value;
                    _handlers.TLTU      = value;
                    _handlers.TNE       = value;
                    _handlers.TNEI      = value;
                }
            }

            public InstructionHandler Branch
            {
                set
                {
                    _handlers.BC1F  = value;
                    _handlers.BC1FL = value;
                    _handlers.BC1T  = value;
                    _handlers.BC1TL = value;
                    _handlers.BEQ   = value;
                    _handlers.BEQL  = value;
                    _handlers.BGEZ  = value;
                    _handlers.BGEZL = value;
                    _handlers.BGTZ  = value;
                    _handlers.BGTZL = value;
                    _handlers.BLEZ  = value;
                    _handlers.BLEZL = value;
                    _handlers.BLTZ  = value;
                    _handlers.BLTZL = value;
                    _handlers.BNE   = value;
                    _handlers.BNEL  = value;
                }
            }

            public InstructionHandler Load
            {
                set
                {
                    // CACHE has a similar instruction format but is likely not used in the same way.
                    //_handlers.CACHE = value;
                    _handlers.LB   = value;
                    _handlers.LBU  = value;
                    _handlers.LD   = value;
                    _handlers.LDC1 = value;
                    _handlers.LDL  = value;
                    _handlers.LDR  = value;
                    _handlers.LH   = value;
                    _handlers.LHU  = value;
                    _handlers.LL   = value;
                    _handlers.LLD  = value;
                    _handlers.LW   = value;
                    _handlers.LWC1 = value;
                    _handlers.LWL  = value;
                    _handlers.LWR  = value;
                    _handlers.LWU  = value;
                }
            }

            public InstructionHandler Store
            {
                set
                {
                    _handlers.SB   = value;
                    _handlers.SC   = value;
                    _handlers.SCD  = value;
                    _handlers.SD   = value;
                    _handlers.SDC1 = value;
                    _handlers.SDL  = value;
                    _handlers.SDR  = value;
                    _handlers.SH   = value;
                    _handlers.SW   = value;
                    _handlers.SWC1 = value;
                    _handlers.SWL  = value;
                    _handlers.SWR  = value;
                }
            }

            public InstructionHandler LoadStore
            {
                set
                {
                    Load = value;
                    Store = value;
                }
            }
        }

        public IHandlers Handlers { get; }
        public IMetaHandlers MetaHandlers { get; }


        private readonly InstructionHandler _defaultHandler;

        public Interpreter(Func<T> dfltHandler)
            : this((pc, insn) => dfltHandler())
        {

        }

        public Interpreter(InstructionHandler dfltHandler = null)
        {
            _defaultHandler = dfltHandler;

            SetUpHandlers();

            Handlers = new HandlersImpl(this);
            MetaHandlers = new MetaHandlersImpl(Handlers);
        }

        public T Execute(UInt32 pc, Instruction insn) =>
            ops_main[insn.Opcode](pc, insn);

        private InstructionHandler[] ops_main;
        private InstructionHandler[] ops_special;
        private InstructionHandler[] ops_regimm;
        private InstructionHandler[] ops_tlb;
        private InstructionHandler[] ops_cop0;
        private InstructionHandler[] ops_cop1;
        private InstructionHandler[] ops_cop1_bc;
        private InstructionHandler[] ops_cop1_s;
        private InstructionHandler[] ops_cop1_d;
        private InstructionHandler[] ops_cop1_w;
        private InstructionHandler[] ops_cop1_l;


        private void SetUpHandlers()
        {
            ops_main = new InstructionHandler[64]
            {
                _S   , _R   , null, null, null, null, null, null,
                null , null , null, null, null, null, null, null,
                _COP0, _COP1, null, null, null, null, null, null,
                null , null , null, null, null, null, null, null,
                null , null , null, null, null, null, null, null,
                null , null , null, null, null, null, null, null,
                null , null , null, null, null, null, null, null,
                null , null , null, null, null, null, null, null
            };

            ops_cop1 = new InstructionHandler[32]
            {
                null, null,  null, null, null, null,  null, null,
                _BC , null , null, null, null, null , null, null,
                _FS , _FD  , null, null, _FW , _FL  , null, null,
                null, null , null, null, null, null , null, null
            };

            ops_cop0 = new InstructionHandler[32]
            {
                null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null,
                _TLB, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null
            };

            ops_special = new InstructionHandler[64];
            ops_regimm  = new InstructionHandler[32];
            ops_cop1_w  = new InstructionHandler[64];
            ops_cop1_l  = new InstructionHandler[64];
            ops_cop1_s  = new InstructionHandler[64];
            ops_cop1_d  = new InstructionHandler[64];
            ops_cop1_bc = new InstructionHandler[4];
            ops_tlb     = new InstructionHandler[64];

            var all = new[] { ops_main, ops_cop1, ops_cop0, ops_special, ops_regimm, ops_cop1_w, ops_cop1_l, ops_cop1_s, ops_cop1_d, ops_cop1_bc, ops_tlb };

            foreach( var grp in all )
            {
                for( var i = 0; i < grp.Length; i++ )
                {
                    if (grp[i] != null)
                        continue;

                    grp[i] = _defaultHandler ?? ((pc, insn) =>
                    {
                        throw new UnhandledInstructionException("An unhandled instruction was encountered!", pc, insn);
                    });
                }
            }
        }


        private T _S(UInt32 pc, Instruction insn) => 
            ops_special[insn.Word & 0x3F](pc, insn); 
        private T _R(UInt32 pc, Instruction insn) => 
            ops_regimm[(insn.Word >> 16) & 0x1F](pc, insn); 
        private T _FS(UInt32 pc, Instruction insn) => 
            ops_cop1_s[insn.Func](pc, insn); 
        private T _FD(UInt32 pc, Instruction insn) => 
            ops_cop1_d[insn.Func](pc, insn); 
        private T _FW(UInt32 pc, Instruction insn) => 
            ops_cop1_w[insn.Func](pc, insn); 
        private T _FL(UInt32 pc, Instruction insn) => 
            ops_cop1_l[insn.Func](pc, insn); 
        private T _BC(UInt32 pc, Instruction insn) => 
            ops_cop1_bc[(insn.Word >> 16) & 3](pc, insn); 
        private T _COP0(UInt32 pc, Instruction insn) => 
            ops_cop0[(insn.Word >> 21) & 0x1F](pc, insn); 
        private T _COP1(UInt32 pc, Instruction insn) => 
            ops_cop1[(insn.Word >> 21) & 0x1F](pc, insn); 
        private T _TLB(UInt32 pc, Instruction insn) => 
            ops_tlb[insn.Func](pc, insn); 
    }
}
