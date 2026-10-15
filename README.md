# BookingSystem

A space reservation management system for booking rooms, sports courts, medical offices and other shared spaces.

## Purpose

This is a portfolio project that demonstrates how to build a maintainable, well-tested .NET application using:

- **Clean Architecture** — strict, inward-only dependency rule between layers.
- **Domain-Driven Design (DDD)** — a rich, framework-agnostic domain model.
- **CQRS** — clear separation between commands (writes) and queries (reads).
- **Testing** — unit, integration and architecture tests.

## Tech Stack

- [.NET 10](https://dotnet.microsoft.com/) / C# (latest)
- ASP.NET Core Web API (controllers)
- xUnit

_The stack will be updated as new phases are completed._

## Solution Structure

```
.
├── BookingSystem.sln
├── Directory.Build.props          # Shared build settings for all projects
├── .editorconfig                  # Code style and naming conventions
├── src
│   ├── BookingSystem.Domain           # Entities, value objects, domain rules (no dependencies)
│   ├── BookingSystem.Application      # Use cases, ports/abstractions (-> Domain)
│   ├── BookingSystem.Infrastructure   # Persistence and external services (-> Application)
│   └── BookingSystem.Api              # HTTP entry point, composition root (-> Application, Infrastructure)
└── tests
    ├── BookingSystem.UnitTests            # Domain and Application tests
    ├── BookingSystem.IntegrationTests     # End-to-end tests through the API
    └── BookingSystem.ArchitectureTests    # Enforces the dependency rule between layers
```

### Dependency Rule

```
Api ──► Infrastructure ──► Application ──► Domain
 └────────────────────────►┘
```

`BookingSystem.Domain` has no project or NuGet package references, keeping the core of the system fully independent of frameworks and infrastructure.

## Getting Started

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
dotnet build
dotnet test
dotnet run --project src/BookingSystem.Api
```

## Status

🚧 **In progress — Phase 0: Setup completed**
