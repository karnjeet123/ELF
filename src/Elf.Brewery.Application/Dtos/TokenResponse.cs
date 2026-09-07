namespace Elf.Brewery.Application.Dtos;
public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
