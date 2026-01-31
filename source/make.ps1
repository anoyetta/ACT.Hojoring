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

$msbuild = ""
foreach ($f in (
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\Msbuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Preview\Msbuild\Current\Bin\MSBuild.exe")) {

    if ((Test-Path $f)) {
        $msbuild = $f
        break
    }
}

$startdir = Get-Location
$7z = Get-Item .\tools\7za.exe
$sln = Get-Item *.sln
$archives = Get-Item .\archives\

# '●Version'
$versionContent = $(Get-Content "@MasterVersion.txt").Trim("`r").Trim("`n")

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

if (Test-Path .\ACT.Hojoring\bin\x64\Release) {
    Remove-Item -Path .\ACT.Hojoring\bin\x64\Release\* -Force -Recurse -ErrorAction SilentlyContinue
}

'●Build ACT.Hojoring Release'
Start-Sleep -m 500
& $msbuild $sln /nologo /v:minimal /p:Configuration=Release /p:Platform=x64 /t:"ACT_Hojoring:Rebuild" | Write-Output
Start-Sleep -m 500

'●Deploy Release'
if (Test-Path .\ACT.Hojoring\bin\x64\Release) {
    Set-Location .\ACT.Hojoring\bin\x64\Release

    '●不要なロケールを削除する'
    $locales = @(
        "cs", "cs-CZ", "de", "es", "fr", "hu", "it", "ja", "ja-JP", "ko",
        "pl", "pt-BR", "ro", "ru", "sv", "tr", "zh-Hans", "zh-Hant"
    )

    foreach ($locale in $locales) {
        if (Test-Path $locale) {
            Remove-Item -Force -Recurse $locale
        }
    }

    '●外部参照用DLLを逃がす bin フォルダを作成'
    if (!(Test-Path "bin")) {
        New-Item -ItemType Directory "bin" | Out-Null
    }

    '●フォルダを整理する'
    # フォルダが存在する場合のみ移動を行う
    $targets = @("yukkuri", "openJTalk", "lib", "tools")
    foreach ($t in $targets) {
        if (Test-Path $t) {
            $dest = Join-Path "bin" $t
            # 移動先に同名フォルダがあるとMove-Itemが失敗するため、事前に削除する
            if (Test-Path $dest) {
                Write-Output "  -> Removing existing destination: $dest"
                # 属性をNormalに戻してから削除（属性起因の削除失敗対策）
                Get-ChildItem -Path $dest -Recurse | ForEach-Object { if ($_.Attributes -ne 'Directory') { $_.Attributes = 'Normal' } }
                Remove-Item -Path $dest -Recurse -Force
            }
            
            Write-Output "  -> Moving $t to bin\"
            # 堅牢性を高めるため Copy + Remove 方式
            Copy-Item -Path $t -Destination $dest -Recurse -Force
            if (Test-Path $dest) {
                Remove-Item -Path $t -Recurse -Force
            }
        }
    }

    '●不要なファイルを削除する'
    Remove-Item -Force *.pdb -ErrorAction SilentlyContinue
    Remove-Item -Force *.xml -ErrorAction SilentlyContinue
    Remove-Item -Force *.exe.config -ErrorAction SilentlyContinue
    Remove-Item -Force libgrpc_csharp_ext.*.so -ErrorAction SilentlyContinue
    Remove-Item -Force libgrpc_csharp_ext.*.dylib -ErrorAction SilentlyContinue
    Remove-Item -Force -Recurse x86 -ErrorAction SilentlyContinue
    Remove-Item -Force -Recurse x64 -ErrorAction SilentlyContinue

    '●外部リソースを間引く (移動後の bin フォルダ内を対象にする)'
    Remove-Item bin\openJTalk\dic\sys.dic -ErrorAction SilentlyContinue
    Remove-Item bin\openJTalk\voice\* -ErrorAction SilentlyContinue
    Remove-Item bin\yukkuri\aq_dic\aqdic.bin -ErrorAction SilentlyContinue
    Remove-Item bin\lib\*.dll -ErrorAction SilentlyContinue
    
    '●その他のリソースを間引く'
    Remove-Item resources\icon\Common\*.png -ErrorAction SilentlyContinue
    Remove-Item resources\icon\Job\*.png -ErrorAction SilentlyContinue
    Remove-Item resources\icon\Role\*.png -ErrorAction SilentlyContinue
    Remove-Item resources\xivdb\*.csv -ErrorAction SilentlyContinue
    Remove-Item resources\timeline\wallpaper\* -ErrorAction SilentlyContinue
    Remove-Item resources\wav\* -Exclude _asterisk.wav,_beep.wav,_wipeout.wav -ErrorAction SilentlyContinue
    Remove-Item resources\icon\Timeline_EN\* -ErrorAction SilentlyContinue
    Remove-Item resources\icon\Timeline_JP\* -ErrorAction SilentlyContinue

    # --- 個別アーカイブ作成セクション開始 ---
    $deployDir = Get-Location
    $workRoot = Join-Path $startdir "build_work"
    if (Test-Path $workRoot) { Remove-Item $workRoot -Recurse -Force }
    New-Item $workRoot -ItemType Directory | Out-Null

    # 個別アーカイブの定義リスト
    $componentTasks = @(
        @{ Name = "SPESPE"; Files = @("ACT.SpecialSpellTimer.dll", "ACT.SpecialSpellTimer.RazorModel.dll") },
        @{ Name = "TTSYukkuri"; Files = @("ACT.TTSYukkuri.dll") },
        @{ Name = "UltraScouter"; Files = @("ACT.UltraScouter.dll") },
        @{ Name = "XIVLog"; Files = @("ACT.XIVLog.dll") },
        @{ Name = "Common"; Files = @(
            "ACT.Hojoring.Common.dll", "ACT.Hojoring.Updater.dll", "FFXIV.Framework.dll", 
            "FFXIV.Framework.Bridge.dll", "FFXIV.Framework.Updater.dll", "RazorEngine.dll", 
            "GetActionIcon.ps1", "GetJobGuides.ps1", "split.ps1", "config\*", "resources\*", "bin\*"
        ) }
    )

    '●各コンポーネントの個別アーカイブを作成する (並列実行)'
    $componentTasks | ForEach-Object -Parallel {
        $task = $_
        $deployDir = $using:deployDir
        $workRoot = $using:workRoot
        $archives = $using:archives
        $versionShort = $using:versionShort
        $sevenZipExe = $using:7z

        $targetDir = Join-Path $workRoot $task.Name
        if (!(Test-Path $targetDir)) { New-Item $targetDir -ItemType Directory -Force | Out-Null }
        
        foreach ($item in $task.Files) {
            $srcPath = Join-Path $deployDir $item
            if (Test-Path $srcPath) {
                $destPath = Join-Path $targetDir $item
                $parentDest = Split-Path $destPath
                if ($parentDest -and !(Test-Path $parentDest)) { 
                    New-Item $parentDest -ItemType Directory -Force | Out-Null 
                }
                Copy-Item $srcPath $parentDest -Recurse -Force
            }
        }

        $baseName = "ACT.Hojoring.$($task.Name)-$versionShort"
        $zipPath = Join-Path $archives ($baseName + ".zip")
        $sevenZipPath = Join-Path $archives ($baseName + ".7z")

        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        if (Test-Path $sevenZipPath) { Remove-Item $sevenZipPath -Force }

        Push-Location $targetDir
        & $sevenZipExe a -tzip -y $zipPath "*" | Out-Null
        & $sevenZipExe a -mx9 -y $sevenZipPath "*" | Out-Null
        Pop-Location
        
        Write-Output "  -> Created Component: $baseName.zip / .7z"
    }

    '●配布ファイルをアーカイブする (Full Package / 並列実行)'
    $archiveBase = "ACT.Hojoring-" + $versionShort
    $fullZipPath = Join-Path $archives ($archiveBase + ".zip")
    $full7zPath = Join-Path $archives ($archiveBase + ".7z")

    if (Test-Path $fullZipPath) { Remove-Item $fullZipPath -Force }
    if (Test-Path $full7zPath) { Remove-Item $full7zPath -Force }

    @(
        @{ Type = "7z"; Args = "-mx9 -r -xr!*.zip -xr!*.7z -xr!*.pdb -xr!archives\" ; Target = $full7zPath },
        @{ Type = "zip"; Args = "-r -xr!*.zip -xr!*.7z -xr!*.pdb -xr!archives\" ; Target = $fullZipPath }
    ) | ForEach-Object -Parallel {
        $sevenZipExe = $using:7z
        $argsList = $_.Args.Split(" ")
        & $sevenZipExe a $argsList $_.Target * | Out-Null
        Write-Output "  -> Created Full Package: $($_.Target | Split-Path -Leaf)"
    }

    Remove-Item $workRoot -Recurse -Force
    Set-Location $startdir
}

Write-Output "***"
Write-Output ("*** ACT.Hojoring " + $versionShort + " Done! ***")
Write-Output "***"

EndMake
