// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Base class for statements.
	/// </summary>
	/// <remarks>
	/// This class is useful even though it doesn't provide any additional functionality:
	/// It can be used to communicate more information in APIs, e.g. "this subnode will always be a statement"
	/// </remarks>
	public abstract class Statement : AstNode
	{
		#region Null
		public new static readonly Statement Null = new NullStatement ();
		
		sealed class NullStatement : Statement
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		public new Statement Clone()
		{
			return (Statement)base.Clone();
		}
		
		public Statement ReplaceWith(Func<Statement, Statement> replaceFunction)
		{
			if (replaceFunction == null)
				throw new ArgumentNullException("replaceFunction");
			return (Statement)base.ReplaceWith(node => replaceFunction((Statement)node));
		}
		
		public override NodeType NodeType {
			get { return NodeType.Statement; }
		}
		
		// Make debugging easier by giving Statements a ToString() implementation
		public override string ToString()
		{
			if (IsNull)
				return "Null";
			StringWriter w = new StringWriter();
			AcceptVisitor(new OutputVisitor(w, new CSharpFormattingPolicy()), null);
			string text = w.ToString().TrimEnd().Replace("\t", "").Replace(w.NewLine, " ");
			if (text.Length > 100)
				return text.Substring(0, 97) + "...";
			else
				return text;
		}
	}
}
