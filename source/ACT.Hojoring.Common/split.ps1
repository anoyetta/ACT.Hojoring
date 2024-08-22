param (
    [string]$inputFile
)

try {
    $content = [System.IO.File]::ReadLines($inputFile)

    $copying = $false
    $outputFile = ""
    $outputBuffer = New-Object System.Collections.ArrayList

    $startPattern = [regex]::new("00:0039::戦闘開始！", [System.Text.RegularExpressions.RegexOptions]::Compiled)
    $endPatterns = [regex]::new("00:0038::Hojoring>WIPEOUT|ブラックキャットを倒した。|ハニー・B・ラブリーを倒した。|ブルートボンバーを倒した。|ウィケッドサンダーを倒した。", [System.Text.RegularExpressions.RegexOptions]::Compiled)

    foreach ($line in $content) {
        if ($startPattern.IsMatch($line)) {
            $copying = $true

            $fileName = $inputFile.Substring(0, $inputFile.Length-4) + "." + $line.Substring(1, 2) + $line.Substring(4, 2) + $line.Substring(7, 2) + ".log"
            $outputFile = $fileName

            Write-Host $outputFile
            Write-Host $line

            if (Test-Path -Path $outputFile) {
                Clear-Content -Path $outputFile
            }
        }
        if ($copying) {
            if (-not ($line -like "*[DEBUG]")) {
                $null = $outputBuffer.Add($line)
            }
            if ($endPatterns.IsMatch($line)) {
                $copying = $false
                $suffix = if ($line -match "00:0038::Hojoring>WIPEOUT") { "_false_" } else { "_true_" }
                $newFileName = $outputFile.Replace(".log", $suffix + ".log")
                $outputBuffer | Out-File -FilePath $newFileName -Encoding UTF8
                $outputBuffer.Clear()
            }
        }
    }
} catch {
    Write-Error "エラーが発生しました: $_"
    Pause
    exit 1
}

Pause
