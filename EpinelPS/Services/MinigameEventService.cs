using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer;
using EpinelPS.LobbyServer.Event.Minigame.AZX;
using EpinelPS.LobbyServer.Event.Minigame.ScenarioChoice;
using EpinelPS.LobbyServer.Minigame;
using EpinelPS.LobbyServer.Minigame.BubbleMarch;
using EpinelPS.Repositories;
using EpinelPS.Utils;

namespace EpinelPS.Services;

public sealed class MinigameEventService(GameContext db, MinigameRepository repo)
{
    // --- AZX ---

    public ResAcquireMiniGameAzxAchievementMissionReward AcquireAzxAchievementMissionReward(
        User user,
        ReqAcquireMiniGameAzxAchievementMissionReward req)
    {
        var reward = new NetRewardData();
        AzxHelper.AcquireReward(user, repo, db, ref reward, req.AzxId, req.AchievementMissionIdList);
        return new ResAcquireMiniGameAzxAchievementMissionReward { Reward = reward };
    }

    public ResEnterMiniGameAzx EnterAzx(ulong userId, ReqEnterMiniGameAzx req)
    {
        var response = new ResEnterMiniGameAzx { PreviousSRankCount = 0 };
        if (req.AzxId > 0 && req.PlayBoardId > 0 && req.PlayCharacterId > 0)
            AzxHelper.EnterAzx(userId, repo, db, ref response, req.AzxId, req.PlayBoardId, req.PlayCharacterId);
        return response;
    }

    public ResFinishMiniGameAzx FinishAzx(User user, ReqFinishMiniGameAzx req)
    {
        var response = new ResFinishMiniGameAzx();
        AzxHelper.FinishAzx(user, repo, db, req, ref response);
        return response;
    }

    public ResGetMiniGameAzxData GetAzxData(ulong userId, int azxId)
    {
        var response = new ResGetMiniGameAzxData
        {
            DailyMissionData = new NetMiniGameAzxDailyMissionData
            {
                DailyAccumulatedScore = 0,
                IsDailyRewarded = false
            }
        };
        AzxHelper.GetAzxData(userId, azxId, repo, ref response);
        return response;
    }

    public ResGetMiniGameAzxRanking GetAzxRanking(ulong userId, int azxId)
    {
        var response = new ResGetMiniGameAzxRanking();
        if (azxId > 0)
            AzxHelper.GetRanking(userId, azxId, repo, ref response);
        return response;
    }

    public ResSetMiniGameAzxBoardUnlocked SetAzxBoardUnlocked(ulong userId, ReqSetMiniGameAzxBoardUnlocked req)
    {
        if (req.BoardId > 0 && req.AzxId > 0)
            AzxHelper.SetBoardUnlocked(userId, req.AzxId, req.BoardId, repo, db);
        return new ResSetMiniGameAzxBoardUnlocked();
    }

    public ResSetMiniGameAzxCharacterUnlocked SetAzxCharacterUnlocked(
        ulong userId,
        ReqSetMiniGameAzxCharacterUnlocked req)
    {
        if (req.CharacterId > 0 && req.AzxId > 0)
            AzxHelper.SetCharacterUnlocked(userId, req.AzxId, req.CharacterId, repo, db);
        return new ResSetMiniGameAzxCharacterUnlocked();
    }

    public ResSetMiniGameAzxCutSceneConfirmed SetAzxCutSceneConfirmed(
        ulong userId,
        ReqSetMiniGameAzxCutSceneConfirmed req)
    {
        if (req.AzxId > 0 && req.ConfirmedCutSceneIdList.Count > 0)
            AzxHelper.SetCutSceneConfirmed(userId, req.AzxId, [.. req.ConfirmedCutSceneIdList], repo, db);
        return new ResSetMiniGameAzxCutSceneConfirmed();
    }

    public ResSetMiniGameAzxSkillUnlocked SetAzxSkillUnlocked(ulong userId, ReqSetMiniGameAzxSkillUnlocked req)
    {
        if (req.SkillId > 0 && req.AzxId > 0)
            AzxHelper.SetSkillUnlocked(userId, req.AzxId, req.SkillId, repo, db);
        return new ResSetMiniGameAzxSkillUnlocked();
    }

    public ResSetMiniGameAzxTutorialConfirmed SetAzxTutorialConfirmed(ulong userId)
    {
        AzxHelper.SetTutorialConfirmed(userId, 1, repo, db);
        return new ResSetMiniGameAzxTutorialConfirmed();
    }

    // --- IsLandBreaker ---

    public ResResetMiniGameIslandBreakerBuff ResetIslandBreakerBuff(ulong userId, int islandBreakerId)
    {
        var response = new ResResetMiniGameIslandBreakerBuff();

        if (repo.TryGetDictEntry<IsLandBreakerData>(
                userId, MinigameRepository.TypeIsLandBreaker, islandBreakerId, out var isLandData)
            && isLandData != null)
        {
            var bufflist = GameData.Instance.EventIslandBreakerBuffTable.Values
                .Where(x => isLandData.Buffs.Contains(x.Id))
                .ToList();
            int totalCost = bufflist.Sum(x => x.BuffCurrencyCount);

            isLandData.Buffs.Clear();
            isLandData.Buffs.AddRange(bufflist.Where(x => x.BuffLevel == 0).Select(x => x.Id));

            if (isLandData.Currencies.TryGetValue(2, out var buffCurrency))
            {
                buffCurrency.CurrentAmount = Math.Min(
                    buffCurrency.CurrentAmount + totalCost,
                    buffCurrency.MaxLimit);
                response.NewBuffCurrencyAmount = buffCurrency.CurrentAmount;
            }

            repo.SaveDictEntry(userId, MinigameRepository.TypeIsLandBreaker, islandBreakerId, isLandData);
            db.SaveChanges();
        }

        return response;
    }

    public ResUpgradeMiniGameIslandBreakerBuff UpgradeIslandBreakerBuff(
        ulong userId,
        ReqUpgradeMiniGameIslandBreakerBuff req)
    {
        var response = new ResUpgradeMiniGameIslandBreakerBuff();

        if (repo.TryGetDictEntry<IsLandBreakerData>(
                userId, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, out var isLandData)
            && isLandData != null)
        {
            isLandData.Buffs.AddUnique(req.UpgradeBuffId);

            var buff = GameData.Instance.EventIslandBreakerBuffTable.Values
                .FirstOrDefault(x => x.Id == req.UpgradeBuffId);
            if (buff != null && isLandData.Currencies.TryGetValue(2, out var buffCurrency))
            {
                buffCurrency.CurrentAmount -= buff.BuffCurrencyCount;
                response.NewBuffCurrencyAmount = buffCurrency.CurrentAmount;
            }

            response.NewBuffId = req.UpgradeBuffId;

            repo.SaveDictEntry(userId, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, isLandData);
            db.SaveChanges();
        }

        return response;
    }

    public ResSaveMiniGameIslandBreakerLastSelectedCharacter SaveIslandBreakerCharacter(
        ulong userId,
        ReqSaveMiniGameIslandBreakerLastSelectedCharacter req)
    {
        if (repo.TryGetDictEntry<IsLandBreakerData>(
                userId, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, out var isLandData)
            && isLandData != null)
        {
            isLandData.CharacterStatistics.TryAdd(req.CharacterId, new MiniGameIslandBreakerCharacterStatistics
            {
                CharacterId = req.CharacterId
            });

            repo.SaveDictEntry(userId, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, isLandData);
            db.SaveChanges();
        }

        return new ResSaveMiniGameIslandBreakerLastSelectedCharacter();
    }

    public ResGetMiniGameIslandBreaker GetIslandBreaker(User user, int islandBreakerId)
    {
        var manage = GameData.Instance.EventIslandBreakerManagerTable.Values
            .FirstOrDefault(x => x.MinigameType == MiniGameSystemType.Normal)
            ?? throw new InvalidOperationException("EventIslandBreakerManagerTable Normal entry not found.");

        bool dirty = false;
        if (!repo.TryGetDictEntry<IsLandBreakerData>(
                user.ID, MinigameRepository.TypeIsLandBreaker, islandBreakerId, out var isLandData)
            || isLandData == null)
        {
            isLandData = new IsLandBreakerData { IslandBreakerId = islandBreakerId };

            var missionlist = GameData.Instance.EventIslandBreakerMissionTable.Values
                .Where(x => x.MissionGroup == manage.MissionGroup)
                .ToList();
            foreach (var item in missionlist)
            {
                isLandData.Missions.TryAdd(item.Id, new()
                {
                    Rewarded = false,
                    MissionId = item.Id,
                    Progress = 0
                });
            }

            var chardata = GameData.Instance.EventIslandBreakerCharacterTable.Values.ToList();
            foreach (var item in chardata)
            {
                isLandData.CharacterStatistics.TryAdd(item.Id, new()
                {
                    CharacterId = item.Id
                });
            }

            if (chardata.Count > 0)
                isLandData.LastSelectedCharacterId = chardata[0].Id;

            var bufflist = GameData.Instance.EventIslandBreakerBuffTable.Values
                .Where(x => x.BuffLevel == 0)
                .Select(x => x.Id)
                .ToList();
            isLandData.Buffs.AddRange(bufflist);

            foreach (var item in GameData.Instance.EventIslandBreakerCurrencyTable.Values)
            {
                isLandData.Currencies.TryAdd(item.Id, new()
                {
                    CurrencyId = item.Id,
                    MaxLimit = item.Limit3Max,
                    CurrentAmount = 0
                });
            }

            dirty = true;
        }

        int day = user.GetDateDay();
        if (day != isLandData.ActiveDate)
        {
            isLandData.ActiveDate = day;
            isLandData.DailyScore = new() { IsDailyRewarded = false, Score = 0 };
            dirty = true;
        }

        if (dirty)
        {
            repo.SaveDictEntry(user.ID, MinigameRepository.TypeIsLandBreaker, islandBreakerId, isLandData);
            db.SaveChanges();
        }

        return isLandData.ToNet();
    }

    public ResGetMiniGameIslandBreakerRedDotData GetIslandBreakerRedDot(ulong userId, int islandBreakerId)
    {
        var response = new ResGetMiniGameIslandBreakerRedDotData();

        if (repo.TryGetDictEntry<IsLandBreakerData>(
                userId, MinigameRepository.TypeIsLandBreaker, islandBreakerId, out var isLandData)
            && isLandData != null
            && MiniGameHelper.IsLandGetComplete(isLandData.Missions))
        {
            response.MissionRewardExists = true;
        }

        return response;
    }

    public ResAcquireMiniGameIslandBreakerMissionReward AcquireIslandBreakerMission(
        User user,
        ReqAcquireMiniGameIslandBreakerMissionReward req)
    {
        var response = new ResAcquireMiniGameIslandBreakerMissionReward();
        List<int> rewardlist = [];

        if (repo.TryGetDictEntry<IsLandBreakerData>(
                user.ID, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, out var isLandData)
            && isLandData != null)
        {
            foreach (var item in req.MissionIds)
            {
                var mission = GameData.Instance.EventIslandBreakerMissionTable.Values
                    .FirstOrDefault(x => x.Id == item);
                if (mission != null)
                {
                    rewardlist.Add(mission.RewardId);
                }

                if (isLandData.Missions.TryGetValue(item, out var missionData))
                {
                    missionData.Rewarded = true;
                }
            }

            response.Reward = RewardUtils.RegisterRewardsForUserDou(user, rewardlist);
            response.UpdatedMissions.AddRange(MiniGameHelper
                .ToProtoDict<int, NetMiniGameIslandBreakerMission, MiniGameIslandBreakerMission>(isLandData.Missions)
                .Values);

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, isLandData);
            db.SaveChanges();
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResGetMiniGameIslandBreakerGuildRanking GetIslandBreakerGuildRanking(User user, int islandBreakerId)
    {
        var response = new ResGetMiniGameIslandBreakerGuildRanking
        {
            UserEntry = new()
            {
                User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
            }
        };

        var manage = GameData.Instance.EventIslandBreakerManagerTable.Values
            .FirstOrDefault(x => x.MinigameType == MiniGameSystemType.Normal);

        if (manage != null && user.Guild.guildId is int guildId && guildId > 0)
        {
            var allBoard = MiniGameHelper.GetFullLeaderboard(guildId, manage.EventId, islandBreakerId);
            foreach (var item in allBoard)
            {
                if (TryCreateWholeUser(item.Record.UserId) is not { } wholeUser)
                    continue;

                var entry = new NetMiniGameIslandBreakerRanking
                {
                    Rank = item.Rank,
                    Score = item.Record.Score,
                    User = wholeUser
                };

                response.Entries.Add(entry);
                if (item.Record.UserId == user.ID)
                    response.UserEntry = entry;
            }
        }

        return response;
    }

    public ResMiniGameIslandBreakerResult SubmitIslandBreakerResult(User user, ReqMiniGameIslandBreakerResult req)
    {
        var response = new ResMiniGameIslandBreakerResult();
        var manage = GameData.Instance.EventIslandBreakerManagerTable.Values
            .FirstOrDefault(x => x.MinigameType == MiniGameSystemType.Normal)
            ?? throw new InvalidOperationException("EventIslandBreakerManagerTable Normal entry not found.");

        if (repo.TryGetDictEntry<IsLandBreakerData>(
                user.ID, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, out var isLandData)
            && isLandData != null)
        {
            bool rewardGranted = false;

            if (isLandData.CharacterStatistics.TryGetValue(req.CharacterId, out var charStats))
            {
                charStats.CumulativeScore += req.Score;
                charStats.PlayCount += 1;
            }

            isLandData.CumulativePlayScore += req.Score;
            isLandData.DailyScore.Score += req.Score;

            if (!isLandData.DailyScore.IsDailyRewarded)
            {
                response.DailyReward = RewardUtils.RegisterRewardsForUser(user, manage.DailyRewardId);
                isLandData.DailyScore.IsDailyRewarded = true;
                rewardGranted = true;
            }

            if (req.Score > isLandData.HighScore.HighScore)
            {
                isLandData.HighScore.HighScore = req.Score;
                MiniGameHelper.InsertOrUpdate(
                    manage.EventId,
                    user.ID,
                    user.Guild.guildId ?? 0,
                    req.Score,
                    req.IslandBreakerId);
            }

            if (req.Wave > isLandData.HighScore.HighWave)
            {
                isLandData.HighScore.HighWave = req.Wave;
            }

            int currency = req.Wave <= 50 ? req.Wave * 2 : 100;
            if (isLandData.Currencies.TryGetValue(2, out var buffCurrency))
            {
                buffCurrency.CurrentAmount = Math.Min(buffCurrency.CurrentAmount + currency, buffCurrency.MaxLimit);
                buffCurrency.Granted = currency;
                buffCurrency.CumulativeAcquired += currency;
            }

            var imageid = GameData.Instance.EventIslandBreakerImageTable.Values
                .Where(x => x.ImageGroup == manage.ImageGroup
                            && x.ConditionCharacter == req.CharacterId
                            && x.ConditionWave <= req.Wave)
                .OrderByDescending(x => x.ConditionWave)
                .FirstOrDefault();
            if (imageid != null)
            {
                isLandData.Album.TryAdd(imageid.Id, new() { ImageId = imageid.Id, Unlocked = true });
            }

            isLandData.CumulativeSummonBallCount += req.SummonBallCount;
            UpIslandBreakerMission(isLandData, waveCount: req.Wave, req.CharacterId, req.Wave);

            response.BanResult = MiniGameBanResult.Success;
            response.Currencies.AddRange(MiniGameHelper
                .ToProtoDict<int, NetMiniGameIslandBreakerCurrency, MiniGameIslandBreakerCurrency>(isLandData.Currencies)
                .Values);
            response.DailyScore = isLandData.DailyScore.ToNet();
            response.HighScore = isLandData.HighScore.ToNet();
            response.UnlockedImage = isLandData.Album.Count;

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, isLandData);
            db.SaveChanges();

            if (rewardGranted)
                UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResSaveMiniGameIslandBreakerSeenContent SaveIslandBreakerSeen(
        ulong userId,
        ReqSaveMiniGameIslandBreakerSeenContent req)
    {
        if (repo.TryGetDictEntry<IsLandBreakerData>(
                userId, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, out var isLandData)
            && isLandData != null)
        {
            isLandData.SeenCharacterIds.AddRangeUnique(req.SeenCharacterIds);
            isLandData.SeenImageIds.AddRangeUnique(req.SeenImageIds);

            repo.SaveDictEntry(userId, MinigameRepository.TypeIsLandBreaker, req.IslandBreakerId, isLandData);
            db.SaveChanges();
        }

        return new ResSaveMiniGameIslandBreakerSeenContent();
    }

    private static void UpIslandBreakerMission(
        IsLandBreakerData isLandData,
        int waveCount = 0,
        int charid = 0,
        int getCurrency = 0)
    {
        var manage = GameData.Instance.EventIslandBreakerManagerTable.Values
            .FirstOrDefault(x => x.Id == isLandData.IslandBreakerId);
        if (manage == null)
            return;

        List<EventIslandBreakerMissionRecord_Raw> missionlist = GameData.Instance.EventIslandBreakerMissionTable.Values
            .Where(x => x.MissionGroup == manage.MissionGroup)
            .ToList();

        foreach (IslandBreakerMissionType missionType in Enum.GetValues<IslandBreakerMissionType>())
        {
            switch (missionType)
            {
                case IslandBreakerMissionType.WaveCount:
                    foreach (var item in missionlist.Where(x => x.ConditionType == missionType))
                    {
                        if (isLandData.Missions.TryGetValue(item.Id, out var value))
                            value.Progress = waveCount;
                    }
                    break;
                case IslandBreakerMissionType.PlayPoint:
                    foreach (var item in missionlist.Where(x => x.ConditionType == missionType))
                    {
                        if (isLandData.Missions.TryGetValue(item.Id, out var value))
                            value.Progress = isLandData.CumulativePlayScore;
                    }
                    break;
                case IslandBreakerMissionType.PlayCharacterPoint:
                    foreach (var item in missionlist.Where(x => x.ConditionType == missionType && x.ConditionId == charid))
                    {
                        if (isLandData.Missions.TryGetValue(item.Id, out var value)
                            && isLandData.CharacterStatistics.TryGetValue(charid, out var charStats))
                        {
                            value.Progress = charStats.CumulativeScore;
                        }
                    }
                    break;
                case IslandBreakerMissionType.SummonBall:
                    foreach (var item in missionlist.Where(x => x.ConditionType == missionType))
                    {
                        if (isLandData.Missions.TryGetValue(item.Id, out var value))
                            value.Progress = isLandData.CumulativeSummonBallCount;
                    }
                    break;
                case IslandBreakerMissionType.GetCurrency:
                    foreach (var item in missionlist.Where(x => x.ConditionType == missionType))
                    {
                        if (isLandData.Missions.TryGetValue(item.Id, out var value))
                            value.Progress += getCurrency;
                    }
                    break;
            }
        }
    }

    // --- ScenarioChoice & Arcade Scenarios ---

    public ResGetScenarioChoiceData GetScenarioChoice(ulong userId, int id)
        => new() { Data = ScenarioChoiceMain.GetData(userId, id, repo, db) };

    public ResUpdateScenarioChoice UpdateScenarioChoice(ulong userId, ReqUpdateScenarioChoice req)
        => new()
        {
            Data = ScenarioChoiceMain.Update(
                userId, req.ScenarioChoiceManagerTableId, req.ScenarioScene, req.ChosenChoices, repo, db)
        };

    public ResCompleteArcadeScenario CompleteArcadeScenario(ulong userId, ReqCompleteArcadeScenario req)
    {
        var evt = repo.GetOrCreateDictEntry<MiniGameScenarios>(
            userId, MinigameRepository.TypeMiniGameScenarios, req.ArcadeId);

        if (!evt.CompletedScenarios.Contains(req.ScenarioId))
        {
            evt.CompletedScenarios.Add(req.ScenarioId);
            repo.SaveDictEntry(userId, MinigameRepository.TypeMiniGameScenarios, req.ArcadeId, evt);
            db.SaveChanges();
        }

        return new ResCompleteArcadeScenario();
    }

    public ResGetArcadeScenario GetArcadeScenario(ulong userId, int arcadeId)
    {
        var response = new ResGetArcadeScenario();
        if (repo.TryGetDictEntry<MiniGameScenarios>(
                userId, MinigameRepository.TypeMiniGameScenarios, arcadeId, out var scenarios)
            && scenarios != null)
        {
            response.ArcadeScenarioIdList.AddRange(scenarios.CompletedScenarios);
        }
        return response;
    }

    // --- BBQ ---

    public ResClearArcadeBBQ ClearBbq(User user, ReqClearArcadeBBQ req)
    {
        var bbq = repo.GetSingle<ArcadeBBQData>(user.ID, MinigameRepository.TypeBbqInfo);
        int newscore = req.Score * 10;
        bool triggerAdded = false;

        if (newscore > bbq.HighScore)
        {
            bbq.HighScore = newscore;

            if (user.Guild.guildId is int guildId && guildId > 0)
            {
                MiniGameHelper.InsertOrUpdate(req.ArcadeId, user.ID, guildId, newscore, 0);
            }

            user.AddTrigger(Trigger.EventBBQTycoonHighScore, newscore, 0);
            triggerAdded = true;
        }

        bbq.PlayCount += 1;
        bbq.TotalAccumulatedScore += newscore;

        repo.SaveSingle(user.ID, MinigameRepository.TypeBbqInfo, bbq);
        db.SaveChanges();
        if (triggerAdded)
            UserSessionStore.SaveUser(user);

        return new ResClearArcadeBBQ { Data = MiniGameHelper.BBQToNet(bbq) };
    }

    public ResGetArcadeBBQData GetBbq(ulong userId, int arcadeId)
    {
        var bbq = repo.GetSingle<ArcadeBBQData>(userId, MinigameRepository.TypeBbqInfo);
        if (bbq.ArcadeId == 0)
        {
            bbq.ArcadeId = arcadeId;
            repo.SaveSingle(userId, MinigameRepository.TypeBbqInfo, bbq);
            db.SaveChanges();
        }

        return new ResGetArcadeBBQData { Data = MiniGameHelper.BBQToNet(bbq) };
    }

    public ResGetArcadeBBQRanking GetBbqRanking(User user, int arcadeId)
    {
        var response = new ResGetArcadeBBQRanking { UserGuildRanking = new() };

        if (user.Guild.guildId is int guildId && guildId > 0)
        {
            var allBoard = MiniGameHelper.GetFullLeaderboard(guildId, arcadeId);
            foreach (var item in allBoard)
            {
                if (TryCreateWholeUser(item.Record.UserId) is not { } wholeUser)
                    continue;

                var guild = new NetArcadeBBQRankingData
                {
                    Rank = item.Rank,
                    Score = item.Record.Score,
                    User = wholeUser
                };

                response.GuildRankingList.Add(guild);
                if (item.Record.UserId == user.ID)
                    response.UserGuildRanking = guild;
            }
        }

        return response;
    }

    public ResObtainArcadeBBQStepUpReward ObtainBbqStepUpReward(User user, int stepUpRewardId)
    {
        var bbq = repo.GetSingle<ArcadeBBQData>(user.ID, MinigameRepository.TypeBbqInfo);
        bbq.StepUpRewardedList.AddUnique(stepUpRewardId);

        var response = new ResObtainArcadeBBQStepUpReward
        {
            Data = MiniGameHelper.BBQToNet(bbq)
        };

        var rewardRecord = GameData.Instance.EventBBQTycoonStepUpRewardTable.Values
            .FirstOrDefault(x => x.Id == stepUpRewardId);
        if (rewardRecord != null)
        {
            response.Reward = RewardUtils.RegisterRewardsForUser(user, rewardRecord.RewardId);
        }

        user.AddTrigger(Trigger.EventBBQTycoonDailyRewardCheck, 1, stepUpRewardId);

        repo.SaveSingle(user.ID, MinigameRepository.TypeBbqInfo, bbq);
        db.SaveChanges();
        UserSessionStore.SaveUser(user);

        return response;
    }

    public ResRecordArcadeBBQCutScene RecordBbqCutscene(ulong userId, int cutSceneTid)
    {
        var bbq = repo.GetSingle<ArcadeBBQData>(userId, MinigameRepository.TypeBbqInfo);
        bbq.RecordedCutSceneList.AddUnique(cutSceneTid);

        repo.SaveSingle(userId, MinigameRepository.TypeBbqInfo, bbq);
        db.SaveChanges();

        return new ResRecordArcadeBBQCutScene { Data = MiniGameHelper.BBQToNet(bbq) };
    }

    // --- BTG ---

    public ResClearArcadeBtg ClearBtg(User user, ReqClearArcadeBtg req)
    {
        var response = new ResClearArcadeBtg();
        var arcade = GetArcadeManager(ArcadeGameType.BTG);

        if (repo.TryGetDictEntry<ArcadeBtgData>(user.ID, MinigameRepository.TypeBtg, req.BtgId, out var btgData)
            && btgData != null)
        {
            if (req.Score > btgData.Score)
            {
                btgData.Score = req.Score;

                if (user.Guild.guildId is int guildId && guildId > 0)
                {
                    MiniGameHelper.InsertOrUpdate(arcade.Id, user.ID, guildId, req.Score, req.BtgId);
                }
            }

            btgData.Data.CutSceneList.AddUnique(req.UnlockedCutScene);
            btgData.Data.WallpaperList.AddUnique(req.UnlockedWallpaper);
            btgData.Data.TotalAccumulatedScore += req.Score;

            UpBtgMission(req.Score, req.ClearData, btgData);

            response.BtgData = MiniGameHelper.ToProto<NetArcadeBtgData, BtgData>(btgData.Data);
            var missionlist = MiniGameHelper.ToProtoDict<int, NetMiniGameBtgMissionData, MiniGameBtgMissionData>(btgData.MissionDatas);
            response.AchievementMissionDataList.AddRange(missionlist.Values);

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeBtg, req.BtgId, btgData);
            db.SaveChanges();
        }

        user.AddTrigger(Trigger.EventBFGCleanUpRewardCheck, 1, 1);
        UserSessionStore.SaveUser(user);

        response.BanResult = MiniGameBanResult.Success;
        return response;
    }

    public ResGetArcadeBtgData GetBtg(ulong userId, int btgId)
    {
        var response = new ResGetArcadeBtgData();

        if (repo.TryGetDictEntry<ArcadeBtgData>(userId, MinigameRepository.TypeBtg, btgId, out var btgData)
            && btgData != null)
        {
            response.BtgData = MiniGameHelper.ToProto<NetArcadeBtgData, BtgData>(btgData.Data);
        }
        else
        {
            var netArcadeBtgData = new BtgData { BtgId = btgId };
            var missionlist = GameData.Instance.EventBTGMissionTable.Values
                .Where(x => x.ManagerId == btgId)
                .ToList();

            Dictionary<int, MiniGameBtgMissionData> missionDatas = [];
            foreach (var item in missionlist)
            {
                missionDatas.TryAdd(item.Id, new MiniGameBtgMissionData
                {
                    MissionId = item.Id,
                    Progress = 0,
                    IsReceived = false
                });
            }

            var newData = new ArcadeBtgData
            {
                Data = netArcadeBtgData,
                MissionDatas = missionDatas
            };

            repo.SaveDictEntry(userId, MinigameRepository.TypeBtg, btgId, newData);
            db.SaveChanges();

            response.BtgData = MiniGameHelper.ToProto<NetArcadeBtgData, BtgData>(netArcadeBtgData);
        }

        return response;
    }

    public ResGetArcadeBtgMissionData GetBtgMission(ulong userId, int btgId)
    {
        var response = new ResGetArcadeBtgMissionData();

        if (repo.TryGetDictEntry<ArcadeBtgData>(userId, MinigameRepository.TypeBtg, btgId, out var btgData)
            && btgData != null)
        {
            var missionlist = MiniGameHelper.ToProtoDict<int, NetMiniGameBtgMissionData, MiniGameBtgMissionData>(btgData.MissionDatas);
            response.AchievementMissionDataList.AddRange(missionlist.Values);
        }

        return response;
    }

    public ResGetArcadeBtgRanking GetBtgRanking(User user, int btgId)
    {
        var response = new ResGetArcadeBtgRanking
        {
            UserGuildRanking = new()
            {
                Rank = 0,
                Score = 0,
                User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
            }
        };

        var arcade = GetArcadeManager(ArcadeGameType.BTG);

        if (user.Guild.guildId is int guildId && guildId > 0)
        {
            var allBoard = MiniGameHelper.GetFullLeaderboard(guildId, arcade.Id, btgId);
            foreach (var item in allBoard)
            {
                if (TryCreateWholeUser(item.Record.UserId) is not { } wholeUser)
                    continue;

                var guild = new NetMiniGameBtgRankingData
                {
                    Rank = item.Rank,
                    Score = (int)item.Record.Score,
                    User = wholeUser
                };

                response.GuildRankingList.Add(guild);
                if (item.Record.UserId == user.ID)
                    response.UserGuildRanking = guild;
            }
        }

        return response;
    }

    public ResClaimArcadeBtgMissionReward ClaimBtgMission(User user, ReqClaimArcadeBtgMissionReward req)
    {
        var response = new ResClaimArcadeBtgMissionReward();
        List<int> rewardlist = [];

        if (repo.TryGetDictEntry<ArcadeBtgData>(user.ID, MinigameRepository.TypeBtg, req.BtgId, out var btgData)
            && btgData != null)
        {
            foreach (var item in req.MissionIdList)
            {
                if (btgData.MissionDatas.TryGetValue(item, out var value))
                    value.IsReceived = true;

                var mission = GameData.Instance.EventBTGMissionTable.Values.FirstOrDefault(x => x.Id == item);
                if (mission != null)
                    rewardlist.Add(mission.RewardId);
            }

            response.Reward = RewardUtils.RegisterRewardsForUserDou(user, rewardlist);
            var missionlist = MiniGameHelper.ToProtoDict<int, NetMiniGameBtgMissionData, MiniGameBtgMissionData>(btgData.MissionDatas);
            response.AchievementMissionDataList.AddRange(missionlist.Values);

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeBtg, req.BtgId, btgData);
            db.SaveChanges();
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResSetArcadeBtgTutorialState SetBtgTutorial(ulong userId, ReqSetArcadeBtgTutorialState req)
    {
        if (repo.TryGetDictEntry<ArcadeBtgData>(userId, MinigameRepository.TypeBtg, req.BtgId, out var btgData)
            && btgData != null)
        {
            btgData.Data.TutorialState = req.TutorialState;
            repo.SaveDictEntry(userId, MinigameRepository.TypeBtg, req.BtgId, btgData);
            db.SaveChanges();
        }

        return new ResSetArcadeBtgTutorialState();
    }

    private static void UpBtgMission(int score, NetMiniGameBtgClearData clearData, ArcadeBtgData btgData)
    {
        if (btgData.MissionDatas.Count == 0)
            return;

        List<EventBTGMissionRecord_Raw> missions = GameData.Instance.EventBTGMissionTable.Values
            .Where(x => x.ManagerId == btgData.Data.BtgId)
            .ToList();

        foreach (EventBTGMissionRecord_Raw miss in missions.Where(x => x.ConditionType == EventBTGMissionConditionType.GetPointStack))
        {
            if (btgData.MissionDatas.TryGetValue(miss.Id, out var value) && !value.IsReceived)
                value.Progress += score;
        }

        foreach (EventBTGMissionRecord_Raw miss in missions.Where(x => x.ConditionType == EventBTGMissionConditionType.UseSkillCount))
        {
            if (btgData.MissionDatas.TryGetValue(miss.Id, out var value) && !value.IsReceived)
            {
                value.Progress += miss.ConditionId switch
                {
                    1 => clearData.UseSkill1Count,
                    2 => clearData.UseSkill2Count,
                    _ => 0
                };
            }
        }
    }

    // --- BubbleMarch ---

    public ResAcquireArcadeBubbleMarchAchievementMissionReward AcquireBubbleMarchAchievementMissionReward(
        User user,
        ReqAcquireArcadeBubbleMarchAchievementMissionReward req)
    {
        var response = new ResAcquireArcadeBubbleMarchAchievementMissionReward();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);
        List<int> rewardlist = [];

        if (repo.TryGetDictEntry<BubbleMarchData>(
                user.ID, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            foreach (var item in req.MissionIdList)
            {
                var mission = GameData.Instance.EventBubbleMarchMissionTable.Values.FirstOrDefault(x => x.Id == item);
                if (mission != null)
                    rewardlist.Add(mission.RewardId);

                if (marchData.AchievementMissionDataList.TryGetValue(item, out var missionData))
                    missionData.IsReceived = true;
            }

            response.Reward = RewardUtils.RegisterRewardsForUserDou(user, rewardlist);
            response.UpdatedAchievementMissions.AddRange(MiniGameHelper
                .ToProtoDict<int, NetMiniGameBubbleMarchMissionData, MiniGameBubbleMarchMissionData>(marchData.AchievementMissionDataList)
                .Values);

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResClearArcadeBubbleMarchTutorial ClearBubbleMarchTutorial(
        ulong userId,
        ReqClearArcadeBubbleMarchTutorial req)
    {
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            marchData.ClearedTutorialIdList.AddRangeUnique(req.TutorialIds);
            repo.SaveDictEntry(userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        return new ResClearArcadeBubbleMarchTutorial();
    }

    public ResFinishArcadeBubbleMarchChallengeStage FinishBubbleMarchChallengeStage(
        User user,
        ReqFinishArcadeBubbleMarchChallengeStage req)
    {
        var response = new ResFinishArcadeBubbleMarchChallengeStage();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                user.ID, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            if (!marchData.ClearedStageIdList.Contains(req.StageId))
                marchData.ClearedStageIdList.Add(req.StageId);

            if (marchData.WaveProgress.TryGetValue(req.StageMissionData.WaveClear, out var wprogress))
            {
                if (req.WaveProgress > wprogress)
                {
                    marchData.WaveProgress[req.StageMissionData.WaveClear] = req.WaveProgress;

                    if (user.Guild.guildId is int guildId && guildId > 0)
                    {
                        MiniGameHelper.InsertOrUpdate(
                            arcade.Id,
                            user.ID,
                            guildId,
                            (int)req.WaveProgress,
                            req.StageMissionData.WaveClear);
                    }
                }
            }
            else
            {
                marchData.WaveProgress[req.StageMissionData.WaveClear] = req.WaveProgress;
            }

            var progress = new BmHelper.MissionProgressData
            {
                ChallengeWaveClear = req.StageMissionData.WaveClear,
                CostCurrency = (1, req.StageMissionData.UseCurrency),
                ClearStage = req.StageId,
                HeroSummon = req.StageMissionData.HeroSummon.Keys.ToList(),
                KillEnemy = req.StageMissionData.KillEnemy.Values.Sum(),
                MinionSummon = req.StageMissionData.MinionSummon.Values.Sum(),
                TargetLevelUp = req.StageMissionData.TargetLevelUp.Values.DefaultIfEmpty(0).Max(),
                LevelUpCount = req.StageMissionData.LevelUpCount
            };

            BmHelper.UpMission(marchData, MiniGameSystemType.Arcade, progress);

            response.UpdatedAchievementMissions.AddRange(MiniGameHelper
                .ToProtoDict<int, NetMiniGameBubbleMarchMissionData, MiniGameBubbleMarchMissionData>(marchData.AchievementMissionDataList)
                .Values);

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        return response;
    }

    public ResFinishArcadeBubbleMarchGeneralStage FinishBubbleMarchGeneralStage(
        ulong userId,
        ReqFinishArcadeBubbleMarchGeneralStage req)
    {
        var response = new ResFinishArcadeBubbleMarchGeneralStage();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            var stage = GameData.Instance.EventBubbleMarchStageTable.Values
                .FirstOrDefault(x => x.Id == req.StageId);

            if (stage != null && req.IsWin && !marchData.ClearedStageIdList.Contains(req.StageId))
            {
                marchData.ClearedStageIdList.Add(req.StageId);
                marchData.UpgradeCurrency[stage.FirstRewardCurrencyId] =
                    marchData.UpgradeCurrency.GetValueOrDefault(stage.FirstRewardCurrencyId) + stage.FirstRewardCurrencyValue;

                response.RewardUpgradeCurrency[stage.FirstRewardCurrencyId] = stage.FirstRewardCurrencyValue;
            }

            var progress = new BmHelper.MissionProgressData
            {
                CostCurrency = (1, req.StageMissionData.UseCurrency),
                ClearStage = req.StageId,
                HeroSummon = req.StageMissionData.HeroSummon.Keys.ToList(),
                KillEnemy = req.StageMissionData.KillEnemy.Values.Sum(),
                WaveClear = req.StageMissionData.WaveClear,
                MinionSummon = req.StageMissionData.MinionSummon.Values.Sum(),
                TargetLevelUp = req.StageMissionData.TargetLevelUp.Values.DefaultIfEmpty(0).Max(),
                LevelUpCount = req.StageMissionData.LevelUpCount
            };

            BmHelper.UpMission(marchData, MiniGameSystemType.Arcade, progress);

            response.UpdatedAchievementMissions.AddRange(MiniGameHelper
                .ToProtoDict<int, NetMiniGameBubbleMarchMissionData, MiniGameBubbleMarchMissionData>(marchData.AchievementMissionDataList)
                .Values);

            foreach (var item in marchData.UpgradeCurrency)
            {
                response.CurrentUpgradeCurrency.TryAdd(item.Key, item.Value);
            }

            repo.SaveDictEntry(userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        return response;
    }

    public ResGetArcadeBubbleMarchData GetBubbleMarch(ulong userId)
    {
        var response = new ResGetArcadeBubbleMarchData();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);
        var manage = GameData.Instance.EventBubbleMarchManagerTable.Values
            .FirstOrDefault(x => x.Id == arcade.GameManagerId)
            ?? throw new InvalidOperationException($"EventBubbleMarchManagerTable entry {arcade.GameManagerId} not found.");

        if (!repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            || marchData == null)
        {
            marchData = new BubbleMarchData();

            var missionlist = GameData.Instance.EventBubbleMarchMissionTable.Values
                .Where(x => x.MissionGroup == manage.MissionGroup)
                .ToList();

            foreach (var item in missionlist)
            {
                marchData.AchievementMissionDataList.TryAdd(item.Id, new()
                {
                    IsReceived = false,
                    MissionId = item.Id,
                    Progress = 0
                });
            }

            repo.SaveDictEntry(userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        response.AchievementMissionDataList.AddRange(MiniGameHelper
            .ToProtoDict<int, NetMiniGameBubbleMarchMissionData, MiniGameBubbleMarchMissionData>(marchData.AchievementMissionDataList)
            .Values);
        response.BuffUpgradeIdList.AddRange(marchData.BuffUpgradeIdList);
        response.CharacterUpgradeIdList.AddRange(marchData.CharacterUpgradeIdList);
        response.ClearedStageIdList.AddRange(marchData.ClearedStageIdList);
        response.ClearedTutorialIdList.AddRange(marchData.ClearedTutorialIdList);
        response.LevelHideOptionActive = marchData.LevelHideOptionActive;

        foreach (var item in marchData.UpgradeCurrency)
        {
            response.UpgradeCurrency.TryAdd(item.Key, item.Value);
        }

        return response;
    }

    public ResGetArcadeBubbleMarchMissionData GetBubbleMarchMission(ulong userId)
    {
        var response = new ResGetArcadeBubbleMarchMissionData();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            response.AchievementMissionDataList.AddRange(MiniGameHelper
                .ToProtoDict<int, NetMiniGameBubbleMarchMissionData, MiniGameBubbleMarchMissionData>(marchData.AchievementMissionDataList)
                .Values);
        }

        return response;
    }

    public ResGetArcadeBubbleMarchRanking GetBubbleMarchRanking(User user)
    {
        var response = new ResGetArcadeBubbleMarchRanking
        {
            UserGuildRanking = new()
            {
                User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
            }
        };

        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (user.Guild.guildId is int guildId && guildId > 0)
        {
            var allBoard = MiniGameHelper.GetFullLeaderboard(guildId, arcade.Id);
            if (allBoard.Any())
            {
                var bestRecords = allBoard
                    .GroupBy(x => x.Record.UserId)
                    .Select(g => g.OrderByDescending(x => x.Record.ModeId).First())
                    .ToList();

                foreach (var item in bestRecords)
                {
                    if (TryCreateWholeUser(item.Record.UserId) is not { } wholeUser)
                        continue;

                    var guild = new NetMiniGameBubbleMarchRankingData
                    {
                        Rank = item.Rank,
                        Wave = item.Record.ModeId,
                        WaveProgress = item.Record.Score,
                        User = wholeUser
                    };

                    response.GuildRankingList.Add(guild);
                    if (item.Record.UserId == user.ID)
                        response.UserGuildRanking = guild;
                }
            }
        }

        return response;
    }

    public ResResetArcadeBubbleMarchBuffUpgrade ResetBubbleMarchBuffUpgrade(ulong userId)
    {
        var response = new ResResetArcadeBubbleMarchBuffUpgrade();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            var bufflist = GameData.Instance.EventBubbleMarchBuffTable.Values
                .Where(x => marchData.BuffUpgradeIdList.Contains(x.Id))
                .ToList();
            var costSummary = bufflist
                .GroupBy(x => x.UpgradeCostCurrencyId)
                .Select(g => new
                {
                    CurrencyId = g.Key,
                    TotalCost = g.Sum(x => x.UpgradeCostCurrencyValue)
                })
                .ToList();

            foreach (var item in costSummary)
            {
                marchData.UpgradeCurrency[item.CurrencyId] =
                    marchData.UpgradeCurrency.GetValueOrDefault(item.CurrencyId) + item.TotalCost;
                response.UpgradeCurrencyId = item.CurrencyId;
                response.UpgradeCurrencyValue = marchData.UpgradeCurrency[item.CurrencyId];
            }

            marchData.BuffUpgradeIdList.Clear();

            repo.SaveDictEntry(userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        return response;
    }

    public ResResetArcadeBubbleMarchCharacterUnlockUpgrade ResetBubbleMarchCharacterUnlockUpgrade(ulong userId)
    {
        var response = new ResResetArcadeBubbleMarchCharacterUnlockUpgrade();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            var bufflist = GameData.Instance.EventBubbleMarchCharacterUnlockTable.Values
                .Where(x => marchData.CharacterUpgradeIdList.Contains(x.Id))
                .ToList();
            var costSummary = bufflist
                .GroupBy(x => x.UnlockCurrencyId)
                .Select(g => new
                {
                    CurrencyId = g.Key,
                    TotalCost = g.Sum(x => x.UnlockCurrencyValue)
                })
                .ToList();

            foreach (var item in costSummary)
            {
                marchData.UpgradeCurrency[item.CurrencyId] =
                    marchData.UpgradeCurrency.GetValueOrDefault(item.CurrencyId) + item.TotalCost;
                response.UpgradeCurrencyId = item.CurrencyId;
                response.UpgradeCurrencyValue = marchData.UpgradeCurrency[item.CurrencyId];
            }

            marchData.CharacterUpgradeIdList.Clear();

            repo.SaveDictEntry(userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        return response;
    }

    public ResSetArcadeBubbleMarchBuffUpgrade SetBubbleMarchBuffUpgrade(ulong userId, int upgradeId)
    {
        var response = new ResSetArcadeBubbleMarchBuffUpgrade();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            var buff = GameData.Instance.EventBubbleMarchBuffTable.Values
                .FirstOrDefault(x => x.Id == upgradeId);

            if (buff != null)
            {
                if (marchData.UpgradeCurrency.ContainsKey(buff.UpgradeCostCurrencyId))
                {
                    var progress = new BmHelper.MissionProgressData
                    {
                        CostCurrency = (buff.UpgradeCostCurrencyId, buff.UpgradeCostCurrencyValue),
                        BuffLevel = buff.BuffLevel
                    };

                    BmHelper.UpMission(marchData, MiniGameSystemType.Arcade, progress);
                }

                marchData.BuffUpgradeIdList.AddUnique(upgradeId);
                response.UpgradeCurrencyId = buff.UpgradeCostCurrencyId;
                response.UpgradeCurrencyValue = marchData.UpgradeCurrency.GetValueOrDefault(buff.UpgradeCostCurrencyId);
            }

            response.UpdatedAchievementMissionList.AddRange(MiniGameHelper
                .ToProtoDict<int, NetMiniGameBubbleMarchMissionData, MiniGameBubbleMarchMissionData>(marchData.AchievementMissionDataList)
                .Values);

            repo.SaveDictEntry(userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        return response;
    }

    public ResSetArcadeBubbleMarchCharacterUnlockUpgrade SetBubbleMarchCharacterUnlockUpgrade(
        ulong userId,
        int upgradeId)
    {
        var response = new ResSetArcadeBubbleMarchCharacterUnlockUpgrade();
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            var unlckchar = GameData.Instance.EventBubbleMarchCharacterUnlockTable.Values
                .FirstOrDefault(x => x.Id == upgradeId);

            if (unlckchar != null)
            {
                if (marchData.UpgradeCurrency.ContainsKey(unlckchar.UnlockCurrencyId))
                {
                    var progress = new BmHelper.MissionProgressData
                    {
                        CostCurrency = (unlckchar.UnlockCurrencyId, unlckchar.UnlockCurrencyValue)
                    };

                    BmHelper.UpMission(marchData, MiniGameSystemType.Arcade, progress);
                    marchData.CharacterUpgradeIdList.AddUnique(upgradeId);
                }

                response.UpgradeCurrencyId = unlckchar.UnlockCurrencyId;
                response.UpgradeCurrencyValue = marchData.UpgradeCurrency.GetValueOrDefault(unlckchar.UnlockCurrencyId);
            }

            response.UpdatedAchievementMissionList.AddRange(MiniGameHelper
                .ToProtoDict<int, NetMiniGameBubbleMarchMissionData, MiniGameBubbleMarchMissionData>(marchData.AchievementMissionDataList)
                .Values);

            repo.SaveDictEntry(userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        return response;
    }

    public ResSetArcadeBubbleMarchOption SetBubbleMarchOption(ulong userId, bool levelHideOptionIsActive)
    {
        var arcade = GetArcadeManager(ArcadeGameType.BubbleMarch);

        if (repo.TryGetDictEntry<BubbleMarchData>(
                userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, out var marchData)
            && marchData != null)
        {
            marchData.LevelHideOptionActive = levelHideOptionIsActive;
            repo.SaveDictEntry(userId, MinigameRepository.TypeBubbleMarch, arcade.GameManagerId, marchData);
            db.SaveChanges();
        }

        return new ResSetArcadeBubbleMarchOption();
    }

    // --- DessertRush ---

    public ResClearArcadeDessertRush ClearDessertRush(User user, ReqClearArcadeDessertRush req)
    {
        var data = repo.GetSingle<ArcadeDessertRushData>(user.ID, MinigameRepository.TypeDessertRush);
        int newscore = req.Score;

        if (newscore > data.HighScore)
        {
            data.HighScore = newscore;
            if (user.Guild.guildId is int guildId && guildId > 0)
            {
                MiniGameHelper.InsertOrUpdate(req.ArcadeId, user.ID, guildId, newscore, 0);
            }
        }

        data.TotalAccumulatedScore += newscore;

        repo.SaveSingle(user.ID, MinigameRepository.TypeDessertRush, data);
        db.SaveChanges();

        return new ResClearArcadeDessertRush
        {
            Data = MiniGameHelper.ToProto<NetArcadeDessertRushData, ArcadeDessertRushData>(data)
        };
    }

    public ResGetArcadeDessertRushData GetDessertRush(ulong userId, int arcadeId)
    {
        var data = repo.GetSingle<ArcadeDessertRushData>(userId, MinigameRepository.TypeDessertRush);
        if (data.ArcadeId == 0)
        {
            data.ArcadeId = arcadeId;
            repo.SaveSingle(userId, MinigameRepository.TypeDessertRush, data);
            db.SaveChanges();
        }

        return new ResGetArcadeDessertRushData
        {
            Data = MiniGameHelper.ToProto<NetArcadeDessertRushData, ArcadeDessertRushData>(data)
        };
    }

    public ResGetArcadeDessertRushRanking GetDessertRushRanking(User user, int arcadeId)
    {
        var response = new ResGetArcadeDessertRushRanking
        {
            UserGuildRanking = new()
            {
                Rank = 0,
                Score = 0,
                User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
            }
        };

        if (user.Guild.guildId is int guildId && guildId > 0)
        {
            var allBoard = MiniGameHelper.GetFullLeaderboard(guildId, arcadeId);
            foreach (var item in allBoard)
            {
                if (TryCreateWholeUser(item.Record.UserId) is not { } wholeUser)
                    continue;

                var guild = new NetArcadeDessertRushRankingData
                {
                    Rank = item.Rank,
                    Score = item.Record.Score,
                    User = wholeUser
                };

                response.GuildRankingList.Add(guild);
                if (item.Record.UserId == user.ID)
                    response.UserGuildRanking = guild;
            }
        }

        return response;
    }

    public ResObtainArcadeDessertRushStepUpReward ObtainDessertRushStepUpReward(User user, int stepUpRewardId)
    {
        var data = repo.GetSingle<ArcadeDessertRushData>(user.ID, MinigameRepository.TypeDessertRush);
        data.StepUpRewardedList.AddUnique(stepUpRewardId);

        var response = new ResObtainArcadeDessertRushStepUpReward
        {
            Data = MiniGameHelper.ToProto<NetArcadeDessertRushData, ArcadeDessertRushData>(data)
        };

        var rewardRecord = GameData.Instance.EventDessertRushStepUpRewardTable.Values
            .FirstOrDefault(x => x.Id == stepUpRewardId);
        if (rewardRecord != null)
        {
            response.Reward = RewardUtils.RegisterRewardsForUser(user, rewardRecord.RewardId);
        }

        repo.SaveSingle(user.ID, MinigameRepository.TypeDessertRush, data);
        db.SaveChanges();
        UserSessionStore.SaveUser(user);

        return response;
    }

    // --- DragonDungeonRun ---

    public ResGetArcadeDragonDungeonRunAlbum GetDdrAlbum(ulong userId)
    {
        var ddr = repo.GetSingle<DragonDungeonRunData>(userId, MinigameRepository.TypeDdr);
        var response = new ResGetArcadeDragonDungeonRunAlbum();

        if (ddr.CutSceneList.Count > 0)
        {
            foreach (var item in ddr.CutSceneList.ToList())
            {
                response.CutSceneList.Add(new ResGetArcadeDragonDungeonRunAlbum.Types.NetArcadeDragonDungeonRunCutSceneData
                {
                    CutSceneId = item.Key,
                    IsNew = item.Value
                });
                ddr.CutSceneList[item.Key] = false;
            }

            ddr.HasUnconfirmedAlbum = false;
            repo.SaveSingle(userId, MinigameRepository.TypeDdr, ddr);
            db.SaveChanges();
        }

        return response;
    }

    public ResEnterArcadeDragonDungeonRunLobby EnterDdrLobby(User user)
    {
        var response = new ResEnterArcadeDragonDungeonRunLobby
        {
            UserRankingData = new()
            {
                User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
            }
        };

        var arcade = GetArcadeManager(ArcadeGameType.DragonDungeonRun);

        if (user.Guild.guildId is int guildId && guildId > 0)
        {
            var rankinfo = MiniGameHelper.GetGuildUserRank(guildId, user.ID, arcade.Id);
            if (rankinfo != null)
            {
                response.UserRankingData = new NetMiniGameDragonDungeonRunRankingData
                {
                    Rank = rankinfo.Value.Rank,
                    Score = (int)rankinfo.Value.Record.Score,
                    User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
                };
            }
        }

        var ddr = repo.GetSingle<DragonDungeonRunData>(user.ID, MinigameRepository.TypeDdr);
        if (InitDdrData(ddr, MiniGameSystemType.Arcade))
        {
            repo.SaveSingle(user.ID, MinigameRepository.TypeDdr, ddr);
            db.SaveChanges();
        }

        response.HasUnconfirmedAlbum = ddr.HasUnconfirmedAlbum;
        response.HasWatchedTutorial = ddr.HasWatchedTutorial;
        response.NewScenarioAvailable = ddr.NewScenarioAvailable;

        return response;
    }

    public ResGetArcadeDragonDungeonRunMissionProgress GetDdrMission(ulong userId)
    {
        var ddr = repo.GetSingle<DragonDungeonRunData>(userId, MinigameRepository.TypeDdr);
        var response = new ResGetArcadeDragonDungeonRunMissionProgress();

        var missiondata = MiniGameHelper
            .ToProtoDict<int, NetDragonDungeonRunMissionProgress, DragonDungeonRunMissionProgress>(ddr.MissionDatas);
        response.MissionProgressList.AddRange(missiondata.Values);
        response.RewardedMissionIdList.AddRange(ddr.RewardedMissionIdList);

        return response;
    }

    public ResAcquireArcadeDragonDungeonRunMissionReward AcquireDdrMission(
        User user,
        ReqAcquireArcadeDragonDungeonRunMissionReward req)
    {
        var ddr = repo.GetSingle<DragonDungeonRunData>(user.ID, MinigameRepository.TypeDdr);
        List<int> rewardlist = [];

        foreach (var item in req.MissionIdList)
        {
            ddr.RewardedMissionIdList.AddUnique(item);
            var mission = GameData.Instance.EventDragonDungeonRunMissionTable.Values.FirstOrDefault(x => x.Id == item);
            if (mission != null)
                rewardlist.Add(mission.RewardId);
        }

        var response = new ResAcquireArcadeDragonDungeonRunMissionReward
        {
            Reward = RewardUtils.RegisterRewardsForUserDou(user, rewardlist)
        };

        repo.SaveSingle(user.ID, MinigameRepository.TypeDdr, ddr);
        db.SaveChanges();
        UserSessionStore.SaveUser(user);

        return response;
    }

    public ResCompleteArcadeDragonDungeonRunStage CompleteDdrStage(
        User user,
        ReqCompleteArcadeDragonDungeonRunStage req)
    {
        var response = new ResCompleteArcadeDragonDungeonRunStage();
        var arcade = GetArcadeManager(ArcadeGameType.DragonDungeonRun);
        var ddr = repo.GetSingle<DragonDungeonRunData>(user.ID, MinigameRepository.TypeDdr);

        ddr.HasWatchedTutorial = true;
        ddr.LastPlayCharacter = req.CharacterId;
        ddr.TotalDistance += req.RealDistance;
        ddr.TotalGold += req.RealGold;
        ddr.TotalSkillCount += req.UseSkill;
        ddr.NewCharacters.Remove(req.CharacterId);

        if (req.RealDistance >= 150)
            ddr.IsPhase2Unlocked = true;
        else if (req.RealDistance >= 80)
            ddr.IsPhase1Unlocked = true;

        if (req.RealDistance > ddr.Distance)
        {
            ddr.Distance = req.RealDistance;
            response.IsNewRecord = true;
        }

        int score = (req.RealDistance + req.RealGold) * 100;

        if (score > ddr.Point)
        {
            ddr.Point = score;
            if (user.Guild.guildId is int guildId && guildId > 0)
            {
                MiniGameHelper.InsertOrUpdate(arcade.Id, user.ID, guildId, score, 0);
            }
        }

        MiniGameHelper.GetDungeonRankByScore(ddr, score);

        if (ddr.StatisticsDatas.TryGetValue(req.CharacterId, out var statisticsData))
        {
            statisticsData.CumulativeDistance += req.RealDistance;
            statisticsData.CumulativeGold += req.RealGold;
            statisticsData.PlayCount += 1;
            statisticsData.CumulativeSkillCount += req.UseSkill;

            if (score > statisticsData.Score)
            {
                statisticsData.Score = score;
                if (user.Guild.guildId is int guildId && guildId > 0)
                {
                    MiniGameHelper.InsertOrUpdate(arcade.Id, user.ID, guildId, score, req.CharacterId);
                }
            }

            statisticsData.DeadCountList[req.DeadCutScenePrintType] =
                statisticsData.DeadCountList.GetValueOrDefault(req.DeadCutScenePrintType) + 1;
        }
        else
        {
            var statistics = new DDRStatisticsData
            {
                Score = score,
                CumulativeDistance = req.RealDistance,
                CumulativeGold = req.RealGold,
                CumulativeSkillCount = req.UseSkill,
                PlayCount = 1
            };

            var deadlist = GameData.Instance.EventDragonDungeonRunCutSceneTable.Values
                .Where(x => x.CutscenePrintTiming == EventDragonDungeonRunCutScenePrintTiming.Dead)
                .ToList();

            foreach (var item in deadlist)
            {
                statistics.DeadCountList.TryAdd(item.Id, 0);
            }
            if (req.DeadCutScenePrintType > 0)
            {
                statistics.DeadCountList[req.DeadCutScenePrintType] =
                    statistics.DeadCountList.GetValueOrDefault(req.DeadCutScenePrintType) + 1;
            }

            ddr.StatisticsDatas.TryAdd(req.CharacterId, statistics);
        }

        if (ddr.CutSceneList.TryAdd(req.DeadCutScenePrintType, true))
        {
            ddr.HasUnconfirmedAlbum = true;
        }

        UpDdrMission(ddr, MiniGameSystemType.Arcade);
        AddDdrCharacterCheck(ddr, MiniGameSystemType.Arcade);

        response.NewCutSceneList.AddRange(ddr.CutSceneList
            .Where(x => x.Value)
            .Select(x => x.Key));

        var scelist = GameData.Instance.EventDragonDungeonRunScenarioTable.Values.ToList();
        int numb = ddr.StatisticsDatas.Values.Sum(x => x.PlayCount);
        if (numb > 0 && numb < scelist.Count)
        {
            response.NextScenarioGroupId = scelist[numb - 1].ScenarioDialogGroupId;
        }

        response.TotalPoint = score;

        repo.SaveSingle(user.ID, MinigameRepository.TypeDdr, ddr);
        db.SaveChanges();

        return response;
    }

    public ResEnterArcadeDragonDungeonRunStage EnterDdrStage(
        ulong userId,
        ReqEnterArcadeDragonDungeonRunStage req)
    {
        var ddr = repo.GetSingle<DragonDungeonRunData>(userId, MinigameRepository.TypeDdr);
        ddr.LastPlayCharacter = req.CharacterId;
        ddr.Phase = req.Phase;

        repo.SaveSingle(userId, MinigameRepository.TypeDdr, ddr);
        db.SaveChanges();

        return new ResEnterArcadeDragonDungeonRunStage();
    }

    public ResGetArcadeDragonDungeonRunStatistics GetDdrStatistics(ulong userId)
    {
        var ddr = repo.GetSingle<DragonDungeonRunData>(userId, MinigameRepository.TypeDdr);
        var response = new ResGetArcadeDragonDungeonRunStatistics();

        foreach (var item in ddr.StatisticsDatas)
        {
            var statistics = new ResGetArcadeDragonDungeonRunStatistics.Types.NetArcadeDragonDungeonRunStatistics
            {
                CharacterId = item.Key,
                CumulativeDistance = item.Value.CumulativeDistance,
                CumulativeGold = item.Value.CumulativeGold,
                MaxPoint = item.Value.Score,
                PlayCount = item.Value.PlayCount
            };

            if (item.Value.DeadCountList.Count > 0)
            {
                foreach (var deadcut in item.Value.DeadCountList)
                {
                    statistics.DeadCountList.Add(
                        new ResGetArcadeDragonDungeonRunStatistics.Types.NetArcadeDragonDungeonRunStatistics.Types.NetDeadCount
                        {
                            DeadCutScenePrintType = deadcut.Key,
                            Count = deadcut.Value
                        });
                }
            }

            response.StatisticsList.Add(statistics);
        }

        return response;
    }

    public ResGetArcadeDragonDungeonRunUnionRanking GetDdrUnionRanking(User user)
    {
        var response = new ResGetArcadeDragonDungeonRunUnionRanking();
        var arcade = GetArcadeManager(ArcadeGameType.DragonDungeonRun);

        if (user.Guild.guildId is int guildId && guildId > 0)
        {
            var allBoard = MiniGameHelper.GetFullLeaderboard(guildId, arcade.Id);
            foreach (var item in allBoard)
            {
                if (TryCreateWholeUser(item.Record.UserId) is not { } wholeUser)
                    continue;

                var guild = new NetMiniGameDragonDungeonRunRankingData
                {
                    Rank = item.Rank,
                    Score = (int)item.Record.Score,
                    User = wholeUser
                };

                response.RankingDataList.Add(guild);
                if (item.Record.UserId == user.ID)
                    response.UserRankingData = guild;
            }
        }

        return response;
    }

    public ResGetArcadeDragonDungeonRunUnlockState GetDdrUnlockState(ulong userId)
    {
        var ddr = repo.GetSingle<DragonDungeonRunData>(userId, MinigameRepository.TypeDdr);
        var response = new ResGetArcadeDragonDungeonRunUnlockState
        {
            IsPhase1Unlocked = ddr.IsPhase1Unlocked,
            IsPhase2Unlocked = ddr.IsPhase2Unlocked,
            LastPlayedCharacter = ddr.LastPlayCharacter
        };

        response.NewCharacterIdList.AddRange(ddr.NewCharacters);
        response.OwnedCharacterIdList.AddRange(ddr.Characters);

        return response;
    }

    private static bool InitDdrData(DragonDungeonRunData ddr, MiniGameSystemType arcade)
    {
        var ddrmanager = GameData.Instance.EventDragonDungeonRunManagerTable.Values
            .FirstOrDefault(x => x.MinigameType == arcade);
        if (ddrmanager == null)
            return false;

        bool mutated = false;

        if (ddr.Characters.Count == 0)
        {
            var firstchar = GameData.Instance.EventDragonDungeonRunCharacterTable.Values
                .Where(x => x.GroupId == ddrmanager.UseCharacterGroupId
                            && x.OpenCondition == EventDragonDungeonRunCharacterUnlockType.None)
                .Select(x => x.Id)
                .ToList();

            ddr.Characters.AddRangeUnique(firstchar);
            ddr.NewCharacters.AddRangeUnique(firstchar);
            ddr.LastPlayCharacter = firstchar.FirstOrDefault();
            mutated = true;
        }

        if (ddr.MissionDatas.Count == 0)
        {
            var missionlist = GameData.Instance.EventDragonDungeonRunMissionTable.Values
                .Where(x => x.GroupId == ddrmanager.MissionGroupId)
                .ToList();

            foreach (var item in missionlist)
            {
                ddr.MissionDatas.TryAdd(item.Id, new DragonDungeonRunMissionProgress
                {
                    MissionTargetId = item.MissionTargetId,
                    MissionType = (int)item.MissionType,
                    Progress = 0
                });
            }

            mutated = true;
        }

        return mutated;
    }

    private static void UpDdrMission(DragonDungeonRunData data, MiniGameSystemType arcade)
    {
        var ddrmanager = GameData.Instance.EventDragonDungeonRunManagerTable.Values
            .FirstOrDefault(x => x.MinigameType == arcade);
        if (ddrmanager == null)
            return;

        var mlist = GameData.Instance.EventDragonDungeonRunMissionTable.Values
            .Where(x => x.GroupId == ddrmanager.MissionGroupId)
            .ToList();

        foreach (EventDragonDungeonRunMissionType missionType in Enum.GetValues<EventDragonDungeonRunMissionType>())
        {
            switch (missionType)
            {
                case EventDragonDungeonRunMissionType.GainPoint:
                    foreach (var item in mlist.Where(x => x.MissionType == missionType))
                    {
                        if (!data.RewardedMissionIdList.Contains(item.Id)
                            && data.MissionDatas.TryGetValue(item.Id, out var value)
                            && data.Point >= value.MissionTargetId)
                        {
                            value.Progress += 1;
                        }
                    }
                    break;
                case EventDragonDungeonRunMissionType.GainGold:
                    foreach (var item in mlist.Where(x => x.MissionType == missionType))
                    {
                        if (!data.RewardedMissionIdList.Contains(item.Id)
                            && data.MissionDatas.TryGetValue(item.Id, out var value))
                        {
                            value.Progress = data.TotalGold;
                        }
                    }
                    break;
                case EventDragonDungeonRunMissionType.MoveDistance:
                    foreach (var item in mlist.Where(x => x.MissionType == missionType))
                    {
                        if (!data.RewardedMissionIdList.Contains(item.Id)
                            && data.MissionDatas.TryGetValue(item.Id, out var value))
                        {
                            value.Progress = data.TotalDistance;
                        }
                    }
                    break;
                case EventDragonDungeonRunMissionType.UseSkillCount:
                    foreach (var item in mlist.Where(x => x.MissionType == missionType))
                    {
                        if (!data.RewardedMissionIdList.Contains(item.Id)
                            && data.MissionDatas.TryGetValue(item.Id, out var value))
                        {
                            value.Progress = data.TotalSkillCount;
                        }
                    }
                    break;
                case EventDragonDungeonRunMissionType.PlayCharacterCount:
                    foreach (var item in mlist.Where(x => x.MissionType == missionType))
                    {
                        if (!data.RewardedMissionIdList.Contains(item.Id)
                            && data.MissionDatas.TryGetValue(item.Id, out var value)
                            && data.StatisticsDatas.TryGetValue(item.MissionTargetId, out var statistics))
                        {
                            value.Progress = statistics.PlayCount;
                        }
                    }
                    break;
            }
        }
    }

    private static void AddDdrCharacterCheck(DragonDungeonRunData data, MiniGameSystemType arcade)
    {
        var ddrmanager = GameData.Instance.EventDragonDungeonRunManagerTable.Values
            .FirstOrDefault(x => x.MinigameType == arcade);
        if (ddrmanager == null)
            return;

        foreach (EventDragonDungeonRunCharacterUnlockType unlockType in Enum.GetValues<EventDragonDungeonRunCharacterUnlockType>())
        {
            var chardata = GameData.Instance.EventDragonDungeonRunCharacterTable.Values
                .FirstOrDefault(x => x.GroupId == ddrmanager.UseCharacterGroupId && x.OpenCondition == unlockType);
            if (chardata == null)
                continue;

            bool unlocked = unlockType switch
            {
                EventDragonDungeonRunCharacterUnlockType.GainPoint => data.Point >= chardata.OpenConditionValue,
                EventDragonDungeonRunCharacterUnlockType.GainGold => data.TotalGold >= chardata.OpenConditionValue,
                EventDragonDungeonRunCharacterUnlockType.MoveDistance => data.TotalDistance >= chardata.OpenConditionValue,
                _ => false
            };

            if (unlocked)
            {
                data.NewCharacters.AddUnique(chardata.Id);
                data.Characters.AddUnique(chardata.Id);
            }
        }
    }

    // --- InTheMirror ---

    public ResAcquireArcadeMvgCollectable AcquireMvgCollectable(ulong userId, int collectableId)
    {
        var mvg = repo.GetSingle<NetArcadeMvgData>(userId, MinigameRepository.TypeArcadeInTheMirror);
        mvg.Collectables.Add(collectableId);

        repo.SaveSingle(userId, MinigameRepository.TypeArcadeInTheMirror, mvg);
        db.SaveChanges();

        return new ResAcquireArcadeMvgCollectable();
    }

    public ResGetArcadeMvgData GetMvgData(ulong userId)
        => new() { Data = repo.GetSingle<NetArcadeMvgData>(userId, MinigameRepository.TypeArcadeInTheMirror) };

    public ResSaveArcadeMvgData SaveMvgData(ulong userId, NetArcadeMvgData? data)
    {
        repo.SaveSingle(userId, MinigameRepository.TypeArcadeInTheMirror, data ?? new NetArcadeMvgData());
        db.SaveChanges();
        return new ResSaveArcadeMvgData();
    }

    public ResObtainArcadeMvgAchievementMissionReward ObtainMvgAchievementMissionReward(
        User user,
        ReqObtainArcadeMvgAchievementMissionReward req)
    {
        var mvg = repo.GetSingle<NetArcadeMvgData>(user.ID, MinigameRepository.TypeArcadeInTheMirror);
        List<NetRewardData> rewards = [];

        foreach (var missionId in req.MissionIds)
        {
            if (!GameData.Instance.EventMvgMissionTable.TryGetValue(missionId, out var mission))
                continue;

            var current = mvg.AchievementMissions.FirstOrDefault(m => m.MissionId == mission.Id);
            if (current != null)
                current.IsReceived = true;

            var achievementMission = GameData.Instance.EventMvgMissionTable
                .FirstOrDefault(m => m.Key > mission.Id && m.Value.ConditionType == EventMVGMissionConditionType.ClearAchievement);
            if (achievementMission.Value != null)
            {
                var next = mvg.AchievementMissions.FirstOrDefault(m => m.MissionId == achievementMission.Key);
                if (next != null)
                    next.Progress++;
            }

            rewards.Add(RewardUtils.RegisterRewardsForUser(user, mission.RewardId));
        }

        var response = new ResObtainArcadeMvgAchievementMissionReward
        {
            Reward = NetUtils.MergeRewards(rewards, user)
        };
        response.Missions.AddRange(mvg.AchievementMissions);

        repo.SaveSingle(user.ID, MinigameRepository.TypeArcadeInTheMirror, mvg);
        db.SaveChanges();
        UserSessionStore.SaveUser(user);

        return response;
    }

    public ResObtainArcadeMvgQuestReward ObtainMvgQuestReward(User user, ReqObtainArcadeMvgQuestReward req)
    {
        var mvg = repo.GetSingle<NetArcadeMvgData>(user.ID, MinigameRepository.TypeArcadeInTheMirror);
        List<NetRewardData> rewards = [];

        foreach (var questId in req.QuestIds)
        {
            if (!GameData.Instance.EventMvgQuestTable.TryGetValue(questId, out var quest) || quest.Id == 0)
                continue;

            var entry = mvg.Quests.FirstOrDefault(q => q.QuestId == quest.Id);
            if (entry != null)
                entry.IsReceived = true;

            rewards.Add(RewardUtils.RegisterRewardsForUser(user, quest.RewardId));
        }

        repo.SaveSingle(user.ID, MinigameRepository.TypeArcadeInTheMirror, mvg);
        db.SaveChanges();
        UserSessionStore.SaveUser(user);

        return new ResObtainArcadeMvgQuestReward
        {
            Reward = NetUtils.MergeRewards(rewards, user)
        };
    }

    public ResStartArcadeMvgNewGame StartMvgNewGame(ulong userId)
    {
        var mvg = repo.GetSingle<NetArcadeMvgData>(userId, MinigameRepository.TypeArcadeInTheMirror);
        mvg.Gold = 0;
        mvg.Core = 0;
        mvg.Quests.Clear();
        mvg.Collectables.Clear();
        mvg.ProgressJson = "";

        repo.SaveSingle(userId, MinigameRepository.TypeArcadeInTheMirror, mvg);
        db.SaveChanges();

        return new ResStartArcadeMvgNewGame();
    }

    public ResStartArcadeMvgNewGamePlus StartMvgNewGamePlus(ulong userId, string progressJson)
    {
        var mvg = repo.GetSingle<NetArcadeMvgData>(userId, MinigameRepository.TypeArcadeInTheMirror);

        foreach (var questData in mvg.Quests.Where(q => !q.IsReceived))
        {
            if (!GameData.Instance.EventMvgQuestTable.TryGetValue(questData.QuestId, out var quest)
                || quest.RewardId == 0)
                continue;

            if (!GameData.Instance.RewardDataRecords.TryGetValue(quest.RewardId, out var rewardRecord)
                || rewardRecord.Rewards == null)
                continue;

            foreach (var rewardEntry in rewardRecord.Rewards)
            {
                if (rewardEntry.RewardType == RewardType.None)
                    continue;

                switch (rewardEntry.RewardId)
                {
                    case 9810003:
                        mvg.Gold += rewardEntry.RewardValue;
                        break;
                    case 9811001:
                        mvg.Core += rewardEntry.RewardValue;
                        break;
                }
            }
        }

        foreach (var quest in mvg.Quests)
        {
            quest.Progress = default;
            quest.IsReceived = default;
        }

        mvg.ProgressJson = progressJson;

        repo.SaveSingle(userId, MinigameRepository.TypeArcadeInTheMirror, mvg);
        db.SaveChanges();

        return new ResStartArcadeMvgNewGamePlus();
    }

    // --- Shared Helpers ---

    private static ArcadeManagerRecord_Raw GetArcadeManager(ArcadeGameType gameType)
        => GameData.Instance.ArcadeManagerTable.Values.FirstOrDefault(x => x.GameType == gameType)
           ?? throw new InvalidOperationException($"ArcadeManagerTable entry for {gameType} not found.");

    private static NetWholeUserData? TryCreateWholeUser(ulong userId)
        => UserSessionStore.GetUser(userId) is { } member
            ? LobbyHandler.CreateWholeUserDataFromDbUser(member)
            : null;
}
