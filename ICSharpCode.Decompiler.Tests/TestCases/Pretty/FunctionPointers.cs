﻿using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class FunctionPointersWithDynamicTypes
	{
		public class D<T, U>
		{
		}
		public class A<T>
		{
			public class B<U>
			{
			}
		}

		public unsafe delegate*<dynamic, dynamic, dynamic> F1;
		public unsafe delegate*<object, object, dynamic> F2;
		public unsafe delegate*<dynamic, object, object> F3;
		public unsafe delegate*<object, dynamic, object> F4;
		public unsafe delegate*<object, object, object> F5;
		public unsafe delegate*<object, object, ref dynamic> F6;
		public unsafe delegate*<ref dynamic, object, object> F7;
		public unsafe delegate*<object, ref dynamic, object> F8;
		public unsafe delegate*<ref object, ref object, dynamic> F9;
		public unsafe delegate*<dynamic, ref object, ref object> F10;
		public unsafe delegate*<ref object, dynamic, ref object> F11;
		public unsafe delegate*<object, ref readonly dynamic> F12;
		public unsafe delegate*<in dynamic, object> F13;
		public unsafe delegate*<out dynamic, object> F14;
		public unsafe D<delegate*<dynamic>[], dynamic> F15;
		public unsafe delegate*<A<object>.B<dynamic>> F16;
	}

	internal class FunctionPointersWithNativeIntegerTypes
	{
		public unsafe delegate*<nint, nint, nint> F1;
		public unsafe delegate*<IntPtr, IntPtr, nint> F2;
		public unsafe delegate*<nint, IntPtr, IntPtr> F3;
		public unsafe delegate*<IntPtr, nint, IntPtr> F4;
		public unsafe delegate*<delegate*<IntPtr, IntPtr, IntPtr>, nint> F5;
		public unsafe delegate*<nint, delegate*<IntPtr, IntPtr, IntPtr>> F6;
		public unsafe delegate*<delegate*<IntPtr, IntPtr, nint>, IntPtr> F7;
		public unsafe delegate*<IntPtr, delegate*<IntPtr, nint, IntPtr>> F8;
	}

	internal class FunctionPointersWithRefParams
	{
		public unsafe delegate*<in byte, ref char, out float, ref readonly int> F1;
	}

	// TODO: the new calling convention syntax isn't yet available in the released Roslyn version
	//internal unsafe class FunctionPointersWithCallingConvention
	//{
	//  public delegate*<void> managed;
	//  public delegate* unmanaged<void> unmanaged;
	//  public delegate* unmanaged[Cdecl]<void> cdecl;
	//}
}
