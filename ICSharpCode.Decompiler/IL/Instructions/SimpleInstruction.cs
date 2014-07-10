﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.IL
{
	/// <summary>
	/// A simple instruction that does not have any arguments.
	/// </summary>
	public abstract class SimpleInstruction(OpCode opCode) : ILInstruction(opCode)
	{
		public override void WriteTo(ITextOutput output)
		{
			output.Write(OpCode);
		}

		/*public override bool IsPeeking { get { return false; } }

		public override void TransformChildren(Func<ILInstruction, ILInstruction> transformFunc)
		{
		}

		internal override ILInstruction Inline(InstructionFlags flagsBefore, Stack<ILInstruction> instructionStack, out bool finished)
		{
			finished = true; // Nothing to do, since we don't have arguments.
			return this;
		}*/
	}

	/*
	class Nop() : SimpleInstruction(OpCode.Nop)
	{
		public override bool NoResult { get { return true; } }

		public override InstructionFlags Flags
		{
			get { return InstructionFlags.None; }
		}
	}

	class Pop() : SimpleInstruction(OpCode.Pop)
	{
		internal override ILInstruction Inline(InstructionFlags flagsBefore, Stack<ILInstruction> instructionStack, out bool finished)
		{
			if (instructionStack.Count > 0 && SemanticHelper.MayReorder(flagsBefore, instructionStack.Peek().Flags)) {
				finished = true;
				return instructionStack.Pop();
			}
			finished = false;
			return this;
		}

		public override InstructionFlags Flags
		{
			get { return InstructionFlags.MayPop; }
		}
	}

	class Peek() : SimpleInstruction(OpCode.Peek)
	{
		public override bool IsPeeking { get { return true; } }

		internal override ILInstruction Inline(InstructionFlags flagsBefore, Stack<ILInstruction> instructionStack, out bool finished)
		{
			finished = false;
			return this;
		}

		public override InstructionFlags Flags
		{
			get { return InstructionFlags.MayPeek; }
		}
	}

	class Ckfinite() : SimpleInstruction(OpCode.Ckfinite)
	{
		public override bool IsPeeking { get { return true; } }
		public override bool NoResult { get { return true; } }

		public override InstructionFlags Flags
		{
			get { return InstructionFlags.MayPeek | InstructionFlags.MayThrow; }
		}
	}

	class Arglist() : SimpleInstruction(OpCode.Arglist)
	{
		public override InstructionFlags Flags
		{
			get { return InstructionFlags.None; }
		}
	}

	class DebugBreak() : SimpleInstruction(OpCode.DebugBreak)
	{
		public override bool NoResult { get { return true; } }

		public override InstructionFlags Flags
		{
			get { return InstructionFlags.SideEffects; }
		}
	}

	class ConstantString(public readonly string Value) : SimpleInstruction(OpCode.LdStr)
	{
		public override InstructionFlags Flags
		{
			get { return InstructionFlags.None; }
		}

		public override void WriteTo(ITextOutput output)
		{
			output.Write("ldstr ");
			Disassembler.DisassemblerHelpers.WriteOperand(output, Value);
		}
	}

	class ConstantI4(public readonly int Value) : SimpleInstruction(OpCode.LdcI4)
	{
		public override InstructionFlags Flags
		{
			get { return InstructionFlags.None; }
		}

		public override void WriteTo(ITextOutput output)
		{
			output.Write("ldc.i4 ");
			Disassembler.DisassemblerHelpers.WriteOperand(output, Value);
		}
	}

	class ConstantI8(public readonly long Value) : SimpleInstruction(OpCode.LdcI8)
	{
		public override InstructionFlags Flags
		{
			get { return InstructionFlags.None; }
		}

		public override void WriteTo(ITextOutput output)
		{
			output.Write("ldc.i8 ");
			Disassembler.DisassemblerHelpers.WriteOperand(output, Value);
		}
	}

	class ConstantFloat(public readonly double Value) : SimpleInstruction(OpCode.LdcF)
	{
		public override InstructionFlags Flags
		{
			get { return InstructionFlags.None; }
		}

		public override void WriteTo(ITextOutput output)
		{
			output.Write("ldc.f ");
			Disassembler.DisassemblerHelpers.WriteOperand(output, Value);
		}
	}

	class ConstantNull() : SimpleInstruction(OpCode.LdNull)
	{
		public override InstructionFlags Flags
		{
			get { return InstructionFlags.None; }
		}
	}*/
}
