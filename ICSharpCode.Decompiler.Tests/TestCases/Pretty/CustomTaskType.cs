﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	public class CustomTaskType
	{
		private int memberField;

		public async ValueTask SimpleVoidTaskMethod()
		{
			Console.WriteLine("Before");
			await Task.Delay(TimeSpan.FromSeconds(1.0));
			Console.WriteLine("After");
		}

		public async ValueTask TaskMethodWithoutAwait()
		{
			Console.WriteLine("No Await");
		}

		public async ValueTask CapturingThis()
		{
			await Task.Delay(memberField);
		}

		public async ValueTask CapturingThisWithoutAwait()
		{
			Console.WriteLine(memberField);
		}

		public async ValueTask<bool> SimpleBoolTaskMethod()
		{
			Console.WriteLine("Before");
			await Task.Delay(TimeSpan.FromSeconds(1.0));
			Console.WriteLine("After");
			return true;
		}

		public async void TwoAwaitsWithDifferentAwaiterTypes()
		{
			Console.WriteLine("Before");
			if (await SimpleBoolTaskMethod()) {
				await Task.Delay(TimeSpan.FromSeconds(1.0));
			}
			Console.WriteLine("After");
		}

		public async void AwaitInLoopCondition()
		{
			while (await SimpleBoolTaskMethod()) {
				Console.WriteLine("Body");
			}
		}

		public async ValueTask AwaitInCatch(bool b, ValueTask<int> task1, ValueTask<int> task2)
		{
			try {
				Console.WriteLine("Start try");
				await task1;
				Console.WriteLine("End try");
			} catch (Exception) {
				if (!b) {
					await task2;
				} else {
					Console.WriteLine("No await");
				}
			}
		}

		public async ValueTask AwaitInFinally(bool b, ValueTask<int> task1, ValueTask<int> task2)
		{
			try {
				Console.WriteLine("Start try");
				await task1;
				Console.WriteLine("End try");
			} finally {
				if (!b) {
					await task2;
				} else {
					Console.WriteLine("No await");
				}
			}
		}

		public static async ValueTask<int> GetIntegerSumAsync(IEnumerable<int> items)
		{
			await Task.Delay(100);
			int num = 0;
			foreach (int item in items) {
				num += item;
			}
			return num;
		}

		public static Func<ValueTask<int>> AsyncLambda()
		{
			return async () => await GetIntegerSumAsync(new int[3] {
				1,
				2,
				3
			});
		}

		public static Func<ValueTask<int>> AsyncDelegate()
		{
			return async delegate {
				await Task.Delay(10);
				return 2;
			};
		}

		public static async ValueTask<int> AsyncLocalFunctions()
		{
			return await Nested(1) + await Nested(2);

			async ValueTask<int> Nested(int i)
			{
				await Task.Delay(i);
				return i;
			}
		}
	}
}
