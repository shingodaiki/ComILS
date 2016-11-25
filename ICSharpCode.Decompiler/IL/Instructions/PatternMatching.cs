﻿// Copyright (c) 2014 Daniel Grunwald
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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.Decompiler.IL
{
	partial class ILInstruction
	{
		public bool MatchLdcI4(int val)
		{
			return OpCode == OpCode.LdcI4 && ((LdcI4)this).Value == val;
		}
		
		public bool MatchLdLoc(ILVariable variable)
		{
			var inst = this as LdLoc;
			return inst != null && inst.Variable == variable;
		}
		
		public bool MatchLdLoca(ILVariable variable)
		{
			var inst = this as LdLoca;
			return inst != null && inst.Variable == variable;
		}
		
		public bool MatchLdThis()
		{
			var inst = this as LdLoc;
			return inst != null && inst.Variable.Kind == VariableKind.Parameter && inst.Variable.Index < 0;
		}
		
		public bool MatchStLoc(out ILVariable variable)
		{
			var inst = this as StLoc;
			if (inst != null) {
				variable = inst.Variable;
				return true;
			}
			variable = null;
			return false;
		}
		
		public bool MatchStLoc(ILVariable variable, out ILInstruction value)
		{
			var inst = this as StLoc;
			if (inst != null && inst.Variable == variable) {
				value = inst.Value;
				return true;
			}
			value = null;
			return false;
		}
		
		public bool MatchLdLen(StackType type, out ILInstruction array)
		{
			var inst = this as LdLen;
			if (inst != null && inst.ResultType == type) {
				array = inst.Array;
				return true;
			}
			array = null;
			return false;
		}
		
		public bool MatchBranch(out Block targetBlock)
		{
			var inst = this as Branch;
			if (inst != null) {
				targetBlock = inst.TargetBlock;
				return true;
			}
			targetBlock = null;
			return false;
		}
		
		public bool MatchBranch(Block targetBlock)
		{
			var inst = this as Branch;
			return inst != null && inst.TargetBlock == targetBlock;
		}
		
		public bool MatchLeave(out BlockContainer targetContainer)
		{
			var inst = this as Leave;
			if (inst != null) {
				targetContainer = inst.TargetContainer;
				return true;
			}
			targetContainer = null;
			return false;
		}
		
		public bool MatchLeave(BlockContainer targetContainer)
		{
			var inst = this as Leave;
			return inst != null && inst.TargetContainer == targetContainer;
		}
		
		public bool MatchIfInstruction(out ILInstruction condition, out ILInstruction trueInst, out ILInstruction falseInst)
		{
			var inst = this as IfInstruction;
			if (inst != null) {
				condition = inst.Condition;
				trueInst = inst.TrueInst;
				falseInst = inst.FalseInst;
				return true;
			}
			condition = null;
			trueInst = null;
			falseInst = null;
			return false;
		}
		
		/// <summary>
		/// Matches an if instruction where the false instruction is a nop.
		/// </summary>
		public bool MatchIfInstruction(out ILInstruction condition, out ILInstruction trueInst)
		{
			var inst = this as IfInstruction;
			if (inst != null && inst.FalseInst.MatchNop()) {
				condition = inst.Condition;
				trueInst = inst.TrueInst;
				return true;
			}
			condition = null;
			trueInst = null;
			return false;
		}

		/// <summary>
		/// Matches a 'logic and' instruction ("if (a) b else ldc.i4 0").
		/// Note: unlike C# '&amp;&amp;', this instruction is not limited to booleans,
		/// but allows passing through arbitrary I4 values on the rhs (but not on the lhs).
		/// </summary>
		public bool MatchLogicAnd(out ILInstruction lhs, out ILInstruction rhs)
		{
			var inst = this as IfInstruction;
			if (inst != null && inst.FalseInst.MatchLdcI4(0)) {
				lhs = inst.Condition;
				rhs = inst.TrueInst;
				return true;
			}
			lhs = null;
			rhs = null;
			return false;
		}

		/// <summary>
		/// Matches a 'logic or' instruction ("if (a) ldc.i4 1 else b").
		/// Note: unlike C# '||', this instruction is not limited to booleans,
		/// but allows passing through arbitrary I4 values on the rhs (but not on the lhs).
		/// </summary>
		public bool MatchLogicOr(out ILInstruction lhs, out ILInstruction rhs)
		{
			var inst = this as IfInstruction;
			if (inst != null && inst.TrueInst.MatchLdcI4(1)) {
				lhs = inst.Condition;
				rhs = inst.FalseInst;
				return true;
			}
			lhs = null;
			rhs = null;
			return false;
		}

		public bool MatchTryCatchHandler(out ILVariable variable)
		{
			var inst = this as TryCatchHandler;
			if (inst != null) {
				variable = inst.Variable;
				return true;
			}
			variable = null;
			return false;
		}
		
		/// <summary>
		/// Matches comp(left == right) or logic.not(comp(left != right)).
		/// </summary>
		public bool MatchCompEquals(out ILInstruction left, out ILInstruction right)
		{
			ComparisonKind op;
			Comp comp;
			if (this is LogicNot logicNot) {
				op = ComparisonKind.Inequality;
				comp = logicNot.Argument as Comp;
			} else {
				op = ComparisonKind.Equality;
				comp = this as Comp;
			}
			if (comp != null && comp.Kind == op) {
				left = comp.Left;
				right = comp.Right;
				return true;
			}
			left = null;
			right = null;
			return false;
		}
		
		public bool MatchLdsFld(IField field)
		{
			LdsFlda ldsflda = (this as LdObj)?.Target as LdsFlda;
			if (ldsflda != null) {
				return field.Equals(ldsflda.Field);
			}
			return false;
		}
		
		public bool MatchLdFld(out IField field)
		{
			LdFlda ldflda = (this as LdObj)?.Target as LdFlda;
			if (ldflda != null) {
				field = ldflda.Field;
				return true;
			}
			field = null;
			return false;
		}
		
		public bool MatchLdsFld(out IField field)
		{
			LdsFlda ldsflda = (this as LdObj)?.Target as LdsFlda;
			if (ldsflda != null) {
				field = ldsflda.Field;
				return true;
			}
			field = null;
			return false;
		}
		
		public bool MatchStsFld(out ILInstruction value, out IField field)
		{
			var stobj = this as StObj;
			LdsFlda ldsflda = stobj?.Target as LdsFlda;
			if (ldsflda != null) {
				value = stobj.Value;
				field = ldsflda.Field;
				return true;
			}
			value = null;
			field = null;
			return false;
		}
		
		public bool MatchStFld(out ILInstruction value, out IField field)
		{
			var stobj = this as StObj;
			LdFlda ldflda = stobj?.Target as LdFlda;
			if (ldflda != null) {
				value = stobj.Value;
				field = ldflda.Field;
				return true;
			}
			value = null;
			field = null;
			return false;
		}
		
		public bool MatchBinaryNumericInstruction(BinaryNumericOperator @operator)
		{
			var op = this as BinaryNumericInstruction;
			return op != null && op.Operator == @operator;
		}
		
		public bool MatchBinaryNumericInstruction(BinaryNumericOperator @operator, out ILInstruction left, out ILInstruction right)
		{
			var op = this as BinaryNumericInstruction;
			if (op != null && op.Operator == @operator) {
				left = op.Left;
				right = op.Right;
				return true;
			}
			left = null;
			right = null;
			return false;
		}
		
		public bool MatchBinaryNumericInstruction(out BinaryNumericOperator @operator, out ILInstruction left, out ILInstruction right)
		{
			var op = this as BinaryNumericInstruction;
			if (op != null) {
				@operator = op.Operator;
				left = op.Left;
				right = op.Right;
				return true;
			}
			@operator = BinaryNumericOperator.None;
			left = null;
			right = null;
			return false;
		}
		
		/// <summary>
		/// If this instruction is a conversion of the specified kind, return its argument.
		/// Otherwise, return the instruction itself.
		/// </summary>
		public virtual ILInstruction UnwrapConv(ConversionKind kind)
		{
			return this;
		}
	}
}
