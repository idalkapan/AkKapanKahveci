using Microsoft.EntityFrameworkCore;
static class DatabaseInitialization
{
    public static void InitializeCoffeeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Store>();
        var backup = SqlSchemaUpgrade.Apply(db.Database.GetConnectionString()!,
            app.Configuration.GetValue("Database:SqlUpgrades:Enabled", true));
        if (backup is not null)
            app.Logger.LogInformation("SQL şema sürümü {Version} uygulandı. Yedek: {Backup}", SqlSchemaUpgrade.CurrentVersion, backup);
    }
}
