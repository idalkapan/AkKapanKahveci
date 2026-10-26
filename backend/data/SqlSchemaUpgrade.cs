using Microsoft.Data.Sqlite;

static class SqlSchemaUpgrade
{
    public const int CurrentVersion = 1;

    public static string? Apply(string connectionString, bool enabled)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        // Serialize startup upgrades before checking the version or taking a backup.
        using var transaction = connection.BeginTransaction(deferred: false);
        var version = Convert.ToInt32(Scalar(connection, transaction, "PRAGMA user_version"));
        if (version > CurrentVersion)
            throw new InvalidOperationException("Veritabanı sürümü bu uygulamadan daha yeni.");
        if (version == CurrentVersion)
            return null;
        if (!enabled)
            throw new InvalidOperationException("SQL yükseltmesi kapalı; veritabanı şeması henüz hazır değil.");

        // No schema/data mutation is allowed before a consistent SQLite backup succeeds.
        var backup = Backup(connection.DataSource);
        Execute(connection, transaction, """
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL,
                Email TEXT NOT NULL, PasswordHash TEXT NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email);
            CREATE TABLE IF NOT EXISTS Documents (
                Id TEXT NOT NULL PRIMARY KEY, Session TEXT NOT NULL,
                Status TEXT NOT NULL, Extension TEXT NOT NULL, Created TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Orders (
                Id TEXT NOT NULL PRIMARY KEY, Session TEXT NOT NULL, Customer TEXT NOT NULL,
                Concept TEXT NOT NULL, Total TEXT NOT NULL, Discount TEXT NOT NULL,
                ItemsJson TEXT NOT NULL, Created TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS UserRoles (
                UserId TEXT NOT NULL, Role TEXT NOT NULL,
                PRIMARY KEY (UserId, Role),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                CHECK (Role IN ('Customer', 'Barista', 'Admin')));
            INSERT OR IGNORE INTO UserRoles (UserId, Role) SELECT Id, 'Customer' FROM Users;
            ALTER TABLE Orders ADD COLUMN PreparationStatus TEXT NOT NULL DEFAULT 'Delivered';
            ALTER TABLE Orders ADD COLUMN PaymentStatus TEXT NOT NULL DEFAULT 'DemoPaid';
            ALTER TABLE Orders ADD COLUMN UpdatedAt TEXT NOT NULL DEFAULT '';
            ALTER TABLE Orders ADD COLUMN Version INTEGER NOT NULL DEFAULT 1;
            UPDATE Orders SET UpdatedAt = Created;
            PRAGMA user_version = 1;
            """);
        transaction.Commit();
        return backup;
    }

    private static string Backup(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.Combine(Path.GetDirectoryName(fullPath)!, "backups");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, $"coffee-{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}.db");
        // CreateNew prevents accidentally overwriting a prior backup.
        using (new FileStream(destination, FileMode.CreateNew, FileAccess.Write)) { }
        using var source = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = fullPath, Mode = SqliteOpenMode.ReadOnly, Pooling = false
        }.ToString());
        using var target = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = destination, Mode = SqliteOpenMode.ReadWrite, Pooling = false
        }.ToString());
        source.Open();
        target.Open();
        source.BackupDatabase(target);
        using var check = target.CreateCommand();
        check.CommandText = "PRAGMA integrity_check";
        if (!string.Equals(check.ExecuteScalar()?.ToString(), "ok", StringComparison.Ordinal))
            throw new InvalidOperationException("Veritabanı yedeği doğrulanamadı; yükseltme durduruldu.");
        return destination;
    }

    private static object? Scalar(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
