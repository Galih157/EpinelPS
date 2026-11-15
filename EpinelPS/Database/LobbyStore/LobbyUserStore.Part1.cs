using EpinelPS.Data;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database;

public static partial class LobbyUserStore
{
    private static void LoadPart1(GameContext db, User user)
    {
        ulong userId = user.ID;

        // Group 1: Inventory, characters, and equipment awakenings
        user.Characters = db.Characters
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new CharacterModel
            {
                Csn = (int)c.Csn,
                Tid = c.Tid,
                CostumeId = c.CostumeId,
                Level = c.Level,
                UltimateLevel = c.UltimateLevel,
                Skill1Lvl = c.Skill1Lvl,
                Skill2Lvl = c.Skill2Lvl,
                Grade = c.Grade,
                IsMainForce = c.IsMainForce
            })
            .ToList();

        var itemEntities = db.Items
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Include(i => i.CharacterAssignments)
            .ToList();

        user.Items = itemEntities
            .Select(i => new DbItemData
            {
                Isn = i.Isn,
                ItemType = i.ItemType,
                Csn = i.Csn,
                Count = i.Count,
                Level = i.Level,
                Exp = i.Exp,
                Position = i.Position,
                Corp = i.Corp,
                CsnList = i.CharacterAssignments
                    .OrderBy(a => a.Position)
                    .Select(a => a.CharacterCsn)
                    .ToList()
            })
            .ToList();

        user.EquipmentAwakenings = db.EquipmentAwakenings
            .AsNoTracking()
            .Where(a => a.Item.UserId == userId)
            .Select(a => new EquipmentAwakeningData
            {
                Isn = a.Item.Isn,
                IsNewData = a.IsNewData,
                Option = new NetEquipmentAwakeningOption
                {
                    Option1Id = a.Option1Id,
                    Option1Lock = a.Option1Lock,
                    IsOption1DisposableLock = a.IsOption1DisposableLock,
                    Option2Id = a.Option2Id,
                    Option2Lock = a.Option2Lock,
                    IsOption2DisposableLock = a.IsOption2DisposableLock,
                    Option3Id = a.Option3Id,
                    Option3Lock = a.Option3Lock,
                    IsOption3DisposableLock = a.IsOption3DisposableLock
                }
            })
            .ToList();

        // Group 2: Outpost
        var outpost = db.Outposts
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

        user.DispatchClearList = db.DispatchClearItems
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.DispatchId)
            .ToList();

        user.SelectableDispatchData = db.SelectableDispatches
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

        user.UserDispatchData.dispatchDatas = db.Dispatches
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

        user.InfraCoreRewardReceived = db.InfraCoreRewards
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.InfraCoreLevel, _ => true);

        user.SynchroSlots = db.SynchroSlots
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

        user.ResearchProgress = db.ResearchProgress
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

        user.OutpostBuildings = db.OutpostBuildings
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

        // Group 3: Completed scenarios
        user.CompletedScenarios = db.UserCompletedScenarios
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ScenarioId)
            .ToList();

        // Group 4: Currency and progress unlocks
        var currencies = db.UserCurrencies
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => (CurrencyType)x.CurrencyType, x => x.Amount);
        if (currencies.Count > 0)
            user.Currency = currencies;

        user.MainQuestData = db.UserMainQuests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.QuestTid, x => x.ReceivedReward);

        user.TowerProgress = db.UserTowerProgress
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => (CorporationTowerType)x.CorporationType, x => x.ClearedFloor);

        user.ClearedTutorialDataNew = db.UserClearedTutorials
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.TutorialGroupId,
                x => new ClearedTutorialData
                {
                    Id = x.TutorialId,
                    VersionGroup = x.VersionGroup
                });

        user.ContentsOpenUnlocked = db.UserContentsUnlocks
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.ContentsOpenId,
                x => new UnlockData(x.ButtonAnimationPlayed, x.PopupAnimationPlayed));

        user.ViewedNoticePopupTableIds = db.UserViewedNoticePopups
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToList()
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.NoticePopupTableId).ToList());

        // Group 5: Daily and weekly reset state
        user.ResetableData ??= new ResetableData();
        user.WeeklyResetableData ??= new WeeklyResetableData();

        var resetState = db.UserResetStates
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

        user.ResetableData.CompletedDailyMissions = db.UserCompletedDailyMissions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MissionId)
            .ToList();

        user.WeeklyResetableData.CompletedWeeklyMissions = db.UserCompletedWeeklyMissions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MissionId)
            .ToList();

        var towerCounts = db.UserDailyTowerCounts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToList();
        foreach (var tc in towerCounts)
        {
            user.ResetableData.TowerCount[(CorporationTowerType)tc.CorporationType] = tc.UsedCount;
        }

        user.ResetableData.DailyCounselCount = db.UserDailyCounselCounts
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.KeyId, x => x.Count);

        // Group 6: Teams and representation
        var teamGroups = db.UserTeamGroups
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Slots)
            .ToList();

        user.UserTeams = new Dictionary<int, NetUserTeamData>();
        foreach (var group in teamGroups)
        {
            var netUserTeam = new NetUserTeamData
            {
                Type = group.TeamType,
                LastContentsTeamNumber = group.LastContentsTeamNumber
            };

            foreach (var teamGroup in group.Slots.GroupBy(s => s.TeamNumber).OrderBy(g => g.Key))
            {
                var netTeam = new NetTeamData { TeamNumber = teamGroup.Key };
                foreach (var slot in teamGroup.OrderBy(s => s.Slot))
                {
                    netTeam.Slots.Add(new NetTeamSlot
                    {
                        Slot = slot.Slot,
                        Value = slot.Value,
                        ValueType = slot.ValueType
                    });
                }
                netUserTeam.Teams.Add(netTeam);
            }

            user.UserTeams[group.TeamType] = netUserTeam;
        }

        user.RepresentationTeamDataNew = db.UserRepresentationTeamSlots
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Slot)
            .Select(x => x.CharacterCsn)
            .ToArray();
    }

    private static void SavePart1(GameContext db, User user)
    {
        ulong userId = user.ID;

        // Group 1: Characters first so ItemCharacterAssignments FK is satisfied
        var characters = user.Characters ?? [];
        var validCharacterCsns = new HashSet<long>();
        SyncKeyed(
            db.Characters,
            db.Characters.Where(c => c.UserId == userId),
            characters,
            c => c.Csn,
            c => (long)c.Csn,
            c =>
            {
                validCharacterCsns.Add(c.Csn);
                return new CharacterEntity
                {
                    UserId = userId,
                    Csn = c.Csn,
                    Tid = c.Tid,
                    CostumeId = c.CostumeId,
                    Level = c.Level,
                    UltimateLevel = c.UltimateLevel,
                    Skill1Lvl = c.Skill1Lvl,
                    Skill2Lvl = c.Skill2Lvl,
                    Grade = c.Grade,
                    IsMainForce = c.IsMainForce
                };
            },
            (entity, c) =>
            {
                validCharacterCsns.Add(c.Csn);
                entity.Tid = c.Tid;
                entity.CostumeId = c.CostumeId;
                entity.Level = c.Level;
                entity.UltimateLevel = c.UltimateLevel;
                entity.Skill1Lvl = c.Skill1Lvl;
                entity.Skill2Lvl = c.Skill2Lvl;
                entity.Grade = c.Grade;
                entity.IsMainForce = c.IsMainForce;
            });

        // Items
        var items = user.Items ?? [];
        var itemsByIsn = new Dictionary<long, ItemEntity>();
        SyncKeyed(
            db.Items,
            db.Items.Where(i => i.UserId == userId),
            items,
            i => i.Isn,
            i => i.Isn,
            i =>
            {
                var entity = new ItemEntity
                {
                    UserId = userId,
                    Isn = i.Isn,
                    ItemType = i.ItemType,
                    Csn = i.Csn,
                    Count = i.Count,
                    Level = i.Level,
                    Exp = i.Exp,
                    Position = i.Position,
                    Corp = i.Corp
                };
                itemsByIsn[i.Isn] = entity;
                return entity;
            },
            (entity, i) =>
            {
                entity.ItemType = i.ItemType;
                entity.Csn = i.Csn;
                entity.Count = i.Count;
                entity.Level = i.Level;
                entity.Exp = i.Exp;
                entity.Position = i.Position;
                entity.Corp = i.Corp;
                itemsByIsn[i.Isn] = entity;
            });

        // ItemCharacterAssignments (skipping any Csn not in validCharacterCsns)
        var existingAssignments = db.ItemCharacterAssignments
            .Where(a => a.UserId == userId)
            .ToList();

        var desiredAssignments = new List<ItemCharacterAssignmentEntity>();
        foreach (var item in items)
        {
            if (!itemsByIsn.TryGetValue(item.Isn, out var itemEntity))
                continue;

            var seenCsns = new HashSet<long>();
            int pos = 0;
            foreach (long csn in item.CsnList ?? [])
            {
                if (!validCharacterCsns.Contains(csn) || !seenCsns.Add(csn))
                    continue;

                desiredAssignments.Add(new ItemCharacterAssignmentEntity
                {
                    UserId = userId,
                    ItemId = itemEntity.Id,
                    CharacterCsn = csn,
                    Position = pos++
                });
            }
        }

        var existingAssignmentSet = existingAssignments
            .Select(a => (a.ItemId, a.CharacterCsn, a.Position))
            .ToHashSet();
        var desiredAssignmentSet = desiredAssignments
            .Select(a => (a.ItemId, a.CharacterCsn, a.Position))
            .ToHashSet();

        if (!existingAssignmentSet.SetEquals(desiredAssignmentSet))
        {
            db.ItemCharacterAssignments.RemoveRange(existingAssignments);
            db.ItemCharacterAssignments.AddRange(desiredAssignments);
        }

        // EquipmentAwakenings (skipping orphan awakenings if item doesn't exist)
        var existingAwakenings = db.EquipmentAwakenings
            .Where(a => a.Item.UserId == userId)
            .ToList();

        var desiredAwakenings = new List<EquipmentAwakeningEntity>();
        foreach (var aw in user.EquipmentAwakenings ?? [])
        {
            if (!itemsByIsn.TryGetValue(aw.Isn, out var itemEntity))
                continue;

            var opt = aw.Option ?? new NetEquipmentAwakeningOption();
            desiredAwakenings.Add(new EquipmentAwakeningEntity
            {
                ItemId = itemEntity.Id,
                IsNewData = aw.IsNewData,
                Option1Id = opt.Option1Id,
                Option1Lock = opt.Option1Lock,
                IsOption1DisposableLock = opt.IsOption1DisposableLock,
                Option2Id = opt.Option2Id,
                Option2Lock = opt.Option2Lock,
                IsOption2DisposableLock = opt.IsOption2DisposableLock,
                Option3Id = opt.Option3Id,
                Option3Lock = opt.Option3Lock,
                IsOption3DisposableLock = opt.IsOption3DisposableLock
            });
        }

        db.EquipmentAwakenings.RemoveRange(existingAwakenings);
        db.EquipmentAwakenings.AddRange(desiredAwakenings);

        // Group 2: Outposts row must exist before its child tables
        var outpost = db.Outposts.FirstOrDefault(o => o.UserId == userId);
        if (outpost == null)
        {
            outpost = new OutpostEntity { UserId = userId };
            db.Outposts.Add(outpost);
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

        SyncKeyed(
            db.DispatchClearItems,
            db.DispatchClearItems.Where(x => x.UserId == userId),
            user.DispatchClearList ?? [],
            x => x.DispatchId,
            id => id,
            id => new DispatchClearItemEntity { UserId = userId, DispatchId = id },
            (_, _) => { });

        var existingSelectable = db.SelectableDispatches.Where(x => x.UserId == userId).ToList();
        db.SelectableDispatches.RemoveRange(existingSelectable);
        foreach (var sd in user.SelectableDispatchData ?? [])
        {
            db.SelectableDispatches.Add(new SelectableDispatchEntity
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

        SyncKeyed(
            db.Dispatches,
            db.Dispatches.Where(x => x.UserId == userId),
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

        SyncKeyed(
            db.InfraCoreRewards,
            db.InfraCoreRewards.Where(x => x.UserId == userId),
            (user.InfraCoreRewardReceived ?? []).Where(kv => kv.Value).Select(kv => kv.Key),
            x => x.InfraCoreLevel,
            lvl => lvl,
            lvl => new InfraCoreRewardEntity { UserId = userId, InfraCoreLevel = lvl },
            (_, _) => { });

        SyncKeyed(
            db.SynchroSlots,
            db.SynchroSlots.Where(x => x.UserId == userId),
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

        SyncKeyed(
            db.ResearchProgress,
            db.ResearchProgress.Where(x => x.UserId == userId),
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

        SyncKeyed(
            db.OutpostBuildings,
            db.OutpostBuildings.Where(x => x.UserId == userId),
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

        // Group 3: Completed scenarios
        SyncKeyed(
            db.UserCompletedScenarios,
            db.UserCompletedScenarios.Where(x => x.UserId == userId),
            user.CompletedScenarios ?? [],
            x => x.ScenarioId,
            s => s,
            s => new UserCompletedScenarioEntity { UserId = userId, ScenarioId = s },
            (_, _) => { });

        // Group 4: Currency and progress unlocks
        SyncKeyed(
            db.UserCurrencies,
            db.UserCurrencies.Where(x => x.UserId == userId),
            user.Currency ?? [],
            x => x.CurrencyType,
            kv => (int)kv.Key,
            kv => new UserCurrencyEntity { UserId = userId, CurrencyType = (int)kv.Key, Amount = kv.Value },
            (entity, kv) => entity.Amount = kv.Value);

        SyncKeyed(
            db.UserMainQuests,
            db.UserMainQuests.Where(x => x.UserId == userId),
            user.MainQuestData ?? [],
            x => x.QuestTid,
            kv => kv.Key,
            kv => new UserMainQuestEntity { UserId = userId, QuestTid = kv.Key, ReceivedReward = kv.Value },
            (entity, kv) => entity.ReceivedReward = kv.Value);

        SyncKeyed(
            db.UserTowerProgress,
            db.UserTowerProgress.Where(x => x.UserId == userId),
            user.TowerProgress ?? [],
            x => x.CorporationType,
            kv => (int)kv.Key,
            kv => new UserTowerProgressEntity { UserId = userId, CorporationType = (int)kv.Key, ClearedFloor = kv.Value },
            (entity, kv) => entity.ClearedFloor = kv.Value);

        SyncKeyed(
            db.UserClearedTutorials,
            db.UserClearedTutorials.Where(x => x.UserId == userId),
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

        SyncKeyed(
            db.UserContentsUnlocks,
            db.UserContentsUnlocks.Where(x => x.UserId == userId),
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

        SyncKeyed(
            db.UserViewedNoticePopups,
            db.UserViewedNoticePopups.Where(x => x.UserId == userId),
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

        // Group 5: Daily and weekly reset state
        var resetState = db.UserResetStates.FirstOrDefault(x => x.UserId == userId);
        if (resetState == null)
        {
            resetState = new UserResetStateEntity { UserId = userId };
            db.UserResetStates.Add(resetState);
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

        SyncKeyed(
            db.UserCompletedDailyMissions,
            db.UserCompletedDailyMissions.Where(x => x.UserId == userId),
            daily.CompletedDailyMissions ?? [],
            x => x.MissionId,
            id => id,
            id => new UserCompletedDailyMissionEntity { UserId = userId, MissionId = id },
            (_, _) => { });

        SyncKeyed(
            db.UserCompletedWeeklyMissions,
            db.UserCompletedWeeklyMissions.Where(x => x.UserId == userId),
            weekly.CompletedWeeklyMissions ?? [],
            x => x.MissionId,
            id => id,
            id => new UserCompletedWeeklyMissionEntity { UserId = userId, MissionId = id },
            (_, _) => { });

        SyncKeyed(
            db.UserDailyTowerCounts,
            db.UserDailyTowerCounts.Where(x => x.UserId == userId),
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

        SyncKeyed(
            db.UserDailyCounselCounts,
            db.UserDailyCounselCounts.Where(x => x.UserId == userId),
            daily.DailyCounselCount ?? [],
            x => x.KeyId,
            kv => kv.Key,
            kv => new UserDailyCounselCountEntity { UserId = userId, KeyId = kv.Key, Count = kv.Value },
            (entity, kv) => entity.Count = kv.Value);

        // Group 6: Teams and representation
        var userTeams = user.UserTeams ?? [];
        var groupsByType = new Dictionary<int, UserTeamGroupEntity>();
        SyncKeyed(
            db.UserTeamGroups,
            db.UserTeamGroups.Where(x => x.UserId == userId),
            userTeams,
            x => x.TeamType,
            kv => kv.Key,
            kv =>
            {
                var entity = new UserTeamGroupEntity
                {
                    UserId = userId,
                    TeamType = kv.Key,
                    LastContentsTeamNumber = kv.Value.LastContentsTeamNumber
                };
                groupsByType[kv.Key] = entity;
                return entity;
            },
            (entity, kv) =>
            {
                entity.LastContentsTeamNumber = kv.Value.LastContentsTeamNumber;
                groupsByType[kv.Key] = entity;
            });

        var desiredSlots = userTeams
            .Where(kv => groupsByType.ContainsKey(kv.Key))
            .SelectMany(kv =>
            {
                string groupId = groupsByType[kv.Key].Id;
                return (kv.Value.Teams ?? Enumerable.Empty<NetTeamData>())
                    .SelectMany(team => (team.Slots ?? Enumerable.Empty<NetTeamSlot>())
                        .Select(slot => (
                            TeamGroupId: groupId,
                            team.TeamNumber,
                            slot.Slot,
                            slot.Value,
                            slot.ValueType)));
            });

        SyncKeyed(
            db.UserTeamSlots,
            db.UserTeamSlots.Where(x => x.UserId == userId),
            desiredSlots,
            x => (x.TeamGroupId, x.TeamNumber, x.Slot),
            s => (s.TeamGroupId, s.TeamNumber, s.Slot),
            s => new UserTeamSlotEntity
            {
                UserId = userId,
                TeamGroupId = s.TeamGroupId,
                TeamNumber = s.TeamNumber,
                Slot = s.Slot,
                Value = s.Value,
                ValueType = s.ValueType
            },
            (entity, s) =>
            {
                entity.Value = s.Value;
                entity.ValueType = s.ValueType;
            });

        var repSlots = (user.RepresentationTeamDataNew ?? [])
            .Select((csn, index) => (Slot: index, CharacterCsn: csn));

        SyncKeyed(
            db.UserRepresentationTeamSlots,
            db.UserRepresentationTeamSlots.Where(x => x.UserId == userId),
            repSlots,
            x => x.Slot,
            s => s.Slot,
            s => new UserRepresentationTeamSlotEntity
            {
                UserId = userId,
                Slot = s.Slot,
                CharacterCsn = s.CharacterCsn
            },
            (entity, s) => entity.CharacterCsn = s.CharacterCsn);
    }

    private static void SyncKeyed<TEntity, TKey, TSource>(
        DbSet<TEntity> set,
        IQueryable<TEntity> existingQuery,
        IEnumerable<TSource> sources,
        Func<TEntity, TKey> entityKey,
        Func<TSource, TKey> sourceKey,
        Func<TSource, TEntity> createEntity,
        Action<TEntity, TSource> updateEntity)
        where TEntity : class
        where TKey : notnull
    {
        var existingByKey = existingQuery.ToDictionary(entityKey);
        var seenKeys = new HashSet<TKey>();

        foreach (var src in sources)
        {
            var key = sourceKey(src);
            if (!seenKeys.Add(key))
                continue;

            if (existingByKey.TryGetValue(key, out var entity))
            {
                updateEntity(entity, src);
            }
            else
            {
                set.Add(createEntity(src));
            }
        }

        foreach (var (key, entity) in existingByKey)
        {
            if (!seenKeys.Contains(key))
            {
                set.Remove(entity);
            }
        }
    }
}
