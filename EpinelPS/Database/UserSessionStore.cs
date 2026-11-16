using System.Collections.Concurrent;

namespace EpinelPS.Database;

public static class UserSessionStore
{
    private static readonly object LoadLock = new();

    public static ConcurrentDictionary<ulong, User> UsersById { get; } = new();

    public static IReadOnlyCollection<User> Users => GetAllUsers();

    public static List<User> GetAllUsers() => [.. UsersById.Values];

    public static User? GetUser(ulong id)
    {
        if (UsersById.TryGetValue(id, out var existing))
            return existing;

        using var db = GameContext.CreateNew();
        bool existsInSql = db.SdkUsers.Any(u => u.ID == id) || db.Users.Any(u => u.ID == id);
        if (!existsInSql)
            return null;

        UserDbHelper.GetOrCreateGameUser(db, id);
        lock (LoadLock)
        {
            if (UsersById.TryGetValue(id, out existing))
                return existing;

            var user = new User { ID = id };
            LobbyUserStore.LoadUser(db, user);
            UsersById[id] = user;
            return user;
        }
    }

    public static User GetOrAddUser(ulong id)
    {
        if (UsersById.TryGetValue(id, out var existing))
            return existing;

        lock (LoadLock)
        {
            if (UsersById.TryGetValue(id, out existing))
                return existing;

            using var db = GameContext.CreateNew();
            UserDbHelper.GetOrCreateGameUser(db, id);
            var user = new User { ID = id };
            LobbyUserStore.LoadUser(db, user);
            UsersById[id] = user;
            return user;
        }
    }

    public static void AddUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        UsersById[user.ID] = user;
    }

    public static bool RemoveUser(ulong id) => UsersById.TryRemove(id, out _);

    public static void LoadAllUsers()
    {
        var users = GetAllUsers();
        LobbyUserStore.LoadAllUsers(users);
        foreach (var user in users)
        {
            UsersById[user.ID] = user;
        }
    }

    public static void SaveUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        UsersById[user.ID] = user;
        if (GameContext.Options != null)
        {
            LobbyUserStore.SaveUser(user);
        }
    }

    public static void SaveAll()
    {
        if (GameContext.Options != null)
        {
            LobbyUserStore.SaveAll(GetAllUsers());
        }
    }

    public static int CurrentJukeboxBgm(int position)
    {
        var activeJukeboxBgm = new List<int>();
        //important first position holds lobby bgm id and second commanders room bgm id
        foreach (var user in Users)
        {
            if (user.JukeboxBgm == null || user.JukeboxBgm.Count == 0)
            {
                user.JukeboxBgm = [2, 5];
            }

            activeJukeboxBgm.AddRange(user.JukeboxBgm);
        }

        if (activeJukeboxBgm.Count == 0)
        {
            return 8995001;
        }

        position = (position == 2 && activeJukeboxBgm.Count > 1) ? 2 : 1;
        return activeJukeboxBgm[position - 1];
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
