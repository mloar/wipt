.SILENT:

!INCLUDE "..\Wipt.ver"

CSFLAGS=/nologo
!IFNDEF NODEBUG
CSFLAGS=$(CSFLAGS) /debug
!ELSE
# No optimications!
CSFLAGS=$(CSFLAGS)
!ENDIF

wipt-get.exe: src\Main.cs src\AssemblyInfo.cs ..\WindowsInstaller\ACM.Wipt.WindowsInstaller.dll ..\WiptLib\WiptLib.dll ..\AssemblyVersion.cs
	csc $(CSFLAGS) /t:exe /out:wipt-get.exe /r:..\WindowsInstaller\ACM.Wipt.WindowsInstaller.dll /r:..\WiptLib\WiptLib.dll src\Main.cs src\AssemblyInfo.cs ..\AssemblyVersion.cs
!IFDEF NODEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt-get.exe
!ENDIF

clean:
	-del wipt-get.exe wipt-get.pdb wipt-get.res wipt-get.module 2> nul
	-rmdir /s /q obj 2> nul
