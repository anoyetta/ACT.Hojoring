# 現在のディレクトリを取得する (PS7+ compliant)
$cd = $PSScriptRoot
Set-Location $cd

# 1. PowerShell Version Check
if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Host "==========================================================" -ForegroundColor Red
    Write-Host " [ERROR] This script requires PowerShell 7 (Core) or later." -ForegroundColor Red
    Write-Host " Please install modern PowerShell from Microsoft Store.   " -ForegroundColor Red
    Write-Host "==========================================================" -ForegroundColor Red
    exit 1
}

Start-Transcript make.log | Out-Null

function EndMake() {
    Stop-Transcript | Out-Null
    ''
    Read-Host "終了するには何かキーを押してください..."
    exit
}

# 2. Find MSBuild (Robust detection via vswhere)
$msbuild = $null
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (Test-Path $vswhere) {
    try {
        $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    } catch {
        Write-Warning "Failed to execute vswhere.exe"
    }
}

# Fallback to hardcoded paths if vswhere fails or is missing
if ([string]::IsNullOrEmpty($msbuild) -or !(Test-Path $msbuild)) {
    foreach ($f in (
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\Msbuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Preview\Msbuild\Current\Bin\MSBuild.exe")) {
        if (Test-Path $f) {
            $msbuild = $f
            break
        }
    }
}

if ([string]::IsNullOrEmpty($msbuild) -or !(Test-Path $msbuild)) {
    Write-Error "MSBuild.exe not found. Please install Visual Studio 2022."
    EndMake
}

Write-Output "Using MSBuild: $msbuild"

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

'●Build ACT.Hojoring.DiscordHelper'
dotnet publish .\ACT.Hojoring.DiscordHelper\ACT.Hojoring.DiscordHelper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\ACT.Hojoring\bin\x64\Release\discord | Write-Output
if ($LASTEXITCODE -ne 0) {
    Write-Error "DiscordHelper Publish Failed! Exit Code: $LASTEXITCODE"
    EndMake
}

'●Build ACT.Hojoring Release'
Start-Sleep -m 500

# 3. Build & Error Check
& $msbuild $sln /nologo /v:minimal /p:Configuration=Release /p:Platform=x64 /t:"ACT_Hojoring:Rebuild" | Write-Output
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build Failed! Exit Code: $LASTEXITCODE"
    EndMake
}

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
    $targets = @("yukkuri", "openJTalk", "lib", "tools", "discord")
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
    # Costura.Fodyで埋め込まれたため不要となったサードパーティDLLを削除
    $embedded_dlls = @(
        # FFXIV.Framework 埋め込み
        "Sharlayan.dll",
        "Newtonsoft.Json.dll", "Newtonsoft.Json.Bson.dll",
        "System.Reactive.dll", "System.Reactive.Linq.dll",
        "Microsoft.CodeAnalysis.CSharp.Scripting.dll", "Microsoft.CodeAnalysis.Scripting.dll",
        "System.Runtime.CompilerServices.Unsafe.dll", "System.Memory.dll", "System.Buffers.dll", "System.Numerics.Vectors.dll",
        "System.Collections.Immutable.dll", "System.Threading.Tasks.Extensions.dll", "System.ValueTuple.dll", "System.Reflection.Metadata.dll",
        "System.Text.Encoding.CodePages.dll", "Microsoft.Bcl.AsyncInterfaces.dll",
        "websocket-sharp.dll", "WindowsInput.dll",
        "Grpc.Core.dll", "Grpc.Core.Api.dll", "Grpc.Net.Client.dll", "Grpc.Net.Common.dll",
        "TagLibSharp.dll", "Prism.dll", "Prism.Wpf.dll", "CommonServiceLocator.dll",

        # ACT.SpecialSpellTimer 埋め込み
        "Microsoft.AspNetCore.Http.Abstractions.dll", "Microsoft.AspNetCore.Http.Features.dll",
        "Microsoft.Extensions.Caching.Abstractions.dll", "Microsoft.Extensions.Caching.Memory.dll", "Microsoft.Extensions.Configuration.Abstractions.dll",
        "Microsoft.Extensions.DependencyInjection.dll", "Microsoft.Extensions.DependencyInjection.Abstractions.dll", "Microsoft.Extensions.FileProviders.Abstractions.dll",
        "Microsoft.Extensions.FileProviders.Physical.dll", "Microsoft.Extensions.FileSystemGlobbing.dll", "Microsoft.Extensions.Hosting.Abstractions.dll",
        "Microsoft.Extensions.Logging.Abstractions.dll", "Microsoft.Extensions.Options.dll", "Microsoft.Extensions.Primitives.dll",
        "Microsoft.IO.RecyclableMemoryStream.dll",
        "SixLabors.Fonts.dll", "SixLabors.ImageSharp.dll",
        "NPOI.Core.dll", "NPOI.OOXML.dll", "NPOI.OpenXml4Net.dll", "NPOI.OpenXmlFormats.dll",
        "ICSharpCode.SharpZipLib.dll", "Enums.NET.dll", "BouncyCastle.Cryptography.dll", "MathNet.Numerics.dll",
        "ICSharpCode.AvalonEdit.dll", "Markdig.Signed.dll", "Hjson.dll",

        # ACT.TTSYukkuri 埋め込み
        "RucheHome.Voiceroid.dll", "RucheHomeLib.dll", "VoiceTextWebAPI.Client.dll",

        # ACT.UltraScouter 埋め込み
        "Extended.Wpf.Toolkit.dll", "Xceed.Wpf.Toolkit.dll",
        "FontAwesome.WPF.dll", "NLog.dll",
        "MahApps.Metro.IconPacks.dll", "MahApps.Metro.IconPacks.Core.dll",
        "MahApps.Metro.IconPacks.Material.dll", "MahApps.Metro.IconPacks.MaterialLight.dll",
        "MahApps.Metro.IconPacks.BootstrapIcons.dll", "MahApps.Metro.IconPacks.BoxIcons.dll",
        "MahApps.Metro.IconPacks.Codicons.dll", "MahApps.Metro.IconPacks.Coolicons.dll",
        "MahApps.Metro.IconPacks.Entypo.dll", "MahApps.Metro.IconPacks.EvaIcons.dll",
        "MahApps.Metro.IconPacks.FeatherIcons.dll", "MahApps.Metro.IconPacks.FileIcons.dll",
        "MahApps.Metro.IconPacks.Fontaudio.dll", "MahApps.Metro.IconPacks.FontAwesome.dll",
        "MahApps.Metro.IconPacks.Fontisto.dll", "MahApps.Metro.IconPacks.ForkAwesome.dll",
        "MahApps.Metro.IconPacks.Ionicons.dll", "MahApps.Metro.IconPacks.JamIcons.dll",
        "MahApps.Metro.IconPacks.MaterialDesign.dll", "MahApps.Metro.IconPacks.MaterialLight.dll",
        "MahApps.Metro.IconPacks.Microns.dll", "MahApps.Metro.IconPacks.Modern.dll",
        "MahApps.Metro.IconPacks.Octicons.dll", "MahApps.Metro.IconPacks.PicolIcons.dll",
        "MahApps.Metro.IconPacks.PixelartIcons.dll", "MahApps.Metro.IconPacks.RadixIcons.dll",
        "MahApps.Metro.IconPacks.RemixIcon.dll", "MahApps.Metro.IconPacks.RPGAwesome.dll",
        "MahApps.Metro.IconPacks.SimpleIcons.dll", "MahApps.Metro.IconPacks.Typicons.dll",
        "MahApps.Metro.IconPacks.Unicons.dll", "MahApps.Metro.IconPacks.VaadinIcons.dll",
        "MahApps.Metro.IconPacks.WeatherIcons.dll", "MahApps.Metro.IconPacks.Zondicons.dll"
    )
    foreach ($dll in $embedded_dlls) {
        Remove-Item -Force $dll -ErrorAction SilentlyContinue
        Remove-Item -Force "bin\$dll" -ErrorAction SilentlyContinue
    }

    $garbage = @(
        "*.pdb", "*.xml", "*.exe.config",
        "libgrpc_csharp_ext.*.so", "libgrpc_csharp_ext.*.dylib"
    )
    foreach ($g in $garbage) {
        Remove-Item -Force $g -ErrorAction SilentlyContinue
    }
    Remove-Item -Force -Recurse x86 -ErrorAction SilentlyContinue
    Remove-Item -Force -Recurse x64 -ErrorAction SilentlyContinue

    '●外部リソースを間引く (移動後の bin フォルダ内を対象にする)'
    Remove-Item bin\openJTalk\dic\sys.dic -ErrorAction SilentlyContinue
    Remove-Item bin\openJTalk\voice\* -ErrorAction SilentlyContinue
    Remove-Item bin\yukkuri\aq_dic\aqdic.bin -ErrorAction SilentlyContinue
    # libopus.dll / libsodium.dll はヘルパー側でロードするため残す
    # Remove-Item bin\lib\*.dll -ErrorAction SilentlyContinue
    
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
<#
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
    # PS7 Parallel feature
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
        # $zipPath = Join-Path $archives ($baseName + ".zip")
        $sevenZipPath = Join-Path $archives ($baseName + ".7z")

        # if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        if (Test-Path $sevenZipPath) { Remove-Item $sevenZipPath -Force }

        Push-Location $targetDir
        # & $sevenZipExe a -tzip -y $zipPath "*" | Out-Null
        # if ($LASTEXITCODE -ne 0) { Write-Warning "Failed to zip $baseName" }
        
        & $sevenZipExe a -mx9 -y $sevenZipPath "*" | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Failed to 7z $baseName" }
        
        Pop-Location
        
        Write-Output "  -> Created Component: $baseName.7z"
    }
#>

    '●配布ファイルをアーカイブする (Full Package / 並列実行)'
    $archiveBase = "ACT.Hojoring-" + $versionShort
    # $fullZipPath = Join-Path $archives ($archiveBase + ".zip")
    $full7zPath = Join-Path $archives ($archiveBase + ".7z")

    # if (Test-Path $fullZipPath) { Remove-Item $fullZipPath -Force }
    if (Test-Path $full7zPath) { Remove-Item $full7zPath -Force }

    @(
        @{ Type = "7z"; Args = "-mx9 -r -xr!*.zip -xr!*.7z -xr!*.pdb -xr!archives\" ; Target = $full7zPath }
        # @{ Type = "zip"; Args = "-r -xr!*.zip -xr!*.7z -xr!*.pdb -xr!archives\" ; Target = $fullZipPath }
    ) | ForEach-Object -Parallel {
        $sevenZipExe = $using:7z
        $argsList = $_.Args.Split(" ")
        & $sevenZipExe a $argsList $_.Target * | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning "Failed to archive full package: $($_.Target)" }
        
        Write-Output "  -> Created Full Package: $($_.Target | Split-Path -Leaf)"
    }

    # Remove-Item $workRoot -Recurse -Force
    Set-Location $startdir
}

Write-Output "***"
Write-Output ("*** ACT.Hojoring " + $versionShort + " Done! ***")
Write-Output "***"

EndMake
