# ComILS
Aside from the WPF UI ILSpy (downloadable via Releases, see also plugins), the following other frontends are available:

Visual Studio 2022 ships with decompilation support for F12 enabled by default (using our engine v7.1).
In Visual Studio 2019, you have to manually enable F12 support. Go to Tools / Options / Text Editor / C# / Advanced and check "Enable navigation to decompiled source"
C# for Visual Studio Code ships with decompilation support as well. To enable, activate the setting "Enable Decompilation Support".
Our Visual Studio 2022 extension marketplace
Our Visual Studio 2017/2019 extension marketplace
Our Visual Studio Code Extension repository | marketplace
Our ICSharpCode.Decompiler NuGet for your own projects
Our dotnet tool for Linux/Mac/Windows - check out ILSpyCmd in this repository
Our Linux/Mac/Windows PowerShell cmdlets in this repository

# Features
Decompilation to C# (check out the language support status)
Whole-project decompilation (csproj, not sln!)
Search for types/methods/properties (learn about the options)
Hyperlink-based type/method/property navigation
Base/Derived types navigation, history
Assembly metadata explorer (feature walkthrough)
BAML to XAML decompiler
ReadyToRun binary support for .NET Core (see the tutorial)
Extensible via plugins
Additional features in DEBUG builds (for the devs)

# How to Build
Windows:
Make sure PowerShell (at least version) 5.0 is installed.
Clone the ILSpy repository using git.
Execute git submodule update --init --recursive to download the ILSpy-Tests submodule (used by some test cases).
Install Visual Studio (documented version: 17.1). You can install the necessary components in one of 3 ways:
Follow Microsoft's instructions for importing a configuration, and import the .vsconfig file located at the root of the solution.
Alternatively, you can open the ILSpy solution (ILSpy.sln) and Visual Studio will prompt you to install the missing components.
Finally, you can manually install the necessary components via the Visual Studio Installer. The workloads/components are as follows:
Workload ".NET Desktop Development". This workload includes the .NET Framework 4.8 SDK and the .NET Framework 4.7.2 targeting pack, as well as the .NET 6.0 SDK (ILSpy.csproj targets .NET 6.0, but we have net472 projects too). Note: The optional components of this workload are not required for ILSpy
Workload "Visual Studio extension development" (ILSpy.sln contains a VS extension project) Note: The optional components of this workload are not required for ILSpy
Individual Component "MSVC v143 - VS 2022 C++ x64/x86 build tools" (or similar)
The VC++ toolset is optional; if present it is used for editbin.exe to modify the stack size used by ILSpy.exe from 1MB to 16MB, because the decompiler makes heavy use of recursion, where small stack sizes lead to problems in very complex methods.
Open ILSpy.sln in Visual Studio.
NuGet package restore will automatically download further dependencies
Run project "ILSpy" for the ILSpy UI
Use the Visual Studio "Test Explorer" to see/run the tests
If you are only interested in a specific subset of ILSpy, you can also use
ILSpy.Wpf.slnf: for the ILSpy WPF frontend
ILSpy.XPlat.slnf: for the cross-platform CLI or PowerShell cmdlets
ILSpy.AddIn.slnf: for the Visual Studio plugin
Note: Visual Studio 16.3 and later include a version of the .NET (Core) SDK that is managed by the Visual Studio installer - once you update, it may get upgraded too. Please note that ILSpy is only compatible with the .NET 6.0 SDK and Visual Studio will refuse to load some projects in the solution (and unit tests will fail). If this problem occurs, please manually install the .NET 6.0 SDK from here.

Unix / Mac:
Make sure .NET 6.0 SDK is installed.
Make sure PowerShell is installed (formerly known as PowerShell Core)
Clone the repository using git.
Execute git submodule update --init --recursive to download the ILSpy-Tests submodule (used by some test cases).
Use dotnet build ILSpy.XPlat.slnf to build the non-Windows flavors of ILSpy (.NET Core Global Tool and PowerShell Core).
