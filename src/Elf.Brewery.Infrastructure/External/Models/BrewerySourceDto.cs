using System.Text.Json.Serialization;

public sealed class BrewerySourceDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("brewery_type")]
    public string BreweryType { get; set; } = default!;

    [JsonPropertyName("city")]
    public string City { get; set; } = default!;

    [JsonPropertyName("state_province")]
    public string StateProvince { get; set; } = default!;

    [JsonPropertyName("country")]
    public string Country { get; set; } = default!;

    [JsonPropertyName("website_url")]
    public string WebsiteUrl { get; set; } = default!;

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = default!;

    [JsonPropertyName("address_1")]
    public string Address1 { get; set; } = default!;

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; } = default!;
}