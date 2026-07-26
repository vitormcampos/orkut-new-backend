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

## Project structure (current)

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

## Architectural principles

All new projects and features must follow:

- **DDD** (Domain-Driven Design)
- **TDD** (Test-Driven Development)
- **Clean Architecture** — layered with clear separation of concerns
- Low coupling, high cohesion

## Layer structure (Clean Architecture)

```
Controllers/            # API entry point (Controllers, not minimal APIs)
App.Domain/             # core business rules
App.Application/        # use case orchestration
App.IOC/                # dependency injection registration
App.Infrastructure/     # concrete implementations (EF Core, repositories, external services)
Program.cs              # entrypoint — minimal API host builder
```

### `App.Domain`

Core business rules. No external dependencies.

Contains:
- **Models** — entities and value objects
- **Exceptions** — domain-specific exceptions
- **Domain interfaces** — only when strictly domain-related
- Pure business logic

Rules:
- ❌ Must NOT depend on `Application` or `Infrastructure`
- ✅ Must be testable in isolation

### `App.Application`

Orchestrates use cases. Depends only on `Domain`.

Contains:
- **Services** — use case implementations
- **DTOs** — immutable data transfer objects (`record`)
- **Interfaces** — repository contracts, etc.
- **Exceptions** — application-specific

Responsibility:
- Call domain logic
- Apply workflow/flow rules
- Coordinate persistence through interfaces (never directly)

### `App.IOC`

Dependency injection composition root.

Responsibility:
- Register services from all layers
- Configure the DI container

### `App.Infrastructure`

Concrete implementations. Depends on `Application` (interfaces).

Contains:
- EF Core `DbContext` and migrations
- Repository implementations
- External service clients

### Controllers

- Use `[ApiController]` and `[Route("[controller]")]` — no minimal APIs.
- Every new feature reaches the controller **last**, after Domain → Application → Infrastructure.

## Modeling conventions

### Entities (`class`)

- Protect their own state
- Never allow invalid state — validate in the constructor
- Identity matters

### DTOs (`record`)

- No business rules
- Data transport only
- Immutable by default

### Exceptions

- Must be specific to the problem domain
- Avoid generic `Exception` — create custom exception types

## Development — TDD

All features must be developed using TDD:

1. Write the test (red)
2. Run the test (fails)
3. Implement minimal code to pass (green)
4. Run the test (passes)
5. Refactor

### Test projects

One test project per layer:

| Project               | What it tests                         |
|-----------------------|---------------------------------------|
| `App.Domain.Test`     | Entities, value objects, domain rules |
| `App.Application.Test`| Use cases / services with mocked deps |
| `App.Infrastructure.Test` | Repositories via Sqlite/Testcontainers |

### Naming conventions

Test class: `{OriginalClassName}Test`
```
ProductTest
ProductServiceTest
```

Test method: `{Method}_{ExpectedBehavior}`
```
Update_ShouldThrowWhenItemIsNull
Create_ShouldReturnSuccess_WhenDataIsValid
```

### Test structure

Every test must follow the **Arrange / Act / Assert** pattern and label each part with a comment.

```csharp
[Fact]
public void Update_ShouldThrowWhenItemIsNull()
{
    // Arrange
    var service = new ProductService();

    // Act
    var act = () => service.Update(null);

    // Assert
    act.Should().Throw<ArgumentNullException>();
}
```

### Test characteristics

- **Small** — test one thing
- **Isolated** — no shared state between tests
- **No real infrastructure** — mock external dependencies

### Tooling

- Framework: **xUnit**
- Mocking: **NSubstitute**
- Integration EF Core: `Microsoft.Data.Sqlite` (in-memory) or `Testcontainers`

### Commands

```bash
dotnet test                                  # run all tests
dotnet test --filter "Category=Unit"         # filter by category
```

### Coverage pyramid

| Test type           | Layer(s)             | What to test                   |
|---------------------|----------------------|--------------------------------|
| Unit                | Domain, Application  | Business rules, use cases      |
| Integration         | Infrastructure       | Repositories, persistence      |
| Integration (host)  | Controllers          | `WebApplicationFactory`        |

## Project checklist

Before starting a new feature or project:

- [ ] Layer structure created (Domain, Application, IOC, Infrastructure)
- [ ] Test projects created (Domain.Test, Application.Test)
- [ ] IOC configured (dependency injection registration)
- [ ] Naming conventions defined and followed
- [ ] Clear separation between Domain and Application
- [ ] README with architectural description (if public-facing)
