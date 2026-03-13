using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;

namespace Ticketing.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    private const string AdminEmail = "admin@test.com";
    private const string AdminPassword = "Admin123!";

    public async Task SeedAsync()
    {
        var existingAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == AdminEmail);

        if (existingAdmin is not null)
        {
            if (!_passwordHasher.Verify(AdminPassword, existingAdmin.PasswordHash))
            {
                existingAdmin.ResetPassword(_passwordHasher.Hash(AdminPassword));
                await _context.SaveChangesAsync();
                _logger.LogInformation("Reset password for seed admin user: {Email}", AdminEmail);
            }
        }
        else
        {
            var admin = new AppUser(
                "System Admin",
                AdminEmail,
                _passwordHasher.Hash(AdminPassword),
                UserRole.Admin);

            await _context.Users.AddAsync(admin);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded default admin user: {Email}", AdminEmail);
        }

        await SeedAgentProfilesAsync();
    }

    private async Task SeedAgentProfilesAsync()
    {
        if (await _context.AgentProfiles.AnyAsync())
            return;

        var agents = new[]
        {
            ("Alice Chen", "alice@test.com", 5),
            ("Bob Patel", "bob@test.com", 8),
            ("Carol Diaz", "carol@test.com", 3),
        };

        foreach (var (name, email, capacity) in agents)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                user = new AppUser(name, email, _passwordHasher.Hash("Agent123!"), UserRole.Agent);
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
            }

            await _context.AgentProfiles.AddAsync(new AgentProfile(user.Id, capacity));
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} agent profiles.", agents.Length);
    }
}
