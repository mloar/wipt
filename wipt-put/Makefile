.SILENT:

!INCLUDE "..\Wipt.ver"

CSFLAGS=/nologo
!IFNDEF NODEBUG
CSFLAGS=$(CSFLAGS) /debug
!ELSE
CSFLAGS=$(CSFLAGS) /optimize
!ENDIF

wipt-put.exe: src\Main.cs ..\WindowsInstaller\ACM.Wipt.WindowsInstaller.dll ..\WiptLib\WiptLib.dll ..\AssemblyVersion.cs
	csc $(CSFLAGS) /t:exe /out:wipt-put.exe /r:..\WindowsInstaller\ACM.Wipt.WindowsInstaller.dll /r:..\WiptLib\WiptLib.dll src\Main.cs ..\AssemblyVersion.cs
!IFDEF NODEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt-put.exe
!ENDIF

clean:
	-del wipt-put.exe wipt-put.pdb wipt-put.res wipt-put.module 2> nul
	-rmdir /s /q obj 2> nul
