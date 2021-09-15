# 現在のディレクトリを取得する
$cd = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $cd

Start-Transcript make.log | Out-Null

function EndMake() {
    Stop-Transcript | Out-Null
    ''
    Read-Host "終了するには何かキーを教えてください..."
    exit
}

$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
if (Test-Path "C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Current\Bin\MSBuild.exe") {
    $msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Current\Bin\MSBuild.exe"
}

$startdir = Get-Location
$7z = Get-Item .\tools\7za.exe
$sln = Get-Item *.sln
$archives = Get-Item .\archives\

# '●Version'
$versionContent = $(Get-Content "@MasterVersion.txt").Trim("\r").Trim("\n")

# AssemblyInfo.cs 向けのバージョン文字列を生成する
[Collections.Generic.List[String]]$versionParts = $versionContent.Replace("v", "").Split(".")
$versionParts.Insert(2, "0")
$version = [string]::Join(".", $versionParts)
$masterVersionCS = "MasterVersion.cs"
$masterVersionTemp = $masterVersionCS + ".tmp"

# バージョン表記
$versionShort = $versionContent

Write-Output "***"
Write-Output ("*** ACT.Hojoring " + $versionShort + " ***")
Write-Output "***"

# MasterVersion.cs のバージョンを置換する
(Get-Content $masterVersionCS) | ForEach-Object { $_ -replace "#MASTER_VERSION#", $version } | Out-File $masterVersionTemp -Encoding utf8

# MasterVersion.cs.tmp をコピーする
Copy-Item -Force $masterVersionTemp ".\ACT.Hojoring.Common\Version.cs"

if (Test-Path .\ACT.Hojoring\bin\Release) {
    Remove-Item -Path .\ACT.Hojoring\bin\Release\* -Force -Recurse
    Remove-Item -Path .\ACT.Hojoring\bin\Release -Force -Recurse
}

'●Build ACT.Hojoring Release'
Start-Sleep -m 500
& $msbuild $sln /nologo /v:minimal /p:Configuration=Release /t:"ACT_Hojoring:Rebuild" | Write-Output
Start-Sleep -m 500

'●Deploy Release'
if (Test-Path .\ACT.Hojoring\bin\Release) {
    Set-Location .\ACT.Hojoring\bin\Release

    '●不要なロケールを削除する'
    $locales = @(
        "cs",
        "cs-CZ",
        "de",
        "es",
        "fr",
        "hu",
        "it",
        "ja",
        "ja-JP",
        "ko",
        "pl",
        "pt-BR",
        "ro",
        "ru",
        "sv",
        "tr",
        "zh-Hans",
        "zh-Hant"
    )

    foreach ($locale in $locales) {
        if (Test-Path $locale) {
            Remove-Item -Force -Recurse $locale
        }
    }

    '●外部参照用DLLを逃がす'
    if (!(Test-Path "bin")) {
        New-Item -ItemType Directory "bin" | Out-Null
    }

    '●不要なファイルを削除する'
    Remove-Item -Force *.pdb
    Remove-Item -Force *.xml
    Remove-Item -Force *.exe.config
    Remove-Item -Force libgrpc_csharp_ext.*.so
    Remove-Item -Force libgrpc_csharp_ext.*.dylib

    '●フォルダをリネームする'
    Rename-Item Yukkuri _yukkuri
    Rename-Item OpenJTalk _openJTalk
    Rename-Item _yukkuri yukkuri
    Rename-Item _openJTalk openJTalk
    Move-Item yukkuri .\bin\
    Move-Item openJTalk .\bin\
    Move-Item lib .\bin\
    Move-Item tools .\bin\

    '●外部リソースを削除する'
    Remove-Item bin\grpc_csharp_ext.*.dll
    Remove-Item bin\openJTalk\dic\sys.dic
    Remove-Item bin\openJTalk\voice\*
    Remove-Item bin\yukkuri\aq_dic\aqdic.bin
    Remove-Item bin\lib\*.dll
    Remove-Item resources\icon\Common\*.png
    Remove-Item resources\icon\Job\*.png
    Remove-Item resources\icon\Role\*.png
    Remove-Item resources\xivdb\*.csv
    Remove-Item resources\timeline\wallpaper\*
    Remove-Item resources\wav\* -Exclude _asterisk.wav,_beep.wav,_wipeout.wav
    Remove-Item resources\icon\Timeline_EN\*
    Remove-Item resources\icon\Timeline_JP\*

    '●配布ファイルをアーカイブする'
    $archive = "ACT.Hojoring-" + $versionShort
    $archiveZip = $archive + ".zip"
    $archive7z = $archive + ".7z"

    if (Test-Path $archiveZip) {
        Remove-Item $archiveZip -Force
    }

    if (Test-Path $archive7z) {
        Remove-Item $archive7z -Force
    }

    '●to 7z'
    & $7z a -mx9 -r "-xr!*.zip" "-xr!*.7z" "-xr!*.pdb" "-xr!archives\" $archive7z *
    Move-Item $archive7z $archives -Force

    <#
    '●to zip'
    & $7z a -r "-xr!*.zip" "-xr!*.7z" "-xr!*.pdb" "-xr!archives\" $archiveZip *
    Move-Item $archiveZip $archives -Force
    #>

    Set-Location $startdir
}

Write-Output "***"
Write-Output ("*** ACT.Hojoring " + $versionShort + " Done! ***")
Write-Output "***"

EndMake
