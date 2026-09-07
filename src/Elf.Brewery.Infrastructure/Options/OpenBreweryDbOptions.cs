using System.ComponentModel.DataAnnotations;

namespace Elf.Brewery.Infrastructure.Options;

public sealed class OpenBreweryDbOptions
{
    [Required]
    [Url(ErrorMessage = "OpenBreweryDb:BaseUrl must be a valid absolute URL.")]
    public string BaseUrl { get; init; } = "https://api.openbrewerydb.org/v1/";

    [Range(1, 200, ErrorMessage = "OpenBreweryDb:PerPage must be between 1 and 200.")]
    public int PerPage { get; init; } = 200;

    [Range(1, int.MaxValue, ErrorMessage = "OpenBreweryDb:MaxPages must be at least 1.")]
    public int MaxPages { get; init; } = 5;
}