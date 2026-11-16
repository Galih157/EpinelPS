using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Outpost.Dispatch;
using EpinelPS.LobbyServer.Pass;
using EpinelPS.Repositories;
using EpinelPS.Utils;

namespace EpinelPS.Services;

public sealed class OutpostService(
    OutpostRepository repo,
    ResetRepository resetRepo,
    CosmeticsRepository cosmeticsRepo,
    GameContext db)
{
    private const long MaxBattleTimeTicks = 864000000000;
    private const long MaxOverBattleTimeTicks = 12096000000000;

    // --- Outpost Overview & Battle Rewards ---

    public ResGetOutpostData GetOutpostData(User user)
    {
        user.ResetDataIfNeeded();

        // Enter reset: restore stamina on each outpost entry (private server convenience)
        var (_, infraCoreLvl) = repo.GetInfraCore(user.ID);
        var infracore = GameData.Instance.GetInfracoreGrade(infraCoreLvl);
        if (infracore != null)
        {
            int staminaVal = 2 + (infracore.FunctionList.Count > 4 ? infracore.FunctionList[4].Function : 0);
            user.Currency[CurrencyType.ContentStamina] = staminaVal;
            UserSessionStore.SaveUser(user);
        }

        var gameUser = UserDbHelper.GetOrCreateGameUser(db, user.ID);
        TimeSpan battleTime = DateTime.UtcNow - gameUser.BattleTime;
        long battleTimeMs = (long)(battleTime.TotalNanoseconds / 100);
        long overBattleTime = battleTimeMs > MaxOverBattleTimeTicks ? battleTimeMs - MaxOverBattleTimeTicks : 0;

        var battleLevel = repo.GetOutpostBattleLevel(user.ID);
        var buildings = repo.GetOrInitializeOutpostBuildings(user.ID);

        ResGetOutpostData response = new()
        {
            OutpostBattleLevel = battleLevel,
            Jukeboxv2 = new NetUserJukeboxDataV2
            {
                CommandBgm = new()
                {
                    Type = NetJukeboxBgmType.JukeboxTableId,
                    JukeboxTableId = cosmeticsRepo.GetJukeboxSetting(user.ID, NetJukeboxLocation.CommanderRoom).TableId
                }
            },
            OutpostBattleTime = new NetOutpostBattleTime
            {
                MaxBattleTime = MaxBattleTimeTicks,
                MaxOverBattleTime = MaxOverBattleTimeTicks,
                BattleTime = battleTimeMs,
                OverBattleTime = overBattleTime
            }
        };

        response.Jukeboxv2.JukeboxTableIds.AddRange(JukeboxUtils.GetUnlockedSongs(user.ID, cosmeticsRepo));
        response.Data.AddRange(buildings);
        response.TimeRewardBuffs.AddRange(NetUtils.GetOutpostTimeReward(user, repo));
        response.ConditionTriggerTidList.AddRange(repo.GetClearedOutpostScenarios(user.ID));

        return response;
    }

    public ResShowOutpostBattleReward ShowBattleReward(User user)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, user.ID);
        TimeSpan battleTime = DateTime.UtcNow - gameUser.BattleTime;
        long battleTimeMs = (long)(battleTime.TotalNanoseconds / 100);

        ResShowOutpostBattleReward response = new()
        {
            OutpostBattleLevel = repo.GetOutpostBattleLevel(user.ID),
            OutpostBattleTime = new NetOutpostBattleTime
            {
                MaxBattleTime = MaxBattleTimeTicks,
                MaxOverBattleTime = MaxOverBattleTimeTicks,
                BattleTime = battleTimeMs,
                OverBattleTime = 0
            },
            BattleTime = 0,
            FastBattleCount = resetRepo.GetWipeoutCount(user.ID),
            MaxBattleTime = MaxBattleTimeTicks,
            Reward = NetUtils.GetOutpostReward(user, battleTime, false, repo)
        };
        response.TimeRewardBuffs.AddRange(NetUtils.GetOutpostTimeReward(user, repo));

        return response;
    }

    public ResObtainOutpostBattleReward ObtainOutpostReward(User user)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, user.ID);
        TimeSpan battleTime = DateTime.UtcNow - gameUser.BattleTime;

        ResObtainOutpostBattleReward response = new()
        {
            OutpostBattleTime = new NetOutpostBattleTime
            {
                MaxBattleTime = MaxBattleTimeTicks,
                MaxOverBattleTime = MaxOverBattleTimeTicks,
                BattleTime = 0,
                OverBattleTime = 0
            },
            BattleTime = 0,
            MaxBattleTime = MaxBattleTimeTicks,
            Reward = NetUtils.GetOutpostReward(user, battleTime, true, repo)
        };

        gameUser.BattleTime = DateTime.UtcNow;
        db.SaveChanges();

        user.AddTrigger(Trigger.OutpostBattleReward, 1);
        UserSessionStore.SaveUser(user);

        return response;
    }

    public ResObtainFastBattleReward DoWipeout(User user)
    {
        if (resetRepo.GetWipeoutCount(user.ID) >= 12)
            throw new InvalidOperationException("wipeout count cannot exceed 12.");

        ResObtainFastBattleReward response = new()
        {
            FastBattleCount = resetRepo.IncrementWipeoutCount(user.ID)
        };
        db.SaveChanges();

        response.Reward = NetUtils.GetOutpostReward(user, TimeSpan.FromHours(2), true, repo);
        AppendUserCurrencies(user, response.Currencies);

        user.AddTrigger(Trigger.OutpostFastBattleReward, 1);
        UserSessionStore.SaveUser(user);

        return response;
    }

    // --- Outpost Buildings ---

    public ResBuilding BuildBuilding(User user, ReqBuilding req)
    {
        BuildingCost cost = GetBuildingCost(req.BuildingId);
        if (!user.CanSubtractCurrency(CurrencyType.Gold, cost.Gold) ||
            !user.SubtractCurrency(CurrencyType.Gold, cost.Gold))
        {
            return new ResBuilding { StartAt = 0, CompleteAt = 0 };
        }

        DateTime startTime = DateTime.UtcNow;
        DateTime completeTime = startTime.AddMinutes(cost.BuildTimeMinutes);

        NetUserOutpostData newBuilding = new()
        {
            SlotId = req.PositionId,
            BuildingId = req.BuildingId,
            IsDone = false,
            StartAt = startTime.Ticks,
            CompleteAt = completeTime.Ticks
        };

        var buildings = repo.GetOutpostBuildings(user.ID);
        int existingIndex = buildings.FindIndex(b => b.SlotId == req.PositionId);
        if (existingIndex >= 0)
            buildings[existingIndex] = newBuilding;
        else
            buildings.Add(newBuilding);

        repo.SaveOutpostBuildings(user.ID, buildings);
        UserSessionStore.SaveUser(user);

        return new ResBuilding
        {
            StartAt = newBuilding.StartAt,
            CompleteAt = newBuilding.CompleteAt
        };
    }

    public ResMoveBuilding MoveBuilding(ulong userId, ReqMoveBuilding req)
    {
        var buildings = repo.GetOutpostBuildings(userId);
        NetUserOutpostData? src = buildings.FirstOrDefault(x => x.SlotId == req.SrcPositionId);
        NetUserOutpostData? dst = buildings.FirstOrDefault(x => x.SlotId == req.DstPositionId);

        if (src != null && dst != null)
        {
            (src.SlotId, dst.SlotId) = (dst.SlotId, src.SlotId);
            repo.SaveOutpostBuildings(userId, buildings);
        }

        return new ResMoveBuilding();
    }

    private static BuildingCost GetBuildingCost(int buildingId)
    {
        return buildingId switch
        {
            // building (22xxx)
            22401 => new BuildingCost { Gold = 1000, BuildTimeMinutes = 5 },
            22701 => new BuildingCost { Gold = 2000, BuildTimeMinutes = 10 },
            22801 => new BuildingCost { Gold = 1500, BuildTimeMinutes = 8 },
            22901 => new BuildingCost { Gold = 3000, BuildTimeMinutes = 15 },
            23001 => new BuildingCost { Gold = 5000, BuildTimeMinutes = 20 },
            23100 => new BuildingCost { Gold = 800, BuildTimeMinutes = 6 },
            23200 => new BuildingCost { Gold = 1200, BuildTimeMinutes = 8 },
            23300 => new BuildingCost { Gold = 2500, BuildTimeMinutes = 12 },
            23400 => new BuildingCost { Gold = 2000, BuildTimeMinutes = 10 },
            23500 => new BuildingCost { Gold = 1800, BuildTimeMinutes = 9 },

            // building (10xxx-12xxx)
            10100 => new BuildingCost { Gold = 500, BuildTimeMinutes = 3 },
            10200 => new BuildingCost { Gold = 800, BuildTimeMinutes = 5 },
            10300 => new BuildingCost { Gold = 1200, BuildTimeMinutes = 7 },
            10400 => new BuildingCost { Gold = 900, BuildTimeMinutes = 6 },
            10500 => new BuildingCost { Gold = 700, BuildTimeMinutes = 4 },
            10600 => new BuildingCost { Gold = 1500, BuildTimeMinutes = 8 },
            10700 => new BuildingCost { Gold = 1100, BuildTimeMinutes = 7 },
            10800 => new BuildingCost { Gold = 1300, BuildTimeMinutes = 8 },
            10900 => new BuildingCost { Gold = 1600, BuildTimeMinutes = 9 },
            11000 => new BuildingCost { Gold = 1000, BuildTimeMinutes = 6 },
            11100 => new BuildingCost { Gold = 1400, BuildTimeMinutes = 8 },
            11200 => new BuildingCost { Gold = 1800, BuildTimeMinutes = 10 },
            11300 => new BuildingCost { Gold = 800, BuildTimeMinutes = 5 },
            11400 => new BuildingCost { Gold = 2000, BuildTimeMinutes = 11 },
            11500 => new BuildingCost { Gold = 1200, BuildTimeMinutes = 7 },
            11600 => new BuildingCost { Gold = 1700, BuildTimeMinutes = 9 },
            11700 => new BuildingCost { Gold = 1300, BuildTimeMinutes = 8 },
            11800 => new BuildingCost { Gold = 900, BuildTimeMinutes = 6 },
            11900 => new BuildingCost { Gold = 2200, BuildTimeMinutes = 12 },
            12000 => new BuildingCost { Gold = 1500, BuildTimeMinutes = 9 },
            12100 => new BuildingCost { Gold = 3000, BuildTimeMinutes = 15 },
            12200 => new BuildingCost { Gold = 2800, BuildTimeMinutes = 14 },
            12300 => new BuildingCost { Gold = 1600, BuildTimeMinutes = 9 },

            _ => new BuildingCost { Gold = 1000, BuildTimeMinutes = 5 }
        };
    }

    // --- InfraCore ---

    public ResCheckReceiveInfraCoreReward CheckInfracore(ulong userId)
    {
        var (_, currentLevel) = repo.GetInfraCore(userId);
        InfraCoreGradeRecord? gradeData = GameData.Instance.GetInfracoreGrade(currentLevel);

        bool isReceived = gradeData != null && gradeData.RewardId > 0 &&
                          repo.IsInfraCoreRewardReceived(userId, currentLevel);

        return new ResCheckReceiveInfraCoreReward { IsReceived = isReceived };
    }

    public ResObtainInfraCoreReward ObtainInfracoreReward(User user)
    {
        ResObtainInfraCoreReward response = new();
        var (_, currentLevel) = repo.GetInfraCore(user.ID);

        InfraCoreGradeRecord? gradeData = GameData.Instance.GetInfracoreGrade(currentLevel);
        if (gradeData != null && gradeData.RewardId > 0 && !repo.IsInfraCoreRewardReceived(user.ID, currentLevel))
        {
            repo.MarkInfraCoreRewardReceived(user.ID, currentLevel);
            response.Reward = RewardUtils.RegisterRewardsForUser(user, gradeData.RewardId);
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    // --- Tactic Academy & Sudden Events ---

    public ResGetTacticAcademyData GetTacticAcademyData(ulong userId)
    {
        ResGetTacticAcademyData response = new();
        response.ClearLessons.AddRange(repo.GetCompletedTacticAcademyLessons(userId));
        return response;
    }

    public ResTacticAcademyClearLesson ClearTacticAcademyLesson(User user, ReqTacticAcademyClearLesson req)
    {
        ResTacticAcademyClearLesson response = new()
        {
            ClearLessonTid = req.LessonTid
        };

        TacticAcademyFunctionRecord lesson = GameData.Instance.GetTacticAcademyLesson(req.LessonTid);
        if (user.CanSubtractCurrency((CurrencyType)lesson.CurrencyId, lesson.CurrencyValue))
        {
            user.SubtractCurrency((CurrencyType)lesson.CurrencyId, lesson.CurrencyValue);
            repo.AddTacticAcademyLesson(user.ID, req.LessonTid);
            ProcessLessonReward(user.ID, lesson);

            AppendUserCurrencies(user, response.Currencies);
            UserSessionStore.SaveUser(user);
        }
        else
        {
            Console.WriteLine($"User {user.ID} tried to clear lesson {req.LessonTid} without enough currency");
        }

        return response;
    }

    private void ProcessLessonReward(ulong userId, TacticAcademyFunctionRecord record)
    {
        if (record.LessonReward == null)
        {
            Console.WriteLine("Warning: lesson_reward shouldnt be null");
            return;
        }

        if (record.LessonType == LessonType.OutpostBattle)
        {
            var buffs = repo.GetOutpostBuffs(userId);
            bool modified = false;
            foreach (var item in record.LessonReward)
            {
                if (item.LessonRewardId != 0 && item.LessonRewardValue != 0)
                {
                    buffs.GetPercentages((CurrencyType)item.LessonRewardId).Add(item.LessonRewardValue);
                    modified = true;
                }
            }

            if (modified)
                repo.SaveOutpostBuffs(userId, buffs);
        }
        else
        {
            Console.WriteLine("Warning: unknown lesson type: " + record.LessonType);
        }
    }

    public ResStartSuddenEvent StartSuddenEvent(User user, ReqStartSuddenEvent req)
    {
        var suddenEvent = GameData.Instance.SuddenEventConditions[req.Tid];
        if (repo.HasClearedOutpostScenario(user.ID, suddenEvent.Id))
            throw new InvalidOperationException();

        if (!user.CanSubtractCurrency(CurrencyType.ContentStamina, 1))
            throw new InvalidOperationException("Not enough stamina");

        user.SubtractCurrency(CurrencyType.ContentStamina, 1);

        ResStartSuddenEvent response = new()
        {
            Currency = new()
            {
                Type = (int)CurrencyType.ContentStamina,
                Value = user.GetCurrencyVal(CurrencyType.ContentStamina)
            },
            Reward = RewardUtils.RegisterRewardsForUser(user, GameData.Instance.GetRewardTableEntry(suddenEvent.RewardId))
        };

        repo.AddClearedOutpostScenario(user.ID, suddenEvent.Id);
        UserSessionStore.SaveUser(user);

        return response;
    }

    // --- Recycle Room ---

    public ResGetRecycleRoomData GetRecycleRoomData(ulong userId)
    {
        ResGetRecycleRoomData response = new();
        response.Recycle.AddRange(repo.GetResearchProgress(userId).Select(progress => new NetUserRecycleRoomData
        {
            Tid = progress.Key,
            Lv = progress.Value.Level
        }));
        return response;
    }

    public ResRecycleRunResearch RunResearch(ulong userId, ReqRecycleRunResearch req)
    {
        var progress = repo.GetResearchProgress(userId, req.Tid);
        if (progress is null)
        {
            RecycleResearchStatRecord researchRecord = GameData.Instance.RecycleResearchStats.Values
                .FirstOrDefault(e => e.Id == req.Tid)
                ?? throw new Exception("not found research record with tId " + req.Tid);

            progress = new RecycleRoomResearchProgress
            {
                Attack = researchRecord.Attack,
                Defense = researchRecord.Defence,
                Hp = researchRecord.Hp
            };
            repo.UpsertResearchProgress(userId, req.Tid, progress);
        }

        return new ResRecycleRunResearch
        {
            Recycle = new NetUserRecycleRoomData
            {
                Tid = req.Tid,
                Lv = progress.Level
            }
        };
    }

    public ResRecycleLevelUpResearch LevelUpResearch(User user, ReqRecycleLevelUpResearch req)
    {
        ResRecycleLevelUpResearch response = new();
        var progress = repo.GetResearchProgress(user.ID, req.Tid);
        if (progress != null)
        {
            AddProgressToResearch(response, user, progress, req);
            repo.UpsertResearchProgress(user.ID, req.Tid, progress);
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResPersonalResearchRecycleLevelUp PersonalResearchLevelUp(User user, ReqPersonalResearchRecycleLevelUp req)
    {
        ResPersonalResearchRecycleLevelUp response = new();
        const int personalResearchTid = 1001;
        RecycleRoomResearchProgress personalResearchProgress = repo.GetResearchProgress(user.ID, personalResearchTid)
            ?? throw new Exception("PersonalRearch not found.");

        if (!GameData.Instance.RecycleResearchStats.TryGetValue(personalResearchTid, out RecycleResearchStatRecord? statRecord))
            return response;

        for (int i = 0; i < req.LevelUpCount; i++)
        {
            RecycleResearchLevelRecord? levelRecord = GameData.Instance.RecycleResearchLevels.Values
                .Where(e => e.RecycleType == statRecord.RecycleType && e.RecycleSubType == statRecord.RecycleSubType)
                .FirstOrDefault(e => e.RecycleLevel == personalResearchProgress.Level);
            if (levelRecord is null) break;

            DbItemData? usedItem = user.Items.FirstOrDefault(e => e.ItemType == levelRecord.ItemId);
            if (usedItem is null || usedItem.Count < levelRecord.ItemValue) break;

            usedItem.Count -= levelRecord.ItemValue;
            response.Item = NetUtils.UserItemDataToNet(usedItem);
            personalResearchProgress.Level += 1;
            personalResearchProgress.Hp = statRecord.Hp * personalResearchProgress.Level;
        }

        response.Recycle = new NetUserRecycleRoomData
        {
            Tid = personalResearchTid,
            Lv = personalResearchProgress.Level
        };

        repo.UpsertResearchProgress(user.ID, personalResearchTid, personalResearchProgress);
        UserSessionStore.SaveUser(user);

        return response;
    }

    private static void AddProgressToResearch(
        ResRecycleLevelUpResearch response,
        User user,
        RecycleRoomResearchProgress progress,
        ReqRecycleLevelUpResearch req)
    {
        if (!GameData.Instance.RecycleResearchStats.TryGetValue(req.Tid, out RecycleResearchStatRecord? statRecord))
            return;

        RecycleResearchLevelRecord? levelRecord = GameData.Instance.RecycleResearchLevels.Values
            .Where(e => e.RecycleType == statRecord.RecycleType && e.RecycleSubType == statRecord.RecycleSubType)
            .FirstOrDefault(e => e.RecycleLevel == progress.Level);
        if (levelRecord is null)
            return;

        if (statRecord.RecycleType == RecycleType.Personal)
        {
            DbItemData? usedItem = user.Items.FirstOrDefault(e => e.ItemType == levelRecord.ItemId);
            if (usedItem is null || usedItem.Count < levelRecord.ItemValue)
                return;

            usedItem.Count -= levelRecord.ItemValue;
            response.Items.Add(NetUtils.UserItemDataToNet(usedItem));

            progress.Level += 1;
            progress.Hp = statRecord.Hp * progress.Level;
            response.Recycle = new NetUserRecycleRoomData
            {
                Tid = req.Tid,
                Lv = progress.Level
            };
        }
        else if (statRecord.RecycleType == RecycleType.Class || statRecord.RecycleType == RecycleType.Corporation)
        {
            for (int i = 0; i < req.LevelUpCount; i++)
            {
                RecycleResearchLevelRecord? currentLevelRecord = GameData.Instance.RecycleResearchLevels.Values
                    .Where(e => e.RecycleType == statRecord.RecycleType && e.RecycleSubType == statRecord.RecycleSubType)
                    .FirstOrDefault(e => e.RecycleLevel == progress.Level);
                if (currentLevelRecord is null) break;

                DbItemData? usedItem = user.Items.FirstOrDefault(e => e.ItemType == currentLevelRecord.ItemId);
                if (usedItem is null || usedItem.Count < currentLevelRecord.ItemValue) break;

                usedItem.Count -= currentLevelRecord.ItemValue;
                response.Items.Add(NetUtils.UserItemDataToNet(usedItem));
                progress.Level += 1;
            }

            response.Recycle = new NetUserRecycleRoomData
            {
                Tid = req.Tid,
                Lv = progress.Level
            };
        }
        else
        {
            throw new Exception($"unknown recycle type {statRecord.RecycleType}");
        }
    }

    // --- Dispatch ---

    public ResGetDispatchList GetDispatchList(User user)
    {
        DispatchHelper.SyncDispatchLevel(user.ID, repo);
        var state = repo.GetDispatchState(user.ID);
        ResGetDispatchList response = new();

        List<DispatchBoardData> dispatch = GameData.Instance.DispatchBoardTable.Values
            .First(x => x.DispatchType == DispatchType.Dispatch && x.DispatchBoardLv == state.DispatchLv)
            .DispatchList;
        DispatchBoardRecord? dispatchcol = GameData.Instance.DispatchBoardTable.Values
            .FirstOrDefault(x => x.DispatchType == DispatchType.DispatchCollection && x.DispatchBoardLv == state.DispatchCollectionLv);
        DispatchBoardRecord? dispatchfav = GameData.Instance.DispatchBoardTable.Values
            .FirstOrDefault(x => x.DispatchType == DispatchType.DispatchFavorite && x.DispatchBoardLv == state.DispatchFavoriteLv);

        DateTime startTime = DateTime.UtcNow;
        int dateDay = user.GetDateDay();

        if (state.Today != dateDay)
        {
            state.Today = dateDay;
            state.DispatchDatas = [];
            state.DispatchClearList = [];
            state.SelectableDispatchData = [];
            state.DispatchResetCount = 0;

            List<DispatchRecord> availableDispatchTable = [];
            int dispatchCount = resetRepo.GetDispatchCount(user.ID);
            AppendRandomDispatches(dispatch, dispatchCount, startTime, availableDispatchTable, state, response);

            if (dispatchcol != null)
                AppendRandomDispatches(dispatchcol.DispatchList, dispatchcol.DispatchMax, startTime, availableDispatchTable, state, response);

            if (dispatchfav != null)
                AppendRandomDispatches(dispatchfav.DispatchList, dispatchfav.DispatchMax, startTime, availableDispatchTable, state, response);

            repo.SaveDispatchState(user.ID, state);
        }
        else
        {
            response.DispatchList.AddRange(state.DispatchDatas);
        }

        response.DispatchResetCount = state.DispatchResetCount;
        return response;
    }

    public ResResetDispatchList ResetDispatchList(ulong userId)
    {
        DispatchHelper.SyncDispatchLevel(userId, repo);
        var state = repo.GetDispatchState(userId);
        ResResetDispatchList response = new();
        DateTime startTime = DateTime.UtcNow;

        state.DispatchResetCount++;

        var reset = GameData.Instance.DispatchResetTable.Values.FirstOrDefault(x => x.Id == state.DispatchResetCount);
        if (reset != null)
        {
            response.Currencies.Add(new NetUserCurrencyData { Type = (int)reset.CurrencyType, Value = reset.CurrencyValue });
        }
        else
        {
            response.Currencies.Add(new NetUserCurrencyData { Type = (int)CurrencyType.FreeCash, Value = 50 });
        }

        List<NetUserDispatchData> idleList = state.DispatchDatas.Where(x => x.IsRun == 0).ToList();
        List<NetUserDispatchData> runningList = state.DispatchDatas.Where(x => x.IsRun == 1).ToList();
        List<int> usedTids = state.DispatchDatas.Select(x => x.Tid).ToList();

        foreach (NetUserDispatchData data in idleList)
        {
            DispatchRecord dispatch = GameData.Instance.DispatchTable.Values.First(c => c.Id == data.Tid);
            List<DispatchRecord> candidates = GameData.Instance.DispatchTable.Values
                .Where(x => x.DispatchGroup == dispatch.DispatchGroup && !usedTids.Contains(x.Id))
                .ToList();

            var picked = candidates[Random.Shared.Next(0, candidates.Count)];
            usedTids.Add(picked.Id);

            NetUserDispatchData netUserDispatch = new()
            {
                Tid = picked.Id,
                IsRun = 0,
                StartAt = startTime.Ticks,
                EndAt = startTime.AddMinutes(picked.TimeMin).Ticks
            };

            state.DispatchDatas.Add(netUserDispatch);
            state.DispatchDatas.RemoveAll(x => x.Tid == data.Tid);
            usedTids.Remove(data.Tid);
            response.DispatchList.Add(netUserDispatch);
        }

        repo.SaveDispatchState(userId, state);

        response.DispatchList.AddRange(runningList);
        response.DispatchResetCount = state.DispatchResetCount;
        return response;
    }

    public ResRunDispatch RunDispatch(ulong userId, ReqRunDispatch req)
    {
        var state = repo.GetDispatchState(userId);
        var activated = ActivateDispatch(state, req.Tid, req.SelectSlotId, DateTime.UtcNow);
        repo.SaveDispatchState(userId, state);

        return new ResRunDispatch { DispatchData = activated };
    }

    public ResRunDispatchList RunDispatchList(ulong userId, ReqRunDispatchList req)
    {
        var state = repo.GetDispatchState(userId);
        ResRunDispatchList response = new();
        DateTime startTime = DateTime.UtcNow;

        foreach (var item in req.DispatchData)
        {
            ActivateDispatch(state, item.Tid, item.SelectSlotId, startTime);
            response.SuccessTidList.Add(item.Tid);
        }

        repo.SaveDispatchState(userId, state);
        return response;
    }

    public ResCancelDispatch CancelDispatch(ulong userId, ReqCancelDispatch req)
    {
        var state = repo.GetDispatchState(userId);
        DateTime startTime = DateTime.UtcNow;
        DispatchRecord? dispatch = GameData.Instance.DispatchTable.Values.FirstOrDefault(x => x.Id == req.Tid);

        var dispatchData = state.DispatchDatas.FirstOrDefault(x => x.Tid == req.Tid);
        if (dispatchData != null && dispatch != null)
        {
            dispatchData.IsRun = 0;
            dispatchData.StartAt = startTime.Ticks;
            dispatchData.EndAt = startTime.AddMinutes(dispatch.TimeMin).Ticks;
        }

        state.SelectableDispatchData.RemoveAll(cs => cs.SelectTid == req.Tid);
        repo.SaveDispatchState(userId, state);

        return new ResCancelDispatch();
    }

    public ResObtainDispatchReward ObtainDispatchReward(User user, ReqObtainDispatchReward req)
    {
        var state = repo.GetDispatchState(user.ID);
        NetRewardData rewardData = new();

        foreach (int item in req.TidList)
        {
            DispatchRecord dispatch = GameData.Instance.DispatchTable.Values.First(c => c.Id == item);
            PassHelper.RewardsForUser(user, ref rewardData, dispatch.RewardId);

            if (!state.DispatchClearList.Contains(item))
                state.DispatchClearList.Add(item);
            state.DispatchDatas.RemoveAll(x => x.Tid == item);

            user.AddTrigger(Trigger.SendDispatch, 1, 0);
            user.AddTrigger(Trigger.SendDispatchGrade, 1, dispatch.DispatchGradeId);
        }

        List<NetSelectableDispatchData> dispatchTable = state.SelectableDispatchData
            .Where(x => state.DispatchClearList.Contains(x.SelectTid))
            .ToList();

        repo.SaveDispatchState(user.ID, state);
        UserSessionStore.SaveUser(user);

        ResObtainDispatchReward response = new() { Reward = rewardData };
        response.DispatchList.AddRange(state.DispatchDatas);
        response.SelectableDispatchList.AddRange(dispatchTable);

        return response;
    }

    public ResDeleteDispatchSelection DeleteDispatch(ReqDeleteDispatchSelection req)
    {
        Logging.WriteLine($"List {req.SelectableDispatchList}", LogType.Info);
        return new ResDeleteDispatchSelection();
    }

    private static void AppendRandomDispatches(
        List<DispatchBoardData> boardList,
        int count,
        DateTime startTime,
        List<DispatchRecord> availableDispatchTable,
        OutpostDispatchState state,
        ResGetDispatchList response)
    {
        for (int i = 0; i < count; i++)
        {
            DispatchBoardData groupData = DispatchHelper.SelectItemByProbability(boardList, x => x.DispatchProb);
            List<DispatchRecord> candidates = GameData.Instance.DispatchTable.Values
                .Where(x => x.DispatchGroup == groupData.DispatchGroup && !availableDispatchTable.Any(d => d.Id == x.Id))
                .ToList();

            var picked = candidates[Random.Shared.Next(0, candidates.Count)];
            availableDispatchTable.Add(picked);

            NetUserDispatchData netUserDispatch = new()
            {
                Tid = picked.Id,
                IsRun = 0,
                StartAt = startTime.Ticks,
                EndAt = startTime.AddMinutes(picked.TimeMin).Ticks
            };

            state.DispatchDatas.Add(netUserDispatch);
            response.DispatchList.Add(netUserDispatch);
        }
    }

    private static NetUserDispatchData ActivateDispatch(
        OutpostDispatchState state,
        int tid,
        int selectSlotId,
        DateTime startTime)
    {
        DispatchRecord dispatch = GameData.Instance.DispatchTable.Values.First(x => x.Id == tid);
        long startTicks = startTime.Ticks;
        long endTicks = startTime.AddMinutes(dispatch.TimeMin).Ticks;

        var dispatchData = state.DispatchDatas.FirstOrDefault(x => x.Tid == tid);
        if (dispatchData != null)
        {
            dispatchData.IsRun = 1;
            dispatchData.StartAt = startTicks;
            dispatchData.EndAt = endTicks;
        }

        state.SelectableDispatchData.Add(new NetSelectableDispatchData
        {
            DispatchGroupId = dispatch.DispatchGradeId,
            IsRun = true,
            StartAt = startTicks,
            EndAt = endTicks,
            SelectSlotId = selectSlotId,
            SelectTid = tid
        });

        return new NetUserDispatchData
        {
            Tid = tid,
            IsRun = 1,
            StartAt = startTicks,
            EndAt = endTicks
        };
    }

    // --- Synchro Device ---

    public ResGetSynchroData GetSynchroDevice(User user)
    {
        var slots = repo.GetOrInitializeSynchroSlots(user.ID);
        var (upgraded, deviceLevel) = repo.GetSynchroDeviceState(user.ID);
        var synchroCsns = slots.Where(x => x.CharacterSerialNumber != 0)
            .Select(x => x.CharacterSerialNumber)
            .ToHashSet();

        List<CharacterModel> highestLevelCharacters = [.. user.Characters.OrderByDescending(x => x.Level).Take(5)];

        ResGetSynchroData response = new()
        {
            Synchro = new NetUserSynchroData()
        };

        foreach (CharacterModel item in highestLevelCharacters)
        {
            response.Synchro.StandardCharacters.Add(new NetUserCharacterData
            {
                Default = ToNetCharacterDefault(item, item.Level),
                IsSynchro = item.Csn != 0 && synchroCsns.Contains(item.Csn)
            });
        }

        foreach (SynchroSlot item in slots)
        {
            response.Synchro.Slots.Add(new NetSynchroSlot
            {
                Slot = item.Slot,
                AvailableRegisterAt = 1,
                Csn = item.CharacterSerialNumber
            });
        }

        int highestLevel = user.Characters.Count > 0 ? user.Characters.Max(c => c.Level) : 0;
        int ownedCount = user.Characters.Count;
        int totalLimitBreaks = user.Characters.Sum(c => Math.Min(c.Grade, 3));
        response.Synchro.SynchroMaxLv = (int)(highestLevel + ownedCount + totalLimitBreaks * 1.334);
        response.Synchro.SynchroLv = OutpostRepository.ComputeSynchroLevel(upgraded, deviceLevel, user.Characters);
        response.Synchro.IsChanged = upgraded;

        return response;
    }

    public ResSynchroRegister RegisterSynchroDevice(User user, ReqSynchroRegister req)
    {
        CharacterModel targetCharacter = user.GetCharacterBySerialNumber(req.Csn)
            ?? throw new Exception("target character does not exist");

        var slots = repo.GetSynchroSlots(user.ID);
        ResSynchroRegister response = new();
        bool changed = false;

        foreach (SynchroSlot item in slots)
        {
            if (item.Slot == req.Slot)
            {
                if (item.CharacterSerialNumber != 0)
                {
                    Console.WriteLine("must remove character from synchrodevice first");
                }
                else
                {
                    item.CharacterSerialNumber = req.Csn;
                    changed = true;
                    response.IsSynchro = true;
                    response.Character = ToNetCharacterDefault(targetCharacter, repo.GetSynchroLevel(user.ID, user.Characters));
                    response.Slot = new NetSynchroSlot
                    {
                        AvailableRegisterAt = item.AvailableAt,
                        Csn = item.CharacterSerialNumber,
                        Slot = item.Slot
                    };
                }
            }
        }

        if (changed)
            repo.SaveSynchroSlots(user.ID, slots);

        return response;
    }

    public ResSynchroUnregist UnregisterSynchroDevice(User user, ReqSynchroUnregist req)
    {
        var slots = repo.GetSynchroSlots(user.ID);
        ResSynchroUnregist response = new();
        bool changed = false;

        foreach (SynchroSlot item in slots)
        {
            if (item.Slot == req.Slot)
            {
                if (item.CharacterSerialNumber == 0)
                {
                    Logging.WriteLine("must add character from synchrodevice first", LogType.Warning);
                }
                else
                {
                    long oldCsn = item.CharacterSerialNumber;
                    item.CharacterSerialNumber = 0;
                    changed = true;

                    CharacterModel data = user.GetCharacterBySerialNumber(oldCsn)
                        ?? throw new Exception("failed to lookup character");

                    response.Character = ToNetCharacterDefault(data, data.Level);
                    response.Slot = new NetSynchroSlot
                    {
                        AvailableRegisterAt = item.AvailableAt,
                        Csn = item.CharacterSerialNumber,
                        Slot = item.Slot
                    };
                    response.IsSynchro = false;

                    foreach (CharacterModel high in user.Characters.OrderByDescending(x => x.Level).Take(5))
                        response.SynchroStandardCharacters.Add(high.Csn);

                    response.SynchroLv = repo.GetSynchroLevel(user.ID, user.Characters);
                }
            }
        }

        if (changed)
            repo.SaveSynchroSlots(user.ID, slots);

        return response;
    }

    public ResSynchroAddSlot SynchroAddSlot(ulong userId, ReqSynchroAddSlot req)
    {
        var slots = repo.GetSynchroSlots(userId);
        ResSynchroAddSlot response = new();

        SynchroSlot? slot = slots.FirstOrDefault(x => x.Slot == req.Slot);
        if (slot != null)
        {
            response.Slot = new NetSynchroSlot
            {
                Csn = slot.CharacterSerialNumber,
                Slot = slot.Slot,
                AvailableRegisterAt = slot.AvailableAt
            };
        }
        else
        {
            NetSynchroSlot newSlot = new()
            {
                Csn = 0,
                Slot = req.Slot,
                AvailableRegisterAt = 0
            };

            slots.Add(new SynchroSlot
            {
                Slot = newSlot.Slot,
                CharacterSerialNumber = newSlot.Csn,
                AvailableAt = newSlot.AvailableRegisterAt
            });
            repo.SaveSynchroSlots(userId, slots);

            response.Slot = newSlot;
        }

        return response;
    }

    public ResSynchroAddSlot SynchroAddSlotByItem(ulong userId)
    {
        var slots = repo.GetOrInitializeSynchroSlots(userId);
        NetSynchroSlot newSlot = new()
        {
            Csn = 0,
            Slot = slots.Last().Slot + 1,
            AvailableRegisterAt = 0
        };

        slots.Add(new SynchroSlot
        {
            Slot = newSlot.Slot,
            CharacterSerialNumber = newSlot.Csn,
            AvailableAt = newSlot.AvailableRegisterAt
        });
        repo.SaveSynchroSlots(userId, slots);

        return new ResSynchroAddSlot { Slot = newSlot };
    }

    public ResSynchroRegisterOneClick SynchroRegisterOneClick(User user, ReqSynchroRegisterOneClick req)
    {
        var slots = repo.GetSynchroSlots(user.ID);
        bool changed = false;

        foreach (NetOneClickSlot oneClickSlot in req.Slots)
        {
            var slot = slots.FirstOrDefault(s => s.Slot == oneClickSlot.Slot);
            if (slot != null && slot.CharacterSerialNumber == 0)
            {
                slot.CharacterSerialNumber = oneClickSlot.Csn;
                changed = true;
            }
        }

        if (changed)
            repo.SaveSynchroSlots(user.ID, slots);

        int synchroLv = repo.GetSynchroLevel(user.ID, user.Characters);
        ResSynchroRegisterOneClick response = new() { IsSynchro = true };

        foreach (SynchroSlot slot in slots)
        {
            response.Slots.Add(new NetSynchroSlot
            {
                Slot = slot.Slot,
                AvailableRegisterAt = 1,
                Csn = slot.CharacterSerialNumber
            });
        }

        foreach (SynchroSlot slot in slots)
        {
            if (slot.CharacterSerialNumber == 0) continue;
            CharacterModel? character = user.GetCharacterBySerialNumber(slot.CharacterSerialNumber);
            if (character == null) continue;

            response.Characters.Add(ToNetCharacterDefault(character, synchroLv));
        }

        return response;
    }

    public ResSynchroChange ChangeSynchroDevice(User user)
    {
        var slots = repo.GetSynchroSlots(user.ID);
        var synchroCsns = slots.Where(x => x.CharacterSerialNumber != 0)
            .Select(x => x.CharacterSerialNumber)
            .ToHashSet();

        ResSynchroChange response = new();
        List<CharacterModel> highestLevelCharacters = [.. user.Characters.OrderByDescending(x => x.Level).Take(5)];

        int slotNumber = 1;
        foreach (CharacterModel item in highestLevelCharacters)
        {
            if (item.Level != 200)
                throw new Exception("expected level to be 200");

            response.Characters.Add(new NetUserCharacterData
            {
                Default = ToNetCharacterDefault(item, item.Level),
                IsSynchro = item.Csn != 0 && synchroCsns.Contains(item.Csn)
            });

            var targetSlot = slots.FirstOrDefault(s => s.Slot == slotNumber);
            if (targetSlot != null)
                targetSlot.CharacterSerialNumber = item.Csn;

            slotNumber++;
        }

        repo.SaveSynchroSlots(user.ID, slots);
        var (_, deviceLevel) = repo.GetSynchroDeviceState(user.ID);
        repo.SaveSynchroDeviceState(user.ID, upgraded: true, deviceLevel);

        foreach (SynchroSlot item in slots)
        {
            response.Slots.Add(new NetSynchroSlot
            {
                Slot = item.Slot,
                AvailableRegisterAt = item.AvailableAt,
                Csn = item.CharacterSerialNumber
            });
        }

        return response;
    }

    public ResSynchroLevelUp SynchroLevelUp(User user)
    {
        ResSynchroLevelUp response = new();
        Dictionary<int, CharacterLevelRecord> data = GameData.Instance.GetCharacterLevelUpData();
        var (upgraded, deviceLevel) = repo.GetSynchroDeviceState(user.ID);

        if (!data.TryGetValue(deviceLevel + 1, out var levelUpData))
            return response;

        int requiredCredit = levelUpData.Gold;
        int requiredBattleData = levelUpData.CharacterExp;
        int requiredCoreDust = levelUpData.CharacterExp2;

        if (user.CanSubtractCurrency(CurrencyType.Gold, requiredCredit) &&
            user.CanSubtractCurrency(CurrencyType.CharacterExp, requiredBattleData) &&
            user.CanSubtractCurrency(CurrencyType.CharacterExp2, requiredCoreDust))
        {
            user.SubtractCurrency(CurrencyType.Gold, requiredCredit);
            user.SubtractCurrency(CurrencyType.CharacterExp, requiredBattleData);
            user.SubtractCurrency(CurrencyType.CharacterExp2, requiredCoreDust);
            deviceLevel++;
            repo.SaveSynchroDeviceState(user.ID, upgraded, deviceLevel);
        }
        else
        {
            Console.WriteLine("ERROR: Not enough currency for upgrade");
            return response;
        }

        AppendUserCurrencies(user, response.Currencies);
        response.SynchroLv = deviceLevel;

        user.AddTrigger(Trigger.CharacterLevelUpCount, 1);
        UserSessionStore.SaveUser(user);

        return response;
    }

    public ResSynchroDeviceOneClick SynchroDeviceOneClick(User user, ReqSynchroDeviceOneClick req)
    {
        ResSynchroDeviceOneClick response = new();
        var (_, deviceLevel) = repo.GetSynchroDeviceState(user.ID);

        int targetLv = req.SynchroLv;
        if (targetLv > deviceLevel)
        {
            var levelData = GameData.Instance.GetCharacterLevelUpData();

            int totalGold = 0;
            int totalExp = 0;
            int totalExp2 = 0;

            int lv;
            for (lv = deviceLevel; lv < targetLv; lv++)
            {
                if (levelData.TryGetValue(lv + 1, out var data))
                {
                    totalGold += data.Gold;
                    totalExp += data.CharacterExp;
                    totalExp2 += data.CharacterExp2;
                }
                else
                {
                    break;
                }
            }

            if (lv > deviceLevel &&
                user.CanSubtractCurrency(CurrencyType.Gold, totalGold) &&
                user.CanSubtractCurrency(CurrencyType.CharacterExp, totalExp) &&
                user.CanSubtractCurrency(CurrencyType.CharacterExp2, totalExp2))
            {
                user.SubtractCurrency(CurrencyType.Gold, totalGold);
                user.SubtractCurrency(CurrencyType.CharacterExp, totalExp);
                user.SubtractCurrency(CurrencyType.CharacterExp2, totalExp2);
                deviceLevel = lv;
                repo.SaveSynchroDeviceState(user.ID, upgraded: true, deviceLevel);
                UserSessionStore.SaveUser(user);
            }
        }

        response.SynchroLv = deviceLevel;
        AppendUserCurrencies(user, response.Currencies);

        return response;
    }

    public ResSynchroOneClick SynchroOneClick(User user, ReqSynchroOneClick req)
    {
        ResSynchroOneClick response = new();
        Dictionary<int, CharacterLevelRecord> data = GameData.Instance.GetCharacterLevelUpData();
        int maxLv = data.Keys.Count > 0 ? data.Keys.Max() : 0;

        foreach (NetSynchroCharacter sc in req.Chars)
        {
            CharacterModel? character = user.Characters.FirstOrDefault(c => c.Csn == sc.Csn);
            if (character == null) continue;

            int targetLv = Math.Min(sc.Lv, maxLv);
            if (targetLv <= character.Level) continue;

            int requiredCredit = 0;
            int requiredBattleData = 0;
            int requiredCoreDust = 0;
            bool valid = true;
            for (int i = character.Level; i < targetLv; i++)
            {
                if (!data.TryGetValue(i, out var levelUpData))
                {
                    valid = false;
                    break;
                }
                requiredCredit += levelUpData.Gold;
                requiredBattleData += levelUpData.CharacterExp;
                requiredCoreDust += levelUpData.CharacterExp2;
            }

            if (!valid) continue;

            if (user.CanSubtractCurrency(CurrencyType.Gold, requiredCredit) &&
                user.CanSubtractCurrency(CurrencyType.CharacterExp, requiredBattleData) &&
                user.CanSubtractCurrency(CurrencyType.CharacterExp2, requiredCoreDust))
            {
                user.SubtractCurrency(CurrencyType.Gold, requiredCredit);
                user.SubtractCurrency(CurrencyType.CharacterExp, requiredBattleData);
                user.SubtractCurrency(CurrencyType.CharacterExp2, requiredCoreDust);
                character.Level = targetLv;
            }
            else
            {
                continue;
            }

            response.Characters.Add(ToNetCharacterDefault(character, character.Level));
        }

        response.SynchroLv = repo.GetSynchroLevel(user.ID, user.Characters);
        foreach (CharacterModel c in user.Characters.OrderByDescending(x => x.Level).Take(5))
            response.SynchroCharacters.Add(c.Csn);

        AppendUserCurrencies(user, response.Currencies);

        user.AddTrigger(Trigger.CharacterLevelUpCount, response.Characters.Count);
        UserSessionStore.SaveUser(user);

        return response;
    }

    private static NetUserCharacterDefaultData ToNetCharacterDefault(CharacterModel character, int level) => new()
    {
        Csn = character.Csn,
        Tid = character.Tid,
        Lv = level,
        Grade = character.Grade,
        CostumeId = character.CostumeId,
        UltiSkillLv = character.UltimateLevel,
        Skill1Lv = character.Skill1Lvl,
        Skill2Lv = character.Skill2Lvl
    };

    private static void AppendUserCurrencies(
        User user,
        Google.Protobuf.Collections.RepeatedField<NetUserCurrencyData> target)
    {
        foreach (KeyValuePair<CurrencyType, long> currency in user.Currency)
            target.Add(new NetUserCurrencyData { Type = (int)currency.Key, Value = currency.Value });
    }
}

public class BuildingCost
{
    public long Gold { get; set; }
    public int BuildTimeMinutes { get; set; } = 5;
}
