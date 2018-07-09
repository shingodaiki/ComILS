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
using System.Linq;
using System.Reflection.Metadata;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedEventTreeNode : AnalyzerEntityTreeNode
	{
		readonly Decompiler.Metadata.PEFile module;
		readonly EventDefinitionHandle analyzedEvent;
		readonly string prefix;

		public AnalyzedEventTreeNode(Decompiler.Metadata.PEFile module, EventDefinitionHandle analyzedEvent, string prefix = "")
		{
			if (analyzedEvent == null)
				throw new ArgumentNullException(nameof(analyzedEvent));
			this.module = module;
			this.analyzedEvent = analyzedEvent;
			this.prefix = prefix;
			this.LazyLoading = true;
		}

		public override Decompiler.Metadata.IMetadataEntity Member => new Decompiler.Metadata.EventDefinition(module, analyzedEvent);

		public override object Icon
		{
			get { return EventTreeNode.GetIcon(new Decompiler.Metadata.EventDefinition(module, analyzedEvent)); }
		}

		// TODO: This way of formatting is not suitable for events which explicitly implement interfaces.
		public override object Text => prefix + Language.EventToString(new Decompiler.Metadata.EventDefinition(module, analyzedEvent), includeTypeName: true, includeNamespace: true);

		protected override void LoadChildren()
		{
			if (analyzedEvent.AddMethod != null)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(analyzedEvent.AddMethod, "add"));
			
			if (analyzedEvent.RemoveMethod != null)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(analyzedEvent.RemoveMethod, "remove"));
			
			foreach (var accessor in analyzedEvent.OtherMethods)
				this.Children.Add(new AnalyzedEventAccessorTreeNode(accessor, null));

			if (AnalyzedEventFiredByTreeNode.CanShow(analyzedEvent))
				this.Children.Add(new AnalyzedEventFiredByTreeNode(analyzedEvent));

			if (AnalyzedEventOverridesTreeNode.CanShow(analyzedEvent))
				this.Children.Add(new AnalyzedEventOverridesTreeNode(analyzedEvent));
			
			if (AnalyzedInterfaceEventImplementedByTreeNode.CanShow(analyzedEvent))
				this.Children.Add(new AnalyzedInterfaceEventImplementedByTreeNode(analyzedEvent));
		}

		/*public static AnalyzerTreeNode TryCreateAnalyzer(MemberReference member)
		{
			if (CanShow(member))
				return new AnalyzedEventTreeNode(member as EventDefinition);
			else
				return null;
		}

		public static bool CanShow(IMemberReference member)
		{
			if (!(member is EventDefinition eventDef))
				return false;

			return !MainWindow.Instance.CurrentLanguage.ShowMember(eventDef.GetAccessors().First().Method)
				|| AnalyzedEventOverridesTreeNode.CanShow(eventDef);
		}*/
	}
}
