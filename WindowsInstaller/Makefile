.SILENT:

CSFLAGS=/nologo /warn:0

!IFDEF NODEBUG
CSFLAGS=$(CSFLAGS) /optimize
!ELSE
CSFLAGS=$(CSFLAGS) /debug
!ENDIF 

ACM.Wipt.WindowsInstaller.dll: src\WindowsInstaller.cs src\AssemblyInfo.cs ..\AssemblyVersion.cs
	csc $(CSFLAGS) /target:library /doc:ACM.Wipt.WindowsInstaller.xml /out:ACM.Wipt.WindowsInstaller.dll src\WindowsInstaller.cs src\AssemblyInfo.cs ..\AssemblyVersion.cs
!IFDEF NODEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" ACM.Wipt.WindowsInstaller.dll
!ENDIF

clean:
	-del ACM.Wipt.WindowsInstaller.dll ACM.Wipt.WindowsInstaller.pdb ACM.Wipt.WindowsInstaller.xml
