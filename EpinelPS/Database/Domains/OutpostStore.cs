using EpinelPS.Data;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database.Domains;

public sealed class OutpostStore : IUserDomainStore
{
    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        var outpost = db.Set<OutpostEntity>()
            .AsNoTracking()
            .FirstOrDefault(o => o.UserId == userId);

        user.UserDispatchData ??= new DispatchData();
        user.OutpostBattleLevel ??= new NetOutpostBattleLevel();

        if (outpost != null)
        {
            user.DispatchLv = outpost.DispatchLv;
            user.DispatchCollectionLv = outpost.DispatchCollectionLv;
            user.DispatchFavoriteLv = outpost.DispatchFavoriteLv;
            user.DispatchResetCount = outpost.DispatchResetCount;
            user.UserDispatchData.Today = outpost.DispatchToday;
            user.InfraCoreExp = outpost.InfraCoreExp;
            user.InfraCoreLvl = outpost.InfraCoreLvl;
            user.SynchroDeviceUpgraded = outpost.SynchroDeviceUpgraded;
            user.SynchroDeviceLevel = outpost.SynchroDeviceLevel;
            user.OutpostBattleLevel.Level = outpost.OutpostBattleLevel;
            user.OutpostBattleLevel.Exp = outpost.OutpostBattleExp;
            user.OutpostBuffs = string.IsNullOrWhiteSpace(outpost.OutpostBuffs)
                ? new OutpostBuffs()
                : JsonConvert.DeserializeObject<OutpostBuffs>(outpost.OutpostBuffs) ?? new OutpostBuffs();
            user.OutpostConditionList = string.IsNullOrWhiteSpace(outpost.OutpostConditionList)
                ? []
                : JsonConvert.DeserializeObject<List<int>>(outpost.OutpostConditionList) ?? [];
        }

        user.DispatchClearList = db.Set<DispatchClearItemEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.DispatchId)
            .ToList();

        user.SelectableDispatchData = db.Set<SelectableDispatchEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetSelectableDispatchData
            {
                DispatchGroupId = x.DispatchGroupId,
                SelectTid = x.SelectTid,
                SelectSlotId = x.SelectSlotId,
                IsRun = x.IsRun,
                StartAt = x.StartAt,
                EndAt = x.EndAt
            })
            .ToList();

        user.UserDispatchData.dispatchDatas = db.Set<DispatchEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetUserDispatchData
            {
                Tid = x.Tid,
                IsRun = x.IsRun,
                StartAt = x.StartAt,
                EndAt = x.EndAt
            })
            .ToList();

        user.InfraCoreRewardReceived = db.Set<InfraCoreRewardEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.InfraCoreLevel, _ => true);

        user.SynchroSlots = db.Set<SynchroSlotEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Slot)
            .Select(x => new SynchroSlot
            {
                Slot = x.Slot,
                CharacterSerialNumber = x.CharacterSerialNumber,
                AvailableAt = x.AvailableAt
            })
            .ToList();

        user.ResearchProgress = db.Set<ResearchProgressEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.ResearchId,
                x => new RecycleRoomResearchProgress
                {
                    Level = x.Level,
                    Attack = x.Attack,
                    Defense = x.Defense,
                    Hp = x.Hp
                });

        user.OutpostBuildings = db.Set<OutpostBuildingEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetUserOutpostData
            {
                SlotId = x.SlotId,
                BuildingId = x.BuildingId,
                IsDone = x.IsDone,
                StartAt = x.StartAt,
                CompleteAt = x.CompleteAt
            })
            .ToList();

        user.ClearedOutpostScenarioIds = db.Set<UserClearedOutpostScenarioEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ScenarioId)
            .ToList();

        user.CompletedTacticAcademyLessons = db.Set<UserTacticAcademyLessonEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.LessonId)
            .ToList();
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        // Outposts row must exist before its child tables
        var outpost = db.Set<OutpostEntity>().FirstOrDefault(o => o.UserId == userId);
        if (outpost == null)
        {
            outpost = new OutpostEntity { UserId = userId };
            db.Set<OutpostEntity>().Add(outpost);
        }

        outpost.DispatchLv = user.DispatchLv;
        outpost.DispatchCollectionLv = user.DispatchCollectionLv;
        outpost.DispatchFavoriteLv = user.DispatchFavoriteLv;
        outpost.DispatchResetCount = user.DispatchResetCount;
        outpost.DispatchToday = user.UserDispatchData?.Today ?? 0;
        outpost.InfraCoreExp = user.InfraCoreExp;
        outpost.InfraCoreLvl = user.InfraCoreLvl;
        outpost.SynchroDeviceUpgraded = user.SynchroDeviceUpgraded;
        outpost.SynchroDeviceLevel = user.SynchroDeviceLevel;
        outpost.OutpostBattleLevel = user.OutpostBattleLevel?.Level ?? 1;
        outpost.OutpostBattleExp = user.OutpostBattleLevel?.Exp ?? 0;
        outpost.OutpostBuffs = JsonConvert.SerializeObject(user.OutpostBuffs ?? new OutpostBuffs());
        outpost.OutpostConditionList = JsonConvert.SerializeObject(user.OutpostConditionList ?? []);

        DomainStoreHelpers.SyncCollection(
            db.Set<DispatchClearItemEntity>(),
            db.Set<DispatchClearItemEntity>().Where(x => x.UserId == userId),
            user.DispatchClearList ?? [],
            x => x.DispatchId,
            id => id,
            id => new DispatchClearItemEntity { UserId = userId, DispatchId = id },
            (_, _) => { });

        var existingSelectable = db.Set<SelectableDispatchEntity>().Where(x => x.UserId == userId).ToList();
        db.Set<SelectableDispatchEntity>().RemoveRange(existingSelectable);
        foreach (var sd in user.SelectableDispatchData ?? [])
        {
            db.Set<SelectableDispatchEntity>().Add(new SelectableDispatchEntity
            {
                UserId = userId,
                DispatchGroupId = sd.DispatchGroupId,
                SelectTid = sd.SelectTid,
                SelectSlotId = sd.SelectSlotId,
                IsRun = sd.IsRun,
                StartAt = sd.StartAt,
                EndAt = sd.EndAt
            });
        }

        DomainStoreHelpers.SyncCollection(
            db.Set<DispatchEntity>(),
            db.Set<DispatchEntity>().Where(x => x.UserId == userId),
            user.UserDispatchData?.dispatchDatas ?? [],
            x => x.Tid,
            d => d.Tid,
            d => new DispatchEntity
            {
                UserId = userId,
                Tid = d.Tid,
                IsRun = d.IsRun,
                StartAt = d.StartAt,
                EndAt = d.EndAt
            },
            (entity, d) =>
            {
                entity.IsRun = d.IsRun;
                entity.StartAt = d.StartAt;
                entity.EndAt = d.EndAt;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<InfraCoreRewardEntity>(),
            db.Set<InfraCoreRewardEntity>().Where(x => x.UserId == userId),
            (user.InfraCoreRewardReceived ?? []).Where(kv => kv.Value).Select(kv => kv.Key),
            x => x.InfraCoreLevel,
            lvl => lvl,
            lvl => new InfraCoreRewardEntity { UserId = userId, InfraCoreLevel = lvl },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<SynchroSlotEntity>(),
            db.Set<SynchroSlotEntity>().Where(x => x.UserId == userId),
            user.SynchroSlots ?? [],
            x => x.Slot,
            s => s.Slot,
            s => new SynchroSlotEntity
            {
                UserId = userId,
                Slot = s.Slot,
                CharacterSerialNumber = s.CharacterSerialNumber,
                AvailableAt = s.AvailableAt
            },
            (entity, s) =>
            {
                entity.CharacterSerialNumber = s.CharacterSerialNumber;
                entity.AvailableAt = s.AvailableAt;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<ResearchProgressEntity>(),
            db.Set<ResearchProgressEntity>().Where(x => x.UserId == userId),
            user.ResearchProgress ?? [],
            x => x.ResearchId,
            kv => kv.Key,
            kv => new ResearchProgressEntity
            {
                UserId = userId,
                ResearchId = kv.Key,
                Level = kv.Value.Level,
                Attack = kv.Value.Attack,
                Defense = kv.Value.Defense,
                Hp = kv.Value.Hp
            },
            (entity, kv) =>
            {
                entity.Level = kv.Value.Level;
                entity.Attack = kv.Value.Attack;
                entity.Defense = kv.Value.Defense;
                entity.Hp = kv.Value.Hp;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<OutpostBuildingEntity>(),
            db.Set<OutpostBuildingEntity>().Where(x => x.UserId == userId),
            user.OutpostBuildings ?? [],
            x => x.SlotId,
            b => b.SlotId,
            b => new OutpostBuildingEntity
            {
                UserId = userId,
                SlotId = b.SlotId,
                BuildingId = b.BuildingId,
                IsDone = b.IsDone,
                StartAt = b.StartAt,
                CompleteAt = b.CompleteAt
            },
            (entity, b) =>
            {
                entity.BuildingId = b.BuildingId;
                entity.IsDone = b.IsDone;
                entity.StartAt = b.StartAt;
                entity.CompleteAt = b.CompleteAt;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserClearedOutpostScenarioEntity>(),
            db.Set<UserClearedOutpostScenarioEntity>().Where(x => x.UserId == userId),
            user.ClearedOutpostScenarioIds ?? [],
            x => x.ScenarioId,
            id => id,
            id => new UserClearedOutpostScenarioEntity
            {
                UserId = userId,
                ScenarioId = id
            },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserTacticAcademyLessonEntity>(),
            db.Set<UserTacticAcademyLessonEntity>().Where(x => x.UserId == userId),
            user.CompletedTacticAcademyLessons ?? [],
            x => x.LessonId,
            id => id,
            id => new UserTacticAcademyLessonEntity
            {
                UserId = userId,
                LessonId = id
            },
            (_, _) => { });
    }
}
