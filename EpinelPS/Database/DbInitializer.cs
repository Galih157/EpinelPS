using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database;

public static class DbInitializer
{
    public static void Initialize(GameContext context)
    {
        Logging.WriteLine("Initializing & migrating database...");
        context.Database.Migrate();
    }
}
