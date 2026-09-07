using Microsoft.Extensions.DependencyInjection;
using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Dtos;
using Elf.Brewery.Application.Search;
using Elf.Brewery.Domain.Enums;
using Elf.Brewery.Domain.Exceptions;
using Elf.Brewery.Domain.ValueObjects;


namespace Elf.Brewery.Application.Services;


public sealed class BreweryService : IBreweryService
{
    private readonly IBreweryDataFacade _breweries;
    private readonly IBrewerySearchService _search;
    private readonly IBrewerySorterFactory _sorterFactory;
    private readonly IBreweryDtoMapper _mapper;

    public BreweryService(
        [ServiceKey] string storageKey,
        IServiceProvider serviceProvider,
        IBrewerySearchService search,
        IBrewerySorterFactory sorterFactory,
        IBreweryDtoMapper mapper)
    {
        _breweries = serviceProvider.GetRequiredKeyedService<IBreweryDataFacade>(storageKey);
        _search = search;
        _sorterFactory = sorterFactory;
        _mapper = mapper;
    }

    public async Task<PagedResult<BreweryDto>> GetBreweriesAsync(BreweryQuery query, CancellationToken ct)
    {
        var all = await _breweries.GetAllAsync(ct);

        var filtered = _search.Filter(all, query.Search);

        var sortField = query.SortField ?? BrewerySortField.Name;
        var sorted = _sorterFactory.Resolve(sortField).Sort(filtered, query).ToList();

        var pageItems = sorted
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize);

        GeoCoordinate? origin = query.Latitude is not null && query.Longitude is not null
            ? new GeoCoordinate(query.Latitude.Value, query.Longitude.Value)
            : null;

        var items = pageItems
            .Select(b => _mapper.ToDto(b, Distance(origin, b)))
            .ToList();

        return new PagedResult<BreweryDto>(items, sorted.Count, query.PageNumber, query.PageSize);
    }

    public async Task<BreweryDto> GetBreweryByIdAsync(string id, CancellationToken ct)
    {
        var all = await _breweries.GetAllAsync(ct);

        var match = all.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            throw new BreweryNotFoundException(id);

        return _mapper.ToDto(match, null);
    }

    public async Task<IReadOnlyList<AutocompleteItemDto>> GetAutoCompleteAsync(string term, int limit, CancellationToken ct)
    {
        return _search.Suggest(await _breweries.GetAllAsync(ct), term, limit);
    }

    public async Task<IReadOnlyList<string>> GetCitiesAsync(CancellationToken ct)
    {
        return (await _breweries.GetAllAsync(ct))
            .Where(b => !string.IsNullOrWhiteSpace(b.City))
            .Select(b => b.City!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    public async Task<int> RefreshBreweriesAsync(CancellationToken ct)
    {
        return await _breweries.RefreshFromExternalAsync(ct);
    }

    private static double? Distance(GeoCoordinate? origin, DomainBrewery brewery)
    {
        if (origin is null || brewery.Latitude is null || brewery.Longitude is null)
            return null;

        var distanceKm = origin.Value.DistanceKmTo(brewery.Latitude.Value, brewery.Longitude.Value);

        return Math.Round(distanceKm, 2);
    }
}
