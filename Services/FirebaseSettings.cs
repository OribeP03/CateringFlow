// Services/FirebaseSettings.cs
using cateringflow.Models;

namespace cateringflow.Services;

public class FirebaseSettings
{
    // Web config values shown on your Firebase console:
    // Project Settings -> General -> Your apps -> Web app (SDK setup and configuration)
    public string? WebApiKey { get; set; }
    public string? AuthDomain { get; set; }
    public string? ProjectId { get; set; }
    public string? StorageBucket { get; set; }
    public string? MessagingSenderId { get; set; }
    public string? AppId { get; set; }
    public string? MeasurementId { get; set; }

    // Server-only credentials: relative path (from project root) to the service
    // account JSON downloaded from Project Settings -> Service accounts.
    public string? ServiceAccountPath { get; set; }

    // The permanent accounts seeded into Firebase Authentication on startup.
    // Each one is created only once (idempotent) and keeps a fixed role.
    public List<SeedAccount> SeedAccounts { get; set; } = new();

    // Extra emails allowed into the admin panels as SuperAdmin (e.g. Google sign-ins).
    public List<string> AdminEmails { get; set; } = new();

    // Role assigned to everyone else who signs in (Google users, etc.).
    public string DefaultRole { get; set; } = UserRoles.Customer;

    // True once Firebase has been initialized with a valid service account file.
    public bool IsConfigured { get; set; }

    // Maps a signed-in email to its RBAC role:
    // 1. Exact match in the seeded accounts -> that account's role
    // 2. Email in the AdminEmails allowlist -> SuperAdmin
    // 3. Anything else -> DefaultRole
    public string ResolveRole(string email)
    {
        foreach (var account in SeedAccounts)
        {
            if (!string.IsNullOrWhiteSpace(account.Email) &&
                string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(account.Role) ? UserRoles.SuperAdmin : account.Role;
            }
        }

        if (AdminEmails.Any(a => string.Equals(a?.Trim(), email, StringComparison.OrdinalIgnoreCase)))
        {
            return UserRoles.SuperAdmin;
        }

        return DefaultRole;
    }
}

public class SeedAccount
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string Role { get; set; } = UserRoles.SuperAdmin;
    public string? DisplayName { get; set; }
}