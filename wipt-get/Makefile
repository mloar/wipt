.SILENT:

wipt-get.exe: src\Main.cs src\AssemblyInfo.cs res\App.ico ..\System.Installer\System.Installer.dll ..\WiptLib\WiptLib.dll
	csc /nologo $(DEBUG) /optimize /Win32Icon:res\App.ico /r:..\System.Installer\System.Installer.dll /r:..\WiptLib\WiptLib.dll /out:wipt-get.exe src\Main.cs src\AssemblyInfo.cs
	signcode -cn "Special Interest Group for Windows Development" wipt-get.exe

clean:
	-del wipt-get.exe wipt-get.pdb
