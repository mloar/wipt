.SILENT:

!INCLUDE "..\Wipt.ver"

CSFLAGS=/nologo
!IFNDEF NODEBUG
CSFLAGS=$(CSFLAGS) /debug
!ELSE
CSFLAGS=$(CSFLAGS) /optimize
!ENDIF

wipt-get.exe: wipt-get.module
	al /nologo /t:exe /out:wipt-get.exe /copyright:"Copyright (c) 2006 Association for Computing Machinery at the University of Illinois at Urbana-Champaign.  All rights reserved." /keyfile:..\keys\wipt-get.snk /main:ACM.Wipt.wipt_get.Main /company:"Special Interest Group for Windows Development" /title:"Wipt Application Maintenance Utility" /version:$(AssemblyVersion) /product:"Windows Installer Package Tool" /productversion:$(ProductVersion) /win32icon:res\App.ico /embed:res\wipt-get.exe.manifest wipt-get.module
!IFDEF NODEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt-get.exe
!ENDIF

wipt-get.module: src\Main.cs ..\System.Installer\ACM.Wipt.WindowsInstaller.dll ..\WiptLib\WiptLib.dll
	csc $(CSFLAGS) /t:module /out:wipt-get.module /r:..\System.Installer\ACM.Wipt.WindowsInstaller.dll /r:..\WiptLib\WiptLib.dll src\Main.cs

clean:
	-del wipt-get.exe wipt-get.pdb wipt-get.res wipt-get.module 2> nul
	-rmdir /s /q obj 2> nul
