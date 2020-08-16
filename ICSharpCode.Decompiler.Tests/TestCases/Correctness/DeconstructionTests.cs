﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Correctness
{
	class DeconstructionTests
	{
		public static void Main()
		{
			new DeconstructionTests().Test();
		}

		public struct MyInt
		{
			public static implicit operator int(MyInt x)
			{
				Console.WriteLine("int op_Implicit(MyInt)");
				return 0;
			}

			public static implicit operator MyInt(int x)
			{
				Console.WriteLine("MyInt op_Implicit(int)");
				return default(MyInt);
			}
		}

		private class DeconstructionSource<T, T2>
		{
			public int Dummy {
				get;
				set;
			}

			public void Deconstruct(out T a, out T2 b)
			{
				Console.WriteLine("Deconstruct");
				a = default(T);
				b = default(T2);
			}
		}

		private class AssignmentTargets
		{
			int id;

			public AssignmentTargets(int id)
			{
				this.id = id;
			}

			public int IntField;

			public int? NullableIntField;

			public MyInt MyIntField;

			public MyInt? NullableMyIntField;

			public MyInt My {
				get {
					Console.WriteLine($"{id}.get_My()");
					return default;
				}
				set {
					Console.WriteLine($"{id}.set_My({value})");
				}
			}

			public MyInt? NMy {
				get {
					Console.WriteLine($"{id}.get_NMy()");
					return default;
				}
				set {
					Console.WriteLine($"{id}.set_NMy({value})");
				}
			}

			public int IntProperty {
				get {
					Console.WriteLine($"{id}.get_IntProperty()");
					return default;
				}
				set {
					Console.WriteLine($"{id}.set_IntProperty({value})");
				}
			}

			public uint UIntProperty {
				get {
					Console.WriteLine($"{id}.get_UIntProperty()");
					return default;
				}
				set {
					Console.WriteLine($"{id}.set_UIntProperty({value})");
				}
			}
		}

		private DeconstructionSource<T, T2> GetSource<T, T2>()
		{
			Console.WriteLine("GetSource()");
			return new DeconstructionSource<T, T2>();
		}

		private AssignmentTargets Get(int i)
		{
			Console.WriteLine($"Get({i})");
			return new AssignmentTargets(i);
		}

		public void Test()
		{
			Property_NoDeconstruction_SwappedAssignments();
			Property_NoDeconstruction_SwappedInits();
			Property_IntToUIntConversion();
		}

		public void Property_NoDeconstruction_SwappedAssignments()
		{
			Console.WriteLine("Property_NoDeconstruction_SwappedAssignments:");
			AssignmentTargets customDeconstructionAndConversion = Get(0);
			AssignmentTargets customDeconstructionAndConversion2 = Get(1);
			GetSource<MyInt?, MyInt>().Deconstruct(out MyInt? x, out MyInt y);
			MyInt myInt2 = customDeconstructionAndConversion2.My = y;
			MyInt? myInt4 = customDeconstructionAndConversion.NMy = x;
		}

		public void Property_NoDeconstruction_SwappedInits()
		{
			Console.WriteLine("Property_NoDeconstruction_SwappedInits:");
			AssignmentTargets customDeconstructionAndConversion = Get(1);
			(Get(0).NMy, customDeconstructionAndConversion.My) = GetSource<MyInt?, MyInt>();
		}

		public void Property_IntToUIntConversion()
		{
			Console.WriteLine("Property_IntToUIntConversion:");
			AssignmentTargets t0 = Get(0);
			AssignmentTargets t1 = Get(1);
			int a;
			uint b;
			GetSource<int, uint>().Deconstruct(out a, out b);
			t0.UIntProperty = (uint)a;
			t1.IntProperty = (int)b;
		}
	}
}
