#!/usr/bin/env bash
# Mutation testing with Stryker.NET over Domain and Application, using only the unit tests.
# Each project has its own config (Stryker mutates one project per run and the break thresholds differ).
# HTML and JSON reports are written to StrykerOutput/<project>/reports/.
#
# Usage: scripts/mutation.sh [domain|application|all]   (default: all)
# Exits non-zero when a project's score is below its "break" threshold.
set -uo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNIT_TESTS_DIR="$ROOT_DIR/tests/BookingSystem.UnitTests"
OUTPUT_DIR="$ROOT_DIR/StrykerOutput"

TARGET="${1:-all}"
case "$TARGET" in
  domain) PROJECTS=(domain) ;;
  application) PROJECTS=(application) ;;
  all) PROJECTS=(domain application) ;;
  *) echo "usage: $0 [domain|application|all]" >&2; exit 2 ;;
esac

dotnet tool restore > /dev/null || exit 1

FAILED=0
SUMMARY=()

for project in "${PROJECTS[@]}"; do
  echo "==> Mutating $project"
  rm -rf "${OUTPUT_DIR:?}/$project"
  log="$(mktemp)"

  # Stryker runs from the unit test project so it only uses those tests; started from the
  # repository root it would pick up the solution and run the integration tests too.
  (cd "$UNIT_TESTS_DIR" && dotnet stryker \
    --config-file "../../stryker-config.$project.json" \
    --output "$OUTPUT_DIR/$project" \
    --skip-version-check) | tee "$log"
  status=${PIPESTATUS[0]}

  score="$(sed -n 's/.*The final mutation score is \([0-9.]*\) %.*/\1/p' "$log" | tail -n 1)"
  rm -f "$log"

  if [ "$status" -ne 0 ]; then
    FAILED=1
    SUMMARY+=("$(printf '  %-12s %7s%%  FAIL (exit code %s)' "$project" "${score:-n/a}" "$status")")
  else
    SUMMARY+=("$(printf '  %-12s %7s%%  OK' "$project" "$score")")
  fi
done

echo
echo "Mutation score"
printf '%s\n' "${SUMMARY[@]}"
echo "Reports: $OUTPUT_DIR/<project>/reports/mutation-report.html"

exit "$FAILED"
