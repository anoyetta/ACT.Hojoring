
all:
	cd src
	nmake /f Makefile.mak
	cd ..

clean:
	cd src
	nmake /f Makefile.mak clean
	cd ..
