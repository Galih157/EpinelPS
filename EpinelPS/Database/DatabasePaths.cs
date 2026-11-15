namespace EpinelPS.Database;

public static class DatabasePaths
{
    public static string AppRoot => AppDomain.CurrentDomain.BaseDirectory;
    public static string DataDirectory => Path.Combine(AppRoot, "data");
    public static string SqliteDatabase => Path.Combine(DataDirectory, "epinelps.db");
    public static string JsonConfiguration => Path.Combine(DataDirectory, "db.json");
    public static string LegacyJsonConfiguration => Path.Combine(AppRoot, "db.json");

    public static void EnsureDataDirectory() => Directory.CreateDirectory(DataDirectory);
}
