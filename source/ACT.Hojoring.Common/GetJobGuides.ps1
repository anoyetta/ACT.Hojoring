# HtmlAgilityPackライブラリをインポート
Add-Type -Path "C:\temp\htmlagilitypack.1.11.71.nupkg\lib\netstandard2.0\HtmlAgilityPack.dll"

# Seleniumライブラリをインポート
Add-Type -Path "C:\temp\selenium.webdriver.4.27.0.nupkg\lib\netstandard2.0\WebDriver.dll"

# ChromeDriverのパスを設定
$chromeDriverPath = "C:\temp\selenium.webdriver.chromedriver.131.0.6778.10800.nupkg\driver\win32\chromedriver.exe"

# Chromeオプションを設定
$options = New-Object OpenQA.Selenium.Chrome.ChromeOptions
$options.AddArgument("--headless") # ヘッドレスモードで実行
$options.AddArgument("--ignore-ssl-errors=yes")
$options.AddArgument("--ignore-certificate-errors")

# ChromeDriverを起動
$driver = New-Object OpenQA.Selenium.Chrome.ChromeDriver -ArgumentList $chromeDriverPath, $options

# 指定されたURLにアクセス
$url = "https://jp.finalfantasyxiv.com/jobguide/battle/"
$driver.Navigate().GoToUrl($url)

# ページが完全に読み込まれるまで待機
Start-Sleep -Seconds 5

# 完全にレンダリングされたHTMLを取得
$html = $driver.PageSource

# HTMLドキュメントをロード
$htmlDoc = [HtmlAgilityPack.HtmlDocument]::new()
$htmlDoc.LoadHtml($html)

# ジョブガイドのURLを格納するリストを作成
$jobGuideUrls = @()

# aタグを取得
$aTags = $htmlDoc.DocumentNode.SelectNodes("//a[contains(@href, '/jobguide/')]")

foreach ($aTag in $aTags) {
    $href = $aTag.GetAttributeValue("href", "")
    if ($href -match "^/jobguide/[^/]+/$") {
        $fullUrl = "https://jp.finalfantasyxiv.com$href"
        $jobGuideUrls += $fullUrl
    }
}

# 結果を出力
$jobGuideUrls | ForEach-Object { Write-Output $_ }
