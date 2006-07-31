.SILENT:

ACM.Wipt.WindowsInstaller.dll: ACM.Wipt.WindowsInstaller.obj AssemblyInfo.obj ACM.Wipt.WindowsInstaller.res
	link /OUT:ACM.Wipt.WindowsInstaller.dll /INCREMENTAL:NO /NOLOGO /DLL /FIXED:No nochkclr.obj mscoree.lib msi.lib kernel32.lib ACM.Wipt.WindowsInstaller.obj AssemblyInfo.obj ACM.Wipt.WindowsInstaller.res
!IFNDEF DEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" ACM.Wipt.WindowsInstaller.dll
!ENDIF

ACM.Wipt.WindowsInstaller.obj: StdAfx.pch ACM.Wipt.WindowsInstaller.cpp
	cl /D "WIN32" /D "_WINDLL" /D "_MBCS" /MT /GS /Yu"stdafx.h" /nologo /c /Zi /clr /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\mscorlib.dll" /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.dll" ACM.Wipt.WindowsInstaller.cpp

AssemblyInfo.obj: StdAfx.pch AssemblyInfo.cpp
	cl /D "WIN32" /D "_WINDLL" /D "_MBCS" /MT /GS  /nologo /c /Zi /clr /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\mscorlib.dll" /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.dll" AssemblyInfo.cpp

ACM.Wipt.WindowsInstaller.res: ACM.Wipt.WindowsInstaller.rc resource.h
	rc ACM.Wipt.WindowsInstaller.rc

stdafx.pch: StdAfx.cpp StdAfx.h
	cl /D "WIN32" /D "_WINDLL" /D "_MBCS" /MT /GS /Yc"stdafx.h" /nologo /c /Zi /clr /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\mscorlib.dll" /FU "C:\WINDOWS\Microsoft.NET\Framework\v1.1.4322\System.dll" StdAfx.cpp

clean:
	-del ACM.Wipt.WindowsInstaller.dll ACM.Wipt.WindowsInstaller.obj stdafx.pch stdafx.obj vc70.pdb vc70.idb AssemblyInfo.obj ACM.Wipt.WindowsInstaller.res ACM.Wipt.WindowsInstaller.aps
