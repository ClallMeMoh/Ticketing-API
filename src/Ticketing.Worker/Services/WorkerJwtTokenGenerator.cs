using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;

namespace Ticketing.Worker.Services;

public sealed class WorkerJwtTokenGenerator : IJwtTokenGenerator
{
    public string GenerateToken(AppUser user)
    {
        return string.Empty;
    }
}
