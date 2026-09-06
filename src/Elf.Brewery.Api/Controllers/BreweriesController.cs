using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/breweries")]
[Produces("application/json")]
public sealed class BreweriesController
    : ControllerBase
{
    
    private readonly IBreweryService service;
    private readonly ILogger<BreweriesController> logger;

    public BreweriesController(
        [FromKeyedServices(BreweryStorageKeys.Sqlite)] IBreweryService service,
        ILogger<BreweriesController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BreweryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get([FromQuery] BreweryQuery query, CancellationToken ct)
    {
        logger.LogInformation("List breweries search={Search} sortBy={SortBy} page={Page}",
            query.Search, query.SortField, query.PageNumber);

        return Ok(await service.GetBreweriesAsync(query, ct));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BreweryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
        {
            return Ok(await service.GetBreweryByIdAsync(id, ct));
        }

    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(IReadOnlyList<AutocompleteItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Autocomplete(
        [FromQuery, Required, MinLength(2)] string term,
        [FromQuery, Range(1, 25)] int limit = 10,
        CancellationToken ct = default)
    {
        return Ok(await service.GetAutoCompleteAsync(term, limit, ct));
    }
         

    [HttpGet("cities")]
    public async Task<IActionResult> Cities(CancellationToken ct)
    {
        return Ok(await service.GetCitiesAsync(ct));
    }
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        logger.LogInformation("Refreshing breweries from external provider");

        var count = await service.RefreshBreweriesAsync(ct);

        return Ok(new { refreshed = count, refreshedAtUtc = DateTimeOffset.UtcNow });
    }
}