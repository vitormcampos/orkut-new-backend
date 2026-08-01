# Orkut New — Backend

**Stack**: ASP.NET Core Web API, .NET 10.0 (`net10.0`), OpenAPI, PostgreSQL, EF Core.

## Dev commands

```bash
dotnet run          # runs the http profile (port 5272)
dotnet run --launch-profile https   # explicit HTTPS (port 7074)
dotnet watch        # hot-reload dev server
dotnet build        # compile only
dotnet test         # run all tests
dotnet ef migrations add <Name>
dotnet ef database update
```

## Ports & endpoints

| Profile | URL                          |
|---------|------------------------------|
| http    | `http://localhost:5272`      |
| https   | `https://localhost:7074`     |

- OpenAPI (Swagger) UI maps to `/openapi/v1.json` in Development environment only.

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
App.Infrastructure/     # concrete implementations (EF Core, external services)
Program.cs              # entrypoint
```

### `App.Domain`

Core business rules. No external dependencies.

Contains:
- **Entities** — entities and value objects
- **Exceptions** — domain-specific exceptions
- Pure business logic

Rules:
- ❌ Must NOT depend on `Application` or `Infrastructure`
- ✅ Must be testable in isolation

### `App.Application`

Orchestrates use cases. Depends on `Domain` and `Microsoft.EntityFrameworkCore`.

Contains:
- **Services** — use case implementations
- **DTOs** — immutable data transfer objects (`record`)
- **Interfaces** — application-level contracts (e.g., `IPasswordHasher`)
- **Exceptions** — application-specific

Responsibility:
- Call domain logic
- Apply workflow/flow rules
- Coordinate persistence through `DbContext` (EF Core base class) — directly, not via repository

### `App.IOC`

Dependency injection composition root. References `Infrastructure` and `Application`.

Responsibility:
- Register `AppDbContext` (concrete from Infrastructure) as `DbContext` base type
- Register services from all layers
- Configure the DI container

### `App.Infrastructure`

Concrete implementations. Depends on `Application` (interfaces) and EF Core provider.

Contains:
- EF Core `DbContext` (`AppDbContext` — extends `Microsoft.EntityFrameworkCore.DbContext`)
- Entity type configurations (Fluent API mappings)
- Migrations
- External service clients (e.g., `BCryptPasswordHasher`)

### Controllers

- Use `[ApiController]` and `[Route("[controller]")]` — no minimal APIs.
- Every new feature reaches the controller **last**, after Domain → Application → Infrastructure.

## Persistence pattern: EF Core direct, no Repository

We do **not** use the Repository pattern. Instead, services inject `DbContext` directly and use `Set<T>()` to access the database.

### Why

- EF Core `DbSet<T>` **is** a repository — `Add()`, `Remove()`, `FindAsync()`, `Where()`
- EF Core `SaveChangesAsync()` **is** Unit of Work
- Wrapping 1:1 calls in repository classes adds indirection without value
- The [eShop reference architecture](https://github.com/dotnet/eshop) from Microsoft uses `DbContext` directly

### How

```csharp
// App.Application/Services/UserService.cs
public class UserService : IUserService
{
    private readonly DbContext _context; // base class from EF Core
    private readonly IPasswordHasher _passwordHasher;

    public UserService(DbContext context, IPasswordHasher passwordHasher) { ... }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await _context.Set<User>().FindAsync([id], ct);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var user = new User(request.Name, request.Email, passwordHash);
        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync(ct);
        return MapToDto(user);
    }
}
```

The IOC layer registers `AppDbContext` (from `App.Infrastructure`) so the DI container resolves the concrete type as `DbContext`.

### Dependency graph

```
App.Domain
    ↑
App.Application ──→ Microsoft.EntityFrameworkCore (DbSet<T>, DbContext)
    ↑
App.Infrastructure ──→ Npgsql, BCrypt, App.Application (interfaces only)
    ↑
App.IOC ──→ App.Infrastructure, App.Application
    ↑
OrkutNew (Web API)
```

- `App.Application` depends on `DbContext` (EF Core abstraction), NOT on `App.Infrastructure`
- No circular dependency — `Application` never references `Infrastructure`

## Modeling conventions

### Entities (`class`)

- Protect their own state
- Never allow invalid state — validate in the constructor
- Identity matters
- Private parameterless constructor for EF Core

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

| Project               | What it tests                         | Strategy |
|-----------------------|---------------------------------------|----------|
| `App.Domain.Test`     | Entities, value objects, domain rules | Unit tests (pure) |
| `App.Application.Test`| Use cases / services                  | EF Core InMemory database (`UseInMemoryDatabase`) |
| `App.Infrastructure.Test` | DbContext, migrations, configurations | SQLite / Testcontainers |

### Why InMemory for Application tests

Since we use `DbContext` directly (no repository mocks), tests use a real EF Core context backed by InMemory database. This exercises the full persistence path — tracking, `SaveChanges`, queries — without external dependencies.

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
- **Isolated** — no shared state between tests (unique DB name per test class)
- **No external infrastructure** — InMemory database for application tests, mock external services only (e.g., `IPasswordHasher`)

### Tooling

- Framework: **xUnit**
- Mocking: **NSubstitute** (for external interfaces only — `IPasswordHasher`, etc.)
- Application tests: `Microsoft.EntityFrameworkCore.InMemory`
- Data faking: **Bogus**

### Commands

```bash
dotnet test                                  # run all tests
dotnet test --filter "Category=Unit"         # filter by category
```

### Coverage pyramid

| Test type           | Layer(s)             | What to test                   |
|---------------------|----------------------|--------------------------------|
| Unit                | Domain               | Business rules, entities       |
| Integration (InMemory) | Application       | Use cases, service orchestration |
| Integration         | Infrastructure       | DbContext, persistence         |
| Integration (host)  | Controllers          | `WebApplicationFactory`        |

## Project checklist

Before starting a new feature or project:

- [ ] Layer structure created (Domain, Application, IOC, Infrastructure)
- [ ] Test projects created (Domain.Test, Application.Test)
- [ ] IOC configured (dependency injection registration)
- [ ] Naming conventions defined and followed
- [ ] Clear separation between Domain and Application
- [ ] `AppDbContext` registered as `DbContext` base type in IOC
