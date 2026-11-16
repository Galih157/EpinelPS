using System.Text.Json;
using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Simroom;
using EpinelPS.LobbyServer.Tower;
using EpinelPS.Models;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace EpinelPS.Services;

public sealed class ResetService(GameContext db, ResetRepository repo, ProgressRepository progressRepo)
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ResetService));

    // --- Interception ---

    public ResGetInterceptData GetInterceptData(ulong userId)
    {
        return new ResGetInterceptData
        {
            NormalInterceptGroup = 1,
            SpecialInterceptId = GetCurrentInterceptionId(),
            TicketCount = repo.GetInterceptionTickets(userId),
            MaxTicketCount = ServerConfigStore.MaxInterceptionCount
        };
    }

    public ResClearIntercept ClearIntercept(User user, ReqClearIntercept req)
    {
        if (repo.GetInterceptionTickets(user.ID) == 0)
            Logging.WriteLine("Attempted to clear interception when 0 tickets remain", LogType.WarningAntiCheat);

        InterceptionClearResult sRes = InterceptionHelper.Clear(user, req.Intercept, req.InterceptId, req.Damage);
        int remainingTickets = repo.DecrementInterceptionTickets(user.ID);
        db.SaveChanges();

        user.AddTrigger(Trigger.InterceptClear, 1);
        UserSessionStore.SaveUser(user);

        return new ResClearIntercept
        {
            Intercept = req.Intercept,
            InterceptId = req.InterceptId,
            TicketCount = remainingTickets,
            MaxTicketCount = ServerConfigStore.MaxInterceptionCount,
            NormalReward = sRes.NormalReward,
            BonusReward = sRes.BonusReward
        };
    }

    public ResEnterIntercept EnterIntercept(User user)
    {
        user.AddTrigger(Trigger.InterceptStart, 1);
        return new ResEnterIntercept();
    }

    public ResFastClearIntercept FastClearIntercept(ulong userId)
    {
        return new ResFastClearIntercept
        {
            TicketCount = repo.GetInterceptionTickets(userId),
            MaxTicketCount = ServerConfigStore.MaxInterceptionCount,
            Damage = 0
        };
    }

    private static int GetCurrentInterceptionId()
    {
        var specialBosses = GameData.Instance.InterceptSpecial.Values
            .Where(x => x.Group == 1)
            .OrderBy(x => x.Order)
            .ToList();

        int specialIndex = DateTime.UtcNow.DayOfYear % specialBosses.Count;
        return specialBosses[specialIndex].Id;
    }

    // --- Missions (Daily & Weekly) ---

    public ResObtainDailyMissionReward ObtainDailyMissionReward(User user, ReqObtainDailyMissionReward req)
    {
        var completed = repo.GetCompletedDailyMissions(user.ID).ToHashSet();
        List<NetRewardData> rewards = [];
        int totalPoints = 0;

        foreach (int item in req.TidList)
        {
            if (!completed.Add(item))
            {
                Logging.WriteLine("already completed daily mission", LogType.Warning);
                continue;
            }

            if (!GameData.Instance.TriggerTable.TryGetValue(item, out TriggerRecord? key))
                throw new Exception("unknown TID");

            repo.AddCompletedDailyMission(user.ID, item);

            if (key.RewardId != 0)
            {
                RewardRecord rewardRecord = GameData.Instance.GetRewardTableEntry(key.RewardId)
                    ?? throw new Exception("unable to lookup reward");
                rewards.Add(RewardUtils.RegisterRewardsForUser(user, rewardRecord));
            }
            else
            {
                totalPoints += key.PointValue;
            }
        }

        user.AddTrigger(Trigger.PointRewardDaily, totalPoints);
        repo.AddDailyMissionPoints(user.ID, totalPoints);
        db.SaveChanges();

        var response = new ResObtainDailyMissionReward
        {
            Reward = NetUtils.MergeRewards(rewards, user),
            EventBonusReward = new NetRewardData { PassPoint = new NetPassPointData() }
        };
        response.Reward.PassPoint = new NetPassPointData();

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResObtainWeeklyMissionReward ObtainWeeklyMissionReward(User user, ReqObtainWeeklyMissionReward req)
    {
        var completed = repo.GetCompletedWeeklyMissions(user.ID).ToHashSet();
        List<NetRewardData> rewards = [];
        int totalPoints = 0;

        foreach (int item in req.TidList)
        {
            if (!completed.Add(item))
                continue;

            if (!GameData.Instance.TriggerTable.TryGetValue(item, out TriggerRecord? key))
                throw new Exception("unknown TID");

            repo.AddCompletedWeeklyMission(user.ID, item);

            if (key.RewardId != 0)
            {
                RewardRecord rewardRecord = GameData.Instance.GetRewardTableEntry(key.RewardId)
                    ?? throw new Exception("unable to lookup reward");
                rewards.Add(RewardUtils.RegisterRewardsForUser(user, rewardRecord));
            }
            else
            {
                totalPoints += key.PointValue;
            }
        }

        user.AddTrigger(Trigger.PointRewardWeekly, totalPoints);
        repo.AddWeeklyMissionPoints(user.ID, totalPoints);
        db.SaveChanges();

        var response = new ResObtainWeeklyMissionReward
        {
            Reward = NetUtils.MergeRewards(rewards, user),
            EventBonusReward = new NetRewardData { PassPoint = new NetPassPointData() }
        };
        response.Reward.PassPoint = new NetPassPointData();

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResGetDailyRewardedData GetDailyRewards(User user)
    {
        user.ResetDataIfNeeded();
        var response = new ResGetDailyRewardedData();
        response.Ids.Add(repo.GetCompletedDailyMissions(user.ID));
        return response;
    }

    public ResGetWeeklyRewardedData GetWeeklyRewards(User user)
    {
        user.ResetDataIfNeeded();
        var response = new ResGetWeeklyRewardedData();
        response.Ids.Add(repo.GetCompletedWeeklyMissions(user.ID));
        return response;
    }

    // --- Tower ---

    public ResClearTower ClearTower(User user, ReqClearTower req)
    {
        var response = new ResClearTower();
        if (req.BattleResult == 1)
        {
            response.Reward = TowerHelper.CompleteTower(user, req.TowerId);
            UserSessionStore.SaveUser(user);
        }
        return response;
    }

    public ResEnterTower EnterTower(User user)
    {
        user.AddTrigger(Trigger.TowerAllStart, 1);
        return new ResEnterTower();
    }

    public ResGetTowerData GetTowerData(User user)
    {
        var response = new ResGetTowerData();
        Dictionary<CorporationTowerType, int> towers = repo.GetDailyTowerCounts(user.ID);

        var towerSchedules = new Dictionary<CorporationTowerType, NetSchedule>
        {
            [CorporationTowerType.ELYSION] = new() { DayOfWeek = new() { DayOfWeeks = { 1, 4, 6 }, StartTime = 720000000000, Duration = 863990000000 } },
            [CorporationTowerType.MISSILIS] = new() { DayOfWeek = new() { DayOfWeeks = { 2, 5, 6 }, StartTime = 720000000000, Duration = 863990000000 } },
            [CorporationTowerType.TETRA] = new() { DayOfWeek = new() { DayOfWeeks = { 0, 3, 6 }, StartTime = 720000000000, Duration = 863990000000 } },
            [CorporationTowerType.OVERSPEC] = new() { DayOfWeek = new() { DayOfWeeks = { 2, 6 }, StartTime = 720000000000, Duration = 863990000000 } },
            [CorporationTowerType.ALL] = new() { AllTime = new() }
        };

        List<NetTowerData> towerData = [];
        foreach (var towerType in System.Enum.GetValues<CorporationTowerType>())
        {
            towers.TryGetValue(towerType, out int count);

            towerData.Add(towerType == CorporationTowerType.ALL
                ? new NetTowerData { Type = (int)towerType }
                : new NetTowerData { Type = (int)towerType, RemainCount = 3 - count });

            towerData.Last().Schedules.Add(towerSchedules[towerType]);
        }

        var towerProgressMap = progressRepo.GetTowerProgress(user.ID);
        if (towerProgressMap.Remove(0))
        {
            var invalidRows = db.Set<UserTowerProgressEntity>()
                .Where(x => x.UserId == user.ID && x.CorporationType == 0)
                .ToList();
            if (invalidRows.Count > 0)
            {
                db.Set<UserTowerProgressEntity>().RemoveRange(invalidRows);
                db.SaveChanges();
            }
        }

        foreach (var towerProgress in towerProgressMap)
            towerData.FirstOrDefault(x => x.Type == (int)towerProgress.Key)?.Floor = towerProgress.Value;

        Console.WriteLine(JsonSerializer.Serialize(towerData));
        response.Data.AddRange(towerData);
        return response;
    }

    // --- Simulation Room ---

    public ResClearSimRoomBattle ClearSimRoomBattle(User user, ReqClearSimRoomBattle req)
    {
        var response = new ResClearSimRoomBattle { Result = SimRoomResult.Success };

        try
        {
            var team = SimRoomHelper.GetTeamData(user, req.TeamNumber, [.. req.RemainingHps]);
            if (team is not null)
                response.Teams.Add(team);
        }
        catch (Exception e)
        {
            Log.Error($"ClearBattle Response Team Exception :{e.Message}");
        }

        SimRoomHelper.UpdateUserRemainingHps(user, [.. req.RemainingHps], req.TeamNumber);

        if (req.BattleResult == 1)
        {
            try
            {
                var buffOptions = SimRoomHelper.GetBuffOptions(user, req.Location);
                if (buffOptions is not null && buffOptions.Count > 0)
                    response.BuffOptions.AddRange(buffOptions);
            }
            catch (Exception e)
            {
                Log.Error($"ClearBattle Response BuffOptions Exception :{e.Message}");
            }
        }

        return response;
    }

    public ResFastClearSimRoomBattle FastClearSimRoomBattle(User user, ReqFastClearSimRoomBattle req)
    {
        var response = new ResFastClearSimRoomBattle { Result = SimRoomResult.Success };

        try
        {
            var team = SimRoomHelper.GetTeamData(user, req.TeamNumber, null);
            if (team is not null)
                response.Teams.Add(team);
        }
        catch (Exception e)
        {
            Log.Error($"ClearBattleFast Response Team Exception :{e.Message}");
        }

        SimRoomHelper.UpdateUserRemainingHps(user, teamNumber: req.TeamNumber);

        try
        {
            var buffOptions = SimRoomHelper.GetBuffOptions(user, req.Location);
            if (buffOptions is not null && buffOptions.Count > 0)
                response.BuffOptions.AddRange(buffOptions);
        }
        catch (Exception e)
        {
            Log.Error($"ClearBattleFast Response BuffOptions Exception: {e.Message}");
        }

        return response;
    }

    public ResGetSimRoomAcquireBuffFunction GetAcquireBuffFunction(User user, ReqGetSimRoomAcquireBuffFunction req)
    {
        var response = new ResGetSimRoomAcquireBuffFunction
        {
            Result = SimRoomResult.Success,
            RandomBuff = 2020406
        };

        var location = req.Location;
        var events = repo.GetSimRoomData(user.ID).Events;
        int simRoomEventIndex = events.FindIndex(x =>
            x.Location.Chapter == location.Chapter &&
            x.Location.Stage == location.Stage &&
            x.Location.Order == location.Order);

        if (simRoomEventIndex < 0)
        {
            Logging.Warn("Not Fond UserSimRoomEvent");
            return response;
        }

        SimRoomHelper.UpdateUserSimRoomEvent(
            user,
            index: simRoomEventIndex,
            events: events,
            selectionNumber: req.SelectionNumber,
            isDone: true);

        return response;
    }

    public ResGetSimRoom GetSimRoom(User user)
    {
        var simRoomData = repo.GetSimRoomData(user.ID);
        int currentDifficulty = simRoomData.CurrentDifficulty;
        int currentChapter = simRoomData.CurrentChapter;
        var buffs = simRoomData.Buffs;
        var legacyBuffs = simRoomData.LegacyBuffs;

        var response = new ResGetSimRoom
        {
            Status = SimRoomStatus.Ready,
            CurrentDifficulty = currentDifficulty,
            NextRenewAt = DateTimeHelper.GetNextDayAtTime("China Standard Time", 2).Ticks,
            NextLegacyBuffResetDate = DateTimeHelper.GetNextWeekdayAtTime("China Standard Time", DayOfWeek.Tuesday, 2).ToTimestamp(),
            IsSimpleModeSkipEnabled = simRoomData.IsSimpleModeSkipEnabled,
            LastPlayedChapter = new NetSimRoomChapterInfo
            {
                Chapter = currentChapter,
                Difficulty = currentDifficulty
            }
        };

        response.LegacyBuffs.AddRange(legacyBuffs);

        try
        {
            response.OverclockData = BuildOverclockData(simRoomData);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Get OverclockData Exception: {ex.Message}", LogType.Error);
        }

        response.ClearInfos.AddRange(BuildClearInfos(simRoomData));

        if (simRoomData.CurrentSeasonData.IsOverclock)
            response.OverclockOptionList.AddRange(simRoomData.CurrentSeasonData.CurrentOptionList);

        if (simRoomData.Entered)
        {
            response.Status = SimRoomStatus.Progress;
            response.Events.AddRange(SimRoomHelper.GetSimRoomEvents(user));
            response.RemainingHps.AddRange(
                simRoomData.RemainingHps.Select(hp => new NetSimRoomCharacterHp { Csn = hp.Csn, Hp = hp.Hp }));
            response.Buffs.AddRange(buffs);
            response.Buffs.AddRange(legacyBuffs);
        }

        return response;
    }

    public ResInfinitePopupCheck InfinitePopupCheck(ulong userId)
    {
        var simRoomData = repo.GetSimRoomData(userId);
        simRoomData.CurrentSeasonData.WasInfinitePopupChecked = true;
        repo.SaveSimRoomData(userId, simRoomData);
        db.SaveChanges();
        return new ResInfinitePopupCheck();
    }

    public ResProceedSimRoomBuffFunction ProceedBuffFunction(User user, ReqProceedSimRoomBuffFunction req)
    {
        var response = new ResProceedSimRoomBuffFunction { Result = SimRoomResult.Success };
        if (req.BuffToDelete > 0)
            response.DeletedBuff.Add(req.BuffToDelete);

        var location = req.Location;
        var events = repo.GetSimRoomData(user.ID).Events;
        int simRoomEventIndex = events.FindIndex(x =>
            x.Location.Chapter == location.Chapter &&
            x.Location.Stage == location.Stage &&
            x.Location.Order == location.Order);

        if (simRoomEventIndex < 0)
        {
            Logging.Warn("Not Fond UserSimRoomEvent");
            return response;
        }

        SimRoomHelper.UpdateUserSimRoomEvent(
            user,
            index: simRoomEventIndex,
            events: events,
            selectionNumber: req.SelectionNumber,
            isDone: true);

        return response;
    }

    public ResProceedSimRoomNikkeFunction ProceedNikkeFunction(User user, ReqProceedSimRoomNikkeFunction req)
    {
        var response = new ResProceedSimRoomNikkeFunction { Result = SimRoomResult.Success };
        var location = req.Location;
        var simRoomData = repo.GetSimRoomData(user.ID);
        var events = simRoomData.Events;
        int simRoomEventIndex = events.FindIndex(x =>
            x.Location.Chapter == location.Chapter &&
            x.Location.Stage == location.Stage &&
            x.Location.Order == location.Order);

        if (simRoomEventIndex < 0)
        {
            Logging.Warn("Not Fond UserSimRoomEvent");
            return response;
        }

        try
        {
            if (GameData.Instance.SimulationRoomSelectionGroupTable.TryGetValue(req.SelectionGroupElementId, out var selectionGroup))
            {
                if (selectionGroup.EventFunctionType is SimulationRoomEventFunctionType.Heal or SimulationRoomEventFunctionType.Resurrection)
                {
                    if (selectionGroup.EventFunctionTargetType == SimulationRoomEventfunctionTargetType.All)
                    {
                        var changedRemainingHps = UpdateRemainingHps(
                            simRoomData,
                            selectionGroup.EventFunctionValue,
                            selectionGroup.EventFunctionType);
                        repo.SaveSimRoomData(user.ID, simRoomData);
                        db.SaveChanges();

                        response.ChangedHps.AddRange(
                            changedRemainingHps.Select(x => new NetSimRoomCharacterHp { Csn = x.Csn, Hp = x.Hp }));

                        SimRoomHelper.UpdateUserSimRoomEvent(
                            user,
                            index: simRoomEventIndex,
                            events: events,
                            selectionNumber: req.SelectionNumber,
                            isDone: true);
                    }
                    else
                    {
                        Logging.Warn($"Not implement EventFunctionTargetType: {selectionGroup.EventFunctionTargetType}");
                    }
                }
                else
                {
                    Logging.Warn($"Not implement EventFunctionType: {selectionGroup.EventFunctionType}");
                }
            }
            else
            {
                Logging.Warn("Not Fond SimulationRoomSelectionGroup");
            }
        }
        catch (Exception e)
        {
            Logging.Warn($"ProceedNikkeFunction ChangedHps Exception {e.Message}");
        }

        var team = SimRoomHelper.GetTeamData(user, 1, null);
        if (team is not null)
            response.Teams.Add(team);

        return response;
    }

    public ResProceedSimRoomSkipFunction ProceedSkipFunction(User user, ReqProceedSimRoomSkipFunction req)
    {
        var response = new ResProceedSimRoomSkipFunction { Result = SimRoomResult.Success };
        var location = req.Location;
        var events = repo.GetSimRoomData(user.ID).Events;
        int simRoomEventIndex = events.FindIndex(x =>
            x.Location.Chapter == location.Chapter &&
            x.Location.Stage == location.Stage &&
            x.Location.Order == location.Order);

        if (simRoomEventIndex < 0)
        {
            Logging.Warn("Not Fond UserSimRoomEvent");
            return response;
        }

        SimRoomHelper.UpdateUserSimRoomEvent(
            user,
            index: simRoomEventIndex,
            events: events,
            selectionNumber: req.SelectionNumber,
            isDone: true);

        return response;
    }

    public ResQuitSimRoom QuitSimRoom(ulong userId, ReqQuitSimRoom req)
    {
        var simRoomData = repo.GetSimRoomData(userId);

        try
        {
            foreach (var item in req.BuffsToAdd)
            {
                if (!simRoomData.LegacyBuffs.Contains(item))
                    simRoomData.LegacyBuffs.Add(item);
            }
        }
        catch (Exception e)
        {
            Logging.Warn($"QuitSimRoom BuffsToAdd Exception {e.Message}");
        }

        try
        {
            foreach (var item in req.BuffsToDelete)
                simRoomData.LegacyBuffs.Remove(item);
        }
        catch (Exception e)
        {
            Logging.Warn($"QuitSimRoom BuffsToDelete Exception {e.Message}");
        }

        simRoomData.Entered = false;
        simRoomData.Events = [];
        simRoomData.RemainingHps = [];
        simRoomData.Buffs = [];
        simRoomData.CurrentSeasonData.IsOverclock = false;

        repo.SaveSimRoomData(userId, simRoomData);
        db.SaveChanges();

        return new ResQuitSimRoom { Result = SimRoomResult.Success };
    }

    public ResSelectSimRoomBuff SelectSimRoomBuff(User user, ReqSelectSimRoomBuff req)
    {
        var response = new ResSelectSimRoomBuff { Result = SimRoomResult.Success };
        var simRoomData = repo.GetSimRoomData(user.ID);

        try
        {
            var buffs = simRoomData.Buffs;
            if (req.BuffToDelete > 0)
                buffs.Remove(req.BuffToDelete);

            if (req.BuffToAdd > 0 && !buffs.Contains(req.BuffToAdd))
                buffs.Add(req.BuffToAdd);

            simRoomData.Buffs = buffs;
        }
        catch (Exception e)
        {
            Logging.Warn($"Update User SimRoomData Buffs Exception: {e.Message}");
        }

        repo.SaveSimRoomData(user.ID, simRoomData);
        db.SaveChanges();

        var location = req.Location;
        if (location is not null)
        {
            var events = simRoomData.Events;
            int simRoomEventIndex = events.FindIndex(x =>
                x.Location.Chapter == location.Chapter &&
                x.Location.Stage == location.Stage &&
                x.Location.Order == location.Order);

            if (simRoomEventIndex > -1)
            {
                SimRoomHelper.UpdateUserSimRoomEvent(
                    user,
                    simRoomEventIndex,
                    events,
                    battleProgress: (int)SimRoomBattleEventProgress.RewardReceived);

                var sorted = events.OrderBy(x => x.Location.Stage).ThenBy(x => x.Location.Order).ToList();
                var last = sorted[^1];
                if (last.Location.Chapter == location.Chapter &&
                    last.Location.Stage == location.Stage &&
                    last.Location.Order == location.Order)
                {
                    var difficulty = simRoomData.CurrentDifficulty;
                    var reward = SimRoomHelper.SimRoomReceivedReward(user, difficulty, location.Chapter);
                    if (reward is not null)
                        response.Reward = reward;

                    var overclockReward = SimRoomHelper.SimRoomOverclockReceivedReward(user);
                    if (overclockReward is not null)
                        response.RewardByOverclock = overclockReward;

                    SimRoomHelper.UpdateOverclockHighScoreData(user, req.Location);
                    UserSessionStore.SaveUser(user);
                }
            }
        }

        return response;
    }

    public ResSelectSimRoomDifficulty SelectDifficulty(User user, ReqSelectSimRoomDifficulty req)
    {
        var response = new ResSelectSimRoomDifficulty { Result = SimRoomResult.Success };
        var simRoomData = repo.GetSimRoomData(user.ID);

        simRoomData.Entered = true;
        simRoomData.CurrentDifficulty = req.Difficulty;
        simRoomData.CurrentChapter = req.StartingChapter;

        bool isOverclock = req.OverclockOptionList is not null &&
                           req.OverclockOptionList.Count > 0 &&
                           req.OverclockSeason > 0 &&
                           req.OverclockSubSeason > 0;

        var currentSeasonData = simRoomData.CurrentSeasonData;
        currentSeasonData.IsOverclock = isOverclock;
        if (isOverclock)
        {
            currentSeasonData.CurrentSeason = req.OverclockSeason;
            currentSeasonData.CurrentSubSeason = req.OverclockSubSeason;
            currentSeasonData.CurrentOptionList = [.. req.OverclockOptionList!];
        }
        simRoomData.CurrentSeasonData = currentSeasonData;

        repo.SaveSimRoomData(user.ID, simRoomData);
        db.SaveChanges();

        List<NetSimRoomEvent> events = SimRoomHelper.GetSimRoomEvents(
            user,
            req.Difficulty,
            req.StartingChapter,
            [.. req.OverclockOptionList!],
            req.OverclockSeason,
            req.OverclockSubSeason);

        response.Events.AddRange(events);
        return response;
    }

    public ResSelectSimRoomSelectionEvent SelectSelectionEvent(User user, ReqSelectSimRoomSelectionEvent req)
    {
        var response = new ResSelectSimRoomSelectionEvent { Result = SimRoomResult.Success };
        var location = req.Location;
        var events = repo.GetSimRoomData(user.ID).Events;
        int simRoomEventIndex = events.FindIndex(x =>
            x.Location.Chapter == location.Chapter &&
            x.Location.Stage == location.Stage &&
            x.Location.Order == location.Order);

        if (simRoomEventIndex < 0)
        {
            Logging.Warn("Not Fond UserSimRoomEvent");
            return response;
        }

        SimRoomHelper.UpdateUserSimRoomEvent(
            user,
            index: simRoomEventIndex,
            events: events,
            selectionNumber: req.SelectionNumber);

        return response;
    }

    public ResSelectSimRoomSimpleModeBuff SimpleModeSelectBuff(ulong userId, ReqSelectSimRoomSimpleModeBuff req)
    {
        var simRoomData = repo.GetSimRoomData(userId);
        List<int> buffs = simRoomData.Buffs;
        List<int> legacyBuffs = simRoomData.LegacyBuffs;

        if (req.BuffToDelete > 0)
        {
            buffs.Remove(req.BuffToDelete);
            legacyBuffs.Remove(req.BuffToDelete);
        }

        if (req.BuffToAdd > 0)
        {
            if (!buffs.Contains(req.BuffToAdd))
                buffs.Add(req.BuffToAdd);
            if (!legacyBuffs.Contains(req.BuffToAdd))
                legacyBuffs.Add(req.BuffToAdd);
        }

        simRoomData.Entered = false;
        repo.SaveSimRoomData(userId, simRoomData);
        db.SaveChanges();

        return new ResSelectSimRoomSimpleModeBuff
        {
            Result = SimRoomResult.Reset,
            NextSimpleModeBuffSelectionInfo = new NetSimRoomSimpleModeBuffSelectionInfo
            {
                RemainingBuffSelectCount = 8 - buffs.Count,
                BuffOptions = { buffs }
            }
        };
    }

    public ResSetSimRoomSimpleModeSkipOption SimpleModeSetSkipOption(ulong userId, ReqSetSimRoomSimpleModeSkipOption req)
    {
        var simRoomData = repo.GetSimRoomData(userId);
        simRoomData.IsSimpleModeSkipEnabled = req.Enabled;
        repo.SaveSimRoomData(userId, simRoomData);
        db.SaveChanges();
        return new ResSetSimRoomSimpleModeSkipOption();
    }

    public ResSkipAllSimRoomSimpleMode SimpleModeSkipAll(User user)
    {
        var response = new ResSkipAllSimRoomSimpleMode { Result = SimRoomResult.Success };

        try
        {
            var simRoomData = repo.GetSimRoomData(user.ID);
            simRoomData.CurrentDifficulty = 5;
            simRoomData.CurrentChapter = 3;
            repo.SaveSimRoomData(user.ID, simRoomData);
            db.SaveChanges();

            var reward = SimRoomHelper.SimRoomReceivedReward(user, 5, 3);
            if (reward is not null)
                response.Reward = reward;

            UserSessionStore.SaveUser(user);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"SkipAllSimpleMode Reward Exception: {ex.Message}", LogType.Error);
        }

        return response;
    }

    public ResSkipSimRoomSimpleModeBuffSelection SimpleModeSkipBuffSelection(ulong userId)
    {
        var simRoomData = repo.GetSimRoomData(userId);
        simRoomData.Entered = false;
        repo.SaveSimRoomData(userId, simRoomData);
        db.SaveChanges();
        return new ResSkipSimRoomSimpleModeBuffSelection { Result = SimRoomResult.Reset };
    }

    public ResStartSimRoomSimpleMode SimpleModeStart(User user)
    {
        var response = new ResStartSimRoomSimpleMode { Result = SimRoomResult.Success };

        try
        {
            var simRoomData = repo.GetSimRoomData(user.ID);
            simRoomData.Entered = true;
            repo.SaveSimRoomData(user.ID, simRoomData);
            db.SaveChanges();

            var simRoomEvents = SimRoomHelper.GetSimRoomEvents(user, 5, 3);
            if (simRoomEvents is not null)
            {
                foreach (var simRoomEvent in simRoomEvents)
                {
                    if (simRoomEvent.EventCase == NetSimRoomEvent.EventOneofCase.Battle &&
                        simRoomEvent.Location.Order == 1 &&
                        simRoomEvent.Location is not null)
                    {
                        SimRoomHelper.GetBuffOptions(user, simRoomEvent.Location);
                    }
                }

                var updatedEvents = repo.GetSimRoomData(user.ID).Events;
                if (updatedEvents is not null)
                    simRoomEvents = [.. updatedEvents.Select(SimRoomHelper.MToNet)];

                response.Events.AddRange(simRoomEvents);
            }
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"SimpleModeStart Events Exception: {ex.Message}", LogType.Error);
        }

        return response;
    }

    // --- SimRoom Private Helpers ---

    private static List<NetSimRoomChapterInfo> BuildClearInfos(SimRoomData simRoomData)
    {
        List<NetSimRoomChapterInfo> clearInfos = [];
        try
        {
            var receivedRewards = simRoomData.ReceivedRewardChapters;
            if (receivedRewards.Count > 0)
            {
                var lastReceivedReward = receivedRewards.OrderBy(x => x.Difficulty).ThenBy(x => x.Chapter).LastOrDefault();
                if (lastReceivedReward is not null)
                {
                    int currentDifficulty = lastReceivedReward.Difficulty;
                    int currentChapter = lastReceivedReward.Chapter;

                    var chapterRecords = GameData.Instance.SimulationRoomChapterTable.Values
                        .Where(x => x.DifficultyId <= currentDifficulty)
                        .ToList();

                    foreach (var chapterRecord in chapterRecords)
                    {
                        if ((chapterRecord.DifficultyId == currentDifficulty && chapterRecord.Chapter <= currentChapter) ||
                            chapterRecord.DifficultyId < currentDifficulty)
                        {
                            clearInfos.Add(new NetSimRoomChapterInfo
                            {
                                Chapter = chapterRecord.Chapter,
                                Difficulty = chapterRecord.DifficultyId
                            });
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Logging.WriteLine($"Get ClearInfos Exception: {e.Message}", LogType.Error);
        }

        return clearInfos;
    }

    private static NetSimRoomOverclockData BuildOverclockData(SimRoomData simRoomData)
    {
        var currentSeasonData = simRoomData.CurrentSeasonData;

        var netOverclockData = new NetSimRoomOverclockData
        {
            HasClearedLevel50 = currentSeasonData.HasClearedLevel50,
            WasInfinitePopupChecked = currentSeasonData.WasInfinitePopupChecked,
            WasMainSeasonResetPopupChecked = currentSeasonData.WasMainSeasonResetPopupChecked,
            WasSubSeasonResetPopupChecked = currentSeasonData.WasSubSeasonResetPopupChecked,
            CurrentSeasonData = new NetSimRoomOverclockSeasonData
            {
                SeasonStartDate = DateTime.UtcNow.Date.AddDays(-2).ToTimestamp(),
                SeasonEndDate = DateTime.UtcNow.Date.AddDays(12).ToTimestamp(),
                IsSeasonOpen = true,
                Season = GameData.Instance.SimulationRoomOcSeasonTable.Keys.Max(),
                SubSeason = 3,
                SeasonWeekCount = 2
            },
            CurrentSeasonHighScore = new NetSimRoomOverclockHighScoreData
            {
                CreatedAt = currentSeasonData.CurrentSeasonHighScore.CreatedAt ?? DateTime.UtcNow.Date.AddDays(-2).ToTimestamp(),
                Season = currentSeasonData.CurrentSeasonHighScore.Season,
                SubSeason = currentSeasonData.CurrentSeasonHighScore.SubSeason,
                OptionList = { currentSeasonData.CurrentSeasonHighScore.OptionList },
                OptionLevel = currentSeasonData.CurrentSeasonHighScore.OptionLevel
            },
            CurrentSubSeasonHighScore = new NetSimRoomOverclockHighScoreData
            {
                CreatedAt = currentSeasonData.CurrentSubSeasonHighScore.CreatedAt ?? DateTime.UtcNow.Date.AddDays(-2).ToTimestamp(),
                Season = currentSeasonData.CurrentSubSeasonHighScore.Season,
                SubSeason = currentSeasonData.CurrentSubSeasonHighScore.SubSeason,
                OptionList = { currentSeasonData.CurrentSubSeasonHighScore.OptionList },
                OptionLevel = currentSeasonData.CurrentSubSeasonHighScore.OptionLevel
            },
            LatestOption = new NetSimRoomOverclockOptionSettingData
            {
                Season = currentSeasonData.LatestOption.Season,
                OptionList = { currentSeasonData.LatestOption.OptionList }
            }
        };

        return netOverclockData;
    }

    private static List<SimRoomCharacterHp> UpdateRemainingHps(
        SimRoomData simRoomData,
        int hpValue,
        SimulationRoomEventFunctionType type)
    {
        var remainingHps = simRoomData.RemainingHps;
        if (remainingHps is not null && remainingHps.Count > 0)
        {
            for (int i = 0; i < remainingHps.Count; i++)
            {
                if ((type is SimulationRoomEventFunctionType.Heal && remainingHps[i].Hp >= 0) ||
                    (type is SimulationRoomEventFunctionType.Resurrection && remainingHps[i].Hp < 0))
                {
                    remainingHps[i] = new SimRoomCharacterHp { Csn = remainingHps[i].Csn, Hp = hpValue };
                }
            }
            simRoomData.RemainingHps = remainingHps;
        }
        return remainingHps ?? [];
    }
}
