param (
    [string]$inputFile
)

try {
    $content = Get-Content -Path $inputFile

    $copying = $false
    $outputFile = ""
    $outputBuffer = @()

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
            $outputBuffer += $line
        }
        if ($copying -and ($line -match "00:0038::Hojoring>WIPEOUT" -or $line -match "ブラックキャットを倒した。" -or $line -match "ハニー・B・ラブリーを倒した。" -or $line -match "ブルートボンバーを倒した。" -or $line -match "ウィケッドサンダーを倒した。")) {
            $copying = $false
            $suffix = if ($line -match "00:0038::Hojoring>WIPEOUT") { "_false_" } else { "_true_" }
            $newFileName = $outputFile.Replace(".log", $suffix + ".log")
            $outputBuffer | Out-File -FilePath $newFileName -Encoding UTF8
            $outputBuffer = @()
        }
    }
} catch {
    Write-Error "エラーが発生しました: $_"
    Pause
    exit 1
}

Pause