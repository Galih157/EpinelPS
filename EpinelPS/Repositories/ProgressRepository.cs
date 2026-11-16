using System.Linq.Expressions;
using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Repositories;

/// <summary>
/// Repository for commander level/guild fields, campaign scenarios/stages, currencies, quests,
/// tower progress, tutorials, contents unlocks, notice popups, map/field state, and stage clear history.
/// Mutations stage changes on <see cref="GameContext"/> without calling <c>SaveChanges</c> so callers
/// can commit multiple operations in a single unit of work.
/// </summary>
public sealed class ProgressRepository(GameContext db)
{
    // --- Core Session Fields (userPointData, Guild, Currency) ---

    public void LoadUserCore(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ulong userId = user.ID;

        var gameUser = FindGameUserReadOnly(userId);
        user.userPointData ??= new UserPointData();
        user.Guild ??= new GuildData();

        if (gameUser != null)
        {
            user.userPointData.UserLevel = gameUser.UserLevel;
            user.userPointData.ExperiencePoint = gameUser.ExperiencePoint;
            user.Guild.guildId = gameUser.GuildId;
            user.Guild.LeaveAt = gameUser.GuildLeaveAt;
        }

        var currencies = GetCurrencies(userId);
        if (currencies.Count > 0)
            user.Currency = currencies;
    }

    public void SaveUserCore(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ulong userId = user.ID;

        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.UserLevel = user.userPointData?.UserLevel ?? 1;
        gameUser.ExperiencePoint = user.userPointData?.ExperiencePoint ?? 0;
        gameUser.GuildId = user.Guild?.guildId;
        gameUser.GuildLeaveAt = user.Guild?.LeaveAt;

        SyncCollection(
            db.Set<UserCurrencyEntity>(),
            db.Set<UserCurrencyEntity>().Where(x => x.UserId == userId),
            user.Currency ?? [],
            x => x.CurrencyType,
            kv => (int)kv.Key,
            kv => new UserCurrencyEntity
            {
                UserId = userId,
                CurrencyType = (int)kv.Key,
                Amount = kv.Value
            },
            (entity, kv) => entity.Amount = kv.Value);
    }

    // --- GameUser Progress Fields ---

    public UserPointData GetUserPointData(ulong userId)
    {
        var gameUser = FindGameUserReadOnly(userId);
        return new UserPointData
        {
            UserLevel = gameUser?.UserLevel ?? 1,
            ExperiencePoint = gameUser?.ExperiencePoint ?? 0
        };
    }

    public void SaveUserPointData(ulong userId, UserPointData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.UserLevel = data.UserLevel;
        gameUser.ExperiencePoint = data.ExperiencePoint;
    }

    public GuildData GetGuildData(ulong userId)
    {
        var gameUser = FindGameUserReadOnly(userId);
        return new GuildData
        {
            guildId = gameUser?.GuildId,
            LeaveAt = gameUser?.GuildLeaveAt
        };
    }

    public void SaveGuildData(ulong userId, GuildData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.GuildId = data.guildId;
        gameUser.GuildLeaveAt = data.LeaveAt;
    }

    // --- Scenarios & Stages ---

    public List<string> GetCompletedScenarios(ulong userId)
    {
        return GetMergedEntities<UserCompletedScenarioEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .Select(x => x.ScenarioId)
            .ToList();
    }

    public bool HasCompletedScenario(ulong userId, string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            return false;

        return GetMergedEntities<UserCompletedScenarioEntity>(
                x => x.UserId == userId && x.ScenarioId == scenarioId,
                x => x.UserId == userId && x.ScenarioId == scenarioId,
                x => x.Id)
            .Count > 0;
    }

    public bool IsStageCompleted(ulong userId, int stageId)
    {
        var fieldRows = GetMergedEntities<UserFieldMapEntity>(
            x => x.UserId == userId,
            x => x.UserId == userId,
            x => x.Id);

        foreach (var row in fieldRows)
        {
            var fieldInfo = DeserializeFieldInfo(row.DataJson);
            if (fieldInfo.CompletedStages.Contains(stageId))
                return true;
        }

        return false;
    }

    public bool CompleteStage(ulong userId, string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            return false;

        return CompleteStages(userId, [scenarioId]) > 0;
    }

    public int CompleteStages(ulong userId, IEnumerable<string> scenarioIds)
    {
        ArgumentNullException.ThrowIfNull(scenarioIds);

        var distinctIds = scenarioIds
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();
        if (distinctIds.Count == 0)
            return 0;

        var existing = GetMergedEntities<UserCompletedScenarioEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .Select(x => x.ScenarioId)
            .ToHashSet();

        int added = 0;
        bool ensuredUser = false;
        var set = db.Set<UserCompletedScenarioEntity>();

        foreach (string scenarioId in distinctIds)
        {
            if (!existing.Add(scenarioId))
                continue;

            if (!ensuredUser)
            {
                UserDbHelper.GetOrCreateGameUser(db, userId);
                ensuredUser = true;
            }

            set.Add(new UserCompletedScenarioEntity
            {
                UserId = userId,
                ScenarioId = scenarioId
            });
            added++;
        }

        return added;
    }

    public bool CompleteStage(ulong userId, int stageId)
    {
        string mapId = ResolveStageMapId(stageId);
        return CompleteStage(userId, mapId, stageId);
    }

    public bool CompleteStage(ulong userId, string mapId, int stageId)
    {
        return CompleteStages(userId, mapId, [stageId]) > 0;
    }

    public int CompleteStages(ulong userId, IEnumerable<int> stageIds)
    {
        ArgumentNullException.ThrowIfNull(stageIds);

        var stagesByMap = stageIds
            .Distinct()
            .GroupBy(ResolveStageMapId);

        int added = 0;
        foreach (var group in stagesByMap)
            added += CompleteStages(userId, group.Key, group);

        return added;
    }

    public int CompleteStages(ulong userId, string mapId, IEnumerable<int> stageIds)
    {
        ArgumentException.ThrowIfNullOrEmpty(mapId);
        ArgumentNullException.ThrowIfNull(stageIds);

        var distinctStages = stageIds.Distinct().ToList();
        if (distinctStages.Count == 0)
            return 0;

        var fieldInfo = GetFieldInfo(userId, mapId) ?? new FieldInfoNew();
        var existing = fieldInfo.CompletedStages.ToHashSet();

        int added = 0;
        foreach (int stageId in distinctStages)
        {
            if (existing.Add(stageId))
            {
                fieldInfo.CompletedStages.Add(stageId);
                added++;
            }
        }

        if (added > 0 || GetFieldInfo(userId, mapId) == null)
            SaveFieldInfo(userId, mapId, fieldInfo);

        return added;
    }

    // --- Currencies ---

    public Dictionary<CurrencyType, long> GetCurrencies(ulong userId)
    {
        return GetMergedEntities<UserCurrencyEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .ToDictionary(x => (CurrencyType)x.CurrencyType, x => x.Amount);
    }

    public long GetCurrencyVal(ulong userId, CurrencyType type)
    {
        int typeInt = (int)type;
        var entity = GetMergedEntities<UserCurrencyEntity>(
                x => x.UserId == userId && x.CurrencyType == typeInt,
                x => x.UserId == userId && x.CurrencyType == typeInt,
                x => x.Id)
            .FirstOrDefault();

        return entity?.Amount ?? 0L;
    }

    public bool CanSubtractCurrency(ulong userId, CurrencyType type, long val)
    {
        if (val < 0)
            return false;
        if (val == 0)
            return true;

        if (type == CurrencyType.FreeCash)
            return GetCurrencyVal(userId, CurrencyType.FreeCash) + GetCurrencyVal(userId, CurrencyType.ChargeCash) >= val;

        return GetCurrencyVal(userId, type) >= val;
    }

    public long AddCurrency(ulong userId, CurrencyType type, long val)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        int typeInt = (int)type;
        var entity = FindTrackedEntity<UserCurrencyEntity>(
            x => x.UserId == userId && x.CurrencyType == typeInt,
            x => x.UserId == userId && x.CurrencyType == typeInt);

        if (entity == null)
        {
            entity = new UserCurrencyEntity
            {
                UserId = userId,
                CurrencyType = typeInt,
                Amount = val
            };
            db.Set<UserCurrencyEntity>().Add(entity);
        }
        else
        {
            entity.Amount += val;
        }

        return entity.Amount;
    }

    public bool SubtractCurrency(ulong userId, CurrencyType type, long val)
    {
        if (val < 0)
            return false;
        if (val == 0)
            return true;

        if (type == CurrencyType.FreeCash)
        {
            long freeCash = GetCurrencyVal(userId, CurrencyType.FreeCash);
            if (freeCash >= val)
            {
                var freeEntity = FindTrackedEntity<UserCurrencyEntity>(
                    x => x.UserId == userId && x.CurrencyType == (int)CurrencyType.FreeCash,
                    x => x.UserId == userId && x.CurrencyType == (int)CurrencyType.FreeCash);

                if (freeEntity == null || freeEntity.Amount < val)
                    return false;

                freeEntity.Amount -= val;
                return true;
            }

            long diff = val - freeCash;
            var chargeEntity = FindTrackedEntity<UserCurrencyEntity>(
                x => x.UserId == userId && x.CurrencyType == (int)CurrencyType.ChargeCash,
                x => x.UserId == userId && x.CurrencyType == (int)CurrencyType.ChargeCash);

            if (chargeEntity == null || chargeEntity.Amount < diff)
                return false;

            if (freeCash > 0)
            {
                var freeEntity = FindTrackedEntity<UserCurrencyEntity>(
                    x => x.UserId == userId && x.CurrencyType == (int)CurrencyType.FreeCash,
                    x => x.UserId == userId && x.CurrencyType == (int)CurrencyType.FreeCash);

                if (freeEntity != null)
                    freeEntity.Amount = 0;
            }

            chargeEntity.Amount -= diff;
            return true;
        }

        int typeInt = (int)type;
        var entity = FindTrackedEntity<UserCurrencyEntity>(
            x => x.UserId == userId && x.CurrencyType == typeInt,
            x => x.UserId == userId && x.CurrencyType == typeInt);

        if (entity == null || entity.Amount < val)
            return false;

        entity.Amount -= val;
        return true;
    }

    public void SetCurrency(ulong userId, CurrencyType type, long val)
    {
        int typeInt = (int)type;
        UpsertEntity<UserCurrencyEntity>(
            userId,
            x => x.UserId == userId && x.CurrencyType == typeInt,
            x => x.UserId == userId && x.CurrencyType == typeInt,
            () => new UserCurrencyEntity
            {
                UserId = userId,
                CurrencyType = typeInt,
                Amount = val
            },
            entity => entity.Amount = val);
    }

    // --- Quests ---

    public Dictionary<int, bool> GetMainQuests(ulong userId)
    {
        return GetMergedEntities<UserMainQuestEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .ToDictionary(x => x.QuestTid, x => x.ReceivedReward);
    }

    public void UpsertMainQuest(ulong userId, int questTid, bool receivedReward)
    {
        UpsertEntity<UserMainQuestEntity>(
            userId,
            x => x.UserId == userId && x.QuestTid == questTid,
            x => x.UserId == userId && x.QuestTid == questTid,
            () => new UserMainQuestEntity
            {
                UserId = userId,
                QuestTid = questTid,
                ReceivedReward = receivedReward
            },
            entity => entity.ReceivedReward = receivedReward);
    }

    public int RemoveMainQuests(ulong userId, IEnumerable<int> questTids)
    {
        ArgumentNullException.ThrowIfNull(questTids);

        var tidSet = questTids.ToHashSet();
        if (tidSet.Count == 0)
            return 0;

        var set = db.Set<UserMainQuestEntity>();
        var rows = set.Where(x => x.UserId == userId && tidSet.Contains(x.QuestTid)).ToList();

        foreach (var local in set.Local)
        {
            if (local.UserId == userId && tidSet.Contains(local.QuestTid) && !rows.Contains(local))
                rows.Add(local);
        }

        rows = rows.Where(r => db.Entry(r).State != EntityState.Deleted).ToList();
        if (rows.Count > 0)
            set.RemoveRange(rows);

        return rows.Count;
    }

    public Dictionary<int, bool> GetSubQuests(ulong userId)
    {
        return GetMergedEntities<UserSubQuestEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .ToDictionary(x => x.SubQuestId, x => x.ReceivedReward);
    }

    public void UpsertSubQuest(ulong userId, int subQuestId, bool receivedReward)
    {
        UpsertEntity<UserSubQuestEntity>(
            userId,
            x => x.UserId == userId && x.SubQuestId == subQuestId,
            x => x.UserId == userId && x.SubQuestId == subQuestId,
            () => new UserSubQuestEntity
            {
                UserId = userId,
                SubQuestId = subQuestId,
                ReceivedReward = receivedReward
            },
            entity => entity.ReceivedReward = receivedReward);
    }

    // --- Tower Progress ---

    public Dictionary<CorporationTowerType, int> GetTowerProgress(ulong userId)
    {
        return GetMergedEntities<UserTowerProgressEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .ToDictionary(x => (CorporationTowerType)x.CorporationType, x => x.ClearedFloor);
    }

    public void SetTowerFloor(ulong userId, CorporationTowerType towerType, int floor)
    {
        int corpType = (int)towerType;
        UpsertEntity<UserTowerProgressEntity>(
            userId,
            x => x.UserId == userId && x.CorporationType == corpType,
            x => x.UserId == userId && x.CorporationType == corpType,
            () => new UserTowerProgressEntity
            {
                UserId = userId,
                CorporationType = corpType,
                ClearedFloor = floor
            },
            entity => entity.ClearedFloor = floor);
    }

    // --- Tutorials ---

    public Dictionary<int, ClearedTutorialData> GetClearedTutorials(ulong userId)
    {
        return GetMergedEntities<UserClearedTutorialEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .ToDictionary(
                x => x.TutorialGroupId,
                x => new ClearedTutorialData
                {
                    Id = x.TutorialId,
                    VersionGroup = x.VersionGroup
                });
    }

    public void UpsertTutorial(ulong userId, int groupId, ClearedTutorialData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        UpsertTutorial(userId, groupId, data.Id, data.VersionGroup);
    }

    public void UpsertTutorial(ulong userId, int groupId, int tutorialId, int versionGroup)
    {
        UpsertEntity<UserClearedTutorialEntity>(
            userId,
            x => x.UserId == userId && x.TutorialGroupId == groupId,
            x => x.UserId == userId && x.TutorialGroupId == groupId,
            () => new UserClearedTutorialEntity
            {
                UserId = userId,
                TutorialGroupId = groupId,
                TutorialId = tutorialId,
                VersionGroup = versionGroup
            },
            entity =>
            {
                entity.TutorialId = tutorialId;
                entity.VersionGroup = versionGroup;
            });
    }

    // --- Contents Open ---

    public Dictionary<int, UnlockData> GetContentsUnlocked(ulong userId)
    {
        return GetMergedEntities<UserContentsUnlockEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .ToDictionary(
                x => x.ContentsOpenId,
                x => new UnlockData(x.ButtonAnimationPlayed, x.PopupAnimationPlayed));
    }

    public void UpsertContentsUnlock(ulong userId, int contentsOpenId, UnlockData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        UpsertContentsUnlock(userId, contentsOpenId, data.ButtonAnimationPlayed, data.PopupAnimationPlayed);
    }

    public void UpsertContentsUnlock(ulong userId, int contentsOpenId, bool buttonAnimationPlayed, bool popupAnimationPlayed)
    {
        UpsertEntity<UserContentsUnlockEntity>(
            userId,
            x => x.UserId == userId && x.ContentsOpenId == contentsOpenId,
            x => x.UserId == userId && x.ContentsOpenId == contentsOpenId,
            () => new UserContentsUnlockEntity
            {
                UserId = userId,
                ContentsOpenId = contentsOpenId,
                ButtonAnimationPlayed = buttonAnimationPlayed,
                PopupAnimationPlayed = popupAnimationPlayed
            },
            entity =>
            {
                entity.ButtonAnimationPlayed = buttonAnimationPlayed;
                entity.PopupAnimationPlayed = popupAnimationPlayed;
            });
    }

    // --- Notice Popups ---

    public Dictionary<int, List<int>> GetViewedNoticePopups(ulong userId)
    {
        return GetMergedEntities<UserViewedNoticePopupEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.NoticePopupTableId).ToList());
    }

    public List<int> GetViewedNoticePopups(ulong userId, int categoryId)
    {
        return GetMergedEntities<UserViewedNoticePopupEntity>(
                x => x.UserId == userId && x.CategoryId == categoryId,
                x => x.UserId == userId && x.CategoryId == categoryId,
                x => x.Id)
            .Select(x => x.NoticePopupTableId)
            .ToList();
    }

    public bool AddViewedNoticePopup(ulong userId, int categoryId, int noticePopupTableId)
    {
        return AddViewedNoticePopups(userId, categoryId, [noticePopupTableId]) > 0;
    }

    public int AddViewedNoticePopups(ulong userId, int categoryId, IEnumerable<int> noticePopupTableIds)
    {
        ArgumentNullException.ThrowIfNull(noticePopupTableIds);

        var distinctIds = noticePopupTableIds.Distinct().ToList();
        if (distinctIds.Count == 0)
            return 0;

        var existing = GetMergedEntities<UserViewedNoticePopupEntity>(
                x => x.UserId == userId && x.CategoryId == categoryId,
                x => x.UserId == userId && x.CategoryId == categoryId,
                x => x.Id)
            .Select(x => x.NoticePopupTableId)
            .ToHashSet();

        int added = 0;
        bool ensuredUser = false;
        var set = db.Set<UserViewedNoticePopupEntity>();

        foreach (int popupId in distinctIds)
        {
            if (!existing.Add(popupId))
                continue;

            if (!ensuredUser)
            {
                UserDbHelper.GetOrCreateGameUser(db, userId);
                ensuredUser = true;
            }

            set.Add(new UserViewedNoticePopupEntity
            {
                UserId = userId,
                CategoryId = categoryId,
                NoticePopupTableId = popupId
            });
            added++;
        }

        return added;
    }

    // --- Map & Field State ---

    public Dictionary<string, string> GetMapJsonMap(ulong userId)
    {
        return GetMergedEntities<UserMapStateEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .ToDictionary(x => x.MapId, x => x.StateJson);
    }

    public string? GetMapJson(ulong userId, string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return null;

        var row = GetMergedEntities<UserMapStateEntity>(
                x => x.UserId == userId && x.MapId == mapId,
                x => x.UserId == userId && x.MapId == mapId,
                x => x.Id)
            .FirstOrDefault();

        return row?.StateJson;
    }

    public void SaveMapJson(ulong userId, string mapId, string stateJson)
    {
        ArgumentException.ThrowIfNullOrEmpty(mapId);
        string resolvedJson = stateJson ?? string.Empty;

        UpsertEntity<UserMapStateEntity>(
            userId,
            x => x.UserId == userId && x.MapId == mapId,
            x => x.UserId == userId && x.MapId == mapId,
            () => new UserMapStateEntity
            {
                UserId = userId,
                MapId = mapId,
                StateJson = resolvedJson
            },
            entity => entity.StateJson = resolvedJson);
    }

    public bool RemoveMapJson(ulong userId, string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return false;

        var entity = FindTrackedEntity<UserMapStateEntity>(
            x => x.UserId == userId && x.MapId == mapId,
            x => x.UserId == userId && x.MapId == mapId);

        if (entity == null)
            return false;

        db.Set<UserMapStateEntity>().Remove(entity);
        return true;
    }

    public Dictionary<string, FieldInfoNew> GetFieldInfoMap(ulong userId)
    {
        return GetMergedEntities<UserFieldMapEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .ToDictionary(x => x.MapId, x => DeserializeFieldInfo(x.DataJson));
    }

    public FieldInfoNew? GetFieldInfo(ulong userId, string mapId)
    {
        if (string.IsNullOrEmpty(mapId))
            return null;

        var row = GetMergedEntities<UserFieldMapEntity>(
                x => x.UserId == userId && x.MapId == mapId,
                x => x.UserId == userId && x.MapId == mapId,
                x => x.Id)
            .FirstOrDefault();

        return row != null ? DeserializeFieldInfo(row.DataJson) : null;
    }

    public void SaveFieldInfo(ulong userId, string mapId, FieldInfoNew fieldInfo)
    {
        ArgumentException.ThrowIfNullOrEmpty(mapId);
        ArgumentNullException.ThrowIfNull(fieldInfo);

        string json = JsonConvert.SerializeObject(fieldInfo);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        UpsertEntity<UserFieldMapEntity>(
            userId,
            x => x.UserId == userId && x.MapId == mapId,
            x => x.UserId == userId && x.MapId == mapId,
            () => new UserFieldMapEntity
            {
                UserId = userId,
                MapId = mapId,
                DataJson = json,
                UpdatedAt = now
            },
            entity =>
            {
                if (entity.DataJson != json)
                {
                    entity.DataJson = json;
                    entity.UpdatedAt = now;
                }
            });
    }

    // --- Stage Clear History ---

    public List<NetStageClearInfo> GetStageClearHistory(ulong userId)
    {
        var rows = GetMergedEntities<UserStageClearHistoryEntity>(
                x => x.UserId == userId,
                x => x.UserId == userId,
                x => x.Id)
            .OrderBy(x => x.ClearedAt)
            .ToList();

        var result = new List<NetStageClearInfo>(rows.Count);
        foreach (var row in rows)
        {
            var info = new NetStageClearInfo
            {
                TeamCombat = row.TeamCombat,
                ClearedAt = row.ClearedAt,
                User = string.IsNullOrWhiteSpace(row.UserJson)
                    ? new NetWholeUserData()
                    : JsonConvert.DeserializeObject<NetWholeUserData>(row.UserJson) ?? new NetWholeUserData()
            };

            if (!string.IsNullOrWhiteSpace(row.SlotsJson))
            {
                var slots = JsonConvert.DeserializeObject<NetStageClearInfoTeam[]>(row.SlotsJson);
                if (slots != null)
                    info.Slots.AddRange(slots);
            }

            result.Add(info);
        }

        return result;
    }

    public void AddStageClearHistory(ulong userId, NetStageClearInfo entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        UserDbHelper.GetOrCreateGameUser(db, userId);

        db.Set<UserStageClearHistoryEntity>().Add(new UserStageClearHistoryEntity
        {
            UserId = userId,
            TeamCombat = entry.TeamCombat,
            ClearedAt = entry.ClearedAt,
            UserJson = JsonConvert.SerializeObject(entry.User ?? new NetWholeUserData()),
            SlotsJson = JsonConvert.SerializeObject(entry.Slots.ToArray())
        });
    }

    // --- Helpers ---

    private static void SyncCollection<TEntity, TKey, TSource>(
        DbSet<TEntity> dbSet,
        IEnumerable<TEntity> existingRows,
        IEnumerable<TSource> desiredItems,
        Func<TEntity, TKey> entityKey,
        Func<TSource, TKey> sourceKey,
        Func<TSource, TEntity> createEntity,
        Action<TEntity, TSource> updateEntity)
        where TEntity : class
        where TKey : notnull
    {
        var existingByKey = existingRows.ToDictionary(entityKey);
        var seenKeys = new HashSet<TKey>();

        foreach (var item in desiredItems)
        {
            var key = sourceKey(item);
            if (!seenKeys.Add(key))
                continue;

            if (existingByKey.TryGetValue(key, out var existing))
            {
                updateEntity(existing, item);
            }
            else
            {
                dbSet.Add(createEntity(item));
            }
        }

        foreach (var (key, existing) in existingByKey)
        {
            if (!seenKeys.Contains(key))
            {
                dbSet.Remove(existing);
            }
        }
    }

    private GameUser? FindGameUserReadOnly(ulong userId)
    {
        return db.Users.Local.FirstOrDefault(u => u.ID == userId)
            ?? db.Users.AsNoTracking().FirstOrDefault(u => u.ID == userId);
    }

    private void UpsertEntity<TEntity>(
        ulong userId,
        Func<TEntity, bool> localPredicate,
        Expression<Func<TEntity, bool>> dbPredicate,
        Func<TEntity> create,
        Action<TEntity> update) where TEntity : class
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var entity = FindTrackedEntity(localPredicate, dbPredicate);
        if (entity == null)
        {
            db.Set<TEntity>().Add(create());
        }
        else
        {
            update(entity);
        }
    }

    private List<TEntity> GetMergedEntities<TEntity>(
        Func<TEntity, bool> localPredicate,
        Expression<Func<TEntity, bool>> dbPredicate,
        Func<TEntity, long> keySelector) where TEntity : class
    {
        var tracked = db.ChangeTracker.Entries<TEntity>()
            .Where(e => localPredicate(e.Entity))
            .ToList();

        if (tracked.Count == 0)
        {
            return db.Set<TEntity>()
                .AsNoTracking()
                .Where(dbPredicate)
                .ToList();
        }

        var deletedKeys = new HashSet<long>();
        var trackedByKey = new Dictionary<long, TEntity>();
        var added = new List<TEntity>();

        foreach (var entry in tracked)
        {
            if (entry.State == EntityState.Deleted)
                deletedKeys.Add(keySelector(entry.Entity));
            else if (entry.State == EntityState.Added)
                added.Add(entry.Entity);
            else if (entry.State != EntityState.Detached)
                trackedByKey[keySelector(entry.Entity)] = entry.Entity;
        }

        var dbRows = db.Set<TEntity>()
            .AsNoTracking()
            .Where(dbPredicate)
            .ToList();

        var result = new List<TEntity>(dbRows.Count + added.Count);
        foreach (var row in dbRows)
        {
            long key = keySelector(row);
            if (deletedKeys.Contains(key))
                continue;

            result.Add(trackedByKey.TryGetValue(key, out var modified) ? modified : row);
        }

        result.AddRange(added);
        return result;
    }

    private TEntity? FindTrackedEntity<TEntity>(
        Func<TEntity, bool> localPredicate,
        Expression<Func<TEntity, bool>> dbPredicate) where TEntity : class
    {
        var set = db.Set<TEntity>();
        var local = set.Local.FirstOrDefault(localPredicate);
        if (local != null)
            return local;

        bool deletedInTracker = db.ChangeTracker.Entries<TEntity>()
            .Any(e => e.State == EntityState.Deleted && localPredicate(e.Entity));
        if (deletedInTracker)
            return null;

        return set.FirstOrDefault(dbPredicate);
    }

    private static string ResolveStageMapId(int stageId)
    {
        if (GameData.Instance?.StageDataRecords == null ||
            !GameData.Instance.StageDataRecords.TryGetValue(stageId, out var stage))
        {
            throw new InvalidOperationException($"Stage {stageId} not found in StageDataRecords.");
        }

        return GameData.Instance.GetMapIdFromChapter(stage.ChapterId, stage.ChapterMod);
    }

    private static FieldInfoNew DeserializeFieldInfo(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? new FieldInfoNew()
            : JsonConvert.DeserializeObject<FieldInfoNew>(json) ?? new FieldInfoNew();
    }
}
