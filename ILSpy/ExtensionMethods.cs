﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using Mono.Cecil;
using ICSharpCode.ILSpy.Options;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// ExtensionMethods used in ILSpy.
	/// </summary>
	public static class ExtensionMethods
	{
		public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> items)
		{
			foreach (T item in items)
				if (!list.Contains(item))
					list.Add(item);
		}
		
		public static int BinarySearch<T>(this IList<T> list, T item, int start, int count, IComparer<T> comparer)
		{
			if (list == null)
				throw new ArgumentNullException("list");
			if (start < 0 || start >= list.Count)
				throw new ArgumentOutOfRangeException("start", start, "Value must be between 0 and " + (list.Count - 1));
			if (count < 0 || count > list.Count - start)
				throw new ArgumentOutOfRangeException("count", count, "Value must be between 0 and " + (list.Count - start));
			int end = start + count - 1;
			while (start <= end) {
				int pivot = (start + end) / 2;
				int result = comparer.Compare(item, list[pivot]);
				if (result == 0)
					return pivot;
				if (result < 0)
					end = pivot - 1;
				else
					start = pivot + 1;
			}
			return ~start;
		}

		public static bool IsCustomAttribute(this TypeDefinition type)
		{
			while (type.FullName != "System.Object") {
				var resolvedBaseType = type.BaseType.Resolve();
				if (resolvedBaseType == null)
					return false;
				if (resolvedBaseType.FullName == "System.Attribute")
					return true;
				type = resolvedBaseType;
			}
			return false;
		}
		
		public static string ToSuffixString(this MetadataToken token)
		{
			if (!DisplaySettingsPanel.CurrentDisplaySettings.ShowMetadataTokens)
				return string.Empty;
			
			return " @" + token.ToInt32().ToString("x8");
		}
	}
}
