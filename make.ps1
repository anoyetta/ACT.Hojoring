Start-Transcript make.log

function EndMake() {
    Stop-Transcript
    Read-Host "終了するには何かキーを教えてください..."
    exit
}

$devenv = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.com"
$startdir = Get-Location
$7z = Get-Item .\tools\7za.exe
$sln = Get-Item *.sln
$archives = Get-Item .\archives\
$libz = Get-Item .\tools\libz.exe

'●Version'
$versionContent = $(Get-Content "@MasterVersion.txt").Trim("\r").Trim("\n")
$version = $versionContent.Replace(".X", ".0")
$versionShort = $versionContent.Replace(".X", "")
$masterVersionCS = "MasterVersion.cs"
$masterVersionTemp = $masterVersionCS + ".tmp"

Write-Output "***"
Write-Output ("*** ACT.Hojoring v" + $versionShort + " ***")
Write-Output "***"

# MasterVersion.cs のバージョンを置換する
(Get-Content $masterVersionCS) | % { $_ -replace "#MASTER_VERSION#", $version } > $masterVersionTemp

# MasterVersion.cs.tmp をコピーする
Copy-Item -Force $masterVersionTemp ".\ACT.Hojoring.Common\Version.cs"

'●Build XIVDBDownloader Debug'
& $devenv $sln /nologo /project ACT.SpecialSpellTimer\XIVDBDownloader\XIVDBDownloader.csproj /Rebuild Debug | Write-Output

'●Build ACT.Hojoring Debug'
& $devenv $sln /nologo /project ACT.Hojoring\ACT.Hojoring.csproj /Rebuild Debug | Write-Output

'●Build XIVDBDownloader Release'
& $devenv $sln /nologo /project ACT.SpecialSpellTimer\XIVDBDownloader\XIVDBDownloader.csproj /Rebuild Release | Write-Output

'●Build ACT.Hojoring Release'
& $devenv $sln /nologo /project ACT.Hojoring\ACT.Hojoring.csproj /Rebuild Release | Write-Output

'●Deploy Release'
if (Test-Path .\ACT.Hojoring\bin\Release) {
    Set-Location .\ACT.Hojoring\bin\Release

    '●Hojoring.dll を削除する'
    Remove-Item -Force ACT.Hojoring.dll

    '●XIVDBDownloader の出力を取得する'
    if (Test-Path .\tools) {
        Remove-Item .\tools -Force -Recurse
    }

    Copy-Item -Recurse -Path ..\..\..\ACT.SpecialSpellTimer\XIVDBDownloader\bin\Release\* -Destination .\ -Exclude *.pdb
    Remove-Item -Recurse .\tools\XIVDBDownloader\resources
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
        "zh-Hant"
    )

    foreach ($locale in $locales) {
        if (Test-Path $locale) {
            Remove-Item -Force -Recurse $locale
        }
    }
    
    '●TTSYukkuri のAssemblyをマージする'
    $dlls = @(
        "DSharpPlus.dll",
        "DSharpPlus.VoiceNext.dll",
        "DSharpPlus.CommandsNext.dll",
        "ReactiveProperty.dll",
        "ReactiveProperty.NET46.dll",
        "RucheHome.Voiceroid.dll",
        "RucheHomeLib.dll",
        "System.Reactive.Core.dll",
        "System.Reactive.Interfaces.dll",
        "System.Reactive.Linq.dll"
        "System.Reactive.PlatformServices.dll"
        "System.Reactive.Windows.Threading.dll"
    )

    foreach ($dll in $dlls) {
        if (Test-Path $dll) {
            & $libz inject-dll --assembly ACT.TTSYukkuri.Core.dll --include $dll --move | Select-String "Injecting"
        }
    }

    '●その他のDLLをマージする'
    $otherLibs = @(
        "FontAwesome.WPF.dll",
        "NAudio.dll",
        "Newtonsoft.Json.dll",
        "NLog.dll",
        "Prism.dll",
        "Prism.Wpf.dll",
        "System.Windows.Interactivity.dll"
    )

    $plugins = @(
        "ACT.SpecialSpellTimer.Core.dll",
        "ACT.TTSYukkuri.Core.dll",
        "ACT.UltraScouter.Core.dll"
        "FFXIV.Framework.TTS.Server.exe"
    )

    foreach ($olib in $otherLibs) {
        foreach ($plugin in $plugins) {
            & $libz inject-dll --assembly $plugin --include $olib | Select-String "Injecting"
      }

      Remove-Item -Force $olib
    }

    '●フォルダをリネームする'
    Rename-Item Yukkuri _yukkuri
    Rename-Item OpenJTalk _openJTalk
    Rename-Item _yukkuri yukkuri
    Rename-Item _openJTalk openJTalk

    '●配布ファイルをアーカイブする'
    $archive = "ACT.Hojoring-v" + $versionShort
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
Write-Output ("*** ACT.Hojoring v" + $versionShort + " Done! ***")
Write-Output "***"

EndMake
