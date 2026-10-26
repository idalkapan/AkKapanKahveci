using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static class AdminBootstrap
{
    public static async Task RunAsync(WebApplication app)
    {
        if (!app.Configuration.GetValue("BootstrapAdmin:Enabled", false))
            return;
        using var scope = app.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<Store>();
        await store.Database.OpenConnectionAsync();
        var connection = (SqliteConnection)store.Database.GetDbConnection();
        using var transaction = connection.BeginTransaction(deferred: false);
        await using var enlistment = await store.Database.UseTransactionAsync(transaction);
        if (await store.UserRoles.AnyAsync(role => role.Role == UserRole.Admin))
        {
            app.Logger.LogInformation("Admin zaten mevcut; bootstrap değişiklik yapmadı.");
            return;
        }

        var email = (app.Configuration["BootstrapAdmin:Email"] ?? "").Trim().ToLowerInvariant();
        if (email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new InvalidOperationException("BootstrapAdmin:Email geçerli bir e-posta olmalı.");
        var useExisting = app.Configuration.GetValue("BootstrapAdmin:UseExistingAccount", false);
        var account = await store.Users.Include(user => user.Roles).SingleOrDefaultAsync(user => user.Email == email);
        if (useExisting)
        {
            if (account is null)
                throw new InvalidOperationException("Bootstrap için seçilen mevcut hesap bulunamadı.");
        }
        else
        {
            if (account is not null)
                throw new InvalidOperationException("Hesap zaten var. Mevcut hesabı kullanmak için UseExistingAccount açıkça etkinleştirilmeli.");
            var name = (app.Configuration["BootstrapAdmin:Name"] ?? "").Trim();
            var password = app.Configuration["BootstrapAdmin:Password"];
            if (!AuthService.IsValidRegistration(name, email, password))
                throw new InvalidOperationException("Yeni Admin için geçerli ad, e-posta ve 10–128 karakter şifre gerekli.");
            account = new Account
            {
                Name = name, Email = email,
                PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(password!),
                Roles = [new() { Role = UserRole.Customer }]
            };
            store.Users.Add(account);
        }
        account!.Roles.Add(new AccountRole { Role = UserRole.Admin, UserId = account.Id });
        await store.SaveChangesAsync();
        transaction.Commit();
        app.Logger.LogInformation("İlk Admin kurulumu tamamlandı. Bootstrap ayarlarını kaldırın.");
    }
}
