# Elf Brewery API

A small ASP.NET Core 8 Web API that pulls brewery data from the public Open Brewery DB, stores it locally, and serves it back with search, sorting, paging and distance calculation.

## What this app actually does, in plain words

Think of it as a middleman between the public Open Brewery DB and your own client app:

1. You call `POST /api/v{n}/breweries/refresh` once. The API goes out to Open Brewery DB, pages through the results, and saves them locally (either to a SQLite file or to an in-memory dictionary, depending on which version you hit).
2. From then on, every other request (`GET /breweries`, `/breweries/{id}`, `/breweries/autocomplete`, `/breweries/cities`) reads from your local copy, not from the internet. That's why it's fast and doesn't fall over if Open Brewery DB is slow or down.
3. Everything except login requires a bearer token, so you log in once, get a token, and reuse it.
4. If you tell it where you are (`latitude`/`longitude`), it works out how far away each brewery is and can sort by that.

That's the whole idea: fetch once, cache it, serve it fast, with the usual production concerns (auth, retries, logging, validation) bolted on around that core loop.

## Running it

You need the .NET 8 SDK.

```
cd Elf.Brewery/src/Elf.Brewery.Api
dotnet run
```

The app starts on http://localhost:5214 and opens Swagger. The SQLite file (`brewery.db`) is created on first start if it isn't there yet.

Before the first request, set the JWT signing key and the login password. Both are kept out of `appsettings.json` on purpose so nobody commits a real secret:

```
cd src/Elf.Brewery.Api
dotnet user-secrets set "Jwt:SigningKey" "some-long-random-string-at-least-32-chars"
dotnet user-secrets set "StaticUser:Password" "admin@123"
```

The app fails fast at startup with a clear message if either is missing.

## Getting a token

Every brewery endpoint needs a bearer token. There is one hardcoded user: the username lives in `appsettings.json` under `StaticUser:Username`, and the password comes from user secrets (set it as shown above).

```
POST /api/auth/token
{ "username": "admin@elfbeauty.com", "password": "admin@123" }
```

Paste the returned token into the Authorize box in Swagger.

## Endpoints

Both versions expose the same routes. Pick the version in the Swagger dropdown.

| Route | What it does |
|---|---|
| `GET /api/v{n}/breweries` | List with `search`, `city`, `sortField`, `direction`, `pageNumber`, `pageSize`, `latitude`, `longitude` |
| `GET /api/v{n}/breweries/{id}` | Single brewery, 404 if it doesn't exist |
| `GET /api/v{n}/breweries/autocomplete?term=&limit=` | Name suggestions, term needs at least 2 characters |
| `GET /api/v{n}/breweries/cities` | Distinct city names |
| `POST /api/v{n}/breweries/refresh` | Re-fetches everything from Open Brewery DB |

`search` is a free-text term matched against name, city, state and brewery type. `city` is an exact-match filter. They combine as an AND, so `?search=brewing&city=Portland` returns Portland breweries whose details mention "brewing". `pageSize` is capped at 100.

If you pass `latitude` and `longitude`, each result gets a `distanceKm` and you can sort by distance. Leave them out and that field comes back null.

Nothing is stored until you call `refresh` at least once.

## The two API versions

v1 and v2 run the exact same code. The only difference is where the data lives:

- **v1** reads and writes SQLite (`brewery.db`), so data survives a restart.
- **v2** keeps everything in a dictionary in memory, so it starts empty every time you run the app.

This is wired up with keyed DI. Both repositories are registered under a key:

```csharp
services.AddKeyedScoped<IBreweryRepository, SqliteBreweryRepository>(BreweryStorageKeys.Sqlite);
services.AddKeyedSingleton<IBreweryRepository, InMemoryBreweryRepository>(BreweryStorageKeys.InMemory);
```

The service and the data facade are registered twice as well, once per key. Each one receives the key it was resolved under through `[ServiceKey]` and uses that same key to pull its own dependency. The controllers start the chain:

```csharp
public BreweriesV2Controller([FromKeyedServices(BreweryStorageKeys.InMemory)] IBreweryService service, ...)
```

So the key travels controller -> service -> facade -> repository. The two versions never share a cache entry either, since the cache key includes the storage key.

## Architecture

The solution is split into four layers, each its own project, and each only allowed to depend on the ones "below" it:

```
Elf.Brewery.Domain          entities, enums, exceptions, GeoCoordinate
        ↑
Elf.Brewery.Application     interfaces, DTOs, service, search, sorters
        ↑
Elf.Brewery.Infrastructure  repositories, EF DbContext, HTTP client, JWT, caching
        ↑
Elf.Brewery.Api             controllers, Swagger, middleware, startup
```

**Why split it like this?** So the "core" of the app (what a brewery is, how distance is calculated, what counts as valid input) doesn't know or care whether the data comes from SQLite, an in-memory dictionary, or the internet. That means:
- `Domain` is the plainest layer — just the `Brewery` entity, enums like sort field/direction, custom exceptions, and the `GeoCoordinate` distance-calculation value object. It has zero dependencies on anything else in the solution.
- `Application` is the "business logic" — it defines interfaces like `IBreweryService`/`IBreweryRepository`/`IBrewerySorter` and the concrete logic that uses them (searching, sorting, paging, mapping to DTOs). It depends only on `Domain`.
- `Infrastructure` is where the real-world plumbing lives — the actual SQLite repository, the in-memory repository, the HTTP client that talks to Open Brewery DB, JWT token generation, and the memory cache. It implements the interfaces `Application` defined.
- `Api` is the outermost layer — HTTP controllers, Swagger setup, exception handling middleware, and `Program.cs` wiring everything together with dependency injection.

Because `Application` only knows about *interfaces*, not concrete classes, you could swap SQLite for a totally different database without touching a single line of business logic — you'd just write a new class in `Infrastructure` that implements `IBreweryRepository`.

### The two API versions (v1 vs v2)

v1 and v2 run the exact same controller/service code. The only difference is where the data lives:

- **v1** reads and writes SQLite (`brewery.db`), so data survives a restart.
- **v2** keeps everything in a dictionary in memory, so it starts empty every time you run the app.

This is wired up with keyed DI. Both repositories are registered under a key:

```csharp
services.AddKeyedScoped<IBreweryRepository, SqliteBreweryRepository>(BreweryStorageKeys.Sqlite);
services.AddKeyedSingleton<IBreweryRepository, InMemoryBreweryRepository>(BreweryStorageKeys.InMemory);
```

The service and the data facade are registered twice as well, once per key. Each one receives the key it was resolved under through `[ServiceKey]` and uses that same key to pull its own dependency. The controllers start the chain:

```csharp
public BreweriesV2Controller([FromKeyedServices(BreweryStorageKeys.InMemory)] IBreweryService service, ...)
```

So the key travels controller → service → facade → repository. The two versions never share a cache entry either, since the cache key includes the storage key.

### A few design choices worth knowing

- **Sorting** uses one class per sort field (`NameSorter`, `CitySorter`, `DistanceSorter`), picked at runtime by a factory that resolves the right one from keyed DI. Adding a new sort field means adding a new class, not editing a big switch statement.
- **Caching**: data goes through `BreweryDataFacade`, which caches the full brewery list in memory for 10 minutes (configurable) and uses a semaphore so a cold cache doesn't trigger several parallel database reads at once (the classic "cache stampede" problem).
- **Resilience**: the Open Brewery DB HTTP client retries three times with exponential backoff and trips a circuit breaker after five failures, so a flaky upstream API doesn't take the whole app down with it.
- **Error handling**: unhandled exceptions come back as a standard `ProblemDetails` JSON response via a single `GlobalExceptionHandler`, so every error looks the same shape to a client, and internal exception details never leak out in a 500 response.
- **Config validation**: settings like the JWT signing key or the Open Brewery DB URL are validated at startup (`ValidateOnStart()`), so a bad config fails immediately with a clear error instead of failing later at some random request.
- **Observability**: every request gets a correlation id (so you can trace one request through the logs), and logs go to both the console and a rolling daily file under `logs/`.

## Testing

There are two test projects under `tests/`:

- **`Elf.Brewery.Application.Tests`** — unit tests with xUnit + Moq. Covers the pure logic: distance calculation, each sorter, the sorter factory, `BreweryService`, and the global exception handler's status-code mapping. No network, no database, no web server involved.
- **`Elf.Brewery.Api.IntegrationTests`** — integration tests using `WebApplicationFactory<Program>`, which boots the real app (real DI, real middleware, real auth) in-memory against an isolated temp SQLite file. Covers login, the auth-required 401 case, listing breweries, 404 on unknown id, and basic validation errors — all through real HTTP calls, no mocking of the pipeline itself.

Run everything with:

```
dotnet test Elf.Brewery.sln
```

### Code coverage (project-wise)

Generated via `dotnet test --collect:"XPlat Code Coverage"` + `reportgenerator`. Overall line coverage is **64.8%** across 33 tests (25 unit + 8 integration). Breakdown by project:

| Project | Line coverage | Notes |
|---|---|---|
| `Elf.Brewery.Api` | 74% | Controllers hit via integration tests; `BreweriesController` (v1/Sqlite) is 0% since tests exercise v2 (in-memory) only; Swagger filter classes untested (cosmetic, not logic) |
| `Elf.Brewery.Application` | 73.3% | Sorters, factory, and `BreweryService` well covered; `BrewerySearchService` (20.8%) and DTO mapping paths are the main gaps |
| `Elf.Brewery.Domain` | 69.2% | `GeoCoordinate` and exceptions at 100%; `Brewery` entity itself is mostly just properties (38.4%, largely auto-property getters/setters that don't need explicit tests) |
| `Elf.Brewery.Infrastructure` | 47.3% | JWT, options, EF `DbContext`, and cache service well covered; `SqliteBreweryRepository` (0%) and `BreweryMapper` (0%) aren't exercised yet — the SQLite path is only reachable via `BreweriesController` (v1), which the integration tests don't currently target |

To regenerate this locally:

```
dotnet test Elf.Brewery.sln --collect:"XPlat Code Coverage" --results-directory .\coverage-results
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"coverage-results\**\coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

Then open `coverage-report\index.html` for the full drill-down. Both `coverage-results/` and `coverage-report/` are gitignored since they're build artifacts.

## Configuration

| Key | Notes |
|---|---|
| `OpenBreweryDb:BaseUrl` | Upstream API |
| `OpenBreweryDb:PerPage` / `MaxPages` | Caps how much gets pulled on refresh |
| `Cache:ExpirationMinutes` | How long the brewery list stays cached |
| `ConnectionStrings:BreweryDb` | SQLite file |
| `Jwt:*` | Issuer, audience, signing key, expiry |
| `StaticUser:*` | The single login |

## Known gaps

- One hardcoded user and no refresh tokens. Fine for an assignment, not for anything real.
- The SQLite schema is created with `EnsureCreated`, so there are no migrations.
- No CI pipeline yet to run the tests automatically on push.
- `OpenBreweryDb:MaxPages` is set to 5 (with `PerPage: 200`), so a refresh pulls at most 1000 breweries rather than the full upstream dataset. This keeps the assignment fast to run and demo. A warning is logged when the cap is hit; raise `MaxPages` in `appsettings.json` to pull more.
