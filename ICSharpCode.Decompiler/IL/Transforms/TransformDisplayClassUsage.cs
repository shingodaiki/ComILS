﻿// Copyright (c) 2019 Siegfried Pammer
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
using System.Diagnostics;
using System.Linq;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL.Transforms
{
	/// <summary>
	/// Transforms closure fields to local variables.
	/// 
	/// This is a post-processing step of <see cref="DelegateConstruction"/>, <see cref="LocalFunctionDecompiler"/> and <see cref="TransformExpressionTrees"/>.
	/// </summary>
	class TransformDisplayClassUsage : ILVisitor, IILTransform
	{
		class DisplayClass
		{
			public bool IsMono;
			public ILInstruction Initializer;
			public ILVariable Variable;
			public ITypeDefinition Definition;
			public Dictionary<IField, ILVariable> Variables;
			public BlockContainer CaptureScope;
			public ILFunction DeclaringFunction;
		}

		ILTransformContext context;
		readonly Dictionary<ILVariable, DisplayClass> displayClasses = new Dictionary<ILVariable, DisplayClass>();
		readonly List<ILInstruction> instructionsToRemove = new List<ILInstruction>();

		public void Run(ILFunction function, ILTransformContext context)
		{
			try {
				if (this.context != null)
					throw new InvalidOperationException("Reentrancy in " + nameof(TransformDisplayClassUsage));
				this.context = context;
				var decompilationContext = new SimpleTypeResolveContext(context.Function.Method);
				// Traverse nested functions in post-order:
				// Inner functions are transformed before outer functions
				foreach (var f in function.Descendants.OfType<ILFunction>()) {
					foreach (var v in f.Variables.ToArray()) {
						if (context.Settings.YieldReturn && HandleMonoStateMachine(function, v, decompilationContext, f))
							continue;
						if ((context.Settings.AnonymousMethods || context.Settings.ExpressionTrees) && IsClosure(v, out ITypeDefinition closureType, out var inst)) {
							AddOrUpdateDisplayClass(f, v, closureType, inst, localFunctionClosureParameter: false);
						}
						if (context.Settings.LocalFunctions && f.Kind == ILFunctionKind.LocalFunction && v.Kind == VariableKind.Parameter && v.Index > -1 && f.Method.Parameters[v.Index.Value] is IParameter p && LocalFunctionDecompiler.IsClosureParameter(p, decompilationContext)) {
							AddOrUpdateDisplayClass(f, v, ((ByReferenceType)p.Type).ElementType.GetDefinition(), f.Body, localFunctionClosureParameter: true);
						}
					}
				}
				VisitILFunction(function);
				if (instructionsToRemove.Count > 0) {
					context.Step($"Remove instructions", function);
					foreach (var store in instructionsToRemove) {
						if (store.Parent is Block containingBlock)
							containingBlock.Instructions.Remove(store);
					}
				}
				RemoveDeadVariableInit.ResetHasInitialValueFlag(function, context);
			} finally {
				instructionsToRemove.Clear();
				displayClasses.Clear();
				this.context = null;
			}
		}

		private void AddOrUpdateDisplayClass(ILFunction f, ILVariable v, ITypeDefinition closureType, ILInstruction inst, bool localFunctionClosureParameter)
		{
			var displayClass = displayClasses.Values.FirstOrDefault(c => c.Definition == closureType);
			// TODO : figure out whether it is a mono compiled closure, without relying on the type name
			bool isMono = f.StateMachineCompiledWithMono || closureType.Name.Contains("AnonStorey");
			if (displayClass == null) {
				displayClasses.Add(v, new DisplayClass {
					IsMono = isMono,
					Initializer = inst,
					Variable = v,
					Definition = closureType,
					Variables = new Dictionary<IField, ILVariable>(),
					CaptureScope = (isMono && IsMonoNestedCaptureScope(closureType)) || localFunctionClosureParameter ? null : v.CaptureScope,
					DeclaringFunction = localFunctionClosureParameter ? f.DeclarationScope.Ancestors.OfType<ILFunction>().First() : f
				});
			} else {
				if (displayClass.CaptureScope == null && !localFunctionClosureParameter)
					displayClass.CaptureScope = isMono && IsMonoNestedCaptureScope(closureType) ? null : v.CaptureScope;
				if (displayClass.CaptureScope != null && !localFunctionClosureParameter) {
					displayClass.DeclaringFunction = displayClass.CaptureScope.Ancestors.OfType<ILFunction>().First();
				}
				displayClass.Variable = v;
				displayClass.Initializer = inst;
				displayClasses.Add(v, displayClass);
			}
		}

		bool IsClosure(ILVariable variable, out ITypeDefinition closureType, out ILInstruction initializer)
		{
			closureType = null;
			initializer = null;
			if (variable.IsSingleDefinition && variable.StoreInstructions.SingleOrDefault() is StLoc inst) {
				initializer = inst;
				if (IsClosureInit(inst, out closureType)) {
					instructionsToRemove.Add(inst);
					return true;
				}
			}
			closureType = variable.Type.GetDefinition();
			if (context.Settings.LocalFunctions && closureType?.Kind == TypeKind.Struct && variable.HasInitialValue && IsPotentialClosure(this.context, closureType)) {
				initializer = LocalFunctionDecompiler.GetStatement(variable.AddressInstructions.OrderBy(i => i.StartILOffset).First());
				return true;
			}
			return false;
		}

		bool IsClosureInit(StLoc inst, out ITypeDefinition closureType)
		{
			if (inst.Value is NewObj newObj) {
				closureType = newObj.Method.DeclaringTypeDefinition;
				return closureType != null && IsPotentialClosure(this.context, newObj);
			}
			closureType = null;
			return false;
		}

		bool IsMonoNestedCaptureScope(ITypeDefinition closureType)
		{
			var decompilationContext = new SimpleTypeResolveContext(context.Function.Method);
			return closureType.Fields.Any(f => IsPotentialClosure(decompilationContext.CurrentTypeDefinition, f.ReturnType.GetDefinition()));
		}

		/// <summary>
		/// mcs likes to optimize closures in yield state machines away by moving the captured variables' fields into the state machine type,
		/// We construct a <see cref="DisplayClass"/> that spans the whole method body.
		/// </summary>
		bool HandleMonoStateMachine(ILFunction currentFunction, ILVariable thisVariable, SimpleTypeResolveContext decompilationContext, ILFunction nestedFunction)
		{
			if (!(nestedFunction.StateMachineCompiledWithMono && thisVariable.IsThis()))
				return false;
			// Special case for Mono-compiled yield state machines
			ITypeDefinition closureType = thisVariable.Type.GetDefinition();
			if (!(closureType != decompilationContext.CurrentTypeDefinition
				&& IsPotentialClosure(decompilationContext.CurrentTypeDefinition, closureType, allowTypeImplementingInterfaces: true)))
				return false;

			var displayClass = new DisplayClass {
				IsMono = true,
				Initializer = nestedFunction.Body,
				Variable = thisVariable,
				Definition = thisVariable.Type.GetDefinition(),
				Variables = new Dictionary<IField, ILVariable>(),
				CaptureScope = (BlockContainer)nestedFunction.Body
			};
			displayClasses.Add(thisVariable, displayClass);
			foreach (var stateMachineVariable in nestedFunction.Variables) {
				if (stateMachineVariable.StateMachineField == null || displayClass.Variables.ContainsKey(stateMachineVariable.StateMachineField))
					continue;
				displayClass.Variables.Add(stateMachineVariable.StateMachineField, stateMachineVariable);
			}
			if (!currentFunction.Method.IsStatic && FindThisField(out var thisField)) {
				var thisVar = currentFunction.Variables
					.FirstOrDefault(t => t.IsThis() && t.Type.GetDefinition() == decompilationContext.CurrentTypeDefinition);
				if (thisVar == null) {
					thisVar = new ILVariable(VariableKind.Parameter, decompilationContext.CurrentTypeDefinition, -1) { Name = "this" };
					currentFunction.Variables.Add(thisVar);
				}
				displayClass.Variables.Add(thisField, thisVar);
			}
			return true;

			bool FindThisField(out IField foundField)
			{
				foundField = null;
				foreach (var field in closureType.GetFields(f2 => !f2.IsStatic && !displayClass.Variables.ContainsKey(f2) && f2.Type.GetDefinition() == decompilationContext.CurrentTypeDefinition)) {
					thisField = field;
					return true;
				}
				return false;
			}
		}

		internal static bool IsSimpleDisplayClass(IType type)
		{
			if (!type.HasGeneratedName() || (!type.Name.Contains("DisplayClass") && !type.Name.Contains("AnonStorey")))
				return false;
			if (type.DirectBaseTypes.Any(t => !t.IsKnownType(KnownTypeCode.Object)))
				return false;
			return true;
		}

		internal static bool IsPotentialClosure(ILTransformContext context, NewObj inst)
		{
			var decompilationContext = new SimpleTypeResolveContext(context.Function.Ancestors.OfType<ILFunction>().Last().Method);
			return IsPotentialClosure(decompilationContext.CurrentTypeDefinition, inst.Method.DeclaringTypeDefinition);
		}

		internal static bool IsPotentialClosure(ILTransformContext context, ITypeDefinition potentialDisplayClass)
		{
			var decompilationContext = new SimpleTypeResolveContext(context.Function.Ancestors.OfType<ILFunction>().Last().Method);
			return IsPotentialClosure(decompilationContext.CurrentTypeDefinition, potentialDisplayClass);
		}

		internal static bool IsPotentialClosure(ITypeDefinition decompiledTypeDefinition, ITypeDefinition potentialDisplayClass, bool allowTypeImplementingInterfaces = false)
		{
			if (potentialDisplayClass == null || !potentialDisplayClass.IsCompilerGeneratedOrIsInCompilerGeneratedClass())
				return false;
			switch (potentialDisplayClass.Kind) {
				case TypeKind.Struct:
					break;
				case TypeKind.Class:
					if (!potentialDisplayClass.IsSealed)
						return false;
					if (!allowTypeImplementingInterfaces) {
						if (!potentialDisplayClass.DirectBaseTypes.All(t => t.IsKnownType(KnownTypeCode.Object)))
							return false;
					}
					break;
				default:
					return false;
			}
			
			while (potentialDisplayClass != decompiledTypeDefinition) {
				potentialDisplayClass = potentialDisplayClass.DeclaringTypeDefinition;
				if (potentialDisplayClass == null)
					return false;
			}
			return true;
		}

		ILFunction currentFunction;

		protected internal override void VisitILFunction(ILFunction function)
		{
			var oldFunction = this.currentFunction;
			try {
				this.currentFunction = function;
				base.VisitILFunction(function);
			} finally {
				this.currentFunction = oldFunction;
			}
		}

		protected override void Default(ILInstruction inst)
		{
			foreach (var child in inst.Children) {
				child.AcceptVisitor(this);
			}
		}

		protected internal override void VisitStLoc(StLoc inst)
		{
			base.VisitStLoc(inst);

			if (inst.Variable.Kind == VariableKind.Local && inst.Variable.IsSingleDefinition && inst.Variable.LoadCount == 0 && inst.Value is StLoc) {
				context.Step($"Remove unused variable assignment {inst.Variable.Name}", inst);
				inst.ReplaceWith(inst.Value);
			}
		}

		protected internal override void VisitStObj(StObj inst)
		{
			inst.Value.AcceptVisitor(this);
			if (IsParameterAssignment(inst, out var displayClass, out var field, out var parameter)) {
				context.Step($"Detected parameter assignment {parameter.Name}", inst);
				displayClass.Variables.Add(field, parameter);
				instructionsToRemove.Add(inst);
			} else if (IsDisplayClassAssignment(inst, out displayClass, out field, out var variable)) {
				context.Step($"Detected display-class assignment {variable.Name}", inst);
				displayClass.Variables.Add(field, variable);
				instructionsToRemove.Add(inst);
			} else {
				inst.Target.AcceptVisitor(this);
				EarlyExpressionTransforms.StObjToStLoc(inst, context);
			}
		}

		protected internal override void VisitLdObj(LdObj inst)
		{
			base.VisitLdObj(inst);
			EarlyExpressionTransforms.LdObjToLdLoc(inst, context);
		}

		private bool IsDisplayClassLoad(ILInstruction target, out ILVariable variable)
		{
			// We cannot use MatchLdLocRef here because local functions use ref parameters
			if (target.MatchLdLoc(out variable) || target.MatchLdLoca(out variable))
				return true;
			return false;
		}

		private bool IsDisplayClassAssignment(StObj inst, out DisplayClass displayClass, out IField field, out ILVariable variable)
		{
			variable = null;
			if (!IsDisplayClassFieldAccess(inst.Target, out var displayClassVar, out displayClass, out field))
				return false;
			if (!(inst.Value.MatchLdLoc(out var v) && displayClasses.ContainsKey(v)))
				return false;
			if (displayClassVar.Function != currentFunction)
				return false;
			variable = v;
			return true;
		}

		private bool IsParameterAssignment(StObj inst, out DisplayClass displayClass, out IField field, out ILVariable parameter)
		{
			parameter = null;
			if (!IsDisplayClassFieldAccess(inst.Target, out var displayClassVar, out displayClass, out field))
				return false;
			if (!(inst.Value.MatchLdLoc(out var v) && v.Kind == VariableKind.Parameter && v.Function == currentFunction))
				return false;
			if (displayClass.Variables.ContainsKey(field))
				return false;
			if (displayClassVar.Function != currentFunction)
				return false;
			parameter = v;
			return true;
		}

		private bool IsDisplayClassFieldAccess(ILInstruction inst, out ILVariable displayClassVar, out DisplayClass displayClass, out IField field)
		{
			displayClass = null;
			displayClassVar = null;
			field = null;
			if (!(inst is LdFlda ldflda))
				return false;
			field = (IField)ldflda.Field.MemberDefinition;
			return IsDisplayClassLoad(ldflda.Target, out displayClassVar)
				&& displayClasses.TryGetValue(displayClassVar, out displayClass);
		}

		protected internal override void VisitLdFlda(LdFlda inst)
		{
			base.VisitLdFlda(inst);
			// Get display class info
			if (!IsDisplayClassFieldAccess(inst, out _, out DisplayClass displayClass, out IField field))
				return;
			if (!displayClass.Variables.TryGetValue(field, out var v)) {
				context.Step($"Introduce captured variable for {field.FullName}", inst);
				// Introduce a fresh variable for the display class field.
				Debug.Assert(displayClass.Definition == field.DeclaringTypeDefinition);
				v = displayClass.DeclaringFunction.RegisterVariable(VariableKind.Local, GetVariableTypeFromClosureField(field), field.Name);
				v.HasInitialValue = true;
				v.CaptureScope = displayClass.CaptureScope;
				inst.ReplaceWith(new LdLoca(v).WithILRange(inst));
				displayClass.Variables.Add(field, v);
			} else {
				context.Step($"Reuse captured variable {v.Name} for {field.FullName}", inst);
				inst.ReplaceWith(new LdLoca(v).WithILRange(inst));
			}
		}

		private IType GetVariableTypeFromClosureField(IField field)
		{
			if (!(field.Type is ITypeParameter typeParameter))
				return field.Type;
			var rootMethod = context.Function.Method;
			if (typeParameter.Owner != field.DeclaringTypeDefinition)
				return field.Type;
			if (typeParameter.Index >= rootMethod.TypeParameters.Count) {
				Debug.Assert(false, "Cannot map display-class type parameter to method type parameter");
				return field.Type;
			}

			return rootMethod.TypeParameters[typeParameter.Index];
		}
	}
}
