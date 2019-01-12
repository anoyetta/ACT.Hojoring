$cd = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $cd

# version
$version = $(Get-Content .\source\@MasterVersion.txt).Trim("\r").Trim("\n")

# build no
$buidno = (Get-Date).ToString("yyMMdd.HHmm")

$tag = ($version + "-" + $buidno)
Write-Output ("-> " + $tag + " merge and tag")

'-> commit'
git commit -a -m ("release " + $tag)

'-> checkout master'
git checkout master

'-> merge develop into master'
git merge develop -m ("Merge branch develop " + $tag) --no-ff

Write-Output ("-> tag " + $tag)
git tag $tag

git checkout develop
'<- done!'
pause
