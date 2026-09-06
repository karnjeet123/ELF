public sealed class BrewerySorterFactory:IBrewerySorterFactory
{
    private readonly Dictionary<BrewerySortField, IBrewerySorter> _sorters;

    public BrewerySorterFactory(IEnumerable<IBrewerySorter> sorters)
    {
        _sorters = sorters.ToDictionary(s => s.Field);
    }

    public IBrewerySorter Resolve(BrewerySortField field)
    {
        if (_sorters.TryGetValue(field, out var sorter))
        {
            return sorter;
        }
        throw new NotSupportedException($"Sorting by {field} is not supported.");
    }
}