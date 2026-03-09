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

            return;
        }

        var admin = new AppUser(
            "System Admin",
            AdminEmail,
            _passwordHasher.Hash(AdminPassword),
            UserRole.Admin);

        await _context.Users.AddAsync(admin);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded default admin user: {Email}", AdminEmail);
    }
}
