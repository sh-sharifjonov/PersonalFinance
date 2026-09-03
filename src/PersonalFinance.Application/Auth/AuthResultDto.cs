namespace PersonalFinance.Application.Auth;

public record AuthResultDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
