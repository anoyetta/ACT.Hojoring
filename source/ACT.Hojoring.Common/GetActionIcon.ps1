# 必要な物
# https://www.nuget.org/packages/HtmlAgilityPack/
# https://www.nuget.org/packages/Selenium.WebDriver
# https://www.nuget.org/packages/Selenium.WebDriver.ChromeDriver

# 前提 Install-Packageはなんかうまくいかない場合が多いのでDownload packageで.nugetを持ってきて適当な場所にunzipする

# この3つのパスを自分の環境に合わせてください
# ↓↓↓

# HtmlAgilityPackライブラリをインポート
Add-Type -Path "C:\temp\htmlagilitypack.1.11.71.nupkg\lib\netstandard2.0\HtmlAgilityPack.dll"

# Seleniumライブラリをインポート
Add-Type -Path "C:\temp\selenium.webdriver.4.27.0.nupkg\lib\netstandard2.0\WebDriver.dll"

# ChromeDriverのパスを設定
$chromeDriverPath = "C:\temp\selenium.webdriver.chromedriver.131.0.6778.10800.nupkg\driver\win32\chromedriver.exe"

# ↑↑↑

$jobGuideUrls = @(
"https://na.finalfantasyxiv.com/jobguide/paladin/",
"https://na.finalfantasyxiv.com/jobguide/warrior/",
"https://na.finalfantasyxiv.com/jobguide/darkknight/",
"https://na.finalfantasyxiv.com/jobguide/gunbreaker/",
"https://na.finalfantasyxiv.com/jobguide/whitemage/",
"https://na.finalfantasyxiv.com/jobguide/scholar/",
"https://na.finalfantasyxiv.com/jobguide/astrologian/",
"https://na.finalfantasyxiv.com/jobguide/sage/",
"https://na.finalfantasyxiv.com/jobguide/monk/",
"https://na.finalfantasyxiv.com/jobguide/dragoon/",
"https://na.finalfantasyxiv.com/jobguide/ninja/",
"https://na.finalfantasyxiv.com/jobguide/samurai/",
"https://na.finalfantasyxiv.com/jobguide/reaper/",
"https://na.finalfantasyxiv.com/jobguide/viper/",
"https://na.finalfantasyxiv.com/jobguide/bard/",
"https://na.finalfantasyxiv.com/jobguide/machinist/",
"https://na.finalfantasyxiv.com/jobguide/dancer/",
"https://na.finalfantasyxiv.com/jobguide/blackmage/",
"https://na.finalfantasyxiv.com/jobguide/summoner/",
"https://na.finalfantasyxiv.com/jobguide/redmage/",
"https://na.finalfantasyxiv.com/jobguide/pictomancer/"
)

Function Remove-InvalidFileNameChars {
	param(
	  [Parameter(Mandatory=$true,
		Position=0,
		ValueFromPipeline=$true,
		ValueFromPipelineByPropertyName=$true)]
	  [String]$Name
	)

	$invalidChars = [IO.Path]::GetInvalidFileNameChars() -join ''
	$re = "[{0}]" -f [RegEx]::Escape($invalidChars)
	return ($Name -replace $re)
}


# Chromeオプションを設定
$options = New-Object OpenQA.Selenium.Chrome.ChromeOptions
$options.AddArgument("--headless") # ヘッドレスモードで実行
$options.AddArgument("--ignore-ssl-errors=yes")
$options.AddArgument("--ignore-certificate-errors")

# ChromeDriverを起動
$driver = New-Object OpenQA.Selenium.Chrome.ChromeDriver -ArgumentList $chromeDriverPath, $options

$htmlDoc = [HtmlAgilityPack.HtmlDocument]::new()

$count = 0

foreach ($jobGuideUrl in $jobGuideUrls) {
	# 指定されたURLにアクセス
	$url = "$jobGuideUrl"
	$jobName = $url.Split('/')[-2]
	$count++
	$path = "{0:D2} $jobName" -f $count
	If(!(test-path $path))
	{
		New-Item -ItemType Directory -Force -Path $path
	}

	$driver.Navigate().GoToUrl($url)

	# ページが完全に読み込まれるまで待機
	Start-Sleep -Seconds 2

	# 完全にレンダリングされたHTMLを取得
	$html = $driver.PageSource

	# HTMLドキュメントをロード
	$htmlDoc.LoadHtml($html)

	# trタグを取得
	$trTags = $htmlDoc.DocumentNode.SelectNodes("//tr[contains(@id, 'pve_action') or contains(@id, 'melee_action') or contains(@id, 'tank_action') or contains(@id, 'prange_action') or contains(@id, 'mprange_action')]")

	foreach ($trTag in $trTags) {
	    # imgタグを取得
	    $imgTag = $trTag.SelectSingleNode(".//div[@class='job__skill_icon']//img")
	    if ($imgTag -ne $null) {
	        # PNGファイルのURLを取得
	        $imgUrl = $imgTag.GetAttributeValue("src", "")
	        if ($imgUrl -match "\.png$") {
	            # strongタグの文字列を取得
	            $strongTag = $trTag.SelectSingleNode(".//strong")
	            if ($strongTag -ne $null) {
					$newFileName = $strongTag.InnerText + ".png"
					$repFileName = Remove-InvalidFileNameChars $newFileName
	                $newFilePath = "$path\$repFileName"
		            Invoke-WebRequest -Uri $imgUrl -OutFile $newFilePath
	                Write-Output "ダウンロードの完了: $repFileName"
	            }
	        }
	    }
	}
}

# ブラウザを閉じる
$driver.Quit()

