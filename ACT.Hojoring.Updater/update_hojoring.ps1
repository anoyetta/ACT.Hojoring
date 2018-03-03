Start-Transcript update.log | Out-Null

# プレリリースを取得するか否か？
$isUsePreRelease = $FALSE
# $isUsePreRelease = $TRUE

# 更新の除外リスト
# UPDATEスクリプトは自動更新されないので自分で更新してください
$updateExclude = @(
    "update_hojoring.ps1",
    "_dummy.txt",
    "_sample.txt"
)

'***************************************************'
'* Hojoring Updater'
'* UPDATE-Kun'
'* rev3'
'* (c) anoyetta, 2018'
'***************************************************'
'* Start Update Hojoring'

# 現在のディレクトリを取得する
$cd = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $cd

# プレリリースを使う？
''
Write-Host ("Use Pre-Release :" + $isUsePreRelease)
''

'-> Download Lastest Version'
$updater = Join-Path $cd ".\ACT.Hojoring.Updater.exe"
$updateDir = Join-Path $cd "update"

if (Test-Path $updateDir) {
    Remove-Item ($updateDir + "/*") -Recurse -Force
    Remove-Item $updateDir -Recurse -Force
}

& $updater $updateDir $isUsePreRelease | Write-Output
'-> Downloaded!'

''
'Execute Update.'
$in = Read-Host "Are you sure? [Y] or [N]"
if ($in.ToUpper() -eq "N") {
    exit
}

$7za = Get-Item ".\tools\7z\7za.exe"
$archive = Get-Item ".\update\*.7z"

''
'-> Extract New Assets'
& $7za x $archive ("-o" + $updateDir) | Write-Output
Remove-Item $archive
'-> Extracted!'

''
'-> Update Assets'
$srcs = Get-ChildItem $updateDir -Recurse -Exclude $updateExclude
foreach ($src in $srcs) {
    if ($src.GetType() -ne "DirectoryInfo") {
        $dst = Join-Path .\ $src.FullName.Substring($updateDir.length)
        Copy-Item -Force $src $dst | Write-Output
        Write-Output ("--> " + $dst)
    }
}
'-> Updated'

# 空のディレクトリを削除する
Get-ChildItem -Recurse | Where-Object { $_.GetType().Name -eq "DirectoryInfo" -and $_.GetFiles().Count -eq 0 } | Remove-Item -Recurse

if (Test-Path $updateDir) {
    Remove-Item ($updateDir + "/*") -Recurse -Force
    Remove-Item $updateDir -Recurse -Force
}

''
'* End Update Hojoring'
''
Stop-Transcript | Out-Null
Read-Host "press any key to exit..."
