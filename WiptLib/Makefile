.SILENT:

WiptLib.dll: src\Library.cs src\Repository.cs src\AssemblyInfo.cs
	csc /nologo $(DEBUG) /optimize /target:library /doc:WiptLib.xml /out:WiptLib.dll src\Library.cs src\Repository.cs src\AssemblyInfo.cs
!IF ("$(FRAMEWORKVERSION)"=="v1.1.4322") && !DEFINED(DEBUG)
	signcode -cn "Special Interest Group for Windows Development" -t "http://timestamp.verisign.com/scripts/timestamp.dll" WiptLib.dll
!ELSEIFNDEF DEBUG
	signtool sign /n "Special Interest Group for Windows Development" WiptLib.dll
!ENDIF

clean:
	-del WiptLib.dll WiptLib.pdb WiptLib.xml
