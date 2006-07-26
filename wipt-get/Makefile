.SILENT:

wipt-get.exe: src\Main.cs src\AssemblyInfo.cs res\App.ico ..\System.Installer\ACM.Wipt.WindowsInstaller.dll ..\WiptLib\WiptLib.dll
	csc /nologo $(DEBUG) /optimize /Win32Icon:res\App.ico /r:..\System.Installer\ACM.Wipt.WindowsInstaller.dll /r:..\WiptLib\WiptLib.dll /out:wipt-get.exe src\Main.cs src\AssemblyInfo.cs
!IF ("$(FRAMEWORKVERSION)"=="v1.1.4322") && !DEFINED(DEBUG)
	signcode -cn "Special Interest Group for Windows Development" -t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt-get.exe
!ELSEIFNDEF DEBUG
	signtool sign /n "Special Interest Group for Windows Development" wipt-get.exe
!ENDIF

clean:
	-del wipt-get.exe wipt-get.pdb 2> nul
	-rmdir /s /q obj 2> nul
