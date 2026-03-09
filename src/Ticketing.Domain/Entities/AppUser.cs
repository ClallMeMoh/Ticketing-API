using Ticketing.Domain.Common;
using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Entities;

public class AppUser : AuditableEntity
{
    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }

    private AppUser() { }

    public AppUser(string fullName, string email, string passwordHash, UserRole role = UserRole.User)
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void ResetPassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }
}
