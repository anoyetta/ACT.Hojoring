Start-Transcript make.log

function EndMake() {
  Stop-Transcript
  Read-Host "終了するには何かキーを教えてください..."
  exit
}

$devenv="C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.com"
$startdir=Get-Location

$sln=Get-Item *.sln

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

    Copy-Item -Recurse -Path ..\..\..\ACT.SpecialSpellTimer\XIVDBDownloader\bin\Release\* -Destination .\

    '●配布ファイルをアーカイブする'
    if (Test-Path ACT.Hojoring.zip) {
        Remove-Item ACT.Hojoring.zip -Force
    }

    $files = Get-ChildItem -Path .\ -Exclude *.zip
    Compress-Archive -CompressionLevel Optimal -Path $files -DestinationPath ACT.Hojoring.zip
    Set-Location $startdir
}

EndMake
