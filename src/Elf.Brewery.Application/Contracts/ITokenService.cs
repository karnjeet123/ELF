using Elf.Brewery.Application.Dtos;
namespace Elf.Brewery.Application.Contracts;

public interface ITokenService
{
    bool ValidateCredentials(string username, string password);
    TokenResponse CreateToken(string username, IEnumerable<string> roles);
}
