.SILENT:

System.Installer.dll: System.Installer.obj AssemblyInfo.obj System.Installer.res
	link /OUT:System.Installer.dll /INCREMENTAL:NO /NOLOGO /DLL /FIXED:No nochkclr.obj mscoree.lib msi.lib kernel32.lib System.Installer.obj AssemblyInfo.obj System.Installer.res
!IF "$(FRAMEWORKVERSION)"=="v1.1.4322"
	signcode -cn "Special Interest Group for Windows Development" System.Installer.dll
!ELSE
	signtool sign /n "Special Interest Group for Windows Development" System.Installer.dll
!ENDIF

System.Installer.obj: StdAfx.pch System.Installer.cpp
	cl /D "WIN32" /D "_WINDLL" /D "_MBCS" /MT /GS /Yu"stdafx.h" /nologo /c /Zi /clr /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\mscorlib.dll" /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.dll" System.Installer.cpp

AssemblyInfo.obj: StdAfx.pch AssemblyInfo.cpp
	cl /D "WIN32" /D "_WINDLL" /D "_MBCS" /MT /GS  /nologo /c /Zi /clr /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\mscorlib.dll" /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.dll" AssemblyInfo.cpp

System.Installer.res: System.Installer.rc resource.h
	rc System.Installer.rc

stdafx.pch: StdAfx.cpp StdAfx.h
	cl /D "WIN32" /D "_WINDLL" /D "_MBCS" /MT /GS /Yc"stdafx.h" /nologo /c /Zi /clr /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\mscorlib.dll" /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.dll" StdAfx.cpp

clean:
	-del System.Installer.dll System.Installer.obj stdafx.pch stdafx.obj vc70.pdb vc70.idb AssemblyInfo.obj System.Installer.res System.Installer.aps
