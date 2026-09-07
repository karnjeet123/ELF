namespace Elf.Brewery.Application.Dtos;
public sealed record LoginRequest(
    string Username,
    string Password
);
