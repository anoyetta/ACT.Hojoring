
# CI Packaging Script
# Mimics the cleanup and packaging logic of make.ps1

$ErrorActionPreference = "Stop"

$startdir = Get-Location
$binDir = Join-Path $startdir "source\ACT.Hojoring\bin\x64\Release"
$toolsDir = Join-Path $startdir "source\tools"
$7z = Join-Path $toolsDir "7za.exe"
$archivesDir = Join-Path $startdir "archives"

if (-not (Test-Path $binDir)) {
    Write-Error "Build output directory not found: $binDir"
    exit 1
}

# Ensure archives directory exists
if (-not (Test-Path $archivesDir)) {
    New-Item -ItemType Directory -Path $archivesDir | Out-Null
}

Set-Location $binDir

Write-Host "Cleaning up locales..."
$locales = @(
    "cs", "cs-CZ", "de", "es", "fr", "hu", "it", "ja", "ja-JP", "ko",
    "pl", "pt-BR", "ro", "ru", "sv", "tr", "zh-Hans", "zh-Hant"
)

foreach ($locale in $locales) {
    if (Test-Path $locale) {
        Remove-Item -Force -Recurse $locale
    }
}

Write-Host "Creating 'bin' directory and moving dependencies..."
if (-not (Test-Path "bin")) {
    New-Item -ItemType Directory "bin" | Out-Null
}

$targets = @("yukkuri", "openJTalk", "lib", "tools")
foreach ($t in $targets) {
    if (Test-Path $t) {
        $dest = Join-Path "bin" $t
        if (Test-Path $dest) {
            Get-ChildItem -Path $dest -Recurse | ForEach-Object { if ($_.Attributes -ne 'Directory') { $_.Attributes = 'Normal' } }
            Remove-Item -Path $dest -Recurse -Force
        }
        
        Write-Host "  Moving $t to bin\"
        Copy-Item -Path $t -Destination $dest -Recurse -Force
        if (Test-Path $dest) {
            Remove-Item -Path $t -Recurse -Force
        }
    }
}

Write-Host "Removing garbage files..."
$garbage = @(
    "*.pdb", "*.xml", "*.exe.config",
    "libgrpc_csharp_ext.*.so", "libgrpc_csharp_ext.*.dylib"
)
foreach ($g in $garbage) {
    Remove-Item -Force $g -ErrorAction SilentlyContinue
}
Remove-Item -Force -Recurse x86 -ErrorAction SilentlyContinue
Remove-Item -Force -Recurse x64 -ErrorAction SilentlyContinue

Write-Host "Removing specific resources..."
Remove-Item bin\openJTalk\dic\sys.dic -ErrorAction SilentlyContinue
Remove-Item bin\openJTalk\voice\* -ErrorAction SilentlyContinue
Remove-Item bin\yukkuri\aq_dic\aqdic.bin -ErrorAction SilentlyContinue
Remove-Item bin\lib\*.dll -ErrorAction SilentlyContinue

Remove-Item resources\icon\Common\*.png -ErrorAction SilentlyContinue
Remove-Item resources\icon\Job\*.png -ErrorAction SilentlyContinue
Remove-Item resources\icon\Role\*.png -ErrorAction SilentlyContinue
Remove-Item resources\xivdb\*.csv -ErrorAction SilentlyContinue
Remove-Item resources\timeline\wallpaper\* -ErrorAction SilentlyContinue
Remove-Item resources\wav\* -Exclude _asterisk.wav, _beep.wav, _wipeout.wav -ErrorAction SilentlyContinue
Remove-Item resources\icon\Timeline_EN\* -ErrorAction SilentlyContinue
Remove-Item resources\icon\Timeline_JP\* -ErrorAction SilentlyContinue

# Get Version
$versionFile = Join-Path $startdir "source\MasterVersion.txt"
if (Test-Path $versionFile) {
    $versionShort = (Get-Content $versionFile).Trim("`r").Trim("`n")
}
else {
    $versionShort = "0.0.0"
}

Write-Host "Packaging Version: $versionShort"

$archiveBase = "ACT.Hojoring-$versionShort.7z"
$full7zPath = Join-Path $archivesDir $archiveBase

if (Test-Path $full7zPath) { Remove-Item $full7zPath -Force }

Write-Host "Creating 7z archive at $full7zPath..."

# Use 7-zip to compress
# -mx9 = Ultra compression
# -r = Recurse (implied by *)
& $7z a -mx9 -r $full7zPath * | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create 7z archive."
    exit 1
}

Write-Host "Package created successfully: $full7zPath"
Set-Location $startdir
