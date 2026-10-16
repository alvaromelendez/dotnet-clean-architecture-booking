#!/usr/bin/env pwsh
# Runs every test project with code coverage, builds an HTML + Cobertura + Markdown report
# with ReportGenerator and fails when a layer is below its line-coverage threshold.
#
# Usage: ./scripts/coverage.ps1
# Requires Docker: the integration tests start PostgreSQL with Testcontainers.
$ErrorActionPreference = 'Stop'

# Minimum line coverage (%). Single source of truth for the thresholds.
$DomainMinLineCoverage = 95
$ApplicationMinLineCoverage = 90
$GlobalMinLineCoverage = 85

$RootDir = Resolve-Path (Join-Path $PSScriptRoot '..')
$ResultsDir = Join-Path $RootDir 'TestResults/coverage'
$ReportDir = Join-Path $RootDir 'coverage-report'

Set-Location $RootDir

docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Error 'Docker is not running. The integration tests need it (Testcontainers).'
    exit 1
}

Remove-Item -Recurse -Force $ResultsDir, $ReportDir -ErrorAction SilentlyContinue

dotnet tool restore | Out-Null
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '==> Running tests with coverage'
dotnet test BookingSystem.sln `
    --configuration Debug `
    --settings coverlet.runsettings `
    --collect 'XPlat Code Coverage' `
    --results-directory $ResultsDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '==> Generating report'
dotnet reportgenerator `
    "-reports:$ResultsDir/**/coverage.cobertura.xml" `
    "-targetdir:$ReportDir" `
    '-reporttypes:Html;Cobertura;MarkdownSummaryGithub' `
    '-verbosity:Warning'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

[xml]$cobertura = Get-Content (Join-Path $ReportDir 'Cobertura.xml')

function Get-PackageLineCoverage([string]$name) {
    $package = $cobertura.coverage.packages.package | Where-Object { $_.name -eq $name }
    if ($null -eq $package) { return $null }
    return [math]::Round([double]::Parse($package.'line-rate', [cultureinfo]::InvariantCulture) * 100, 2)
}

$failed = $false

function Test-Threshold([string]$name, $actual, [double]$minimum) {
    if ($null -eq $actual) {
        Write-Host ('  {0,-30} {1,8}   (min {2}%)  MISSING' -f $name, 'n/a', $minimum)
        $script:failed = $true
    }
    elseif ($actual -ge $minimum) {
        Write-Host ('  {0,-30} {1,7}%   (min {2}%)  OK' -f $name, $actual, $minimum)
    }
    else {
        Write-Host ('  {0,-30} {1,7}%   (min {2}%)  FAIL' -f $name, $actual, $minimum)
        $script:failed = $true
    }
}

$globalLineCoverage = [math]::Round([double]::Parse($cobertura.coverage.'line-rate', [cultureinfo]::InvariantCulture) * 100, 2)

Write-Host ''
Write-Host 'Line coverage by layer'
Test-Threshold 'BookingSystem.Domain' (Get-PackageLineCoverage 'BookingSystem.Domain') $DomainMinLineCoverage
Test-Threshold 'BookingSystem.Application' (Get-PackageLineCoverage 'BookingSystem.Application') $ApplicationMinLineCoverage
Write-Host ('  {0,-30} {1,7}%' -f 'BookingSystem.Infrastructure', (Get-PackageLineCoverage 'BookingSystem.Infrastructure'))
Write-Host ('  {0,-30} {1,7}%' -f 'BookingSystem.Api', (Get-PackageLineCoverage 'BookingSystem.Api'))
Test-Threshold 'Global' $globalLineCoverage $GlobalMinLineCoverage
Write-Host ''
Write-Host "HTML report:      $(Join-Path $ReportDir 'index.html')"
Write-Host "Markdown summary: $(Join-Path $ReportDir 'SummaryGithub.md')"

if ($failed) {
    Write-Error 'Coverage thresholds not met.'
    exit 1
}
