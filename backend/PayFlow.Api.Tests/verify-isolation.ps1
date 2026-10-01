param([int]$Seed = 20261001, [string]$Filter = 'FullyQualifiedName~PayFlow.Api.Tests.Unit')
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'PayFlow.Api.Tests.csproj'
$results = Join-Path $PSScriptRoot 'TestResults/isolation'
$previousSeed = $env:TEST_ORDER_SEED
$env:TEST_ORDER_SEED = $Seed.ToString()
try {
    dotnet test $project -c Release --no-restore --filter $Filter --results-directory $results --logger 'trx;LogFileName=discovery.trx'
    if ($LASTEXITCODE -ne 0) { throw 'Selected suite failed.' }
    [xml]$report = Get-Content -LiteralPath (Join-Path $results 'discovery.trx')
    $names = @($report.TestRun.Results.UnitTestResult | ForEach-Object { $_.testName } | Sort-Object -Unique)
    if ($names.Count -eq 0) { throw 'No test cases matched the selected filter.' }
    $random = [Random]::new($Seed)
    $names = @($names | Sort-Object { $random.Next() })
    $index = 0
    foreach ($name in $names) {
        $index++
        $escaped = [regex]::Replace($name, '[\\()&|=!~]', '\$0')
        # A runsettings file avoids native-shell quoting of string theory arguments.
        $settingsPath = Join-Path $results 'individual.runsettings'
        $xmlFilter = [Security.SecurityElement]::Escape("DisplayName=$escaped")
        [IO.File]::WriteAllText($settingsPath, "<RunSettings><RunConfiguration><TestCaseFilter>$xmlFilter</TestCaseFilter></RunConfiguration></RunSettings>")
        $ErrorActionPreference = 'Continue'
        $output = dotnet test $project -c Release --no-build --no-restore --settings $settingsPath --results-directory $results --logger 'trx;LogFileName=individual.trx' --verbosity quiet 2>&1
        $resultCode = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
        if ($resultCode -ne 0) { throw "Isolated test failed: $name`n$output" }
        [xml]$individual = Get-Content -LiteralPath (Join-Path $results 'individual.trx')
        if ([int]$individual.TestRun.ResultSummary.Counters.executed -ne 1) { throw "Expected exactly one isolated case: $name`n$output" }
        Write-Output "[$index/$($names.Count)] Passed: $name"
    }
    Write-Output "All $($names.Count) selected test cases passed in isolated processes, including individual theory rows. Seed: $Seed."
} finally {
    $env:TEST_ORDER_SEED = $previousSeed
}
