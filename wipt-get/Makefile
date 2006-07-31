.SILENT:

wipt-get.exe: src\Main.cs src\AssemblyInfo.cs res\App.ico ..\System.Installer\ACM.Wipt.WindowsInstaller.dll ..\WiptLib\WiptLib.dll
	csc /nologo $(DEBUG) /optimize /Win32Icon:res\App.ico /r:..\System.Installer\ACM.Wipt.WindowsInstaller.dll /r:..\WiptLib\WiptLib.dll /out:wipt-get.exe src\Main.cs src\AssemblyInfo.cs
!IFNDEF DEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt-get.exe
!ENDIF

clean:
	-del wipt-get.exe wipt-get.pdb 2> nul
	-rmdir /s /q obj 2> nul
