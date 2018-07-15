﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.Decompiler.Metadata;
using System.Reflection;
using ICSharpCode.Decompiler.TypeSystem;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.ILSpy.Search
{
	abstract class AbstractSearchStrategy
	{
		protected readonly string[] searchTerm;
		protected readonly Regex regex;
		protected readonly bool fullNameSearch;
		protected readonly Language language;
		protected readonly Action<SearchResult> addResult;

		protected AbstractSearchStrategy(Language language, Action<SearchResult> addResult, params string[] terms)
		{
			this.language = language;
			this.addResult = addResult;

			if (terms.Length == 1 && terms[0].Length > 2) {
				string search = terms[0];
				if (search.StartsWith("/", StringComparison.Ordinal) && search.Length > 4) {
					var regexString = search.Substring(1, search.Length - 1);
					fullNameSearch = search.Contains("\\.");
					if (regexString.EndsWith("/", StringComparison.Ordinal))
						regexString = regexString.Substring(0, regexString.Length - 1);
					regex = SafeNewRegex(regexString);
				} else {
					fullNameSearch = search.Contains(".");
				}
			}
			searchTerm = terms;
		}

		protected float CalculateFitness(IEntity member)
		{
			string text = member.Name;

			// Probably compiler generated types without meaningful names, show them last
			if (text.StartsWith("<")) {
				return 0;
			}

			// Constructors always have the same name in IL:
			// Use type name instead
			if (text == ".cctor" || text == ".ctor") {
				text = member.DeclaringType.Name;
			}

			// Ignore generic arguments, it not possible to search based on them either
			text = ReflectionHelper.SplitTypeParameterCountFromReflectionName(text);

			return 1.0f / text.Length;
		}

		protected virtual bool IsMatch(string entityName)
		{
			if (regex != null) {
				return regex.IsMatch(entityName);
			}

			for (int i = 0; i < searchTerm.Length; ++i) {
				// How to handle overlapping matches?
				var term = searchTerm[i];
				if (string.IsNullOrEmpty(term)) continue;
				string text = entityName;
				switch (term[0]) {
					case '+': // must contain
						term = term.Substring(1);
						goto default;
					case '-': // should not contain
						if (term.Length > 1 && text.IndexOf(term.Substring(1), StringComparison.OrdinalIgnoreCase) >= 0)
							return false;
						break;
					case '=': // exact match
						{
							var equalCompareLength = text.IndexOf('`');
							if (equalCompareLength == -1)
								equalCompareLength = text.Length;

							if (term.Length > 1 && String.Compare(term, 1, text, 0, Math.Max(term.Length, equalCompareLength),
								StringComparison.OrdinalIgnoreCase) != 0)
								return false;
						}
						break;
					case '~':
						if (term.Length > 1 && !IsNoncontiguousMatch(text.ToLower(), term.Substring(1).ToLower()))
							return false;
						break;
					default:
						if (text.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
							return false;
						break;
				}
			}
			return true;
		}

		bool IsNoncontiguousMatch(string text, string searchTerm)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerm)) {
				return false;
			}
			var textLength = text.Length;
			if (searchTerm.Length > textLength) {
				return false;
			}
			var i = 0;
			for (int searchIndex = 0; searchIndex < searchTerm.Length;) {
				while (i != textLength) {
					if (text[i] == searchTerm[searchIndex]) {
						// Check if all characters in searchTerm have been matched
						if (searchTerm.Length == ++searchIndex)
							return true;
						i++;
						break;
					}
					i++;
				}
				if (i == textLength)
					return false;
			}
			return false;
		}

		protected string GetLanguageSpecificName(IEntity member)
		{
			switch (member) {
				case ITypeDefinition t:
					return language.TypeToString(t, false);
				case IField f:
					return language.FieldToString(f, true, false);
				case IProperty p:
					return language.PropertyToString(p, true, false);
				case IMethod m:
					return language.MethodToString(m, true, false);
				case IEvent e:
					return language.EventToString(e, true, false);
				default:
					throw new NotSupportedException(member?.GetType() + " not supported!");
			}
		}

		protected ImageSource GetIcon(IEntity member)
		{
			switch (member) {
				case ITypeDefinition t:
					return TypeTreeNode.GetIcon(t);
				case IField f:
					return FieldTreeNode.GetIcon(f);
				case IProperty p:
					return PropertyTreeNode.GetIcon(p);
				case IMethod m:
					return MethodTreeNode.GetIcon(m);
				case IEvent e:
					return EventTreeNode.GetIcon(e);
				default:
					throw new NotSupportedException(member?.GetType() + " not supported!");
			}
		}

		public abstract void Search(PEFile module);

		Regex SafeNewRegex(string unsafePattern)
		{
			try {
				return new Regex(unsafePattern, RegexOptions.Compiled);
			} catch (ArgumentException) {
				return null;
			}
		}

		protected SearchResult ResultFromEntity(IEntity item)
		{
			var declaringType = item.DeclaringTypeDefinition;
			return new SearchResult {
				Member = item,
				Fitness = CalculateFitness(item),
				Image = GetIcon(item),
				Name = GetLanguageSpecificName(item),
				LocationImage = declaringType != null ? TypeTreeNode.GetIcon(declaringType) : Images.Namespace,
				Location = declaringType != null ? language.TypeToString(declaringType, includeNamespace: true) : item.Namespace
			};
		}
	}
}
