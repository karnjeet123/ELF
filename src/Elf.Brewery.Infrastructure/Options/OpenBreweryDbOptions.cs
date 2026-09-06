public sealed class OpenBreweryDbOptions
{
    public string BaseUrl { get; init; } = "https://api.openbrewerydb.org/v1/";
    public int PerPage { get; init; } = 200;
    public int MaxPages { get; init; } = 5;
}