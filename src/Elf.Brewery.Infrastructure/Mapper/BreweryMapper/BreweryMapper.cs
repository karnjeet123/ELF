public sealed class BreweryMapper: IBrewerySourceMapper,IBreweryDtoMapper
{
    public Brewery ToDomain(BrewerySourceDto source)
    {
        return new Brewery
        {
            Id = source.Id,
            Name = source.Name?.Trim()??string.Empty,
            BreweryType = source.BreweryType,
            Street= source.Address1?.Trim()??string.Empty,
            City = source.City?.Trim()??string.Empty,
            State = source.StateProvince?.Trim()??string.Empty,
            Country = source.Country?.Trim()??string.Empty,
            PostalCode = source.PostalCode?.Trim()??string.Empty,
            Phone = FormatPhone(source.Phone?.Trim()??string.Empty),
            WebsiteUrl = source.WebsiteUrl?.Trim()??string.Empty,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            LastRefreshedUtc = DateTimeOffset.UtcNow
        };
    }

    public BreweryDto ToDto(Brewery brewery, double? distanceKm)
    {
        return new BreweryDto(
            brewery.Id,
            brewery.Name,
            brewery.City,
            brewery.State,
            brewery.Country,
            brewery.Phone,
            brewery.WebsiteUrl,
            brewery.BreweryType,
            brewery.Street,
            brewery.Latitude,
            brewery.Longitude,
            distanceKm);
    }

    private static string? FormatPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        return digits.Length == 10
            ? $"({digits[..3]}) {digits[3..6]}-{digits[6..]}"
            : phone;
    }

    
}