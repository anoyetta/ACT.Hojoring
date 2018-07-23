!include macro.inc

all:
	cd lib
	nmake /f Makefile.mak
	cd ..
	cd bin
	nmake /f Makefile.mak
	cd ..

clean:
	cd lib
	nmake /f Makefile.mak clean
	cd ..
	cd bin
	nmake /f Makefile.mak clean
	cd ..

install::
	@if not exist "$(HTS_ENGINE_API_INSTALLDIR)\lib" mkdir "$(HTS_ENGINE_API_INSTALLDIR)\lib"
	cd lib
	copy *.lib $(HTS_ENGINE_API_INSTALLDIR)\lib
	cd ..
	@if not exist "$(HTS_ENGINE_API_INSTALLDIR)\bin" mkdir "$(HTS_ENGINE_API_INSTALLDIR)\bin"
	cd bin
	copy *.exe $(HTS_ENGINE_API_INSTALLDIR)\bin
	cd ..
	@if not exist "$(HTS_ENGINE_API_INSTALLDIR)\include" mkdir "$(HTS_ENGINE_API_INSTALLDIR)\include"
	cd include
	copy *.h $(HTS_ENGINE_API_INSTALLDIR)\include
	cd ..
