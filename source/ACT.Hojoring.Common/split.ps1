param (
    [string]$inputFile
)

try {
    # 入力ファイルの内容を読み込み
    $content = Get-Content -Path $inputFile

    # フラグの初期化
    $copying = $false
    $outputFile = ""

    # 行ごとに処理
    foreach ($line in $content) {
        if ($line -match "00:0039::戦闘開始！") {
            $copying = $true

            $fileName = $inputFile.Substring(0, $inputFile.Length-4) + "." + $line.Substring(1, 2) + $line.Substring(4, 2) + $line.Substring(7, 2) + ".log"
            $outputFile = $fileName

			Write-Host $outputFile
			Write-Host $line

            if (Test-Path -Path $outputFile) {
                Clear-Content -Path $outputFile
            }
        }
        if ($copying -and -not ($line -match "\[DEBUG\]$")) {
            Add-Content -Path $outputFile -Value $line
        }
        if ($copying -and $line -match "00:0038::Hojoring>WIPEOUT") {
            $copying = $false
            $newFileName = $outputFile.Replace(".log", "_false_.log")
            Rename-Item -Path $outputFile -NewName $newFileName
        }
        if ($copying -and $line -match "ブラックキャットを倒した。") {
            $copying = $false
            $newFileName = $outputFile.Replace(".log", "_true_.log")
            Rename-Item -Path $outputFile -NewName $newFileName
        }
        if ($copying -and $line -match "ハニー・B・ラブリーを倒した。") {
            $copying = $false
            $newFileName = $outputFile.Replace(".log", "_true_.log")
            Rename-Item -Path $outputFile -NewName $newFileName
        }
        if ($copying -and $line -match "ブルートボンバーを倒した。") {
            $copying = $false
            $newFileName = $outputFile.Replace(".log", "_true_.log")
            Rename-Item -Path $outputFile -NewName $newFileName
        }
        if ($copying -and $line -match "ウィケッドサンダーを倒した。") {
            $copying = $false
            $newFileName = $outputFile.Replace(".log", "_true_.log")
            Rename-Item -Path $outputFile -NewName $newFileName
        }
    }
} catch {
    Write-Error "エラーが発生しました: $_"
	Pause
    exit 1
}

Pause
