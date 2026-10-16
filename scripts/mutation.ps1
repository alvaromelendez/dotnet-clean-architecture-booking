#!/usr/bin/env pwsh
# Mutation testing with Stryker.NET over Domain and Application, using only the unit tests.
# Each project has its own config (Stryker mutates one project per run and the break thresholds differ).
# HTML and JSON reports are written to StrykerOutput/<project>/reports/.
#
# Usage: ./scripts/mutation.ps1 [-Target domain|application|all]   (default: all)
# Exits non-zero when a project's score is below its "break" threshold.
param(
    [ValidateSet('domain', 'application', 'all')]
    [string]$Target = 'all'
)

$RootDir = Resolve-Path (Join-Path $PSScriptRoot '..')
$UnitTestsDir = Join-Path $RootDir 'tests/BookingSystem.UnitTests'
$OutputDir = Join-Path $RootDir 'StrykerOutput'

$projects = if ($Target -eq 'all') { @('domain', 'application') } else { @($Target) }

dotnet tool restore | Out-Null
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$failed = $false
$summary = @()

foreach ($project in $projects) {
    Write-Host "==> Mutating $project"
    $projectOutput = Join-Path $OutputDir $project
    Remove-Item -Recurse -Force $projectOutput -ErrorAction SilentlyContinue

    # Stryker runs from the unit test project so it only uses those tests; started from the
    # repository root it would pick up the solution and run the integration tests too.
    Push-Location $UnitTestsDir
    try {
        $output = dotnet stryker `
            --config-file "../../stryker-config.$project.json" `
            --output $projectOutput `
            --skip-version-check | Tee-Object -Variable lines
        $status = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    $scoreLine = $lines | Select-String -Pattern 'The final mutation score is ([0-9.]+) %' | Select-Object -Last 1
    $score = if ($scoreLine) { $scoreLine.Matches[0].Groups[1].Value } else { 'n/a' }

    if ($status -ne 0) {
        $failed = $true
        $summary += ('  {0,-12} {1,7}%  FAIL (exit code {2})' -f $project, $score, $status)
    }
    else {
        $summary += ('  {0,-12} {1,7}%  OK' -f $project, $score)
    }
}

Write-Host ''
Write-Host 'Mutation score'
$summary | ForEach-Object { Write-Host $_ }
Write-Host "Reports: $OutputDir/<project>/reports/mutation-report.html"

if ($failed) { exit 1 }
