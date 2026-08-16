# Mini-MES Codex Guide

## Project

Mini-MES backend built with:

- ASP.NET Core
- Entity Framework Core
- OPC UA
- SignalR
- Service / Repository pattern

This is a portfolio-scale MES project.
Prefer simple and maintainable solutions over enterprise-level complexity.

---

## Architecture

Follow the existing architecture:

Controller
→ Service
→ Repository
→ EF Core

Responsibilities:

- Controller: HTTP request / response
- Service: business logic and use cases
- Repository: data access and queries

Do not move business logic into Controllers or Repositories.

Avoid unnecessary architectural complexity such as:

- CQRS
- MediatR
- Microservices
- Event Sourcing
- Excessive interfaces or abstractions

unless explicitly requested.

---

## Service Responsibilities

- AuthService: authentication, JWT, refresh tokens
- UserService: user management
- ProductionService: production workflow orchestration
- WorkOrderService: work order lifecycle
- LotService: LOT lifecycle and process movement
- PerformanceService: production performance
- InventoryService: inventory consumption / receipt
- EquipmentService: equipment state, downtime and telemetry
- DailyEquipmentProductionService: equipment production aggregation

ProductionService should primarily coordinate other services rather than contain detailed domain logic.

---

## OPC Architecture

Expected flow:

OPC UA Server
→ OpcUaService
→ OpcUaBackgroundService
→ OpcEventService
→ Application Services

Responsibilities:

### OpcUaService
- Connection
- Session
- Subscription
- Raw tag reception

### OpcUaBackgroundService
- Start / stop OPC connection
- Receive OPC events
- Concurrency / throttling
- Create DI scope
- Dispatch events

### OpcEventService
- Interpret OPC tags
- Route events to the appropriate Services

Do not implement production, inventory, LOT, WorkOrder, or analytics business logic directly inside the OPC layer.

Reuse existing Services.

Current demo tags:

- Counter → production event
- Sinusoid → temperature / telemetry
- Square → equipment status

`CNC01` and similar mappings are currently demo-only.

Future OPC design should support multiple equipment without hardcoded mappings.

---

## Repository / EF Core

Use GenericRepository for simple CRUD.

Use dedicated repository methods for complex or repeated domain queries.

Avoid:

- unnecessary full-table loading
- N+1 queries
- duplicated queries across Services
- sync-over-async
- unnecessary tracking

Prefer `AsNoTracking()` for read-only queries.

Do not introduce a large UnitOfWork refactor unless explicitly requested.

Be careful with SaveChanges and transaction boundaries when one operation modifies multiple domains.

---

## SignalR

SignalR is used for real-time UI updates.

Avoid adding duplicated `IHubContext` broadcasting logic across many Services.

If broadcasting becomes significantly duplicated, suggest a dedicated realtime/notification abstraction before implementing it.

---

## Refactoring Rules

When working on existing code:

1. Inspect relevant code before changing it.
2. Preserve existing behavior unless behavior changes are requested.
3. Make small and focused changes.
4. Do not refactor unrelated files.
5. Reuse existing Services instead of duplicating business logic.
6. Distinguish actual bugs from optional improvements.
7. Avoid overengineering.
8. Run `dotnet build` after changes when possible.
9. Run tests when relevant tests exist.

When reviewing code, prioritize findings as:

- Critical
- High
- Medium
- Low

Reference actual files/classes/methods when reporting problems.

---

## Efficiency

Keep Codex work focused and token-efficient.

- Keep responses concise.
- Do not provide long explanations unless requested.
- Do not reproduce unchanged code.
- Do not output entire files unless requested.
- Limit code reviews to the most important findings by default.
- Do not scan unrelated areas when the requested scope is clear.
- Avoid repeating analysis already established in the current task.
- Prefer summaries over large code blocks.

After modifying code, normally report only:

- Changed files
- Key changes
- Build / test result
- Important remaining issues