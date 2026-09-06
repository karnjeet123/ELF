using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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


        return FixedEquals(username, _userOptions.Username)
            && FixedEquals(password, _userOptions.Password);
    }

    private static bool FixedEquals(string a, string b)
    => CryptographicOperations.FixedTimeEquals(
           Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

}
