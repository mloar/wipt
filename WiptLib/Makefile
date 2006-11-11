.SILENT:

CSFLAGS=/nologo

!IFDEF NODEBUG
CSFLAGS=$(CSFLAGS) /optimize
!ELSE
CSFLAGS=$(CSFLAGS) /debug
!ENDIF 

WiptLib.dll: src\Library.cs src\Repository.cs src\AssemblyInfo.cs ..\AssemblyVersion.cs
	csc $(CSFLAGS) /target:library /doc:WiptLib.xml /out:WiptLib.dll src\Library.cs src\Repository.cs src\AssemblyInfo.cs ..\AssemblyVersion.cs
!IFDEF NODEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" WiptLib.dll
!ENDIF

clean:
	-del WiptLib.dll WiptLib.pdb WiptLib.xml
