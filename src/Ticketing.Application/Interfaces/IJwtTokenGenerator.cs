using Ticketing.Domain.Entities;

namespace Ticketing.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(AppUser user);
}
