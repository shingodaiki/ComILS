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
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Collection of transforms that detect simple expression patterns
	/// (e.g. 'cgt.un(..., ld.null)') and replace them with different instructions.
	/// </summary>
	/// <remarks>
	/// Should run after inlining so that the expression patterns can be detected.
	/// </remarks>
	public class ExpressionTransforms : ILVisitor, IILTransform, ISingleStep
	{
		public int MaxStepCount { get; set; } = int.MaxValue;
		Stepper stepper;

		void IILTransform.Run(ILFunction function, ILTransformContext context)
		{
			stepper = new Stepper(MaxStepCount);
			function.AcceptVisitor(this);
		}
		
		protected override void Default(ILInstruction inst)
		{
			foreach (var child in inst.Children) {
				child.AcceptVisitor(this);
			}
		}
		
		protected internal override void VisitComp(Comp inst)
		{
			base.VisitComp(inst);
			if (inst.Right.MatchLdNull()) {
				// comp(left > ldnull)  => comp(left != ldnull)
				// comp(left <= ldnull) => comp(left == ldnull)
				if (inst.Kind == ComparisonKind.GreaterThan)
					inst.Kind = ComparisonKind.Inequality;
				else if (inst.Kind == ComparisonKind.LessThanOrEqual)
					inst.Kind = ComparisonKind.Equality;
			} else if (inst.Left.MatchLdNull()) {
				// comp(ldnull < right)  => comp(ldnull != right)
				// comp(ldnull >= right) => comp(ldnull == right)
				if (inst.Kind == ComparisonKind.LessThan)
					inst.Kind = ComparisonKind.Inequality;
				else if (inst.Kind == ComparisonKind.GreaterThanOrEqual)
					inst.Kind = ComparisonKind.Equality;
			}
			
			var rightWithoutConv = inst.Right.UnwrapConv(ConversionKind.SignExtend).UnwrapConv(ConversionKind.ZeroExtend);
			if (rightWithoutConv.MatchLdcI4(0)
			    && inst.Sign == Sign.Unsigned
			    && (inst.Kind == ComparisonKind.GreaterThan || inst.Kind == ComparisonKind.LessThanOrEqual))
			{
				ILInstruction array;
				if (inst.Left.MatchLdLen(StackType.I, out array)) {
					// comp.unsigned(ldlen array > conv i4->i(ldc.i4 0))
					// => comp(ldlen.i4 array > ldc.i4 0)
					// This is a special case where the C# compiler doesn't generate conv.i4 after ldlen.
					inst.Left.ReplaceWith(new LdLen(StackType.I4, array) { ILRange = inst.Left.ILRange });
					inst.Right = rightWithoutConv;
				}
				// comp.unsigned(left > ldc.i4 0) => comp(left != ldc.i4 0)
				// comp.unsigned(left <= ldc.i4 0) => comp(left == ldc.i4 0)
				if (inst.Kind == ComparisonKind.GreaterThan)
					inst.Kind = ComparisonKind.Inequality;
				else if (inst.Kind == ComparisonKind.LessThanOrEqual)
					inst.Kind = ComparisonKind.Equality;
				stepper.Stepped();
			}
		}
		
		protected internal override void VisitConv(Conv inst)
		{
			inst.Argument.AcceptVisitor(this);
			ILInstruction array;
			if (inst.Argument.MatchLdLen(StackType.I, out array) && inst.TargetType.IsIntegerType() && !inst.CheckForOverflow) {
				// conv.i4(ldlen array) => ldlen.i4(array)
				inst.AddILRange(inst.Argument.ILRange);
				inst.ReplaceWith(new LdLen(inst.TargetType.GetStackType(), array) { ILRange = inst.ILRange });
				stepper.Stepped();
			}
		}
		
		protected internal override void VisitLogicNot(LogicNot inst)
		{
			ILInstruction arg, lhs, rhs;
			if (inst.Argument.MatchLogicNot(out arg)) {
				// logic.not(logic.not(arg))
				// ==> arg
				Debug.Assert(arg.ResultType == StackType.I4);
				arg.AddILRange(inst.ILRange);
				arg.AddILRange(inst.Argument.ILRange);
				inst.ReplaceWith(arg);
				stepper.Stepped();
				arg.AcceptVisitor(this);
			} else if (inst.Argument is Comp) {
				Comp comp = (Comp)inst.Argument;
				if (comp.InputType != StackType.F || comp.Kind.IsEqualityOrInequality()) {
					// push negation into comparison:
					comp.Kind = comp.Kind.Negate();
					comp.AddILRange(inst.ILRange);
					inst.ReplaceWith(comp);
					stepper.Stepped();
				}
				comp.AcceptVisitor(this);
			} else if (inst.Argument.MatchLogicAnd(out lhs, out rhs)) {
				// logic.not(if (lhs) rhs else ldc.i4 0)
				// ==> if (logic.not(lhs)) ldc.i4 1 else logic.not(rhs)
				IfInstruction ifInst = (IfInstruction)inst.Argument;
				var ldc0 = ifInst.FalseInst;
				Debug.Assert(ldc0.MatchLdcI4(0));
				ifInst.Condition = new LogicNot(lhs) { ILRange = inst.ILRange };
				ifInst.TrueInst = new LdcI4(1) { ILRange = ldc0.ILRange };
				ifInst.FalseInst = new LogicNot(rhs) { ILRange = inst.ILRange };
				inst.ReplaceWith(ifInst);
				stepper.Stepped();
				ifInst.AcceptVisitor(this);
			} else if (inst.Argument.MatchLogicOr(out lhs, out rhs)) {
				// logic.not(if (lhs) ldc.i4 1 else rhs)
				// ==> if (logic.not(lhs)) logic.not(rhs) else ldc.i4 0)
				IfInstruction ifInst = (IfInstruction)inst.Argument;
				var ldc1 = ifInst.TrueInst;
				Debug.Assert(ldc1.MatchLdcI4(1));
				ifInst.Condition = new LogicNot(lhs) { ILRange = inst.ILRange };
				ifInst.TrueInst = new LogicNot(rhs) { ILRange = inst.ILRange };
				ifInst.FalseInst = new LdcI4(0) { ILRange = ldc1.ILRange };
				inst.ReplaceWith(ifInst);
				stepper.Stepped();
				ifInst.AcceptVisitor(this);
			} else {
				inst.Argument.AcceptVisitor(this);
			}
		}
		
		protected internal override void VisitCall(Call inst)
		{
			if (inst.Method.IsConstructor && !inst.Method.IsStatic && inst.Method.DeclaringType.Kind == TypeKind.Struct) {
				Debug.Assert(inst.Arguments.Count == inst.Method.Parameters.Count + 1);
				// Transform call to struct constructor:
				// call(ref, ...)
				// => stobj(ref, newobj(...))
				var newObj = new NewObj(inst.Method);
				newObj.ILRange = inst.ILRange;
				newObj.Arguments.AddRange(inst.Arguments.Skip(1));
				var expr = new StObj(inst.Arguments[0], newObj, inst.Method.DeclaringType);
				inst.ReplaceWith(expr);
				stepper.Stepped();
				// Both the StObj and the NewObj may trigger further rules, so continue visiting the replacement:
				VisitStObj(expr);
			} else {
				base.VisitCall(inst);
			}
		}
		
		protected internal override void VisitNewObj(NewObj inst)
		{
			LdcDecimal decimalConstant;
			if (TransformDecimalCtorToConstant(inst, out decimalConstant)) {
				inst.ReplaceWith(decimalConstant);
				stepper.Stepped();
				return;
			}
			base.VisitNewObj(inst);
		}
		
		bool TransformDecimalCtorToConstant(NewObj inst, out LdcDecimal result)
		{
			IType t = inst.Method.DeclaringType;
			result = null;
			if (!t.IsKnownType(KnownTypeCode.Decimal))
				return false;
			var args = inst.Arguments;
			if (args.Count == 1) {
				int val;
				if (args[0].MatchLdcI4(out val)) {
					result = new LdcDecimal(val);
					return true;
				}
			} else if (args.Count == 5) {
				int lo, mid, hi, isNegative, scale;
				if (args[0].MatchLdcI4(out lo) && args[1].MatchLdcI4(out mid) &&
				    args[2].MatchLdcI4(out hi) && args[3].MatchLdcI4(out isNegative) &&
				    args[4].MatchLdcI4(out scale))
				{
					result = new LdcDecimal(new decimal(lo, mid, hi, isNegative != 0, (byte)scale));
					return true;
				}
			}
			return false;
		}
		
		// This transform is required because ILInlining only works with stloc/ldloc
		protected internal override void VisitStObj(StObj inst)
		{
			base.VisitStObj(inst);
			ILVariable v;
			if (inst.Target.MatchLdLoca(out v)
			    && TypeUtils.IsCompatibleTypeForMemoryAccess(new ByReferenceType(v.Type), inst.Type)
			    && inst.UnalignedPrefix == 0
			    && !inst.IsVolatile)
			{
				// stobj(ldloca(v), ...)
				// => stloc(v, ...)
				inst.ReplaceWith(new StLoc(v, inst.Value));
				stepper.Stepped();
			}
			
			ILInstruction target;
			IType t;
			BinaryNumericInstruction binary = inst.Value as BinaryNumericInstruction;
			if (binary != null && binary.Left.MatchLdObj(out target, out t) && inst.Target.Match(target).Success) {
				// stobj(target, binary.op(ldobj(target), ...))
				// => compound.op(target, ...)
				inst.ReplaceWith(new CompoundAssignmentInstruction(binary.Operator, binary.Left, binary.Right, t, binary.CheckForOverflow, binary.Sign, CompoundAssignmentType.EvaluatesToNewValue));
				stepper.Stepped();
			}
		}
		
		protected internal override void VisitIfInstruction(IfInstruction inst)
		{
			// Bring LogicAnd/LogicOr into their canonical forms:
			// if (cond) ldc.i4 0 else RHS --> if (!cond) RHS else ldc.i4 0
			// if (cond) RHS else ldc.i4 1 --> if (!cond) ldc.i4 1 else RHS
			if (inst.TrueInst.MatchLdcI4(0) || inst.FalseInst.MatchLdcI4(1)) {
				var t = inst.TrueInst;
				inst.TrueInst = inst.FalseInst;
				inst.FalseInst = t;
				inst.Condition = new LogicNot(inst.Condition);
			}

			base.VisitIfInstruction(inst);

			// if (cond) stloc (A, V1) else stloc (A, V2) --> stloc (A, if (cond) V1 else V2)
			Block trueInst = inst.TrueInst as Block;
			if (trueInst == null || trueInst.Instructions.Count != 1)
				return;
			Block falseInst = inst.FalseInst as Block;
			if (falseInst == null || falseInst.Instructions.Count != 1)
				return;
			ILVariable v1, v2;
			ILInstruction value1, value2;
			if (trueInst.Instructions[0].MatchStLoc(out v1, out value1) && falseInst.Instructions[0].MatchStLoc(out v2, out value2) && v1 == v2) {
				inst.ReplaceWith(new StLoc(v1, new IfInstruction(new LogicNot(inst.Condition), value2, value1)));
				stepper.Stepped();
			}
		}
	}
}
