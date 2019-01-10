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

# $devenv = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.com"
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
$startdir = Get-Location
$7z = Get-Item .\tools\7za.exe
$sln = Get-Item *.sln
$archives = Get-Item .\archives\
$libz = Get-Item .\tools\libz.exe
$cevioLib = Get-Item FFXIV.Framework\Thirdparty\CeVIO.Talk.RemoteService.dll

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
& $msbuild $sln /nologo /v:minimal /p:Configuration=Release /t:Rebuild | Write-Output
Start-Sleep -m 500

'●Deploy Release'
if (Test-Path .\ACT.Hojoring\bin\Release) {
    Set-Location .\ACT.Hojoring\bin\Release

    '●Hojoring.dll を削除する'
    Remove-Item -Force ACT.Hojoring.dll

    <#
    Copy-Item -Recurse -Force -Path ..\..\..\ACT.SpecialSpellTimer\XIVDBDownloader\bin\Release\* -Destination .\ -Exclude *.pdb
    Remove-Item -Recurse .\tools\XIVDBDownloader\*.config
    Remove-Item -Recurse .\tools\XIVDBDownloader\resources
    #>
    Remove-Item -Recurse * -Include *.pdb

    '●不要なロケールを削除する'
    $locales = @(
        "de",
        "en",
        "es",
        "fr",
        "it",
        "ja",
        "ko",
        "ja",
        "ru",
        "zh-Hans",
        "zh-Hant",
        "hu",
        "pt-BR",
        "ro",
        "sv"
    )

    foreach ($locale in $locales) {
        if (Test-Path $locale) {
            Remove-Item -Force -Recurse $locale
        }
    }

    '●外部参照用DLLを逃がす'
    $references = @(
        "x64",
        "x86",
        "System.Windows.Interactivity.dll",
        "ICSharpCode.SharpZipLib.dll",
        "ICSharpCode.AvalonEdit.dll"
        "CommonServiceLocator.dll",
        "ReactiveProperty*.dll",
        "System.Reactive.*.dll",
        "System.Collections.Immutable.dll",
        "System.Interactive.Async.dll",
        "SuperSocket.ClientEngine.dll",
        "WebSocket4Net.dll",
        "System.Data*.dll"
    )

    New-Item -ItemType Directory "references" | Out-Null
    Move-Item -Path $references -Destination "references" | Out-Null

    '●TTSServer にCeVIOをマージする'
    (& $libz inject-dll -a "FFXIV.Framework.TTS.Server.exe" -i $cevioLib) | Select-String "Injecting"

    '●ACT.Hojoring.Updater をマージする'
    (& $libz inject-dll -a "ACT.Hojoring.Updater.exe" -i "Octokit.dll") | Select-String "Injecting"
    (& $libz inject-dll -a "ACT.Hojoring.Updater.exe" -i "SevenZipSharp.dll" --move) | Select-String "Injecting"

    # ●作業ディレクトリを作る
    New-Item -ItemType Directory "temp" | Out-Null
    
    '●TTSYukkuri のAssemblyをマージする'
    $libs = @(
        #        "DSharpPlus*.dll",
        "Discord.*.dll",
        "RucheHome*.dll"
    )

    Move-Item -Path $libs -Destination "temp" | Out-Null
    (& $libz inject-dll -a "ACT.TTSYukkuri.Core.dll" -i "temp\*.dll" --move) | Select-String "Injecting"

    '●その他のDLLをマージする'
    $libs = @(
        "FFXIV_MemoryReader*.dll",
        "FontAwesome.WPF.dll",
        "System.Web.Razor.dll",
        "Newtonsoft.Json.dll",
        "Octokit.dll",
        "NAudio.dll",
        "Hjson.dll",
        "NLog*.dll",
        "Prism*.dll",
        "Xceed.*.dll",
        "NPOI*.dll",
        "AWSSDK.*.dll"
    )

    Move-Item -Path $libs -Destination "temp" | Out-Null
    (& $libz inject-dll -a "FFXIV.Framework.dll" -i "temp\*.dll" --move) | Select-String "Injecting"

    # ●作業ディレクトリを削除する
    Remove-Item -Force -Recurse "temp"

    '●不要なファイルを削除する'
    Remove-Item -Force System.*.dll
    Remove-Item -Force Microsoft.*.dll
    Remove-Item -Force *.exe.config

    '●フォルダをリネームする'
    Rename-Item Yukkuri _yukkuri
    Rename-Item OpenJTalk _openJTalk
    Rename-Item _yukkuri yukkuri
    Rename-Item _openJTalk openJTalk

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
    & $7z a -r "-xr!*.zip" "-xr!*.7z" "-xr!*.pdb" "-xr!archives\" $archive7z *
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
