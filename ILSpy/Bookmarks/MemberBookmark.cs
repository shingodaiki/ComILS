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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Bookmarks
{
	/// <summary>
	/// Bookmark used to give additional operations for class members.
	/// Does not derive from SDBookmark because it is not stored in the central BookmarkManager,
	/// but only in the document's BookmarkManager.
	/// </summary>
	public class MemberBookmark : IBookmark
	{
		MemberReference member;
		
		public MemberReference Member {
			get {
				return member;
			}
		}
		
		public MemberBookmark(MemberReference member, int line)
		{
			this.member = member;
			LineNumber = line;
		}
		
		public virtual ImageSource Image {
			get {
				if (member is FieldDefinition)
					return GetMemberOverlayedImage(member, MemberIcon.Field);
				
				if (member is PropertyDefinition)
					return GetMemberOverlayedImage(member, MemberIcon.Property);
				
				if (member is EventDefinition)
					return GetMemberOverlayedImage(member, MemberIcon.Event);
				
				return GetMemberOverlayedImage(member, MemberIcon.Method);
			}
		}
		
		ImageSource GetMemberOverlayedImage(MemberReference member, MemberIcon icon)
		{
			if (member is FieldDefinition)
				return Images.GetIcon(icon, ((FieldDefinition)member).IsPublic ? AccessOverlayIcon.Public : AccessOverlayIcon.Private, false);
			
			if (member is PropertyDefinition)
				return Images.GetIcon(icon, AccessOverlayIcon.Public, false);
			
			if (member is EventDefinition)
				return Images.GetIcon(icon, AccessOverlayIcon.Public, false);
			
			return Images.GetIcon(icon, ((MethodDefinition)member).IsPublic ? AccessOverlayIcon.Public : AccessOverlayIcon.Private, false);
		}
		
		public int LineNumber {
			get; private set;
		}
		
		public virtual void MouseDown(MouseButtonEventArgs e)
		{
		}
		
		public virtual void MouseUp(MouseButtonEventArgs e)
		{
		}
		
		int IBookmark.ZOrder {
			get { return -10; }
		}
		
		bool IBookmark.CanDragDrop {
			get { return false; }
		}
		
		void IBookmark.Drop(int lineNumber)
		{
			throw new NotSupportedException();
		}
	}
	
	public class TypeBookmark : MemberBookmark
	{
		public TypeBookmark(MemberReference member, int line) : base (member, line)
		{
		}
		
		public override ImageSource Image {
			get {
				if (Member is TypeDefinition) {
					var type = Member as TypeDefinition;
					if (type.IsEnum)
						return GetTypeOverlayedImage(type, TypeIcon.Enum);
					if (type.IsValueType)
						return GetTypeOverlayedImage(type, TypeIcon.Struct);
					if (type.IsInterface)
						return GetTypeOverlayedImage(type, TypeIcon.Interface);
					
					return GetTypeOverlayedImage(type, TypeIcon.Class);
				}
				
				return null;
			}
		}
		
		ImageSource GetTypeOverlayedImage(TypeDefinition type, TypeIcon icon)
		{
			if (type.IsNotPublic)
				return Images.GetIcon(icon, AccessOverlayIcon.Private);
			
			return Images.GetIcon(icon, AccessOverlayIcon.Public);
		}
	}
}
