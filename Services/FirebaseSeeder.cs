// Services/FirebaseSeeder.cs
using FirebaseAdmin.Auth;

namespace cateringflow.Services;

public static class FirebaseSeeder
{
    // Creates the permanent accounts (from appsettings -> Firebase -> SeedAccounts)
    // in Firebase Authentication. Runs on every startup but is idempotent: accounts
    // are created only the first time, so existing credentials are never overwritten.
    public static async Task EnsureSeedAdminsAsync(FirebaseSettings settings, Action<string>? log = null)
    {
        var auth = FirebaseAuth.DefaultInstance;
        var created = 0;

        foreach (var account in settings.SeedAccounts)
        {
            if (account == null || string.IsNullOrWhiteSpace(account.Email) || string.IsNullOrWhiteSpace(account.Password))
            {
                continue;
            }

            try
            {
                await auth.CreateUserAsync(new UserRecordArgs
                {
                    Email = account.Email,
                    Password = account.Password,
                    EmailVerified = true,
                    DisplayName = string.IsNullOrWhiteSpace(account.DisplayName) ? account.Role : account.DisplayName,
                    Disabled = false
                });
                log?.Invoke($"Seeded account {account.Email} ({account.Role}) created in Firebase Auth.");
                created++;
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
            {
                log?.Invoke($"Seeded account {account.Email} already exists in Firebase Auth.");
            }
        }

        if (created == 0)
        {
            log?.Invoke("No new Firebase seed accounts were created.");
        }
    }
}