public sealed class BrewerySearchService: IBrewerySearchService
{
    public IEnumerable<Brewery> Filter(IEnumerable<Brewery> source, string? term )
    {
       if (string.IsNullOrWhiteSpace(term))
       {
           return source;
       }
       term=term.Trim();
       return source.Where(b => 
    (b.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
    || (b.City?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
    || (b.State?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
    || (b.BreweryType?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    public IReadOnlyList<AutocompleteItemDto> Suggest(IEnumerable<Brewery> source, string? term,int limit)
    {
         if (string.IsNullOrWhiteSpace(term))
       {
           return Array.Empty<AutocompleteItemDto>();   
       }
       term=term.Trim();
       return source.Where(b=>b.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    .OrderByDescending(b => b.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase)) 
                    .ThenBy(b => b.Name.Length)
                    .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                    .Take(limit)
                    .Select(b => new AutocompleteItemDto(b.Id, b.Name, b.City))
                    .ToList();
    }
}