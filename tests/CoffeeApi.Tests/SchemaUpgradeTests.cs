using Microsoft.Data.Sqlite;
using Xunit;

public class SchemaUpgradeTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "coffee-unit-" + Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(directory, "coffee.db");
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = DatabasePath, Pooling = false }.ToString();

    public SchemaUpgradeTests() => Directory.CreateDirectory(directory);

    [Fact]
    public void UpgradeBacksUpPreservesDataAndRunsOnce()
    {
        Execute("""
            CREATE TABLE Users (Id TEXT PRIMARY KEY, Name TEXT, Email TEXT, PasswordHash TEXT);
            INSERT INTO Users VALUES ('old-user', 'Test', 'old@example.com', 'hash');
            CREATE TABLE Orders (Id TEXT PRIMARY KEY, Session TEXT, Customer TEXT, Concept TEXT,
              Total TEXT, Discount TEXT, ItemsJson TEXT, Created TEXT);
            INSERT INTO Orders VALUES ('old-order', 'old-user', 'Test', 'candy', '100', '0', '[]', '2026-01-01 12:00:00');
            """);
        var backup = SqlSchemaUpgrade.Apply(ConnectionString, true);
        Assert.NotNull(backup);
        Assert.True(File.Exists(backup));
        Assert.Equal("1", Read("PRAGMA user_version"));
        Assert.Equal("Customer", Read("SELECT Role FROM UserRoles WHERE UserId = 'old-user'"));
        Assert.Equal("Delivered|DemoPaid|2026-01-01 12:00:00|1", Read("SELECT PreparationStatus || '|' || PaymentStatus || '|' || UpdatedAt || '|' || Version FROM Orders"));
        using (var source = new SqliteConnection($"Data Source={backup};Mode=ReadOnly;Pooling=False"))
        {
            source.Open();
            using var command = source.CreateCommand();
            command.CommandText = "PRAGMA user_version";
            Assert.Equal(0L, command.ExecuteScalar());
            command.CommandText = "SELECT COUNT(*) FROM Orders";
            Assert.Equal(1L, command.ExecuteScalar());
        }
        Assert.Null(SqlSchemaUpgrade.Apply(ConnectionString, true));
        Assert.Null(SqlSchemaUpgrade.Apply(ConnectionString, false));
        Assert.Single(Directory.GetFiles(Path.Combine(directory, "backups"), "*.db"));
    }

    [Fact]
    public void BackupFailurePreventsAllSchemaChanges()
    {
        Execute("CREATE TABLE Sentinel (Id INTEGER)");
        File.WriteAllText(Path.Combine(directory, "backups"), "blocks directory creation");
        Assert.ThrowsAny<IOException>(() => SqlSchemaUpgrade.Apply(ConnectionString, true));
        Assert.Equal("0", Read("PRAGMA user_version"));
        Assert.Equal("0", Read("SELECT COUNT(*) FROM sqlite_master WHERE name = 'UserRoles'"));
    }

    [Fact]
    public void SqlFailureRollsBackAllChangesAndVersion()
    {
        Execute("CREATE TABLE Orders (Id TEXT PRIMARY KEY, PreparationStatus TEXT)");
        Assert.Throws<SqliteException>(() => SqlSchemaUpgrade.Apply(ConnectionString, true));
        Assert.Equal("0", Read("PRAGMA user_version"));
        Assert.Equal("0", Read("SELECT COUNT(*) FROM sqlite_master WHERE name IN ('Users', 'UserRoles', 'Documents')"));
    }

    [Fact]
    public void DisabledUpgradeCannotSilentlyStartWithAnOldSchema()
    {
        Execute("CREATE TABLE Sentinel (Id INTEGER)");
        Assert.Throws<InvalidOperationException>(() => SqlSchemaUpgrade.Apply(ConnectionString, false));
        Assert.False(Directory.Exists(Path.Combine(directory, "backups")));
    }

    [Fact]
    public void BackupIncludesCommittedWalData()
    {
        using var writer = new SqliteConnection(ConnectionString);
        writer.Open();
        using var command = writer.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; CREATE TABLE Sentinel (Id INTEGER); INSERT INTO Sentinel VALUES (42)";
        command.ExecuteNonQuery();
        Assert.True(File.Exists(DatabasePath + "-wal"));
        var backup = SqlSchemaUpgrade.Apply(ConnectionString, true);
        using var saved = new SqliteConnection($"Data Source={backup};Mode=ReadOnly;Pooling=False");
        saved.Open();
        using var read = saved.CreateCommand();
        read.CommandText = "SELECT Id FROM Sentinel";
        Assert.Equal(42L, read.ExecuteScalar());
    }

    [Fact]
    public async Task ConcurrentStartupsApplyTheUpgradeOnlyOnce()
    {
        Execute("CREATE TABLE Sentinel (Id INTEGER)");
        var results = await Task.WhenAll(
            Task.Run(() => SqlSchemaUpgrade.Apply(ConnectionString, true)),
            Task.Run(() => SqlSchemaUpgrade.Apply(ConnectionString, true)));
        Assert.Single(results, result => result is not null);
        Assert.Equal("1", Read("PRAGMA user_version"));
        Assert.Single(Directory.GetFiles(Path.Combine(directory, "backups"), "*.db"));
    }

    private void Execute(string sql)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private string? Read(string sql)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar()?.ToString();
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}
