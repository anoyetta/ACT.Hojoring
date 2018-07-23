@echo off
setlocal
set CURDIR=%~dp0
set PLATFORM=x86
set CHARSET=SHIFT_JIS
set DICDIR=dic
set VOICEDIR=voice
for /F "tokens=1,2 delims==" %%a in (%CURDIR%bin\build.info) do (
	if "%%a"=="PLATFORM" set PLATFORM=%%b
	if "%%a"=="CHARSET" set CHARSET=%%b
	if "%%a"=="DICDIR" set DICDIR=%%b
	if "%%a"=="VOICEDIR" set VOICEDIR=%%b
)
set VOICE_NAME=hts_voice_nitech_jp_atr503_m001
set VDIRBASE=%CURDIR%\%VOICEDIR%
set DDIR=%CURDIR%%DICDIR%
set TEXTFILE="%CURDIR%text.txt"
set TEXT=
set MESSAGE=open jtalk
set S=16000
set P=80
set A=0.06
set G=0
set B=0.0
set U=0.5
set Z=1600
set JM=1.0
set JF=1.0
set JL=1.0
set OW_VALUE=%CURDIR%out.wav
set OT_VALUE=%CURDIR%log.txt
set F_VALUE=%TEXTFILE%
set L_SWITCH=
set VNAME=
set S_VALUE=
set P_VALUE=
set A_VALUE=
set G_VALUE=
set B_VALUE=
set U_VALUE=
set Z_VALUE=
set JM_VALUE=
set JF_VALUE=
set JL_VALUE=
:loop
shift
if "%0" == "" goto exit_loop
if not "%0" == "-l" goto skip_sw_l
set L_SWITCH=-l
goto loop
:skip_sw_l
if not "%0" == "-vname" goto skip_sw_vname
shift
if "%0" == "" goto error
set VNAME=%0
goto loop
:skip_sw_vname
if not "%0" == "-f" goto skip_sw_f
shift
if "%0" == "" goto error
set F_VALUE=%0
goto loop
:skip_sw_f
if not "%0" == "-ow" goto skip_sw_ow
shift
if "%0" == "" goto error
set OW_VALUE=%0
goto loop
:skip_sw_ow
if not "%0" == "-ot" goto skip_sw_ot
shift
if "%0" == "" goto error
set OT_VALUE=%0
goto loop
:skip_sw_ot
if not "%0" == "-s" goto skip_sw_s
shift
if "%0" == "" goto error
set S_VALUE=%0
goto loop
:skip_sw_s
if not "%0" == "-p" goto skip_sw_p
shift
if "%0" == "" goto error
set P_VALUE=%0
goto loop
:skip_sw_p
if not "%0" == "-a" goto skip_sw_a
shift
if "%0" == "" goto error
set A_VALUE=%0
goto loop
:skip_sw_a
if not "%0" == "-g" goto skip_sw_g
shift
if "%0" == "" goto error
set G_VALUE=%0
goto loop
:skip_sw_g
if not "%0" == "-b" goto skip_sw_b
shift
if "%0" == "" goto error
set B_VALUE=%0
goto loop
:skip_sw_b
if not "%0" == "-u" goto skip_sw_u
shift
if "%0" == "" goto error
set U_VALUE=%0
goto loop
:skip_sw_u
if not "%0" == "-jm" goto skip_sw_jm
shift
if "%0" == "" goto error
set JM_VALUE=%0
goto loop
:skip_sw_jm
if not "%0" == "-jf" goto skip_sw_jf
shift
if "%0" == "" goto error
set JF_VALUE=%0
goto loop
:skip_sw_jf
if not "%0" == "-jl" goto skip_sw_jl
shift
if "%0" == "" goto error
set JL_VALUE=%0
goto loop
:skip_sw_jl
if not "%0" == "-z" goto skip_sw_z
shift
if "%0" == "" goto error
set Z_VALUE=%0
goto loop
:skip_sw_z
rem ### cat texts ###
if "%TEXT%"=="" (
	set TEXT=%0
) else (
	set TEXT=%TEXT% %0
)
goto loop
:exit_loop
rem ### set voice ###
set VDIR=%VDIRBASE%\%VOICE_NAME%
if "%VNAME%" == "" goto skip_set_voice
if exist "%VDIRBASE%\%VNAME%" set VDIR=%VDIRBASE%\%VNAME%
:skip_set_voice
if not exist "%VDIR%\voice.info" goto skip_load_voice_info
for /F "tokens=1,2 delims==" %%a in (%VDIR%\voice.info) do (
	if "%%a"=="MESSAGE" set MESSAGE=%%b
	if "%%a"=="s" set S=%%b
	if "%%a"=="p" set P=%%b
	if "%%a"=="a" set A=%%b
	if "%%a"=="g" set G=%%b
	if "%%a"=="b" set B=%%b
	if "%%a"=="u" set U=%%b
	if "%%a"=="z" set Z=%%b
	if "%%a"=="jm" set JM=%%b
	if "%%a"=="jf" set JF=%%b
	if "%%a"=="jl" set JL=%%b
)
:skip_load_voice_info
if "%S_VALUE%"=="" set S_VALUE=%S%
if "%P_VALUE%"=="" set P_VALUE=%P%
if "%A_VALUE%"=="" set A_VALUE=%A%
if "%G_VALUE%"=="" set G_VALUE=%G%
if "%B_VALUE%"=="" set B_VALUE=%B%
if "%U_VALUE%"=="" set U_VALUE=%U%
if "%Z_VALUE%"=="" set Z_VALUE=%Z%
if "%JM_VALUE%"=="" set JM_VALUE=%JM%
if "%JF_VALUE%"=="" set JF_VALUE=%JF%
if "%JL_VALUE%"=="" set JL_VALUE=%JL%
rem ### make text ###
if "%TEXT%" == "" goto skip_echo
echo %TEXT%>%TEXTFILE%
if exist "%CURDIR%bin\cscnv.exe" echo %TEXT%|"%CURDIR%bin\cscnv.exe" -%CHARSET%>%TEXTFILE%
goto skip_make_text
:skip_echo
if exist %TEXTFILE% goto skip_make_text
echo %MESSAGE%>%TEXTFILE%
if exist "%CURDIR%bin\cscnv.exe" echo %MESSAGE%|"%CURDIR%bin\cscnv.exe" -%CHARSET%>%TEXTFILE%
:skip_make_text
rem ### execute ###
"%CURDIR%\bin\open_jtalk" ^
-x  "%DDIR%" ^
-td "%VDIR%/tree-dur.inf" ^
-tm "%VDIR%/tree-mgc.inf" ^
-tf "%VDIR%/tree-lf0.inf" ^
-tl "%VDIR%/tree-lpf.inf" ^
-md "%VDIR%/dur.pdf" ^
-mm "%VDIR%/mgc.pdf" ^
-mf "%VDIR%/lf0.pdf" ^
-ml "%VDIR%/lpf.pdf" ^
-dm "%VDIR%/mgc.win1" ^
-dm "%VDIR%/mgc.win2" ^
-dm "%VDIR%/mgc.win3" ^
-df "%VDIR%/lf0.win1" ^
-df "%VDIR%/lf0.win2" ^
-df "%VDIR%/lf0.win3" ^
-dl "%VDIR%/lpf.win1" ^
-ow "%OW_VALUE%" ^
-ot "%OT_VALUE%" ^
-s %S_VALUE% ^
-p %P_VALUE% ^
-a %A_VALUE% ^
-g %G_VALUE% ^
-b %B_VALUE% ^
%L_SWITCH% ^
-u %U_VALUE% ^
-em "%VDIR%/tree-gv-mgc.inf" ^
-ef "%VDIR%/tree-gv-lf0.inf" ^
-cm "%VDIR%/gv-mgc.pdf" ^
-cf "%VDIR%/gv-lf0.pdf" ^
-jm %JM_VALUE% ^
-jf %JF_VALUE% ^
-jl %JL_VALUE% ^
-k  "%VDIR%/gv-switch.inf" ^
-z %Z_VALUE% ^
%F_VALUE%
goto end
:error
echo スイッチの指定に誤りがあります。
:end
