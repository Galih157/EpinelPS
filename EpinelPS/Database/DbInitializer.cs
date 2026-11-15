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

        const string email = "admin@nikke.com";
        if (context.SdkUsers.Any(user => user.Email == email))
            return;

        ulong id;
        do
        {
            id = (ulong)Random.Shared.Next(1, int.MaxValue);
        }
        while (context.SdkUsers.Any(user => user.ID == id) || context.Users.Any(user => user.ID == id));

        context.SdkUsers.Add(new SdkUser
        {
            ID = id,
            Email = email,
            PasswordHash = Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes("admin123"))).ToLowerInvariant(),
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
