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

using System;
using System.Diagnostics;
using ICSharpCode.Decompiler.IL;

namespace ICSharpCode.Decompiler.FlowAnalysis
{
	/// <summary>
	/// DataFlowVisitor that performs definite assignment analysis.
	/// </summary>
	class DefiniteAssignmentVisitor : DataFlowVisitor<DefiniteAssignmentVisitor.State>
	{
		/// <summary>
		/// State for definite assignment analysis.
		/// </summary>
		[DebuggerDisplay("{bits}")]
		public struct State : IDataFlowState<State>
		{
			/// <summary>
			/// bits[i]: There is a code path from the entry point to this state's position
			///          that does not write to function.Variables[i].
			/// 
			/// Initial state: all bits set
			/// Bottom state: all bits clear
			/// </summary>
			readonly BitSet bits;

			/// <summary>
			/// Creates the initial state.
			/// </summary>
			public State(int variableCount)
			{
				this.bits = new BitSet(variableCount);
				this.bits.Set(0, variableCount);
			}
			
			private State(BitSet bits)
			{
				this.bits = bits;
			}

			public bool LessThanOrEqual(State otherState)
			{
				return bits.IsSubsetOf(otherState.bits);
			}

			public State Clone()
			{
				return new State(bits.Clone());
			}

			public void ReplaceWith(State newContent)
			{
				bits.ReplaceWith(newContent.bits);
			}

			public void JoinWith(State incomingState)
			{
				bits.UnionWith(incomingState.bits);
			}

			public void MeetWith(State incomingState)
			{
				bits.IntersectWith(incomingState.bits);
			}

			public void ReplaceWithBottom()
			{
				bits.ClearAll();
			}

			public bool IsBottom {
				get { return !bits.Any(); }
			}

			public void MarkVariableInitialized(int variableIndex)
			{
				bits.Clear(variableIndex);
			}

			public bool IsPotentiallyUninitialized(int variableIndex)
			{
				return bits[variableIndex];
			}
		}
		
		readonly ILVariableScope scope;
		readonly BitSet variablesWithUninitializedUsage;
		
		public DefiniteAssignmentVisitor(ILVariableScope scope)
		{
			this.scope = scope;
			this.variablesWithUninitializedUsage = new BitSet(scope.Variables.Count);
			Initialize(new State(scope.Variables.Count));
		}
		
		public bool IsPotentiallyUsedUninitialized(ILVariable v)
		{
			Debug.Assert(v.Scope == scope);
			return variablesWithUninitializedUsage[v.IndexInScope];
		}
		
		void HandleStore(ILVariable v)
		{
			if (v.Scope == scope) {
				// Mark the variable as initialized:
				state.MarkVariableInitialized(v.IndexInScope);
				// Note that this gets called even if the store is in unreachable code,
				// but that's OK because bottomState.MarkVariableInitialized() has no effect.
				
				// After the state change, we have to call
				//  PropagateStateOnException() = currentStateOnException.JoinWith(state);
				// but because MarkVariableInitialized() only clears a bit,
				// this is guaranteed to be a no-op.
			}
		}
		
		void EnsureInitialized(ILVariable v)
		{
			if (v.Scope == scope && state.IsPotentiallyUninitialized(v.IndexInScope)) {
				variablesWithUninitializedUsage.Set(v.IndexInScope);
			}
		}
		
		protected internal override void VisitStLoc(StLoc inst)
		{
			base.VisitStLoc(inst);
			HandleStore(inst.Variable);
		}
		
		protected override void BeginTryCatchHandler(TryCatchHandler inst)
		{
			HandleStore(inst.Variable);
			base.BeginTryCatchHandler(inst);
		}
		
		protected internal override void VisitLdLoc(LdLoc inst)
		{
			base.VisitLdLoc(inst);
			EnsureInitialized(inst.Variable);
		}
		
		protected internal override void VisitLdLoca(LdLoca inst)
		{
			base.VisitLdLoca(inst);
			EnsureInitialized(inst.Variable);
		}
	}
}
