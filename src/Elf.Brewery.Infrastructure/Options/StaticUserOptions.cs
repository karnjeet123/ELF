using System.ComponentModel.DataAnnotations;

namespace Elf.Brewery.Infrastructure.Options;

public sealed class StaticUserOptions
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "StaticUser:PasswordHash must be supplied via user secrets or environment configuration.")]
    public string PasswordHash { get; init; } = string.Empty;
}