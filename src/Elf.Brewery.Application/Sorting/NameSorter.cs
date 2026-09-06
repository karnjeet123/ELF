public sealed class NameSorter : IBrewerySorter
{
    public BrewerySortField Field => BrewerySortField.Name;

    public IEnumerable<Brewery> Sort(IEnumerable<Brewery> source, BreweryQuery query)
    {
        return query.Direction == SortDirection.Asc
            ? source.OrderBy(b => b.Name,StringComparer.OrdinalIgnoreCase)
            : source.OrderByDescending(b => b.Name,StringComparer.OrdinalIgnoreCase);
    }
}