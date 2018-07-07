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
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.DebugInfo;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Disassembles type and member definitions.
	/// </summary>
	public sealed class ReflectionDisassembler
	{
		readonly ITextOutput output;
		CancellationToken cancellationToken;
		bool isInType;   // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
		MethodBodyDisassembler methodBodyDisassembler;

		public bool DetectControlStructure {
			get => methodBodyDisassembler.DetectControlStructure;
			set => methodBodyDisassembler.DetectControlStructure = value;
		}

		public bool ShowSequencePoints {
			get => methodBodyDisassembler.ShowSequencePoints;
			set => methodBodyDisassembler.ShowSequencePoints = value;
		}

		public bool ShowMetadataTokens {
			get => methodBodyDisassembler.ShowMetadataTokens;
			set => methodBodyDisassembler.ShowMetadataTokens = value;
		}

		public IDebugInfoProvider DebugInfo {
			get => methodBodyDisassembler.DebugInfo;
			set => methodBodyDisassembler.DebugInfo = value;
		}

		public bool ExpandMemberDefinitions { get; set; } = false;

		public IAssemblyResolver AssemblyResolver { get; set; }

		public ReflectionDisassembler(ITextOutput output, CancellationToken cancellationToken)
			: this(output, new MethodBodyDisassembler(output, cancellationToken), cancellationToken)
		{
		}

		public ReflectionDisassembler(ITextOutput output, MethodBodyDisassembler methodBodyDisassembler, CancellationToken cancellationToken)
		{
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			this.output = output;
			this.cancellationToken = cancellationToken;
			this.methodBodyDisassembler = methodBodyDisassembler;
		}

		#region Disassemble Method
		EnumNameCollection<MethodAttributes> methodAttributeFlags = new EnumNameCollection<MethodAttributes>() {
			{ MethodAttributes.Final, "final" },
			{ MethodAttributes.HideBySig, "hidebysig" },
			{ MethodAttributes.SpecialName, "specialname" },
			{ MethodAttributes.PinvokeImpl, null },	// handled separately
			{ MethodAttributes.UnmanagedExport, "export" },
			{ MethodAttributes.RTSpecialName, "rtspecialname" },
			{ MethodAttributes.RequireSecObject, "reqsecobj" },
			{ MethodAttributes.NewSlot, "newslot" },
			{ MethodAttributes.CheckAccessOnOverride, "strict" },
			{ MethodAttributes.Abstract, "abstract" },
			{ MethodAttributes.Virtual, "virtual" },
			{ MethodAttributes.Static, "static" },
			{ MethodAttributes.HasSecurity, null },	// ?? also invisible in ILDasm
		};

		EnumNameCollection<MethodAttributes> methodVisibility = new EnumNameCollection<MethodAttributes>() {
			{ MethodAttributes.Private, "private" },
			{ MethodAttributes.FamANDAssem, "famandassem" },
			{ MethodAttributes.Assembly, "assembly" },
			{ MethodAttributes.Family, "family" },
			{ MethodAttributes.FamORAssem, "famorassem" },
			{ MethodAttributes.Public, "public" },
		};

		EnumNameCollection<SignatureCallingConvention> callingConvention = new EnumNameCollection<SignatureCallingConvention>() {
			{ SignatureCallingConvention.CDecl, "unmanaged cdecl" },
			{ SignatureCallingConvention.StdCall, "unmanaged stdcall" },
			{ SignatureCallingConvention.ThisCall, "unmanaged thiscall" },
			{ SignatureCallingConvention.FastCall, "unmanaged fastcall" },
			{ SignatureCallingConvention.VarArgs, "vararg" },
			{ SignatureCallingConvention.Default, null },
		};

		EnumNameCollection<MethodImplAttributes> methodCodeType = new EnumNameCollection<MethodImplAttributes>() {
			{ MethodImplAttributes.IL, "cil" },
			{ MethodImplAttributes.Native, "native" },
			{ MethodImplAttributes.OPTIL, "optil" },
			{ MethodImplAttributes.Runtime, "runtime" },
		};

		EnumNameCollection<MethodImplAttributes> methodImpl = new EnumNameCollection<MethodImplAttributes>() {
			{ MethodImplAttributes.Synchronized, "synchronized" },
			{ MethodImplAttributes.NoInlining, "noinlining" },
			{ MethodImplAttributes.NoOptimization, "nooptimization" },
			{ MethodImplAttributes.PreserveSig, "preservesig" },
			{ MethodImplAttributes.InternalCall, "internalcall" },
			{ MethodImplAttributes.ForwardRef, "forwardref" },
			{ MethodImplAttributes.AggressiveInlining, "aggressiveinlining" },
		};

		public void DisassembleMethod(PEFile module, MethodDefinitionHandle handle)
		{
			var genericContext = new GenericContext(handle, module);
			// write method header
			output.WriteReference(module, handle, ".method", isDefinition: true);
			output.Write(" ");
			DisassembleMethodHeaderInternal(module, handle, genericContext);
			DisassembleMethodBlock(module, handle, genericContext);
		}

		public void DisassembleMethodHeader(PEFile module, MethodDefinitionHandle handle)
		{
			var genericContext = new GenericContext(handle, module);
			// write method header
			output.WriteReference(module, handle, ".method", isDefinition: true);
			output.Write(" ");
			DisassembleMethodHeaderInternal(module, handle, genericContext);
		}

		void DisassembleMethodHeaderInternal(PEFile module, MethodDefinitionHandle handle, GenericContext genericContext)
		{
			var metadata = module.Metadata;

			WriteMetadataToken(handle, spaceAfter:true);
			var methodDefinition = metadata.GetMethodDefinition(handle);
			//    .method public hidebysig  specialname
			//               instance default class [mscorlib]System.IO.TextWriter get_BaseWriter ()  cil managed
			//
			//emit flags
			WriteEnum(methodDefinition.Attributes & MethodAttributes.MemberAccessMask, methodVisibility);
			WriteFlags(methodDefinition.Attributes & ~MethodAttributes.MemberAccessMask, methodAttributeFlags);
			bool isCompilerControlled = (methodDefinition.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope;
			if (isCompilerControlled)
				output.Write("privatescope ");

			if ((methodDefinition.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl) {
				output.Write("pinvokeimpl");
				var info = methodDefinition.GetImport();
				if (!info.Module.IsNil) {
					var moduleRef = metadata.GetModuleReference(info.Module);
					output.Write("(\"" + DisassemblerHelpers.EscapeString(metadata.GetString(moduleRef.Name)) + "\"");

					if (!info.Name.IsNil && metadata.GetString(info.Name) != metadata.GetString(methodDefinition.Name))
						output.Write(" as \"" + DisassemblerHelpers.EscapeString(metadata.GetString(info.Name)) + "\"");

					if ((info.Attributes & MethodImportAttributes.ExactSpelling) == MethodImportAttributes.ExactSpelling)
						output.Write(" nomangle");

					switch (info.Attributes & MethodImportAttributes.CharSetMask) {
						case MethodImportAttributes.CharSetAnsi:
							output.Write(" ansi");
							break;
						case MethodImportAttributes.CharSetAuto:
							output.Write(" autochar");
							break;
						case MethodImportAttributes.CharSetUnicode:
							output.Write(" unicode");
							break;
					}

					if ((info.Attributes & MethodImportAttributes.SetLastError) == MethodImportAttributes.SetLastError)
						output.Write(" lasterr");

					switch (info.Attributes & MethodImportAttributes.CallingConventionMask) {
						case MethodImportAttributes.CallingConventionCDecl:
							output.Write(" cdecl");
							break;
						case MethodImportAttributes.CallingConventionFastCall:
							output.Write(" fastcall");
							break;
						case MethodImportAttributes.CallingConventionStdCall:
							output.Write(" stdcall");
							break;
						case MethodImportAttributes.CallingConventionThisCall:
							output.Write(" thiscall");
							break;
						case MethodImportAttributes.CallingConventionWinApi:
							output.Write(" winapi");
							break;
					}

					output.Write(')');
				}
				output.Write(' ');
			}

			output.WriteLine();
			output.Indent();
			var declaringType = methodDefinition.GetDeclaringType();
			var signatureProvider = new DisassemblerSignatureProvider(module, output);
			var signature = methodDefinition.DecodeSignature(signatureProvider, genericContext);
			if (signature.Header.HasExplicitThis) {
				output.Write("instance explicit ");
			} else if (signature.Header.IsInstance) {
				output.Write("instance ");
			}

			//call convention
			WriteEnum(signature.Header.CallingConvention, callingConvention);

			//return type
			signature.ReturnType(ILNameSyntax.Signature);
			output.Write(' ');

			var parameters = methodDefinition.GetParameters().ToArray();
			if (parameters.Length > 0 && parameters.Length > signature.ParameterTypes.Length) {
				var marshallingDesc = metadata.GetParameter(parameters[0]).GetMarshallingDescriptor();

				if (!marshallingDesc.IsNil) {
					WriteMarshalInfo(metadata.GetBlobReader(marshallingDesc));
				}
			}

			if (isCompilerControlled) {
				output.Write(DisassemblerHelpers.Escape(metadata.GetString(methodDefinition.Name) + "$PST" + MetadataTokens.GetToken(handle).ToString("X8")));
			} else {
				output.Write(DisassemblerHelpers.Escape(metadata.GetString(methodDefinition.Name)));
			}

			WriteTypeParameters(output, module, genericContext, methodDefinition.GetGenericParameters());

			//( params )
			output.Write(" (");
			if (signature.ParameterTypes.Length > 0) {
				output.WriteLine();
				output.Indent();
				WriteParameters(metadata, parameters, signature);
				output.Unindent();
			}
			output.Write(") ");
			//cil managed
			WriteEnum(methodDefinition.ImplAttributes & MethodImplAttributes.CodeTypeMask, methodCodeType);
			if ((methodDefinition.ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Managed)
				output.Write("managed ");
			else
				output.Write("unmanaged ");
			WriteFlags(methodDefinition.ImplAttributes & ~(MethodImplAttributes.CodeTypeMask | MethodImplAttributes.ManagedMask), methodImpl);

			output.Unindent();
		}

		private void WriteMetadataToken(Handle handle, bool spaceAfter)
		{
			if (ShowMetadataTokens) {
				output.Write("/* {0:X8} */", MetadataTokens.GetToken(handle));
				if (spaceAfter) {
					output.Write(' ');
				}
			}
		}

		void DisassembleMethodBlock(PEFile module, MethodDefinitionHandle handle, GenericContext genericContext)
		{
			var metadata = module.Metadata;
			var methodDefinition = metadata.GetMethodDefinition(handle);

			OpenBlock(defaultCollapsed: isInType);
			WriteAttributes(module, methodDefinition.GetCustomAttributes());
			foreach (var h in handle.GetMethodImplementations(metadata)) {
				var impl = metadata.GetMethodImplementation(h);
				output.Write(".override method ");
				impl.MethodDeclaration.WriteTo(module, output, genericContext);
				output.WriteLine();
			}

			foreach (var p in methodDefinition.GetGenericParameters()) {
				WriteGenericParameterAttributes(module, p);
			}
			foreach (var p in methodDefinition.GetParameters()) {
				WriteParameterAttributes(module, p);
			}
			WriteSecurityDeclarations(module, methodDefinition.GetDeclarativeSecurityAttributes());

			if (methodDefinition.HasBody()) {
				methodBodyDisassembler.Disassemble(module, handle);
			}
			var declaringType = metadata.GetTypeDefinition(methodDefinition.GetDeclaringType());
			CloseBlock("end of method " + DisassemblerHelpers.Escape(metadata.GetString(declaringType.Name)) + "::" + DisassemblerHelpers.Escape(metadata.GetString(methodDefinition.Name)));
		}

		#region Write Security Declarations
		void WriteSecurityDeclarations(PEFile module, DeclarativeSecurityAttributeHandleCollection secDeclProvider)
		{
			if (secDeclProvider.Count == 0)
				return;
			var metadata = module.Metadata;
			foreach (var h in secDeclProvider) {
				output.Write(".permissionset ");
				var secdecl = metadata.GetDeclarativeSecurityAttribute(h);
				switch ((ushort)secdecl.Action) {
					case 1: // DeclarativeSecurityAction.Request
						output.Write("request");
						break;
					case 2: // DeclarativeSecurityAction.Demand
						output.Write("demand");
						break;
					case 3: // DeclarativeSecurityAction.Assert
						output.Write("assert");
						break;
					case 4: // DeclarativeSecurityAction.Deny
						output.Write("deny");
						break;
					case 5: // DeclarativeSecurityAction.PermitOnly
						output.Write("permitonly");
						break;
					case 6: // DeclarativeSecurityAction.LinkDemand
						output.Write("linkcheck");
						break;
					case 7: // DeclarativeSecurityAction.InheritDemand
						output.Write("inheritcheck");
						break;
					case 8: // DeclarativeSecurityAction.RequestMinimum
						output.Write("reqmin");
						break;
					case 9: // DeclarativeSecurityAction.RequestOptional
						output.Write("reqopt");
						break;
					case 10: // DeclarativeSecurityAction.RequestRefuse
						output.Write("reqrefuse");
						break;
					case 11: // DeclarativeSecurityAction.PreJitGrant
						output.Write("prejitgrant");
						break;
					case 12: // DeclarativeSecurityAction.PreJitDeny
						output.Write("prejitdeny");
						break;
					case 13: // DeclarativeSecurityAction.NonCasDemand
						output.Write("noncasdemand");
						break;
					case 14: // DeclarativeSecurityAction.NonCasLinkDemand
						output.Write("noncaslinkdemand");
						break;
					case 15: // DeclarativeSecurityAction.NonCasInheritance
						output.Write("noncasinheritance");
						break;
					default:
						output.Write(secdecl.Action.ToString());
						break;
				}
				if (AssemblyResolver == null) {
					output.Write(" = ");
					WriteBlob(secdecl.PermissionSet, metadata);
				} else {
					output.WriteLine(" = {");
					output.Indent();
					var blob = metadata.GetBlobReader(secdecl.PermissionSet);
					if ((char)blob.ReadByte() != '.') {
						blob.Reset();
						WriteXmlSecurityDeclaration(blob.ReadUTF8(blob.RemainingBytes));
					} else {
						string currentAssemblyName = null;
						string currentFullAssemblyName = null;
						if (metadata.IsAssembly) {
							currentAssemblyName = metadata.GetString(metadata.GetAssemblyDefinition().Name);
							currentFullAssemblyName = metadata.GetFullAssemblyName();
						}
						int count = blob.ReadCompressedInteger();
						for (int i = 0; i < count; i++) {
							var typeName = blob.ReadSerializedString();
							string[] nameParts = typeName.Split(new[] { ", " }, StringSplitOptions.None);
							if (nameParts.Length < 2 || nameParts[1] == currentAssemblyName) {
								output.Write("class ");
								output.Write(DisassemblerHelpers.Escape(typeName + ", " + currentFullAssemblyName));
							} else {
								output.Write('[');
								output.Write(nameParts[1]);
								output.Write(']');
								output.WriteReference(nameParts[0], null); // TODO : hyperlink!
							}
							output.Write(" = {");
							blob.ReadCompressedInteger(); // ?
														  // The specification seems to be incorrect here, so I'm using the logic from Cecil instead.
							int argCount = blob.ReadCompressedInteger();
							if (argCount > 0) {
								output.WriteLine();
								output.Indent();

								for (int j = 0; j < argCount; j++) {
									WriteSecurityDeclarationArgument(module, ref blob);
									output.WriteLine();
								}

								output.Unindent();
							}
							output.Write('}');

							if (i + 1 < count)
								output.Write(',');
							output.WriteLine();
						}
					}
					output.Unindent();
					output.WriteLine("}");
				}
			}
		}

		void WriteXmlSecurityDeclaration(string xml)
		{
			output.Write("property string XML = ");
			output.Write("string('{0}')", DisassemblerHelpers.EscapeString(xml).Replace("'", "\'"));
		}

		enum TypeKind
		{
			Primitive,
			Type,
			Boxed,
			Enum
		}

		(PrimitiveSerializationTypeCode TypeCode, TypeKind Kind, bool IsArray, string TypeName) ReadArgumentType(ref BlobReader blob)
		{
			var b = blob.ReadByte();
			if (2 <= b && b <= 14) {
				return ((PrimitiveSerializationTypeCode)b, TypeKind.Primitive, false, null);
			}
			switch (b) {
				case 0x1d:
					var result = ReadArgumentType(ref blob);
					return (result.TypeCode, result.Kind, true, result.TypeName);
				case 0x50:
					return (0, TypeKind.Type, false, null);
				case 0x51: // boxed value type
					return (0, TypeKind.Boxed, false, null);
				case 0x55: // enum
					return (0, TypeKind.Enum, false, blob.ReadSerializedString());
				default:
					throw new NotSupportedException($"Custom attribute type 0x{b:x} is not supported.");
			}
		}

		object ReadSimpleArgumentValue(ref BlobReader blob, PrimitiveSerializationTypeCode typeCode, TypeKind kind, string typeName)
		{
			switch (kind) {
				case TypeKind.Enum:
				case TypeKind.Primitive:
					switch (typeCode) {
						case PrimitiveSerializationTypeCode.Boolean:	return blob.ReadBoolean();
						case PrimitiveSerializationTypeCode.Byte:		return blob.ReadByte();
						case PrimitiveSerializationTypeCode.SByte:		return blob.ReadSByte();
						case PrimitiveSerializationTypeCode.Char:		return blob.ReadChar();
						case PrimitiveSerializationTypeCode.Int16:		return blob.ReadInt16();
						case PrimitiveSerializationTypeCode.UInt16:		return blob.ReadUInt16();
						case PrimitiveSerializationTypeCode.Int32:		return blob.ReadInt32();
						case PrimitiveSerializationTypeCode.UInt32:		return blob.ReadUInt32();
						case PrimitiveSerializationTypeCode.Int64:		return blob.ReadInt64();
						case PrimitiveSerializationTypeCode.UInt64:		return blob.ReadUInt64();
						case PrimitiveSerializationTypeCode.Single:		return blob.ReadSingle();
						case PrimitiveSerializationTypeCode.Double:		return blob.ReadDouble();
						case PrimitiveSerializationTypeCode.String:		return blob.ReadSerializedString();
						default: throw new NotSupportedException();
					}
				case TypeKind.Type:
					return blob.ReadSerializedString();
				case TypeKind.Boxed:
					var typeInfo = ReadArgumentType(ref blob);
					return ReadArgumentValue(ref blob, typeInfo.TypeCode, typeInfo.Kind, typeInfo.IsArray, typeInfo.TypeName);
				default:
					throw new NotSupportedException();
			}
		}

		PrimitiveSerializationTypeCode ResolveEnumUnderlyingType(string typeName, PEFile module, out (PEFile Module, EntityHandle Handle) typeDefinition)
		{
			typeDefinition = default;

			TypeDefinitionHandle FindType(PEFile currentModule, string[] name)
			{
				var metadata = currentModule.Metadata;
				var currentNamespace = metadata.GetNamespaceDefinitionRoot();
				ImmutableArray<TypeDefinitionHandle> typeDefinitions = default;

				for (int i = 0; i < name.Length; i++) {
					string identifier = name[i];
					if (!typeDefinitions.IsDefault) {
						restart:
						foreach (var type in typeDefinitions) {
							var typeDef = metadata.GetTypeDefinition(type);
							var currentTypeName = metadata.GetString(typeDef.Name);
							if (identifier == currentTypeName) {
								if (i + 1 == name.Length)
									return type;
								typeDefinitions = typeDef.GetNestedTypes();
								goto restart;
							}
						}
					} else {
						var next = currentNamespace.NamespaceDefinitions.FirstOrDefault(ns => metadata.GetString(metadata.GetNamespaceDefinition(ns).Name) == identifier);
						if (!next.IsNil) {
							currentNamespace = metadata.GetNamespaceDefinition(next);
						} else {
							typeDefinitions = currentNamespace.TypeDefinitions;
							i--;
						}
					}
				}
				return default;
			}
			string[] nameParts = typeName.Split(new[] { ", " }, 2, StringSplitOptions.None);
			string[] typeNameParts = nameParts[0].Split('.');
			PEFile containingModule = null;
			// if we deal with an assembly-qualified name, resolve the assembly
			if (nameParts.Length == 2)
				containingModule = AssemblyResolver.Resolve(AssemblyNameReference.Parse(nameParts[1]));
			if (containingModule != null) {
				// try to find the type in the assembly
				var handle = FindType(containingModule, typeNameParts);
				var metadata = containingModule.Metadata;
				if (handle.IsNil || !handle.IsEnum(metadata, out var typeCode))
					throw new NotSupportedException();
				typeDefinition = (containingModule, handle);
				return (PrimitiveSerializationTypeCode)typeCode;
			} else {
				// just fully-qualified name, try current assembly
				var handle = FindType(module, typeNameParts);
				if (handle.IsNil) {
					// otherwise try mscorlib
					var mscorlib = AssemblyResolver.Resolve(AssemblyNameReference.Parse("mscorlib"));
					handle = FindType(mscorlib, typeNameParts);
					if (handle.IsNil)
						throw new NotImplementedException();
					module = mscorlib;
				}
				var metadata = module.Metadata;
				if (handle.IsNil || !handle.IsEnum(metadata, out var typeCode))
					throw new NotSupportedException();
				typeDefinition = (module, handle);
				return (PrimitiveSerializationTypeCode)typeCode;
			}
		}

		object ReadArgumentValue(ref BlobReader blob, PrimitiveSerializationTypeCode typeCode, TypeKind kind, bool isArray, string typeName)
		{
			if (isArray) {
				uint elementCount = blob.ReadUInt32();
				if (elementCount == 0xFFFF_FFFF) {
					return null;
				} else {
					var array = new object[elementCount];
					for (int i = 0; i < elementCount; i++) {
						array[i] = ReadSimpleArgumentValue(ref blob, typeCode, kind, typeName);
					}
					return array;
				}
			} else {
				return ReadSimpleArgumentValue(ref blob, typeCode, kind, typeName);
			}
		}

		void WritePrimitiveTypeCode(PrimitiveSerializationTypeCode typeCode)
		{
			switch (typeCode) {
				case PrimitiveSerializationTypeCode.Boolean:
					output.Write("bool");
					break;
				case PrimitiveSerializationTypeCode.Byte:
					output.Write("uint8");
					break;
				case PrimitiveSerializationTypeCode.SByte:
					output.Write("int8");
					break;
				case PrimitiveSerializationTypeCode.Char:
					output.Write("char");
					break;
				case PrimitiveSerializationTypeCode.Int16:
					output.Write("int16");
					break;
				case PrimitiveSerializationTypeCode.UInt16:
					output.Write("uint16");
					break;
				case PrimitiveSerializationTypeCode.Int32:
					output.Write("int32");
					break;
				case PrimitiveSerializationTypeCode.UInt32:
					output.Write("uint32");
					break;
				case PrimitiveSerializationTypeCode.Int64:
					output.Write("int64");
					break;
				case PrimitiveSerializationTypeCode.UInt64:
					output.Write("uint64");
					break;
				case PrimitiveSerializationTypeCode.Single:
					output.Write("float32");
					break;
				case PrimitiveSerializationTypeCode.Double:
					output.Write("float64");
					break;
				case PrimitiveSerializationTypeCode.String:
					output.Write("string");
					break;
			}
		}

		void WriteSecurityDeclarationArgument(PEFile module, ref BlobReader blob)
		{
			switch (blob.ReadByte()) {
				case 0x53:
					output.Write("field ");
					break;
				case 0x54:
					output.Write("property ");
					break;
			}
			var typeInfo = ReadArgumentType(ref blob);
			var typeCode = typeInfo.TypeCode;
			var typeDefinition = default((PEFile Module, EntityHandle Handle));
			if (typeInfo.Kind == TypeKind.Enum) {
				typeCode = ResolveEnumUnderlyingType(typeInfo.TypeName, module, out typeDefinition);
			}
			var name = blob.ReadSerializedString();
			object value = ReadArgumentValue(ref blob, typeCode, typeInfo.Kind, typeInfo.IsArray, typeInfo.TypeName);

			WriteTypeInfo(module, typeInfo.TypeCode, typeInfo.Kind, typeInfo.IsArray, typeInfo.TypeName, typeDefinition.Module, typeDefinition.Handle);

			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(name));
			output.Write(" = ");

			if (value is string) {
				// secdecls use special syntax for strings
				output.Write("string('{0}')", DisassemblerHelpers.EscapeString((string)value).Replace("'", "\'"));
			} else {
				if (typeInfo.Kind == TypeKind.Enum || typeInfo.Kind == TypeKind.Primitive) {
					WritePrimitiveTypeCode(typeCode);
				} else {
					WriteTypeInfo(module, typeInfo.TypeCode, typeInfo.Kind, typeInfo.IsArray, typeInfo.TypeName, typeDefinition.Module, typeDefinition.Handle);
				}
				output.Write('(');
				DisassemblerHelpers.WriteOperand(output, value);
				output.Write(')');
			}
		}

		private void WriteTypeInfo(PEFile currentModule, PrimitiveSerializationTypeCode typeCode, TypeKind kind,
			bool isArray, string typeName, PEFile referencedModule, EntityHandle type)
		{
			switch (kind) {
				case TypeKind.Primitive:
					WritePrimitiveTypeCode(typeCode);
					break;
				case TypeKind.Type:
					break;
				case TypeKind.Boxed:
					break;
				case TypeKind.Enum:
					output.Write("enum ");
					if (type.IsNil) {
						output.Write(DisassemblerHelpers.Escape(typeName));
						break;
					}
					if (referencedModule != currentModule) {
						output.Write('[');
						output.Write(referencedModule.Name);
						output.Write(']');
						output.WriteReference(referencedModule, type, type.GetFullTypeName(referencedModule.Metadata).ToString());
					} else {
						output.Write(DisassemblerHelpers.Escape(typeName));
					}
					break;
				default:
					break;
			}

			if (isArray) {
				output.Write("[]");
			}
		}
		#endregion

		#region WriteMarshalInfo
		void WriteMarshalInfo(BlobReader marshalInfo)
		{
			output.Write("marshal(");
			WriteNativeType(ref marshalInfo);
			output.Write(") ");
		}

		void WriteNativeType(ref BlobReader blob)
		{
			byte type;
			switch (type = blob.ReadByte()) {
				case 0x66: // None
				case 0x50: // Max
					break;
				case 0x02: // NATIVE_TYPE_BOOLEAN 
					output.Write("bool");
					break;
				case 0x03: // NATIVE_TYPE_I1
					output.Write("int8");
					break;
				case 0x04: // NATIVE_TYPE_U1
					output.Write("unsigned int8");
					break;
				case 0x05: // NATIVE_TYPE_I2
					output.Write("int16");
					break;
				case 0x06: // NATIVE_TYPE_U2
					output.Write("unsigned int16");
					break;
				case 0x07: // NATIVE_TYPE_I4
					output.Write("int32");
					break;
				case 0x08: // NATIVE_TYPE_U4
					output.Write("unsigned int32");
					break;
				case 0x09: // NATIVE_TYPE_I8
					output.Write("int64");
					break;
				case 0x0a: // NATIVE_TYPE_U8
					output.Write("unsigned int64");
					break;
				case 0x0b: // NATIVE_TYPE_R4
					output.Write("float32");
					break;
				case 0x0c: // NATIVE_TYPE_R8
					output.Write("float64");
					break;
				case 0x14: // NATIVE_TYPE_LPSTR
					output.Write("lpstr");
					break;
				case 0x1f: // NATIVE_TYPE_INT
					output.Write("int");
					break;
				case 0x20: // NATIVE_TYPE_UINT
					output.Write("unsigned int");
					break;
				case 0x26: // NATIVE_TYPE_FUNC
					output.Write("Func");
					break;
				case 0x2a: // NATIVE_TYPE_ARRAY
					if (blob.RemainingBytes > 0)
						WriteNativeType(ref blob);
					output.Write('[');
					int sizeParameterIndex = blob.TryReadCompressedInteger(out int value) ? value : -1;
					int size = blob.TryReadCompressedInteger(out value) ? value : -1;
					int sizeParameterMultiplier = blob.TryReadCompressedInteger(out value) ? value : -1;
					if (size >= 0) {
						output.Write(size.ToString());
					}
					if (sizeParameterIndex >= 0 && sizeParameterMultiplier != 0) {
						output.Write(" + ");
						output.Write(sizeParameterIndex.ToString());
					}
					output.Write(']');
					break;
				case 0x0f: // Currency
					output.Write("currency");
					break;
				case 0x13: // BStr
					output.Write("bstr");
					break;
				case 0x15: // LPWStr
					output.Write("lpwstr");
					break;
				case 0x16: // LPTStr
					output.Write("lptstr");
					break;
				case 0x17: // FixedSysString
					output.Write("fixed sysstring[{0}]", blob.ReadCompressedInteger());
					break;
				case 0x19: // IUnknown
					output.Write("iunknown");
					break;
				case 0x1a: // IDispatch
					output.Write("idispatch");
					break;
				case 0x1b: // Struct
					output.Write("struct");
					break;
				case 0x1c: // IntF
					output.Write("interface");
					break;
				case 0x1d: // SafeArray
					output.Write("safearray ");
					if (blob.RemainingBytes > 0) {
						byte elementType = blob.ReadByte();
						switch (elementType) {
							case 0: // None
								break;
							case 2: // I2
								output.Write("int16");
								break;
							case 3: // I4
								output.Write("int32");
								break;
							case 4: // R4
								output.Write("float32");
								break;
							case 5: // R8
								output.Write("float64");
								break;
							case 6: // Currency
								output.Write("currency");
								break;
							case 7: // Date
								output.Write("date");
								break;
							case 8: // BStr
								output.Write("bstr");
								break;
							case 9: // Dispatch
								output.Write("idispatch");
								break;
							case 10: // Error
								output.Write("error");
								break;
							case 11: // Bool
								output.Write("bool");
								break;
							case 12: // Variant
								output.Write("variant");
								break;
							case 13: // Unknown
								output.Write("iunknown");
								break;
							case 14: // Decimal
								output.Write("decimal");
								break;
							case 16: // I1
								output.Write("int8");
								break;
							case 17: // UI1
								output.Write("unsigned int8");
								break;
							case 18: // UI2
								output.Write("unsigned int16");
								break;
							case 19: // UI4
								output.Write("unsigned int32");
								break;
							case 22: // Int
								output.Write("int");
								break;
							case 23: // UInt
								output.Write("unsigned int");
								break;
							default:
								output.Write(elementType.ToString());
								break;
						}
					}
					break;
				case 0x1e: // FixedArray
					output.Write("fixed array");
					output.Write("[{0}]", blob.TryReadCompressedInteger(out value) ? value : 0);
					if (blob.RemainingBytes > 0) {
						output.Write(' ');
						WriteNativeType(ref blob);
					}
					break;
				case 0x22: // ByValStr
					output.Write("byvalstr");
					break;
				case 0x23: // ANSIBStr
					output.Write("ansi bstr");
					break;
				case 0x24: // TBStr
					output.Write("tbstr");
					break;
				case 0x25: // VariantBool
					output.Write("variant bool");
					break;
				case 0x28: // ASAny
					output.Write("as any");
					break;
				case 0x2b: // LPStruct
					output.Write("lpstruct");
					break;
				case 0x2c: // CustomMarshaler
					string guidValue = blob.ReadSerializedString();
					string unmanagedType = blob.ReadSerializedString();
					string managedType = blob.ReadSerializedString();
					string cookie = blob.ReadSerializedString();

					var guid = !string.IsNullOrEmpty(guidValue) ? new Guid(guidValue) : Guid.Empty;

					output.Write("custom(\"{0}\", \"{1}\"",
								 DisassemblerHelpers.EscapeString(managedType),
								 DisassemblerHelpers.EscapeString(cookie));
					if (guid != Guid.Empty || !string.IsNullOrEmpty(unmanagedType)) {
						output.Write(", \"{0}\", \"{1}\"", guid.ToString(), DisassemblerHelpers.EscapeString(unmanagedType));
					}
					output.Write(')');
					break;
				case 0x2d: // Error
					output.Write("error");
					break;
				default:
					output.Write(type.ToString());
					break;
			}
		}
		#endregion

		void WriteParameters(MetadataReader metadata, ParameterHandle[] parameters, MethodSignature<Action<ILNameSyntax>> signature)
		{
			int parameterOffset = parameters.Length > signature.ParameterTypes.Length ? 1 : 0;
			for (int i = 0; i < signature.ParameterTypes.Length; i++) {
				if (i + parameterOffset < parameters.Length) {
					var p = metadata.GetParameter(parameters[i + parameterOffset]);
					if ((p.Attributes & ParameterAttributes.In) == ParameterAttributes.In)
						output.Write("[in] ");
					if ((p.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out)
						output.Write("[out] ");
					if ((p.Attributes & ParameterAttributes.Optional) == ParameterAttributes.Optional)
						output.Write("[opt] ");
					signature.ParameterTypes[i](ILNameSyntax.Signature);
					output.Write(' ');
					var md = p.GetMarshallingDescriptor();
					if (!md.IsNil) {
						WriteMarshalInfo(metadata.GetBlobReader(md));
					}
					output.WriteDefinition(DisassemblerHelpers.Escape(metadata.GetString(p.Name)), p);
				} else {
					signature.ParameterTypes[i](ILNameSyntax.Signature);
					output.Write(" ''");
				}
				if (i < signature.ParameterTypes.Length - 1)
					output.Write(',');
				output.WriteLine();
			}
		}

		void WriteGenericParameterAttributes(PEFile module, GenericParameterHandle handle)
		{
			var metadata = module.Metadata;
			var p = metadata.GetGenericParameter(handle);
			if (p.GetCustomAttributes().Count == 0)
				return;
			output.Write(".param type {0}", metadata.GetString(p.Name));
			output.WriteLine();
			WriteAttributes(module, p.GetCustomAttributes());
		}

		void WriteParameterAttributes(PEFile module, ParameterHandle handle)
		{
			var metadata = module.Metadata;
			var p = metadata.GetParameter(handle);
			if (p.GetDefaultValue().IsNil && p.GetCustomAttributes().Count == 0)
				return;
			output.Write(".param [{0}]", p.SequenceNumber);
			if (!p.GetDefaultValue().IsNil) {
				output.Write(" = ");
				WriteConstant(metadata, metadata.GetConstant(p.GetDefaultValue()));
			}
			output.WriteLine();
			WriteAttributes(module, p.GetCustomAttributes());
		}

		void WriteConstant(MetadataReader metadata, Constant constant)
		{
			var blob = metadata.GetBlobReader(constant.Value);
			switch (constant.TypeCode) {
				case ConstantTypeCode.NullReference:
					output.Write("nullref");
					break;
				default:
					var value = blob.ReadConstant(constant.TypeCode);
					if (value is string) {
						DisassemblerHelpers.WriteOperand(output, value);
					} else {
						string typeName = DisassemblerHelpers.PrimitiveTypeName(value.GetType().FullName);
						output.Write(typeName);
						output.Write('(');
						float? cf = value as float?;
						double? cd = value as double?;
						if (cf.HasValue && (float.IsNaN(cf.Value) || float.IsInfinity(cf.Value))) {
							output.Write("0x{0:x8}", BitConverter.ToInt32(BitConverter.GetBytes(cf.Value), 0));
						} else if (cd.HasValue && (double.IsNaN(cd.Value) || double.IsInfinity(cd.Value))) {
							output.Write("0x{0:x16}", BitConverter.DoubleToInt64Bits(cd.Value));
						} else {
							DisassemblerHelpers.WriteOperand(output, value);
						}
						output.Write(')');
					}
					break;
			}
		}
		#endregion

		#region Disassemble Field
		EnumNameCollection<FieldAttributes> fieldVisibility = new EnumNameCollection<FieldAttributes>() {
			{ FieldAttributes.Private, "private" },
			{ FieldAttributes.FamANDAssem, "famandassem" },
			{ FieldAttributes.Assembly, "assembly" },
			{ FieldAttributes.Family, "family" },
			{ FieldAttributes.FamORAssem, "famorassem" },
			{ FieldAttributes.Public, "public" },
		};

		EnumNameCollection<FieldAttributes> fieldAttributes = new EnumNameCollection<FieldAttributes>() {
			{ FieldAttributes.Static, "static" },
			{ FieldAttributes.Literal, "literal" },
			{ FieldAttributes.InitOnly, "initonly" },
			{ FieldAttributes.SpecialName, "specialname" },
			{ FieldAttributes.RTSpecialName, "rtspecialname" },
			{ FieldAttributes.NotSerialized, "notserialized" },
		};

		public void DisassembleField(PEFile module, FieldDefinitionHandle field)
		{
			var metadata = module.Metadata;
			var fieldDefinition = metadata.GetFieldDefinition(field);
			output.WriteDefinition(".field ", field);
			int offset = fieldDefinition.GetOffset();
			if (offset > -1) {
				output.Write("[" + offset + "] ");
			}
			WriteEnum(fieldDefinition.Attributes & FieldAttributes.FieldAccessMask, fieldVisibility);
			const FieldAttributes hasXAttributes = FieldAttributes.HasDefault | FieldAttributes.HasFieldMarshal | FieldAttributes.HasFieldRVA;
			WriteFlags(fieldDefinition.Attributes & ~(FieldAttributes.FieldAccessMask | hasXAttributes), fieldAttributes);

			var signature = fieldDefinition.DecodeSignature(new DisassemblerSignatureProvider(module, output), new GenericContext(fieldDefinition.GetDeclaringType(), module));

			var marshallingDescriptor = fieldDefinition.GetMarshallingDescriptor();
			if (!marshallingDescriptor.IsNil) {
				WriteMarshalInfo(metadata.GetBlobReader(marshallingDescriptor));
			}

			signature(ILNameSyntax.Signature);
			output.Write(' ');
			var fieldName = metadata.GetString(fieldDefinition.Name);
			output.Write(DisassemblerHelpers.Escape(fieldName));
			if (fieldDefinition.HasFlag(FieldAttributes.HasFieldRVA)) {
				output.Write(" at I_{0:x8}", fieldDefinition.GetRelativeVirtualAddress());
			}

			var defaultValue = fieldDefinition.GetDefaultValue();
			if (!defaultValue.IsNil) {
				output.Write(" = ");
				WriteConstant(metadata, metadata.GetConstant(defaultValue));
			}
			output.WriteLine();
			var attributes = fieldDefinition.GetCustomAttributes();
			if (attributes.Count > 0) {
				output.MarkFoldStart();
				WriteAttributes(module, fieldDefinition.GetCustomAttributes());
				output.MarkFoldEnd();
			}
		}
		#endregion

		#region Disassemble Property
		EnumNameCollection<PropertyAttributes> propertyAttributes = new EnumNameCollection<PropertyAttributes>() {
			{ PropertyAttributes.SpecialName, "specialname" },
			{ PropertyAttributes.RTSpecialName, "rtspecialname" },
			{ PropertyAttributes.HasDefault, "hasdefault" },
		};

		public void DisassembleProperty(PEFile module, PropertyDefinitionHandle property)
		{
			var metadata = module.Metadata;
			var propertyDefinition = metadata.GetPropertyDefinition(property);
			output.WriteReference(module, property, ".property", true);
			output.Write(" ");
			WriteFlags(propertyDefinition.Attributes, propertyAttributes);
			var accessors = propertyDefinition.GetAccessors();
			var declaringType = metadata.GetMethodDefinition(accessors.GetAny()).GetDeclaringType();
			var signature = propertyDefinition.DecodeSignature(new DisassemblerSignatureProvider(module, output), new GenericContext(declaringType, module));

			if (signature.Header.IsInstance)
				output.Write("instance ");
			signature.ReturnType(ILNameSyntax.Signature);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(metadata.GetString(propertyDefinition.Name)));

			output.Write('(');
			if (signature.ParameterTypes.Length > 0) {
				var parameters = metadata.GetMethodDefinition(accessors.GetAny()).GetParameters();
				int parametersCount = accessors.Getter.IsNil ? parameters.Count - 1 : parameters.Count;

				output.WriteLine();
				output.Indent();
				WriteParameters(metadata, parameters.Take(parametersCount).ToArray(), signature);
				output.Unindent();
			}
			output.Write(')');

			OpenBlock(false);
			WriteAttributes(module, propertyDefinition.GetCustomAttributes());
			WriteNestedMethod(".get", module, accessors.Getter);
			WriteNestedMethod(".set", module, accessors.Setter);
			/*foreach (var method in property.OtherMethods) {
				WriteNestedMethod(".other", method);
			}*/
			CloseBlock();
		}

		void WriteNestedMethod(string keyword, PEFile module, MethodDefinitionHandle method)
		{
			if (method.IsNil)
				return;

			output.Write(keyword);
			output.Write(' ');
			((EntityHandle)method).WriteTo(module, output, GenericContext.Empty);
			output.WriteLine();
		}
		#endregion

		#region Disassemble Event
		EnumNameCollection<EventAttributes> eventAttributes = new EnumNameCollection<EventAttributes>() {
			{ EventAttributes.SpecialName, "specialname" },
			{ EventAttributes.RTSpecialName, "rtspecialname" },
		};

		public void DisassembleEvent(PEFile module, EventDefinitionHandle handle)
		{
			var eventDefinition = module.Metadata.GetEventDefinition(handle);
			var accessors = eventDefinition.GetAccessors();
			TypeDefinitionHandle declaringType;
			if (!accessors.Adder.IsNil) {
				declaringType = module.Metadata.GetMethodDefinition(accessors.Adder).GetDeclaringType();
			} else if (!accessors.Remover.IsNil) {
				declaringType = module.Metadata.GetMethodDefinition(accessors.Remover).GetDeclaringType();
			} else {
				declaringType = module.Metadata.GetMethodDefinition(accessors.Raiser).GetDeclaringType();
			}
			output.WriteReference(module, handle, ".event", true);
			output.Write(" ");
			WriteFlags(eventDefinition.Attributes, eventAttributes);
			var signature = eventDefinition.DecodeSignature(module.Metadata, new DisassemblerSignatureProvider(module, output), new GenericContext(declaringType, module));
			signature(ILNameSyntax.TypeName);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(module.Metadata.GetString(eventDefinition.Name)));
			OpenBlock(false);
			WriteAttributes(module, eventDefinition.GetCustomAttributes());
			WriteNestedMethod(".addon", module, accessors.Adder);
			WriteNestedMethod(".removeon", module, accessors.Remover);
			WriteNestedMethod(".fire", module, accessors.Raiser);
			/*foreach (var method in ev.OtherMethods) {
				WriteNestedMethod(".other", method);
			}*/
			CloseBlock();
		}
		#endregion

		#region Disassemble Type
		EnumNameCollection<TypeAttributes> typeVisibility = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.Public, "public" },
			{ TypeAttributes.NotPublic, "private" },
			{ TypeAttributes.NestedPublic, "nested public" },
			{ TypeAttributes.NestedPrivate, "nested private" },
			{ TypeAttributes.NestedAssembly, "nested assembly" },
			{ TypeAttributes.NestedFamily, "nested family" },
			{ TypeAttributes.NestedFamANDAssem, "nested famandassem" },
			{ TypeAttributes.NestedFamORAssem, "nested famorassem" },
		};

		EnumNameCollection<TypeAttributes> typeLayout = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.AutoLayout, "auto" },
			{ TypeAttributes.SequentialLayout, "sequential" },
			{ TypeAttributes.ExplicitLayout, "explicit" },
		};

		EnumNameCollection<TypeAttributes> typeStringFormat = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.AutoClass, "auto" },
			{ TypeAttributes.AnsiClass, "ansi" },
			{ TypeAttributes.UnicodeClass, "unicode" },
		};

		EnumNameCollection<TypeAttributes> typeAttributes = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.Abstract, "abstract" },
			{ TypeAttributes.Sealed, "sealed" },
			{ TypeAttributes.SpecialName, "specialname" },
			{ TypeAttributes.Import, "import" },
			{ TypeAttributes.Serializable, "serializable" },
			{ TypeAttributes.WindowsRuntime, "windowsruntime" },
			{ TypeAttributes.BeforeFieldInit, "beforefieldinit" },
			{ TypeAttributes.HasSecurity, null },
		};

		public void DisassembleType(PEFile module, TypeDefinitionHandle type)
		{
			var typeDefinition = module.Metadata.GetTypeDefinition(type);
			output.WriteReference(module, type, ".class", true);
			output.Write(" ");
			if ((typeDefinition.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
				output.Write("interface ");
			WriteEnum(typeDefinition.Attributes & TypeAttributes.VisibilityMask, typeVisibility);
			WriteEnum(typeDefinition.Attributes & TypeAttributes.LayoutMask, typeLayout);
			WriteEnum(typeDefinition.Attributes & TypeAttributes.StringFormatMask, typeStringFormat);
			const TypeAttributes masks = TypeAttributes.ClassSemanticsMask | TypeAttributes.VisibilityMask | TypeAttributes.LayoutMask | TypeAttributes.StringFormatMask;
			WriteFlags(typeDefinition.Attributes & ~masks, typeAttributes);

			output.Write(typeDefinition.GetDeclaringType().IsNil ? typeDefinition.GetFullTypeName(module.Metadata).ToILNameString() : DisassemblerHelpers.Escape(module.Metadata.GetString(typeDefinition.Name)));
			GenericContext genericContext = new GenericContext(type, module);
			WriteTypeParameters(output, module, genericContext, typeDefinition.GetGenericParameters());
			output.MarkFoldStart(defaultCollapsed: !ExpandMemberDefinitions && isInType);
			output.WriteLine();

			if (!typeDefinition.BaseType.IsNil) {
				output.Indent();
				output.Write("extends ");
				typeDefinition.BaseType.WriteTo(module, output, genericContext, ILNameSyntax.TypeName);
				output.WriteLine();
				output.Unindent();
			}
			var interfaces = typeDefinition.GetInterfaceImplementations();
			if (interfaces.Count > 0) {
				output.Indent();
				bool first = true;
				foreach (var i in interfaces) {
					if (!first)
						output.WriteLine(",");
					if (first)
						output.Write("implements ");
					else
						output.Write("           ");
					first = false;
					var iface = module.Metadata.GetInterfaceImplementation(i);
					WriteAttributes(module, iface.GetCustomAttributes());
					iface.Interface.WriteTo(module, output, genericContext, ILNameSyntax.TypeName);
				}
				output.WriteLine();
				output.Unindent();
			}

			output.WriteLine("{");
			output.Indent();
			bool oldIsInType = isInType;
			isInType = true;
			WriteAttributes(module, typeDefinition.GetCustomAttributes());
			WriteSecurityDeclarations(module, typeDefinition.GetDeclarativeSecurityAttributes());
			var layout = typeDefinition.GetLayout();
			if (!layout.IsDefault) {
				output.WriteLine(".pack {0}", layout.PackingSize);
				output.WriteLine(".size {0}", layout.Size);
				output.WriteLine();
			}
			var nestedTypes = typeDefinition.GetNestedTypes();
			if (!nestedTypes.IsEmpty) {
				output.WriteLine("// Nested Types");
				foreach (var nestedType in nestedTypes) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleType(module, nestedType);
					output.WriteLine();
				}
				output.WriteLine();
			}
			var fields = typeDefinition.GetFields();
			if (fields.Any()) {
				output.WriteLine("// Fields");
				foreach (var field in fields) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleField(module, field);
				}
				output.WriteLine();
			}
			var methods = typeDefinition.GetMethods();
			if (methods.Any()) {
				output.WriteLine("// Methods");
				foreach (var m in methods) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleMethod(module, m);
					output.WriteLine();
				}
			}
			var events = typeDefinition.GetEvents();
			if (events.Any()) {
				output.WriteLine("// Events");
				foreach (var ev in events) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleEvent(module, ev);
					output.WriteLine();
				}
				output.WriteLine();
			}
			var properties = typeDefinition.GetProperties();
			if (properties.Any()) {
				output.WriteLine("// Properties");
				foreach (var prop in properties) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleProperty(module, prop);
				}
				output.WriteLine();
			}
			CloseBlock("end of class " + (!typeDefinition.GetDeclaringType().IsNil ? module.Metadata.GetString(typeDefinition.Name) : typeDefinition.GetFullTypeName(module.Metadata).ToString()));
			isInType = oldIsInType;
		}

		void WriteTypeParameters(ITextOutput output, PEFile module, GenericContext context, GenericParameterHandleCollection p)
		{
			if (p.Count > 0) {
				output.Write('<');
				var metadata = module.Metadata;
				for (int i = 0; i < p.Count; i++) {
					if (i > 0)
						output.Write(", ");
					var gp = metadata.GetGenericParameter(p[i]);
					if ((gp.Attributes & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint) {
						output.Write("class ");
					} else if ((gp.Attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == GenericParameterAttributes.NotNullableValueTypeConstraint) {
						output.Write("valuetype ");
					}
					if ((gp.Attributes & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint) {
						output.Write(".ctor ");
					}
					var constraints = gp.GetConstraints();
					if (constraints.Count > 0) {
						output.Write('(');
						for (int j = 0; j < constraints.Count; j++) {
							if (j > 0)
								output.Write(", ");
							var constraint = metadata.GetGenericParameterConstraint(constraints[j]);
							constraint.Type.WriteTo(module, output, context, ILNameSyntax.TypeName);
						}
						output.Write(") ");
					}
					if ((gp.Attributes & GenericParameterAttributes.Contravariant) == GenericParameterAttributes.Contravariant) {
						output.Write('-');
					} else if ((gp.Attributes & GenericParameterAttributes.Covariant) == GenericParameterAttributes.Covariant) {
						output.Write('+');
					}
					output.Write(DisassemblerHelpers.Escape(metadata.GetString(gp.Name)));
				}
				output.Write('>');
			}
		}
		#endregion

		#region Helper methods
		void WriteAttributes(PEFile module, CustomAttributeHandleCollection attributes)
		{
			var metadata = module.Metadata;
			foreach (CustomAttributeHandle a in attributes) {
				output.Write(".custom ");
				var attr = metadata.GetCustomAttribute(a);
				attr.Constructor.WriteTo(module, output, GenericContext.Empty);
				if (!attr.Value.IsNil) {
					output.Write(" = ");
					WriteBlob(attr.Value, metadata);
				}
				output.WriteLine();
			}
		}

		void WriteBlob(BlobHandle blob, MetadataReader metadata)
		{
			var reader = metadata.GetBlobReader(blob);
			output.Write("(");
			output.Indent();

			for (int i = 0; i < reader.Length; i++) {
				if (i % 16 == 0 && i < reader.Length - 1) {
					output.WriteLine();
				} else {
					output.Write(' ');
				}
				output.Write(reader.ReadByte().ToString("x2"));
			}

			output.WriteLine();
			output.Unindent();
			output.Write(")");
		}

		void OpenBlock(bool defaultCollapsed)
		{
			output.MarkFoldStart(defaultCollapsed: !ExpandMemberDefinitions && defaultCollapsed);
			output.WriteLine();
			output.WriteLine("{");
			output.Indent();
		}

		void CloseBlock(string comment = null)
		{
			output.Unindent();
			output.Write("}");
			if (comment != null)
				output.Write(" // " + comment);
			output.MarkFoldEnd();
			output.WriteLine();
		}

		void WriteFlags<T>(T flags, EnumNameCollection<T> flagNames) where T : struct
		{
			long val = Convert.ToInt64(flags);
			long tested = 0;
			foreach (var pair in flagNames) {
				tested |= pair.Key;
				if ((val & pair.Key) != 0 && pair.Value != null) {
					output.Write(pair.Value);
					output.Write(' ');
				}
			}
			if ((val & ~tested) != 0)
				output.Write("flag({0:x4}) ", val & ~tested);
		}

		void WriteEnum<T>(T enumValue, EnumNameCollection<T> enumNames) where T : struct
		{
			long val = Convert.ToInt64(enumValue);
			foreach (var pair in enumNames) {
				if (pair.Key == val) {
					if (pair.Value != null) {
						output.Write(pair.Value);
						output.Write(' ');
					}
					return;
				}
			}
			if (val != 0) {
				output.Write("flag({0:x4})", val);
				output.Write(' ');
			}

		}

		sealed class EnumNameCollection<T> : IEnumerable<KeyValuePair<long, string>> where T : struct
		{
			List<KeyValuePair<long, string>> names = new List<KeyValuePair<long, string>>();

			public void Add(T flag, string name)
			{
				this.names.Add(new KeyValuePair<long, string>(Convert.ToInt64(flag), name));
			}

			public IEnumerator<KeyValuePair<long, string>> GetEnumerator()
			{
				return names.GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return names.GetEnumerator();
			}
		}
		#endregion

		public void DisassembleNamespace(string nameSpace, PEFile module, IEnumerable<TypeDefinitionHandle> types)
		{
			if (!string.IsNullOrEmpty(nameSpace)) {
				output.Write(".namespace " + DisassemblerHelpers.Escape(nameSpace));
				OpenBlock(false);
			}
			bool oldIsInType = isInType;
			isInType = true;
			foreach (var td in types) {
				cancellationToken.ThrowIfCancellationRequested();
				DisassembleType(module, td);
				output.WriteLine();
			}
			if (!string.IsNullOrEmpty(nameSpace)) {
				CloseBlock();
				isInType = oldIsInType;
			}
		}

		public void WriteAssemblyHeader(PEFile module)
		{
			var metadata = module.Metadata;
			if (!metadata.IsAssembly) return;
			output.Write(".assembly ");
			var asm = metadata.GetAssemblyDefinition();
			if ((asm.Flags & AssemblyFlags.WindowsRuntime) == AssemblyFlags.WindowsRuntime)
				output.Write("windowsruntime ");
			output.Write(DisassemblerHelpers.Escape(metadata.GetString(asm.Name)));
			OpenBlock(false);
			WriteAttributes(module, asm.GetCustomAttributes());
			WriteSecurityDeclarations(module, asm.GetDeclarativeSecurityAttributes());
			if (!asm.PublicKey.IsNil) {
				output.Write(".publickey = ");
				WriteBlob(asm.PublicKey, metadata);
				output.WriteLine();
			}
			if (asm.HashAlgorithm != AssemblyHashAlgorithm.None) {
				output.Write(".hash algorithm 0x{0:x8}", (int)asm.HashAlgorithm);
				if (asm.HashAlgorithm == AssemblyHashAlgorithm.Sha1)
					output.Write(" // SHA1");
				output.WriteLine();
			}
			Version v = asm.Version;
			if (v != null) {
				output.WriteLine(".ver {0}:{1}:{2}:{3}", v.Major, v.Minor, v.Build, v.Revision);
			}
			CloseBlock();
		}

		public void WriteAssemblyReferences(MetadataReader metadata)
		{
			foreach (var m in metadata.GetModuleReferences()) {
				var mref = metadata.GetModuleReference(m);
				output.WriteLine(".module extern {0}", DisassemblerHelpers.Escape(metadata.GetString(mref.Name)));
			}
			foreach (var a in metadata.AssemblyReferences) {
				var aref = metadata.GetAssemblyReference(a);
				output.Write(".assembly extern ");
				if ((aref.Flags & AssemblyFlags.WindowsRuntime) == AssemblyFlags.WindowsRuntime)
					output.Write("windowsruntime ");
				output.Write(DisassemblerHelpers.Escape(metadata.GetString(aref.Name)));
				OpenBlock(false);
				if (!aref.PublicKeyOrToken.IsNil) {
					output.Write(".publickeytoken = ");
					WriteBlob(aref.PublicKeyOrToken, metadata);
					output.WriteLine();
				}
				if (aref.Version != null) {
					output.WriteLine(".ver {0}:{1}:{2}:{3}", aref.Version.Major, aref.Version.Minor, aref.Version.Build, aref.Version.Revision);
				}
				CloseBlock();
			}
		}

		public void WriteModuleHeader(PEFile module, bool skipMVID = false)
		{
			var metadata = module.Metadata;

			void WriteExportedType(ExportedType exportedType)
			{
				if (!exportedType.Namespace.IsNil) {
					output.Write(DisassemblerHelpers.Escape(metadata.GetString(exportedType.Namespace)));
					output.Write('.');
				}
				output.Write(DisassemblerHelpers.Escape(metadata.GetString(exportedType.Name)));
			}

			foreach (var et in metadata.ExportedTypes) {
				var exportedType = metadata.GetExportedType(et);
				output.Write(".class extern ");
				if (exportedType.IsForwarder)
					output.Write("forwarder ");
				WriteExportedType(exportedType);
				OpenBlock(false);
				switch (exportedType.Implementation.Kind) {
					case HandleKind.AssemblyFile:
						throw new NotImplementedException();
					case HandleKind.ExportedType:
						output.Write(".class extern ");
						var declaringType = metadata.GetExportedType((ExportedTypeHandle)exportedType.Implementation);
						while (true) {
							WriteExportedType(declaringType);
							if (declaringType.Implementation.Kind == HandleKind.ExportedType) {
								declaringType = metadata.GetExportedType((ExportedTypeHandle)declaringType.Implementation);
							} else {
								break;
							}
						}
						output.WriteLine();
						break;
					case HandleKind.AssemblyReference:
						output.Write(".assembly extern ");
						var reference = metadata.GetAssemblyReference((AssemblyReferenceHandle)exportedType.Implementation);
						output.Write(DisassemblerHelpers.Escape(metadata.GetString(reference.Name)));
						output.WriteLine();
						break;
					default:
						throw new NotSupportedException();
				}
				CloseBlock();
			}
			var moduleDefinition = metadata.GetModuleDefinition();

			output.WriteLine(".module {0}", metadata.GetString(moduleDefinition.Name));
			if (!skipMVID) {
				output.WriteLine("// MVID: {0}", metadata.GetGuid(moduleDefinition.Mvid).ToString("B").ToUpperInvariant());
			}

			var headers = module.Reader.PEHeaders;
			output.WriteLine(".imagebase 0x{0:x8}", headers.PEHeader.ImageBase);
			output.WriteLine(".file alignment 0x{0:x8}", headers.PEHeader.FileAlignment);
			output.WriteLine(".stackreserve 0x{0:x8}", headers.PEHeader.SizeOfStackReserve);
			output.WriteLine(".subsystem 0x{0:x} // {1}", headers.PEHeader.Subsystem, headers.PEHeader.Subsystem.ToString());
			output.WriteLine(".corflags 0x{0:x} // {1}", headers.CorHeader.Flags, headers.CorHeader.Flags.ToString());

			WriteAttributes(module, metadata.GetCustomAttributes(EntityHandle.ModuleDefinition));
		}

		public void WriteModuleContents(PEFile module)
		{
			foreach (var handle in module.Metadata.GetTopLevelTypeDefinitions()) {
				DisassembleType(module, handle);
				output.WriteLine();
			}
		}
	}

	class DisassemblerSignatureProvider : ISignatureTypeProvider<Action<ILNameSyntax>, GenericContext>
	{
		readonly PEFile module;
		readonly MetadataReader metadata;
		readonly ITextOutput output;

		public DisassemblerSignatureProvider(PEFile module, ITextOutput output)
		{
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.metadata = module.Metadata;
		}

		public Action<ILNameSyntax> GetArrayType(Action<ILNameSyntax> elementType, ArrayShape shape)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write('[');
				for (int i = 0; i < shape.Rank; i++) {
					if (i > 0)
						output.Write(", ");
					if (i < shape.LowerBounds.Length || i < shape.Sizes.Length) {
						int lower = 0;
						if (i < shape.LowerBounds.Length) {
							lower = shape.LowerBounds[i];
							output.Write(lower.ToString());
						}
						output.Write("...");
						if (i < shape.Sizes.Length)
							output.Write((lower + shape.Sizes[i] - 1).ToString());
					}
				}
				output.Write(']');
			};
		}

		public Action<ILNameSyntax> GetByReferenceType(Action<ILNameSyntax> elementType)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write('&');
			};
		}

		public Action<ILNameSyntax> GetFunctionPointerType(MethodSignature<Action<ILNameSyntax>> signature)
		{
			return syntax => {
				output.Write("method ");
				signature.ReturnType(syntax);
				output.Write(" *(");
				for (int i = 0; i < signature.ParameterTypes.Length; i++) {
					if (i > 0)
						output.Write(", ");
					signature.ParameterTypes[i](syntax);
				}
				output.Write(')');
			};
		}

		public Action<ILNameSyntax> GetGenericInstantiation(Action<ILNameSyntax> genericType, ImmutableArray<Action<ILNameSyntax>> typeArguments)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				genericType(syntaxForElementTypes);
				output.Write('<');
				for (int i = 0; i < typeArguments.Length; i++) {
					if (i > 0)
						output.Write(", ");
					typeArguments[i](syntaxForElementTypes);
				}
				output.Write('>');
			};
		}

		public Action<ILNameSyntax> GetGenericMethodParameter(GenericContext genericContext, int index)
		{
			return syntax => {
				output.Write("!!");
				WriteTypeParameter(genericContext.GetGenericMethodTypeParameterHandleOrNull(index), index, syntax);
			};
		}

		public Action<ILNameSyntax> GetGenericTypeParameter(GenericContext genericContext, int index)
		{
			return syntax => {
				output.Write("!");
				WriteTypeParameter(genericContext.GetGenericTypeParameterHandleOrNull(index), index, syntax);
			};
		}

		void WriteTypeParameter(GenericParameterHandle paramRef, int index, ILNameSyntax syntax)
		{
			if (paramRef.IsNil || syntax == ILNameSyntax.SignatureNoNamedTypeParameters)
				output.Write(index.ToString());
			else {
				var param = metadata.GetGenericParameter(paramRef);
				if (param.Name.IsNil)
					output.Write(param.Index.ToString());
				else
					output.Write(DisassemblerHelpers.Escape(metadata.GetString(param.Name)));
			}
		}

		public Action<ILNameSyntax> GetModifiedType(Action<ILNameSyntax> modifier, Action<ILNameSyntax> unmodifiedType, bool isRequired)
		{
			return syntax => {
				unmodifiedType(syntax);
				if (isRequired)
					output.Write(" modreq");
				else
					output.Write(" modopt");
				output.Write('(');
				modifier(ILNameSyntax.TypeName);
				output.Write(')');
			};
		}

		public Action<ILNameSyntax> GetPinnedType(Action<ILNameSyntax> elementType)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write(" pinned");
			};
		}

		public Action<ILNameSyntax> GetPointerType(Action<ILNameSyntax> elementType)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write('*');
			};
		}

		public Action<ILNameSyntax> GetPrimitiveType(PrimitiveTypeCode typeCode)
		{
			switch (typeCode) {
				case PrimitiveTypeCode.SByte:
					return syntax => output.Write("int8");
				case PrimitiveTypeCode.Int16:
					return syntax => output.Write("int16");
				case PrimitiveTypeCode.Int32:
					return syntax => output.Write("int32");
				case PrimitiveTypeCode.Int64:
					return syntax => output.Write("int64");
				case PrimitiveTypeCode.Byte:
					return syntax => output.Write("uint8");
				case PrimitiveTypeCode.UInt16:
					return syntax => output.Write("uint16");
				case PrimitiveTypeCode.UInt32:
					return syntax => output.Write("uint32");
				case PrimitiveTypeCode.UInt64:
					return syntax => output.Write("uint64");
				case PrimitiveTypeCode.Single:
					return syntax => output.Write("float32");
				case PrimitiveTypeCode.Double:
					return syntax => output.Write("float64");
				case PrimitiveTypeCode.Void:
					return syntax => output.Write("void");
				case PrimitiveTypeCode.Boolean:
					return syntax => output.Write("bool");
				case PrimitiveTypeCode.String:
					return syntax => output.Write("string");
				case PrimitiveTypeCode.Char:
					return syntax => output.Write("char");
				case PrimitiveTypeCode.Object:
					return syntax => output.Write("object");
				case PrimitiveTypeCode.IntPtr:
					return syntax => output.Write("native int");
				case PrimitiveTypeCode.UIntPtr:
					return syntax => output.Write("native uint");
				case PrimitiveTypeCode.TypedReference:
					return syntax => output.Write("typedref");
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public Action<ILNameSyntax> GetSZArrayType(Action<ILNameSyntax> elementType)
		{
			return syntax => {
				var syntaxForElementTypes = syntax == ILNameSyntax.SignatureNoNamedTypeParameters ? syntax : ILNameSyntax.Signature;
				elementType(syntaxForElementTypes);
				output.Write('[');
				output.Write(']');
			};
		}

		public Action<ILNameSyntax> GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
		{
			return syntax => {
				switch (rawTypeKind) {
					case 0x00:
						break;
					case 0x11:
						output.Write("valuetype ");
						break;
					case 0x12:
						output.Write("class ");
						break;
					default:
						throw new NotSupportedException($"rawTypeKind: {rawTypeKind} (0x{rawTypeKind:x})");
				}
				((EntityHandle)handle).WriteTo(module, output, GenericContext.Empty);
			};
		}

		public Action<ILNameSyntax> GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
		{
			return syntax => {
				switch (rawTypeKind) {
					case 0x00:
						break;
					case 0x11:
						output.Write("valuetype ");
						break;
					case 0x12:
						output.Write("class ");
						break;
					default:
						throw new NotSupportedException($"rawTypeKind: {rawTypeKind} (0x{rawTypeKind:x})");
				}
				((EntityHandle)handle).WriteTo(module, output, GenericContext.Empty);
			};
		}

		public Action<ILNameSyntax> GetTypeFromSpecification(MetadataReader reader, GenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
		{
			return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
		}
	}
}