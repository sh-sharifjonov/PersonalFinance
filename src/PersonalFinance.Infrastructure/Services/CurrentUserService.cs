using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PersonalFinance.Application.Common.Exceptions;
using PersonalFinance.Application.Common.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (value is null || !Guid.TryParse(value, out var userId))
            {
                throw new UnauthorizedException("User is not authenticated.");
            }

            return userId;
        }
    }
}
