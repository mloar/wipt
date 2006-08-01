.SILENT:

!INCLUDE "Wipt.ver"

all: AssemblyVersion.cs
	echo Building Windows Installer assembly...
	cd System.Installer
	nmake /$(MAKEFLAGS) ACM.Wipt.WindowsInstaller.dll
	echo Building WiptLib...
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) WiptLib.dll
	echo Building wipt-get...
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) wipt-get.exe
	echo Building wipt-put...
	cd ..\wipt-put
	nmake /$(MAKEFLAGS) wipt-put.exe
	cd ..
	-rmdir /s /q bin 2> nul
	-mkdir bin 2> nul
	copy /y wipt-get\wipt-get.exe bin
	copy /y wipt-put\wipt-put.exe bin
	copy /y wiptlib\wiptlib.dll bin
	copy /y System.Installer\ACM.Wipt.WindowsInstaller.dll bin
!IFDEF DEBUG
	copy /y wipt-get\wipt-get.pdb bin
	copy /y wipt-put\wipt-put.pdb bin
	copy /y wiptlib\wiptlib.pdb bin
!ENDIF

AssemblyVersion.cs: Wipt.ver
	echo using System.Reflection; > AssemblyVersion.cs
	echo [assembly:AssemblyVersion("$(ProductVersion).*")] >> AssemblyVersion.cs

wipt.msi: all
	candle /nologo /dProductVersion=$(ProductVersion) wipt.wxs
	light /nologo wipt.wixobj
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt.msi

clean:
	-rmdir /s /q bin 2> nul
	-del Wipt.msi Wipt.wixobj Wipt.ncb AssemblyVersion.cs 2> nul
	cd System.Installer
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\wipt-put
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\wipt-gui
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..
