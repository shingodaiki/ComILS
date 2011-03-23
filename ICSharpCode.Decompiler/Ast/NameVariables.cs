﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.ILAst;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast
{
	public class NameVariables
	{
		static readonly Dictionary<string, string> typeNameToVariableNameDict = new Dictionary<string, string> {
			{ "System.Boolean", "flag" },
			{ "System.Byte", "b" },
			{ "System.SByte", "b" },
			{ "System.Int16", "num" },
			{ "System.Int32", "num" },
			{ "System.Int64", "num" },
			{ "System.UInt16", "num" },
			{ "System.UInt32", "num" },
			{ "System.UInt64", "num" },
			{ "System.Single", "num" },
			{ "System.Double", "num" },
			{ "System.Decimal", "num" },
			{ "System.String", "text" },
			{ "System.Object", "obj" },
			{ "System.Char", "c" }
		};
		
		
		public static void AssignNamesToVariables(DecompilerContext context, IEnumerable<ILVariable> parameters, IEnumerable<ILVariable> variables, ILBlock methodBody)
		{
			NameVariables nv = new NameVariables();
			nv.context = context;
			nv.fieldNamesInCurrentType = context.CurrentType.Fields.Select(f => f.Name).ToList();
			nv.AddExistingNames(parameters.Select(p => p.Name));
			nv.AddExistingNames(variables.Where(v => v.IsGenerated).Select(v => v.Name));
			foreach (ILVariable p in parameters) {
				if (string.IsNullOrEmpty(p.Name))
					p.Name = nv.GenerateNameForVariable(p, methodBody);
			}
			foreach (ILVariable varDef in variables) {
				if (!varDef.IsGenerated) {
					varDef.Name = nv.GenerateNameForVariable(varDef, methodBody);
				}
			}
		}
		
		DecompilerContext context;
		List<string> fieldNamesInCurrentType;
		Dictionary<string, int> typeNames = new Dictionary<string, int>();
		
		void AddExistingNames(IEnumerable<string> existingNames)
		{
			foreach (string name in existingNames) {
				if (string.IsNullOrEmpty(name))
					continue;
				// First, identify whether the name already ends with a number:
				int pos = name.Length;
				while (pos > 0 && name[pos-1] >= '0' && name[pos-1] <= '9')
					pos--;
				if (pos < name.Length) {
					int number;
					if (int.TryParse(name.Substring(pos), out number)) {
						string nameWithoutDigits = name.Substring(0, pos);
						int existingNumber;
						if (typeNames.TryGetValue(nameWithoutDigits, out existingNumber)) {
							typeNames[nameWithoutDigits] = Math.Max(number, existingNumber);
						} else {
							typeNames.Add(nameWithoutDigits, number);
						}
						continue;
					}
				}
				if (!typeNames.ContainsKey(name))
					typeNames.Add(name, 1);
			}
		}
		
		string GenerateNameForVariable(ILVariable variable, ILBlock methodBody)
		{
			string proposedName = null;
			if (variable.Type == context.CurrentType.Module.TypeSystem.Int32) {
				// test whether the variable might be a loop counter
				bool isLoopCounter = false;
				foreach (ILWhileLoop loop in methodBody.GetSelfAndChildrenRecursive<ILWhileLoop>()) {
					ILExpression expr = loop.Condition;
					while (expr != null && expr.Code == ILCode.LogicNot)
						expr = expr.Arguments[0];
					if (expr != null) {
						switch (expr.Code) {
							case ILCode.Clt:
							case ILCode.Clt_Un:
							case ILCode.Cgt:
							case ILCode.Cgt_Un:
								ILVariable loadVar;
								if (expr.Arguments[0].Match(ILCode.Ldloc, out loadVar) && loadVar == variable) {
									isLoopCounter = true;
								}
								break;
						}
					}
				}
				if (isLoopCounter) {
					// For loop variables, use i,j,k,l,m,n
					for (char c = 'i'; c <= 'n'; c++) {
						if (!typeNames.ContainsKey(c.ToString())) {
							proposedName = c.ToString();
							break;
						}
					}
				}
			}
			if (string.IsNullOrEmpty(proposedName)) {
				var proposedNameForStores =
					(from expr in methodBody.GetSelfAndChildrenRecursive<ILExpression>()
					 where expr.Code == ILCode.Stloc && expr.Operand == variable
					 select GetNameFromExpression(expr.Arguments.Single())
					).Except(fieldNamesInCurrentType).ToList();
				if (proposedNameForStores.Count == 1) {
					proposedName = proposedNameForStores[0];
				}
			}
			if (string.IsNullOrEmpty(proposedName)) {
				var proposedNameForLoads =
					(from expr in methodBody.GetSelfAndChildrenRecursive<ILExpression>()
					 from i in Enumerable.Range(0, expr.Arguments.Count)
					 let arg = expr.Arguments[i]
					 where arg.Code == ILCode.Ldloc && arg.Operand == variable
					 select GetNameForArgument(expr, i)
					).Except(fieldNamesInCurrentType).ToList();
				if (proposedNameForLoads.Count == 1) {
					proposedName = proposedNameForLoads[0];
				}
			}
			if (string.IsNullOrEmpty(proposedName)) {
				proposedName = GetNameByType(variable.Type);
			}
			
			if (!typeNames.ContainsKey(proposedName)) {
				typeNames.Add(proposedName, 0);
			}
			int count = ++typeNames[proposedName];
			if (count > 1) {
				return proposedName + count.ToString();
			} else {
				return proposedName;
			}
		}
		
		static string GetNameFromExpression(ILExpression expr)
		{
			switch (expr.Code) {
				case ILCode.Ldfld:
				case ILCode.Ldsfld:
					return CleanUpVariableName(((FieldReference)expr.Operand).Name);
				case ILCode.Call:
				case ILCode.Callvirt:
				case ILCode.CallGetter:
				case ILCode.CallvirtGetter:
					MethodReference mr = (MethodReference)expr.Operand;
					if (mr.Name.StartsWith("get_", StringComparison.OrdinalIgnoreCase) && mr.Parameters.Count == 0) {
						// use name from properties, but not from indexers
						return CleanUpVariableName(mr.Name.Substring(4));
					} else if (mr.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase) && mr.Name.Length >= 4 && char.IsUpper(mr.Name[3])) {
						// use name from Get-methods
						return CleanUpVariableName(mr.Name.Substring(3));
					}
					break;
			}
			return null;
		}
		
		static string GetNameForArgument(ILExpression parent, int i)
		{
			switch (parent.Code) {
				case ILCode.Stfld:
				case ILCode.Stsfld:
					if (i == parent.Arguments.Count - 1) // last argument is stored value
						return CleanUpVariableName(((FieldReference)parent.Operand).Name);
					else
						break;
				case ILCode.Call:
				case ILCode.Callvirt:
				case ILCode.Newobj:
				case ILCode.CallGetter:
				case ILCode.CallvirtGetter:
				case ILCode.CallSetter:
				case ILCode.CallvirtSetter:
					MethodReference methodRef = (MethodReference)parent.Operand;
					if (methodRef.Parameters.Count == 1 && i == parent.Arguments.Count - 1) {
						// argument might be value of a setter
						if (methodRef.Name.StartsWith("set_", StringComparison.OrdinalIgnoreCase)) {
							return CleanUpVariableName(methodRef.Name.Substring(4));
						} else if (methodRef.Name.StartsWith("Set", StringComparison.OrdinalIgnoreCase) && methodRef.Name.Length >= 4 && char.IsUpper(methodRef.Name[3])) {
							return CleanUpVariableName(methodRef.Name.Substring(3));
						}
					}
					MethodDefinition methodDef = methodRef.Resolve();
					if (methodDef != null) {
						var p = methodDef.Parameters.ElementAtOrDefault((parent.Code != ILCode.Newobj && methodDef.HasThis) ? i - 1 : i);
						if (p != null && !string.IsNullOrEmpty(p.Name))
							return CleanUpVariableName(p.Name);
					}
					break;
				case ILCode.Ret:
					return "result";
			}
			return null;
		}
		
		string GetNameByType(TypeReference type)
		{
			GenericInstanceType git = type as GenericInstanceType;
			if (git != null && git.ElementType.FullName == "System.Nullable`1" && git.GenericArguments.Count == 1) {
				type = ((GenericInstanceType)type).GenericArguments[0];
			}
			
			string name;
			if (type.IsArray) {
				name = "array";
			} else if (type.IsPointer || type.IsByReference) {
				name = "ptr";
			} else if (!typeNameToVariableNameDict.TryGetValue(type.FullName, out name)) {
				name = type.Name;
				// remove the 'I' for interfaces
				if (name.Length >= 3 && name[0] == 'I' && char.IsUpper(name[1]) && char.IsLower(name[2]))
					name = name.Substring(1);
				name = CleanUpVariableName(name);
			}
			return name;
		}
		
		static string CleanUpVariableName(string name)
		{
			// remove the backtick (generics)
			int pos = name.IndexOf('`');
			if (pos >= 0)
				name = name.Substring(0, pos);
			
			// remove field prefix:
			if (name.Length > 2 && name.StartsWith("m_", StringComparison.Ordinal))
				name = name.Substring(2);
			else if (name.Length > 1 && name[0] == '_')
				name = name.Substring(1);
			
			if (name.Length == 0)
				return "obj";
			else
				return char.ToLower(name[0]) + name.Substring(1);
		}
	}
}
