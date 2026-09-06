public interface IBrewerySorter
{
   
    BrewerySortField Field { get; }
    IEnumerable<Brewery> Sort(IEnumerable<Brewery> source, BreweryQuery query);
}

public interface IBrewerySorterFactory 
{
    IBrewerySorter Resolve(BrewerySortField field);
}

public interface IBrewerySearchService
{
    IEnumerable<Brewery> Filter(IEnumerable<Brewery> source, string? term);
    IReadOnlyList<AutocompleteItemDto> Suggest(IEnumerable<Brewery> source, string term, int limit);
}

public interface ITokenService
{
    bool ValidateCredentials(string username, string password);
    TokenResponse CreateToken(string username, IEnumerable<string> roles);
}

public interface IBreweryDtoMapper
{
    BreweryDto ToDto(Brewery brewery,double? distanceKm);
}

public interface IBreweryDataFacade
{
    
    Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken ct);

    Task<int> RefreshFromExternalAsync(CancellationToken ct);
}