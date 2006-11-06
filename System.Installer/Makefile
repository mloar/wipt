.SILENT:

!INCLUDE "..\Wipt.ver"

CFLAGS=/nologo /D "WIN32" /D "_WINDLL" /D "_MBCS" /GS /FU "mscorlib.dll" /FU "System.dll"
LFLAGS=/NOLOGO
RCFLAGS=/DWIPTVER=$(FileVersion)
!IFNDEF NODEBUG
CFLAGS=$(CFLAGS) /Zi
LFLAGS=$(LFLAGS) /DEBUG
RCFLAGS=$(RCFLAGS) /D_DEBUG
!ENDIF

!IF "$(TARGET_FRAMEWORK)" != "1.1"
CFLAGS=$(CFLAGS) /clr:oldSyntax
!ELSE
CFLAGS=$(CFLAGS) /clr
!ENDIF

ACM.Wipt.WindowsInstaller.dll: ACM.Wipt.WindowsInstaller.obj AssemblyInfo.obj ACM.Wipt.WindowsInstaller.res stdafx.obj
	link $(LFLAGS) /OUT:ACM.Wipt.WindowsInstaller.dll /INCREMENTAL:NO /DLL /FIXED:No nochkclr.obj mscoree.lib msi.lib wintrust.lib kernel32.lib ACM.Wipt.WindowsInstaller.obj AssemblyInfo.obj stdafx.obj ACM.Wipt.WindowsInstaller.res
!IFDEF NODEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" ACM.Wipt.WindowsInstaller.dll
!ENDIF

ACM.Wipt.WindowsInstaller.obj: StdAfx.pch ACM.Wipt.WindowsInstaller.cpp ACM.Wipt.WindowsInstaller.h
	cl $(CFLAGS) /c /Yu"stdafx.h" ACM.Wipt.WindowsInstaller.cpp

AssemblyInfo.obj: StdAfx.pch AssemblyInfo.cpp
	cl $(CFLAGS) /c AssemblyInfo.cpp

ACM.Wipt.WindowsInstaller.res: ACM.Wipt.WindowsInstaller.rc resource.h
	rc $(RCFLAGS) ACM.Wipt.WindowsInstaller.rc

stdafx.pch: StdAfx.cpp StdAfx.h
	cl $(CFLAGS) /c /Yc"stdafx.h" StdAfx.cpp

clean:
	-del *.dll *.obj *.pch *.pdb *.idb *.res *.aps *.manifest
