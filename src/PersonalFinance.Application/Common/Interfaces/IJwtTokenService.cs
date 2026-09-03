using PersonalFinance.Domain.Entities;

namespace PersonalFinance.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);

    (string Token, DateTime ExpiresAt) GenerateRefreshToken();
}
