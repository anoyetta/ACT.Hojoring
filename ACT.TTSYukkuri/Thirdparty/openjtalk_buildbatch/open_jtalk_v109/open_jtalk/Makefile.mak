!include macro.inc

all:
	cd jpcommon
	$(MAKE) /f Makefile.mak
	cd ..
	cd mecab2njd
	$(MAKE) /f Makefile.mak
	cd ..
	cd njd
	$(MAKE) /f Makefile.mak
	cd ..
	cd njd_set_accent_phrase
	$(MAKE) /f Makefile.mak
	cd ..
	cd njd_set_accent_type
	$(MAKE) /f Makefile.mak
	cd ..
	cd njd_set_digit
	$(MAKE) /f Makefile.mak
	cd ..
	cd njd_set_long_vowel
	$(MAKE) /f Makefile.mak
	cd ..
	cd njd_set_pronunciation
	$(MAKE) /f Makefile.mak
	cd ..
	cd njd_set_unvoiced_vowel
	$(MAKE) /f Makefile.mak
	cd ..
	cd njd2jpcommon
	$(MAKE) /f Makefile.mak
	cd ..
	cd text2mecab
	$(MAKE) /f Makefile.mak
	cd ..
	cd mecab\src
	$(MAKE) /f Makefile.mak
	cd ..\..
	cd bin
	$(MAKE) /f Makefile.mak
	cd ..
	cd mecab-naist-jdic
	nmake /f Makefile.mak
	cd ..

clean:
	cd jpcommon
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd mecab2njd
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd njd
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd njd_set_accent_phrase
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd njd_set_accent_type
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd njd_set_digit
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd njd_set_long_vowel
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd njd_set_pronunciation
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd njd_set_unvoiced_vowel
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd njd2jpcommon
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd text2mecab
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd mecab\src
	$(MAKE) /f Makefile.mak clean
	cd ..\..
	cd bin
	$(MAKE) /f Makefile.mak clean
	cd ..
	cd mecab-naist-jdic
	nmake /f Makefile.mak clean
	cd ..

install::
	if not exist "$(OPENJTALK_INSTALLDIR)\bin" md "$(OPENJTALK_INSTALLDIR)\bin"
	copy bin\open_jtalk.exe "$(OPENJTALK_INSTALLDIR)\bin"
	copy ..\cscnv.exe "$(OPENJTALK_INSTALLDIR)\bin"
	copy ..\ojtalk.bat "$(OPENJTALK_INSTALLDIR)"
	if not exist "$(OPENJTALK_INSTALLDIR)\$(DICDIR)" md "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	cd mecab-naist-jdic
	copy char.bin "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	copy matrix.bin "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	copy sys.dic "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	copy unk.dic "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	copy left-id.def "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	copy right-id.def "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	copy rewrite.def "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	copy pos-id.def "$(OPENJTALK_INSTALLDIR)\$(DICDIR)"
	cd ..
