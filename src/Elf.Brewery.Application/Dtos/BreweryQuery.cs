namespace Elf.Brewery.Application.Dtos;
using System.ComponentModel.DataAnnotations;
using Elf.Brewery.Domain.Enums;

public sealed class BreweryQuery
{
    /// <summary>Free-text term matched against name, city, state and brewery type.</summary>
    public string? Search { get; init; }

    /// <summary>Exact city filter. Combines with <see cref="Search"/> as an AND condition.</summary>
    public string? City { get; init; }

    public BrewerySortField? SortField { get; init; } = BrewerySortField.Name;
    public SortDirection? Direction { get; init; } = SortDirection.Asc;

    [Range(-90, 90)]
    public double? Latitude { get; init; }

    [Range(-180, 180)]
    public double? Longitude { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}

