using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database;

public static partial class LobbyUserStore
{
    private static readonly object SyncLock = new();

    public static void LoadUser(GameContext db, User user)
    {
        lock (SyncLock)
        {
            LoadPart1(db, user);
            LoadPart2(db, user);
        }
    }

    public static void LoadUser(User user)
    {
        using var db = GameContext.CreateNew();
        LoadUser(db, user);
    }

    public static void LoadAllUsers(List<User> users)
    {
        using var db = GameContext.CreateNew();
        var sqlUserIds = db.Users.AsNoTracking().Select(u => u.ID)
            .Concat(db.SdkUsers.AsNoTracking().Select(u => u.ID))
            .Distinct()
            .ToList();

        lock (users)
        {
            foreach (ulong id in sqlUserIds)
            {
                if (!users.Any(u => u.ID == id))
                    users.Add(new User { ID = id });
            }

            foreach (var user in users)
            {
                UserDbHelper.GetOrCreateGameUser(db, user.ID);
                LoadUser(db, user);
            }
        }
    }

    public static void SaveUser(GameContext db, User user)
    {
        lock (SyncLock)
        {
            UserDbHelper.GetOrCreateGameUser(db, user.ID);
            SavePart1(db, user);
            SavePart2(db, user);
            db.SaveChanges();
        }
    }

    public static void SaveUser(User user)
    {
        using var db = GameContext.CreateNew();
        using var tx = db.Database.BeginTransaction();
        SaveUser(db, user);
        tx.Commit();
    }

    public static void SaveAll(IEnumerable<User> users)
    {
        List<User> snapshot;
        lock (users)
        {
            snapshot = users.ToList();
        }

        if (snapshot.Count == 0)
            return;

        lock (SyncLock)
        {
            using var db = GameContext.CreateNew();
            using var tx = db.Database.BeginTransaction();
            foreach (var user in snapshot)
            {
                UserDbHelper.GetOrCreateGameUser(db, user.ID);
                SavePart1(db, user);
                SavePart2(db, user);
            }
            db.SaveChanges();
            tx.Commit();
        }
    }
}
