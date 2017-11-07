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

'●Version'
$versionContent = $(Get-Content "@MasterVersion.txt")
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

'●Replace License Key'
$keyfile = ".\ACT.TTSYukkuri\AquesTalk.key"
$codefile = ".\ACT.TTSYukkuri\ACT.TTSYukkuri.Core\Yukkuri\AquesTalk.cs"
$codefileBack = ".\ACT.TTSYukkuri\ACT.TTSYukkuri.Core\Yukkuri\AquesTalk.cs.back"

if (Test-Path $keyfile) {
    $replacement = "#DEVELOPER_KEY_IS_HERE#"

    if (Test-Path $codefile) {
        $key = $(Get-Content $keyfile)
        Copy-Item -Force $codefile $codefileBack
        (Get-Content $codefile) | % { $_ -replace $replacement, $key } > $codefile
    } else {
        '×error:AquesTalk.cs not found.'
        EndMake
    }
} else {
    '×error:License key not found.'
    EndMake
}

'●Build XIVDBDownloader Debug'
& $devenv $sln /nologo /project ACT.SpecialSpellTimer\XIVDBDownloader\XIVDBDownloader.csproj /Rebuild Debug

'●Build ACT.Hojoring Debug'
& $devenv $sln /nologo /project ACT.Hojoring\ACT.Hojoring.csproj /Rebuild Debug

'●Build XIVDBDownloader Release'
& $devenv $sln /nologo /project ACT.SpecialSpellTimer\XIVDBDownloader\XIVDBDownloader.csproj /Rebuild Release

'●Build ACT.Hojoring Release'
& $devenv $sln /nologo /project ACT.Hojoring\ACT.Hojoring.csproj /Rebuild Release

'●Restore Backup'
if (Test-Path $codefileBack) {
    Copy-Item -Force $codefileBack $codefile
}

'●Deploy Release'
if (Test-Path .\ACT.Hojoring\bin\Release) {
  Set-Location .\ACT.Hojoring\bin\Release

  '●XIVDBDownloader の出力を取得する'
  if (Test-Path .\tools) {
    Remove-Item .\tools -Force -Recurse
  }

  Copy-Item -Recurse -Path ..\..\..\ACT.SpecialSpellTimer\XIVDBDownloader\bin\Release\* -Destination .\ -Exclude *.pdb
  Remove-Item -Recurse * -Include *.pdb

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

  '●to zip'
  & $7z a -r "-xr!*.zip" "-xr!*.7z" "-xr!*.pdb" "-xr!archives\" $archiveZip *

  Move-Item $archiveZip $archives -Force
  Move-Item $archive7z $archives -Force

  Set-Location $startdir
}

Write-Output "***"
Write-Output ("*** ACT.Hojoring v" + $versionShort + " Done! ***")
Write-Output "***"

EndMake
