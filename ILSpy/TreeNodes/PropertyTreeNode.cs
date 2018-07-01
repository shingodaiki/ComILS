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
using System.Reflection;
using System.Reflection.Metadata;
using System.Windows.Media;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using SRM = System.Reflection.Metadata;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Represents a property in the TreeView.
	/// </summary>
	public sealed class PropertyTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly bool isIndexer;

		public PropertyTreeNode(IProperty property)
		{
			this.PropertyDefinition = property ?? throw new ArgumentNullException(nameof(property));
			this.isIndexer = property.IsIndexer;

			if (property.CanGet)
				this.Children.Add(new MethodTreeNode(property.Getter));
			if (property.CanSet)
				this.Children.Add(new MethodTreeNode(property.Setter));
			/*foreach (var m in property.OtherMethods)
				this.Children.Add(new MethodTreeNode(m));*/
		}

		public IProperty PropertyDefinition { get; }

		public override object Text => GetText(PropertyDefinition, Language, isIndexer) + PropertyDefinition.MetadataToken.ToSuffixString();

		public static object GetText(IProperty property, Language language, bool? isIndexer = null)
		{
			return language.PropertyToString(property, false, false, isIndexer);
		}

		public override object Icon => GetIcon(PropertyDefinition);

		public static ImageSource GetIcon(IProperty property, bool isIndexer = false)
		{
			MemberIcon icon = isIndexer ? MemberIcon.Indexer : MemberIcon.Property;
			MethodAttributes attributesOfMostAccessibleMethod = GetAttributesOfMostAccessibleMethod(property);
			bool isStatic = (attributesOfMostAccessibleMethod & MethodAttributes.Static) != 0;
			return Images.GetIcon(icon, GetOverlayIcon(attributesOfMostAccessibleMethod), isStatic);
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

		private static MethodAttributes GetAttributesOfMostAccessibleMethod(IProperty property)
		{
			// There should always be at least one method from which to
			// obtain the result, but the compiler doesn't know this so
			// initialize the result with a default value
			MethodAttributes result = (MethodAttributes)0;

			// Method access is defined from inaccessible (lowest) to public (highest)
			// in numeric order, so we can do an integer comparison of the masked attribute
			int accessLevel = 0;

			var metadata = ((MetadataAssembly)property.ParentAssembly).PEFile.Metadata;
			var propertyDefinition = metadata.GetPropertyDefinition((PropertyDefinitionHandle)property.MetadataToken);
			var accessors = propertyDefinition.GetAccessors();

			if (!accessors.Getter.IsNil) {
				var getter = metadata.GetMethodDefinition(accessors.Getter);
				int methodAccessLevel = (int)(getter.Attributes & MethodAttributes.MemberAccessMask);
				if (accessLevel < methodAccessLevel) {
					accessLevel = methodAccessLevel;
					result = getter.Attributes;
				}
			}

			if (!accessors.Setter.IsNil) {
				var setter = metadata.GetMethodDefinition(accessors.Setter);
				int methodAccessLevel = (int)(setter.Attributes & MethodAttributes.MemberAccessMask);
				if (accessLevel < methodAccessLevel) {
					accessLevel = methodAccessLevel;
					result = setter.Attributes;
				}
			}

			/*foreach (var m in property.OtherMethods) {
				int methodAccessLevel = (int)(m.Attributes & MethodAttributes.MemberAccessMask);
				if (accessLevel < methodAccessLevel) {
					accessLevel = methodAccessLevel;
					result = m.Attributes;
				}
			}*/

			return result;
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			if (!settings.ShowInternalApi && !IsPublicAPI)
				return FilterResult.Hidden;
			if (settings.SearchTermMatches(PropertyDefinition.Name) && settings.Language.ShowMember(PropertyDefinition))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileProperty(PropertyDefinition, output, options);
		}

		public override bool IsPublicAPI {
			get {
				switch (GetAttributesOfMostAccessibleMethod(PropertyDefinition) & MethodAttributes.MemberAccessMask) {
					case MethodAttributes.Public:
					case MethodAttributes.Family:
					case MethodAttributes.FamORAssem:
						return true;
					default:
						return false;
				}
			}
		}

		IEntity IMemberTreeNode.Member => PropertyDefinition;
	}
}
