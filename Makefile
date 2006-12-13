.SILENT:

!INCLUDE "Wipt.ver"

all: AssemblyVersion.cs
	-rmdir /s /q bin 2> nul
	-mkdir bin 2> nul
	echo Building Windows Installer assembly...
	cd WindowsInstaller
	nmake /$(MAKEFLAGS) ACM.Wipt.WindowsInstaller.dll
	copy /y ACM.Wipt.WindowsInstaller.dll ..\bin
!IFNDEF NODEBUG
	copy /y ACM.Wipt.WindowsInstaller.pdb ..\bin
!ENDIF
	echo Building WiptLib...
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) WiptLib.dll
	copy /y wiptlib.dll ..\bin
!IFNDEF NODEBUG
	copy /y wiptlib.pdb ..\bin
!ENDIF
	echo Building wipt-get...
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) wipt-get.exe
	copy /y wipt-get.exe ..\bin
!IFNDEF NODEBUG
	copy /y wipt-get.pdb ..\bin
!ENDIF
	echo Building wipt-put...
	cd ..\wipt-put
	nmake /$(MAKEFLAGS) wipt-put.exe
	copy /y wipt-put.exe ..\bin
!IFNDEF NODEBUG
	copy /y wipt-put.pdb ..\bin
!ENDIF
	cd ..

AssemblyVersion.cs: Wipt.ver
	echo using System.Reflection; > AssemblyVersion.cs
	echo [assembly:AssemblyVersion("$(ProductVersion).*")] >> AssemblyVersion.cs

wipt.msi: all
	candle /nologo /dProductVersion=$(ProductVersion) Wipt.wxs
	light /nologo -ext WixUIExtension -cultures:en-us wipt.wixobj
!IFDEF NODEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt.msi
!ENDIF
clean:
	-rmdir /s /q bin 2> nul
	-del Wipt.msi Wipt.wixobj Wipt.ncb AssemblyVersion.cs 2> nul
	cd WindowsInstaller
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\wipt-put
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..
