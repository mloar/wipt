.SILENT:

all:
	cd System.Installer
	nmake /$(MAKEFLAGS) System.Installer.dll
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) WiptLib.dll
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) wipt-get.exe
!IF "$(FRAMEWORKVERSION)"=="v2.0.50727"
	cd ..\wipt-gui
	nmake /$(MAKEFLAGS) wipt-gui.exe
!ENDIF
	cd ..
  -rmdir /s /q bin 2> nul
	mkdir bin
	copy /y wipt-get\wipt-get.exe bin
	copy /y wiptlib\wiptlib.dll bin
	copy /y System.Installer\System.Installer.dll bin
!IF "$(FRAMEWORKVERSION)"=="v2.0.50727"
	copy /y wipt-gui\wipt-gui.exe bin
!ENDIF
!IF "$(DEBUG)"!=""
	copy /y wipt-get\wipt-get.pdb bin
	copy /y wiptlib\wiptlib.pdb bin
!IF "$(FRAMEWORKVERSION)"=="v2.0.50727"
	copy /y wipt-gui\wipt-gui.pdb bin
!ENDIF
!ENDIF

wipt.msi: all
	candle /nologo wipt.wxs
	light /nologo wipt.wixobj
!IF "$(FRAMEWORKVERSION)"=="v1.1.4322"
	signcode -cn "Special Interest Group for Windows Development" -t "http://timestamp.verisign.com/scripts/timestamp.dll" wipt.msi
!ELSE
	signtool sign /n "Special Interest Group for Windows Development" wipt.msi
!ENDIF

clean:
	-rmdir /s /q bin 2> nul
	-del Wipt.msi Wipt.wixobj Wipt.ncb 2> nul
	cd System.Installer
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\wipt-get
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\WiptLib
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..\wipt-gui
	nmake /$(MAKEFLAGS) clean 2> nul
	cd ..
