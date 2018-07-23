﻿// Copyright (c) 2018 Siegfried Pammer
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

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class OptionalArguments
	{
		private void SimpleTests()
		{
			Test();
			Test(5);
			Test(10, "Hello World!");
		}

		private void Conflicts()
		{
			OnlyDifferenceIsLastArgument(5, 3, "Hello");
			OnlyDifferenceIsLastArgument(5, 3, 3.141);
			OnlyDifferenceIsLastArgument(5, 3, null);
			OnlyDifferenceIsLastArgument(5, 3, double.NegativeInfinity);

			OnlyDifferenceIsLastArgumentCastNecessary(10, "World", (string)null);
			OnlyDifferenceIsLastArgumentCastNecessary(10, "Hello", (OptionalArguments)null);

			DifferenceInArgumentCount();
			DifferenceInArgumentCount("Hello");
			DifferenceInArgumentCount("World");
		}

		private void ParamsTests()
		{
			ParamsMethod(5, 10, 9, 8);
			ParamsMethod(null);
			ParamsMethod(5);
			ParamsMethod(10);
			ParamsMethod(null, 1, 2, 3);
		}

		private void ParamsMethod(int a = 5, params int[] values)
		{
		}

		private void ParamsMethod(string a = null, params int[] values)
		{
		}

		private void DifferenceInArgumentCount()
		{
		}

		private void DifferenceInArgumentCount(string a = "Hello")
		{
		}

		private void Test(int a = 10, string b = "Test")
		{
		}

		private void OnlyDifferenceIsLastArgument(int a, int b, string c = null)
		{
		}

		private void OnlyDifferenceIsLastArgument(int a, int b, double d = double.NegativeInfinity)
		{
		}

		private void OnlyDifferenceIsLastArgumentCastNecessary(int a, string b, string c = null)
		{
		}

		private void OnlyDifferenceIsLastArgumentCastNecessary(int a, string b, OptionalArguments args = null)
		{
		}

		private string Get(out int a)
		{
			throw null;
		}
	}
}
