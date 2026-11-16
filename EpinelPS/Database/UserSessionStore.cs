using System.Collections.Concurrent;
using EpinelPS.Repositories;

namespace EpinelPS.Database;

public static class UserSessionStore
{
    private static readonly object LoadLock = new();
    private static readonly ConcurrentDictionary<ulong, byte> InitializedUsers = new();

    public static User? GetUser(ulong id)
    {
        using var db = GameContext.CreateNew();
        bool existsInSql = db.SdkUsers.Any(u => u.ID == id) || db.Users.Any(u => u.ID == id);
        if (!existsInSql)
            return null;

        UserDbHelper.GetOrCreateGameUser(db, id);
        var user = new User { ID = id };
        LoadUserFromDb(db, user);
        EnsureInitialized(db, user);
        return user;
    }

    public static User GetOrAddUser(ulong id)
    {
        using var db = GameContext.CreateNew();
        UserDbHelper.GetOrCreateGameUser(db, id);
        var user = new User { ID = id };
        LoadUserFromDb(db, user);
        EnsureInitialized(db, user);
        return user;
    }

    private static void EnsureInitialized(GameContext db, User user)
    {
        if (InitializedUsers.ContainsKey(user.ID))
            return;

        lock (LoadLock)
        {
            if (InitializedUsers.TryAdd(user.ID, 0))
            {
                InitializeLoadedUser(db, user);
            }
            else
            {
                LoadUserFromDb(db, user);
            }
        }
    }

    private static void LoadUserFromDb(GameContext db, User user)
    {
        new InventoryRepository(db).LoadUserInventory(user);
        new ProgressRepository(db).LoadUserCore(user);
    }

    private static void SaveUserToDb(GameContext db, User user)
    {
        new InventoryRepository(db).SaveUserInventory(user);
        new ProgressRepository(db).SaveUserCore(user);
        db.SaveChanges();
    }

    private static void InitializeLoadedUser(GameContext db, User user)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, user.ID);
        if (gameUser.LastNormalStageCleared >= 6000002)
        {
            LobbyServer.Stage.ClearStage.EnsureDefaultCharacters(user);
        }
        LobbyServer.Stage.ClearStage.ReconcileMainQuests(user, logToConsole: false);
        Utils.MessengerMessageCreator.CreateAllEligibleOpeners(user, logToConsole: false);
        user.Currency[Data.CurrencyType.GuildCoin] = Math.Max(user.GetCurrencyVal(Data.CurrencyType.GuildCoin), 100000);
        SaveUserToDb(db, user);
    }

    public static bool RemoveUser(ulong id) => InitializedUsers.TryRemove(id, out _);

    public static void SaveUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (GameContext.Options != null)
        {
            using var db = GameContext.CreateNew();
            SaveUserToDb(db, user);
        }
    }

    public static bool IsSickPulls(User selectedUser)
    {
        if (selectedUser != null)
        {
            return UserDbHelper.GetOrCreateGameUser(selectedUser.ID).SickPullsEnabled;
        }
        else
        {
            throw new Exception("User not found");
        }
    }
}
