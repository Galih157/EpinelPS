using EpinelPS.Models;

namespace EpinelPS.Database;

public static class UserDbHelper
{
    public static ulong GenerateNextUserId(GameContext db)
    {
        ulong maxSdkId = db.SdkUsers.Select(u => u.ID).AsEnumerable().DefaultIfEmpty(0UL).Max();
        ulong maxGameId = db.Users.Select(u => u.ID).AsEnumerable().DefaultIfEmpty(0UL).Max();
        ulong maxJsonId = File.Exists(DatabasePaths.JsonConfiguration) && JsonDb.Instance.Users.Count > 0
            ? JsonDb.Instance.Users.Max(u => u.ID)
            : 0;

        ulong next = Math.Max(10000UL, Math.Max(maxSdkId, Math.Max(maxGameId, maxJsonId))) + 1;
        while (db.SdkUsers.Any(u => u.ID == next) || db.Users.Any(u => u.ID == next))
            next++;

        return next;
    }

    public static GameUser GetOrCreateGameUser(GameContext db, ulong userId)
    {
        var gameUser = db.Users.Find(userId);
        if (gameUser == null)
        {
            gameUser = new GameUser { ID = userId };
            db.Users.Add(gameUser);
            db.SaveChanges();
        }
        return gameUser;
    }

    public static GameUser GetOrCreateGameUser(ulong userId)
    {
        using var db = GameContext.CreateNew();
        return GetOrCreateGameUser(db, userId);
    }

    public static void UpdateGameUser(ulong userId, Action<GameUser> update)
    {
        using var db = GameContext.CreateNew();
        var gameUser = GetOrCreateGameUser(db, userId);
        update(gameUser);
        db.SaveChanges();
    }

    public static SdkUser? GetSdkUser(ulong userId)
    {
        using var db = GameContext.CreateNew();
        return db.SdkUsers.Find(userId);
    }

    public static UserBan? GetActiveBan(GameContext db, ulong userId)
    {
        var ban = db.UserBans
            .Where(b => b.UserId == userId && b.IsBanned)
            .OrderByDescending(b => b.BanStart)
            .FirstOrDefault();

        if (ban != null && ban.BanEnd < DateTime.UtcNow)
        {
            ban.IsBanned = false;
            ban.BanId = 0;
            ban.BanStart = DateTime.MinValue;
            ban.BanEnd = DateTime.MinValue;
            db.SaveChanges();
            return null;
        }

        return ban;
    }

    public static void SetBanState(GameContext db, ulong userId, bool isBanned, DateTime? banStart = null, DateTime? banEnd = null, int banId = 0)
    {
        GetOrCreateGameUser(db, userId);
        var activeBans = db.UserBans.Where(b => b.UserId == userId && b.IsBanned).ToList();

        if (!isBanned)
        {
            foreach (var ban in activeBans)
            {
                ban.IsBanned = false;
                ban.BanId = 0;
                ban.BanStart = DateTime.MinValue;
                ban.BanEnd = DateTime.MinValue;
            }
            db.SaveChanges();
            return;
        }

        var current = activeBans.FirstOrDefault();
        if (current == null)
        {
            db.UserBans.Add(new UserBan
            {
                Id = Utils.UlidGenerator.NewUlid(),
                UserId = userId,
                IsBanned = true,
                BanStart = banStart ?? DateTime.UtcNow,
                BanEnd = banEnd ?? DateTime.UtcNow.AddYears(10),
                BanId = banId
            });
        }
        else
        {
            current.IsBanned = true;
            if (banStart.HasValue) current.BanStart = banStart.Value;
            if (banEnd.HasValue) current.BanEnd = banEnd.Value;
            if (banId != 0) current.BanId = banId;
        }

        db.SaveChanges();
    }
}
