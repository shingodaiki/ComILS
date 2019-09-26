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
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Metadata
{
	class DeclSecurityTableTreeNode : ILSpyTreeNode
	{
		private PEFile module;

		public DeclSecurityTableTreeNode(PEFile module)
		{
			this.module = module;
		}

		public override object Text => $"0E DeclSecurity ({module.Metadata.GetTableRowCount(TableIndex.DeclSecurity)})";

		public override object Icon => Images.Literal;

		public override bool View(DecompilerTextView textView)
		{
			ListView view = Helpers.CreateListView("DeclSecurityAttrsView");
			var metadata = module.Metadata;

			var list = new List<DeclSecurityEntry>();

			foreach (var row in metadata.DeclarativeSecurityAttributes) {
				list.Add(new DeclSecurityEntry(module, row));
			}

			view.ItemsSource = list;

			textView.ShowContent(new[] { this }, view);
			return true;
		}

		struct DeclSecurityEntry
		{
			readonly int metadataOffset;
			readonly PEFile module;
			readonly MetadataReader metadata;
			readonly DeclarativeSecurityAttributeHandle handle;
			readonly DeclarativeSecurityAttribute declSecAttr;

			public int RID => MetadataTokens.GetRowNumber(handle);

			public int Token => MetadataTokens.GetToken(handle);

			public int Offset => metadataOffset
				+ metadata.GetTableMetadataOffset(TableIndex.DeclSecurity)
				+ metadata.GetTableRowSize(TableIndex.DeclSecurity) * (RID - 1);

			public int ParentHandle => MetadataTokens.GetToken(declSecAttr.Parent);

			public string ParentTooltip {
				get {
					ITextOutput output = new PlainTextOutput();
					var context = new GenericContext(default(TypeDefinitionHandle), module);
					declSecAttr.Parent.WriteTo(module, output, context);
					return output.ToString();
				}
			}

			public int Action => (int)declSecAttr.Action;

			public string ActionTooltip {
				get {
					return null;
				}
			}

			public int PermissionSetHandle => MetadataTokens.GetHeapOffset(declSecAttr.PermissionSet);

			public string PermissionSetTooltip {
				get {
					return null;
				}
			}

			public DeclSecurityEntry(PEFile module, DeclarativeSecurityAttributeHandle handle)
			{
				this.metadataOffset = module.Reader.PEHeaders.MetadataStartOffset;
				this.module = module;
				this.metadata = module.Metadata;
				this.handle = handle;
				this.declSecAttr = metadata.GetDeclarativeSecurityAttribute(handle);
			}
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, "DeclSecurityAttrs");
		}
	}
}
