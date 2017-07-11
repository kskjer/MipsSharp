using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Mips
{
    public class Analyzer
    {
        public abstract class Value
        {
            public static Value Gpr(uint gprReg) => new GprValue { Reg = gprReg };
            public static Value Fpr(uint fprReg) => new FprValue { Reg = fprReg };

            public static Value Memory(short offset, uint baseReg) => null;

            public static Value Constant(int value) => null;
            public static Value Constant(uint value) => null;

            public static Value Assign(Value target, Value value) => 
                new AssignmentValue { Target = target, Value = value };

            public static Value operator +(Value left, Value right) => 
                new MathValue { Left = left, Right = right, Operator = Operators.Add };
            public static Value operator -(Value left, Value right) =>
                new MathValue { Left = left, Right = right, Operator = Operators.Subtract };
            public static Value operator *(Value left, Value right) =>
                new MathValue { Left = left, Right = right, Operator = Operators.Multiply };
            public static Value operator /(Value left, Value right) =>
                new MathValue { Left = left, Right = right, Operator = Operators.Divide };
            public static Value operator |(Value left, Value right) =>
                new MathValue { Left = left, Right = right, Operator = Operators.Or };

            public int? DestinationGpr =>
                (int)((this as AssignmentValue)?.Target as GprValue)?.Reg;


            public enum Operators
            {
                Add,
                Subtract,
                Multiply,
                Divide,
                ShiftLeft,
                ShiftRight,
                Or
            }

            public class GprValue : Value
            {
                public uint Reg { get; set; }

                public override int GetHashCode() =>
                    CombineHashCode(GetType(), Reg);
            }

            public class FprValue : Value
            {
                public uint Reg { get; set; }
            }

            public class AssignmentValue : Value
            {
                public Value Target { get; set; }
                public Value Value { get; set; }
            }

            public class MathValue : Value
            {
                public Operators Operator { get; set; }
                public Value Left { get; set; }
                public Value Right { get; set; }

                public override void UpdateReferences(Analyzer a)
                {
                    //a.NoteReference(Left, this);
                    //a.NoteRefer
                }
            }

            public Value Previous { get; set; }
            public Value Next { get; set; }
#warning make abstract
            public virtual void UpdateReferences(Analyzer a) { }

            protected static int CombineHashCode(params object[] objs) =>
                objs.Aggregate(17, (hash, obj) => unchecked (hash * 31 + obj.GetHashCode()));
        }

        private void SetupInterp(Interpreter<Value> interp)
        {
            interp.Handlers.LUI = (p, i) =>
                Value.Assign(Value.Gpr(i.GprRt), Value.Constant(i.Immediate << 16));
            interp.Handlers.ADDIU = (p, i) => 
                Value.Assign(Value.Gpr(i.GprRt), Value.Gpr(i.GprRs) + Value.Constant(i.ImmediateSigned));
            interp.Handlers.ORI = (p, i) =>
                Value.Assign(Value.Gpr(i.GprRt), Value.Gpr(i.GprRs) | Value.Constant(i.Immediate));

            interp.Handlers.SW = (p, i) =>
                Value.Assign(Value.Memory(i.ImmediateSigned, i.GprBase), Value.Gpr(i.GprRt));
            interp.Handlers.LW = (p, i) =>
                Value.Assign(Value.Gpr(i.GprRt), Value.Memory(i.ImmediateSigned, i.GprBase));
        }

        private readonly Interpreter<Value> _interp =
            new Interpreter<Value>(() => null);

        public Analyzer()
        {
            SetupInterp(_interp);
        }

        public void InterpretFunction(IEnumerable<InstructionWithPc> insns)
        {
            var values = new Dictionary<Value, Value>();

            foreach (var i in insns)
            {
                var rval = _interp.Execute(i.Pc, i.Instruction);

                var assignment = rval as Value.AssignmentValue;

                if( assignment != null)
                {
                    values.Add(assignment.Target, assignment.Value);
                }
            }
        }

        public Value Analyze(Instruction insn) =>
            _interp.Execute(0, insn);
    }
}
