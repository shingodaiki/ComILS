﻿// Copyright (c) 2014 Daniel Grunwald
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

using System.Reflection.Metadata;
using ICSharpCode.Decompiler.Metadata;

namespace ICSharpCode.Decompiler.TypeSystem
{
	/// <summary>
	/// Allows resolving cecil types into the NRefactory type system.
	/// </summary>
	public interface IDecompilerTypeSystem
	{
		ICompilation Compilation { get; }

		IType ResolveFromSignature(ITypeReference typeReference);
		IType ResolveAsType(EntityHandle typeReference);
		IField ResolveAsField(EntityHandle fieldReference);
		IMethod ResolveAsMethod(EntityHandle methodReference);
		IMember ResolveAsMember(EntityHandle memberReference);

		MetadataReader GetMetadata();
		PEFile ModuleDefinition { get; }
		PEFile GetModuleDefinition(IAssembly assembly);

		/// <summary>
		/// Gets a type system instance that automatically specializes the results
		/// of each Resolve() call with the provided substitution.
		/// </summary>
		IDecompilerTypeSystem GetSpecializingTypeSystem(TypeParameterSubstitution substitution);
	}
}
