# Architecture & Design Notes

This document explains the architecture, design patterns, and trade-offs in the
Elf Brewery API — written to help prepare for a client-facing technical
round about this assignment.

## 1. High-level architecture: Clean Architecture (layered, dependency inversion)

The solution is split into four projects, each with a single responsibility.
Dependencies only point **inward** — outer layers know about inner layers,
never the other way round.

```
Elf.Brewery.Api            <-- Controllers, middleware, Swagger, startup wiring
Elf.Brewery.Infrastructure <-- EF Core, SQLite, HTTP client, JWT, caching
Elf.Brewery.Application    <-- Interfaces (contracts), DTOs, services, sorting, search
Elf.Brewery.Domain         <-- Entities, value objects, enums, exceptions (no dependencies)
```

- **Domain** has zero project references — it only contains `Brewery` (entity),
  `GeoCoordinate` (value object), enums, and custom exceptions. This keeps
  business concepts framework-agnostic and easy to unit test.
- **Application** depends only on Domain. It defines interfaces
  (`IBreweryService`, `IBreweryRepository`, `ICacheService`, etc.) and holds
  business logic that is independent of *how* data is stored or exposed.
- **Infrastructure** depends on Application + Domain, and implements the
  interfaces: EF Core/SQLite repository, in-memory repository, HTTP client for
  Open Brewery DB, JWT token service, memory cache wrapper.
- **Api** depends on all three. It only wires things together (DI, middleware,
  controllers) and has no business logic of its own.

**Why this matters (interview angle):** this is the Dependency Inversion
Principle (the "D" in SOLID) — controllers and business logic depend on
abstractions (`IBreweryRepository`), not concrete implementations
(`SqliteBreweryRepository`). You can swap SQLite for another store, or the
Open Brewery DB HTTP client for a mock, without touching business logic.

## 2. Design patterns used

### a) Repository pattern
`IBreweryRepository` abstracts data access. Two implementations exist:
- `SqliteBreweryRepository` (EF Core + SQLite, persists across restarts)
- `InMemoryBreweryRepository` (dictionary-backed, resets on every run)

### b) Keyed dependency injection (strategy-like selection at resolve-time)
Both repository implementations, services, and facades are registered under
named keys using .NET 8's **keyed services**:

```csharp
services.AddKeyedScoped<IBreweryRepository, SqliteBreweryRepository>(BreweryStorageKeys.Sqlite);
services.AddKeyedSingleton<IBreweryRepository, InMemoryBreweryRepository>(BreweryStorageKeys.InMemory);
```

Controllers resolve a specific key via `[FromKeyedServices(...)]`, and that
same key is threaded through every layer using the `[ServiceKey]` attribute
(introduced in .NET 8) — `BreweryService` and `BreweryDataFacade` both accept
`[ServiceKey] string storageKey` in their constructor and use it to pull their
own keyed dependency from `IServiceProvider`. This lets `v1` (SQLite) and
`v2` (in-memory) run the *exact same code* against different backing stores,
without `if/else` branching or duplicated services.

### c) Facade pattern
`BreweryDataFacade` sits between the service layer and the repository. It is
responsible for:
- Reading through a cache first (`ICacheService`)
- Falling back to the repository on cache miss
- Refreshing data from the external provider and invalidating the cache

This isolates caching/refresh concerns from `BreweryService`, which only deals
with querying/paging/sorting.

### d) Strategy pattern (pluggable sorting)
Sorting is implemented as one class per sortable field:
`NameSorter`, `CitySorter`, `DistanceSorter`, all implementing `IBrewerySorter`
(exposing a `Field` and a `Sort(...)` method). `BrewerySorterFactory` builds a
dictionary of `BrewerySortField -> IBrewerySorter` at startup (via DI's
`IEnumerable<IBrewerySorter>` auto-collection) and resolves the correct
strategy at request time:

```csharp
_sorters = sorters.ToDictionary(s => s.Field);
...
public IBrewerySorter Resolve(BrewerySortField field) => _sorters[field];
```

**Why this matters:** adding a new sort field means adding a new class that
implements the interface — the factory and DI container pick it up
automatically. No existing code (switch statements, if-chains) needs editing,
which satisfies the Open/Closed Principle.

### e) Factory pattern
`BrewerySorterFactory` is a textbook factory — it decouples "which sorter to
use" (decided by `BreweryQuery.SortField`) from "how sorting is performed"
(delegated to the resolved `IBrewerySorter`).

### f) Cache-aside pattern with double-checked locking
`BreweryDataFacade.GetAllAsync`:
1. Try cache first.
2. On miss, acquire a `SemaphoreSlim` gate (shared across requests).
3. Re-check the cache after acquiring the lock (in case another request
   already populated it while this one was waiting).
4. Only then hit the repository, then populate the cache.

This avoids a **cache stampede** (many concurrent requests all hitting the
database when the cache is cold) while still allowing full concurrency once
the cache is warm (the semaphore is only touched on a miss).

Cache keys are namespaced per storage key
(`"breweries:all:{storageKey}"`), so v1 and v2 never share cached data.

### g) Circuit breaker + retry (resilience pattern via Polly)
The HTTP client for Open Brewery DB is configured with:
```csharp
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, n => TimeSpan.FromMilliseconds(300 * Math.Pow(2, n))))
.AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```
- Retries transient failures 3 times with exponential backoff (300ms, 600ms, 1200ms).
- Trips a circuit breaker after 5 consecutive failures, breaking the circuit
  for 30 seconds (fails fast instead of hammering a downed upstream API).

### h) Global exception handling (chain of responsibility-ish, centralized mapping)
`GlobalExceptionHandler` implements ASP.NET Core's `IExceptionHandler` and
maps domain/application exceptions to HTTP status codes + RFC 7807
`ProblemDetails` responses:

| Exception | Status |
|---|---|
| `BreweryNotFoundException` | 404 |
| `ValidationException` | 400 |
| `ExternalServiceException` | 502 |
| `OperationCanceledException` | 499 (client closed request) |
| anything else | 500 |

This keeps controllers free of try/catch blocks — exceptions bubble up and
are translated in exactly one place.

### i) Middleware pattern
`CorrelationIdMiddleware` attaches/propagates a correlation ID per request,
useful for tracing a request across logs (Serilog writes to console and to
rolling daily files `logs/BreweryLog-*.log`).

### j) Mapper pattern
`BreweryMapper` implements two interfaces — `IBrewerySourceMapper` (external
API DTO -> domain entity) and `IBreweryDtoMapper` (domain entity -> API DTO).
Keeps translation logic out of services/controllers.

### k) Value Object pattern
`GeoCoordinate` is a `readonly record struct` encapsulating latitude/longitude
and the haversine distance calculation (`DistanceKmTo`). Immutable, equality
by value, no identity — classic DDD value object.

### l) Options pattern
Strongly typed configuration via `IOptions<T>`:
`CacheOptions`, `JwtOptions`, `OpenBreweryDbOptions`, `StaticUserOptions`,
bound from `appsettings.json` sections in `Program.cs`.

## 3. Cross-cutting concerns

| Concern | How it's handled |
|---|---|
| Authentication | JWT Bearer tokens (`JwtTokenService`), single hardcoded user from config for the assignment |
| Authorization | `[Authorize]` on brewery endpoints, all requiring a valid bearer token |
| API versioning | `Asp.Versioning` package; v1 and v2 share controllers logic but resolve different keyed services |
| Logging | Serilog, structured logs to console + rolling file, enriched with correlation ID |
| Error handling | Centralized via `GlobalExceptionHandler` + `ProblemDetails` |
| Health checks | `/health` endpoint via `AddHealthChecks()` |
| API docs | Swagger/Swashbuckle, one doc per API version |
| Resilience | Polly retry + circuit breaker on the external HTTP client |
| Caching | In-memory (`IMemoryCache`), 10-minute absolute expiration, high priority |

## 4. Data flow (typical GET /breweries request)

```
Controller (BreweriesController / BreweriesV2Controller)
  -> resolves IBreweryService via [FromKeyedServices]
  -> BreweryService.GetBreweriesAsync(query)
       -> BreweryDataFacade.GetAllAsync()   // cache-aside + semaphore gate
            -> ICacheService (hit?) 
            -> IBreweryRepository (miss -> Sqlite or InMemory)
       -> IBrewerySearchService.Filter(all, query.Search)
       -> IBrewerySorterFactory.Resolve(field).Sort(filtered, query)
       -> Skip/Take for paging
       -> IBreweryDtoMapper.ToDto(...) (+ optional distance calc via GeoCoordinate)
  <- PagedResult<BreweryDto>
```

## 5. Possible interview questions & answers

**Q: Why did you choose a layered/clean architecture instead of a simpler
single-project setup?**
A: It enforces separation of concerns and dependency inversion — the Domain
and Application layers have no knowledge of EF Core, HTTP, or ASP.NET Core.
This makes business logic unit-testable in isolation and makes it easy to
swap infrastructure (e.g., replace SQLite with Postgres, or Polly with
another resilience library) without touching business rules.

**Q: How do v1 and v2 endpoints share the same code but use different storage?**
A: Through .NET 8 keyed DI. Both the SQLite and in-memory repositories (and
the services/facades that consume them) are registered under distinct keys
(`BreweryStorageKeys.Sqlite` / `.InMemory`). The versioned controllers each
request their own keyed `IBreweryService` instance. Internally the key is
propagated via the `[ServiceKey]` attribute so each layer resolves its own
correctly-keyed dependency. This avoids duplicating any business logic.

**Q: What happens if two requests hit a cold cache at the same time?**
A: `BreweryDataFacade` uses a shared static `SemaphoreSlim` gate. The first
request acquires the lock and repopulates the cache; any other concurrent
request blocks on the semaphore, and after acquiring it re-checks the cache
(double-checked locking) — so only one request ever hits the repository, and
the rest get the freshly cached result instead of a duplicate DB read.

**Q: How is data kept up to date?**
A: It isn't automatic — a client calls `POST /breweries/refresh`, which
triggers `BreweryDataFacade.RefreshFromExternalAsync`. This fetches
everything from Open Brewery DB via `IBreweryProvider`, upserts it into the
repository, stamps `LastRefreshedUtc`, and invalidates the cache so the next
read gets fresh data. Nothing is stored until refresh is called at least once.

**Q: How would you add a new sortable field, e.g. sort by state?**
A: Create a new class (e.g., `StateSorter`) implementing `IBrewerySorter`
with `Field = BrewerySortField.State`, register it in DI (or rely on
assembly scanning if configured), add the enum value. The factory
automatically picks it up via `IEnumerable<IBrewerySorter>` — no other code
needs modification. This is the Open/Closed Principle in practice.

**Q: How is resilience against the external Open Brewery DB API handled?**
A: Via Polly policies attached to the typed `HttpClient`: 3 retries with
exponential backoff for transient errors, and a circuit breaker that opens
after 5 consecutive failures for 30 seconds, preventing the app from
repeatedly calling a downed upstream service.

**Q: How are errors surfaced to API consumers?**
A: A single `GlobalExceptionHandler` implementing `IExceptionHandler` maps
known exception types (`BreweryNotFoundException` -> 404, `ValidationException`
-> 400, `ExternalServiceException` -> 502, everything else -> 500) into RFC
7807-compliant `ProblemDetails` responses, including a `traceId` for
correlating with logs.

**Q: How is distance calculated and why a value object?**
A: `GeoCoordinate` is an immutable `readonly record struct` with a
`DistanceKmTo` method implementing the haversine formula. Wrapping this in a
value object keeps the math out of services/controllers, gives free
value-based equality, and prevents accidental mutation.

**Q: Why keyed singleton for in-memory repository but keyed scoped for SQLite?**
A: The in-memory repository must be a singleton — its dictionary needs to
persist across requests within the app's lifetime (since there's no real
database backing it). The SQLite repository is scoped because it wraps an
`EF Core DbContext`, which is not thread-safe and should be one-per-request
(the standard scoped lifetime for `DbContext`).

**Q: What are the known limitations, and how would you improve this for
production?**
A:
- Single hardcoded user / no refresh tokens — would replace with a real
  identity provider (e.g., ASP.NET Core Identity, Azure AD, or IdentityServer/
  Duende) supporting multiple users and refresh token rotation.
- `EnsureCreated()` instead of EF Core migrations — would switch to proper
  migrations for schema versioning in production.
- No automated tests — would add unit tests for services/sorters/mappers and
  integration tests for controllers (e.g., using `WebApplicationFactory`).
- In-memory cache is single-instance — would move to a distributed cache
  (Redis) if scaling out to multiple instances.
- No rate limiting on the refresh endpoint, which calls an external API —
  would add throttling to avoid abuse.

**Q: Why use a facade between the service and the repository instead of
calling the repository directly from the service?**
A: To keep caching/refresh/locking concerns separate from query/business
logic (filtering, sorting, paging) in `BreweryService`. This follows the
Single Responsibility Principle — `BreweryService` only orchestrates
search/sort/paging/mapping, while `BreweryDataFacade` owns "how do I get the
full list efficiently and safely."

**Q: How is API versioning implemented, and why two versions with identical
logic?**
A: Via the `Asp.Versioning` package (`AddApiVersioning` + `AddApiExplorer`),
producing separate Swagger docs per version. v1 and v2 share the exact same
controller/service code — the *only* difference is which keyed repository
(SQLite vs in-memory) gets resolved, demonstrating the keyed-DI pattern
described above rather than maintaining duplicate business logic.

## 6. Project reference summary

```
Elf.Brewery.Domain          entities, enums, exceptions, GeoCoordinate (no deps)
Elf.Brewery.Application     interfaces, DTOs, service, search, sorters (-> Domain)
Elf.Brewery.Infrastructure  repositories, EF DbContext, HTTP client, JWT, caching (-> Application, Domain)
Elf.Brewery.Api             controllers, Swagger, middleware, startup (-> all)
```
