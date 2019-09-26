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
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Controls;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Metadata
{
	internal class AssemblyRefTableTreeNode : ILSpyTreeNode
	{
		private PEFile module;

		public AssemblyRefTableTreeNode(PEFile module)
		{
			this.module = module;
		}

		public override object Text => $"23 AssemblyRef ({module.Metadata.GetTableRowCount(TableIndex.AssemblyRef)})";

		public override object Icon => Images.Literal;

		public override bool View(DecompilerTextView textView)
		{
			ListView view = Helpers.CreateListView("AssemblyRefView");

			var metadata = module.Metadata;

			var list = new List<AssemblyRefEntry>();

			foreach (var row in metadata.AssemblyReferences) {
				list.Add(new AssemblyRefEntry(module, row));
			}

			view.ItemsSource = list;

			textView.ShowContent(new[] { this }, view);
			return true;
		}

		struct AssemblyRefEntry
		{
			readonly int metadataOffset;
			readonly PEFile module;
			readonly MetadataReader metadata;
			readonly AssemblyReferenceHandle handle;
			readonly System.Reflection.Metadata.AssemblyReference assemblyRef;

			public int RID => MetadataTokens.GetRowNumber(handle);

			public int Token => MetadataTokens.GetToken(handle);

			public int Offset => metadataOffset
				+ metadata.GetTableMetadataOffset(TableIndex.AssemblyRef)
				+ metadata.GetTableRowSize(TableIndex.AssemblyRef) * (RID - 1);

			public Version Version => assemblyRef.Version;

			public int Flags => (int)assemblyRef.Flags;

			public string FlagsTooltip => null;// Helpers.AttributesToString(assemblyRef.Flags);

			public int PublicKeyOrToken => MetadataTokens.GetHeapOffset(assemblyRef.PublicKeyOrToken);

			public string PublicKeyOrTokenTooltip {
				get {
					if (assemblyRef.PublicKeyOrToken.IsNil)
						return null;
					System.Collections.Immutable.ImmutableArray<byte> token = metadata.GetBlobContent(assemblyRef.PublicKeyOrToken);
					return token.ToHexString(token.Length);
				}
			}

			public int NameStringHandle => MetadataTokens.GetHeapOffset(assemblyRef.Name);

			public string Name => metadata.GetString(assemblyRef.Name);

			public int CultureStringHandle => MetadataTokens.GetHeapOffset(assemblyRef.Culture);

			public string Culture => metadata.GetString(assemblyRef.Culture);

			public AssemblyRefEntry(PEFile module, AssemblyReferenceHandle handle)
			{
				this.metadataOffset = module.Reader.PEHeaders.MetadataStartOffset;
				this.module = module;
				this.metadata = module.Metadata;
				this.handle = handle;
				this.assemblyRef = metadata.GetAssemblyReference(handle);
			}
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, "AssemblyRef");
		}
	}
}