param([string]$targetNetFramework)

$projectName='Microsoft.Identity.Web.AotCompatibility.TestApp'
$rootDirectory = Split-Path $PSScriptRoot -Parent
$publishOutput = dotnet publish $rootDirectory/tests/$projectName/$projectName.csproj --framework $targetNetFramework -nodeReuse:false /p:UseSharedCompilation=false

$actualWarningCount = 0

foreach ($line in $($publishOutput -split "`r`n"))
{
    if (($line -like "*analysis warning IL*") -or ($line -like "*analysis error IL*"))
    {
        Write-Host $line
        $actualWarningCount += 1
    }
}

Write-Host "Actual warning count is: ", $actualWarningCount
$expectedWarningCount = 50

if ($LastExitCode -ne 0)
{
    Write-Host "There was an error while publishing AotCompatibility Test App. LastExitCode is:", $LastExitCode
    Write-Host $publishOutput
}

$runtime = if ($IsWindows) {  "win-x64" } elseif ($IsMacOS) { "macos-x64"} else {"linux-x64"}
$app = if ($IsWindows ) {"./$projectName.exe" } else {"./$projectName" }

Push-Location $rootDirectory/tests/$projectName/bin/Release/$targetNetFramework/$runtime

Write-Host "Executing test App..."
$app
Write-Host "Finished executing test App"

if ($LastExitCode -ne 0)
{
  Write-Host "There was an error while executing AotCompatibility Test App. LastExitCode is:", $LastExitCode
}

Pop-Location

$testPassed = 0
if ($expectedWarningCount -ne $actualWarningCount)
{
    $testPassed = 1
    Write-Host "Actual warning count:", $actualWarningCount, "is not as expected. Expected warning count is:", $expectedWarningCount
}

Exit $testPassed
