using EpinelPS.Data;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database.Domains;

public sealed class ResetStore : IUserDomainStore
{
    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        user.ResetableData ??= new ResetableData();
        user.WeeklyResetableData ??= new WeeklyResetableData();

        var resetState = db.Set<UserResetStateEntity>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId);

        if (resetState != null)
        {
            user.ResetableData.WipeoutCount = resetState.WipeoutCount;
            user.ResetableData.ClearedSimulationRoom = resetState.ClearedSimulationRoom;
            user.ResetableData.InterceptionTickets = resetState.InterceptionTickets;
            user.ResetableData.DailyMissionPoints = resetState.DailyMissionPoints;
            user.ResetableData.DispatchCount = resetState.DispatchCount;
            user.ResetableData.SimRoomData = string.IsNullOrWhiteSpace(resetState.SimRoomStateJson)
                ? new SimRoomData()
                : JsonConvert.DeserializeObject<SimRoomData>(resetState.SimRoomStateJson) ?? new SimRoomData();
            user.WeeklyResetableData.WeeklyMissionPoints = resetState.WeeklyMissionPoints;
        }

        user.ResetableData.CompletedDailyMissions = db.Set<UserCompletedDailyMissionEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MissionId)
            .ToList();

        user.WeeklyResetableData.CompletedWeeklyMissions = db.Set<UserCompletedWeeklyMissionEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MissionId)
            .ToList();

        var towerCounts = db.Set<UserDailyTowerCountEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToList();
        foreach (var tc in towerCounts)
        {
            user.ResetableData.TowerCount[(CorporationTowerType)tc.CorporationType] = tc.UsedCount;
        }

        user.ResetableData.DailyCounselCount = db.Set<UserDailyCounselCountEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.KeyId, x => x.Count);
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        var resetStateSet = db.Set<UserResetStateEntity>();
        var resetState = resetStateSet.FirstOrDefault(x => x.UserId == userId);
        if (resetState == null)
        {
            resetState = new UserResetStateEntity { UserId = userId };
            resetStateSet.Add(resetState);
        }

        var daily = user.ResetableData ?? new ResetableData();
        var weekly = user.WeeklyResetableData ?? new WeeklyResetableData();

        resetState.WipeoutCount = daily.WipeoutCount;
        resetState.ClearedSimulationRoom = daily.ClearedSimulationRoom;
        resetState.InterceptionTickets = daily.InterceptionTickets;
        resetState.DailyMissionPoints = daily.DailyMissionPoints;
        resetState.DispatchCount = daily.DispatchCount;
        resetState.WeeklyMissionPoints = weekly.WeeklyMissionPoints;
        resetState.SimRoomStateJson = JsonConvert.SerializeObject(daily.SimRoomData ?? new SimRoomData());

        DomainStoreHelpers.SyncCollection(
            db.Set<UserCompletedDailyMissionEntity>(),
            db.Set<UserCompletedDailyMissionEntity>().Where(x => x.UserId == userId),
            daily.CompletedDailyMissions ?? [],
            x => x.MissionId,
            id => id,
            id => new UserCompletedDailyMissionEntity { UserId = userId, MissionId = id },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserCompletedWeeklyMissionEntity>(),
            db.Set<UserCompletedWeeklyMissionEntity>().Where(x => x.UserId == userId),
            weekly.CompletedWeeklyMissions ?? [],
            x => x.MissionId,
            id => id,
            id => new UserCompletedWeeklyMissionEntity { UserId = userId, MissionId = id },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserDailyTowerCountEntity>(),
            db.Set<UserDailyTowerCountEntity>().Where(x => x.UserId == userId),
            daily.TowerCount ?? [],
            x => x.CorporationType,
            kv => (int)kv.Key,
            kv => new UserDailyTowerCountEntity
            {
                UserId = userId,
                CorporationType = (int)kv.Key,
                UsedCount = kv.Value
            },
            (entity, kv) => entity.UsedCount = kv.Value);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserDailyCounselCountEntity>(),
            db.Set<UserDailyCounselCountEntity>().Where(x => x.UserId == userId),
            daily.DailyCounselCount ?? [],
            x => x.KeyId,
            kv => kv.Key,
            kv => new UserDailyCounselCountEntity { UserId = userId, KeyId = kv.Key, Count = kv.Value },
            (entity, kv) => entity.Count = kv.Value);
    }
}
