.SILENT:

WiptLib.dll: src\Library.cs src\Repository.cs src\AssemblyInfo.cs ..\AssemblyVersion.cs
	csc /nologo $(DEBUG) /optimize /target:library /doc:WiptLib.xml /out:WiptLib.dll src\Library.cs src\Repository.cs src\AssemblyInfo.cs ..\AssemblyVersion.cs
!IFNDEF DEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" WiptLib.dll
!ENDIF

clean:
	-del WiptLib.dll WiptLib.pdb WiptLib.xml
