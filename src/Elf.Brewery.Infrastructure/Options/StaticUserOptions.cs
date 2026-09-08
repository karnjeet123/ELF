using System.ComponentModel.DataAnnotations;

namespace Elf.Brewery.Infrastructure.Options;

public sealed class StaticUserOptions
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "StaticUser:Password must be supplied via user secrets or environment configuration.")]
    public string Password { get; init; } = string.Empty;
}