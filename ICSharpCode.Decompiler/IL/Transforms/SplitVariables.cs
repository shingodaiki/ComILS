﻿// Copyright (c) 2016 Daniel Grunwald
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

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.FlowAnalysis;
using ICSharpCode.Decompiler.Util;
using System.Threading;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Live range splitting for IL variables.
	/// </summary>
	public class SplitVariables : IILTransform
	{
		public void Run(ILFunction function, ILTransformContext context)
		{
			var groupStores = new GroupStores(function, context.CancellationToken);
			function.Body.AcceptVisitor(groupStores);
			// Replace analyzed variables with their split versions:
			foreach (var inst in function.Descendants.OfType<IInstructionWithVariableOperand>()) {
				if (groupStores.IsAnalyzedVariable(inst.Variable)) {
					inst.Variable = groupStores.GetNewVariable(inst);
				}
			}
			function.Variables.RemoveDead();
		}

		static bool IsCandidateVariable(ILVariable v)
		{
			switch (v.Kind) {
				case VariableKind.Local:
				case VariableKind.Exception:
					foreach (var ldloca in v.AddressInstructions) {
						if (DetermineAddressUse(ldloca) == AddressUse.Unknown) {
							// If the address isn't used immediately,
							// we'd need to deal with aliases.
							return false;
						}
					}
					return true;
				case VariableKind.StackSlot:
					// stack slots: are already split by construction,
					// except for the locals-turned-stackslots in async functions
					if (v.Function.IsAsync)
						goto case VariableKind.Local;
					else
						return false;
				default:
					// parameters: avoid splitting parameters
					// pinned locals: must not split (doing so would extend the life of the pin to the end of the method)
					return false;
			}
		}

		enum AddressUse
		{
			Unknown,
			LocalRead,
			LocalReadWrite
		}

		static AddressUse DetermineAddressUse(ILInstruction addressLoadingInstruction)
		{
			switch (addressLoadingInstruction.Parent) {
				case LdObj ldobj:
					return AddressUse.LocalRead;
				case LdFlda ldflda:
					return DetermineAddressUse(ldflda);
				case Await await:
					// GetAwaiter() may write to the struct, but shouldn't store the address for later use
					return AddressUse.LocalReadWrite;
				case Call call:
					// Address is passed to method.
					// We'll assume the method only uses the address locally,
					// unless we can see an address being returned from the method:
					if (call.Method.ReturnType.IsByRefLike) {
						return AddressUse.Unknown;
					}
					foreach (var p in call.Method.Parameters) {
						// catch "out Span<int>" and similar
						if (p.Type.SkipModifiers() is ByReferenceType brt && brt.ElementType.IsByRefLike)
							return AddressUse.Unknown;
					}
					/* Currently there's not really any need to distinguish between readonly and readwrite method calls:
					var addrParam = call.GetParameter(addressLoadingInstruction.ChildIndex);
					bool isReadOnly;
					if (addrParam == null) {
						isReadOnly = (call.Method.DeclaringTypeDefinition?.HasAttribute(KnownAttribute.IsReadOnly) ?? false)
							|| (call.Method.DeclaringType?.IsKnownType(KnownTypeCode.NullableOfT) ?? false);
					} else {
						isReadOnly = false;
					}*/
					return AddressUse.LocalReadWrite;
				default:
					return AddressUse.Unknown;
			}
		}

		/// <summary>
		/// Use the union-find structure to merge
		/// </summary>
		/// <remarks>
		/// Instructions in a group are stores to the same variable that must stay together (cannot be split).
		/// </remarks>
		class GroupStores : ReachingDefinitionsVisitor
		{
			readonly UnionFind<IInstructionWithVariableOperand> unionFind = new UnionFind<IInstructionWithVariableOperand>();

			/// <summary>
			/// For each uninitialized variable, one representative instruction that
			/// potentially observes the unintialized value of the variable.
			/// Used to merge together all such loads of the same uninitialized value.
			/// </summary>
			readonly Dictionary<ILVariable, IInstructionWithVariableOperand> uninitVariableUsage = new Dictionary<ILVariable, IInstructionWithVariableOperand>();
			
			public GroupStores(ILFunction scope, CancellationToken cancellationToken) : base(scope, IsCandidateVariable, cancellationToken)
			{
			}

			protected internal override void VisitLdLoc(LdLoc inst)
			{
				base.VisitLdLoc(inst);
				HandleLoad(inst);
			}

			protected internal override void VisitLdLoca(LdLoca inst)
			{
				base.VisitLdLoca(inst);
				HandleLoad(inst);
			}

			void HandleLoad(IInstructionWithVariableOperand inst)
			{
				if (IsAnalyzedVariable(inst.Variable)) {
					if (IsPotentiallyUninitialized(state, inst.Variable)) {
						// merge all uninit loads together:
						if (uninitVariableUsage.TryGetValue(inst.Variable, out var uninitLoad)) {
							unionFind.Merge(inst, uninitLoad);
						} else {
							uninitVariableUsage.Add(inst.Variable, inst);
						}
					}
					foreach (var store in GetStores(state, inst.Variable)) {
						unionFind.Merge(inst, (IInstructionWithVariableOperand)store);
					}
				}
			}
			
			readonly Dictionary<IInstructionWithVariableOperand, ILVariable> newVariables = new Dictionary<IInstructionWithVariableOperand, ILVariable>();
			
			/// <summary>
			/// Gets the new variable for a LdLoc, StLoc or TryCatchHandler instruction.
			/// </summary>
			internal ILVariable GetNewVariable(IInstructionWithVariableOperand inst)
			{
				var representative = unionFind.Find(inst);
				ILVariable v;
				if (!newVariables.TryGetValue(representative, out v)) {
					v = new ILVariable(inst.Variable.Kind, inst.Variable.Type, inst.Variable.StackType, inst.Variable.Index);
					v.Name = inst.Variable.Name;
					v.HasGeneratedName = inst.Variable.HasGeneratedName;
					v.StateMachineField = inst.Variable.StateMachineField;
					v.HasInitialValue = false; // we'll set HasInitialValue when we encounter an uninit load
					newVariables.Add(representative, v);
					inst.Variable.Function.Variables.Add(v);
				}
				if (inst.Variable.HasInitialValue && uninitVariableUsage.TryGetValue(inst.Variable, out var uninitLoad) && uninitLoad == inst) {
					v.HasInitialValue = true;
				}
				return v;
			}
		}
	}
}
