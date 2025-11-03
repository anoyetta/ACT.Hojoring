Start-Transcript update.log | Out-Null

# アップデートチャンネル
## プレリリースを取得するか否か？
$isUsePreRelease = $FALSE
# $isUsePreRelease = $TRUE

# API呼び出しをスキップしてデバッグをするか否か？
$isSkipApiCall = $FALSE
# $isSkipApiCall = $TRUE

'***************************************************'
'* Hojoring Updater'
'* '
'* rev16 + File CRC32 Check'
'* (c) anoyetta, 2019-2021'
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
        Get-ChildItem $path -File -Recurse | Sort-Object FullName -Descending | Remove-Item -Force -Confirm:$false;
        Get-ChildItem $path -Directory -Recurse | Sort-Object FullName -Descending | Remove-Item -Force -Confirm:$false;
        Remove-Item $path -Force;
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

function Get-ArchiveFileCRCs {
    param(
        [string]$Path7za,
        [string]$ArchivePath
    )
    
    Write-Host "-> Extracting file CRC32 list from archive metadata (7z l -slt)..."
    
    $crcHashes = @{}
    $currentPath = $null
    
    $output = & $Path7za l -slt $ArchivePath
    $exitCode = $LASTEXITCODE # Exit codeを即座に取得

    if ($exitCode -ne 0) {
        Write-Error "-> ERROR! 7z list command (7z l -slt) failed. Exit code: $exitCode. Cannot extract CRC metadata."
        Write-Error "-> Check if the archive exists and is valid: $ArchivePath"
        return $crcHashes
    }
    
    foreach ($line in $output) {
        # Path = [相対パス]
        if ($line -match "^Path = (.+)$") {
            $currentPath = $Matches[1].Trim().Replace("\", "/")
        }
        # CRC = [CRC32値]
        elseif ($line -match "^CRC = ([0-9A-Fa-f]{8})$") {
            if (-not [string]::IsNullOrEmpty($currentPath)) {
                $crc = $Matches[1].Trim().ToUpper()
                $crcHashes.Add($currentPath, $crc)
                
                Write-Host "   - Added CRC: $crc for file: $currentPath"
                
                $currentPath = $null # 次のファイルへ
            }
        }
    }
    
    Write-Host "-> Found $($crcHashes.Count) files with expected CRC32 values in archive metadata."
    return $crcHashes
}

function Check-ExtractedFilesHash {
    param (
        [string]$Path7za,             # 7za.exeのパス
        [string]$UpdateDirectory,
        [hashtable]$ExpectedCRCs      # 期待されるCRC32ハッシュテーブル
    )

    Write-Host "-> Calculating CRC32 of extracted files using 7z h..."

    if (-not $ExpectedCRCs -is [hashtable] -or $ExpectedCRCs.Count -eq 0) {
        Write-Warning "-> WARNING: Expected file CRC list is empty. Skipping file CRC check."
        return $TRUE
    }
    
    $errorCount = 0
    $originalLocation = Get-Location
    
    $updateDirFullPath = (Resolve-Path $UpdateDirectory).Path
    $updateDirPrefix = $updateDirFullPath.TrimEnd('/', '\').Replace('\', '/') + '/'
    
    try {
        Set-Location $UpdateDirectory 
        
        # 7z h *.* -r を実行して、解凍された全ファイルのCRC32を計算
        $output = & $Path7za h *.* -r
        if ($LASTEXITCODE -ne 0) {
            Write-Error "-> ERROR! 7z hash calculation failed (Exit code: $LASTEXITCODE)."
            return $FALSE
        }
    }
    catch {
        Write-Error "-> FATAL ERROR: Failed to execute 7z h command. $($_.Exception.Message)"
        return $FALSE
    }
    finally {
        Set-Location $originalLocation # 元のディレクトリに必ず戻る
    }

    $regex = '^([0-9A-Fa-f]{8})\s+\S+\s+(.+)$'
    
    foreach ($line in $output) {
        if ($line -match $regex) {
            $calculatedCRC = $Matches[1].Trim().ToUpper()
            $fullPath = $Matches[2].Trim().Replace("\", "/") 
            
            if ($fullPath.StartsWith($updateDirPrefix, [System.StringComparison]::InvariantCultureIgnoreCase)) {
                $relativePath = $fullPath.Substring($updateDirPrefix.Length)
            } else {
                # $updateDirPrefix が含まれていない場合は、この行を無視 (予期せぬパス)
                continue
            }
            
            if ([string]::IsNullOrEmpty($relativePath)) {
                continue
            }

            if ($relativePath.EndsWith('/')) { continue }

            if (-not $ExpectedCRCs.ContainsKey($relativePath)) {
                Write-Warning "-> WARNING: Calculated file '$relativePath' CRC, but no expected CRC was provided. Skipping comparison."
                continue
            }

            $expectedCRC = $ExpectedCRCs[$relativePath].ToUpper()

            if ($calculatedCRC -eq $expectedCRC) {
                # OK
            } else {
                Write-Error "-> ERROR! CRC mismatch detected for file: '$relativePath'."
                Write-Error "-> Expected CRC: $expectedCRC"
                Write-Error "-> Calculated CRC: $calculatedCRC"
                $errorCount++
            }
        }
    }
    
    if ($errorCount -gt 0) {
        Write-Error "-> ERROR! Total $errorCount file CRC mismatches found. Update FAILED."
        return $FALSE
    }

    Write-Host "-> All extracted files passed CRC integrity check."
    return $TRUE
}
## functions <-

# 現在のディレクトリを取得する
$cd = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $cd

# ACT本体と同じディレクトリに配置されている？
$act = Join-Path $cd "Advanced Combat Tracker.exe"
if (Test-Path $act) {
    Write-Error ("-> ERROR! Cannot update. Hojoring installation directory is wrong. Hojoring and ACT are in same directory.")
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

# ACTの終了を待つ
$actPath = $null
for ($i = 0; $i -lt 60; $i++) {
    $isExistsAct = $FALSE
    $processes = Get-Process

    foreach ($p in $processes) {
        if ($p.Name -eq "Advanced Combat Tracker") {
            $isExistsAct = $TRUE
            $actPath = $p.Path
        }
    }

    if ($isExistsAct) {
        if ((Get-Culture).Name -eq "ja-JP") {
            Write-Warning ("-> Advanced Combat Tracker の終了を待機しています。Advanced Combat Tracker を手動で終了してください。")
        }
        else {
            Write-Warning ("-> Please shut down Advanced Combat Tracker.")
        }
    }

    Start-Sleep 5

    if (!($isExistsAct)) {
        break
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

$7zaPath = $null
$7zaNew = Join-Path $cd ".\bin\tools\7z\7za.exe"
$7zaOld = Join-Path $cd ".\tools\7z\7za.exe"

if (Test-Path($7zaNew)) {
    $7zaPath = $7zaNew
}
elseif (Test-Path($7zaOld)) {
    $7zaPath = $7zaOld
}

if ([string]::IsNullOrEmpty($7zaPath)) {
    Write-Error ("-> ERROR! 7za.exe not found! Cannot perform extraction or integrity checks.")
    Exit-Update 1
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
'-> Check Latest Release'

if ($isSkipApiCall) {
    Write-Warning "-> WARNING: API call skipped (Debug Mode). Using hardcoded version information."

    # デバッグモード用情報。実際のアップデートを行う場合はAssetUrlを正しく設定してください。
    $info = New-Object PSObject -Property @{
        Version = "99.99.99"
        Tag = "debug-tag"
        Note = "APIコールをスキップしました。AssetUrlを正しく設定しているか確認してください。"
        # ここに、前回成功したダウンロードURL (7zファイルのURL) を貼り付けてください。
        AssetUrl = "https://example.com/placeholder-hojoring-asset.7z"
        AssetName = "Hojoring-Debug.7z"
        ReleasePageUrl = "https://www.anoyetta.com/"
    }

    if ($info.AssetUrl -eq "https://example.com/placeholder-hojoring-asset.7z") {
        Write-Warning "-> WARNING: AssetUrl is a placeholder. If you want to download a real file, please update the AssetUrl in the script."
    }

} else {
    $updater = Join-Path $cd ".\ACT.Hojoring.Updater.dll"
    if (!(Test-Path $updater)) {
        Write-Error ("-> ERROR! ""ACT.Hojoring.Updater.dll"" not found!")
        Exit-Update 1
    }

    $info = Get-NewerVersion($isUsePreRelease)

    if ([string]::IsNullOrEmpty($info.Version)) {
        Write-Error ("-> No releases found.")
        Exit-Update 0
    }
}

# 新しいバージョンを表示する
''
Write-Host ("Latest Available Version")
Write-Host ("version : " + $info.Version)
Write-Host ("tag     : " + $info.Tag)
Write-Host ($info.Note)
Write-Host ($info.ReleasePageUrl)

do {
    ''
    $in = Read-Host "Are you sure you want to update? [y] or [n]"
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
'-> Download Latest Version'
New-Item $updateDir -ItemType Directory
Start-Sleep -Milliseconds 200
Invoke-WebRequest -Uri $info.AssetUrl -OutFile (Join-Path $updateDir $info.AssetName)
'-> Downloaded!'

$archive = Get-Item ".\update\*.7z"

$expectedCRCHashes = Get-ArchiveFileCRCs -Path7za $7zaPath -ArchivePath $archive.FullName

# ----------------------------------------------------

''
'-> Execute Update.'

Start-Sleep -Milliseconds 10

''
'-> Test Archive Integrity (7-Zip internal CRC check)'
& $7zaPath t $archive.FullName
if ($LASTEXITCODE -ne 0) {
    Write-Error "-> ERROR! 7-Zip internal integrity test FAILED. Exit code: $LASTEXITCODE"
    Write-Error "-> The archive file might be corrupted. Update aborted."
    Exit-Update 1
}
'-> Archive Test OK!'

''
'-> Extract New Assets'
& $7zaPath x $archive.FullName ("-o" + $updateDir)
Start-Sleep -Milliseconds 10
Remove-Item $archive
'-> Extracted!'

if ($expectedCRCHashes -is [hashtable] -and $expectedCRCHashes.Count -gt 0) {
    # 7z h コマンドを利用するため、$7zaPathを渡す
    if (-not (Check-ExtractedFilesHash -Path7za $7zaPath -UpdateDirectory $updateDir -ExpectedCRCs $expectedCRCHashes)) {
        Write-Error "-> ERROR! File integrity check FAILED (CRC mismatch). Update ABORTED."
        Exit-Update 1
    }
} else {
    Write-Warning "-> WARNING: No CRC32 metadata was extracted from archive. Skipping extracted file CRC check."
}

''
'-> Backup Current Version'
if (Test-Path ".\backup") {
    & cmd /c rmdir ".\backup" /s /q
}
Start-Sleep -Milliseconds 10
$temp = (New-TemporaryDirectory).FullName
Copy-Item .\ $temp -Recurse -Force
Move-Item $temp ".\backup" -Force

# Clean ->
Start-Sleep -Milliseconds 10
Remove-Directory ".\references"
if (Test-Path ".\*.dll") {
    Remove-Item ".\*.dll" -Force
}
Remove-Item ".\bin\*" -Recurse -Include *.dll
Remove-Item ".\bin\*" -Recurse -Include *.exe
Remove-Directory ".\openJTalk\"
Remove-Directory ".\yukkuri\"
Remove-Directory ".\tools\"
# Clean <-

# Migration ->
if (Test-Path ".\resources\icon\Timeline EN") {
    if (!(Test-Path ".\resources\icon\Timeline_EN")) {
        $f = Resolve-Path(".\resources\icon\Timeline EN\")
        Rename-Item $f "Timeline_EN"
    } else {
        Copy-Item ".\resources\icon\Timeline EN\*" -Destination ".\resources\icon\Timeline_EN\" -Recurse -Force
        Remove-Directory ".\resources\icon\Timeline EN\"
    }
}

if (Test-Path ".\resources\icon\Timeline JP") {
    if (!(Test-Path ".\resources\icon\Timeline_JP")) {
        $f = Resolve-Path(".\resources\icon\Timeline JP\")
        Rename-Item $f "Timeline_JP"
    } else {
        Copy-Item ".\resources\icon\Timeline JP\*" -Destination ".\resources\icon\Timeline_JP\" -Recurse -Force
        Remove-Directory ".\resources\icon\Timeline JP\"
    }
}
# Migration <-

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
