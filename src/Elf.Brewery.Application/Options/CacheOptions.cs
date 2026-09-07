using System.ComponentModel.DataAnnotations;

namespace Elf.Brewery.Application.Options;

public sealed class CacheOptions
{
    [Range(1, int.MaxValue, ErrorMessage = "Cache:ExpirationMinutes must be at least 1.")]
    public int ExpirationMinutes { get; set; }
}