using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EpinelPS.Database;

public class GameContextFactory : IDesignTimeDbContextFactory<GameContext>
{
    public GameContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<GameContext>();
        builder.UseSqlite($"Data Source=\"{DatabasePaths.SqliteDatabase}\"");
        return new GameContext(builder.Options);
    }
}
