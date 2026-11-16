using EpinelPS.Data;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database.Domains;

public sealed class ProgressStore : IUserDomainStore
{
    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        var gameUser = db.Set<GameUser>()
            .AsNoTracking()
            .FirstOrDefault(u => u.ID == userId);

        user.userPointData ??= new UserPointData();
        user.Guild ??= new GuildData();

        if (gameUser != null)
        {
            user.userPointData.UserLevel = gameUser.UserLevel;
            user.userPointData.ExperiencePoint = gameUser.ExperiencePoint;
            user.Guild.guildId = gameUser.GuildId;
            user.Guild.LeaveAt = gameUser.GuildLeaveAt;
        }

        user.CompletedScenarios = db.Set<UserCompletedScenarioEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ScenarioId)
            .ToList();

        var currencies = db.Set<UserCurrencyEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => (CurrencyType)x.CurrencyType, x => x.Amount);
        if (currencies.Count > 0)
            user.Currency = currencies;

        user.MainQuestData = db.Set<UserMainQuestEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.QuestTid, x => x.ReceivedReward);

        user.TowerProgress = db.Set<UserTowerProgressEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => (CorporationTowerType)x.CorporationType, x => x.ClearedFloor);

        user.ClearedTutorialDataNew = db.Set<UserClearedTutorialEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.TutorialGroupId,
                x => new ClearedTutorialData
                {
                    Id = x.TutorialId,
                    VersionGroup = x.VersionGroup
                });

        user.ContentsOpenUnlocked = db.Set<UserContentsUnlockEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.ContentsOpenId,
                x => new UnlockData(x.ButtonAnimationPlayed, x.PopupAnimationPlayed));

        user.ViewedNoticePopupTableIds = db.Set<UserViewedNoticePopupEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToList()
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.NoticePopupTableId).ToList());

        user.MapJson = db.Set<UserMapStateEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.MapId, x => x.StateJson);

        user.FieldInfoNew = db.Set<UserFieldMapEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.MapId,
                x => string.IsNullOrWhiteSpace(x.DataJson)
                    ? new FieldInfoNew()
                    : JsonConvert.DeserializeObject<FieldInfoNew>(x.DataJson) ?? new FieldInfoNew());

        user.SubQuestData = db.Set<UserSubQuestEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.SubQuestId, x => x.ReceivedReward);

        var stageClearRows = db.Set<UserStageClearHistoryEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ClearedAt)
            .ToList();

        user.StageClearHistorys = new List<NetStageClearInfo>(stageClearRows.Count);
        foreach (var row in stageClearRows)
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

            user.StageClearHistorys.Add(info);
        }
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.UserLevel = user.userPointData?.UserLevel ?? 1;
        gameUser.ExperiencePoint = user.userPointData?.ExperiencePoint ?? 0;
        gameUser.GuildId = user.Guild?.guildId;
        gameUser.GuildLeaveAt = user.Guild?.LeaveAt;

        DomainStoreHelpers.SyncCollection(
            db.Set<UserCompletedScenarioEntity>(),
            db.Set<UserCompletedScenarioEntity>().Where(x => x.UserId == userId),
            user.CompletedScenarios ?? [],
            x => x.ScenarioId,
            s => s,
            s => new UserCompletedScenarioEntity { UserId = userId, ScenarioId = s },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserCurrencyEntity>(),
            db.Set<UserCurrencyEntity>().Where(x => x.UserId == userId),
            user.Currency ?? [],
            x => x.CurrencyType,
            kv => (int)kv.Key,
            kv => new UserCurrencyEntity { UserId = userId, CurrencyType = (int)kv.Key, Amount = kv.Value },
            (entity, kv) => entity.Amount = kv.Value);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserMainQuestEntity>(),
            db.Set<UserMainQuestEntity>().Where(x => x.UserId == userId),
            user.MainQuestData ?? [],
            x => x.QuestTid,
            kv => kv.Key,
            kv => new UserMainQuestEntity { UserId = userId, QuestTid = kv.Key, ReceivedReward = kv.Value },
            (entity, kv) => entity.ReceivedReward = kv.Value);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserTowerProgressEntity>(),
            db.Set<UserTowerProgressEntity>().Where(x => x.UserId == userId),
            user.TowerProgress ?? [],
            x => x.CorporationType,
            kv => (int)kv.Key,
            kv => new UserTowerProgressEntity { UserId = userId, CorporationType = (int)kv.Key, ClearedFloor = kv.Value },
            (entity, kv) => entity.ClearedFloor = kv.Value);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserClearedTutorialEntity>(),
            db.Set<UserClearedTutorialEntity>().Where(x => x.UserId == userId),
            user.ClearedTutorialDataNew ?? [],
            x => x.TutorialGroupId,
            kv => kv.Key,
            kv => new UserClearedTutorialEntity
            {
                UserId = userId,
                TutorialGroupId = kv.Key,
                TutorialId = kv.Value.Id,
                VersionGroup = kv.Value.VersionGroup
            },
            (entity, kv) =>
            {
                entity.TutorialId = kv.Value.Id;
                entity.VersionGroup = kv.Value.VersionGroup;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserContentsUnlockEntity>(),
            db.Set<UserContentsUnlockEntity>().Where(x => x.UserId == userId),
            user.ContentsOpenUnlocked ?? [],
            x => x.ContentsOpenId,
            kv => kv.Key,
            kv => new UserContentsUnlockEntity
            {
                UserId = userId,
                ContentsOpenId = kv.Key,
                ButtonAnimationPlayed = kv.Value.ButtonAnimationPlayed,
                PopupAnimationPlayed = kv.Value.PopupAnimationPlayed
            },
            (entity, kv) =>
            {
                entity.ButtonAnimationPlayed = kv.Value.ButtonAnimationPlayed;
                entity.PopupAnimationPlayed = kv.Value.PopupAnimationPlayed;
            });

        var noticeEntries = (user.ViewedNoticePopupTableIds ?? [])
            .SelectMany(kv => (kv.Value ?? []).Select(id => (CategoryId: kv.Key, NoticePopupTableId: id)));

        DomainStoreHelpers.SyncCollection(
            db.Set<UserViewedNoticePopupEntity>(),
            db.Set<UserViewedNoticePopupEntity>().Where(x => x.UserId == userId),
            noticeEntries,
            x => (x.CategoryId, x.NoticePopupTableId),
            e => (e.CategoryId, e.NoticePopupTableId),
            e => new UserViewedNoticePopupEntity
            {
                UserId = userId,
                CategoryId = e.CategoryId,
                NoticePopupTableId = e.NoticePopupTableId
            },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserMapStateEntity>(),
            db.Set<UserMapStateEntity>().Where(x => x.UserId == userId),
            user.MapJson ?? [],
            x => x.MapId,
            kv => kv.Key,
            kv => new UserMapStateEntity
            {
                UserId = userId,
                MapId = kv.Key,
                StateJson = kv.Value ?? string.Empty
            },
            (entity, kv) => entity.StateJson = kv.Value ?? string.Empty);

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        DomainStoreHelpers.SyncCollection(
            db.Set<UserFieldMapEntity>(),
            db.Set<UserFieldMapEntity>().Where(x => x.UserId == userId),
            user.FieldInfoNew ?? [],
            x => x.MapId,
            kv => kv.Key,
            kv => new UserFieldMapEntity
            {
                UserId = userId,
                MapId = kv.Key,
                DataJson = JsonConvert.SerializeObject(kv.Value ?? new FieldInfoNew()),
                UpdatedAt = now
            },
            (entity, kv) =>
            {
                string json = JsonConvert.SerializeObject(kv.Value ?? new FieldInfoNew());
                if (entity.DataJson != json)
                {
                    entity.DataJson = json;
                    entity.UpdatedAt = now;
                }
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserSubQuestEntity>(),
            db.Set<UserSubQuestEntity>().Where(x => x.UserId == userId),
            user.SubQuestData ?? [],
            x => x.SubQuestId,
            kv => kv.Key,
            kv => new UserSubQuestEntity
            {
                UserId = userId,
                SubQuestId = kv.Key,
                ReceivedReward = kv.Value
            },
            (entity, kv) => entity.ReceivedReward = kv.Value);

        var stageClearHistorySet = db.Set<UserStageClearHistoryEntity>();
        var existingHistory = stageClearHistorySet
            .Where(x => x.UserId == userId)
            .ToList();
        if (existingHistory.Count > 0)
            stageClearHistorySet.RemoveRange(existingHistory);

        foreach (var entry in user.StageClearHistorys ?? [])
        {
            stageClearHistorySet.Add(new UserStageClearHistoryEntity
            {
                UserId = userId,
                TeamCombat = entry.TeamCombat,
                ClearedAt = entry.ClearedAt,
                UserJson = JsonConvert.SerializeObject(entry.User ?? new NetWholeUserData()),
                SlotsJson = JsonConvert.SerializeObject(entry.Slots.ToArray())
            });
        }
    }
}
