using Elf.Brewery.Application.Contracts;
using Elf.Brewery.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elf.Brewery.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ITokenService tokens;
    private readonly ILogger<AuthController> logger;

    public AuthController(ITokenService tokens, ILogger<AuthController> logger)
    {
        this.tokens = tokens;
        this.logger = logger;
    }

    [HttpPost("token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Token([FromBody] LoginRequest request)
    {
        if (!tokens.ValidateCredentials(request.Username, request.Password))
        {
            logger.LogWarning("Failed login for {User}", request.Username);
            return Unauthorized(new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = "Invalid username or password."     // deliberately vague
            });
        }
        // Single static user, so the role is fixed rather than looked up.
        return Ok(tokens.CreateToken(request.Username, new[] { "Admin" }));
    }
}
