using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Dtos;
using Elf.Brewery.Infrastructure.Options;

namespace Elf.Brewery.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly StaticUserOptions _userOptions;
    public JwtTokenService(IOptions<JwtOptions> jwtOptions, IOptions<StaticUserOptions> userOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _userOptions = userOptions.Value;
    }



    public TokenResponse CreateToken(string username, IEnumerable<string> roles)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, username),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (JwtRegisteredClaimNames.Name, username),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expires);

    }

    public bool ValidateCredentials(string username, string password)
    {
        // Both comparisons are evaluated (no short-circuit) so a wrong username and a wrong
        // password take the same path.
        var usernameMatches = FixedEquals(username, _userOptions.Username);
        var passwordMatches = FixedEquals(password, _userOptions.Password);

        return usernameMatches && passwordMatches;
    }

    /// <summary>
    /// Compares two strings in constant time. Both sides are hashed first because
    /// <see cref="CryptographicOperations.FixedTimeEquals"/> throws on a length mismatch,
    /// which would otherwise leak the expected length through timing and exceptions.
    /// SHA-256 always produces 32 bytes, so the comparison length never varies.
    /// </summary>
    private static bool FixedEquals(string? a, string? b)
    {
        var hashA = SHA256.HashData(Encoding.UTF8.GetBytes(a ?? string.Empty));
        var hashB = SHA256.HashData(Encoding.UTF8.GetBytes(b ?? string.Empty));

        return CryptographicOperations.FixedTimeEquals(hashA, hashB);
    }
}
