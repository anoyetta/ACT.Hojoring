$nuget = Get-Item .\tools\nuget.exe
$solutions = Get-ChildItem -Recurse *.sln

foreach ($sln in $solutions) {
    & $nuget restore $sln | Write-Output
}

Read-Host "終了するには何かキーを教えてください..."
exit
