public sealed class CitySorter : IBrewerySorter
{
    public BrewerySortField Field => BrewerySortField.City;

    public IEnumerable<Brewery> Sort(IEnumerable<Brewery> source, BreweryQuery query)
    {
        return query.Direction == SortDirection.Asc
            ? source.OrderBy(b => b.City,StringComparer.OrdinalIgnoreCase)
            : source.OrderByDescending(b => b.City,StringComparer.OrdinalIgnoreCase);   
    }
}