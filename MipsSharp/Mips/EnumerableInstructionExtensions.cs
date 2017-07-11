using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MipsSharp.Mips
{
    public static class EnumerableInstructionExtensions
    {
        public static IEnumerable<Instruction> ToInstructions(this IEnumerable<byte> bytes)
        {
            UInt32 word = 0;
            int index = 0;

            foreach( var b in bytes )
            {
                word |= (uint)b << (24 - (index % 4) * 8);
                index++;


                if (index > 0 && (index % 4) == 0)
                {
                    yield return new Instruction(word);
                    word = 0;
                }
            }
        }

        public class HiLoPair
        {
            public UInt32 HiAddress { get; }
            public UInt32 LoAddress { get; }
            public UInt32 SymbolAddress { get; }

            public HiLoPair(UInt32 hi, UInt32 lo, UInt32 symAddr)
            {
                HiAddress = hi;
                LoAddress = lo;
                SymbolAddress = symAddr;
            }
        }

        public static IEnumerable<HiLoPair> DiscoverHiLoPairs(this IEnumerable<Instruction> insns, UInt32 startPc)
        {
            var index = new Dictionary<uint, HiLoPair>();

            var interp = new Interpreter<HiLoPair>(
                () =>
                {
                    index.Clear();

                    return null;
                }
            );

            Action<UInt32, uint, uint> noteHi = (pc, reg, val) =>
            {
                index[reg] = new HiLoPair(pc, 0, val << 16);
            };

            Func<UInt32, uint, short, HiLoPair> noteLo = (pc, reg, val) =>
            {
                if (!index.ContainsKey(reg))
                    return null;

                var rval = new HiLoPair(
                    index[reg].HiAddress,
                    pc,
                    (UInt32)(index[reg].SymbolAddress + val)
                );

                index.Remove(reg);

                return rval;
            };

            interp.MetaHandlers.AllValid = (p, i) => null;

            interp.Handlers.LUI = (p, i) =>
            {
                noteHi(p, i.GprRt, i.Immediate);

                return null;
            };
            interp.Handlers.ADDIU = (p, i) =>
            {
                if (i.GprRt == i.GprRs)
                    return noteLo(p, i.GprRt, i.ImmediateSigned);

                return null;
            };
            interp.MetaHandlers.LoadStore = (p, i) =>
            {
                return noteLo(p, i.GprBase, i.ImmediateSigned);
            };

            foreach(var insn in insns)
            {
                var result = interp.Execute(startPc, insn);

                if (result != null)
                    yield return result;

                startPc += 4;
            }
        }
        
        private interface IDiscoveredFunction
        {
            UInt32 Size { set; }
            void AddInstruction(InstructionWithPc iwp);
        }

        public class DiscoveredFunction : IDiscoveredFunction
        {
            public UInt32 StartAddress { get; }
            public UInt32 EndAddress => StartAddress + Size;
            public UInt32 Size { get; set; }
            public IReadOnlyList<InstructionWithPc> Instructions => _instructions;

            private readonly List<InstructionWithPc> _instructions =
                new List<InstructionWithPc>();

            uint IDiscoveredFunction.Size
            {
                set
                {
                    this.Size = value;
                }
            }

            void IDiscoveredFunction.AddInstruction(InstructionWithPc iwp) =>
                _instructions.Add(iwp);

            public override string ToString() =>
                string.Format("0x{0:X8} - 0x{1:X8} ({2} instructions)", StartAddress, EndAddress, Size / 4);


            public DiscoveredFunction(UInt32 start, UInt32 size)
            {
                StartAddress = start;
                Size = size;
            }
        }

        public static IEnumerable<DiscoveredFunction> DiscoverFunctions(this IEnumerable<InstructionWithPc> insns)
        {
            var e = insns.GetEnumerator();
            if (e.MoveNext())
            {
                var first = e.Current;

                return DiscoverFunctions(new[] { first }.Concat(e.ToIEnumerable()), first.Pc);
            }

            return new DiscoveredFunction[0];
        }


        public static IEnumerable<DiscoveredFunction> DiscoverFunctions<T>(this IEnumerable<T> insns, UInt32 startPc)
            where T : struct, IHasInstruction
        {
            var interp = new Interpreter<bool>(() => false);

            DiscoveredFunction current = null;
            int currentExtent = 0;
            int maxExtent = 0;
            bool inFInalReturnDelaySlot = false;

            var preamble = interp.MetaHandlers.AllValid = (p, i) =>
            {
                current = current ?? new DiscoveredFunction(p, 0);
                currentExtent += 4;

                if(inFInalReturnDelaySlot)
                {
                    ((IDiscoveredFunction)current).Size = (UInt32)currentExtent;

                    inFInalReturnDelaySlot = false;
                }

                return true;
            };

            Func<UInt32, int, bool> noteMaxExtent = (pc, relOffset) =>
            {
                if (current == null)
                    return true;

                if (relOffset < 0)
                    return false;

                maxExtent = Math.Max(maxExtent, relOffset);

                return true;
            };

            interp.Handlers.JR = (p, i) =>
            {
                preamble(p, i);

                var currentOffset = p - current.StartAddress;

                if (i.GprRs == 31 && maxExtent <= currentOffset)
                {
                    inFInalReturnDelaySlot = true;
                }

                return true;
            };

            interp.Handlers.J = (p, i) =>
                preamble(p, i) &&
                noteMaxExtent(p, (int)(i.FullTarget(p) - current.StartAddress));

            interp.MetaHandlers.Branch = (p, i) =>
                preamble(p, i) && 
                noteMaxExtent(p, (int)(p + i.BranchOffset - current.StartAddress));

            foreach( var hasInsn in insns )
            {
                var insn = hasInsn.Instruction;

                // Skip leading nops if we aren't in any functions
                if (insn == 0 && current == null)
                    goto cont;

                var interpRval = interp.Execute(startPc, insn);

                if( current != null && insn.Id != MipsInstruction.Invalid)
                    ((IDiscoveredFunction)current).AddInstruction(
                        new InstructionWithPc(startPc, insn)
                    );

                if (current != null && (!interpRval || current.Size != 0))
                {
                    ((IDiscoveredFunction)current).Size = (uint)currentExtent;

                    if (current.Instructions.Any(i => i.Instruction.Id == MipsInstruction.JR && i.Instruction.GprRs == 31))
                        yield return current;

                    current = null;
                    currentExtent = 0;
                    maxExtent = 0;
                    inFInalReturnDelaySlot = false;
                }

                cont:
                startPc += 4;
            }
        }


        /// <summary>
        /// This also includes unconditional branches with the j instruction.
        /// </summary>
        /// <param name="insns"></param>
        /// <param name="startPc"></param>
        /// <returns></returns>
        public static IEnumerable<UInt32> DiscoverBranchTargets(this IEnumerable<Instruction> insns, UInt32 startPc)
        {
            var interp = new Interpreter<UInt32>(() => 0);
            var discovered = new HashSet<UInt32>();

            interp.MetaHandlers.Branch = (p, i) => (UInt32)(p + i.BranchOffset);
            interp.Handlers.J = (p, i) => i.FullTarget(p);

            foreach (var insn in insns)
            {
                var rval = interp.Execute(startPc, insn);

                if (rval != 0 && !discovered.Contains(rval))
                {
                    yield return rval;

                    discovered.Add(rval);
                }

                startPc += 4;
            }
        }

        public static IEnumerable<UInt32> DiscoverFunctionCalls(this IEnumerable<Instruction> insns, UInt32 startPc)
        {
            var interp = new Interpreter<UInt32>(() => 0);
            var discovered = new HashSet<UInt32>();

            interp.Handlers.JAL = (p, i) => i.FullTarget(p);

            foreach (var insn in insns)
            {
                var rval = interp.Execute(startPc, insn);

                if (rval != 0 && !discovered.Contains(rval))
                {
                    yield return rval;

                    discovered.Add(rval);
                }

                startPc += 4;
            }
        }


        /// <summary>
        /// Searches for J, JAL, and HI16/LO16 pairs. When it finds these and their values will be zeroed out.
        /// This is to facilitate searching of function signatures. <para/>
        /// This method buffers its input.
        /// </summary>
        /// <param name="insns"></param>
        /// <returns></returns>
        public static IReadOnlyList<InstructionWithPc> ZeroRelocatedValues(this IEnumerable<InstructionWithPc> insns)
        {
            var analyzer = new Analyzer();
            var lastInsn = new Dictionary<int, Instruction>();

            var input = insns.ToArray();
            var haveHi16 = new bool[32];
            var hi16Index = new int[32];

            Action reset = null;
            Action pendingAction = null;


            for(var i = 0; i < input.Length; i++)
            {
                var insn = input[i].Instruction;
                var action = pendingAction;
                var insnId = insn.Id;
                var analysis = analyzer.Analyze(insn);

                var processedId = insn.IsLoadStore ? MipsInstruction.ADDIU : insnId;

                switch (processedId)
                {
                    case MipsInstruction.JAL:
                        //pendingAction = reset;
                        goto J;

                        // Fallthrough
                    case MipsInstruction.J:
                    J:
                        insn.Target = 0;
                        break;

                    case MipsInstruction.LUI:
                        haveHi16[insn.GprRt] = true;
                        hi16Index[insn.GprRt] = i;
                        break;

                    // Case for LO16 relos, ADDIU and load/store ops
                    case MipsInstruction.ADDIU:
                        if( haveHi16[insn.GprRs] )
                        {
                            var tmp = input[hi16Index[insn.GprRs]].Instruction;
                            tmp.Immediate = 0;
                            input[hi16Index[insn.GprRs]] = new InstructionWithPc(input[hi16Index[insn.GprRs]].Pc, tmp);
                            insn.Immediate = 0;
                        }

                        haveHi16[insn.GprRs] = false;
                        hi16Index[insn.GprRs] = 0;

                        break;

                    default:
                        if(analysis != null && analysis.DestinationGpr.HasValue)
                            haveHi16[analysis.DestinationGpr.Value] = false;

                        break;
                }

                input[i] = new InstructionWithPc(input[i].Pc, insn) ;

                action?.Invoke();
            }

            return input;
        }

        public static IEnumerable<byte> ToBytes<T>(this IEnumerable<T> insns)
            where T : struct, IHasInstruction
        {
            foreach(var insn in insns)
            {
                var w = insn.Instruction;

                yield return (byte)(w >> 24);
                yield return (byte)(w >> 16);
                yield return (byte)(w >> 8);
                yield return (byte)(w);
            }
        }

        public static IEnumerable<InstructionWithPc> WithPc(this IEnumerable<Instruction> insns, UInt32 startPc) =>
            insns
                .Select((i, idx) => new InstructionWithPc((UInt32)(idx * 4 + startPc), i));

        public static IEnumerable<T> RemoveTrailingNops<T>(this IReadOnlyList<T> insns)
            where T : struct, IHasInstruction
        {
            int i;

            for (i = insns.Count - 1; i >= 0; i--)
                if (insns[i].Instruction != 0)
                    break;

            if (i <= 0)
                return new T[0];

            return insns.Take(i + 1);
        }
    }
}
