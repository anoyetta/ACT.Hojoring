@echo off
setlocal
rem -------------------------------------------
set BUILD_INFO=build.info
call :load_build_info

rem -------------------------------------------
if not exist "%HTS_ENGINE_API%" (
	echo %HTS_ENGINE_API%フォルダが見つかりません。
	echo prepare.bat を実行してから、
	echo このバッチファイルを実行してください。
	goto quit
)

rem -------------------------------------------
if not exist "%OPEN_JTALK%" (
	echo %OPEN_JTALK%フォルダが見つかりません。
	echo prepare.bat を実行してから、
	echo このバッチファイルを実行してください。
	goto quit
)

rem -------------------------------------------
if %PROCESSOR_ARCHITECTURE% == x86 set ProgramFiles(x86)=C:\Program Files
set VCVARSALL_PATH=%PROGRAMFILES(x86)%\Microsoft Visual Studio %VSVER%\VC\vcvarsall.bat
if not exist "%VCVARSALL_PATH%" (
	echo エラー：このバッチファイルを実行するには、VC++ がインストールされている必要があります。
	echo お手数ですが、Visual Studio C++ をインストールしてから実行してください。
	goto quit
)

call "%VCVARSALL_PATH%" %PLATFORM%
(cl 2>&1)>NUL
if errorlevel 9009 (
	echo エラー：このコンパイラでは %PLATFORM% 向けのビルドはできないようです。
	goto quit
)

rem -------------------------------------------
if exist cscnv.cpp cl /clr:safe cscnv.cpp
if exist cscnv.obj del cscnv.obj

rem -------------------------------------------
call :save_build_info

rem -------------------------------------------
cd "%HTS_ENGINE_API%"
copy "..\%BUILD_INFO%" .>NUL
copy ..\macro.inc .>NUL
nmake /f Makefile.mak
nmake /f Makefile.mak install
cd ..

cd "%OPEN_JTALK%"
copy "..\%BUILD_INFO%" .>NUL
copy ..\macro.inc .>NUL
nmake /f Makefile.mak
nmake /f Makefile.mak install
cd ..

rem -------------------------------------------
if not exist bin md bin
if exist "%BUILD_INFO%" copy "%BUILD_INFO%" bin>NUL
if exist "%OPEN_JTALK%\bin\open_jtalk.exe" copy "%OPEN_JTALK%\bin\open_jtalk.exe" bin>NUL
if exist cscnv.exe copy cscnv.exe bin>NUL
if not exist dic md dic
cd "%OPEN_JTALK%\mecab-naist-jdic"
for %%i in ( char.bin matrix.bin sys.dic unk.dic left-id.def right-id.def rewrite.def pos-id.def ) do (
	if exist %%i copy %%i ..\..\dic>NUL
)
cd ..\..

rem -------------------------------------------
if not exist "%OPENJTALK_INSTALLDIR%\bin" md "%OPENJTALK_INSTALLDIR%\bin"
if exist "%BUILD_INFO%" copy "%BUILD_INFO%" "%OPENJTALK_INSTALLDIR%\bin">NUL
if exist cscnv.exe copy cscnv.exe "%OPENJTALK_INSTALLDIR%\bin">NUL
if exist ojtalk.bat copy ojtalk.bat "%OPENJTALK_INSTALLDIR%">NUL

rem -------------------------------------------
:quit
echo.
set /p key=エンターを押すと終了します...
goto :eof

rem -------------------------------------------
:load_build_info
set PATCH_JPCOMMON_LABEL=_EMPTY_
set PATCH_OPEN_JTALK_x64=_EMPTY_
set PATCH_HTS_ENGINE_API_x64=_EMPTY_
set CONVERT_TO_ESCAPE_SEQUENCE=_EMPTY_
set CHARSET=_EMPTY_
set PLATFORM=_EMPTY_
set VSVER=_EMPTY_
set OPENJTALK_INSTALLDIR=_EMPTY_
set BINDIR=_EMPTY_
set DICDIR=_EMPTY_
set VOICEDIR=_EMPTY_
set OPEN_JTALK=_EMPTY_
set HTS_ENGINE_API=_EMPTY_
set HTS_VOICE=_EMPTY_
if not exist %BUILD_INFO% goto skip_load_info
for /F "tokens=1,2 delims==" %%a in (%BUILD_INFO%) do (
	if "%%a"=="PATCH_JPCOMMON_LABEL" set PATCH_JPCOMMON_LABEL=%%b
	if "%%a"=="PATCH_OPEN_JTALK_x64" set PATCH_OPEN_JTALK_x64=%%b
	if "%%a"=="PATCH_HTS_ENGINE_API_x64" set PATCH_HTS_ENGINE_API_x64=%%b
	if "%%a"=="CONVERT_TO_ESCAPE_SEQUENCE" set CONVERT_TO_ESCAPE_SEQUENCE=%%b
	if "%%a"=="CHARSET" set CHARSET=%%b
	if "%%a"=="PLATFORM" set PLATFORM=%%b
	if "%%a"=="VSVER" set VSVER=%%b
	if "%%a"=="OPENJTALK_INSTALLDIR" set OPENJTALK_INSTALLDIR=%%b
	if "%%a"=="BINDIR" set BINDIR=%%b
	if "%%a"=="DICDIR" set DICDIR=%%b
	if "%%a"=="VOICEDIR" set VOICEDIR=%%b
	if "%%a"=="OPEN_JTALK" set OPEN_JTALK=%%b
	if "%%a"=="HTS_ENGINE_API" set HTS_ENGINE_API=%%b
	if "%%a"=="HTS_VOICE" set HTS_VOICE=%%b
)
:skip_load_info
if "%PATCH_JPCOMMON_LABEL%" == "_EMPTY_" set PATCH_JPCOMMON_LABEL=YES
if "%PATCH_OPEN_JTALK_x64%" == "_EMPTY_" set PATCH_OPEN_JTALK_x64=YES
if "%PATCH_HTS_ENGINE_API_x64%" == "_EMPTY_" set PATCH_HTS_ENGINE_API_x64=YES
if "%CONVERT_TO_ESCAPE_SEQUENCE%" == "_EMPTY_" set CONVERT_TO_ESCAPE_SEQUENCE=YES
if "%CHARSET%" == "_EMPTY_" set CHARSET=SHIFT_JIS
if "%PLATFORM%" == "_EMPTY_" set PLATFORM=x86
if "%VSVER%" == "_EMPTY_" set VSVER=10.0
if "%OPENJTALK_INSTALLDIR%" == "_EMPTY_" set OPENJTALK_INSTALLDIR=C:\open_jtalk
if "%BINDIR%" == "_EMPTY_" set BINDIR=bin
if "%DICDIR%" == "_EMPTY_" set DICDIR=dic
if "%VOICEDIR%" == "_EMPTY_" set VOICEDIR=voice
if "%OPEN_JTALK%" == "_EMPTY_" set OPEN_JTALK=open_jtalk
if "%HTS_ENGINE_API%" == "_EMPTY_" set HTS_ENGINE_API=hts_engine_API
if "%HTS_VOICE%" == "_EMPTY_" set HTS_VOICE=hts_voice_nitech_jp_atr503_m001
goto :eof

rem -------------------------------------------
:save_build_info
echo PATCH_JPCOMMON_LABEL=%PATCH_JPCOMMON_LABEL%>%BUILD_INFO%
echo PATCH_OPEN_JTALK_x64=%PATCH_OPEN_JTALK_x64%>>%BUILD_INFO%
echo PATCH_HTS_ENGINE_API_x64=%PATCH_HTS_ENGINE_API_x64%>>%BUILD_INFO%
echo CONVERT_TO_ESCAPE_SEQUENCE=%CONVERT_TO_ESCAPE_SEQUENCE%>>%BUILD_INFO%
echo CHARSET=%CHARSET%>>%BUILD_INFO%
echo PLATFORM=%PLATFORM%>>%BUILD_INFO%
echo VSVER=%VSVER%>>%BUILD_INFO%
echo OPENJTALK_INSTALLDIR=%OPENJTALK_INSTALLDIR%>>%BUILD_INFO%
echo BINDIR=%BINDIR%>>%BUILD_INFO%
echo DICDIR=%DICDIR%>>%BUILD_INFO%
echo VOICEDIR=%VOICEDIR%>>%BUILD_INFO%
echo OPEN_JTALK=%OPEN_JTALK%>>%BUILD_INFO%
echo HTS_ENGINE_API=%HTS_ENGINE_API%>>%BUILD_INFO%
echo HTS_VOICE=%HTS_VOICE%>>%BUILD_INFO%
goto :eof

