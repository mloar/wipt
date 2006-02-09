.SILENT:

all:
	cd System.Installer
	nmake /$(MAKEFLAGS) System.Installer.dll
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) WiptLib.dll
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) wipt-get.exe
	cd ..
	-mkdir bin
	copy /y wipt-get\wipt-get.exe bin
	copy /y wiptlib\wiptlib.dll bin
	copy /y System.Installer\System.Installer.dll bin

wipt.msi: all
	candle /nologo wipt.wxs
	light /nologo wipt.wixobj
	signcode -cn "Special Interest Group for Windows Development" wipt.msi

clean:
	-del Wipt.msi Wipt.wixobj
	cd System.Installer
	nmake /$(MAKEFLAGS) clean
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) clean
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) clean
	cd ..
