using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

record AuthResult(Account? Account, int StatusCode = 200, string? Error = null);

sealed class AuthService(Store store, IPasswordHasher passwords)
{
    public static bool IsValidRegistration(string name, string email, string? password) =>
        name.Length is >= 2 and <= 80 && email.Length <= 254 &&
        System.Net.Mail.MailAddress.TryCreate(email, out var address) && address.Address == email &&
        password is { Length: >= 10 and <= 128 };

    public async Task<AuthResult> RegisterAsync(Register input)
    {
        var name = (input.Name ?? "").Trim();
        var email = (input.Email ?? "").Trim().ToLowerInvariant();
        if (!IsValidRegistration(name, email, input.Password))
            return new(null, 400, "Adınızı, geçerli e-postanızı ve 10–128 karakterlik şifrenizi girin.");
        var account = new Account
        {
            Name = name, Email = email, PasswordHash = passwords.Hash(input.Password!),
            Roles = [new() { Role = UserRole.Customer }]
        };
        store.Users.Add(account);
        try
        {
            // Account and its Customer role are inserted atomically by SaveChanges.
            await store.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            return new(null, 409, "Bu e-posta ile hesap oluşturulamıyor. Giriş yapmayı deneyin.");
        }
        return new(account);
    }

    public async Task<AuthResult> LoginAsync(Login input)
    {
        if (input.Password is null || input.Password.Length > 128 || input.Email is null || input.Email.Length > 254)
            return new(null, 400, "Giriş bilgileri geçersiz.");
        var email = input.Email.Trim().ToLowerInvariant();
        var account = await store.Users.Include(user => user.Roles).SingleOrDefaultAsync(user => user.Email == email);
        if (!passwords.Verify(input.Password, account?.PasswordHash) || account is null)
            return new(null, 401, "E-posta veya şifre hatalı.");
        return new(account);
    }
}
