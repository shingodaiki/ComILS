﻿// Copyright (c) 2020 Siegfried Pammer
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

using System.Diagnostics;
using System.Linq;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler.IL
{
	partial class MatchInstruction : ILInstruction
	{
		/* Pseudo-Code for interpreting a MatchInstruction:
			bool Eval()
			{
				var value = this.TestedOperand.Eval();
				if (this.CheckNotNull && value == null)
					return false;
				if (this.CheckType && !(value is this.Variable.Type))
					return false;
				if (this.Deconstruct) {
					deconstructResult = new[numArgs];
					EvalCall(this.Method, value, out deconstructResult[0], .., out deconstructResult[numArgs-1]);
					// any occurrences of 'deconstruct.result' in the subPatterns will refer
					// to the values provided by evaluating the call.
				}
				Variable.Value = value;
				foreach (var subPattern in this.SubPatterns) {
					if (!subPattern.Eval())
						return false;
				}
				return true;
			}
		*/
		/* Examples of MatchInstructions:
			expr is var x:
				match(x = expr)

			expr is {} x:
				match.notnull(x = expr

			expr is T x:
				match.type[T](x = expr)

			expr is C { A: var x } z:
				match.type[C](z = expr) {
				   match(x = z.A)
				}

			expr is C { A: var x, B: 42, C: { A: 4 } } z:
				match.type[C](z = expr) {
					match(x = z.A),
					comp (z.B == 42),
					match.notnull(temp2 = z.C) {
						comp (temp2.A == 4)
					}
				}

			expr is C(var x, var y, <4):
				match.type[C].deconstruct[C.Deconstruct](tmp1 = expr) {
					match(x = deconstruct.result0(tmp1)),
					match(y = deconstruct.result1(tmp1)),
					comp(deconstruct.result2(tmp1) < 4),
				}
			
			expr is C(1, D(2, 3)):
				match.type[C].deconstruct(c = expr) {
					comp(deconstruct.result0(c) == 1),
					match.type[D].deconstruct(d = deconstruct.result1(c)) {
						comp(deconstruct.result0(d) == 2),
						comp(deconstruct.result1(d) == 2),
					}
				}
		 */

		/// <summary>
		/// Checks whether the input instruction can represent a pattern matching operation.
		/// 
		/// Any pattern matching instruction will first evaluate the `testedOperand` (a descendant of `inst`),
		/// and then match the value of that operand against the pattern encoded in the instruction.
		/// The matching may have side-effects on the newly-initialized pattern variables
		/// (even if the pattern fails to match!).
		/// The pattern matching instruction evaluates to 1 (as I4) if the pattern matches, or 0 otherwise.
		/// </summary>
		public static bool IsPatternMatch(ILInstruction inst, out ILInstruction testedOperand)
		{
			switch (inst) {
				case MatchInstruction m:
					testedOperand = m.testedOperand;
					return true;
				case Comp comp:
					testedOperand = comp.Left;
					return IsConstant(comp.Right);
				case ILInstruction logicNot when logicNot.MatchLogicNot(out var operand):
					return IsPatternMatch(operand, out testedOperand);
				default:
					testedOperand = null;
					return false;
			}
		}

		private static bool IsConstant(ILInstruction inst)
		{
			return inst.OpCode switch
			{
				OpCode.LdcDecimal => true,
				OpCode.LdcF4 => true,
				OpCode.LdcF8 => true,
				OpCode.LdcI4 => true,
				OpCode.LdcI8 => true,
				OpCode.LdNull => true,
				_ => false
			};
		}

		internal IParameter GetDeconstructResult(int index)
		{
			Debug.Assert(this.Deconstruct);
			int firstOutParam = (method.IsStatic ? 1 : 0);
			return this.Method.Parameters[firstOutParam + index];
		}

		void AdditionalInvariants()
		{
			Debug.Assert(variable.Kind == VariableKind.PatternLocal);
			if (this.Deconstruct) {
				Debug.Assert(method.Name == "Deconstruct");
				int firstOutParam = (method.IsStatic ? 1 : 0);
				Debug.Assert(method.Parameters.Count >= firstOutParam);
				Debug.Assert(method.Parameters.Skip(firstOutParam).All(p => p.IsOut));
			}
			foreach (var subPattern in SubPatterns) {
				if (!IsPatternMatch(subPattern, out ILInstruction operand))
					Debug.Fail("Sub-Pattern must be a valid pattern");
				if (operand.MatchLdFld(out var target, out _)) {
					Debug.Assert(target.MatchLdLoc(variable));
				} else if (operand is CallInstruction call) {
					Debug.Assert(call.Method.AccessorKind == System.Reflection.MethodSemanticsAttributes.Getter);
					Debug.Assert(call.Arguments[0].MatchLdLoc(variable));
				} else if (operand is DeconstructResultInstruction resultInstruction) {
					Debug.Assert(this.Deconstruct);
				} else {
					Debug.Fail("Tested operand of sub-pattern is invalid.");
				}
			}
		}

		public override void WriteTo(ITextOutput output, ILAstWritingOptions options)
		{
			WriteILRange(output, options);
			output.Write(OpCode);
			if (CheckNotNull) {
				output.Write(".notnull");
			}
			if (CheckType) {
				output.Write(".type[");
				variable.Type.WriteTo(output);
				output.Write(']');
			}
			if (Deconstruct) {
				output.Write(".deconstruct[");
				method.WriteTo(output);
				output.Write(']');
			}
			output.Write(' ');
			output.Write('(');
			Variable.WriteTo(output);
			output.Write(" = ");
			TestedOperand.WriteTo(output, options);
			output.Write(')');
		}
	}
}
