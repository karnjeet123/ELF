public sealed class Brewery
{
    public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
    public string? BreweryType { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? WebsiteUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTimeOffset LastRefreshedUtc { get; set; }
}