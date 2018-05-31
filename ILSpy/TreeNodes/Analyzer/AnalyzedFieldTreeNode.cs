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
using System.Reflection.Metadata;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	class AnalyzedFieldTreeNode : AnalyzerEntityTreeNode
	{
		readonly Decompiler.Metadata.PEFile module;
		readonly FieldDefinitionHandle analyzedField;

		public AnalyzedFieldTreeNode(Decompiler.Metadata.PEFile module, FieldDefinitionHandle analyzedField)
		{
			if (analyzedField.IsNil)
				throw new ArgumentNullException(nameof(analyzedField));
			this.module = module;
			this.analyzedField = analyzedField;
			this.LazyLoading = true;
		}

		public override object Icon => FieldTreeNode.GetIcon(new Decompiler.Metadata.FieldDefinition(module, analyzedField));

		public override object Text => Language.FieldToString(new Decompiler.Metadata.FieldDefinition(module, analyzedField), true, true);

		protected override void LoadChildren()
		{
			this.Children.Add(new AnalyzedFieldAccessTreeNode(module, analyzedField, false));
			var fd = module.Metadata.GetFieldDefinition(analyzedField);
			if (!fd.HasFlag(System.Reflection.FieldAttributes.Literal))
				this.Children.Add(new AnalyzedFieldAccessTreeNode(module, analyzedField, true));
		}

		public override Decompiler.Metadata.IMetadataEntity Member => new Decompiler.Metadata.FieldDefinition(module, analyzedField);
	}
}
