using EpinelPS.Database.Domains;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database;

public static class LobbyUserStore
{
    private static readonly object SyncLock = new();

    private static readonly IUserDomainStore[] DomainStores =
    [
        new InventoryStore(),
        new OutpostStore(),
        new ProgressStore(),
        new ResetStore(),
        new TeamStore(),
        new CosmeticsStore(),
        new GachaShopPassStore(),
        new EventsStore(),
        new BondLiberationStore(),
        new SocialMailStore(),
        new ExplorationStore(),
        new SoloRaidStore(),
        new MinigameStore()
    ];

    public static void LoadUser(GameContext db, User user)
    {
        lock (SyncLock)
        {
            foreach (var store in DomainStores)
                store.Load(db, user);
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
            foreach (var store in DomainStores)
                store.Save(db, user);
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
                foreach (var store in DomainStores)
                    store.Save(db, user);
            }
            db.SaveChanges();
            tx.Commit();
        }
    }
}
