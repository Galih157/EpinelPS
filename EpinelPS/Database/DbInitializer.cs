using EpinelPS.Utils;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EpinelPS.Database;

public static class DbInitializer
{
    public static void Initialize(GameContext context)
    {
        Logging.WriteLine("Initializing & migrating database...");
        context.Database.Migrate();
        ServerConfigStore.InitializeFromContext(context);

        const string email = "admin@nikke.com";
        if (context.SdkUsers.Any(user => user.Email == email))
            return;

        ulong id = UserDbHelper.GenerateNextUserId(context);

        context.SdkUsers.Add(new SdkUser
        {
            ID = id,
            Email = email,
            PasswordHash = PasswordHelper.HashMd5("admin123"),
            PlayerName = "Admin",
            RegisterTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsAdmin = true,
            LastLogin = DateTime.UtcNow,
        });
        context.Users.Add(new GameUser { ID = id, Nickname = "Admin" });
        context.SaveChanges();
        Logging.WriteLine($"Created default SQLite admin account: {email}");
    }
}
