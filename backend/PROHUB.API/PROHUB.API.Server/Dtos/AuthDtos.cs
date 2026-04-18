namespace PROHUB.API.Server.Dtos;

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);

public record AuthUserDto(Guid Id, string Email, string Name, string Role);

public record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    AuthUserDto User,
    Guid? CompanyId
);

public record RefreshResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt
);
