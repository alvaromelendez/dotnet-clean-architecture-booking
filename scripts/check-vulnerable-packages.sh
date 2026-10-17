#!/usr/bin/env bash
# Lists every known-vulnerable NuGet package (direct and transitive) and fails when any of them has
# a severity listed in FAIL_SEVERITIES. Lower severities are reported but do not fail the build.
#
# Usage: scripts/check-vulnerable-packages.sh [solution-or-project]   (default: BookingSystem.sln)
# Requires jq.
set -euo pipefail

# Severities that fail the check. Single source of truth for the policy.
FAIL_SEVERITIES='["High", "Critical"]'

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TARGET="${1:-$ROOT_DIR/BookingSystem.sln}"

if ! command -v jq > /dev/null 2>&1; then
  echo "error: jq is required." >&2
  exit 2
fi

echo "==> Checking $TARGET for vulnerable packages (fails on: $(jq -r 'join(", ")' <<< "$FAIL_SEVERITIES"))"

report="$(dotnet list "$TARGET" package --vulnerable --include-transitive --format json)"

# One line per (project, package, advisory): severity, project, package, version, advisory URL.
findings="$(jq -r '
  .projects[]
  | (.path | split("/") | last) as $project
  | .frameworks[]?
  | ((.topLevelPackages // []) + (.transitivePackages // []))[]
  | . as $package
  | .vulnerabilities[]?
  | [.severity, $project, $package.id, $package.resolvedVersion, .advisoryurl]
  | @tsv' <<< "$report" | sort -u)"

if [ -z "$findings" ]; then
  echo "No vulnerable packages found."
  exit 0
fi

printf '%-10s %-45s %-40s %-12s %s\n' "SEVERITY" "PROJECT" "PACKAGE" "VERSION" "ADVISORY"
while IFS=$'\t' read -r severity project package version advisory; do
  printf '%-10s %-45s %-40s %-12s %s\n' "$severity" "$project" "$package" "$version" "$advisory"
done <<< "$findings"

blocking="$(cut -f1 <<< "$findings" | jq -R --argjson fail "$FAIL_SEVERITIES" 'select(. as $s | $fail | index($s))' | wc -l | tr -d ' ')"

if [ "$blocking" -gt 0 ]; then
  echo "error: $blocking vulnerable package reference(s) with a blocking severity." >&2
  exit 1
fi

echo "Only non-blocking severities found."
