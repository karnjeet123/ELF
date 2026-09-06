public sealed record LoginRequest(
    string Username,
    string Password
);  

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);