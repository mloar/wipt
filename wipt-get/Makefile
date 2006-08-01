.SILENT:

wipt-get.exe: src\Main.cs ..\AssemblyVersion.cs res\App.ico ..\System.Installer\ACM.Wipt.WindowsInstaller.dll ..\WiptLib\WiptLib.dll
	csc /nologo $(DEBUG) /optimize /out:wipt-get.exe /Win32Icon:res\App.ico /r:..\System.Installer\ACM.Wipt.WindowsInstaller.dll /r:..\WiptLib\WiptLib.dll ..\AssemblyVersion.cs src\AssemblyInfo.cs src\Main.cs
!IFNDEF DEBUG
	signtool sign /n "Special Interest Group for Windows Development" /t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt-get.exe
!ENDIF

clean:
	-del wipt-get.exe wipt-get.pdb 2> nul
	-rmdir /s /q obj 2> nul
