﻿using System;
using System.Collections;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class DynamicTests
	{
		private static dynamic field;
		private static object objectField;
		public dynamic Property {
			get;
			set;
		}

		private static void Main(string[] args)
		{
			IComparable comparable = 1;
			DynamicTests dynamicTests = new DynamicTests();
			dynamicTests.Property = 1;
			dynamicTests.Property += (dynamic)1;
		}

		private static void MemberAccess(dynamic a)
		{
			a.Test1();
			a.GenericTest<int, int>();
			a.Test2(1);
			a.Test3(a.InnerTest(1, 2, 3, 4, 5));
			a.Test4(2, null, a.Index[0]);
			a.Test5(a, a.Number, a.String);
			a[0] = 3;
			a.Index[a.Number] = 5;
			a.Setter = new DynamicTests();
			a.Setter2 = 5;
		}

		private static void RequiredCasts()
		{
			((dynamic)objectField).A = 5;
			((dynamic)objectField).B += 5;
			((dynamic)objectField).Call();
			((object)field).ToString();
			field.Call("Hello World");
			field.Call((object)"Hello World");
			field.Call((dynamic)"Hello World");
		}

		private static void DynamicCallWithString()
		{
			field.Call("Hello World");
		}

		private static void DynamicCallWithNamedArgs()
		{
			field.Call(a: "Hello World");
		}

		private static void DynamicCallWithRefOutArg(int a, out int b)
		{
			field.Call(ref a, out b);
		}

		private static void DynamicCallWithStringCastToObj()
		{
			field.Call((object)"Hello World");
		}

		private static void DynamicCallWithStringCastToDynamic()
		{
			field.Call((dynamic)"Hello World");
		}

		private static void DynamicCallWithStringCastToDynamic2()
		{
			field.Call((dynamic)"Hello World", (int)5, null);
		}

		private static void DynamicCallWithStringCastToDynamic3()
		{
			field.Call((dynamic)"Hello World", 5u, (dynamic)null);
		}

		private static void Invocation(dynamic a, dynamic b)
		{
			a(null, b.Test());
		}

		private static dynamic Test1(dynamic a)
		{
			dynamic p = a.IndexedProperty;
			return p[0];
		}

		private static dynamic Test2(dynamic a)
		{
			return a.IndexedProperty[0];
		}

		private static void ArithmeticBinaryOperators(dynamic a, dynamic b)
		{
			DynamicTests.MemberAccess(a + b);
			DynamicTests.MemberAccess(a + 1);
			DynamicTests.MemberAccess(a + null);
			DynamicTests.MemberAccess(a - b);
			DynamicTests.MemberAccess(a - 1);
			DynamicTests.MemberAccess(a - null);
			DynamicTests.MemberAccess(a * b);
			DynamicTests.MemberAccess(a * 1);
			DynamicTests.MemberAccess(a * null);
			DynamicTests.MemberAccess(a / b);
			DynamicTests.MemberAccess(a / 1);
			DynamicTests.MemberAccess(a / null);
			DynamicTests.MemberAccess(a % b);
			DynamicTests.MemberAccess(a % 1);
			DynamicTests.MemberAccess(a % null);
		}

		private static void RelationalOperators(dynamic a, dynamic b)
		{
			DynamicTests.MemberAccess(a == b);
			DynamicTests.MemberAccess(a == 1);
			DynamicTests.MemberAccess(a == null);
			DynamicTests.MemberAccess(a != b);
			DynamicTests.MemberAccess(a != 1);
			DynamicTests.MemberAccess(a != null);
			DynamicTests.MemberAccess(a < b);
			DynamicTests.MemberAccess(a < 1);
			DynamicTests.MemberAccess(a < null);
			DynamicTests.MemberAccess(a > b);
			DynamicTests.MemberAccess(a > 1);
			DynamicTests.MemberAccess(a > null);
			DynamicTests.MemberAccess(a >= b);
			DynamicTests.MemberAccess(a >= 1);
			DynamicTests.MemberAccess(a >= null);
			DynamicTests.MemberAccess(a <= b);
			DynamicTests.MemberAccess(a <= 1);
			DynamicTests.MemberAccess(a <= null);
		}

		private static void Casts(dynamic a)
		{
			Console.WriteLine();
			int b = 5;
			if (b < 0)
				return;
			MemberAccess((int)a);
		}

		private static void CompoundAssignment(dynamic a, dynamic b)
		{
			a.Setter2 += 5;
			a.Setter2 -= 1;
			a.Setter2 *= 2;
			a.Setter2 /= 5;
			a.Setter2 += b;
			a.Setter2 -= b;
			a.Setter2 *= b;
			a.Setter2 /= b;
			field.Setter += 5;
			field.Setter -= 5;
		}

		private static void InlineCompoundAssignment(dynamic a, dynamic b)
		{
			Console.WriteLine(a.Setter2 += 5);
			Console.WriteLine(a.Setter2 -= 1);
			Console.WriteLine(a.Setter2 *= 2);
			Console.WriteLine(a.Setter2 /= 5);
			Console.WriteLine(a.Setter2 += b);
			Console.WriteLine(a.Setter2 -= b);
			Console.WriteLine(a.Setter2 *= b);
			Console.WriteLine(a.Setter2 /= b);
		}

		private static void UnaryOperators(dynamic a)
		{
			a--;
			a++;
			--a;
			++a;
			Casts(-a);
			Casts(+a);
		}

		private static void Loops(dynamic list)
		{
			foreach (dynamic item in list) {
				UnaryOperators(item);
			}
		}

		private static void If(dynamic a, dynamic b)
		{
			if (a == b)
			{
				Console.WriteLine("Equal");
			}
		}

		private static void If2(dynamic a, dynamic b)
		{
			if (a == null || b == null)
			{
				Console.WriteLine("Equal");
			}
		}
	}
}
