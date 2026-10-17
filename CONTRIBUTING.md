# Contributing

Thanks for your interest. This is a portfolio project, but issues and pull requests are welcome.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Docker (integration tests start PostgreSQL with Testcontainers; the full stack runs with Docker Compose)

```bash
dotnet tool restore   # dotnet-ef, ReportGenerator, Stryker.NET (pinned in .config/dotnet-tools.json)
```

## Before opening a pull request

Run what CI runs:

```bash
dotnet format BookingSystem.sln --verify-no-changes
dotnet build BookingSystem.sln -c Release -p:TreatWarningsAsErrors=true
dotnet test BookingSystem.sln                 # all categories; Docker must be running
scripts/coverage.sh                           # fails below the per-layer thresholds
scripts/check-vulnerable-packages.sh          # fails on High/Critical advisories
```

`dotnet test --filter "Category!=Integration"` runs the fast suite without Docker. Mutation testing
(`scripts/mutation.sh`) is slow and runs weekly in CI; run it when you change Domain or Application logic.

## Conventions

- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/), e.g. `feat(domain): ...`,
  `fix(api): ...`, `test: ...`, `docs: ...`, `ci: ...`, `build: ...`, `chore: ...`. Keep commits atomic.
- **Style:** `.editorconfig` is the source of truth and `dotnet format` enforces it. Nullable reference types are
  on, and CI treats warnings as errors.
- **Architecture:** dependencies point inward (Api → Infrastructure → Application → Domain); the architecture
  tests fail otherwise. Domain has no project or package references and never reads the clock.
- **Errors:** return `Result`/`Error` for expected failures; throw only for programming errors
  ([ADR 0003](docs/adr/0003-result-pattern.md)).
- **Tests:** every behavior change comes with tests. No system clock, no sleeps, no ordering assumptions;
  build test data with the builders in `tests/BookingSystem.TestUtilities`.
- **Decisions:** a change to an architectural decision needs a new ADR in `docs/adr/`.

## Language

Code, comments, commits and documentation are in English.
