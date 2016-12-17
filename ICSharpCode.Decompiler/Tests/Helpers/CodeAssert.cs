﻿using System;
using System.Collections.Generic;
using System.IO;
using DiffLib;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Helpers
{
	public static class CodeAssert
	{
		public static void FilesAreEqual(string fileName1, string fileName2)
		{
			AreEqual(File.ReadAllText(fileName1), File.ReadAllText(fileName2));
		}

		public static void AreEqual(string input1, string input2)
		{
			var diff = new StringWriter();
			if (!CodeComparer.Compare(input1, input2, diff, CodeComparer.NormalizeLine)) {
				Assert.Fail(diff.ToString());
			}
		}
	}

	public static class CodeComparer
	{
		public static bool Compare(string input1, string input2, StringWriter diff, Func<string, string> normalizeLine)
		{
			var differ = new AlignedDiff<string>(
				NormalizeAndSplitCode(input1),
				NormalizeAndSplitCode(input2),
				new CodeLineEqualityComparer(normalizeLine),
				new StringSimilarityComparer(),
				new StringAlignmentFilter());

			bool result = true, ignoreChange;

			int line1 = 0, line2 = 0;

			foreach (var change in differ.Generate()) {
				switch (change.Change) {
					case ChangeType.Same:
						diff.Write("{0,4} {1,4} ", ++line1, ++line2);
						diff.Write("  ");
						diff.WriteLine(change.Element1);
						break;
					case ChangeType.Added:
						diff.Write("     {1,4} ", line1, ++line2);
						result &= ignoreChange = ShouldIgnoreChange(change.Element2);
						diff.Write(ignoreChange ? "    " : " +  ");
						diff.WriteLine(change.Element2);
						break;
					case ChangeType.Deleted:
						diff.Write("{0,4}      ", ++line1, line2);
						result &= ignoreChange = ShouldIgnoreChange(change.Element1);
						diff.Write(ignoreChange ? "    " : " -  ");
						diff.WriteLine(change.Element1);
						break;
					case ChangeType.Changed:
						diff.Write("{0,4}      ", ++line1, line2);
						result = false;
						diff.Write("(-) ");
						diff.WriteLine(change.Element1);
						diff.Write("     {1,4} ", line1, ++line2);
						diff.Write("(+) ");
						diff.WriteLine(change.Element2);
						break;
				}
			}

			return result;
		}

		class CodeLineEqualityComparer : IEqualityComparer<string>
		{
			private IEqualityComparer<string> baseComparer = EqualityComparer<string>.Default;
			private Func<string, string> normalizeLine;

			public CodeLineEqualityComparer(Func<string, string> normalizeLine)
			{
				this.normalizeLine = normalizeLine;
			}

			public bool Equals(string x, string y)
			{
				return baseComparer.Equals(
					normalizeLine(x),
					normalizeLine(y)
				);
			}

			public int GetHashCode(string obj)
			{
				return baseComparer.GetHashCode(NormalizeLine(obj));
			}
		}

		public static string NormalizeLine(string line)
		{
			line = line.Trim();
			var index = line.IndexOf("//", StringComparison.Ordinal);
			if (index >= 0) {
				return line.Substring(0, index);
			} else if (line.StartsWith("#", StringComparison.Ordinal)) {
				return string.Empty;
			} else {
				return line;
			}
		}

		private static bool ShouldIgnoreChange(string line)
		{
			// for the result, we should ignore blank lines and added comments
			return NormalizeLine(line) == string.Empty;
		}

		private static IEnumerable<string> NormalizeAndSplitCode(string input)
		{
			return input.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
