@echo off
setlocal
rem -------------------------------------------
set BUILD_INFO=build.info
call :load_build_info

rem -------------------------------------------
set PATH=%PATH%;C:\MinGW\msys\1.0\bin

rem -------------------------------------------
call :search_vc
set VCVARSALL_PATH=%RESULT%
if exist "%VCVARSALL_PATH%" (set VC_EXIST=1) else (set VC_EXIST=0)

rem -------------------------------------------
call :search_archive %HTS_VOICE%-*.tar.gz
if "%SEARCH_RESULT%" == "" goto quit
set NAME_HTS_VOICE=%SEARCH_RESULT%

call :search_archive %HTS_ENGINE_API%-*.tar.gz
if "%SEARCH_RESULT%" == "" goto quit
set NAME_HTS_ENGINE_API=%SEARCH_RESULT%

call :search_archive %OPEN_JTALK%-*.tar.gz
if "%SEARCH_RESULT%" == "" goto quit
set NAME_OPEN_JTALK=%SEARCH_RESULT%

call :search_archive makefile_%OPEN_JTALK%-*_win.tar.gz
set NAME_OPEN_JTALK_MAKE=%SEARCH_RESULT%

rem -------------------------------------------
set TAR_EXIST=0
(tar --version 2>&1)>NUL
if not errorlevel 9009 (
	set TAR_EXIST=1
)
if not %TAR_EXIST% == 0 goto skip_no_7z_command
set PATH=%ProgramFiles%\7-Zip;%ProgramFiles(x86)%\7-Zip;%PATH%
(7z 2>&1)>NUL
if errorlevel 9009 (
	echo エラー：tar.gzファイルを展開するコマンドが使えません。
	echo tar、7zip などのどれかをインストールして、やり直してください。
	goto quit
)
:skip_no_7z_command

rem -------------------------------------------
set PATCH_LIST=
(patch --version 2>&1)>NUL
if not errorlevel 9009 (set PATCH_EXIST=1) else (set PATCH_EXIST=0)

rem -------------------------------------------
if %VC_EXIST%==0 (
	echo エラー：ソースをビルドするには、
	echo Visual Studio C++ がインストールされている必要があります。
)
if not %VC_EXIST%==0 call "%VCVARSALL_PATH%" %PLATFORM%
if exist cscnv.cpp cl /clr:safe cscnv.cpp>NUL
if exist cscnv.obj del cscnv.obj
if exist csesc.cpp cl /EHsc csesc.cpp>NUL
if exist csesc.obj del csesc.obj

rem -------------------------------------------
if not exist %VOICEDIR% md %VOICEDIR%
if exist %VOICEDIR%\%HTS_VOICE% goto skip_expand_hts_voice
copy %NAME_HTS_VOICE%.tar.gz %VOICEDIR%>NUL
cd %VOICEDIR%
call :expand %NAME_HTS_VOICE%
if not %RESULT% == 0 goto quit
ren %NAME_HTS_VOICE% %HTS_VOICE%
del %NAME_HTS_VOICE%.tar.gz
copy ..\voice.info %HTS_VOICE%>NUL
cd ..
:skip_expand_hts_voice

rem -------------------------------------------
if exist %HTS_ENGINE_API% goto skip_expand_hts_engine_API
call :expand %NAME_HTS_ENGINE_API%
if not %RESULT% == 0 goto quit
ren %NAME_HTS_ENGINE_API% %HTS_ENGINE_API%

if "%PATCH_HTS_ENGINE_API_x64%" == "" goto skip_patch_hts_engine_API
if not exist %NAME_HTS_ENGINE_API%_x64.patch goto skip_patch_hts_engine_API
if not "%PATCH_EXIST%" == "0" (
	patch -p0 < %NAME_HTS_ENGINE_API%_x64.patch
) else (
	set PATCH_LIST=%PATCH_LIST% %NAME_HTS_ENGINE_API%_x64.patch
)
:skip_patch_hts_engine_API
:skip_expand_hts_engine_API

rem -------------------------------------------
if exist %OPEN_JTALK% goto skip_expand_open_jtalk
call :expand %NAME_OPEN_JTALK%
if not %RESULT% == 0 goto quit
ren %NAME_OPEN_JTALK% %OPEN_JTALK%
call :expand %NAME_OPEN_JTALK_MAKE%
if not %RESULT% == 0 goto quit

if "%PATCH_OPEN_JTALK_x64%" == "" goto skip_patch_open_jtalk
if not exist %NAME_OPEN_JTALK%_x64.patch goto skip_patch_open_jtalk
if not "%PATCH_EXIST%" == "0" (
	patch -p0 < %NAME_OPEN_JTALK%_x64.patch
) else (
	set PATCH_LIST=%PATCH_LIST% %NAME_OPEN_JTALK%_x64.patch
)
:skip_patch_open_jtalk

if "%PATCH_JPCOMMON_LABEL%" == "" goto skip_patch_jpcommon_label
if not exist %NAME_OPEN_JTALK%_jpcommon_label.patch goto skip_patch_jpcommon_label
if not "%PATCH_EXIST%" == "0" (
	patch -p0 < %NAME_OPEN_JTALK%_jpcommon_label.patch
) else (
	set PATCH_LIST=%PATCH_LIST% %NAME_OPEN_JTALK%_jpcommon_label.patch
)
:skip_patch_jpcommon_label

if not exist csesc.exe goto skip_convert_to_escape_sequence
if "%CONVERT_TO_ESCAPE_SEQUENCE%" == "" goto skip_convert_to_escape_sequence
cd open_jtalk
for /R %%i in (*_utf_8.h *_euc_jp.h) do  (
	copy "%%i" "%%i.backup">NUL
	..\csesc<"%%i.backup">"%%i"
)
echo 非シフトJIS文字列をエスケープシーケンスに変換しました。
cd ..
:skip_convert_to_escape_sequence
:skip_expand_open_jtalk

rem -------------------------------------------
copy %BUILD_INFO% %OPEN_JTALK%>NUL
copy %BUILD_INFO% %HTS_ENGINE_API%>NUL

rem -------------------------------------------
if not exist bin md bin
if exist "%BUILD_INFO%" copy "%BUILD_INFO%" bin>NUL
if exist cscnv.exe copy cscnv.exe bin>NUL

rem -------------------------------------------
if "%OPENJTALK_INSTALLDIR%" == "" goto skip_install
if not exist "%OPENJTALK_INSTALLDIR%\bin" md "%OPENJTALK_INSTALLDIR%\bin"
if not exist "%OPENJTALK_INSTALLDIR%\%VOICEDIR%" md "%OPENJTALK_INSTALLDIR%\%VOICEDIR%"
xcopy voice %OPENJTALK_INSTALLDIR%\%VOICEDIR% /S /Y>NUL
if exist %BUILD_INFO% copy %BUILD_INFO% "%OPENJTALK_INSTALLDIR%\bin">NUL
if exist cscnv.exe copy cscnv.exe "%OPENJTALK_INSTALLDIR%\bin">NUL
if exist ojtalk.bat copy ojtalk.bat "%OPENJTALK_INSTALLDIR%">NUL
echo.
echo インストール先ディレクトリにファイルを転送しました。
:skip_install

rem -------------------------------------------
echo.
echo 準備が完了しました。
if "%PATCH_LIST%" == "" goto skip_patch_list
echo.
echo patchコマンドが無かったために以下のパッチが当てられませんでした。
for %%i in ( %PATCH_LIST% ) do echo ・%%i
echo 手作業で修正してください。
:skip_patch_list

echo make.bat もしくは open_jtalk\open_jtalk.sln を使って、ビルドしてください。

rem -------------------------------------------
:quit
echo.
set /p DUMMY="エンターを押すと終了します..." 
exit /b

rem --- subroutine ---
rem -------------------------------------------
:expand
set RESULT=0
if "%1" == "" (
	echo 展開する名前がありません。
	set RESULT=1
	goto :eof
)
if not exist %1.tar.gz (
	echo 展開する %1.tar.gz が見つかりません。
	set RESULT=1
	goto :eof
)
if "%TAR_EXIST%" == "0" (
	7z x %1.tar.gz>NUL
	7z x -y %1.tar>NUL
) else (
	tar xzvf %1.tar.gz>NUL
)
if exist %1.tar del %1.tar
echo %1.tar.gz を展開しました。
goto :eof

rem -------------------------------------------
:search_archive
set SEARCH_RESULT=
if "%1" == "" (
	echo 名前の指定がありません。
	goto :eof
)
set SEARCH_COUNTER=0
for %%i in ( %1 ) do set /a SEARCH_COUNTER+=1
if %SEARCH_COUNTER% == 0 (
	echo %1 を一つここにおいてください。
	goto :eof
)
if not %SEARCH_COUNTER% == 1 (
	echo %1 が複数あります。
	goto :eof
)
set SEARCH_ARCHIVE_NAME=
set SEARCH_ARCHIVE_EXT=
for %%i in ( %1  ) do (
	set SEARCH_ARCHIVE_NAME=%%~ni
	set SEARCH_ARCHIVE_EXT=%%~xi
)
if "%SEARCH_ARCHIVE_EXT%" == ".zip" (
	set SEARCH_RESULT=%SEARCH_ARCHIVE_NAME%
	goto :eof
)
if not "%SEARCH_ARCHIVE_EXT%" == ".gz" (
	echo 対応していない形式の圧縮ファイルです。
	goto :eof
)
set SEARCH_RESULT=
for %%i in ( %SEARCH_ARCHIVE_NAME% ) do set SEARCH_RESULT=%%~ni
if "%SEARCH_RESULT%" == "" (
	echo 何故か圧縮ファイルが見つかりません。
	goto :eof
)
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

rem -------------------------------------------
:search_vc
if %PROCESSOR_ARCHITECTURE% == x86 set ProgramFiles(x86)=C:\Program Files
if not "%VSVER%" == "" goto skip_search_vc
set VSVER=10.0
set RESULT=%ProgramFiles(x86)%\Microsoft Visual Studio %VSVER%\VC\vcvarsall.bat
if exist "%RESULT%" goto exit_search_vc
set VSVER=9.0
:skip_search_vc
set RESULT=%ProgramFiles(x86)%\Microsoft Visual Studio %VSVER%\VC\vcvarsall.bat
if exist "%RESULT%" goto exit_search_vc
:exit_search_vc
goto :eof


