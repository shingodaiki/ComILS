#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:0:0:0
}

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly System.Runtime.CompilerServices.Unsafe
{
  .custom instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                                   63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [CORE_ASSEMBLY]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [CORE_ASSEMBLY]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 07 34 2E 30 2E 30 2E 30 00 00 )             // ...4.0.0.0..
  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 07 34 2E 30 2E 30 2E 30 00 00 )             // ...4.0.0.0..
  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 26 53 79 73 74 65 6D 2E 52 75 6E 74 69 6D   // ..&System.Runtim
                                                                                              65 2E 43 6F 6D 70 69 6C 65 72 53 65 72 76 69 63   // e.CompilerServic
                                                                                              65 73 2E 55 6E 73 61 66 65 00 00 )                // es.Unsafe..
  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyDescriptionAttribute::.ctor(string) = ( 01 00 26 53 79 73 74 65 6D 2E 52 75 6E 74 69 6D   // ..&System.Runtim
                                                                                                    65 2E 43 6F 6D 70 69 6C 65 72 53 65 72 76 69 63   // e.CompilerServic
                                                                                                    65 73 2E 55 6E 73 61 66 65 00 00 )                // es.Unsafe..
  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyMetadataAttribute::.ctor(string, string) = (
    01 00 15 2e 4e 45 54 46 72 61 6d 65 77 6f 72 6b
    41 73 73 65 6d 62 6c 79 00 00 00
  ) // ".NETFrameworkAssembly", ""
  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyMetadataAttribute::.ctor(string, string) = (
    01 00 0b 53 65 72 76 69 63 65 61 62 6c 65 04 54
    72 75 65 00 00
  ) // "Serviceable", "True"
  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyCopyrightAttribute::.ctor(string) = ( 01 00 2F C2 A9 20 4D 69 63 72 6F 73 6F 66 74 20   // ../.. Microsoft 
                                                                                        43 6F 72 70 6F 72 61 74 69 6F 6E 2E 20 20 41 6C   // Corporation.  Al
                                                                                        6C 20 72 69 67 68 74 73 20 72 65 73 65 72 76 65   // l rights reserve
                                                                                        64 2E 00 00 )                                     // d...
  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 15 4D 69 63 72 6F 73 6F 66 74 20 43 6F 72   // ...Microsoft Cor
                                                                                      70 6F 72 61 74 69 6F 6E 00 00 )                   // poration..
  .custom instance void [CORE_ASSEMBLY]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 1A 4D 69 63 72 6F 73 6F 66 74 C2 AE 20 2E   // ...Microsoft.. .
                                                                                      4E 45 54 20 46 72 61 6D 65 77 6F 72 6B 00 00 )    // NET Framework..
  .custom instance void [CORE_ASSEMBLY]System.CLSCompliantAttribute::.ctor(bool) = (
    01 00 00 00 00
  ) // false
  .hash algorithm 0x00008004
  .ver 4:0:5:0
}
.module System.Runtime.CompilerServices.Unsafe.dll
// MVID: {1E97D84A-565B-49C5-B60A-F31A1A4ACE13}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02ED0000


// =============== CLASS MEMBERS DECLARATION ===================

.class public abstract auto ansi sealed beforefieldinit System.Runtime.CompilerServices.Unsafe
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig static !!T Read<T>(void* source) cil managed aggressiveinlining
  {
        .maxstack 1
        ldarg.0
        ldobj !!T
        ret
  } // end of method Unsafe::Read

  .method public hidebysig static !!T ReadUnaligned<T>(void* source) cil managed aggressiveinlining
  {
        .maxstack 1
        ldarg.0
        unaligned. 0x1
        ldobj !!T
        ret
  } // end of method Unsafe::ReadUnaligned

  .method public hidebysig static !!T ReadUnaligned<T>(uint8& source) cil managed aggressiveinlining
  {
        .maxstack 1
        ldarg.0
        unaligned. 0x1
        ldobj !!T
        ret
  } // end of method Unsafe::ReadUnaligned

  .method public hidebysig static void Write<T>(void* destination,
                                                 !!T 'value') cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        stobj !!T
        ret
  } // end of method Unsafe::Write

  .method public hidebysig static void WriteUnaligned<T>(void* destination,
                                                 !!T 'value') cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        unaligned. 0x01
        stobj !!T
        ret
  } // end of method Unsafe::WriteUnaligned

  .method public hidebysig static void WriteUnaligned<T>(uint8& destination,
                                                 !!T 'value') cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        unaligned. 0x01
        stobj !!T
        ret
  } // end of method Unsafe::WriteUnaligned

  .method public hidebysig static void Copy<T>(void* destination,
                                                !!T& source) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        ldobj !!T
        stobj !!T
        ret
  } // end of method Unsafe::Copy

  .method public hidebysig static void Copy<T>(!!T& destination,
                                                void* source) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        ldobj !!T
        stobj !!T
        ret
  } // end of method Unsafe::Copy

  .method public hidebysig static void* AsPointer<T>(!!T& 'value') cil managed aggressiveinlining
  {
        .maxstack 1
        ldarg.0
        conv.u
        ret
  } // end of method Unsafe::AsPointer

  .method public hidebysig static void SkipInit<T> ([out] !!T& 'value') cil managed aggressiveinlining
  {
        .maxstack 0
        ret
  } // end of method Unsafe::SkipInit

  .method public hidebysig static int32 SizeOf<T>() cil managed aggressiveinlining
  {
        .maxstack 1
        sizeof !!T
        ret
  } // end of method Unsafe::SizeOf

  .method public hidebysig static void CopyBlock(void* destination, void* source, uint32 byteCount) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        cpblk
        ret
  } // end of method Unsafe::CopyBlock

  .method public hidebysig static void CopyBlock(uint8& destination, uint8& source, uint32 byteCount) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        cpblk
        ret
  } // end of method Unsafe::CopyBlock

  .method public hidebysig static void CopyBlockUnaligned(void* destination, void* source, uint32 byteCount) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        unaligned. 0x1
        cpblk
        ret
  } // end of method Unsafe::CopyBlockUnaligned

  .method public hidebysig static void CopyBlockUnaligned(uint8& destination, uint8& source, uint32 byteCount) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        unaligned. 0x1
        cpblk
        ret
  } // end of method Unsafe::CopyBlockUnaligned

  .method public hidebysig static void InitBlock(void* startAddress, uint8 'value', uint32 byteCount) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        initblk
        ret
  } // end of method Unsafe::InitBlock

  .method public hidebysig static void InitBlock(uint8& startAddress, uint8 'value', uint32 byteCount) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        initblk
        ret
  } // end of method Unsafe::InitBlock

  .method public hidebysig static void InitBlockUnaligned(void* startAddress, uint8 'value', uint32 byteCount) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        unaligned. 0x1
        initblk
        ret
  } // end of method Unsafe::InitBlockUnaligned

  .method public hidebysig static void InitBlockUnaligned(uint8& startAddress, uint8 'value', uint32 byteCount) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        ldarg.2
        unaligned. 0x1
        initblk
        ret
  } // end of method Unsafe::InitBlockUnaligned

  .method public hidebysig static !!T As<class T>(object o) cil managed aggressiveinlining
  {
        .maxstack 1
        ldarg.0
        ret
  } // end of method Unsafe::As

  .method public hidebysig static !!T& AsRef<T>(void* source) cil managed aggressiveinlining
  {
// For .NET Core the roundtrip via a local is no longer needed see:
// https://github.com/dotnet/runtime/issues/8730
// and
// https://github.com/dotnet/coreclr/pull/11218
#ifdef netcoreapp
        .maxstack 1
        ldarg.0
        ret
#else
        .locals (int32&)
        .maxstack 1
        ldarg.0
        // Roundtrip via a local to avoid type mismatch on return that the JIT inliner chokes on.
        stloc.0
        ldloc.0
        ret
#endif
  } // end of method Unsafe::AsRef

  .method public hidebysig static !!T& AsRef<T>(!!T& source) cil managed aggressiveinlining
  {
        .param [1]
#ifdef netcoreapp
        .custom instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
#else
        .custom instance void System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = ( 01 00 00 00 )
#endif
        .maxstack 1
        ldarg.0
        ret
  } // end of method Unsafe::AsRef

  .method public hidebysig static !!TTo& As<TFrom, TTo>(!!TFrom& source) cil managed aggressiveinlining
  {
        .maxstack 1
        ldarg.0
        ret
  } // end of method Unsafe::As

  .method public hidebysig static !!T& Unbox<valuetype .ctor ([CORE_ASSEMBLY]System.ValueType) T> (object 'box') cil managed aggressiveinlining
  {
        .maxstack 1
        ldarg.0
        unbox !!T
        ret
  } // end of method Unsafe::Unbox

  .method public hidebysig static !!T& Add<T>(!!T& source, int32 elementOffset) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        sizeof !!T
        conv.i
        mul
        add
        ret
  } // end of method Unsafe::Add

  .method public hidebysig static void* Add<T>(void* source, int32 elementOffset) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        sizeof !!T
        conv.i
        mul
        add
        ret
  } // end of method Unsafe::Add

  .method public hidebysig static !!T& Add<T>(!!T& source, native int elementOffset) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        sizeof !!T
        mul
        add
        ret
  } // end of method Unsafe::Add

  .method public hidebysig static !!T& Add<T>(!!T& source, native uint elementOffset) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        sizeof !!T
        mul
        add
        ret
  } // end of method Unsafe::Add

  .method public hidebysig static !!T& AddByteOffset<T>(!!T& source, native int byteOffset) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        add
        ret
  } // end of method Unsafe::AddByteOffset

  .method public hidebysig static !!T& AddByteOffset<T>(!!T& source, native uint byteOffset) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        add
        ret
  } // end of method Unsafe::AddByteOffset

  .method public hidebysig static !!T& Subtract<T>(!!T& source, int32 elementOffset) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        sizeof !!T
        conv.i
        mul
        sub
        ret
  } // end of method Unsafe::Subtract

  .method public hidebysig static void* Subtract<T>(void* source, int32 elementOffset) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        sizeof !!T
        conv.i
        mul
        sub
        ret
  } // end of method Unsafe::Subtract

  .method public hidebysig static !!T& Subtract<T>(!!T& source, native int elementOffset) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        sizeof !!T
        mul
        sub
        ret
  } // end of method Unsafe::Subtract

  .method public hidebysig static !!T& Subtract<T>(!!T& source, native uint elementOffset) cil managed aggressiveinlining
  {
        .maxstack 3
        ldarg.0
        ldarg.1
        sizeof !!T
        mul
        sub
        ret
  } // end of method Unsafe::Subtract

  .method public hidebysig static !!T& SubtractByteOffset<T>(!!T& source, native int byteOffset) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        sub
        ret
  } // end of method Unsafe::SubtractByteOffset

  .method public hidebysig static !!T& SubtractByteOffset<T>(!!T& source, native uint byteOffset) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        sub
        ret
  } // end of method Unsafe::SubtractByteOffset

  .method public hidebysig static native int ByteOffset<T>(!!T& origin, !!T& target) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.1
        ldarg.0
        sub
        ret
  } // end of method Unsafe::ByteOffset

  .method public hidebysig static bool AreSame<T>(!!T& left, !!T& right) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        ceq
        ret
  } // end of method Unsafe::AreSame

  .method public hidebysig static bool IsAddressGreaterThan<T>(!!T& left, !!T& right) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        cgt.un
        ret
  } // end of method Unsafe::IsAddressGreaterThan

  .method public hidebysig static bool IsAddressLessThan<T>(!!T& left, !!T& right) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldarg.1
        clt.un
        ret
  } // end of method Unsafe::IsAddressLessThan

  .method public hidebysig static bool IsNullRef<T>(!!T& source) cil managed aggressiveinlining
  {
        .maxstack 2
        ldarg.0
        ldc.i4.0
        conv.u
        ceq
        ret
  } // end of method Unsafe::IsNullRef

  .method public hidebysig static !!T& NullRef<T>() cil managed aggressiveinlining
  {
        .maxstack 1
        ldc.i4.0
        conv.u
        ret
  } // end of method Unsafe::NullRef

} // end of class System.Runtime.CompilerServices.Unsafe

#ifdef netcoreapp
#else
.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
  extends [CORE_ASSEMBLY]System.Attribute
{
  .custom instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
      01 00 00 00
  )
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
      01 00 00 00
  )
  .method public hidebysig specialname rtspecialname 
      instance void .ctor () cil managed 
  {
      .maxstack 8
      IL_0000: ldarg.0
      IL_0001: call instance void [CORE_ASSEMBLY]System.Attribute::.ctor()
      IL_0006: ret
  } // end of method EmbeddedAttribute::.ctor

} // end of class Microsoft.CodeAnalysis.EmbeddedAttribute

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IsReadOnlyAttribute
       extends [CORE_ASSEMBLY]System.Attribute
{
  .custom instance void [CORE_ASSEMBLY]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
  	01 00 00 00
  )
  .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
  	01 00 00 00
  )
  .method public hidebysig specialname rtspecialname 
          instance void .ctor () cil managed 
  {
        .maxstack 1
        ldarg.0
        call instance void [CORE_ASSEMBLY]System.Attribute::.ctor()
        ret
  } // end of method IsReadOnlyAttribute::.ctor

} // end of class System.Runtime.CompilerServices.IsReadOnlyAttribute
#endif

.class private auto ansi sealed beforefieldinit ExtraUnsafeTests
  extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void .ctor () cil managed 
  {
        .maxstack 1
        ldarg.0
        call instance void [CORE_ASSEMBLY]System.Object::.ctor()
        ret
  } // end of method ExtraUnsafeTests::.ctor

  .method public hidebysig static void PinWithTypeMismatch(uint32& managedPtr)
  {
    .maxstack 8
    .locals (
      [0] uint16& pinned
    )
    // Pin:
    ldarg.0
    stloc.0

    // Unpin:
    ldc.i4 0
    stloc.0

    ret
  }

  .method public hidebysig static uint32* RefToPointerWithoutPinning(uint32& managedPtr)
  {
    .maxstack 8

    ldarg.0
    ret
  }

  .method public hidebysig static uint64& RefAssignTypeMismatch(uint32& a, uint32& b)
  {
    .maxstack 8
    .locals (
      [0] uint16&
    )

    ldarg.0
    stloc.0
    
    ldarg.0
    ldind.i4
    brfalse lbl

    ldarg.1
    stloc.0

lbl:
    ldloc.0
    ldind.i2
    call       void [System.Console]System.Console::WriteLine(uint16)

    ldloc.0
    ret
  }

  .method public hidebysig static 
	  uint8[] Issue1292 (
		  int32 val,
		  uint8[] arr
	  ) cil managed 
  {
	  .maxstack 2
	  .locals init (
		  [0] uint8[],
		  [1] uint8[],
		  [2] uint8& pinned
	  )

	  IL_0000: ldarg.1
	  IL_0001: stloc.0
	  IL_0002: ldloc.0
	  IL_0003: dup
	  IL_0004: stloc.1
	  IL_0005: brfalse.s IL_0016

	  IL_0007: ldloc.1
	  IL_0008: ldlen
	  IL_0009: conv.i4
	  IL_000a: brfalse.s IL_0016

	  IL_000c: ldloc.1
	  IL_000d: ldc.i4.0
	  IL_000e: ldelema [System.Runtime]System.Byte
	  IL_0013: stloc.2
	  IL_0014: br.s IL_0019

	  IL_0016: ldc.i4.0
	  IL_0017: conv.u
	  IL_0018: stloc.2

	  IL_0019: ldloc.2
	  IL_001a: conv.i
	  IL_001b: ldarg.0
	  IL_001c: stind.i4
	  IL_001d: ldc.i4.0
	  IL_001e: conv.u
	  IL_001f: stloc.2
	  IL_0020: ldloc.0
	  IL_0021: ret
  } // end of method Issue1292

.method /* 06000066 */ public hidebysig 
	instance void pin_ptr_test (
		int32[] a,
		int32[] b
	) cil managed 
{
	/* From C++/CLI:
      void pin_ptr_test(array<int>^ a, array<int>^ b)
        {
            pin_ptr<int> p = &a[0];
            if (*p > 0)
            {
                p = &b[*p];
            }
            p[0] = 1;
        }
   */
	.maxstack 3
	.locals /* 11000004 */ (
		[0] int32& pinned modopt([System.Runtime]System.Runtime.CompilerServices.IsExplicitlyDereferenced) p
	)

	IL_0000: ldarg.1
	IL_0001: ldc.i4.0
	IL_0002: ldelema [System.Runtime]System.Int32 /* 01000016 */
	IL_0007: stloc.0
	IL_0008: ldloc.0
	IL_0009: ldind.i4
	IL_000a: ldc.i4.0
	IL_000b: ble.s IL_0016

	IL_000d: ldarg.2
	IL_000e: ldloc.0
	IL_000f: ldind.i4
	IL_0010: ldelema [System.Runtime]System.Int32 /* 01000016 */
	IL_0015: stloc.0

	IL_0016: ldloc.0
	IL_0017: ldc.i4.4
	IL_0018: ldc.i4.0
	IL_0019: mul
	IL_001a: add
	IL_001b: ldc.i4.1
	IL_001c: stind.i4
	IL_001d: ret
} // end of method pin_ptr_test

.method private hidebysig static 
	void Issue2148 (
		string[] args
	) cil managed 
{
	// Header Size: 12 bytes
	// Code Size: 24 (0x18) bytes
	// LocalVarSig Token: 0x11000001 RID: 1
	.maxstack 2
	.locals init (
		[0] int32 pinned
	)

	/* 0x00000264 16           */ IL_0000: ldc.i4.0
	/* 0x00000265 0A           */ IL_0001: stloc.0
	/* 0x00000266 2B0E         */ IL_0002: br.s      IL_0012
	// loop start (head: IL_0012)
		/* 0x00000268 7201000070   */ IL_0004: ldstr     "Hello World!"
		/* 0x0000026D 280B00000A   */ IL_0009: call      void [System.Console]System.Console::WriteLine(string)
		/* 0x00000272 06           */ IL_000E: ldloc.0
		/* 0x00000273 17           */ IL_000F: ldc.i4.1
		/* 0x00000274 58           */ IL_0010: add
		/* 0x00000275 0A           */ IL_0011: stloc.0

		/* 0x00000276 06           */ IL_0012: ldloc.0
		/* 0x00000277 1F64         */ IL_0013: ldc.i4.s  100
		/* 0x00000279 32ED         */ IL_0015: blt.s     IL_0004
	// end loop

	/* 0x0000027B 2A           */ IL_0017: ret
} // end of method Issue2148


.method private hidebysig static 
	void Issue2189 () cil managed 
{
	.maxstack 2
	.locals init (
    [0] int32,
		[1] int32& pinned
	)

	IL_0000: ldsflda valuetype [CORE_ASSEMBLY]SomeStruct [CORE_ASSEMBLY]SomeStruct::'instance'
	IL_0005: ldflda uint32 [CORE_ASSEMBLY]SomeStruct::mtfhist
	IL_000a: conv.u
	IL_000b: stloc.1

  ldloc.1
  ldind.i4
  stloc.0

	IL_0185: ldc.i4.0
	IL_0186: conv.i
	IL_0187: stloc.1
	IL_0188: ret

} // end of method Issue2189

.method private hidebysig static 
	void PinUnmanagedPtr (int32*) cil managed 
{
	.maxstack 2
	.locals init (
    [0] int32,
		[1] int32& pinned
	)

	ldarg.0
	stloc.1

  ldloc.1
  ldind.i4
  stloc.0

	ldc.i4.0
	conv.i
	stloc.1
	ret

} // end of method Issue2189


.method private hidebysig static 
	float32& AddressTypeMismatch (int32&) cil managed
{
	ldarg.0
	ret
}
.method private hidebysig static 
	float32& AddressTypeMismatch (int32*) cil managed
{
	ldarg.0
	ret
}

.method private hidebysig static 
	float32 LoadWithTypeMismatch (int32&) cil managed
{
	ldarg.0
	ldind.r4
	ret
}
.method private hidebysig static 
	float32 LoadWithTypeMismatch (int32*) cil managed
{
	ldarg.0
	ldind.r4
	ret
}

.method private hidebysig static 
	void StoreWithTypeMismatch (int32&) cil managed
{
	ldarg.0
	ldc.r4 1
  stind.r4
	ret
}
.method private hidebysig static 
	void StoreWithTypeMismatch (int32*) cil managed
{
	ldarg.0
	ldc.r4 1
  stind.r4
	ret
}

.method private hidebysig static 
	float32& AddressOfFieldTypeMismatch (int32&) cil managed
{
	ldarg.0
  ldflda float32 SomeStruct::float_field
	ret
}
.method private hidebysig static 
	float32& AddressOfFieldTypeMismatch (int32*) cil managed
{
	ldarg.0
  ldflda float32 SomeStruct::float_field
	ret
}

.method private hidebysig static 
	float32 LoadOfFieldTypeMismatch (int32&) cil managed
{
	ldarg.0
  ldfld float32 SomeStruct::float_field
	ret
}
.method private hidebysig static 
	float32 LoadOfFieldTypeMismatch (int32*) cil managed
{
	ldarg.0
  ldfld float32 SomeStruct::float_field
	ret
}

.method private hidebysig static 
	void StoreOfFieldTypeMismatch (int32&) cil managed
{
	ldarg.0
	ldc.r4 1
  stfld float32 SomeStruct::float_field
	ret
}
.method private hidebysig static 
	void StoreOfFieldTypeMismatch (int32*) cil managed
{
	ldarg.0
	ldc.r4 1
  stfld float32 SomeStruct::float_field
	ret
}

} // class ExtraUnsafeTests

.class  private sequential ansi sealed beforefieldinit SomeStruct
	extends [mscorlib]System.ValueType
{
	// Fields
	.field public int32 int_field
	.field public float32 float_field
} // end of class SomeStruct
