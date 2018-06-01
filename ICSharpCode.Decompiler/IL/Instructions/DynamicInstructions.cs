﻿// Copyright (c) 2018 Siegfried Pammer
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using ICSharpCode.Decompiler.IL.Patterns;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.IL
{
	[Flags]
	public enum CSharpArgumentInfoFlags
	{
		None = 0,
		UseCompileTimeType = 1,
		Constant = 2,
		NamedArgument = 4,
		IsRef = 8,
		IsOut = 0x10,
		IsStaticType = 0x20
	}

	[Flags]
	public enum CSharpBinderFlags
	{
		None = 0,
		CheckedContext = 1,
		InvokeSimpleName = 2,
		InvokeSpecialName = 4,
		BinaryOperationLogical = 8,
		ConvertExplicit = 0x10,
		ConvertArrayIndex = 0x20,
		ResultIndexed = 0x40,
		ValueFromCompoundAssignment = 0x80,
		ResultDiscarded = 0x100
	}

	public struct CSharpArgumentInfo
	{
		public string Name { get; set; }
		public CSharpArgumentInfoFlags Flags { get; set; }
		public IType CompileTimeType { get; set; }
	}

	partial class DynamicInstruction
	{
		public CSharpBinderFlags BinderFlags { get; }
		public IType CallingContext { get; }

		protected DynamicInstruction(OpCode opCode, CSharpBinderFlags binderFlags, IType context)
			: base(opCode)
		{
			BinderFlags = binderFlags;
			CallingContext = context;
		}

		protected void WriteBinderFlags(ITextOutput output, ILAstWritingOptions options)
		{
			if ((BinderFlags & CSharpBinderFlags.BinaryOperationLogical) != 0)
				output.Write(".logic");
			if ((BinderFlags & CSharpBinderFlags.CheckedContext) != 0)
				output.Write(".checked");
			if ((BinderFlags & CSharpBinderFlags.ConvertArrayIndex) != 0)
				output.Write(".arrayindex");
			if ((BinderFlags & CSharpBinderFlags.ConvertExplicit) != 0)
				output.Write(".explicit");
			if ((BinderFlags & CSharpBinderFlags.InvokeSimpleName) != 0)
				output.Write(".invokesimple");
			if ((BinderFlags & CSharpBinderFlags.InvokeSpecialName) != 0)
				output.Write(".invokespecial");
			if ((BinderFlags & CSharpBinderFlags.ResultDiscarded) != 0)
				output.Write(".discard");
			if ((BinderFlags & CSharpBinderFlags.ResultIndexed) != 0)
				output.Write(".resultindexed");
			if ((BinderFlags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0)
				output.Write(".compound");
		}

		public abstract CSharpArgumentInfo GetArgumentInfoOfChild(int index);
	}

	partial class DynamicConvertInstruction
	{
		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			type.WriteTo(output);
			output.Write('(');
			argument.WriteTo(output, options);
			output.Write(')');
		}

		public DynamicConvertInstruction(CSharpBinderFlags binderFlags, IType type, IType context, ILInstruction argument)
			: base(OpCode.DynamicConvertInstruction, binderFlags, context)
		{
			Type = type;
			Argument = argument;
		}

		protected internal override bool PerformMatch(ref ListMatch listMatch, ref Match match)
		{
			return base.PerformMatch(ref listMatch, ref match);
		}

		public override StackType ResultType => type.GetStackType();

		public bool IsChecked => (BinderFlags & CSharpBinderFlags.CheckedContext) != 0;

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			return default(CSharpArgumentInfo);
		}
	}

	partial class DynamicInvokeMemberInstruction
	{
		public string Name { get; }
		public IReadOnlyList<IType> TypeArguments { get; }
		public IReadOnlyList<CSharpArgumentInfo> ArgumentInfo { get; }

		public DynamicInvokeMemberInstruction(CSharpBinderFlags binderFlags, string name, IType[] typeArguments, IType context, CSharpArgumentInfo[] argumentInfo, ILInstruction[] arguments)
			: base(OpCode.DynamicInvokeMemberInstruction, binderFlags, context)
		{
			Name = name;
			TypeArguments = typeArguments ?? Empty<IType>.Array;
			ArgumentInfo = argumentInfo;
			Arguments = new InstructionCollection<ILInstruction>(this, 0);
			Arguments.AddRange(arguments);
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write(Name);
			if (TypeArguments.Count > 0) {
				output.Write('<');
				int i = 0;
				foreach (var typeArg in TypeArguments) {
					if (i > 0)
						output.Write(", ");
					typeArg.WriteTo(output);
					i++;
				}
				output.Write('>');
			}
			output.Write('(');
			int j = 0;
			foreach (var arg in Arguments) {
				if (j > 0)
					output.Write(", ");
				output.Write("[flags: ");
				output.Write(ArgumentInfo[j].Flags.ToString());
				output.Write(", name: " + ArgumentInfo[j].Name + "] ");
				arg.WriteTo(output, options);
				j++;
			}
			output.Write(')');
		}

		public override StackType ResultType => SpecialType.Dynamic.GetStackType();

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			if (index < 0 || index >= ArgumentInfo.Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			return ArgumentInfo[index];
		}
	}

	partial class DynamicGetMemberInstruction
	{
		public string Name { get; }
		public CSharpArgumentInfo TargetArgumentInfo { get; }

		public DynamicGetMemberInstruction(CSharpBinderFlags binderFlags, string name, IType context, CSharpArgumentInfo targetArgumentInfo, ILInstruction target)
			: base(OpCode.DynamicGetMemberInstruction, binderFlags, context)
		{
			Name = name;
			TargetArgumentInfo = targetArgumentInfo;
			Target = target;
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write(Name);
			output.Write('(');
			Target.WriteTo(output, options);
			output.Write(')');
		}

		public override StackType ResultType => SpecialType.Dynamic.GetStackType();

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			if (index != 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			return TargetArgumentInfo;
		}
	}

	partial class DynamicSetMemberInstruction
	{
		public string Name { get; }
		public CSharpArgumentInfo TargetArgumentInfo { get; }
		public CSharpArgumentInfo ValueArgumentInfo { get; }

		public DynamicSetMemberInstruction(CSharpBinderFlags binderFlags, string name, IType context, CSharpArgumentInfo targetArgumentInfo, ILInstruction target, CSharpArgumentInfo valueArgumentInfo, ILInstruction value)
			: base(OpCode.DynamicSetMemberInstruction, binderFlags, context)
		{
			Name = name;
			TargetArgumentInfo = targetArgumentInfo;
			Target = target;
			ValueArgumentInfo = valueArgumentInfo;
			Value = value;
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write(Name);
			output.Write('(');
			Target.WriteTo(output, options);
			output.Write(", ");
			Value.WriteTo(output, options);
			output.Write(')');
		}

		public override StackType ResultType => SpecialType.Dynamic.GetStackType();

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			switch (index) {
				case 0:
					return TargetArgumentInfo;
				case 1:
					return ValueArgumentInfo;
				default:
					throw new ArgumentOutOfRangeException(nameof(index));
			}
		}
	}

	partial class DynamicGetIndexInstruction
	{
		public IReadOnlyList<CSharpArgumentInfo> ArgumentInfo { get; }

		public DynamicGetIndexInstruction(CSharpBinderFlags binderFlags, IType context, CSharpArgumentInfo[] argumentInfo, ILInstruction[] arguments)
			: base(OpCode.DynamicGetIndexInstruction, binderFlags, context)
		{
			ArgumentInfo = argumentInfo;
			Arguments = new InstructionCollection<ILInstruction>(this, 0);
			Arguments.AddRange(arguments);
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write("get_Item");
			output.Write('(');
			int j = 0;
			foreach (var arg in Arguments) {
				if (j > 0)
					output.Write(", ");
				arg.WriteTo(output, options);
				j++;
			}
			output.Write(')');
		}

		public override StackType ResultType => SpecialType.Dynamic.GetStackType();

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			if (index < 0 || index >= ArgumentInfo.Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			return ArgumentInfo[index];
		}
	}

	partial class DynamicSetIndexInstruction
	{
		public IReadOnlyList<CSharpArgumentInfo> ArgumentInfo { get; }

		public DynamicSetIndexInstruction(CSharpBinderFlags binderFlags, IType context, CSharpArgumentInfo[] argumentInfo, ILInstruction[] arguments)
			: base(OpCode.DynamicSetIndexInstruction, binderFlags, context)
		{
			ArgumentInfo = argumentInfo;
			Arguments = new InstructionCollection<ILInstruction>(this, 0);
			Arguments.AddRange(arguments);
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write("set_Item");
			output.Write('(');
			int j = 0;
			foreach (var arg in Arguments) {
				if (j > 0)
					output.Write(", ");
				arg.WriteTo(output, options);
				j++;
			}
			output.Write(')');
		}

		public override StackType ResultType => SpecialType.Dynamic.GetStackType();

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			if (index < 0 || index >= ArgumentInfo.Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			return ArgumentInfo[index];
		}
	}

	partial class DynamicInvokeConstructorInstruction
	{
		public IReadOnlyList<CSharpArgumentInfo> ArgumentInfo { get; }

		public DynamicInvokeConstructorInstruction(CSharpBinderFlags binderFlags, IType context, CSharpArgumentInfo[] argumentInfo, ILInstruction[] arguments)
			: base(OpCode.DynamicInvokeConstructorInstruction, binderFlags, context)
		{
			ArgumentInfo = argumentInfo;
			Arguments = new InstructionCollection<ILInstruction>(this, 0);
			Arguments.AddRange(arguments);
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write(".ctor");
			output.Write('(');
			int j = 0;
			foreach (var arg in Arguments) {
				if (j > 0)
					output.Write(", ");
				arg.WriteTo(output, options);
				j++;
			}
			output.Write(')');
		}

		public override StackType ResultType => SpecialType.Dynamic.GetStackType();

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			if (index < 0 || index >= ArgumentInfo.Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			return ArgumentInfo[index];
		}
	}

	partial class DynamicBinaryOperatorInstruction
	{
		public CSharpArgumentInfo LeftArgumentInfo { get; }
		public CSharpArgumentInfo RightArgumentInfo { get; }
		public ExpressionType Operation { get; }

		public DynamicBinaryOperatorInstruction(CSharpBinderFlags binderFlags, ExpressionType operation, IType context, CSharpArgumentInfo leftArgumentInfo, ILInstruction left, CSharpArgumentInfo rightArgumentInfo, ILInstruction right)
			: base(OpCode.DynamicBinaryOperatorInstruction, binderFlags, context)
		{
			Operation = operation;
			LeftArgumentInfo = leftArgumentInfo;
			Left = left;
			RightArgumentInfo = rightArgumentInfo;
			Right = right;
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write(Operation.ToString());
			output.Write('(');
			Left.WriteTo(output, options);
			output.Write(", ");
			Right.WriteTo(output, options);
			output.Write(')');
		}

		public override StackType ResultType => SpecialType.Dynamic.GetStackType();

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			switch (index) {
				case 0:
					return LeftArgumentInfo;
				case 1:
					return RightArgumentInfo;
				default:
					throw new ArgumentOutOfRangeException(nameof(index));
			}
		}
	}

	partial class DynamicUnaryOperatorInstruction
	{
		public CSharpArgumentInfo OperandArgumentInfo { get; }
		public ExpressionType Operation { get; }

		public DynamicUnaryOperatorInstruction(CSharpBinderFlags binderFlags, ExpressionType operation, IType context, CSharpArgumentInfo operandArgumentInfo, ILInstruction operand)
			: base(OpCode.DynamicUnaryOperatorInstruction, binderFlags, context)
		{
			Operation = operation;
			OperandArgumentInfo = operandArgumentInfo;
			Operand = operand;
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write(Operation.ToString());
			output.Write('(');
			Operand.WriteTo(output, options);
			output.Write(')');
		}

		public override StackType ResultType {
			get {
				switch (Operation) {
					case ExpressionType.IsFalse:
					case ExpressionType.IsTrue:
						return StackType.I4; // bool
					default:
						return SpecialType.Dynamic.GetStackType();
				}
			}
		}

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			switch (index) {
				case 0:
					return OperandArgumentInfo;
				default:
					throw new ArgumentOutOfRangeException(nameof(index));
			}
		}
	}

	partial class DynamicInvokeInstruction
	{
		public IReadOnlyList<CSharpArgumentInfo> ArgumentInfo { get; }

		public DynamicInvokeInstruction(CSharpBinderFlags binderFlags, IType context, CSharpArgumentInfo[] argumentInfo, ILInstruction[] arguments)
			: base(OpCode.DynamicInvokeInstruction, binderFlags, context)
		{
			ArgumentInfo = argumentInfo;
			Arguments = new InstructionCollection<ILInstruction>(this, 0);
			Arguments.AddRange(arguments);
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write('(');
			int j = 0;
			foreach (var arg in Arguments) {
				if (j > 0)
					output.Write(", ");
				arg.WriteTo(output, options);
				j++;
			}
			output.Write(')');
		}

		public override StackType ResultType => SpecialType.Dynamic.GetStackType();

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			if (index < 0 || index >= ArgumentInfo.Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			return ArgumentInfo[index];
		}
	}

	partial class DynamicIsEventInstruction
	{
		public string Name { get; }

		public DynamicIsEventInstruction(CSharpBinderFlags binderFlags, string name, IType context, ILInstruction argument)
			: base(OpCode.DynamicIsEventInstruction, binderFlags, context)
		{
			Name = name;
			Argument = argument;
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			ILRange.WriteTo(output, options);
			output.Write(OpCode);
			WriteBinderFlags(output, options);
			output.Write(' ');
			output.Write('(');
			Argument.WriteTo(output, options);
			output.Write(')');
		}

		public override StackType ResultType => StackType.I4;

		public override CSharpArgumentInfo GetArgumentInfoOfChild(int index)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}
	}
}
