using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
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

        return Ok(tokens.CreateToken(request.Username, new[] { "Admin" }));
    }
}
