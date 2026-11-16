using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Repositories;

public sealed class ResetRepository(GameContext db)
{
    public UserResetStateEntity GetOrCreateResetState(ulong userId)
    {
        var set = db.Set<UserResetStateEntity>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId)
                  ?? set.FirstOrDefault(x => x.UserId == userId);

        if (row != null)
            return row;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        row = new UserResetStateEntity
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            InterceptionTickets = 3
        };
        set.Add(row);
        return row;
    }

    // --- SimRoom State ---

    public SimRoomData GetSimRoomData(ulong userId)
    {
        var row = db.Set<UserResetStateEntity>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId);

        return DeserializeSimRoomData(row?.SimRoomStateJson);
    }

    public void SaveSimRoomData(ulong userId, SimRoomData simRoomData)
    {
        var row = GetOrCreateResetState(userId);
        row.SimRoomStateJson = JsonConvert.SerializeObject(simRoomData ?? new SimRoomData());
    }

    // --- Scalar Reset Counters ---

    public int GetWipeoutCount(ulong userId)
    {
        return db.Set<UserResetStateEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.WipeoutCount)
            .FirstOrDefault() ?? 0;
    }

    public int IncrementWipeoutCount(ulong userId)
    {
        var row = GetOrCreateResetState(userId);
        row.WipeoutCount++;
        return row.WipeoutCount;
    }

    public int GetInterceptionTickets(ulong userId)
    {
        return db.Set<UserResetStateEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.InterceptionTickets)
            .FirstOrDefault() ?? 3;
    }

    public int DecrementInterceptionTickets(ulong userId)
    {
        var row = GetOrCreateResetState(userId);
        row.InterceptionTickets--;
        return row.InterceptionTickets;
    }

    public int GetDispatchCount(ulong userId)
    {
        return db.Set<UserResetStateEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.DispatchCount)
            .FirstOrDefault() ?? 0;
    }

    public void AddDailyMissionPoints(ulong userId, int points)
    {
        var row = GetOrCreateResetState(userId);
        row.DailyMissionPoints += points;
    }

    public void AddWeeklyMissionPoints(ulong userId, int points)
    {
        var row = GetOrCreateResetState(userId);
        row.WeeklyMissionPoints += points;
    }

    // --- Daily & Weekly Missions ---

    public List<int> GetCompletedDailyMissions(ulong userId)
    {
        return db.Set<UserCompletedDailyMissionEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MissionId)
            .ToList();
    }

    public void AddCompletedDailyMission(ulong userId, int missionId)
    {
        var set = db.Set<UserCompletedDailyMissionEntity>();
        if (set.Local.Any(x => x.UserId == userId && x.MissionId == missionId) ||
            set.AsNoTracking().Any(x => x.UserId == userId && x.MissionId == missionId))
            return;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        set.Add(new UserCompletedDailyMissionEntity
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            MissionId = missionId
        });
    }

    public List<int> GetCompletedWeeklyMissions(ulong userId)
    {
        return db.Set<UserCompletedWeeklyMissionEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MissionId)
            .ToList();
    }

    public void AddCompletedWeeklyMission(ulong userId, int missionId)
    {
        var set = db.Set<UserCompletedWeeklyMissionEntity>();
        if (set.Local.Any(x => x.UserId == userId && x.MissionId == missionId) ||
            set.AsNoTracking().Any(x => x.UserId == userId && x.MissionId == missionId))
            return;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        set.Add(new UserCompletedWeeklyMissionEntity
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            MissionId = missionId
        });
    }

    // --- Tower Counts ---

    public Dictionary<CorporationTowerType, int> GetDailyTowerCounts(ulong userId)
    {
        var counts = Enum.GetValues<CorporationTowerType>().ToDictionary(t => t, _ => 0);

        var rows = db.Set<UserDailyTowerCountEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToList();

        foreach (var row in rows)
            counts[(CorporationTowerType)row.CorporationType] = row.UsedCount;

        return counts;
    }

    public void AddDailyTowerCount(ulong userId, CorporationTowerType towerType, int count)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        int corpType = (int)towerType;
        var set = db.Set<UserDailyTowerCountEntity>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.CorporationType == corpType)
                  ?? set.FirstOrDefault(x => x.UserId == userId && x.CorporationType == corpType);

        if (row == null)
        {
            set.Add(new UserDailyTowerCountEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                CorporationType = corpType,
                UsedCount = count
            });
        }
        else
        {
            row.UsedCount += count;
        }
    }

    // --- Daily Counsel Count ---

    public int GetDailyCounselCount(ulong userId, int keyId = 1, int defaultValue = 3)
    {
        return db.Set<UserDailyCounselCountEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.KeyId == keyId)
            .Select(x => (int?)x.Count)
            .FirstOrDefault() ?? defaultValue;
    }

    public void SetDailyCounselCount(ulong userId, int keyId, int count)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserDailyCounselCountEntity>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.KeyId == keyId)
                  ?? set.FirstOrDefault(x => x.UserId == userId && x.KeyId == keyId);

        if (row == null)
        {
            set.Add(new UserDailyCounselCountEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                KeyId = keyId,
                Count = count
            });
        }
        else
        {
            row.Count = count;
        }
    }

    // --- Scheduled Resets ---

    public void PerformWeeklyReset(ulong userId)
    {
        var row = GetOrCreateResetState(userId);
        var prevSimRoom = DeserializeSimRoomData(row.SimRoomStateJson);
        var currentSeasonData = prevSimRoom.CurrentSeasonData ?? new OverclockData();
        currentSeasonData.LatestOption = new();

        var nextSimRoom = new SimRoomData
        {
            // Retain old CurrentDifficulty, CurrentChapter, and currentSeason data
            CurrentDifficulty = prevSimRoom.CurrentDifficulty,
            CurrentChapter = prevSimRoom.CurrentChapter,
            CurrentSeasonData = currentSeasonData
        };

        row.WipeoutCount = 0;
        row.ClearedSimulationRoom = false;
        row.InterceptionTickets = 3;
        row.DailyMissionPoints = 0;
        row.DispatchCount = 0;
        row.WeeklyMissionPoints = 0;
        row.SimRoomStateJson = JsonConvert.SerializeObject(nextSimRoom);

        ClearRows<UserCompletedDailyMissionEntity>(userId);
        ClearRows<UserCompletedWeeklyMissionEntity>(userId);
        ClearRows<UserDailyTowerCountEntity>(userId);
        ClearRows<UserDailyCounselCountEntity>(userId);
    }

    public void PerformDailyReset(ulong userId, int dispatchCount, int counselCount)
    {
        var row = GetOrCreateResetState(userId);
        var prevSimRoom = DeserializeSimRoomData(row.SimRoomStateJson);

        var nextSimRoom = new SimRoomData
        {
            LegacyBuffs = prevSimRoom.LegacyBuffs ?? [],
            CurrentDifficulty = prevSimRoom.CurrentDifficulty,
            CurrentChapter = prevSimRoom.CurrentChapter,
            CurrentSeasonData = prevSimRoom.CurrentSeasonData ?? new OverclockData()
        };

        row.WipeoutCount = 0;
        row.ClearedSimulationRoom = false;
        row.InterceptionTickets = 3;
        row.DailyMissionPoints = 0;
        row.DispatchCount = dispatchCount;
        row.SimRoomStateJson = JsonConvert.SerializeObject(nextSimRoom);

        ClearRows<UserCompletedDailyMissionEntity>(userId);
        ClearRows<UserDailyTowerCountEntity>(userId);

        var counselSet = db.Set<UserDailyCounselCountEntity>();
        var staleCounsel = counselSet.Where(x => x.UserId == userId && x.KeyId != 1).ToList();
        if (staleCounsel.Count > 0)
            counselSet.RemoveRange(staleCounsel);

        SetDailyCounselCount(userId, 1, counselCount);
    }

    private void ClearRows<TEntity>(ulong userId) where TEntity : class
    {
        var set = db.Set<TEntity>();
        var userIdProp = typeof(TEntity).GetProperty("UserId")
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no UserId property.");
        var rows = set.Where(e => EF.Property<ulong>(e, userIdProp.Name) == userId).ToList();
        if (rows.Count > 0)
            set.RemoveRange(rows);
    }

    private static SimRoomData DeserializeSimRoomData(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? new SimRoomData()
            : JsonConvert.DeserializeObject<SimRoomData>(json) ?? new SimRoomData();
    }
}
