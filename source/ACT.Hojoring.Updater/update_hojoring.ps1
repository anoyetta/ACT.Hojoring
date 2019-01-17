Start-Transcript update.log | Out-Null

# アップデートチャンネル
## プレリリースを取得するか否か？
$isUsePreRelease = $FALSE
# $isUsePreRelease = $TRUE

'***************************************************'
'* Hojoring Updater'
'* UPDATE-Kun'
'* rev7'
'* (c) anoyetta, 2018'
'***************************************************'
'* Start Update Hojoring'

# 現在のディレクトリを取得する
$cd = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $cd

# 更新の除外リストを読み込む
$updateExclude = @()
if (Test-Path ".\update_hojoring_ignores.txt") {
    $updateExclude = (Get-Content ".\update_hojoring_ignores.txt" -Encoding UTF8) -as [string[]]
}

# 引数があればアップデートチャンネルを書き換える
if ($args.Length -gt 0) {
    $b = $FALSE
    if ([bool]::TryParse($args[0], [ref]$b)) {
        $isUsePreRelease = $b
    }
}

# Processを殺す
$processes = Get-Process
foreach ($p in $processes) {
    if ($p.Name -eq "Advanced Combat Tracker") {
        Stop-Process -InputObject $p
        Start-Sleep -s 1
    }

    if ($p.Name -eq "FFXIV.Framework.TTS.Server") {
        Stop-Process -InputObject $p
        Start-Sleep -s 1
    }
}

# プレリリースを使う？
''
if ($isUsePreRelease) {
    Write-Host ("-> Update Channel: *Pre-Release")
}
else {
    Write-Host ("-> Update Channel: Release")
}
''

'-> Download Lastest Version'
$updater = Join-Path $cd ".\ACT.Hojoring.Updater.exe"
if (!(Test-Path $updater)) {
    Write-Error ("-> ERROR! ""ACT.Hojoring.Updater.exe"" not found!")
    Stop-Transcript | Out-Null
    Read-Host "press any key to exit..."
    exit 1
}

$updateDir = Join-Path $cd "update"

if (Test-Path $updateDir) {
    Remove-Item ($updateDir + "/*") -Recurse -Force
    Remove-Item $updateDir -Recurse -Force
}

& $updater $updateDir $isUsePreRelease
'-> Downloaded!'

''
'-> Execute Update.'
do {
    $in = Read-Host "Are you sure? [Y] or [n]"
    $in = $in.ToUpper()

    if ($in -eq "Y") {
        break;
    }

    if ($in -eq "N") {
        exit
    }
} while ($TRUE)

$7za = Get-Item ".\tools\7z\7za.exe"
$archive = Get-Item ".\update\*.7z"

''
'-> Extract New Assets'
& $7za x $archive ("-o" + $updateDir)
Remove-Item $archive
'-> Extracted!'

# Clean ->
if (Test-Path ".\references") {
    Get-ChildItem -Path ".\references" -Recurse | Remove-Item -Force -Recurse
    Remove-Item -Recurse -Force ".\references"
}

Get-ChildItem -Path ".\*.dll" | Remove-Item -Force
# Clean <-

''
'-> Update Assets'
$srcs = Get-ChildItem $updateDir -Recurse -Exclude $updateExclude
foreach ($src in $srcs) {
    if ($src.GetType().Name -ne "DirectoryInfo") {
        $dst = Join-Path .\ $src.FullName.Substring($updateDir.length)
        New-Item (Split-Path $dst -Parent) -ItemType Directory -Force | Out-Null
        Copy-Item -Force $src $dst | Write-Output
        Write-Output ("--> " + $dst)
    }
}
'-> Updated'

if (Test-Path $updateDir) {
    Remove-Item ($updateDir + "/*") -Recurse -Force
    Remove-Item $updateDir -Recurse -Force
}

''
'* End Update Hojoring'
''
Stop-Transcript | Out-Null
Read-Host "press any key to exit..."
