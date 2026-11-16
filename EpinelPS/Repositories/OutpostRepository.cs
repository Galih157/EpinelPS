using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Repositories;

public sealed class OutpostDispatchState
{
    public int DispatchLv { get; set; } = 1;
    public int DispatchCollectionLv { get; set; }
    public int DispatchFavoriteLv { get; set; }
    public int DispatchResetCount { get; set; }
    public int Today { get; set; }
    public List<int> DispatchClearList { get; set; } = [];
    public List<NetSelectableDispatchData> SelectableDispatchData { get; set; } = [];
    public List<NetUserDispatchData> DispatchDatas { get; set; } = [];
}

public sealed class OutpostRepository(GameContext db)
{
    private const long DefaultBuildingTimestampTicks = 638549982076760660;

    private static readonly (int SlotId, int BuildingId)[] DefaultBuildingSlots =
    [
        (1, 22401),
        (4, 22701),
        (5, 22801),
        (6, 22901),
        (7, 23001),
        (3, 23101),
        (2, 23201),
        (9, 23301),
        (8, 23401),
        (10, 23501),
        (38, 33601)
    ];

    // --- Outpost Root ---

    public OutpostEntity GetOrCreateOutpost(ulong userId)
    {
        var set = db.Set<OutpostEntity>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId)
                  ?? set.FirstOrDefault(x => x.UserId == userId);

        if (row != null)
            return row;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        row = new OutpostEntity
        {
            UserId = userId,
            DispatchLv = 1,
            InfraCoreLvl = 1,
            SynchroDeviceLevel = 200,
            OutpostBattleLevel = 1,
            OutpostBuffs = "{}",
            OutpostConditionList = "[]"
        };
        set.Add(row);
        db.SaveChanges();
        return row;
    }

    // --- Outpost Battle Level ---

    public NetOutpostBattleLevel GetOutpostBattleLevel(ulong userId)
    {
        var row = db.Set<OutpostEntity>()
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new { o.OutpostBattleLevel, o.OutpostBattleExp })
            .FirstOrDefault();

        if (row == null)
            return new NetOutpostBattleLevel { Level = 1, Exp = 0 };

        return new NetOutpostBattleLevel
        {
            Level = Math.Max(1, row.OutpostBattleLevel),
            Exp = row.OutpostBattleExp
        };
    }

    public void SaveOutpostBattleLevel(ulong userId, int level, int exp)
    {
        var outpost = GetOrCreateOutpost(userId);
        outpost.OutpostBattleLevel = Math.Max(1, level);
        outpost.OutpostBattleExp = exp;
        db.SaveChanges();
    }

    public NetOutpostBattleLevel AddOutpostBattleExp(ulong userId, int expGain = 1)
    {
        var outpost = GetOrCreateOutpost(userId);
        int level = Math.Max(1, outpost.OutpostBattleLevel);
        int exp = outpost.OutpostBattleExp + expGain;
        bool isLevelUp = false;

        while (exp >= 5)
        {
            exp -= 5;
            level++;
            isLevelUp = true;
        }

        outpost.OutpostBattleLevel = level;
        outpost.OutpostBattleExp = exp;
        db.SaveChanges();

        return new NetOutpostBattleLevel
        {
            IsLevelUp = isLevelUp,
            Level = level,
            Exp = exp
        };
    }

    // --- InfraCore ---

    public (int Exp, int Level) GetInfraCore(ulong userId)
    {
        var row = db.Set<OutpostEntity>()
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new { o.InfraCoreExp, o.InfraCoreLvl })
            .FirstOrDefault();

        return row == null ? (0, 1) : (row.InfraCoreExp, Math.Max(1, row.InfraCoreLvl));
    }

    public void SetInfraCore(ulong userId, int exp, int level)
    {
        var outpost = GetOrCreateOutpost(userId);
        outpost.InfraCoreExp = exp;
        outpost.InfraCoreLvl = Math.Max(1, level);
        db.SaveChanges();
    }

    public (int BeforeExp, int BeforeLevel, int CurrentExp, int CurrentLevel) AddInfraCoreExp(ulong userId, int amount)
    {
        var outpost = GetOrCreateOutpost(userId);
        int beforeExp = outpost.InfraCoreExp;
        int beforeLevel = Math.Max(1, outpost.InfraCoreLvl);

        outpost.InfraCoreExp += amount;
        outpost.InfraCoreLvl = GameData.Instance.GetInfraCoreLev(outpost.InfraCoreExp);
        db.SaveChanges();

        return (beforeExp, beforeLevel, outpost.InfraCoreExp, outpost.InfraCoreLvl);
    }

    public bool IsInfraCoreRewardReceived(ulong userId, int level)
    {
        return db.Set<InfraCoreRewardEntity>()
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.InfraCoreLevel == level);
    }

    public void MarkInfraCoreRewardReceived(ulong userId, int level)
    {
        GetOrCreateOutpost(userId);

        var set = db.Set<InfraCoreRewardEntity>();
        if (set.Local.Any(x => x.UserId == userId && x.InfraCoreLevel == level) ||
            set.AsNoTracking().Any(x => x.UserId == userId && x.InfraCoreLevel == level))
        {
            return;
        }

        set.Add(new InfraCoreRewardEntity
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            InfraCoreLevel = level
        });
        db.SaveChanges();
    }

    // --- Outpost Buffs ---

    public OutpostBuffs GetOutpostBuffs(ulong userId)
    {
        string? json = db.Set<OutpostEntity>()
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => o.OutpostBuffs)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(json)
            ? new OutpostBuffs()
            : JsonConvert.DeserializeObject<OutpostBuffs>(json) ?? new OutpostBuffs();
    }

    public void SaveOutpostBuffs(ulong userId, OutpostBuffs buffs)
    {
        var outpost = GetOrCreateOutpost(userId);
        outpost.OutpostBuffs = JsonConvert.SerializeObject(buffs ?? new OutpostBuffs());
        db.SaveChanges();
    }

    // --- Outpost Buildings ---

    public List<NetUserOutpostData> GetOutpostBuildings(ulong userId)
    {
        return db.Set<OutpostBuildingEntity>()
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
    }

    public void SaveOutpostBuildings(ulong userId, IEnumerable<NetUserOutpostData> buildings)
    {
        GetOrCreateOutpost(userId);

        var set = db.Set<OutpostBuildingEntity>();
        SyncCollection(
            set,
            set.Where(x => x.UserId == userId).ToList(),
            buildings,
            x => x.SlotId,
            b => b.SlotId,
            b => new OutpostBuildingEntity
            {
                Id = UlidGenerator.NewUlid(),
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

        db.SaveChanges();
    }

    public List<NetUserOutpostData> GetOrInitializeOutpostBuildings(ulong userId)
    {
        var buildings = GetOutpostBuildings(userId);
        if (buildings.Count == 0)
        {
            buildings = CreateDefaultBuildings();
            SaveOutpostBuildings(userId, buildings);
            return buildings;
        }

        long nowTicks = DateTime.UtcNow.Ticks;
        bool changed = false;
        foreach (var building in buildings)
        {
            if (!building.IsDone && nowTicks >= building.CompleteAt)
            {
                building.IsDone = true;
                changed = true;
            }
        }

        if (changed)
            SaveOutpostBuildings(userId, buildings);

        return buildings;
    }

    private static List<NetUserOutpostData> CreateDefaultBuildings()
    {
        return DefaultBuildingSlots.Select(s => new NetUserOutpostData
        {
            SlotId = s.SlotId,
            BuildingId = s.BuildingId,
            IsDone = true,
            StartAt = DefaultBuildingTimestampTicks,
            CompleteAt = DefaultBuildingTimestampTicks
        }).ToList();
    }

    // --- Synchro Slots & Device State ---

    public List<SynchroSlot> GetSynchroSlots(ulong userId)
    {
        return db.Set<SynchroSlotEntity>()
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
    }

    public const int MaxSynchroSlots = 213;

    public void UnlockAllSynchroSlots(ulong userId)
    {
        var slots = GetOrInitializeSynchroSlots(userId);
        var existing = slots.Select(x => x.Slot).ToHashSet();
        foreach (var i in Enumerable.Range(1, MaxSynchroSlots).Where(i => !existing.Contains(i)))
            slots.Add(new SynchroSlot { Slot = i });
        foreach (var s in slots) s.AvailableAt = 0;
        SaveSynchroSlots(userId, slots);
    }

    public List<SynchroSlot> GetOrInitializeSynchroSlots(ulong userId)
    {
        var slots = GetSynchroSlots(userId);
        if (slots.Count > 0)
            return slots;

        slots = Enumerable.Range(1, 5)
            .Select(i => new SynchroSlot { Slot = i })
            .ToList();
        SaveSynchroSlots(userId, slots);
        return slots;
    }

    public void SaveSynchroSlots(ulong userId, IEnumerable<SynchroSlot> slots)
    {
        GetOrCreateOutpost(userId);

        var set = db.Set<SynchroSlotEntity>();
        SyncCollection(
            set,
            set.Where(x => x.UserId == userId).ToList(),
            slots,
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

        db.SaveChanges();
    }

    public (bool Upgraded, int Level) GetSynchroDeviceState(ulong userId)
    {
        var row = db.Set<OutpostEntity>()
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new { o.SynchroDeviceUpgraded, o.SynchroDeviceLevel })
            .FirstOrDefault();

        if (row == null)
            return (false, 200);

        return (row.SynchroDeviceUpgraded, row.SynchroDeviceLevel > 0 ? row.SynchroDeviceLevel : 200);
    }

    public void SaveSynchroDeviceState(ulong userId, bool upgraded, int level)
    {
        var outpost = GetOrCreateOutpost(userId);
        outpost.SynchroDeviceUpgraded = upgraded;
        outpost.SynchroDeviceLevel = level;
        db.SaveChanges();
    }

    public int GetSynchroLevel(ulong userId, IEnumerable<CharacterModel> characters)
    {
        var (upgraded, deviceLevel) = GetSynchroDeviceState(userId);
        return ComputeSynchroLevel(upgraded, deviceLevel, characters);
    }

    public static int ComputeSynchroLevel(bool upgraded, int deviceLevel, IEnumerable<CharacterModel> characters)
    {
        if (upgraded)
            return deviceLevel;

        var highestLevelCharacters = characters.OrderByDescending(x => x.Level).Take(5).ToList();
        return highestLevelCharacters.Count > 0 ? highestLevelCharacters.Last().Level : 1;
    }

    // --- Recycle Room Research ---

    public Dictionary<int, RecycleRoomResearchProgress> GetResearchProgress(ulong userId)
    {
        return db.Set<ResearchProgressEntity>()
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
    }

    public RecycleRoomResearchProgress? GetResearchProgress(ulong userId, int researchId)
    {
        var row = db.Set<ResearchProgressEntity>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId && x.ResearchId == researchId);

        return row == null
            ? null
            : new RecycleRoomResearchProgress
            {
                Level = row.Level,
                Attack = row.Attack,
                Defense = row.Defense,
                Hp = row.Hp
            };
    }

    public void UpsertResearchProgress(ulong userId, int researchId, RecycleRoomResearchProgress progress)
    {
        GetOrCreateOutpost(userId);

        var set = db.Set<ResearchProgressEntity>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.ResearchId == researchId)
                  ?? set.FirstOrDefault(x => x.UserId == userId && x.ResearchId == researchId);

        if (row == null)
        {
            set.Add(new ResearchProgressEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                ResearchId = researchId,
                Level = progress.Level,
                Attack = progress.Attack,
                Defense = progress.Defense,
                Hp = progress.Hp
            });
        }
        else
        {
            row.Level = progress.Level;
            row.Attack = progress.Attack;
            row.Defense = progress.Defense;
            row.Hp = progress.Hp;
        }

        db.SaveChanges();
    }

    // --- Tactic Academy Lessons ---

    public List<int> GetCompletedTacticAcademyLessons(ulong userId)
    {
        return db.Set<UserTacticAcademyLessonEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Id)
            .Select(x => x.LessonId)
            .ToList();
    }

    public bool AddTacticAcademyLesson(ulong userId, int lessonId)
    {
        var set = db.Set<UserTacticAcademyLessonEntity>();
        if (set.Local.Any(x => x.UserId == userId && x.LessonId == lessonId) ||
            set.AsNoTracking().Any(x => x.UserId == userId && x.LessonId == lessonId))
        {
            return false;
        }

        UserDbHelper.GetOrCreateGameUser(db, userId);
        set.Add(new UserTacticAcademyLessonEntity
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            LessonId = lessonId
        });
        db.SaveChanges();
        return true;
    }

    // --- Cleared Outpost Scenarios (Sudden Events) ---

    public List<int> GetClearedOutpostScenarios(ulong userId)
    {
        return db.Set<UserClearedOutpostScenarioEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Id)
            .Select(x => x.ScenarioId)
            .ToList();
    }

    public bool HasClearedOutpostScenario(ulong userId, int scenarioId)
    {
        return db.Set<UserClearedOutpostScenarioEntity>()
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.ScenarioId == scenarioId);
    }

    public void AddClearedOutpostScenario(ulong userId, int scenarioId)
    {
        var set = db.Set<UserClearedOutpostScenarioEntity>();
        if (set.Local.Any(x => x.UserId == userId && x.ScenarioId == scenarioId) ||
            set.AsNoTracking().Any(x => x.UserId == userId && x.ScenarioId == scenarioId))
        {
            return;
        }

        UserDbHelper.GetOrCreateGameUser(db, userId);
        set.Add(new UserClearedOutpostScenarioEntity
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            ScenarioId = scenarioId
        });
        db.SaveChanges();
    }

    // --- Dispatch ---

    public OutpostDispatchState GetDispatchState(ulong userId)
    {
        var outpost = db.Set<OutpostEntity>()
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Select(o => new
            {
                o.DispatchLv,
                o.DispatchCollectionLv,
                o.DispatchFavoriteLv,
                o.DispatchResetCount,
                o.DispatchToday
            })
            .FirstOrDefault();

        return new OutpostDispatchState
        {
            DispatchLv = outpost is { DispatchLv: > 0 } ? outpost.DispatchLv : 1,
            DispatchCollectionLv = outpost?.DispatchCollectionLv ?? 0,
            DispatchFavoriteLv = outpost?.DispatchFavoriteLv ?? 0,
            DispatchResetCount = outpost?.DispatchResetCount ?? 0,
            Today = outpost?.DispatchToday ?? 0,
            DispatchClearList = db.Set<DispatchClearItemEntity>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.DispatchId)
                .ToList(),
            SelectableDispatchData = db.Set<SelectableDispatchEntity>()
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
                .ToList(),
            DispatchDatas = db.Set<DispatchEntity>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new NetUserDispatchData
                {
                    Tid = x.Tid,
                    IsRun = x.IsRun,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt
                })
                .ToList()
        };
    }

    public void SaveDispatchState(ulong userId, OutpostDispatchState state)
    {
        var outpost = GetOrCreateOutpost(userId);
        outpost.DispatchLv = state.DispatchLv;
        outpost.DispatchCollectionLv = state.DispatchCollectionLv;
        outpost.DispatchFavoriteLv = state.DispatchFavoriteLv;
        outpost.DispatchResetCount = state.DispatchResetCount;
        outpost.DispatchToday = state.Today;

        var clearSet = db.Set<DispatchClearItemEntity>();
        SyncCollection(
            clearSet,
            clearSet.Where(x => x.UserId == userId).ToList(),
            state.DispatchClearList,
            x => x.DispatchId,
            id => id,
            id => new DispatchClearItemEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                DispatchId = id
            },
            (_, _) => { });

        var selectableSet = db.Set<SelectableDispatchEntity>();
        var existingSelectable = selectableSet.Where(x => x.UserId == userId).ToList();
        if (existingSelectable.Count > 0)
            selectableSet.RemoveRange(existingSelectable);

        foreach (var sd in state.SelectableDispatchData)
        {
            selectableSet.Add(new SelectableDispatchEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                DispatchGroupId = sd.DispatchGroupId,
                SelectTid = sd.SelectTid,
                SelectSlotId = sd.SelectSlotId,
                IsRun = sd.IsRun,
                StartAt = sd.StartAt,
                EndAt = sd.EndAt
            });
        }

        var dispatchSet = db.Set<DispatchEntity>();
        SyncCollection(
            dispatchSet,
            dispatchSet.Where(x => x.UserId == userId).ToList(),
            state.DispatchDatas,
            x => x.Tid,
            d => d.Tid,
            d => new DispatchEntity
            {
                Id = UlidGenerator.NewUlid(),
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

        db.SaveChanges();
    }

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
}
