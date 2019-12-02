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
	class FileTableTreeNode : ILSpyTreeNode
	{
		private PEFile module;

		public FileTableTreeNode(PEFile module)
		{
			this.module = module;
		}

		public override object Text => $"26 File ({module.Metadata.GetTableRowCount(TableIndex.File)})";

		public override object Icon => Images.Literal;

		public override bool View(ViewModels.TabPageModel tabPage)
		{
			tabPage.SupportsLanguageSwitching = false;
			ListView view = Helpers.CreateListView("FilesView");
			var metadata = module.Metadata;

			var list = new List<FileEntry>();

			foreach (var row in metadata.AssemblyFiles) {
				list.Add(new FileEntry(module, row));
			}

			view.ItemsSource = list;

			tabPage.Content = view;
			return true;
		}

		struct FileEntry
		{
			readonly int metadataOffset;
			readonly PEFile module;
			readonly MetadataReader metadata;
			readonly AssemblyFileHandle handle;
			readonly AssemblyFile assemblyFile;

			public int RID => MetadataTokens.GetRowNumber(handle);

			public int Token => MetadataTokens.GetToken(handle);

			public int Offset => metadataOffset
				+ metadata.GetTableMetadataOffset(TableIndex.File)
				+ metadata.GetTableRowSize(TableIndex.File) * (RID - 1);

			public int Attributes => assemblyFile.ContainsMetadata ? 1 : 0;

			public string AttributesTooltip => assemblyFile.ContainsMetadata ? "ContainsMetaData" : "ContainsNoMetaData";

			public int NameStringHandle => MetadataTokens.GetHeapOffset(assemblyFile.Name);

			public string Name => metadata.GetString(assemblyFile.Name);

			public int HashValue => MetadataTokens.GetHeapOffset(assemblyFile.HashValue);

			public string HashValueTooltip {
				get {
					if (assemblyFile.HashValue.IsNil)
						return null;
					System.Collections.Immutable.ImmutableArray<byte> token = metadata.GetBlobContent(assemblyFile.HashValue);
					return token.ToHexString(token.Length);
				}
			}

			public FileEntry(PEFile module, AssemblyFileHandle handle)
			{
				this.metadataOffset = module.Reader.PEHeaders.MetadataStartOffset;
				this.module = module;
				this.metadata = module.Metadata;
				this.handle = handle;
				this.assemblyFile = metadata.GetAssemblyFile(handle);
			}
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, "Files");
		}
	}
}
