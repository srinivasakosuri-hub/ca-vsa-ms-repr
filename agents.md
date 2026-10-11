# TemplateMs — Agent Reference

Detailed technical reference for AI coding agents working in this repository. Read this before making any changes.

---

## Codebase Identity

| Property | Value |
|---|---|
| Solution file | `TemplateMs.slnx` |
| Target framework | `net10.0` (all production projects) |
| Analyzer project target | `netstandard2.0` |
| Language version | `latest` (C# 13+) |
| Nullable reference types | Enabled everywhere |
| Warnings as errors | Yes — `Directory.Build.props` |
| Test framework | xUnit 2.x + FluentAssertions 7.x + NSubstitute 5.x |

---

## Project Map

| Project path | Assembly name | Role |
|---|---|---|
| `src/core/TemplateMs.Domain` | `TemplateMs.Domain` | DDD domain model, zero dependencies |
| `src/core/TemplateMs.Application` | `TemplateMs.Application` | Use-cases, vertical slices, interfaces |
| `src/infrastructure/TemplateMs.Infrastructure` | `TemplateMs.Infrastructure` | Dapper repos, UoW, AMQ, outbox |
| `src/infrastructure/TemplateMs.Sql` | `TemplateMs.Sql` | SQL string constants only |
| `src/infrastructure/TemplateMs.Sql.Analyzers` | `TemplateMs.Sql.Analyzers` | Roslyn build analyzers |
| `src/infrastructure/TemplateMs.Database` | *(SSDT sqlproj)* | DDL scripts, no C# |
| `src/presentation/TemplateMs.Api` | `TemplateMs.Api` | ASP.NET Core 10, controllers, DI |
| `tests/TemplateMs.Domain.Tests` | test | Domain unit tests |
| `tests/TemplateMs.Application.Tests` | test | Application unit tests |
| `tests/TemplateMs.Infrastructure.Tests` | test | Infrastructure unit tests |
| `tests/TemplateMs.Api.Tests` | test | API unit tests |
| `tests/TemplateMs.Architecture.Tests` | test | NetArchTest dependency rules (63 tests) |
| `tests/TemplateMs.Sql.Analyzers.Tests` | test | Roslyn analyzer tests (29 tests) |
| `tests/TemplateMs.IntegrationTests` | test | Testcontainers SQL Server integration |

---

## Dependency Graph

```
TemplateMs.Api
  -> TemplateMs.Application
  -> TemplateMs.Domain
  -> TemplateMs.Infrastructure   (DI extension methods only)

TemplateMs.Infrastructure
  -> TemplateMs.Application
  -> TemplateMs.Domain
  -> TemplateMs.Sql              (string constants)

TemplateMs.Application
  -> TemplateMs.Domain

TemplateMs.Domain       (no references)
TemplateMs.Sql          (no references)
```

Any edge not listed above is **forbidden**. Architecture tests enforce this.

---

## Key Source Files

### Entry point

```
src/presentation/TemplateMs.Api/Program.cs
```

Bootstrap order:
1. `services.AddInfrastructure(config)` — repositories, UoW, messaging, background services.
2. `services.AddApplication(config)` — FluentValidation validators, `IDateTimeProvider`.
3. `services.AddMessageBusServices(config)` — `ActiveMqConfiguration` binding.
4. `builder.AddWolverineMessaging()` — `WolverineOptions` via `WolverineRegistration.Configure`.
5. Middleware: `CorrelationIdMiddleware` → `UseExceptionHandler` → controllers.
6. Scalar: `MapOpenApi()` + `MapScalarApiReference()`.

### DI registrations

All infrastructure DI lives in:
```
src/presentation/TemplateMs.Api/DI/AddInfrastructureServices.cs
```

All application DI lives in:
```
src/presentation/TemplateMs.Api/DI/AddApplicationServices.cs
```

Wolverine configuration lives in:
```
src/infrastructure/TemplateMs.Infrastructure/Messaging/Wolverine/WolverineRegistration.cs
```

---

## Domain Model

### Inheritance chain

```
BaseEntity              (int Id)
  BaseAuditableEntity   (+ CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
	AggregateRoot       (+ _domainEvents, DomainEvents, AddDomainEvent, ClearDomainEvents)
	  Order             (sealed, aggregate root)
	BaseAuditableEntity
	  OrderItem         (sealed, child entity — created via Order.AddItem())
	  OrderItemAttribute(sealed, grandchild — created via OrderItem.AddAttribute())
```

### Order aggregate — key factory/command methods

| Method | What it does |
|---|---|
| `Order.Create(name, date, createdAt)` | Factory — validates, returns new `Order`, does NOT raise event yet |
| `order.RaiseCreatedEvent(createdAt)` | Raises `OrderCreatedEvent` — call AFTER DB Id is assigned |
| `order.AddItem(...)` | Validates, creates `OrderItem`, adds to `_items`, returns item |
| `order.RaiseItemAddedEvent(item)` | Raises `OrderItemAddedEvent` — call AFTER item Id is assigned |
| `order.RemoveItem(itemId, updatedAt)` | Soft-deletes item, raises `OrderItemRemovedEvent` |
| `order.Update(name, date, updatedAt)` | Updates mutable fields, raises `OrderUpdatedEvent` |
| `order.Deactivate(deletedAt)` | Soft-deletes order, raises `OrderDeletedEvent` |
| `order.AddAttributeToItem(...)` | Delegates to `item.AddAttribute`, returns attribute |
| `order.RaiseAttributeAddedEvent(...)` | Raises `OrderItemAttributeAddedEvent` — call AFTER attr Id assigned |
| `order.RemoveAttributeFromItem(...)` | Delegates to `item.RemoveAttribute`, raises event |
| `Order.Reconstitute(...)` | Bypasses invariants — use only in read-side infrastructure |

---

## Application Layer

### Handler pattern

```csharp
// Wolverine discovers by convention: class ending in "Handler", method named "Handle"
public sealed class CreateOrderCommandHandler
{
	public CreateOrderCommandHandler(
		IOrderWriteRepository writeRepo,
		IOrderItemWriteRepository itemRepo,
		IOrderItemAttributeWriteRepository attrRepo,
		IUnitOfWork unitOfWork,
		IDateTimeProvider dateTimeProvider) { ... }

	public async Task<OrderDto> Handle(CreateOrderCommand cmd, CancellationToken ct)
	{
		var now = _dateTimeProvider.UtcNow;
		var order = Order.Create(cmd.CustomerName, cmd.OrderDate, now);
		_unitOfWork.RegisterAggregate(order);
		await _writeRepo.AddAsync(order, ct);
		// ... items and attributes ...
		await _unitOfWork.CommitAsync(ct);
		return OrderMapper.ToDto(order);
	}
}
```

### Middleware pipeline (executed per message)

```
LoggingBehavior.Before()        -> logs envelope type + id
ValidationBehavior.ExecuteAsync -> runs IValidator<T>, throws ValidationException on failure
  [handler executes]
TransactionBehavior.ExecuteAsync -> calls unitOfWork.CommitAsync() after handler
LoggingBehavior.After()         -> logs success
LoggingBehavior.OnException()   -> logs error on failure
```

### Interfaces defined in Application (implemented in Infrastructure)

```
IUnitOfWork
  RegisterAggregate(AggregateRoot)
  QueueOutboxMessageAsync(messageType, payload, ct)
  CommitAsync(ct)
  RollbackAsync(ct)
  Connection: IDbConnection
  Transaction: IDbTransaction

ICommandDbConnectionFactory  -> CreateConnection(): IDbConnection  (write DB)
IQueryDbConnectionFactory    -> CreateConnection(): IDbConnection  (read DB)

IDomainEventDispatcher       -> DispatchAsync(aggregates, ct)
IMessageBusPublisher         -> PublishAsync<T>(message, ct)
ICurrentUserProvider         -> UserId: Guid
IDateTimeProvider            -> UtcNow: DateTime

IOrderWriteRepository        -> AddAsync, UpdateAsync, GetByIdAsync
IOrderReadRepository         -> GetByIdAsync(returns OrderDto?), GetAggregateByIdAsync, GetAllAsync
IOrderItemWriteRepository    -> AddAsync, DeactivateAsync
IOrderItemAttributeWriteRepository -> AddAsync, DeactivateAsync
```

---

## Infrastructure Layer

### DapperUnitOfWork — transaction lifecycle

```csharp
// Created scoped per request/message
public sealed class DapperUnitOfWork : IUnitOfWork, IDisposable, IAsyncDisposable
{
	// Opens SqlConnection + begins ReadCommitted transaction in constructor
	// Connection and Transaction are exposed for repositories

	public void RegisterAggregate(AggregateRoot aggregate)  // add to tracked list
	public async Task QueueOutboxMessageAsync(...)           // INSERT OutboxMessages inside tx
	public async Task CommitAsync(ct)                        // Commit tx, then dispatch domain events
	public Task RollbackAsync(ct)                            // Rollback tx
	public void Dispose() / ValueTask DisposeAsync()        // dispose tx + connection
}
```

### Repository conventions

**Write repositories** — inject `DapperUnitOfWork` directly (not via `IUnitOfWork`) to access the shared `Connection` and `Transaction`:
```csharp
public sealed class OrderWriteRepository : IOrderWriteRepository
{
	private readonly DapperUnitOfWork _unitOfWork;
	private readonly ICurrentUserProvider _currentUser;

	public async Task AddAsync(Order order, CancellationToken ct)
	{
		var id = await _unitOfWork.Connection.ExecuteScalarAsync<int>(
			OrderCommandSql.Insert,
			new { ... },
			_unitOfWork.Transaction);   // participates in the UoW transaction
		order.Id = id;
	}
}
```

**Read repositories** — inject `IQueryDbConnectionFactory`, open their own connection, no transaction:
```csharp
public sealed class OrderReadRepository : IOrderReadRepository
{
	private readonly IQueryDbConnectionFactory _queryFactory;

	public async Task<OrderDto?> GetByIdAsync(int orderId, CancellationToken ct)
	{
		using IDbConnection conn = _queryFactory.CreateConnection();
		using var multi = await conn.QueryMultipleAsync(OrderQuerySql.GetById, new { Id = orderId });
		// ... Dapper multi-result-set mapping ...
	}
}
```

### Background services

| Service | Responsibility |
|---|---|
| `OutboxProcessor` | Polls `OutboxMessages` every `OutboxPollingIntervalMs` ms; publishes via `IMessageBusPublisher`; marks processed/failed |
| `InboxProcessor` | Placeholder — extend for inbound AMQ topic subscriptions |
| `ActiveMqConsumer` | Connects to AMQ broker; extend with `IMessageConsumer` subscriptions per topic |

---

## SQL Layer

### File structure

```
src/infrastructure/TemplateMs.Sql/
  Features/Order/
	Commands/
	  OrderCommandSql.cs           <- public static class
	  OrderItemCommandSql.cs
	  OrderItemAttributeCommandSql.cs
	Queries/
	  OrderQuerySql.cs
  Infrastructure/Outbox/
	OutboxSql.cs
```

### Rules enforced by Roslyn analyzers

| Diagnostic | What triggers it | Example |
|---|---|---|
| `TMSSQL001` Error | 3-part name in `const string` after DML keyword | `FROM MyDb.dbo.Orders` |
| `TMSSQL002` Error | 2-part name in `const string` after DML keyword | `FROM dbo.Orders` |

Both analyzers check `const string` fields only. They detect bracket-quoted, plain, and mixed forms. They fire after: `FROM`, `INTO`, `UPDATE`, `JOIN`, `INNER JOIN`, `LEFT JOIN`, `RIGHT JOIN`, `FULL JOIN`, `CROSS JOIN`.

Column aliases (e.g., `o.Id` in a SELECT list) are **not** flagged because the regex only matches immediately after the DML keyword tokens.

---

## API Layer

### Controller pattern

```csharp
[ApiController]
[Route("api/orders")]
public sealed class OrderController : ControllerBase
{
	private readonly IMessageBus _bus;
	public OrderController(IMessageBus bus) => _bus = bus;

	[HttpPost]
	[ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Create(
		[FromBody] CreateOrderRequest request,
		CancellationToken cancellationToken)
	{
		var command = new CreateOrderCommand(...);
		var dto = await _bus.InvokeAsync<OrderDto>(command, cancellationToken);
		var response = OrderResponse.FromDto(dto!);
		return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
	}
}
```

### Error handling

`GlobalExceptionFilter` (implements `IExceptionHandler`, registered via `AddExceptionHandler<T>`) maps:

| Exception type | HTTP status | Title |
|---|---|---|
| `ValidationException` | 400 | "Validation Failure" |
| `OrderNotFoundException` | 404 | "Resource Not Found" |
| `DomainException` | 422 | "Business Rule Violation" |
| anything else | 500 | "Internal Server Error" |

All responses include `correlationId` from `HttpContext.Items["CorrelationId"]`.

### Middleware pipeline order

```
CorrelationIdMiddleware    -> reads/generates X-Correlation-Id, stores in HttpContext.Items
UseExceptionHandler        -> delegates to GlobalExceptionFilter
UseHttpsRedirection
UseAuthorization
MapControllers
MapOpenApi / MapScalarApiReference
```

---

## Test Infrastructure

### Integration test container (SqlServerFixture)

```csharp
// One container shared across DatabaseIntegrationTests and EndToEndTests
// via ICollectionFixture<SqlServerFixture>

[CollectionDefinition(IntegrationTestCollection.Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<SqlServerFixture> { }

[Collection(IntegrationTestCollection.Name)]
public sealed class DatabaseIntegrationTests
{
	private readonly SqlServerFixture _fixture;
	public DatabaseIntegrationTests(SqlServerFixture fixture) => _fixture = fixture;
	private string ConnectionString => _fixture.ConnectionString;
	// ... tests use ConnectionString, no per-test container spin-up ...
}
```

Never create a new `MsSqlContainer` inside a test class. Always use the shared fixture.

### Analyzer test helper

```csharp
// Compiles a C# source snippet in-process and runs analyzers against it
var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
	sourceCode,
	new HardCodedDatabaseNameAnalyzer());

diagnostics.Should().ContainSingle(d => d.Id == "TMSSQL001");
```

### Architecture test pattern

```csharp
var result = Types.InAssembly(AssemblyReferences.Domain)
	.ShouldNot()
	.HaveDependencyOn("TemplateMs.Application")
	.GetResult();

result.IsSuccessful.Should().BeTrue("because ...");
```

`AssemblyReferences` provides typed anchor properties: `Domain`, `Application`, `Infrastructure`, `Api`, `Sql`, `All`.

---

## Configuration

### Strongly typed options classes

| Class | Section key | Location |
|---|---|---|
| `InfrastructureOptions` | `"Infrastructure"` | `Infrastructure/Configuration/InfrastructureOptions.cs` |
| `ActiveMqConfiguration` | `"ActiveMQ"` | `Infrastructure/Messaging/AMQ/ActiveMqConfiguration.cs` |

### Properties of InfrastructureOptions

| Property | Type | Default |
|---|---|---|
| `CommandConnectionString` | `string` | `""` |
| `QueryConnectionString` | `string` | `""` |
| `OutboxBatchSize` | `int` | `50` |
| `OutboxPollingIntervalMs` | `int` | `5000` |

### Properties of ActiveMqConfiguration

| Property | Type | Default |
|---|---|---|
| `BrokerUrl` | `string` | `"activemq:tcp://localhost:61616"` |
| `Username` | `string` | `"admin"` |
| `Password` | `string` | `"adminPassword1!"` |
| `OrderEventsQueue` | `string` | `"order.events"` |

---

## Common Agent Tasks

### Add a new endpoint to OrderController

1. Define the request record in `Api/RequestModels/`.
2. Define or reuse a command/query in Application.
3. Add the handler and validator in Application.
4. Add repository methods if new SQL is needed.
5. Add SQL constants to `TemplateMs.Sql` — no schema prefix.
6. Register any new repository in `AddInfrastructureServices.cs`.
7. Add the action method to `OrderController`.
8. Add unit tests in `TemplateMs.Api.Tests` and `TemplateMs.Application.Tests`.

### Add a new domain event + handler

1. Create `sealed record MyNewEvent(...)  : DomainEvent` in `Domain/Events/`.
2. Call `AddDomainEvent(new MyNewEvent(...))` from the aggregate method that triggers it.
3. Create `MyNewDomainEventHandler` in `Application/Features/Order/Events/`.
4. If it needs to publish an integration event, create a `sealed record` in `Application/Events/` and route through `QueueOutboxMessageAsync`.
5. No DI registration needed — Wolverine discovers handlers by convention.

### Fix a failing architecture test

1. Read the test body to understand the exact rule being enforced.
2. Identify what code change caused the failure.
3. Fix the root cause (wrong dependency, wrong namespace, wrong base class).
4. Never suppress the test, comment it out, or add a `[Skip]` attribute.
5. If the rule itself is wrong (very rare), update both the test and this document.

### Modify SQL in TemplateMs.Sql

1. Open the relevant `*Sql.cs` file in `TemplateMs.Sql`.
2. Edit the `const string` value.
3. Do not add `schema.table` or `db.schema.table` patterns — `TMSSQL001`/`TMSSQL002` will fail the build.
4. If the SQL adds or removes columns, update the corresponding Dapper parameter objects in the repository.
5. If the schema changes, update the SSDT script in `TemplateMs.Database/dbo/Tables/`.

---

## Build Verification

After any change, run:

```powershell
# Full solution build (catches compiler errors + analyzer errors)
dotnet build TemplateMs.slnx

# Architecture tests (catches dependency rule violations)
dotnet test tests/TemplateMs.Architecture.Tests/TemplateMs.Architecture.Tests.csproj

# Domain + Application unit tests (fast, no infrastructure)
dotnet test tests/TemplateMs.Domain.Tests/TemplateMs.Domain.Tests.csproj
dotnet test tests/TemplateMs.Application.Tests/TemplateMs.Application.Tests.csproj
```

For integration tests, Docker must be running:

```powershell
dotnet test tests/TemplateMs.IntegrationTests/TemplateMs.IntegrationTests.csproj
```

---

## Known Gotchas

| Scenario | What happens | How to avoid it |
|---|---|---|
| Adding `"TemplateMs.Api"` as a string literal in Infrastructure | `InfrastructureDoesNotDependOnApi` architecture test fails — NetArchTest scans string literals | Use a generic constant like `"TemplateMs"` for service names |
| Using compiler-generated types (lambda display classes) in architecture test filters | `NonAggregateEntitiesInheritFromBaseAuditableEntity` may falsely fail | Filter with `&& !t.IsDefined(typeof(CompilerGeneratedAttribute), false)` |
| Creating a new `MsSqlContainer` inside a test class | Each test method gets its own container — 15+ Docker start/stop cycles per run | Always use `SqlServerFixture` + `[Collection(IntegrationTestCollection.Name)]` |
| Adding a `const string` with `dbo.TableName` to `TemplateMs.Sql` | `TMSSQL002` Roslyn error at build time | Write just `TableName` — no schema prefix |
| Writing SQL inline in a repository instead of using `TemplateMs.Sql` | Violates the no-inline-SQL rule, breaks maintainability | Always add a constant to `TemplateMs.Sql` first |
| Not calling `order.RaiseCreatedEvent(...)` after `AddAsync` | `OrderCreatedEvent` is never raised; downstream handlers never fire | Call `RaiseCreatedEvent` in `OrderWriteRepository.AddAsync` after setting `order.Id` |
| Calling `CommitAsync` twice in the same handler | Second call attempts to commit an already-committed transaction — SQL exception | Call `CommitAsync` once; `TransactionBehavior` will find the transaction already committed and skip |
