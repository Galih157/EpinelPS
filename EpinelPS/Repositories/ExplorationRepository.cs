using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Repositories;

public sealed class ExplorationRepository(GameContext db)
{
    public const string ArchiveKindRecord = "Record";
    public const string ArchiveKindEventQuest = "EventQuest";

    // --- Achievements ---

    public Task<List<int>> GetCompletedAchievementsAsync(ulong userId, CancellationToken ct = default)
    {
        return db.Set<UserCompletedAchievement>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.AchievementId)
            .Select(x => x.AchievementId)
            .ToListAsync(ct);
    }

    public async Task<HashSet<int>> GetCompletedAchievementSetAsync(ulong userId, CancellationToken ct = default)
    {
        var ids = await GetCompletedAchievementsAsync(userId, ct);
        return ids.ToHashSet();
    }

    public async Task AddCompletedAchievementsAsync(ulong userId, IEnumerable<int> achievementIds, CancellationToken ct = default)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserCompletedAchievement>();
        var existing = await set.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.AchievementId)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        bool added = false;
        foreach (int id in achievementIds.Distinct())
        {
            if (!existingSet.Add(id))
                continue;

            set.Add(new UserCompletedAchievement
            {
                UserId = userId,
                AchievementId = id
            });
            added = true;
        }

        if (added)
            await db.SaveChangesAsync(ct);
    }

    // --- SideStory ---

    public async Task<(List<int> Completed, List<int> Viewed)> GetSideStoryStagesAsync(ulong userId, CancellationToken ct = default)
    {
        var rows = await db.Set<UserSideStoryStage>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.StageId)
            .ToListAsync(ct);

        return (
            rows.Where(x => x.IsCompleted).Select(x => x.StageId).ToList(),
            rows.Where(x => x.IsViewed).Select(x => x.StageId).ToList()
        );
    }

    public async Task<bool> MarkSideStoryCompletedAsync(ulong userId, int stageId, CancellationToken ct = default)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserSideStoryStage>();
        var row = await set.FirstOrDefaultAsync(x => x.UserId == userId && x.StageId == stageId, ct);
        if (row != null)
        {
            if (row.IsCompleted)
                return false;

            row.IsCompleted = true;
        }
        else
        {
            set.Add(new UserSideStoryStage
            {
                UserId = userId,
                StageId = stageId,
                IsCompleted = true,
                IsViewed = false
            });
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkSideStoriesViewedAsync(ulong userId, IEnumerable<int> stageIds, CancellationToken ct = default)
    {
        var distinctIds = stageIds.Distinct().ToList();
        if (distinctIds.Count == 0)
            return;

        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserSideStoryStage>();
        var existingByStage = await set
            .Where(x => x.UserId == userId && distinctIds.Contains(x.StageId))
            .ToDictionaryAsync(x => x.StageId, ct);

        bool changed = false;
        foreach (int stageId in distinctIds)
        {
            if (existingByStage.TryGetValue(stageId, out var row))
            {
                if (!row.IsViewed)
                {
                    row.IsViewed = true;
                    changed = true;
                }
            }
            else
            {
                set.Add(new UserSideStoryStage
                {
                    UserId = userId,
                    StageId = stageId,
                    IsCompleted = false,
                    IsViewed = true
                });
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync(ct);
    }

    // --- Archive Unlocks ---

    public async Task<(List<int> RecordIds, List<int> EventQuestIds)> GetArchiveUnlocksAsync(ulong userId, CancellationToken ct = default)
    {
        var rows = await db.Set<UserArchiveUnlock>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.TargetId)
            .ToListAsync(ct);

        return (
            rows.Where(x => x.Kind == ArchiveKindRecord).Select(x => x.TargetId).ToList(),
            rows.Where(x => x.Kind == ArchiveKindEventQuest).Select(x => x.TargetId).ToList()
        );
    }

    public Task<bool> IsArchiveUnlockedAsync(ulong userId, string kind, int targetId, CancellationToken ct = default)
    {
        return db.Set<UserArchiveUnlock>()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.Kind == kind && x.TargetId == targetId, ct);
    }

    public async Task<bool> UnlockArchiveAsync(ulong userId, string kind, int targetId, CancellationToken ct = default)
    {
        var set = db.Set<UserArchiveUnlock>();
        if (await set.AsNoTracking().AnyAsync(x => x.UserId == userId && x.Kind == kind && x.TargetId == targetId, ct))
            return false;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        set.Add(new UserArchiveUnlock
        {
            UserId = userId,
            Kind = kind,
            TargetId = targetId
        });
        await db.SaveChangesAsync(ct);
        return true;
    }

    // --- LostSector ---

    public async Task<Dictionary<int, LostSectorData>> GetAllLostSectorsAsync(ulong userId, CancellationToken ct = default)
    {
        var rows = await db.Set<UserLostSector>()
            .AsNoTracking()
            .Include(x => x.ClearedStages)
            .Include(x => x.Objects)
            .Where(x => x.UserId == userId)
            .AsSplitQuery()
            .ToListAsync(ct);

        var result = new Dictionary<int, LostSectorData>(rows.Count);
        foreach (var row in rows)
        {
            result[row.SectorId] = MapLostSector(row);
        }
        return result;
    }

    public async Task<LostSectorData?> GetLostSectorAsync(ulong userId, int sectorId, CancellationToken ct = default)
    {
        var row = await db.Set<UserLostSector>()
            .AsNoTracking()
            .Include(x => x.ClearedStages)
            .Include(x => x.Objects)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SectorId == sectorId, ct);

        return row != null ? MapLostSector(row) : null;
    }

    public async Task UpsertLostSectorAsync(ulong userId, int sectorId, LostSectorData data, CancellationToken ct = default)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var sectorSet = db.Set<UserLostSector>();
        var stageSet = db.Set<UserLostSectorClearedStage>();
        var objectSet = db.Set<UserLostSectorObject>();

        var row = await sectorSet
            .Include(x => x.ClearedStages)
            .Include(x => x.Objects)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SectorId == sectorId, ct);

        if (row == null)
        {
            row = new UserLostSector
            {
                UserId = userId,
                SectorId = sectorId,
                IsOpen = data.IsOpen,
                IsPlaying = data.IsPlaying,
                StateJson = data.Json ?? "",
                TeamPositionsJson = JsonConvert.SerializeObject(data.TeamPositions ?? []),
                ObtainedRewards = data.ObtainedRewards,
                ReceivedFinalReward = data.ReceivedFinalReward,
                CompletedPerfectly = data.CompletedPerfectly
            };
            sectorSet.Add(row);

            foreach (var stage in data.ClearedStages)
            {
                stageSet.Add(new UserLostSectorClearedStage
                {
                    UserId = userId,
                    LostSector = row,
                    StageKey = stage.Key,
                    ClearState = stage.Value
                });
            }

            foreach (var obj in data.Objects)
            {
                objectSet.Add(new UserLostSectorObject
                {
                    UserId = userId,
                    LostSector = row,
                    ObjectKey = obj.Key,
                    ActionAt = obj.Value.ActionAt,
                    Type = obj.Value.Type,
                    PositionId = obj.Value.PositionId ?? "",
                    Json = obj.Value.Json ?? "",
                    TeamPositionsJson = JsonConvert.SerializeObject(obj.Value.TeamPosition.ToList())
                });
            }
        }
        else
        {
            row.IsOpen = data.IsOpen;
            row.IsPlaying = data.IsPlaying;
            row.StateJson = data.Json ?? "";
            row.TeamPositionsJson = JsonConvert.SerializeObject(data.TeamPositions ?? []);
            row.ObtainedRewards = data.ObtainedRewards;
            row.ReceivedFinalReward = data.ReceivedFinalReward;
            row.CompletedPerfectly = data.CompletedPerfectly;

            var existingStages = row.ClearedStages.ToDictionary(s => s.StageKey);
            foreach (var existingStage in row.ClearedStages.Where(s => !data.ClearedStages.ContainsKey(s.StageKey)).ToList())
            {
                stageSet.Remove(existingStage);
            }
            foreach (var (stageKey, clearState) in data.ClearedStages)
            {
                if (existingStages.TryGetValue(stageKey, out var entity))
                {
                    entity.ClearState = clearState;
                }
                else
                {
                    stageSet.Add(new UserLostSectorClearedStage
                    {
                        UserId = userId,
                        LostSector = row,
                        StageKey = stageKey,
                        ClearState = clearState
                    });
                }
            }

            var existingObjects = row.Objects.ToDictionary(o => o.ObjectKey);
            foreach (var existingObj in row.Objects.Where(o => !data.Objects.ContainsKey(o.ObjectKey)).ToList())
            {
                objectSet.Remove(existingObj);
            }
            foreach (var (objKey, objVal) in data.Objects)
            {
                string teamPositionsJson = JsonConvert.SerializeObject(objVal.TeamPosition.ToList());
                if (existingObjects.TryGetValue(objKey, out var entity))
                {
                    entity.ActionAt = objVal.ActionAt;
                    entity.Type = objVal.Type;
                    entity.PositionId = objVal.PositionId ?? "";
                    entity.Json = objVal.Json ?? "";
                    entity.TeamPositionsJson = teamPositionsJson;
                }
                else
                {
                    objectSet.Add(new UserLostSectorObject
                    {
                        UserId = userId,
                        LostSector = row,
                        ObjectKey = objKey,
                        ActionAt = objVal.ActionAt,
                        Type = objVal.Type,
                        PositionId = objVal.PositionId ?? "",
                        Json = objVal.Json ?? "",
                        TeamPositionsJson = teamPositionsJson
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static LostSectorData MapLostSector(UserLostSector row)
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

        return new LostSectorData
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
