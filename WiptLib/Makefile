.SILENT:

WiptLib.dll: src\Library.cs src\AssemblyInfo.cs
	csc /nologo $(DEBUG) /optimize /target:library /doc:WiptLib.xml /out:WiptLib.dll src\Library.cs src\AssemblyInfo.cs
	signcode -cn "Special Interest Group for Windows Development" WiptLib.dll

clean:
	-del WiptLib.dll WiptLib.pdb WiptLib.xml
