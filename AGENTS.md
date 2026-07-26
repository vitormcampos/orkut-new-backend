# Orkut New — Backend

**Stack**: ASP.NET Core Web API, .NET 10.0 (`net10.0`), OpenAPI.

Minimal scaffold generated from `dotnet new webapi`. Only the default `WeatherForecast` endpoint exists.

## Dev commands

```bash
dotnet run          # runs the http profile (port 5272)
dotnet run --launch-profile https   # explicit HTTPS (port 7074)
dotnet watch        # hot-reload dev server
dotnet build        # compile only
```

- No test project, linter, formatter, or CI config exists yet.
- `.env` is gitignored but no `.env` file has been created.
- No Dockerfile or container support yet.

## Ports & endpoints

| Profile | URL                          |
|---------|------------------------------|
| http    | `http://localhost:5272`      |
| https   | `https://localhost:7074`     |

- OpenAPI (Swagger) UI maps to `/openapi/v1.json` in Development environment only.
- The `.http` file at the project root uses `http://localhost:5272` — works with VS Code REST Client.

## Architecture — DDD with Controllers

The project follows **Domain-Driven Design** layered architecture, exposed via traditional ASP.NET Core **Controllers**:

```
Controllers/        # API controllers (API entry point)
Domain/             # entities, value objects, aggregates, domain services, domain events
Application/        # use cases / application services, DTOs, repository interfaces
Infrastructure/     # concrete implementations (EF Core, repositories, external services)
Program.cs          # entrypoint — minimal API host builder
```

- Every new feature starts in **Domain**, then **Application**, then **Infrastructure**, and finally the **Controller**.
- Controllers use `[ApiController]` and `[Route("[controller]")]` — no minimal APIs.
- Use `record` for immutable DTOs and `class` for entities with identity.
- `Nullable` is enabled — annotate correctly with `?` or `required`.

## Development — TDD

All features must be developed using TDD:

1. Write the test (red)
2. Implement minimal code to pass (green)
3. Refactor

### Tests

- Framework: **xUnit** (.NET default).
- Mocking: **NSubstitute**.
- Organization: one test project per layer or a single project mirroring the source structure.
- Naming convention: `[Class]_[Method]_[ExpectedScenario]`.
- Every test must label the 3 parts (Arrange, Act, Assert) with comments.
- Integration tests with EF Core use `Microsoft.Data.Sqlite` (in-memory) or `Testcontainers`.
- Expected command (after creating the test project):

```bash
dotnet test                    # run all tests
dotnet test --filter "Categoria=Unit"   # filter by category
```

### Coverage

- Unit tests for Domain and Application (business rules and use cases).
- Integration tests for Infrastructure (repositories, persistence).
- Controller tests using `WebApplicationFactory` (integration test host).

## Project structure (current — will expand)

```
Controllers/        # API controllers
Properties/         # launchSettings.json
appsettings.json    # shared config
appsettings.Development.json
Program.cs          # entrypoint — minimal API host builder
OrkutNew.csproj     # net10.0, ImplicitUsings, Nullable enable
```

- `ImplicitUsings` is on (`using` for System, Linq, etc. are auto-generated).
- `Nullable` is enabled project-wide.

Only `Microsoft.AspNetCore.OpenApi` (v10.0.10) — no EF Core, auth middleware, or other packages added yet.
