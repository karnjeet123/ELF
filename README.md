# Elf Brewery API

A small ASP.NET Core 8 Web API that pulls brewery data from the public Open Brewery DB, stores it locally, and serves it back with search, sorting, paging and distance calculation.

## Running it

You need the .NET 8 SDK.

```
cd Elf.Brewery/src/Elf.Brewery.Api
dotnet run
```

The app starts on http://localhost:5214 and opens Swagger. The SQLite file (`brewery.db`) is created on first start if it isn't there yet.

Before the first request, set a JWT signing key. It is blank in `appsettings.json` on purpose so nobody commits a real one:

```
dotnet user-secrets set "Jwt:SigningKey" "some-long-random-string-at-least-32-chars"
```

## Getting a token

Every brewery endpoint needs a bearer token. There is one hardcoded user in `appsettings.json` under `StaticUser`.

```
POST /api/auth/token
{ "username": "admin@elfbeauty.com", "password": "admin@123" }
```

Paste the returned token into the Authorize box in Swagger.

## Endpoints

Both versions expose the same routes. Pick the version in the Swagger dropdown.

| Route | What it does |
|---|---|
| `GET /api/v{n}/breweries` | List with `search`, `sortField`, `direction`, `pageNumber`, `pageSize`, `latitude`, `longitude` |
| `GET /api/v{n}/breweries/{id}` | Single brewery, 404 if it doesn't exist |
| `GET /api/v{n}/breweries/autocomplete?term=&limit=` | Name suggestions, term needs at least 2 characters |
| `GET /api/v{n}/breweries/cities` | Distinct city names |
| `POST /api/v{n}/breweries/refresh` | Re-fetches everything from Open Brewery DB |

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

## Projects

```
Elf.Brewery.Domain          entities, enums, exceptions, GeoCoordinate
Elf.Brewery.Application     interfaces, DTOs, service, search, sorters
Elf.Brewery.Infrastructure  repositories, EF DbContext, HTTP client, JWT, caching
Elf.Brewery.Api             controllers, Swagger, middleware, startup
```

Dependencies point inwards. Domain knows about nothing, Application knows about Domain, Infrastructure and Api know about both.

A few things worth knowing:

- Sorting uses one class per sort field (`NameSorter`, `CitySorter`, `DistanceSorter`) picked by a factory, so adding a new sort field means adding a class, not editing a switch.
- Data goes through `BreweryDataFacade`, which caches the full list in memory for 10 minutes and uses a semaphore so a cold cache doesn't cause several parallel database reads.
- The Open Brewery DB client retries three times with backoff and trips a circuit breaker after five failures.
- Unhandled exceptions come back as ProblemDetails via `GlobalExceptionHandler`.
- Every request gets a correlation id, and logs go to the console and to `logs/BreweryLog-*.log`.

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
- No tests yet.
