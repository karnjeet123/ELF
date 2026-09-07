namespace Elf.Brewery.Application.Dtos;
using Elf.Brewery.Domain.Enums;

public sealed class BreweryQuery
{
    public string? Search { get; init; }
    public BrewerySortField? SortField { get; init; } = BrewerySortField.Name;
    public SortDirection? Direction { get; init; } = SortDirection.Asc;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;

}

