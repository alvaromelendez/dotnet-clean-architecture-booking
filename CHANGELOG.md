# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-10-16

First release. Summary by development phase.

### Added

- **Phase 0 – Solution setup:** .NET 10 solution with Domain, Application, Infrastructure and Api projects,
  shared build settings (`Directory.Build.props`) and code style (`.editorconfig`).
- **Phase 1 – Domain:** `Room` and `Booking` aggregates, the booking state machine (Pending, Confirmed, Cancelled,
  Completed), `BookingService` reservation rules, `CancellationPolicy` refunds, `TimeSlot` and `Money` value
  objects, strongly typed ids, `Result`/`Error` and domain events. No package references; time is a parameter.
- **Phase 2 – Application:** CQRS use cases for rooms and bookings with MediatR 12, FluentValidation validators,
  logging and validation pipeline behaviors, persistence and clock ports, domain event handlers.
- **Phase 3 – Infrastructure:** EF Core 10 with PostgreSQL (Npgsql, snake_case), repositories and read-side
  queries, a GiST exclusion constraint against overlapping bookings, optimistic concurrency on `xmin`, a unit of
  work that publishes domain events after the commit, and integration tests with Testcontainers.
- **Phase 4 – API:** versioned Minimal APIs (`/api/v1`) for rooms and bookings, RFC 9457 problem details with
  stable error codes, global exception handling, OpenAPI with the Scalar UI (Development), health checks,
  per-IP rate limiting, and HTTP integration tests.
- **Phase 5 – Testing:** architecture tests (ArchUnitNET), test data builders, FsCheck property-based tests,
  coverage thresholds per layer (`scripts/coverage.sh`) and Stryker.NET mutation testing (`scripts/mutation.sh`).
- **Phase 6 – Release:**
  - Multi-stage `Dockerfile` on Ubuntu Chiseled (non-root, port 8080) with a .NET health probe, an EF Core
    migration bundle image, and a `docker-compose.yml` stack (postgres, migrator, api).
  - GitHub Actions: CI (format, warnings as errors, tests, coverage, vulnerable packages, gitleaks), CodeQL,
    weekly mutation testing, Docker image build and GHCR publishing, GitHub Releases; Dependabot.
  - Architecture decision records, architecture diagrams, README, license, security policy, contributing guide,
    issue and pull request templates.

### Fixed

- `GET /api/v1/rooms` now declares its `400` response in the OpenAPI document (Phase 5).
- Pipeline behaviors forward the cancellation token (Phase 2).

[Unreleased]: https://github.com/<OWNER>/<REPO>/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/<OWNER>/<REPO>/releases/tag/v1.0.0
