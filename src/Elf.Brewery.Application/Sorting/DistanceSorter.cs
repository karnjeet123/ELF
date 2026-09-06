public sealed class DistanceSorter : IBrewerySorter
{
    public BrewerySortField Field => BrewerySortField.Distance;

    public IEnumerable<Brewery> Sort(IEnumerable<Brewery> source, BreweryQuery query)
    {
        if(query.Latitude.HasValue && query.Longitude.HasValue)
        {
            var origin=new GeoCoordinate(query.Latitude!.Value, query.Longitude!.Value);  
            var withDistance=source.Where(b=>b.Latitude.HasValue && b.Longitude.HasValue)
            .Select(b => (Brewery: b, KM: origin.DistanceKmTo(b.Latitude!.Value, b.Longitude!.Value)));
             return query.Direction == SortDirection.Asc
            ? withDistance.OrderBy(b => b.KM).Select(b => b.Brewery)
            : withDistance.OrderByDescending(b => b.KM).Select(b => b.Brewery);
            
        }
        else
        {
             throw new ValidationException("latitude and longitude are required when sortBy=distance");

        }
       
    }
}