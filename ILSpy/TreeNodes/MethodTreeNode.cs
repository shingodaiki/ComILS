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
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Windows.Media;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using SRM = System.Reflection.Metadata;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Tree Node representing a field, method, property, or event.
	/// </summary>
	public sealed class MethodTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		public IMethod MethodDefinition { get; }

		public MethodTreeNode(IMethod method)
		{
			this.MethodDefinition = method ?? throw new ArgumentNullException(nameof(method));
		}

		public override object Text => GetText(MethodDefinition, Language) + MethodDefinition.MetadataToken.ToSuffixString();

		public static object GetText(IMethod method, Language language)
		{
			return language.MethodToString(method, false, false);
		}

		public override object Icon => GetIcon(MethodDefinition);

		public static ImageSource GetIcon(IMethod method)
		{
			var metadata = ((MetadataAssembly)method.ParentAssembly).PEFile.Metadata;
			var methodDefinition = metadata.GetMethodDefinition((MethodDefinitionHandle)method.MetadataToken);
			var methodName = metadata.GetString(methodDefinition.Name);
			if (methodDefinition.HasFlag(MethodAttributes.SpecialName) && methodName.StartsWith("op_", StringComparison.Ordinal)) {
				return Images.GetIcon(MemberIcon.Operator, GetOverlayIcon(methodDefinition.Attributes), false);
			}

			if (methodDefinition.IsExtensionMethod(metadata)) {
				return Images.GetIcon(MemberIcon.ExtensionMethod, GetOverlayIcon(methodDefinition.Attributes), false);
			}

			if (methodDefinition.HasFlag(MethodAttributes.SpecialName) &&
				(methodName == ".ctor" || methodName == ".cctor")) {
				return Images.GetIcon(MemberIcon.Constructor, GetOverlayIcon(methodDefinition.Attributes), methodDefinition.HasFlag(MethodAttributes.Static));
			}

			if (methodDefinition.HasFlag(MethodAttributes.PinvokeImpl) && !methodDefinition.GetImport().Module.IsNil)
				return Images.GetIcon(MemberIcon.PInvokeMethod, GetOverlayIcon(methodDefinition.Attributes), true);

			bool showAsVirtual = methodDefinition.HasFlag(MethodAttributes.Virtual)
				&& !(methodDefinition.HasFlag(MethodAttributes.NewSlot) && methodDefinition.HasFlag(MethodAttributes.Final))
				&& (metadata.GetTypeDefinition(methodDefinition.GetDeclaringType()).Attributes & TypeAttributes.ClassSemanticsMask) != TypeAttributes.Interface;

			return Images.GetIcon(
				showAsVirtual ? MemberIcon.VirtualMethod : MemberIcon.Method,
				GetOverlayIcon(methodDefinition.Attributes),
				methodDefinition.HasFlag(MethodAttributes.Static));
		}

		private static AccessOverlayIcon GetOverlayIcon(MethodAttributes methodAttributes)
		{
			switch (methodAttributes & MethodAttributes.MemberAccessMask) {
				case MethodAttributes.Public:
					return AccessOverlayIcon.Public;
				case MethodAttributes.Assembly:
					return AccessOverlayIcon.Internal;
				case MethodAttributes.FamANDAssem:
					return AccessOverlayIcon.PrivateProtected;
				case MethodAttributes.Family:
					return AccessOverlayIcon.Protected;
				case MethodAttributes.FamORAssem:
					return AccessOverlayIcon.ProtectedInternal;
				case MethodAttributes.Private:
					return AccessOverlayIcon.Private;
				case 0:
					return AccessOverlayIcon.CompilerControlled;
				default:
					throw new NotSupportedException();
			}
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileMethod(MethodDefinition, output, options);
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			if (!settings.ShowInternalApi && !IsPublicAPI)
				return FilterResult.Hidden;
			if (settings.SearchTermMatches(MethodDefinition.Name) && settings.Language.ShowMember(MethodDefinition))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		public override bool IsPublicAPI {
			get {
				switch (MethodDefinition.Accessibility) {
					case Accessibility.Public:
					case Accessibility.Protected:
					case Accessibility.ProtectedOrInternal:
						return true;
					default:
						return false;
				}
			}
		}

		IEntity IMemberTreeNode.Member => MethodDefinition;
	}
}
