# TemplateMs

A production-grade **.NET 10 microservice template** combining **Clean Architecture**, **Vertical Slice Architecture**, and **Domain-Driven Design (DDD)**. Built with **WolverineFX** for message handling, **Dapper** for data access, **ActiveMQ** for integration events, and **Scalar** for API documentation.

---

## Table of Contents

- [Why This Template Exists](#why-this-template-exists)
- [Architecture Overview](#architecture-overview)
- [Solution Structure](#solution-structure)
- [Dependency Rules](#dependency-rules)
- [The Full Request Lifecycle](#the-full-request-lifecycle)
- [The Domain Event to Integration Event Pipeline](#the-domain-event-to-integration-event-pipeline)
- [Key Design Decisions](#key-design-decisions)
- [Project Reference Guide](#project-reference-guide)
- [Database Schema](#database-schema)
- [Configuration Reference](#configuration-reference)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Running Tests](#running-tests)
- [Docker](#docker)
- [API Reference](#api-reference)
- [Adding a New Feature Slice](#adding-a-new-feature-slice)
- [Analyzer: TemplateMs.Sql.Analyzers](#analyzer-templatemsqlanalyzers)
- [Architecture Tests](#architecture-tests)
- [Engineering Standards](#engineering-standards)

---

## Why This Template Exists

Most microservice templates make you choose between Clean Architecture (horizontal layers) and Vertical Slice Architecture (feature folders). This template uses **both together**:

- **Clean Architecture** enforces hard dependency rules between rings (Domain → Application → Infrastructure → Api). No inner ring ever knows about an outer ring.
- **Vertical Slice Architecture** organises code inside the Application layer by feature, not by technical concern. `CreateOrderCommand`, its handler, validator, DTO, and interfaces all live together under `Features/Order/Commands/`.
- **DDD** provides the vocabulary: aggregates, entities, domain events, repositories, and a rich domain model that enforces its own invariants.

The result is a codebase that is easy to navigate, impossible to corrupt accidentally (architecture tests fail the build if a dependency rule is broken), and straightforward to extend.

---

## Architecture Overview

```
+--------------------------------------------------+
|                  TemplateMs.Api                  |  <- HTTP / Scalar / Middleware
|  Controllers  RequestModels  ResponseModels       |
|  Middleware  DI extensions                        |
+--------------------+-----------------------------+
                     |  IMessageBus (Wolverine)
+--------------------v-----------------------------+
|              TemplateMs.Application              |  <- Use-cases / Vertical Slices
|  Features/{Feature}/Commands  Queries  DTOs      |
|  Behaviors (Logging  Validation  Transaction)    |
|  Integration Events  Interfaces                  |
+--------+----------------------------+------------+
         |                           |
+--------v---------+   +-------------v-----------+
| TemplateMs.Domain|   | TemplateMs.Infrastructure|
| Aggregates       |   | Repositories  Messaging  |
| Entities  Events |   | Background services      |
| Exceptions       |   +-------------+-----------+
+------------------+                 |
                        +------------v-----------+
                        |    TemplateMs.Sql       |  <- Pure SQL string constants
                        |  No logic  No deps      |
                        +------------------------+
```

### Message flow (WolverineFX)

```
HTTP POST /api/orders
  -> OrderController.Create()
    -> IMessageBus.InvokeAsync<OrderDto>(CreateOrderCommand)
      -> [Logging] -> [Validation] -> [Transaction] middleware
        -> CreateOrderCommandHandler.Handle()
          -> Order.Create()
          -> OrderWriteRepository.AddAsync()  (Dapper, inside UoW transaction)
          -> IUnitOfWork.CommitAsync()
            -> SQL Transaction.Commit()
            -> IDomainEventDispatcher.DispatchAsync()
              -> IMessageBus.PublishAsync(OrderCreatedEvent)
                -> OrderDomainEventHandler.Handle()
                  -> INSERT OutboxMessages (same connection)
                -> [OutboxProcessor background service, polls every 5s]
                  -> ActiveMqPublisher.PublishAsync()
                    -> ActiveMQ: order.events queue
```

---

## Solution Structure

```
wolverine2/
+-- src/
|   +-- core/
|   |   +-- TemplateMs.Domain/           <- DDD domain model (no dependencies)
|   |   +-- TemplateMs.Application/      <- Use-cases, vertical slices
|   +-- infrastructure/
|   |   +-- TemplateMs.Infrastructure/   <- Dapper, AMQ, outbox, UoW
|   |   +-- TemplateMs.Sql/              <- SQL string constants only
|   |   +-- TemplateMs.Sql.Analyzers/    <- Roslyn build-time analyzers
|   |   +-- TemplateMs.Database/         <- SSDT DDL scripts
|   +-- presentation/
|       +-- TemplateMs.Api/              <- ASP.NET Core 10 Web API
+-- tests/
|   +-- TemplateMs.Domain.Tests/
|   +-- TemplateMs.Application.Tests/
|   +-- TemplateMs.Infrastructure.Tests/
|   +-- TemplateMs.Api.Tests/
|   +-- TemplateMs.Architecture.Tests/   <- NetArchTest dependency rules (63 tests)
|   +-- TemplateMs.Sql.Analyzers.Tests/  <- Roslyn analyzer tests (29 tests)
|   +-- TemplateMs.IntegrationTests/     <- Testcontainers, SQL Server
+-- Directory.Build.props                <- TreatWarningsAsErrors, EnableNETAnalyzers
+-- Dockerfile
+-- TemplateMs.slnx
```

---

## Dependency Rules

Enforced by `TemplateMs.Architecture.Tests` on every build. Breaking any rule fails CI.

| Assembly | May reference | Must never reference |
|---|---|---|
| `TemplateMs.Domain` | nothing | Application, Infrastructure, Api, Sql |
| `TemplateMs.Application` | Domain | Infrastructure, Api, Sql |
| `TemplateMs.Infrastructure` | Application, Domain, Sql | Api |
| `TemplateMs.Sql` | nothing | Domain, Application, Infrastructure, Api |
| `TemplateMs.Api` | Application, Domain, Infrastructure (DI only) | Infrastructure concrete types |

**Forbidden libraries (anywhere in production code):**

| Library | Reason |
|---|---|
| MediatR | Replaced by WolverineFX |
| AutoMapper | Replaced by manual mappers (`OrderMapper`) |
| Entity Framework Core | Replaced by Dapper |
| `System.Data.SqlClient` | Use `Microsoft.Data.SqlClient` instead |
| FluentValidation (in Domain) | Domain enforces invariants via `DomainException` |
| Dapper (in Domain or Application) | Persistence is an infrastructure concern |

---

## The Full Request Lifecycle

### POST /api/orders — Create an Order

**1. HTTP layer** (`OrderController.Create`)  
Deserialises `CreateOrderRequest`, maps to `CreateOrderCommand` (sealed record), calls `IMessageBus.InvokeAsync<OrderDto>(command)`.

**2. Wolverine middleware pipeline**  
`LoggingBehavior.Before` logs the envelope. `ValidationBehavior.ExecuteAsync` runs `CreateOrderCommandValidator` — returns HTTP 400 on failure. `TransactionBehavior.ExecuteAsync` wraps the handler and calls `CommitAsync()` on success.

**3. Handler** (`CreateOrderCommandHandler.Handle`)  
Calls `Order.Create()`, registers aggregate with the UoW, persists via Dapper, iterates items and attributes, returns `OrderMapper.ToDto(order)`.

**4. Unit of Work commit** (`DapperUnitOfWork.CommitAsync`)  
Commits `SqlTransaction`. Then calls `IDomainEventDispatcher.DispatchAsync` which publishes each domain event via `IMessageBus.PublishAsync`.

**5. Domain event handling** (`OrderDomainEventHandler.Handle`)  
Receives `OrderCreatedEvent`, serialises `OrderCreatedIntegrationEvent` to JSON, calls `IUnitOfWork.QueueOutboxMessageAsync` which INSERTs into `OutboxMessages`.

**6. Outbox processing** (`OutboxProcessor` background service)  
Polls every 5 000 ms, reads batches of 50, calls `ActiveMqPublisher.PublishAsync`, marks records processed or failed.

**7. API response**  
Returns HTTP 201 Created with `OrderResponse` body and `Location` header.

---

## The Domain Event to Integration Event Pipeline

```
Order aggregate
  AddDomainEvent(OrderCreatedEvent)
      |
      | stored on AggregateRoot._domainEvents
      v
DapperUnitOfWork.CommitAsync()
  1. SQL Transaction.Commit()
  2. IDomainEventDispatcher.DispatchAsync()
      |
      | IMessageBus.PublishAsync(OrderCreatedEvent)
      v
OrderDomainEventHandler.Handle()
  -> creates OrderCreatedIntegrationEvent payload (JSON)
  -> INSERT OutboxMessages (inside new UoW transaction)
      |
      | (persisted, not yet sent to AMQ)
      v
OutboxProcessor (BackgroundService, polls every 5s)
  -> reads unprocessed batch
      |
      | IMessageBusPublisher.PublishAsync
      v
ActiveMqPublisher  ->  ActiveMQ: order.events queue
```

**Why the outbox pattern?**  
Publishing to ActiveMQ and committing the SQL transaction are two independent operations. A crash between them leaves data inconsistent. The outbox writes the event to the database inside the same SQL transaction as the business data, guaranteeing at-least-once delivery without a distributed transaction.

---

## Key Design Decisions

### WolverineFX instead of MediatR

WolverineFX provides convention-based handler discovery (a class ending in `Handler` with a `Handle` method — no interface required), a built-in middleware pipeline with `Before`/`After`/`Finally`/`OnException` hooks, SQL Server-backed durable messaging via `WolverineFx.SqlServer`, and both request/response (`InvokeAsync<T>`) and fire-and-forget (`PublishAsync`) dispatch.

### Dapper instead of Entity Framework

All SQL lives in `TemplateMs.Sql`. Roslyn analyzers enforce at compile time that no hardcoded database names (`TMSSQL001`) or schema names (`TMSSQL002`) appear in those strings. The SQL is explicit, reviewable, and environment-agnostic.

### Dual connection factories

`ICommandDbConnectionFactory` → write database. `IQueryDbConnectionFactory` → read database. Both point to the same server in development. A read-replica can be introduced in production by changing configuration only.

### Two-level mapping

| Layer | Type | Purpose |
|---|---|---|
| Domain → Application | `OrderDto` | Internal data shape, all fields |
| Application → Api | `OrderResponse` | HTTP contract, independently versioned |

### Sealed and record types

- Handlers, services, repositories: `sealed class` — no accidental inheritance.
- Commands, queries, DTOs, domain events: `sealed record` — structural equality, immutability.
- Domain entities: `sealed class` — mutable state managed through factory and business methods only.

---

## Project Reference Guide

### TemplateMs.Domain

Innermost ring. Zero NuGet packages. Zero project references.

| Type | Purpose |
|---|---|
| `BaseEntity` | `int Id` |
| `BaseAuditableEntity` | Adds audit fields + `MarkAsCreated`/`MarkAsUpdated` helpers |
| `AggregateRoot` | Extends `BaseAuditableEntity`, tracks domain events, exposes `ClearDomainEvents()` |
| `DomainEvent` | Abstract record — base for all domain events |
| `DomainException` | Base exception for all domain rule violations |
| `Order` | Aggregate root; owns `OrderItem` collection; enforces all business invariants |
| `OrderItem` | Entity created exclusively through `Order.AddItem()` |
| `OrderItemAttribute` | Entity created through `OrderItem.AddAttribute()` |
| `OrderCreatedEvent` | Raised after INSERT and DB Id assigned to the order |
| `OrderNotFoundException` | Thrown by read handlers when an order ID is not found |

### TemplateMs.Application

Use-case ring. References Domain only.

| Folder | Contents |
|---|---|
| `Behaviors/` | `LoggingBehavior`, `ValidationBehavior`, `TransactionBehavior` — Wolverine middleware |
| `Common/Interfaces/` | `IUnitOfWork`, `ICommandDbConnectionFactory`, `IQueryDbConnectionFactory`, `IDateTimeProvider`, `IDomainEventDispatcher`, `IMessageBusPublisher`, `ICurrentUserProvider` |
| `Events/` | 7 integration events as sealed records (cross-service AMQ messages) |
| `Exceptions/` | `ValidationException` |
| `Features/Order/Commands/` | `CreateOrderCommand`, `AddOrderItemCommand`, `RemoveOrderItemCommand`, `RemoveOrderItemAttributeCommand` + handlers + validators |
| `Features/Order/Queries/` | `GetOrderQuery` + handler + validator |
| `Features/Order/Dtos/` | `OrderDto`, `OrderItemDto`, `OrderItemAttributeDto` |
| `Features/Order/Events/` | 7 domain event handlers (one per domain event type) |
| `Features/Order/Interfaces/` | `IOrderReadRepository`, `IOrderWriteRepository`, `IOrderItemWriteRepository`, `IOrderItemAttributeWriteRepository` |
| `Features/Order/Mapping/` | `OrderMapper` — manual, zero reflection |

### TemplateMs.Infrastructure

Outer ring. Implements all Application interfaces.

| Folder | Contents |
|---|---|
| `Database/` | `CommandDbConnectionFactory`, `QueryDbConnectionFactory`, `DapperTypeMappings` |
| `Repositories/` | `OrderWriteRepository`, `OrderReadRepository`, `OrderItemWriteRepository`, `OrderItemAttributeWriteRepository` |
| `UnitOfWork/` | `DapperUnitOfWork` — transaction management + domain event dispatch |
| `Messaging/Wolverine/` | `WolverineRegistration` (handler discovery, SQL durability), `WolverineEventForwarders` |
| `Messaging/AMQ/` | `ActiveMqPublisher`, `ActiveMqConsumer`, `ActiveMqConfiguration` |
| `Messaging/Outbox/` | `OutboxProcessor` (BackgroundService), `OutboxStorage`, `OutboxRecord` |
| `Messaging/Inbox/` | `InboxProcessor` (BackgroundService), `InboxRecord` |
| `Services/` | `CurrentUserProvider` (JWT `sub` claim → Guid), `SystemDateTimeProvider` |
| `Configuration/` | `InfrastructureOptions` (strongly typed config) |

### TemplateMs.Sql

Pure SQL strings. Zero NuGet packages. Zero project references.

| Class | SQL fields |
|---|---|
| `OrderCommandSql` | `Insert`, `UpdateStatus`, `GetOrderIdByDomainId` |
| `OrderItemCommandSql` | `Insert`, `Deactivate` |
| `OrderItemAttributeCommandSql` | `Insert`, `Deactivate` |
| `OrderQuerySql` | `GetById` (3-grid multi-result-set), `GetAll` |
| `OutboxSql` | `Insert`, `SelectUnprocessed`, `MarkProcessed`, `MarkFailed` |

### TemplateMs.Api

Presentation ring. References Application and Infrastructure only through DI extension methods.

| File | Purpose |
|---|---|
| `Program.cs` | Bootstrap: services, middleware pipeline, Scalar, Wolverine |
| `Controllers/OrderController.cs` | 6 endpoints covering full CRUD + attribute management |
| `Middleware/CorrelationIdMiddleware.cs` | Reads or generates `X-Correlation-Id` header |
| `Filters/GlobalExceptionFilter.cs` | Maps exceptions to RFC 7807 ProblemDetails |
| `DI/AddInfrastructureServices.cs` | Wires all Infrastructure dependencies into the DI container |
| `DI/AddApplicationServices.cs` | Registers FluentValidation validators and `IDateTimeProvider` |
| `DI/AddWolverine.cs` | Configures WolverineFX via `WolverineRegistration` |
| `DI/AddMessageBus.cs` | Binds `ActiveMqConfiguration` from `appsettings.json` |

---

## Database Schema

```sql
CREATE TABLE Orders (
    Id           INT IDENTITY(1,1)  PRIMARY KEY,
    CustomerName NVARCHAR(200)      NOT NULL,
    OrderDate    DATETIME2          NOT NULL,
    IsActive     BIT                NOT NULL DEFAULT 1,
    CreatedBy    UNIQUEIDENTIFIER   NOT NULL,
    CreatedAt    DATETIME2          NOT NULL,
    UpdatedBy    UNIQUEIDENTIFIER   NOT NULL,
    UpdatedAt    DATETIME2          NOT NULL
);

CREATE TABLE OrderItems (
    Id          INT IDENTITY(1,1)  PRIMARY KEY,
    OrderId     INT                NOT NULL REFERENCES Orders(Id),
    ProductName NVARCHAR(200)      NOT NULL,
    Quantity    INT                NOT NULL,
    UnitPrice   DECIMAL(18,2)      NOT NULL,
    IsActive    BIT                NOT NULL DEFAULT 1,
    CreatedBy   UNIQUEIDENTIFIER   NOT NULL,
    CreatedAt   DATETIME2          NOT NULL,
    UpdatedBy   UNIQUEIDENTIFIER   NOT NULL,
    UpdatedAt   DATETIME2          NOT NULL
);

CREATE TABLE OrderItemAttributes (
    Id             INT IDENTITY(1,1)  PRIMARY KEY,
    OrderItemId    INT                NOT NULL REFERENCES OrderItems(Id),
    AttributeName  NVARCHAR(100)      NOT NULL,
    AttributeValue NVARCHAR(500)      NOT NULL,
    IsActive       BIT                NOT NULL DEFAULT 1,
    CreatedBy      UNIQUEIDENTIFIER   NOT NULL,
    CreatedAt      DATETIME2          NOT NULL,
    UpdatedBy      UNIQUEIDENTIFIER   NOT NULL,
    UpdatedAt      DATETIME2          NOT NULL
);

CREATE TABLE OutboxMessages (
    Id          UNIQUEIDENTIFIER PRIMARY KEY,
    MessageType NVARCHAR(500)    NOT NULL,
    Payload     NVARCHAR(MAX)    NOT NULL,
    CreatedAt   DATETIME2        NOT NULL,
    ProcessedAt DATETIME2        NULL,
    Error       NVARCHAR(MAX)    NULL
);
```

DDL scripts live in `src/infrastructure/TemplateMs.Database/dbo/Tables/`. Integration tests copy them to output automatically via `<Content>` items in the test project file.

---

## Configuration Reference

```json
{
  "Infrastructure": {
    "CommandConnectionString": "Server=localhost;Database=TemplateMsDb;User Id=sa;Password=...;TrustServerCertificate=true;",
    "QueryConnectionString":   "Server=localhost;Database=TemplateMsDb;User Id=sa;Password=...;TrustServerCertificate=true;Application Intent=ReadOnly;",
    "OutboxBatchSize":         50,
    "OutboxPollingIntervalMs": 5000
  },
  "ActiveMQ": {
    "BrokerUrl":        "activemq:tcp://localhost:61616",
    "Username":         "admin",
    "Password":         "adminPassword1!",
    "OrderEventsQueue": "order.events"
  }
}
```

Docker / Kubernetes environment variable overrides use the `__` separator:

| Variable | Example value |
|---|---|
| `Infrastructure__CommandConnectionString` | `Server=sql;Database=TemplateMsDb;...` |
| `Infrastructure__OutboxPollingIntervalMs` | `3000` |
| `ActiveMQ__BrokerUrl` | `activemq:tcp://activemq:61616` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0+ |
| Docker Desktop | Any current version |
| SQL Server | 2022 via Docker (`mcr.microsoft.com/mssql/server:2022-latest`) |
| ActiveMQ Classic | Latest via Docker (`apache/activemq-classic:latest`) |

---

## Getting Started

```powershell
# 1. Restore
dotnet restore TemplateMs.slnx

# 2. Start SQL Server
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong*Password12" `
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# 3. Apply schema (run each script in order)
$scripts = @("Orders","OrderItems","OrderItemAttributes","OutboxMessages")
foreach ($s in $scripts) {
    sqlcmd -S localhost -U sa -P "YourStrong*Password12" `
      -i "src/infrastructure/TemplateMs.Database/dbo/Tables/$s.sql"
}

# 4. Start ActiveMQ (optional — needed only for AMQ event publishing). To download below package you will have to create a login and enter userid and password to download this image. 
docker run -p 61616:61616 -p 8161:8161 --name activemq -d amq7/amq-broker:latest
# Admin console: http://localhost:8161  (admin / admin)

# 5. Run the API
dotnet run --project src/presentation/TemplateMs.Api/TemplateMs.Api.csproj
# Scalar docs: http://localhost:5000/scalar/v1

# 6. Quick test
curl -X POST http://localhost:5000/api/orders `
  -H "Content-Type: application/json" `
  -d '{"customerName":"Jane Doe","orderDate":"2025-06-01T00:00:00Z","items":[{"productName":"Widget","quantity":2,"unitPrice":9.99}]}'
```

---

## Running Tests

```powershell
# All tests
dotnet test TemplateMs.slnx

# Individual projects
dotnet test tests/TemplateMs.Architecture.Tests/TemplateMs.Architecture.Tests.csproj
dotnet test tests/TemplateMs.Sql.Analyzers.Tests/TemplateMs.Sql.Analyzers.Tests.csproj

# Integration tests (requires Docker running)
dotnet test tests/TemplateMs.IntegrationTests/TemplateMs.IntegrationTests.csproj
```

Integration tests use a **single shared SQL Server container** per test run (`SqlServerFixture` + xUnit `ICollectionFixture<T>`). The container starts once, the schema is applied once, and all tests in `DatabaseIntegrationTests` and `EndToEndTests` share it. This replaces a previous pattern that spun up a new container per test method.

---

## Docker

```powershell
# Build
docker build -t templatems:latest .

# Run (supply connection strings via environment variables)
docker run -p 8080:8080 `
  -e "Infrastructure__CommandConnectionString=Server=host.docker.internal;Database=TemplateMsDb;User Id=sa;Password=YourStrong*Password12;TrustServerCertificate=true;" `
  -e "Infrastructure__QueryConnectionString=Server=host.docker.internal;Database=TemplateMsDb;User Id=sa;Password=YourStrong*Password12;TrustServerCertificate=true;" `
  -e "ActiveMQ__BrokerUrl=activemq:tcp://host.docker.internal:61616" `
  templatems:latest
```

Multi-stage Dockerfile: SDK image for compile + publish, ASP.NET runtime image for final layer. Runs as non-root user (`appuser`) on port 8080.

---

## API Reference

| Method | Path | Handler | Success |
|---|---|---|---|
| `POST` | `/api/orders` | `CreateOrderCommandHandler` | 201 Created |
| `GET` | `/api/orders/{id}` | `GetOrderQueryHandler` | 200 OK |
| `POST` | `/api/orders/{orderId}/items` | `AddOrderItemCommandHandler` | 201 Created |
| `DELETE` | `/api/orders/{orderId}/items/{itemId}` | `RemoveOrderItemCommandHandler` | 204 No Content |
| `POST` | `/api/orders/{orderId}/items/{itemId}/attributes` | `AddOrderItemAttributeCommandHandler` | 201 Created |
| `DELETE` | `/api/orders/{orderId}/items/{itemId}/attributes/{attrId}` | `RemoveOrderItemAttributeCommandHandler` | 204 No Content |

Error response format (RFC 7807 `application/problem+json`):

```json
{
  "title": "Validation Failure",
  "status": 400,
  "errors": { "CustomerName": ["CustomerName is required."] },
  "correlationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Status | Trigger |
|---|---|
| 400 | FluentValidation failure |
| 404 | `OrderNotFoundException` |
| 422 | `DomainException` (business rule violation) |
| 500 | Unexpected exception |

---

## Adding a New Feature Slice

Use `Product` as the example. Follow this checklist in order.

**Step 1 — Domain** (`src/core/TemplateMs.Domain/`)
```csharp
// Entities/Product.cs
public sealed class Product : BaseAuditableEntity { ... }

// Events/ProductCreatedEvent.cs
public sealed record ProductCreatedEvent(int ProductId, ...) : DomainEvent;

// Exceptions/ProductNotFoundException.cs
public sealed class ProductNotFoundException : DomainException { ... }
```

**Step 2 — SQL constants** (`src/infrastructure/TemplateMs.Sql/Features/Product/`)
```csharp
// Commands/ProductCommandSql.cs
public static class ProductCommandSql
{
    // No schema or DB prefixes — TMSSQL001/TMSSQL002 will fail the build if added
    public const string Insert = "INSERT INTO Products (...) OUTPUT INSERTED.Id VALUES (...);";
}
```

**Step 3 — Application slice** (`src/core/TemplateMs.Application/Features/Product/`)
```
Commands/
  CreateProductCommand.cs            <- sealed record
  CreateProductCommandHandler.cs     <- sealed class, Handle() method
  CreateProductCommandValidator.cs   <- AbstractValidator<CreateProductCommand>
Queries/
  GetProductQuery.cs + Handler
Dtos/ProductDto.cs                   <- sealed record
Events/ProductCreatedDomainEventHandler.cs
Interfaces/
  IProductReadRepository.cs
  IProductWriteRepository.cs
Mapping/ProductMapper.cs             <- static class, manual mapping
```

**Step 4 — Infrastructure** (`src/infrastructure/TemplateMs.Infrastructure/Repositories/`)
```csharp
public sealed class ProductWriteRepository : IProductWriteRepository { ... }
public sealed class ProductReadRepository  : IProductReadRepository  { ... }
```

Register in `AddInfrastructureServices.cs`:
```csharp
services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
services.AddScoped<IProductReadRepository,  ProductReadRepository>();
```

**Step 5 — API** (`src/presentation/TemplateMs.Api/`)
```csharp
[ApiController, Route("api/products")]
public sealed class ProductController : ControllerBase
{
    private readonly IMessageBus _bus;
    public ProductController(IMessageBus bus) => _bus = bus;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var dto = await _bus.InvokeAsync<ProductDto>(new CreateProductCommand(...), ct);
        return CreatedAtAction(nameof(GetById), new { id = dto!.Id }, ProductResponse.FromDto(dto));
    }
}
```

**Step 6 — Tests**
- `TemplateMs.Domain.Tests` — invariants, event raising, exception throwing.
- `TemplateMs.Application.Tests` — handler happy/failure paths, validator accept/reject.
- `TemplateMs.IntegrationTests` — SQL INSERT/SELECT round-trip.
- Architecture tests — automatically covered by the existing 63 rules.

---

## Analyzer: TemplateMs.Sql.Analyzers

Roslyn analyzer project (`netstandard2.0`) wired into `TemplateMs.Sql` as `OutputItemType="Analyzer"`. Runs on every build of `TemplateMs.Sql`.

| Diagnostic | Severity | Rule |
|---|---|---|
| `TMSSQL001` | Error | No hardcoded 3-part database names in `const string` SQL fields (`MyDb.dbo.Orders` or `[MyDb].[dbo].[Orders]`) |
| `TMSSQL002` | Error | No hardcoded 2-part schema names in `const string` SQL fields (`dbo.Orders` or `[dbo].[Orders]`) |

Both analyzers fire only after SQL DML keywords (`FROM`, `INTO`, `UPDATE`, `JOIN`, etc.) so column aliases (`o.Id` in a SELECT list) are never flagged. They detect plain, fully-bracketed, and mixed bracket styles.

**29 tests** in `TemplateMs.Sql.Analyzers.Tests` cover plain names, bracketed names, mixed styles, all DML keywords, raw string literals, cross-analyzer separation (3-part fires only TMSSQL001, 2-part fires only TMSSQL002), and 12 negative cases (table-only names, non-const fields, local variables, column aliases, empty strings).

---

## Architecture Tests

63 tests in `TemplateMs.Architecture.Tests` using **NetArchTest.Rules**:

| Class | What it enforces |
|---|---|
| `CleanArchitectureTests` | All ring-to-ring dependency direction rules |
| `DomainModelTests` | Entity hierarchy, namespace placement, sealed constraints, no public setters on business-state properties |
| `ForbiddenLibrariesTests` | AutoMapper, MediatR, EF Core, SqlClient banned per layer |
| `HandlerConventionTests` | Handlers sealed, commands/queries as records, validators inherit `AbstractValidator<T>`, repository naming suffixes |
| `VerticalSliceTests` | Handler co-location with their command/query, no cross-slice coupling, feature folder naming |

---

## Engineering Standards

| Standard | How it is enforced |
|---|---|
| No inline SQL | All SQL in `TemplateMs.Sql`; Roslyn analyzers block DB/schema names at build time |
| No AutoMapper | `ForbiddenLibrariesTests` in Architecture Tests |
| No MediatR | `ForbiddenLibrariesTests` in Architecture Tests |
| Warnings as errors | `Directory.Build.props`: `TreatWarningsAsErrors=true` |
| Nullable reference types | Enabled on all production projects |
| Sealed by default | All handlers, services, repositories, middleware are `sealed` |
| Records for immutable types | Commands, queries, DTOs, integration events, domain events |
| `CancellationToken` everywhere | Propagated through every async method signature |
| No `.Result` / `.Wait()` | Async all the way from controller to repository |
| RFC 7807 error responses | `GlobalExceptionFilter` returns `ProblemDetails` JSON with `correlationId` |
| Correlation ID | `CorrelationIdMiddleware` reads `X-Correlation-Id` header or generates a GUID |
| XML documentation | `GenerateDocumentationFile=true`; every public type and member documented |
| AAA test pattern | `// Arrange`, `// Act`, `// Assert` comments in all tests |
| PascalCase test names | No underscores in any test method name |
| One container per test run | `SqlServerFixture` + `ICollectionFixture<T>` — no per-test Docker container lifecycle |

