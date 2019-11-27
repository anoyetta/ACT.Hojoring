Start-Transcript update.log | Out-Null

# アップデートチャンネル
## プレリリースを取得するか否か？
$isUsePreRelease = $FALSE
# $isUsePreRelease = $TRUE

'***************************************************'
'* Hojoring Updater'
'* UPDATE-Kun'
'* rev14'
'* (c) anoyetta, 2019'
'***************************************************'
'* Start Update Hojoring'

## functions ->
function New-TemporaryDirectory {
    $tempDirectoryBase = [System.IO.Path]::GetTempPath();
    $newTempDirPath = [String]::Empty;

    do {
        [string] $name = [System.Guid]::NewGuid();
        $newTempDirPath = (Join-Path $tempDirectoryBase $name);
    } while (Test-Path $newTempDirPath);

    New-Item -ItemType Directory -Path $newTempDirPath;
    return $newTempDirPath;
}

function Remove-Directory (
    [string] $path) {
    if (Test-Path $path) {
        Remove-Item ($path + "\*") -Recurse -Force
        Remove-Item $path -Recurse -Force
    }
}

function Exit-Update (
    [int] $exitCode) {
    $targets = @(
        ".\update"
    )

    foreach ($d in $targets) {
        Remove-Directory $d
    }

    ''
    '* End Update Hojoring'
    ''
    Stop-Transcript | Out-Null
    Read-Host "press any key to exit..."

    exit $exitCode
}

function Get-NewerVersion(
    [bool] $usePreRelease) {
    $cd = Convert-Path .
    $dll = Join-Path $cd "ACT.Hojoring.Updater.dll"

    $bytes = [System.IO.File]::ReadAllBytes($dll)
    [System.Reflection.Assembly]::Load($bytes)

    $checker = New-Object ACT.Hojoring.UpdateChecker
    $checker.UsePreRelease = $usePreRelease
    $info = $checker.GetNewerVersion()

    return $info
}
## functions <-

# 現在のディレクトリを取得する
$cd = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $cd

# ACT本体と同じディレクトリに配置されている？
$act = Join-Path $cd "Advanced Combat Tracker.exe"
if (Test-Path $act) {
    Write-Error ("-> ERROR! Cannot update. Hojoring instration directory is wrong. Hojoring and ACT are in same directory.")
    Write-Error ("-> You can re-setup following this site.")
    Write-Error ("-> https://www.anoyetta.com/entry/hojoring-setup")
    Exit-Update 1
}

# 更新の除外リストを読み込む
$ignoreFile = ".\config\update_hojoring_ignores.txt"
$updateExclude = @()
if (Test-Path $ignoreFile) {
    $updateExclude = (Get-Content $ignoreFile -Encoding UTF8) -as [string[]]
}

# 引数があればアップデートチャンネルを書き換える
if ($args.Length -gt 0) {
    $b = $FALSE
    if ([bool]::TryParse($args[0], [ref]$b)) {
        $isUsePreRelease = $b
    }
}

# Processを殺す
$actPath = $null
$processes = Get-Process
foreach ($p in $processes) {
    if ($p.Name -eq "Advanced Combat Tracker") {
        $actPath = $p.Path
        if ($p.CloseMainWindow()) {
            $p.WaitForExit(15 * 1000) | Out-Null
        } else {
            $p.Kill()
        }
    }

    if ($p.Name -eq "FFXIV.Framework.TTS.Server") {
        if ($p.CloseMainWindow()) {
            $p.WaitForExit(15 * 1000) | Out-Null
        } else {
            $p.Kill()
        }
    }
}

Start-Sleep -Milliseconds 200
$processes = Get-Process
foreach ($p in $processes) {
    if ($p.Name -eq "Advanced Combat Tracker") {
        Write-Error ("-> ERROR! ACT is still running.")
        Exit-Update 1
    }

    if ($p.Name -eq "FFXIV.Framework.TTS.Server") {
        Write-Error ("-> ERROR! TTSServer is still running.")
        Exit-Update 1
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

$updater = Join-Path $cd ".\ACT.Hojoring.Updater.dll"
if (!(Test-Path $updater)) {
    Write-Error ("-> ERROR! ""ACT.Hojoring.Updater.dll"" not found!")
    Exit-Update 1
}

''
'-> Check Lastest Release'
$info = Get-NewerVersion($isUsePreRelease)

if ([string]::IsNullOrEmpty($info.Version)) {
    Write-Error ("-> Not found any releases.")
    Exit-Update 0
}

# 新しいバージョンを表示する
''
Write-Host ("Available Lastest Version")
Write-Host ("version : " + $info.Version)
Write-Host ("tag     : " + $info.Tag)
Write-Host ($info.Note)
Write-Host ($info.ReleasePageUrl)

do {
    ''
    $in = Read-Host "Are you sure to update? [y] or [n]"
    $in = $in.ToUpper()

    if ($in -eq "Y") {
        break;
    }

    if ($in -eq "N") {
        'Update canceled.'
        Exit-Update 0
    }
} while ($TRUE)

$updateDir = Join-Path $cd "update"
if (Test-Path $updateDir) {
    Remove-Directory $updateDir
}

''
'-> Backup Current Version'
if (Test-Path ".\backup") {
	Remove-Directory ".\backup"
}
Start-Sleep -Milliseconds 10
$temp = (New-TemporaryDirectory).FullName
Copy-Item .\ $temp -Recurse -Force
Move-Item $temp ".\backup" -Force

''
'-> Download Lastest Version'
New-Item $updateDir -ItemType Directory
Start-Sleep -Milliseconds 200
Invoke-WebRequest -Uri $info.AssetUrl -OutFile (Join-Path $updateDir $info.AssetName)
'-> Downloaded!'

''
'-> Execute Update.'

Start-Sleep -Milliseconds 10

$7zaOld = ".\tools\7z\7za.exe"
$7zaNew = ".\bin\tools\7z\7za.exe"

$7za = $null
if (Test-Path($7zaNew)) {
    $7za = Get-Item $7zaNew
}
else {
    if (Test-Path($7zaOld)) {
        $7za = Get-Item $7zaOld
    }
}

$archive = Get-Item ".\update\*.7z"

''
'-> Extract New Assets'
& $7za x $archive ("-o" + $updateDir)
Start-Sleep -Milliseconds 10
Remove-Item $archive
'-> Extracted!'

# Clean ->
Start-Sleep -Milliseconds 10
Remove-Directory ".\references"
if (Test-Path ".\*.dll") {
    Remove-Item ".\*.dll" -Force
}
Remove-Item ".\bin\*" -Recurse -Include *.dll
Remove-Item ".\bin\*" -Recurse -Include *.exe
Remove-Directory ".\openJTalk"
Remove-Directory ".\yukkuri"
Remove-Directory ".\tools"
# Clean <-

''
'-> Update Assets'
Start-Sleep -Milliseconds 10
$srcs = Get-ChildItem $updateDir -Recurse -Exclude $updateExclude
foreach ($src in $srcs) {
    if ($src.GetType().Name -ne "DirectoryInfo") {
        $dst = Join-Path .\ $src.FullName.Substring($updateDir.length)
        New-Item (Split-Path $dst -Parent) -ItemType Directory -Force | Out-Null
        Copy-Item -Force $src $dst | Write-Output
        Write-Output ("--> " + $dst)
        Start-Sleep -Milliseconds 1
    }
}
'-> Updated'

# Release Notesを開く
Start-Sleep -Milliseconds 500
Start-Process $info.ReleasePageUrl
Start-Sleep -Milliseconds 500

# Update開始時にACTを起動していた場合、ACTを開始する
if (![string]::IsNullOrEmpty($actPath)) {
    Start-Process $actPath -Verb runas
}

Exit-Update 0
