using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Guid UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string Role =>
        _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
