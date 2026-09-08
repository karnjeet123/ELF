using DomainBrewery = Elf.Brewery.Domain.Entities.Brewery;
using System;
using System.Collections.Generic;
using System.Linq;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Dtos;

namespace Elf.Brewery.Application.Search;

public sealed class BrewerySearchService : IBrewerySearchService
{
    public IEnumerable<DomainBrewery> Filter(IEnumerable<DomainBrewery> source, string? term, string? city = null)
    {
        var result = source;

        if (!string.IsNullOrWhiteSpace(city))
        {
            var trimmedCity = city.Trim();
            result = result.Where(b =>
                string.Equals(b.City, trimmedCity, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var trimmedTerm = term.Trim();
            result = result.Where(b =>
                (b.Name?.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                || (b.City?.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                || (b.State?.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                || (b.BreweryType?.Contains(trimmedTerm, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return result;
    }

    public IReadOnlyList<AutocompleteItemDto> Suggest(IEnumerable<DomainBrewery> source, string? term, int limit)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Array.Empty<AutocompleteItemDto>();
        }
        term = term.Trim();
        return source.Where(b => b.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                     .OrderByDescending(b => b.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                     .ThenBy(b => b.Name.Length)
                     .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                     .Take(limit)
                     .Select(b => new AutocompleteItemDto(b.Id, b.Name, b.City))
                     .ToList();
    }
}