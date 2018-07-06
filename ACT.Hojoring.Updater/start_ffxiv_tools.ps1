<#
ACTなど外部ツールを自動的に起動させる常駐スクリプト
rev3

# 使い方
タスクスケジューラに登録して使います

### タスクスケジューラの設定
  [全般] - [セキュリティオプション]
   ・ユーザがログオンしているときのみ実行する
   ・最上位の特権で実行する ckecked
   ・表示しない checked (この設定は多分関係ない)

  [トリガー]
   ・ログオン時

  [操作]
   ・プログラム/スクリプト
      powershell.exe

   ・引数の追加
     -WindowStyle Hidden -File "C:\[スクリプトを配置した場所]\start_ffxiv_tools.ps1"

   ・開始
     C:\[スクリプトを配置した場所]
     * ダブルクォートで囲まないように注意すること
#>

# 定数の定義 ===============================================================>
# 起動するツールのリスト
# フルパスで定義すること
$Tools = @(
    "C:[インストールフォルダ]\Advanced Combat Tracker.exe",
    "C:[インストールフォルダ]\FFXIVZoomHack.exe"
)

# FFXIVのプロセスを検出する間隔（秒）
$DetectInterval = 20

# FFXIVのプロセスを検知してからツールを起動するまでのディレイ（秒）
$StartDelay = 60

# ツールの起動後にFFXIVを再度アクティブウィンドウにするまでのディレイ（秒）
$ReactiveFFXIVDelay = 10
# 定数の定義 <===============================================================

Add-Type -AssemblyName Microsoft.VisualBasic

function StartProcess($path) {
    if (Test-Path $path) {
        $dir = Split-Path $path -Parent
        Start-Process $path -WorkingDirectory $dir
    }
}

function ExistsProcess($name) {
    $p = Get-Process | Where-Object {$_.Name -like $name}
    return $p -ne $null
}

while ($true) {
    $isKicked = $false

    Start-Sleep -Seconds $DetectInterval

    if (!(ExistsProcess "ffxiv_dx11")) {
        continue
    }

    Start-Sleep -Seconds $StartDelay

    if (!(ExistsProcess "ffxiv_dx11")) {
        continue
    }

    foreach ($tool in $Tools) {
        $name = Split-Path $tool -Leaf
        $name = $name.Replace(".exe", "")

        # 指定したツールが実行されていない？
        if (!(ExistsProcess $name)) {
            # ツールを起動する
            StartProcess $tool 
            Start-Sleep -Seconds 2

            $isKicked = $true
        }
    }

    if ($isKicked) {
        Start-Sleep -Seconds $ReactiveFFXIVDelay

        # FFXIVをアクティブにする
        $ffxiv = Get-Process | Where-Object {$_.Name -like "ffxiv_dx11"}
        if ($ffxiv -ne $null) {
            [Microsoft.VisualBasic.Interaction]::AppActivate($ffxiv.Id)
        }
    }
}
