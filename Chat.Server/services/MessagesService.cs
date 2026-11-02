using System.Globalization;
using Chat.Shared.Contracts;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Chat.Server.Services;

public sealed class MessagesService(
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    private SqliteConnection CreateConnection()
    {
        var connectionString =
            configuration.GetConnectionString("ChatConnection")
            ?? throw new InvalidOperationException(
                "Falta configurar ChatConnection.");

        var settings = new SqliteConnectionStringBuilder(
            connectionString);

        if (!Path.IsPathRooted(settings.DataSource))
            settings.DataSource = Path.Combine(
                environment.ContentRootPath,
                settings.DataSource);

        var directory = Path.GetDirectoryName(settings.DataSource);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        return new SqliteConnection(settings.ToString());
    }

    public async Task InitializeAsync()
    {
        const string query = """
            CREATE TABLE IF NOT EXISTS Messages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserName TEXT NOT NULL,
                Message TEXT NOT NULL,
                SentAt TEXT NOT NULL
            );
            """;

        using var connection = CreateConnection();
        await connection.ExecuteAsync(query);
    }

    public async Task<ChatMessage> SaveAsync(
        string user,
        string message)
    {
        const string query = """
            INSERT INTO Messages (UserName, Message, SentAt)
            VALUES (@User, @Message, @SentAt);

            SELECT last_insert_rowid();
            """;

        var sentAt = DateTimeOffset.UtcNow;

        using var connection = CreateConnection();

        long id = await connection.ExecuteScalarAsync<long>(
            query,
            new
            {
                User = user,
                Message = message,
                SentAt = sentAt.ToString(
                    "O",
                    CultureInfo.InvariantCulture)
            });

        return new ChatMessage(id, user, message, sentAt);
    }

    public async Task<IEnumerable<ChatMessage>> GetHistoryAsync()
    {
        const string query = """
            SELECT Id, UserName AS User, Message, SentAt
            FROM (
                SELECT Id, UserName, Message, SentAt
                FROM Messages
                ORDER BY Id DESC
                LIMIT 50
            )
            ORDER BY Id;
            """;

        using var connection = CreateConnection();

        var rows = await connection.QueryAsync<StoredMessage>(query);

        return rows.Select(row => new ChatMessage(
            row.Id,
            row.User,
            row.Message,
            DateTimeOffset.Parse(
                row.SentAt,
                CultureInfo.InvariantCulture)
        )).ToArray();
    }

    private sealed record StoredMessage(
        long Id,
        string User,
        string Message,
        string SentAt);
}