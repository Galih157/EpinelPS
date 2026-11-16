using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database.Domains;

public sealed class ExplorationStore : IUserDomainStore
{
    private const string ArchiveKindRecord = "Record";
    private const string ArchiveKindEventQuest = "EventQuest";

    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        user.CompletedAchievements = db.Set<UserCompletedAchievement>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.AchievementId)
            .ToList();

        var sideStoryRows = db.Set<UserSideStoryStage>().AsNoTracking().Where(x => x.UserId == userId).ToList();
        user.CompletedSideStoryStages = sideStoryRows.Where(x => x.IsCompleted).Select(x => x.StageId).ToList();
        user.ViewedSideStoryStages = sideStoryRows.Where(x => x.IsViewed).Select(x => x.StageId).ToList();

        var archiveRows = db.Set<UserArchiveUnlock>().AsNoTracking().Where(x => x.UserId == userId).ToList();
        user.UnlockedArchiveRecordIds = archiveRows.Where(x => x.Kind == ArchiveKindRecord).Select(x => x.TargetId).ToList();
        user.UnlockedArchiveEventQuestIds = archiveRows.Where(x => x.Kind == ArchiveKindEventQuest).Select(x => x.TargetId).ToList();

        var lostSectorRows = db.Set<UserLostSector>().AsNoTracking()
            .Include(x => x.ClearedStages)
            .Include(x => x.Objects)
            .Where(x => x.UserId == userId)
            .ToList();
        user.LostSectorData = new Dictionary<int, LostSectorData>(lostSectorRows.Count);
        foreach (var row in lostSectorRows)
        {
            var teamPositions = string.IsNullOrEmpty(row.TeamPositionsJson)
                ? []
                : JsonConvert.DeserializeObject<List<NetLostSectorTeamPosition>>(row.TeamPositionsJson) ?? [];

            var objects = new Dictionary<string, NetLostSectorFieldObject>(row.Objects.Count);
            foreach (var objRow in row.Objects)
            {
                var fieldObj = new NetLostSectorFieldObject
                {
                    ActionAt = objRow.ActionAt,
                    Type = objRow.Type,
                    PositionId = objRow.PositionId ?? "",
                    Json = objRow.Json ?? ""
                };
                var objTeamPositions = string.IsNullOrEmpty(objRow.TeamPositionsJson)
                    ? null
                    : JsonConvert.DeserializeObject<List<NetLostSectorTeamPosition>>(objRow.TeamPositionsJson);
                if (objTeamPositions != null)
                    fieldObj.TeamPosition.AddRange(objTeamPositions);

                objects[objRow.ObjectKey] = fieldObj;
            }

            user.LostSectorData[row.SectorId] = new LostSectorData
            {
                IsOpen = row.IsOpen,
                IsPlaying = row.IsPlaying,
                Json = row.StateJson ?? "",
                TeamPositions = teamPositions,
                ObtainedRewards = row.ObtainedRewards,
                ReceivedFinalReward = row.ReceivedFinalReward,
                CompletedPerfectly = row.CompletedPerfectly,
                ClearedStages = row.ClearedStages.ToDictionary(s => s.StageKey, s => s.ClearState),
                Objects = objects
            };
        }
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        DomainStoreHelpers.SyncCollection(
            db.Set<UserCompletedAchievement>(),
            db.Set<UserCompletedAchievement>().Where(x => x.UserId == userId).ToList(),
            user.CompletedAchievements,
            e => e.AchievementId,
            id => id,
            id => new UserCompletedAchievement
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                AchievementId = id
            },
            (_, _) => { });

        var completedSideStorySet = user.CompletedSideStoryStages.ToHashSet();
        var viewedSideStorySet = user.ViewedSideStoryStages.ToHashSet();
        var allSideStoryIds = completedSideStorySet.Union(viewedSideStorySet);
        DomainStoreHelpers.SyncCollection(
            db.Set<UserSideStoryStage>(),
            db.Set<UserSideStoryStage>().Where(x => x.UserId == userId).ToList(),
            allSideStoryIds,
            e => e.StageId,
            id => id,
            id => new UserSideStoryStage
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                StageId = id,
                IsCompleted = completedSideStorySet.Contains(id),
                IsViewed = viewedSideStorySet.Contains(id)
            },
            (e, id) =>
            {
                e.IsCompleted = completedSideStorySet.Contains(id);
                e.IsViewed = viewedSideStorySet.Contains(id);
            });

        var desiredArchiveUnlocks = user.UnlockedArchiveRecordIds.Select(id => (Kind: ArchiveKindRecord, TargetId: id))
            .Concat(user.UnlockedArchiveEventQuestIds.Select(id => (Kind: ArchiveKindEventQuest, TargetId: id)));
        DomainStoreHelpers.SyncCollection(
            db.Set<UserArchiveUnlock>(),
            db.Set<UserArchiveUnlock>().Where(x => x.UserId == userId).ToList(),
            desiredArchiveUnlocks,
            e => (e.Kind, e.TargetId),
            s => s,
            s => new UserArchiveUnlock
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Kind = s.Kind,
                TargetId = s.TargetId
            },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserLostSector>(),
            db.Set<UserLostSector>().Include(x => x.ClearedStages).Include(x => x.Objects).Where(x => x.UserId == userId).ToList(),
            user.LostSectorData,
            e => e.SectorId,
            kvp => kvp.Key,
            kvp =>
            {
                var ls = new UserLostSector
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    SectorId = kvp.Key,
                    IsOpen = kvp.Value.IsOpen,
                    IsPlaying = kvp.Value.IsPlaying,
                    StateJson = kvp.Value.Json ?? "",
                    TeamPositionsJson = JsonConvert.SerializeObject(kvp.Value.TeamPositions ?? []),
                    ObtainedRewards = kvp.Value.ObtainedRewards,
                    ReceivedFinalReward = kvp.Value.ReceivedFinalReward,
                    CompletedPerfectly = kvp.Value.CompletedPerfectly
                };
                foreach (var stage in kvp.Value.ClearedStages)
                {
                    db.Set<UserLostSectorClearedStage>().Add(new UserLostSectorClearedStage
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LostSectorId = ls.Id,
                        StageKey = stage.Key,
                        ClearState = stage.Value
                    });
                }
                foreach (var obj in kvp.Value.Objects)
                {
                    db.Set<UserLostSectorObject>().Add(new UserLostSectorObject
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LostSectorId = ls.Id,
                        ObjectKey = obj.Key,
                        ActionAt = obj.Value.ActionAt,
                        Type = obj.Value.Type,
                        PositionId = obj.Value.PositionId ?? "",
                        Json = obj.Value.Json ?? "",
                        TeamPositionsJson = JsonConvert.SerializeObject(obj.Value.TeamPosition.ToList())
                    });
                }
                return ls;
            },
            (e, kvp) =>
            {
                e.IsOpen = kvp.Value.IsOpen;
                e.IsPlaying = kvp.Value.IsPlaying;
                e.StateJson = kvp.Value.Json ?? "";
                e.TeamPositionsJson = JsonConvert.SerializeObject(kvp.Value.TeamPositions ?? []);
                e.ObtainedRewards = kvp.Value.ObtainedRewards;
                e.ReceivedFinalReward = kvp.Value.ReceivedFinalReward;
                e.CompletedPerfectly = kvp.Value.CompletedPerfectly;

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserLostSectorClearedStage>(),
                    e.ClearedStages,
                    kvp.Value.ClearedStages,
                    s => s.StageKey,
                    s => s.Key,
                    s => new UserLostSectorClearedStage
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LostSectorId = e.Id,
                        StageKey = s.Key,
                        ClearState = s.Value
                    },
                    (s, src) => s.ClearState = src.Value);

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserLostSectorObject>(),
                    e.Objects,
                    kvp.Value.Objects,
                    o => o.ObjectKey,
                    o => o.Key,
                    o => new UserLostSectorObject
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LostSectorId = e.Id,
                        ObjectKey = o.Key,
                        ActionAt = o.Value.ActionAt,
                        Type = o.Value.Type,
                        PositionId = o.Value.PositionId ?? "",
                        Json = o.Value.Json ?? "",
                        TeamPositionsJson = JsonConvert.SerializeObject(o.Value.TeamPosition.ToList())
                    },
                    (o, src) =>
                    {
                        o.ActionAt = src.Value.ActionAt;
                        o.Type = src.Value.Type;
                        o.PositionId = src.Value.PositionId ?? "";
                        o.Json = src.Value.Json ?? "";
                        o.TeamPositionsJson = JsonConvert.SerializeObject(src.Value.TeamPosition.ToList());
                    });
            });
    }
}
