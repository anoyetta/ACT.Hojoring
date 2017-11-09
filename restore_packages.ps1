$nuget = Get-Item .\tools\nuget.exe
$solutions = Get-ChildItem -Recurse *.sln

foreach ($sln in $solutions) {
    & $nuget restore $sln | Write-Output
}

Read-Host "I—¹‚·‚é‚É‚Í‰½‚©ƒL[‚ğ‹³‚¦‚Ä‚­‚¾‚³‚¢..."
exit
