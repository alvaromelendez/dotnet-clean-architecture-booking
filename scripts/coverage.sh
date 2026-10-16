#!/usr/bin/env bash
# Runs every test project with code coverage, builds an HTML + Cobertura + Markdown report
# with ReportGenerator and fails when a layer is below its line-coverage threshold.
#
# Usage: scripts/coverage.sh
# Requires Docker: the integration tests start PostgreSQL with Testcontainers.
set -euo pipefail

# Minimum line coverage (%). Single source of truth for the thresholds.
DOMAIN_MIN_LINE_COVERAGE=95
APPLICATION_MIN_LINE_COVERAGE=90
GLOBAL_MIN_LINE_COVERAGE=85

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RESULTS_DIR="$ROOT_DIR/TestResults/coverage"
REPORT_DIR="$ROOT_DIR/coverage-report"

cd "$ROOT_DIR"

if ! docker info > /dev/null 2>&1; then
  echo "error: Docker is not running. The integration tests need it (Testcontainers)." >&2
  exit 1
fi

rm -rf "$RESULTS_DIR" "$REPORT_DIR"

dotnet tool restore > /dev/null

echo "==> Running tests with coverage"
dotnet test BookingSystem.sln \
  --configuration Debug \
  --settings coverlet.runsettings \
  --collect "XPlat Code Coverage" \
  --results-directory "$RESULTS_DIR"

echo "==> Generating report"
dotnet reportgenerator \
  "-reports:$RESULTS_DIR/**/coverage.cobertura.xml" \
  "-targetdir:$REPORT_DIR" \
  "-reporttypes:Html;Cobertura;MarkdownSummaryGithub" \
  "-verbosity:Warning"

COBERTURA="$REPORT_DIR/Cobertura.xml"

# Prints the line coverage (%) of a <package> (assembly) in the merged Cobertura report.
package_line_coverage() {
  sed -n "s/.*<package name=\"$1\" line-rate=\"\([0-9.]*\)\".*/\1/p" "$COBERTURA" \
    | awk '{ printf "%.2f", $1 * 100 }'
}

global_line_coverage() {
  sed -n 's/.*<coverage line-rate="\([0-9.]*\)".*/\1/p' "$COBERTURA" \
    | awk '{ printf "%.2f", $1 * 100 }'
}

FAILED=0

check() {
  local name="$1" actual="$2" minimum="$3"
  if [ -z "$actual" ]; then
    printf "  %-30s %8s   (min %s%%)  MISSING\n" "$name" "n/a" "$minimum"
    FAILED=1
  elif awk -v a="$actual" -v m="$minimum" 'BEGIN { exit !(a >= m) }'; then
    printf "  %-30s %7s%%   (min %s%%)  OK\n" "$name" "$actual" "$minimum"
  else
    printf "  %-30s %7s%%   (min %s%%)  FAIL\n" "$name" "$actual" "$minimum"
    FAILED=1
  fi
}

echo
echo "Line coverage by layer"
check "BookingSystem.Domain" "$(package_line_coverage BookingSystem.Domain)" "$DOMAIN_MIN_LINE_COVERAGE"
check "BookingSystem.Application" "$(package_line_coverage BookingSystem.Application)" "$APPLICATION_MIN_LINE_COVERAGE"
printf "  %-30s %7s%%\n" "BookingSystem.Infrastructure" "$(package_line_coverage BookingSystem.Infrastructure)"
printf "  %-30s %7s%%\n" "BookingSystem.Api" "$(package_line_coverage BookingSystem.Api)"
check "Global" "$(global_line_coverage)" "$GLOBAL_MIN_LINE_COVERAGE"
echo
echo "HTML report:      $REPORT_DIR/index.html"
echo "Markdown summary: $REPORT_DIR/SummaryGithub.md"

if [ "$FAILED" -ne 0 ]; then
  echo "error: coverage thresholds not met." >&2
  exit 1
fi
