.SILENT:

!INCLUDE "Wipt.ver"

all: AssemblyVersion.cs
	-rmdir /s /q bin 2> nul
	-mkdir bin 2> nul
	echo Building Windows Installer assembly...
	cd WindowsInstaller
	nmake /$(MAKEFLAGS) ACM.Wipt.WindowsInstaller.dll
	copy /y ACM.Wipt.WindowsInstaller.dll ..\bin > nul
!IFNDEF NODEBUG
	copy /y ACM.Wipt.WindowsInstaller.pdb ..\bin > nul
!ENDIF
	echo Building WiptLib...
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) WiptLib.dll
	copy /y wiptlib.dll ..\bin > nul
!IFNDEF NODEBUG
	copy /y wiptlib.pdb ..\bin > nul
!ENDIF
	echo Building wipt-get...
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) wipt-get.exe
	copy /y wipt-get.exe ..\bin > nul
!IFNDEF NODEBUG
	copy /y wipt-get.pdb ..\bin > nul
!ENDIF
	echo Building wipt-put...
	cd ..\wipt-put
	nmake /$(MAKEFLAGS) wipt-put.exe
	copy /y wipt-put.exe ..\bin > nul
!IFNDEF NODEBUG
	copy /y wipt-put.pdb ..\bin > nul
!ENDIF
	cd ..

AssemblyVersion.cs: Wipt.ver
	echo using System.Reflection; > AssemblyVersion.cs
	echo [assembly:AssemblyVersion("$(ProductVersion).*")] >> AssemblyVersion.cs

wipt.msi: skipunregister
	echo Attempting to verify full signatures on binaries.
	echo MSI creation will not continue if binaries are not fully-signed.
	sn -q -v WindowsInstaller\ACM.Wipt.WindowsInstaller.dll >&2
	sn -q -v WiptLib\WiptLib.dll >&2
	sn -q -v wipt-get\wipt-get.exe >&2
	sn -q -v wipt-put\wipt-put.exe >&2
	candle /nologo /dProductVersion=$(ProductVersion) Wipt.wxs
	light /nologo -ext WixUIExtension -cultures:en-us wipt.wixobj
!IFDEF NODEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt.msi
!ENDIF

skipregister: all
	sn -q -Vr bin\ACM.Wipt.WindowsInstaller.dll >&2
	sn -q -Vr bin\WiptLib.dll >&2
	sn -q -Vr bin\wipt-get.exe >&2
	sn -q -Vr bin\wipt-put.exe >&2

skipunregister: all
	-sn -q -Vu bin\ACM.Wipt.WindowsInstaller.dll 2>nul >&2
	-sn -q -Vu bin\WiptLib.dll 2>nul >&2
	-sn -q -Vu bin\wipt-get.exe 2>nul >&2
	-sn -q -Vu bin\wipt-put.exe 2>nul >&2

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
