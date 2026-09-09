# E-Commerce Order Management API

Enterprise ASP.NET Core Web API using **CQRS + MediatR + EF Core + SQL Server**.

## Quick Start

```bash
# 1. Start SQL Server
docker compose up -d

# 2. Run API (applies migrations automatically)
cd src/ECommerce.API
dotnet run

# 3. Swagger UI
open http://localhost:5162/swagger

# 4. Run tests
dotnet test
```

**Connection string:** `appsettings.json` → `Server=localhost,1433;Database=ECommerceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;`

---

## 1. Architecture Overview

| Layer | Responsibility |
|-------|----------------|
| **ECommerce.API** | HTTP boundary — routing, controllers, middleware, DI composition |
| **ECommerce.Application** | Use cases — CQRS commands/queries, validation, MediatR behaviors, DTOs |
| **ECommerce.Domain** | Business core — entities, enums, value objects, domain exceptions |
| **ECommerce.Infrastructure** | Technical concerns — EF Core `AppDbContext`, Fluent API configurations |

**Dependency rule:** API → Application + Infrastructure → Domain. Domain has zero external dependencies.

**Repository decision:** **Direct `DbContext` via `IApplicationDbContext`** — no generic repository.

| Approach | Verdict |
|----------|---------|
| Repository + UoW | Adds ceremony; EF Core `DbContext` already implements UoW |
| Direct DbContext | **Selected** — testable via interface, supports complex LINQ, less abstraction tax |

---

## 2. Project Structure

```
src/
  ECommerce.API/           Controllers, Middleware, Program.cs
  ECommerce.Application/   Features/Orders/{Commands,Queries}, Behaviors, DTOs
  ECommerce.Domain/        Entities, Enums, ValueObjects, Exceptions
  ECommerce.Infrastructure/Persistence, Configurations
tests/
  ECommerce.Application.Tests/
```

---

## 3. Domain Model

**Order** — aggregate root with `OrderItem` children, `ShippingAddress` value object, `RowVersion` for optimistic concurrency.

**OrderStatus:** `Pending → Confirmed → Shipped → Delivered | Cancelled`

Domain methods (`Order.Update`) encapsulate invariants; handlers orchestrate persistence.

---

## 4. EF Core / Database

**Tables:** `Orders`, `OrderItems` (cascade delete on items)

| Index | Why |
|-------|-----|
| `IX_Orders_OrderNumber` (unique) | Lookup by business key; prevents duplicates at DB level |
| `IX_Orders_CustomerId` | Customer order history queries |
| `IX_Orders_OrderDate` | Date-range reporting, default sort |
| `IX_Orders_Status` | Filter `?status=Pending` |
| `IX_OrderItems_OrderId` | FK join performance |

`RowVersion` configured only for SQL Server (`AppDbContext.OnModelCreating`).

---

## 5–9. CQRS Operations

### Create — `POST /api/orders`

```
Controller → CreateOrderCommand → ValidationBehavior → CreateOrderCommandHandler → EF Core → SQL Server
```

- `CreateOrderCommandValidator` — unique `OrderNumber`, `TotalAmount > 0`, ≥1 item, `Quantity > 0`, `UnitPrice > 0`
- Returns `CreateOrderResponse` (not entity)

### Get By Id — `GET /api/orders/{id}`

Projection via `.Select()` → `OrderDto`. **Why projection?** Fetches only required columns, no tracking overhead, avoids loading full entity graph, prevents accidental lazy-load N+1.

### Get All — `GET /api/orders?pageNumber=1&pageSize=20&status=Pending`

`Skip/Take` before `ToListAsync()` — pagination happens in SQL (`OFFSET/FETCH`), not in memory.

### Update — `PUT /api/orders/{id}`

| PUT | PATCH |
|-----|-------|
| Replaces full resource | Partial field update |
| **Used here** — client sends complete order state | Better for large resources with sparse edits |

Flow: load tracked entity → validate `RowVersion` → sync items by SKU → `SaveChangesAsync`.

### Delete — `DELETE /api/orders/{id}`

- Not found → `NotFoundException` → 404
- `OrderItem` records removed via **cascade delete** (Fluent API `OnDelete(DeleteBehavior.Cascade)`)
- Success → **204 No Content**

---

## 10. Validation Behavior

```csharp
// MediatR Request → ValidationBehavior → Handler
```

All `FluentValidation` validators run in pipeline **before** handler. Controllers stay free of `if` validation blocks.

---

## 11. Exception Handling

`ExceptionMiddleware` maps:

| Exception | HTTP |
|-----------|------|
| `ValidationException` | 400 |
| `NotFoundException` | 404 |
| `BusinessRuleException` | 409 |
| Other | 500 |

---

## 12. Controller

`OrdersController` — thin: maps HTTP ↔ MediatR commands/queries, propagates `CancellationToken`. **Never touches `DbContext`.**

---

## 13. Unit Tests

**10 tests** covering: valid/invalid create, duplicate order number, get existing/missing, update success/concurrency/missing, delete success/missing.

| Unit Test | Integration Test |
|-----------|------------------|
| Handler logic, validation rules | EF mappings, SQL constraints, migrations |
| InMemory provider | SQL Server provider |
| Mocked/fast | Real database round-trips |

---

## 14. API Examples

### POST /api/orders
```json
// Request
{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "orderNumber": "ORD-2026-001",
  "orderDate": "2026-09-09T10:00:00Z",
  "status": 0,
  "shippingAddress": {
    "street": "742 Evergreen Terrace",
    "city": "Springfield",
    "state": "IL",
    "postalCode": "62701",
    "country": "USA"
  },
  "items": [
    { "productSku": "SKU-WIDGET", "productName": "Premium Widget", "quantity": 2, "unitPrice": 49.99 }
  ]
}

// Response 201
{ "id": "5d67e80a-e09d-45f3-846a-2654ceab43df", "orderNumber": "ORD-2026-001", "totalAmount": 99.98 }
```

### GET /api/orders?pageNumber=1&pageSize=20&status=0
```json
{
  "items": [{ "id": "...", "customerId": "...", "orderNumber": "ORD-2026-001", "orderDate": "2026-09-09T10:00:00", "totalAmount": 99.98, "status": 0 }],
  "pageNumber": 1, "pageSize": 20, "totalCount": 1
}
```

### GET /api/orders/{id}
Returns full `OrderDto` with items and shipping address.

### PUT /api/orders/{id}
```json
{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "orderNumber": "ORD-2026-001",
  "orderDate": "2026-09-09T10:00:00Z",
  "status": 1,
  "shippingAddress": { "street": "742 Evergreen Terrace", "city": "Springfield", "state": "IL", "postalCode": "62701", "country": "USA" },
  "items": [{ "productSku": "SKU-WIDGET", "productName": "Premium Widget", "quantity": 3, "unitPrice": 49.99 }],
  "rowVersion": null
}
```
→ **204 No Content**

### DELETE /api/orders/{id}
→ **204 No Content**

---

## 15. Runtime Execution Flow

```
HTTP Request
  → ASP.NET Core Routing
  → OrdersController
  → IMediator.Send(command/query)
  → Pipeline Behaviors (Validation → Performance)
  → Handler
  → IApplicationDbContext (EF Core)
  → SQL Server
  → Handler Result
  → Controller
  → HTTP Response
```

---

## 16. CQRS + MediatR Internal Working

When `_mediator.Send(command)` executes:

1. MediatR resolves `IRequestHandler<TRequest,TResponse>` from DI (registered via assembly scan)
2. Builds behavior chain (`IPipelineBehavior<,>`) — outermost behavior calls next
3. `ValidationBehavior` runs all `IValidator<TRequest>` instances
4. Inner behavior invokes handler `Handle()`
5. Handler uses `IApplicationDbContext`, calls `SaveChangesAsync`
6. Result propagates back through behaviors to controller

**DI resolution:** `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` scans assembly for handlers and registers them as transient.

---

## 17. Performance Considerations

| Technique | Order Management Use |
|-----------|---------------------|
| `AsNoTracking()` | All read queries (`GetOrderById`, `GetOrders`) |
| Projection `.Select()` | List/detail DTOs — no full entity materialization |
| `Skip/Take` | Paginated order list — avoids loading 100K orders |
| Indexes | Filter by status, customer, date range |
| Async APIs | All DB calls use `*Async` + `CancellationToken` |
| Avoid `Include()` on reads | Projection replaces eager loading |
| Tracking overhead | Writes tracked; reads untracked |

**CancellationToken:** Under load, cancelled HTTP requests stop DB work — prevents thread pool exhaustion from abandoned queries.

---

## 18. Concurrency

`RowVersion` (SQL Server `rowversion`) on `Order`.

**Scenario:** Admin A loads order → Admin B updates → Admin A submits stale `rowVersion` → **409 Conflict**.

Client should reload and retry. Handler checks `request.RowVersion` before save; `DbUpdateConcurrencyException` caught on SQL Server.

---

## 19. Production Trade-offs

| Decision | Trade-off |
|----------|-----------|
| CQRS in-process | Separation of concerns without distributed complexity |
| No repository layer | Faster development; test via `IApplicationDbContext` |
| FluentValidation in pipeline | Centralized; don't put business rules in validators |
| Direct DbContext | Couples Application to EF — mitigated by interface |
| `PerformanceBehavior` | Logs slow requests >500ms — optional observability |

---

## 20. Common Anti-Patterns

| Anti-Pattern | Problem | Fix |
|--------------|---------|-----|
| Fat Controller | Mixed HTTP + business + data access | Thin controller + MediatR |
| Fat Handler | 500-line `Handle()` | Extract domain methods, smaller handlers |
| Generic Repository | Leaky abstraction over `IQueryable` | `IApplicationDbContext` |
| Return EF entities | Exposes persistence model, over-fetching | DTOs + projection |
| DbContext in Controller | Untestable, violates layering | MediatR handler |
| Business logic in DTOs | Anemic + wrong layer | Domain entity methods |
| Business logic in validation | Validators check shape, not workflow | Domain exceptions for rules |
| Unnecessary `Include()` | Cartesian explosion, over-fetching | Projection |
| CQRS for trivial CRUD | Ceremony without benefit | Simple service for 1-table apps |

---

## 21. Interview Questions / Answers

1. **What is CQRS?** — Separate models for reads (queries) and writes (commands).
2. **What problem does it solve?** — Different read/write optimization; clearer intent; scales teams.
3. **Why use it?** — Complex domains, different read/write shapes, pipeline cross-cutting concerns.
4. **What is MediatR?** — In-process mediator dispatching requests to handlers.
5. **CQRS vs MediatR** — CQRS is pattern; MediatR is library implementing request/handler dispatch.
6. **Command vs Query** — Command mutates state; query is side-effect free.
7. **Command vs Handler** — Command is message; handler executes it.
8. **Request vs Handler** — `IRequest<T>` defines contract; `IRequestHandler<,>` implements it.
9. **Pipeline Behavior** — Middleware for cross-cutting concerns (validation, logging, caching).
10. **Thin controllers** — HTTP translation only; testable application layer.
11. **Business rules location** — Domain entities + handlers; not controllers/DTOs.
12. **DI resolves handlers** — Assembly scan registers `IRequestHandler<,>` implementations.
13. **MediatR finds handlers** — Matches `request.GetType()` to handler generic args.
14. **Exception propagation** — Uncaught in handler → middleware → HTTP status.
15. **CQRS with EF Core** — Same DbContext; different queries/commands optimized separately.
16. **Separate databases?** — Not required; useful at scale for read replicas.
17. **Separate APIs?** — Not required; can split microservices later.
18. **Overengineering when** — Simple CRUD, small team, no read/write divergence.
19. **Real value when** — Complex workflows, validation pipelines, evolving read models.
20. **Simple CRUD better when** — Prototype, internal tool, <5 endpoints, no business rules.

---

## 22. Final Architecture Summary

```
┌─────────────┐     ┌──────────────────┐     ┌─────────────────────┐     ┌────────────┐
│  API Layer  │────▶│  MediatR Pipeline │────▶│  Application Layer  │────▶│ SQL Server │
│  Controller │     │  Validation/Perf  │     │  Commands / Queries │     │  Orders    │
└─────────────┘     └──────────────────┘     └─────────────────────┘     └────────────┘
       │                                              │
       ▼                                              ▼
 ExceptionMiddleware                            Domain Entities
```

**Packages (purpose):**
- `MediatR` — request dispatch
- `FluentValidation` — input validation
- `EF Core SqlServer` — ORM persistence

**Verified:** 10/10 unit tests pass. API running at `http://localhost:5162` with full CRUD demonstrated against SQL Server 2022.
