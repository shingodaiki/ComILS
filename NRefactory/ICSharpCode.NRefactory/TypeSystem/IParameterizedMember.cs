﻿// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a method or property.
	/// </summary>
	[ContractClass(typeof(IParameterizedMemberContract))]
	public interface IParameterizedMember : IMember
	{
		IList<IParameter> Parameters { get; }
	}
	
	[ContractClassFor(typeof(IParameterizedMember))]
	abstract class IParameterizedMemberContract : IMemberContract, IParameterizedMember
	{
		IList<IParameter> IParameterizedMember.Parameters {
			get {
				Contract.Ensures(Contract.Result<IList<IParameter>>() != null);
				return null;
			}
		}
	}
}
