# Claude Instructions for TemplateMs

This file is read by Claude (and GitHub Copilot agents) at the start of every session in this repository. Follow every rule here without exception.

---

## 1. What this codebase is

**TemplateMs** is a production-grade .NET 10 microservice template that combines:
- **Clean Architecture** — strict ring-to-ring dependency rules enforced by architecture tests.
- **Vertical Slice Architecture** — all code for a feature lives together inside `Features/{Feature}/`.
- **Domain-Driven Design** — aggregates, domain events, repositories, bounded context.
- **WolverineFX** — convention-based handler discovery + middleware pipeline. **Not MediatR.**
- **Dapper** — explicit SQL via constant strings in `TemplateMs.Sql`. **Not Entity Framework.**
- **ActiveMQ** (Apache NMS) — integration event bus. **Not MassTransit.**
- **Scalar** — API documentation. **Not Swagger / Swashbuckle.**

---

## 2. Hard rules — never violate these

### 2.1 Dependency directions

```
Domain        ->  (nothing)
Application   ->  Domain only
Infrastructure -> Application + Domain + Sql
Api           ->  Application + Domain  (Infrastructure via DI extension methods only)
Sql           ->  (nothing)
```

**Never** add a reference that flows the wrong way. Architecture tests will catch it and fail the build.

### 2.2 Forbidden libraries — do not add these to any production project

| Banned | Use instead |
|---|---|
| MediatR | WolverineFX (`IMessageBus`, handler convention) |
| AutoMapper | Manual mappers in `Features/{Feature}/Mapping/` |
| Entity Framework Core | Dapper + repository pattern |
| `System.Data.SqlClient` | `Microsoft.Data.SqlClient` |
| FluentValidation in Domain | `DomainException` for invariant enforcement |
| Dapper in Domain or Application | Belongs in Infrastructure only |

### 2.3 No inline SQL

All SQL strings live in `TemplateMs.Sql` as `public const string` on `static` classes.  
No SQL string literals anywhere else — not in repositories, not in handlers, not in tests.  
The Roslyn analyzers `TMSSQL001` and `TMSSQL002` will reject any SQL constant that contains a hardcoded database name or schema name — write plain `TableName` references only.

### 2.4 Naming and type conventions

| Concept | Convention |
|---|---|
| Commands / Queries | `sealed record` ending in `Command` or `Query` |
| Handlers | `sealed class` ending in `CommandHandler` or `QueryHandler` — `Handle()` method |
| Validators | `sealed class` extending `AbstractValidator<T>` |
| DTOs | `sealed record` ending in `Dto` in `Features/{Feature}/Dtos/` |
| Integration events | `sealed record` ending in `IntegrationEvent` in `Application/Events/` |
| Domain events | `sealed record` extending `DomainEvent` in `Domain/Events/` |
| Repositories (interfaces) | `IOrderWriteRepository`, `IOrderReadRepository` — feature-namespaced in `Features/{Feature}/Interfaces/` |
| Repository implementations | `OrderWriteRepository`, `OrderReadRepository` — in `Infrastructure/Repositories/` |
| Services | `sealed class` implementing an Application interface |

### 2.5 No public property setters on domain business state

Domain entity business properties (e.g., `CustomerName`, `IsActive`, `OrderDate`) must have `private set`. Audit fields (`CreatedBy`, `CreatedAt`, `UpdatedBy`, `UpdatedAt`, `Id`) may have `set` for Dapper hydration.

### 2.6 Async discipline

- Every async method must accept and propagate `CancellationToken`.
- Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Never use `async void` (except event handlers when unavoidable).

### 2.7 Sealed classes

Mark all handlers, services, repositories, middleware, and filters `sealed` by default. Only abstract base types and interfaces should be unsealed.

### 2.8 Warnings as errors

`Directory.Build.props` sets `TreatWarningsAsErrors=true`. Never suppress a warning with `#pragma warning disable` or `/nowarn` without an explicit, reviewed reason.

---

## 3. File placement rules

| What you are creating | Where it goes |
|---|---|
| New domain aggregate or entity | `src/core/TemplateMs.Domain/Entities/` |
| New domain event | `src/core/TemplateMs.Domain/Events/` |
| New domain exception | `src/core/TemplateMs.Domain/Exceptions/` |
| New command + handler + validator | `src/core/TemplateMs.Application/Features/{Feature}/Commands/` |
| New query + handler + validator | `src/core/TemplateMs.Application/Features/{Feature}/Queries/` |
| New DTO | `src/core/TemplateMs.Application/Features/{Feature}/Dtos/` |
| New domain event handler | `src/core/TemplateMs.Application/Features/{Feature}/Events/` |
| New repository interface | `src/core/TemplateMs.Application/Features/{Feature}/Interfaces/` |
| New integration event | `src/core/TemplateMs.Application/Events/` |
| New mapper | `src/core/TemplateMs.Application/Features/{Feature}/Mapping/` |
| New SQL constants | `src/infrastructure/TemplateMs.Sql/Features/{Feature}/Commands/` or `Queries/` |
| New repository implementation | `src/infrastructure/TemplateMs.Infrastructure/Repositories/` |
| New background service | `src/infrastructure/TemplateMs.Infrastructure/Messaging/{Concern}/` |
| New DI registration | `src/presentation/TemplateMs.Api/DI/AddInfrastructureServices.cs` (infra) or `AddApplicationServices.cs` (app) |
| New controller | `src/presentation/TemplateMs.Api/Controllers/` |
| New request model | `src/presentation/TemplateMs.Api/RequestModels/` |
| New response model | `src/presentation/TemplateMs.Api/ResponseModels/` |

---

## 4. Test rules

- **Framework**: xUnit 2.x, FluentAssertions 7.x, NSubstitute 5.x.
- **Pattern**: Every test follows Arrange / Act / Assert with `// Arrange`, `// Act`, `// Assert` comments.
- **Naming**: PascalCase only. No underscores in method names.
- **Domain tests**: Zero infrastructure, zero mocks. Pure in-memory.
- **Application tests**: Mock only Application interfaces (`IOrderWriteRepository`, `IUnitOfWork`, etc.). Never mock domain types.
- **Integration tests**: Use `SqlServerFixture` + `[Collection(IntegrationTestCollection.Name)]`. Never create a new `MsSqlContainer` inside a test class — that was the old (wrong) pattern.
- **Architecture tests**: Do not modify unless a new rule is being deliberately added. Never comment out or skip an existing rule.
- **Roslyn analyzer tests**: Use `AnalyzerTestHelper.GetDiagnosticsAsync` for in-process compilation + analysis.

---

## 5. The WolverineFX handler pattern

```csharp
// Convention: class ending in "Handler" with a "Handle" method
// No interface required — Wolverine discovers it automatically
public sealed class CreateOrderCommandHandler
{
	// Constructor injection — Wolverine resolves from DI
	public CreateOrderCommandHandler(
		IOrderWriteRepository writeRepository,
		IUnitOfWork unitOfWork,
		IDateTimeProvider dateTimeProvider) { ... }

	// Return type drives InvokeAsync<T> at the call site
	public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
	{
		...
		await _unitOfWork.CommitAsync(cancellationToken);
		return OrderMapper.ToDto(order);
	}
}
```

Middleware is registered globally in `WolverineRegistration.Configure`:

```csharp
opts.Policies.AddMiddleware<LoggingBehavior>();
// ValidationBehavior and TransactionBehavior are added similarly
```

---

## 6. The Unit of Work pattern

```csharp
// DapperUnitOfWork is scoped per request/message
// Repositories take it as a constructor dependency to share the transaction

// In a handler:
_unitOfWork.RegisterAggregate(order);          // track for domain event dispatch
await _writeRepository.AddAsync(order, ct);    // uses _unitOfWork.Connection + Transaction
await _unitOfWork.CommitAsync(ct);             // commits SQL, then dispatches domain events
```

Never call `CommitAsync()` twice in the same handler. `TransactionBehavior` calls it once after the handler returns — handlers that need explicit control (e.g., to raise post-commit events) call it themselves and the middleware call becomes a no-op (the transaction is already committed).

---

## 7. The outbox pattern

When a domain event handler needs to publish an integration event to ActiveMQ:

```csharp
// Inside a domain event handler (Application layer):
var payload = JsonSerializer.Serialize(integrationEvent);
await _unitOfWork.QueueOutboxMessageAsync(
	"TemplateMs.Application.Events.OrderCreatedIntegrationEvent",
	payload,
	cancellationToken);
await _unitOfWork.CommitAsync(cancellationToken);
// OutboxProcessor (background service) handles publishing to AMQ asynchronously
```

Never call `IMessageBusPublisher.PublishAsync` directly from a handler — always go through the outbox.

---

## 8. SQL conventions

```csharp
// SQL lives in TemplateMs.Sql — plain table names, no schema prefix
public static class OrderCommandSql
{
	public const string Insert = """
		INSERT INTO Orders (CustomerName, OrderDate, IsActive, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
		OUTPUT INSERTED.Id
		VALUES (@CustomerName, @OrderDate, @IsActive, @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt);
		""";
}

// Repositories use the constants and the shared UoW connection + transaction
var id = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
	OrderCommandSql.Insert,
	new { order.CustomerName, ... },
	_unitOfWork.Transaction);
```

Query repositories use `IQueryDbConnectionFactory` (read replica) directly — they do not share the UoW transaction.

---

## 9. Adding a new feature — minimum checklist

When asked to add a new feature (e.g., `Customer`, `Product`, `Invoice`):

1. Domain entity extending `BaseAuditableEntity` or `AggregateRoot`.
2. Domain event(s) as `sealed record : DomainEvent`.
3. `OrderNotFoundException`-style exception extending `DomainException`.
4. SQL constants in `TemplateMs.Sql/Features/{Feature}/`.
5. Command(s) + handler(s) + validator(s) in Application feature slice.
6. Query + handler in Application feature slice.
7. `sealed record` DTO in Application feature slice.
8. Domain event handler(s) in Application feature slice.
9. Repository interfaces in Application feature slice.
10. Manual mapper (static class) in Application feature slice.
11. Write and Read repository implementations in Infrastructure.
12. DI registrations in `AddInfrastructureServices.cs`.
13. Controller in Api with request/response models separate from DTOs.
14. Unit tests (domain + application), integration tests (SQL round-trip).

---

## 10. What to do when asked to modify existing code

1. Read the file first — never assume its current content.
2. Check which layer it belongs to and confirm the change respects dependency rules.
3. If modifying a handler, confirm the UoW/commit flow is not broken.
4. If modifying SQL, confirm no schema/DB names are introduced.
5. After any change, verify: does the build still pass? Do architecture tests still pass? Do unit tests still pass?
6. XML document every new public type and member.

---

## 11. Configuration and secrets

- Connection strings are in `appsettings.json` and overridden via environment variables.
- Never hardcode connection strings, passwords, or queue names in source code.
- Use `IOptions<InfrastructureOptions>` for infrastructure config and `IOptions<ActiveMqConfiguration>` for AMQ config.
- The `InfrastructureOptions.SectionName` constant (`"Infrastructure"`) and `ActiveMqConfiguration.SectionName` (`"ActiveMQ"`) are the config section keys.
