
namespace Elf.Brewery.Application.Dtos;

public sealed record BreweryDto(
   string Id,
    string Name,
    string? City,
    string? State,
    string? Country,
    string? Phone,
    string? WebsiteUrl,
    string? BreweryType,
    string? Street,
    double? Latitude,
    double? Longitude,
    double? DistanceKM
);