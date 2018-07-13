﻿// Copyright (c) 2017 Siegfried Pammer
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
using SRM = System.Reflection.Metadata;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;
using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	public sealed class PinnedType : TypeWithElementType
	{
		public PinnedType(IType elementType)
			: base(elementType)
		{
		}

		public override string NameSuffix => " pinned";

		public override bool? IsReferenceType => elementType.IsReferenceType;
		public override bool IsByRefLike => elementType.IsByRefLike;

		public override TypeKind Kind => TypeKind.Other;

		public override IType VisitChildren(TypeVisitor visitor)
		{
			var newType = elementType.AcceptVisitor(visitor);
			if (newType == elementType)
				return this;
			return new PinnedType(newType);
		}
	}

	public sealed class PinnedTypeReference : ITypeReference
	{
		public ITypeReference ElementType { get; }

		public PinnedTypeReference(ITypeReference elementType)
		{
			ElementType = elementType;
		}

		public IType Resolve(ITypeResolveContext context)
		{
			return new PinnedType(ElementType.Resolve(context));
		}
	}

	public sealed class ModifiedTypeReference : ITypeReference
	{
		public ITypeReference ElementType { get; }
		public ITypeReference ModifierType { get; }
		public bool IsRequired { get; }

		public ModifiedTypeReference(ITypeReference elementType, ITypeReference modifierType, bool isRequired)
		{
			ElementType = elementType;
			ModifierType = modifierType;
			IsRequired = isRequired;
		}

		public IType Resolve(ITypeResolveContext context)
		{
			return ElementType.Resolve(context);
		}
	}
	
	sealed class TypeDefTokenTypeReference : ITypeReference
	{
		readonly SRM.TypeDefinitionHandle token;

		public TypeDefTokenTypeReference(SRM.TypeDefinitionHandle token)
		{
			this.token = token;
		}

		public IType Resolve(ITypeResolveContext context)
		{
			ITypeDefinition td = ((MetadataAssembly)context.CurrentAssembly).GetDefinition(token);
			if (td != null)
				return td;
			return SpecialType.UnknownType;
		}
	}

	sealed class TypeSpecTypeReference : ITypeReference
	{
		readonly SRM.TypeSpecification typeSpec;

		public TypeSpecTypeReference(SRM.TypeSpecification typeSpec)
		{
			this.typeSpec = typeSpec;
		}

		public IType Resolve(ITypeResolveContext context)
		{
			return typeSpec.DecodeSignature(new TypeProvider(context.CurrentAssembly), new GenericContext(context));
		}
	}
}
