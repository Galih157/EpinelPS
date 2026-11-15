using System.Text.Json;
using EpinelPS.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: EpinelPS.DBMigration <app-root>");
    return 2;
}

string appRoot = Path.GetFullPath(args[0]);
string dataDirectory = Path.Combine(appRoot, "data");
string legacyJsonPath = Path.Combine(appRoot, "db.json");
string dataJsonPath = Path.Combine(dataDirectory, "db.json");
string jsonPath = File.Exists(dataJsonPath) ? dataJsonPath : legacyJsonPath;
string oldDatabasePath = Path.Combine(appRoot, "epinelps.db");
string newDatabasePath = Path.Combine(dataDirectory, "epinelps.db");

if (Path.GetFullPath(oldDatabasePath).Equals(newDatabasePath, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("The source and destination database paths must differ.");
    return 2;
}

if (!File.Exists(jsonPath) && !File.Exists(oldDatabasePath))
{
    Console.Error.WriteLine("No db.json or epinelps.db source was found under the app root.");
    return 2;
}

Directory.CreateDirectory(dataDirectory);
try
{
    var options = new DbContextOptionsBuilder<GameContext>()
        .UseSqlite($"Data Source={newDatabasePath}")
        .Options;
    await using (var context = new GameContext(options))
        await context.Database.MigrateAsync();

    await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
    {
        DataSource = newDatabasePath,
        ForeignKeys = true
    }.ToString());
    await connection.OpenAsync();

    if (File.Exists(oldDatabasePath))
    {
        await using var attach = connection.CreateCommand();
        attach.CommandText = "ATTACH DATABASE $source AS legacy";
        attach.Parameters.AddWithValue("$source", oldDatabasePath);
        await attach.ExecuteNonQueryAsync();
    }

    await using var transaction = connection.BeginTransaction();

    int copiedSqlRows = File.Exists(oldDatabasePath)
        ? await CopySqliteTablesAsync(connection, transaction)
        : 0;
    int importedJsonRows = File.Exists(jsonPath)
        ? await ImportJsonAsync(connection, transaction, jsonPath)
        : 0;

    transaction.Commit();
    if (File.Exists(legacyJsonPath) && !File.Exists(dataJsonPath))
        File.Copy(legacyJsonPath, dataJsonPath);
    Console.WriteLine($"SQLite migration complete: {copiedSqlRows} rows copied from epinelps.db; {importedJsonRows} JSON records imported.");
    Console.WriteLine($"New database: {newDatabasePath}");
    Console.WriteLine("The source files were left unchanged.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Migration failed; source files were left unchanged: {ex.Message}");
    return 1;
}

static async Task<int> CopySqliteTablesAsync(SqliteConnection connection, SqliteTransaction transaction)
{
    int copied = 0;
    foreach (string table in new[] { "SdkUsers", "Users", "Triggers" })
    {
        if (!await TableExistsAsync(connection, transaction, "legacy", table) ||
            !await TableExistsAsync(connection, transaction, "main", table))
        {
            Console.WriteLine($"Skipped legacy SQLite table {table} (missing at source or destination).");
            continue;
        }

        HashSet<string> targetColumns = await GetColumnsAsync(connection, transaction, "main", table);
        HashSet<string> sourceColumns = await GetColumnsAsync(connection, transaction, "legacy", table);
        string[] columns = targetColumns.Intersect(sourceColumns, StringComparer.OrdinalIgnoreCase).ToArray();
        if (columns.Length == 0)
            throw new InvalidDataException($"Legacy table {table} has no columns compatible with the new schema.");

        string columnList = string.Join(", ", columns.Select(QuoteIdentifier));
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"INSERT OR IGNORE INTO main.{QuoteIdentifier(table)} ({columnList}) SELECT {columnList} FROM legacy.{QuoteIdentifier(table)}; SELECT changes();";
        copied += Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    return copied;
}

static async Task<int> ImportJsonAsync(SqliteConnection connection, SqliteTransaction transaction, string jsonPath)
{
    string json = await File.ReadAllTextAsync(jsonPath);
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement root = document.RootElement;
    int imported = 0;

    if (!TryGetProperty(root, "Users", out JsonElement users) || users.ValueKind != JsonValueKind.Array)
        return imported;

    HashSet<string> userColumns = await GetColumnsAsync(connection, transaction, "main", "Users");
    HashSet<string> sdkUserColumns = await GetColumnsAsync(connection, transaction, "main", "SdkUsers");
    HashSet<string> triggerColumns = await GetColumnsAsync(connection, transaction, "main", "Triggers");

    foreach (JsonElement user in users.EnumerateArray())
    {
        if (!TryGetProperty(user, "ID", out JsonElement idElement) ||
            !idElement.TryGetUInt64(out ulong userId) || userId > long.MaxValue)
            throw new InvalidDataException("A db.json user has a missing or unsupported ID.");

        Dictionary<string, object?> sdkUser = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ID"] = (long)userId,
            ["Email"] = GetString(user, "Username") ?? GetString(user, "Email") ?? "",
            ["PasswordHash"] = GetString(user, "Password") ?? GetString(user, "PasswordHash") ?? "",
            ["PlayerName"] = GetString(user, "PlayerName") ?? "Player_NULL",
            ["RegisterTime"] = GetInt64(user, "RegisterTime"),
            ["IsAdmin"] = GetBoolean(user, "IsAdmin"),
            ["LastLogin"] = GetString(user, "LastLogin") ?? DateTime.UnixEpoch.ToString("O")
        };
        imported += await InsertRowAsync(connection, transaction, "SdkUsers", sdkUserColumns, sdkUser);

        Dictionary<string, object?> gameUser = GetScalarProperties(user);
        gameUser["ID"] = (long)userId;
        gameUser["Nickname"] = GetString(user, "Nickname") ?? "Guest";
        gameUser["LastAction"] = TryGetProperty(user, "LastLogin", out JsonElement lastLogin)
            ? ToDatabaseValue(lastLogin)
            : DateTime.UnixEpoch.ToString("O");
        imported += await InsertRowAsync(connection, transaction, "Users", userColumns, gameUser);

        if (TryGetProperty(user, "Triggers", out JsonElement triggers) && triggers.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement trigger in triggers.EnumerateArray())
            {
                Dictionary<string, object?> triggerRow = GetScalarProperties(trigger);
                triggerRow["UserId"] = (long)userId;
                imported += await InsertRowAsync(connection, transaction, "Triggers", triggerColumns, triggerRow);
            }
        }
    }

    return imported;
}

static async Task<int> InsertRowAsync(SqliteConnection connection, SqliteTransaction transaction, string table, HashSet<string> tableColumns, Dictionary<string, object?> values)
{
    string[] columns = values.Keys.Where(tableColumns.Contains).ToArray();
    if (columns.Length == 0)
        return 0;

    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    string names = string.Join(", ", columns.Select(QuoteIdentifier));
    string parameters = string.Join(", ", columns.Select((_, i) => $"$p{i}"));
    command.CommandText = $"INSERT OR IGNORE INTO main.{QuoteIdentifier(table)} ({names}) VALUES ({parameters});";
    for (int i = 0; i < columns.Length; i++)
        command.Parameters.AddWithValue($"$p{i}", values[columns[i]] ?? DBNull.Value);
    return await command.ExecuteNonQueryAsync();
}

static Dictionary<string, object?> GetScalarProperties(JsonElement element)
{
    Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase);
    if (element.ValueKind != JsonValueKind.Object)
        return values;
    foreach (JsonProperty property in element.EnumerateObject())
    {
        object? value = ToDatabaseValue(property.Value);
        if (value is not null)
            values[property.Name] = value;
    }
    return values;
}

static object? ToDatabaseValue(JsonElement value) => value.ValueKind switch
{
    JsonValueKind.String => value.GetString(),
    JsonValueKind.Number when value.TryGetInt64(out long number) => number,
    JsonValueKind.Number => value.GetDouble(),
    JsonValueKind.True => 1L,
    JsonValueKind.False => 0L,
    JsonValueKind.Null => null,
    _ => null
};

static string? GetString(JsonElement element, string name) =>
    TryGetProperty(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

static long GetInt64(JsonElement element, string name) =>
    TryGetProperty(element, name, out JsonElement value) && value.TryGetInt64(out long number) ? number : 0;

static bool GetBoolean(JsonElement element, string name) =>
    TryGetProperty(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.True;

static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
{
    if (element.ValueKind == JsonValueKind.Object)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
    }
    value = default;
    return false;
}

static async Task<bool> TableExistsAsync(SqliteConnection connection, SqliteTransaction transaction, string schema, string table)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = $"SELECT EXISTS(SELECT 1 FROM {QuoteIdentifier(schema)}.sqlite_master WHERE type='table' AND name=$name);";
    command.Parameters.AddWithValue("$name", table);
    return Convert.ToInt32(await command.ExecuteScalarAsync()) != 0;
}

static async Task<HashSet<string>> GetColumnsAsync(SqliteConnection connection, SqliteTransaction transaction, string schema, string table)
{
    await using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = $"PRAGMA {QuoteIdentifier(schema)}.table_info({QuoteIdentifier(table)});";
    await using var reader = await command.ExecuteReaderAsync();
    HashSet<string> columns = new(StringComparer.OrdinalIgnoreCase);
    while (await reader.ReadAsync())
        columns.Add(reader.GetString(1));
    return columns;
}

static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
