using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer;
using EpinelPS.LobbyServer.Minigame;
using EpinelPS.LobbyServer.Minigame.TTS;
using EpinelPS.Models;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.Services;

public sealed class MinigameArcadeService(GameContext db, MinigameRepository repo)
{
    // --- Shared Helpers ---

    private void PopulateGuildLeaderboard<TRank>(
        User user,
        int arcadeId,
        int modeId,
        Google.Protobuf.Collections.RepeatedField<TRank> rankingList,
        Action<TRank> setUserRank,
        Func<int, long, NetWholeUserData, TRank> createEntry)
    {
        if (user.Guild.guildId is not > 0)
            return;

        long guildId = user.Guild.guildId.Value;
        foreach (var item in MiniGameHelper.GetFullLeaderboard(guildId, arcadeId, modeId))
        {
            if (UserSessionStore.GetUser(item.Record.UserId) is not User rankedUser)
                continue;

            var entry = createEntry(item.Rank, item.Record.Score, LobbyHandler.CreateWholeUserDataFromDbUser(rankedUser));
            rankingList.Add(entry);
            if (item.Record.UserId == user.ID)
                setUserRank(entry);
        }
    }

    // --- nksv2 ---

    private static Nksv2Data CreateInitialNksv2Data(int nksId, string? progressJson = null)
    {
        var data = new Nksv2Data();
        if (progressJson != null)
            data.ProgressJson = progressJson;

        var missions = GameData.Instance.EventNKSMissionTable.Values
            .Where(x => x.CommonSettingsID == nksId);

        foreach (var item in missions)
        {
            data.MissionProgressData.TryAdd(item.Id, new MiniGameNKSV2MissionProgress
            {
                NKsMissionId = item.Id,
                CreatedAt = DateTime.UtcNow.ToTimestamp(),
                Progress = 0,
                Seq = User.GenerateMsn()
            });
        }

        return data;
    }

    public ResGetMiniGameNKSV2Data GetNksv2(ulong userId, ReqGetMiniGameNKSV2Data req)
    {
        var response = new ResGetMiniGameNKSV2Data();

        if (!repo.TryGetDictEntry<Nksv2Data>(userId, MinigameRepository.TypeNksv2, req.NKsId, out var nksv2Data) || nksv2Data == null)
        {
            nksv2Data = CreateInitialNksv2Data(req.NKsId);
            repo.SaveDictEntry(userId, MinigameRepository.TypeNksv2, req.NKsId, nksv2Data);
            db.SaveChanges();
        }
        else if (nksv2Data.ProgressJson != null)
        {
            response.ProgressJson = nksv2Data.ProgressJson;
        }

        var missprodata = MiniGameHelper
            .ToProtoDict<int, NetMiniGameNKSV2MissionProgress, MiniGameNKSV2MissionProgress>(nksv2Data.MissionProgressData);
        response.MissionProgressList.AddRange(missprodata.Values);

        return response;
    }

    public ResGetNKSV2Scenario GetNksv2Scenario(ulong userId, ReqGetNKSV2Scenario req)
    {
        var response = new ResGetNKSV2Scenario();

        if (repo.TryGetDictEntry<Nksv2Data>(userId, MinigameRepository.TypeNksv2, req.NKsId, out var nksv2Data) && nksv2Data != null)
        {
            response.ScenarioIdList.AddRange(nksv2Data.CompletedScenarios);
        }
        else
        {
            var created = CreateInitialNksv2Data(req.NKsId, progressJson: "");
            repo.SaveDictEntry(userId, MinigameRepository.TypeNksv2, req.NKsId, created);
            db.SaveChanges();
        }

        return response;
    }

    public ResCompleteNKSV2Scenario CompleteNksv2Scenario(ulong userId, ReqCompleteNKSV2Scenario req)
    {
        if (repo.TryGetDictEntry<Nksv2Data>(userId, MinigameRepository.TypeNksv2, req.NKsId, out var nksv2Data) && nksv2Data != null)
        {
            nksv2Data.CompletedScenarios.AddUnique(req.ScenarioId);
            repo.SaveDictEntry(userId, MinigameRepository.TypeNksv2, req.NKsId, nksv2Data);
            db.SaveChanges();
        }

        return new ResCompleteNKSV2Scenario();
    }

    public ResEnterMiniGameNKSV2Stage EnterNksv2Stage(User user)
    {
        user.AddTrigger(Trigger.EventMiniGameNKSPlayCheck, 1, 0);
        return new ResEnterMiniGameNKSV2Stage();
    }

    public ResUploadMiniGameNKSV2Progress UploadNksv2Progress(ulong userId, ReqUploadMiniGameNKSV2Progress req)
    {
        var response = new ResUploadMiniGameNKSV2Progress { BanResult = MiniGameBanResult.Success };

        if (repo.TryGetDictEntry<Nksv2Data>(userId, MinigameRepository.TypeNksv2, req.NKsId, out var nksv2Data) && nksv2Data != null)
        {
            nksv2Data.ProgressJson = req.ProgressJson;
            repo.SaveDictEntry(userId, MinigameRepository.TypeNksv2, req.NKsId, nksv2Data);
            db.SaveChanges();

            response.BanResult = MiniGameBanResult.Success;
            response.Error = NKSV2MissionExpiredError.Succeed;
        }

        return response;
    }

    public ResUpdateNKSV2MissionProgress UpdateNksv2MissionProgress(ulong userId, ReqUpdateNKSV2MissionProgress req)
    {
        var response = new ResUpdateNKSV2MissionProgress();

        if (repo.TryGetDictEntry<Nksv2Data>(userId, MinigameRepository.TypeNksv2, req.NKsId, out var nksv2Data) && nksv2Data != null)
        {
            foreach (var item in req.ProgressUpdateList)
            {
                var newdata = nksv2Data.MissionProgressData.Values.FirstOrDefault(x => x.Seq == item.SeqId);
                if (newdata != null)
                    newdata.Progress = item.ProgressCount;
            }

            repo.SaveDictEntry(userId, MinigameRepository.TypeNksv2, req.NKsId, nksv2Data);
            db.SaveChanges();

            var missprodata = MiniGameHelper
                .ToProtoDict<int, NetMiniGameNKSV2MissionProgress, MiniGameNKSV2MissionProgress>(nksv2Data.MissionProgressData);
            response.MissionProgressList.AddRange(missprodata.Values);
            response.Error = NKSV2MissionExpiredError.Succeed;
        }

        return response;
    }

    public ResCompleteNKSV2Mission CompleteNksv2Mission(User user, ReqCompleteNKSV2Mission req)
    {
        var response = new ResCompleteNKSV2Mission();
        NetRewardData ret = new();
        List<int> rewardlist = [];

        if (repo.TryGetDictEntry<Nksv2Data>(user.ID, MinigameRepository.TypeNksv2, req.NKsId, out var nksv2Data) && nksv2Data != null)
        {
            foreach (var item in req.NKsMissionSeqIdList)
            {
                var newdata = nksv2Data.MissionProgressData.Values.FirstOrDefault(x => x.Seq == item);
                if (newdata != null)
                {
                    newdata.ReceivedAt = DateTime.UtcNow.ToTimestamp();

                    var reward = GameData.Instance.EventNKSMissionTable.Values
                        .FirstOrDefault(x => x.Id == newdata.NKsMissionId);
                    if (reward != null && reward.RewardType == EventNKSMissionRewardTypeData.Item)
                        rewardlist.Add(reward.RewardValue);
                }
            }

            nksv2Data.ProgressJson = req.ProgressJsonAfterComplete;
            repo.SaveDictEntry(user.ID, MinigameRepository.TypeNksv2, req.NKsId, nksv2Data);
            db.SaveChanges();

            if (rewardlist.Count > 0)
            {
                ret = RewardUtils.RegisterRewardsForUserDou(user, rewardlist);
                UserSessionStore.SaveUser(user);
            }

            var missprodata = MiniGameHelper
                .ToProtoDict<int, NetMiniGameNKSV2MissionProgress, MiniGameNKSV2MissionProgress>(nksv2Data.MissionProgressData);
            response.MissionProgressList.AddRange(missprodata.Values);
            response.Error = NKSV2MissionExpiredError.Succeed;
            response.BanResult = MiniGameBanResult.Success;
            response.Reward = ret;
        }

        return response;
    }

    public ResAccumulateMiniGameNKSV2RankingScore AccumulateNksv2RankingScore(User user, ReqAccumulateMiniGameNKSV2RankingScore req)
    {
        var arcade = GameData.Instance.ArcadeManagerTable.Values
            .FirstOrDefault(x => x.GameType == ArcadeGameType.NKSMiniGame);

        if (repo.TryGetDictEntry<Nksv2Data>(user.ID, MinigameRepository.TypeNksv2, req.NKsId, out var nksv2Data) && nksv2Data != null)
        {
            if (req.Score > nksv2Data.Score)
            {
                nksv2Data.Score = req.Score;
                repo.SaveDictEntry(user.ID, MinigameRepository.TypeNksv2, req.NKsId, nksv2Data);
                db.SaveChanges();

                if (user.Guild.guildId > 0 && arcade != null)
                    MiniGameHelper.InsertOrUpdate(arcade.Id, user.ID, user.Guild.guildId.Value, (int)req.Score, req.NKsId);
            }
        }

        return new ResAccumulateMiniGameNKSV2RankingScore();
    }

    public ResGetMiniGameNKSV2Ranking GetNksv2Ranking(User user, ReqGetMiniGameNKSV2Ranking req)
    {
        var response = new ResGetMiniGameNKSV2Ranking
        {
            UserGuildRanking = new()
            {
                Rank = 0,
                Score = 0,
                User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
            }
        };

        var arcade = GameData.Instance.ArcadeManagerTable.Values
            .FirstOrDefault(x => x.GameType == ArcadeGameType.NKSMiniGame);

        if (arcade != null)
        {
            PopulateGuildLeaderboard(
                user,
                arcade.Id,
                req.NKsId,
                response.GuildRankingList,
                entry => response.UserGuildRanking = entry,
                (rank, score, wholeUser) => new NetMiniGameNKSV2RankingData
                {
                    Rank = rank,
                    Score = (int)score,
                    User = wholeUser
                });
        }

        return response;
    }

    // --- PirateCafe ---

    public ResGetArcadePirateCafeData GetPirateCafe(ulong userId, ReqGetArcadePirateCafeData req)
    {
        var data = repo.GetSingle<ArcadePirateCafeData>(userId, MinigameRepository.TypePirateCafe);
        if (data.ArcadeId == 0)
        {
            data.ArcadeId = req.ArcadeId;
            repo.SaveSingle(userId, MinigameRepository.TypePirateCafe, data);
            db.SaveChanges();
        }

        return new ResGetArcadePirateCafeData
        {
            Data = MiniGameHelper.ToProto<NetArcadePirateCafeData, ArcadePirateCafeData>(data)
        };
    }

    public ResClearArcadePirateCafe ClearPirateCafe(User user, ReqClearArcadePirateCafe req)
    {
        var data = repo.GetSingle<ArcadePirateCafeData>(user.ID, MinigameRepository.TypePirateCafe);
        int score = req.Score;

        if (score > data.HighScore)
        {
            data.HighScore = score;
            if (user.Guild.guildId > 0)
                MiniGameHelper.InsertOrUpdate(req.ArcadeId, user.ID, user.Guild.guildId.Value, score, 0);
        }

        data.TotalAccumulatedScore += score;
        repo.SaveSingle(user.ID, MinigameRepository.TypePirateCafe, data);
        db.SaveChanges();

        return new ResClearArcadePirateCafe
        {
            Data = MiniGameHelper.ToProto<NetArcadePirateCafeData, ArcadePirateCafeData>(data)
        };
    }

    public ResLevelUpArcadePirateCafeSkill LevelUpPirateCafe(ulong userId, ReqLevelUpArcadePirateCafeSkill req)
    {
        var data = repo.GetSingle<ArcadePirateCafeData>(userId, MinigameRepository.TypePirateCafe);
        data.SkillLevel = req.SkillLevel;
        repo.SaveSingle(userId, MinigameRepository.TypePirateCafe, data);
        db.SaveChanges();

        return new ResLevelUpArcadePirateCafeSkill
        {
            Data = MiniGameHelper.ToProto<NetArcadePirateCafeData, ArcadePirateCafeData>(data)
        };
    }

    public ResObtainArcadePirateCafeMissionReward ObtainPirateCafeMissionReward(User user, ReqObtainArcadePirateCafeMissionReward req)
    {
        var data = repo.GetSingle<ArcadePirateCafeData>(user.ID, MinigameRepository.TypePirateCafe);
        List<int> rewardlist = [];
        foreach (var item in req.MissionIdList)
        {
            var mission = GameData.Instance.EventPirateCafeMissionTable.Values.FirstOrDefault(x => x.Id == item);
            if (mission != null)
                rewardlist.Add(mission.RewardId);
        }

        NetRewardData ret = RewardUtils.RegisterRewardsForUserDou(user, rewardlist);
        data.MissionRewardedList.AddRange(req.MissionIdList);
        repo.SaveSingle(user.ID, MinigameRepository.TypePirateCafe, data);
        db.SaveChanges();

        if (rewardlist.Count > 0)
            UserSessionStore.SaveUser(user);

        return new ResObtainArcadePirateCafeMissionReward
        {
            Reward = ret,
            Data = MiniGameHelper.ToProto<NetArcadePirateCafeData, ArcadePirateCafeData>(data)
        };
    }

    public ResGetArcadePirateCafeRanking GetPirateCafeRanking(User user, ReqGetArcadePirateCafeRanking req)
    {
        var response = new ResGetArcadePirateCafeRanking
        {
            UserGuildRanking = new()
            {
                Rank = 0,
                Score = 0,
                User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
            }
        };

        PopulateGuildLeaderboard(
            user,
            req.ArcadeId,
            0,
            response.GuildRankingList,
            entry => response.UserGuildRanking = entry,
            (rank, score, wholeUser) => new NetMiniGamePirateCafeRankingData
            {
                Rank = rank,
                Score = score,
                User = wholeUser
            });

        return response;
    }

    // --- PlaySoda ---

    private static int[] GetPlaySodaPointValues(int challengeStageId)
    {
        if (challengeStageId == (int)EventPlaySodaGameType.CatchCoin)
            return [300000, 600000, 1000000];
        if (challengeStageId == (int)EventPlaySodaGameType.Smash)
            return [120000, 240000, 400000];
        return [360000, 720000, 1200000];
    }

    private List<NetPlaySodaEachGameInfo> GetOrInitArcadePlaySodaInfoList(ulong userId)
    {
        var list = repo.GetSingle<List<NetPlaySodaEachGameInfo>>(userId, MinigameRepository.TypeArcadePlaySoda);
        bool changed = false;

        foreach (int stageId in GameData.Instance.EventPlaySodaChallengeModeTable.Keys)
        {
            if (list.Count < GameData.Instance.EventPlaySodaChallengeModeTable.Count &&
                !list.Any(x => x.ChallengeStageId == stageId))
            {
                list.Add(new NetPlaySodaEachGameInfo
                {
                    AccumulatedScore = 0,
                    CanReceivePointReward = false,
                    ChallengeStageId = stageId,
                    LastRewardStep = 0,
                    IsInGuild = false,
                    UserRank = 0,
                    UserMaxScoreInUnion = 0
                });
                changed = true;
            }
        }

        if (changed)
        {
            repo.SaveSingle(userId, MinigameRepository.TypeArcadePlaySoda, list);
            db.SaveChanges();
        }

        return list;
    }

    private void FinishPlaySodaChallenge(ulong userId, EventPlaySodaGameType gameType, int score)
    {
        var challengeStageId = GameData.Instance.EventPlaySodaChallengeModeTable
            .First(i => i.Value.GameType == gameType).Key;
        var list = GetOrInitArcadePlaySodaInfoList(userId);
        var info = list.First(i => i.ChallengeStageId == challengeStageId);

        if (info.UserRank < score)
            info.UserRank = score;

        info.AccumulatedScore += score;

        var pointValues = GetPlaySodaPointValues(info.ChallengeStageId);
        if (pointValues.Length > info.LastRewardStep && info.AccumulatedScore >= pointValues[info.LastRewardStep])
            info.CanReceivePointReward = true;

        repo.SaveSingle(userId, MinigameRepository.TypeArcadePlaySoda, list);
        db.SaveChanges();
    }

    public ResGetArcadePlaySodaChallengeModeInfo GetPlaySodaChallengeInfo(User user)
    {
        var infoList = GetOrInitArcadePlaySodaInfoList(user.ID);
        var response = new ResGetArcadePlaySodaChallengeModeInfo
        {
            WholeUser = LobbyHandler.CreateWholeUserDataFromDbUser(user)
        };
        response.ArcadePlaySodaEachGameInfoList.AddRange(infoList);
        return response;
    }

    public ResEnterArcadePlaySodaChallengeStage EnterPlaySodaChallenge(ulong userId, ReqEnterArcadePlaySodaChallengeStage req)
    {
        var list = GetOrInitArcadePlaySodaInfoList(userId);
        return new ResEnterArcadePlaySodaChallengeStage
        {
            UserMaxScore = list.First(i => i.ChallengeStageId == req.ChallengeStageId).UserRank
        };
    }

    public ResFinishArcadePlaySodaCatchCoinChallenge FinishPlaySodaCatchCoin(ulong userId, ReqFinishArcadePlaySodaCatchCoinChallenge req)
    {
        FinishPlaySodaChallenge(userId, EventPlaySodaGameType.CatchCoin, req.Score);
        return new ResFinishArcadePlaySodaCatchCoinChallenge();
    }

    public ResFinishArcadePlaySodaClimbingChallenge FinishPlaySodaClimbing(ulong userId, ReqFinishArcadePlaySodaClimbingChallenge req)
    {
        FinishPlaySodaChallenge(userId, EventPlaySodaGameType.Climbing, req.Score);
        return new ResFinishArcadePlaySodaClimbingChallenge();
    }

    public ResFinishArcadePlaySodaRunningChallenge FinishPlaySodaRunning(ulong userId, ReqFinishArcadePlaySodaRunningChallenge req)
    {
        FinishPlaySodaChallenge(userId, EventPlaySodaGameType.Running, req.Score);
        return new ResFinishArcadePlaySodaRunningChallenge();
    }

    public ResFinishArcadePlaySodaSmashChallenge FinishPlaySodaSmash(ulong userId, ReqFinishArcadePlaySodaSmashChallenge req)
    {
        FinishPlaySodaChallenge(userId, EventPlaySodaGameType.Smash, req.Score);
        return new ResFinishArcadePlaySodaSmashChallenge();
    }

    public ResFinishArcadePlaySodaStackUpChallenge FinishPlaySodaStackUp(ulong userId, ReqFinishArcadePlaySodaStackUpChallenge req)
    {
        FinishPlaySodaChallenge(userId, EventPlaySodaGameType.StackUp, req.Score);
        return new ResFinishArcadePlaySodaStackUpChallenge();
    }

    public ResObtainArcadePlaySodaPointReward ObtainPlaySodaPointReward(User user, ReqObtainArcadePlaySodaPointReward req)
    {
        var list = GetOrInitArcadePlaySodaInfoList(user.ID);
        var info = list.First(i => i.ChallengeStageId == req.ChallengeStageId);
        info.CanReceivePointReward = false;

        List<NetRewardData> rewards = [];
        var pointValues = GetPlaySodaPointValues(info.ChallengeStageId);

        while (pointValues.Length > info.LastRewardStep && info.AccumulatedScore >= pointValues[info.LastRewardStep])
        {
            info.LastRewardStep++;
            int rewardId = GameData.Instance.EventPlaySodaPointRewardTable
                .First(r => (int)r.Value.GameType == info.ChallengeStageId &&
                            r.Value.Step == info.LastRewardStep &&
                            r.Value.PointValue == pointValues[info.LastRewardStep - 1])
                .Value.RewardId;
            rewards.Add(RewardUtils.RegisterRewardsForUser(user, rewardId));
        }

        repo.SaveSingle(user.ID, MinigameRepository.TypeArcadePlaySoda, list);
        db.SaveChanges();

        if (rewards.Count > 0)
            UserSessionStore.SaveUser(user);

        return new ResObtainArcadePlaySodaPointReward
        {
            LastRewardStep = info.LastRewardStep,
            Reward = NetUtils.MergeRewards(rewards, user)
        };
    }

    // --- RebuildEden ---

    private static EventRebuildEdenManagerRecord_Raw? GetRebuildEdenManager() =>
        GameData.Instance.EventRebuildEdenManagerTable.Values
            .FirstOrDefault(x => x.MinigameType == MiniGameSystemType.Arcade);

    public ResGetArcadeRebuildEdenData GetRebuildEden(ulong userId)
    {
        var response = new ResGetArcadeRebuildEdenData();
        var manager = GetRebuildEdenManager();
        if (manager == null)
            return response;

        if (!repo.TryGetDictEntry<RebuildEdenData>(userId, MinigameRepository.TypeRebuildEden, manager.Id, out var rebuildEden) || rebuildEden == null)
        {
            rebuildEden = new RebuildEdenData
            {
                FirstEnteredAt = DateTime.UtcNow.Date.ToTimestamp()
            };

            var missionlist = GameData.Instance.EventRebuildEdenMissionTable.Values
                .Where(x => x.EventGroup == manager.EventGroup);
            foreach (var item in missionlist)
            {
                rebuildEden.MissionDatas.TryAdd(item.Id, new RebuildEdenMissionData
                {
                    MissionId = item.Id,
                    IsReceived = false,
                    Progress = 0
                });
            }

            repo.SaveDictEntry(userId, MinigameRepository.TypeRebuildEden, manager.Id, rebuildEden);
            db.SaveChanges();
        }

        var missdata = MiniGameHelper
            .ToProtoDict<int, NetRebuildEdenMissionData, RebuildEdenMissionData>(rebuildEden.MissionDatas);
        response.AchievementMissions.AddRange(missdata.Values);
        response.Data = MiniGameHelper.ToProto<NetRebuildEdenData, EdenData>(rebuildEden.Data);
        response.FirstEnteredAt = rebuildEden.FirstEnteredAt;

        return response;
    }

    public ResSaveRebuildEdenData SaveRebuildEden(ulong userId, ReqSaveRebuildEdenData req)
    {
        var response = new ResSaveRebuildEdenData();
        var manager = GetRebuildEdenManager();
        if (manager == null)
            return response;

        if (repo.TryGetDictEntry<RebuildEdenData>(userId, MinigameRepository.TypeRebuildEden, manager.Id, out var rebuildEden) && rebuildEden != null)
        {
            rebuildEden.Data = MiniGameHelper.FromProto<EdenData, NetRebuildEdenData>(req.Data);
            foreach (var item in req.DailyMissions)
                rebuildEden.DailyMissions[item.MissionId] = item.Progress;

            repo.SaveDictEntry(userId, MinigameRepository.TypeRebuildEden, manager.Id, rebuildEden);
            db.SaveChanges();

            response.Result = MissionActionResult.Success;
        }

        return response;
    }

    public ResObtainRebuildEdenAchievementMissionReward ObtainRebuildEdenAchievementMissionReward(
        User user, ReqObtainRebuildEdenAchievementMissionReward req)
    {
        var response = new ResObtainRebuildEdenAchievementMissionReward();
        var manager = GetRebuildEdenManager();
        if (manager == null)
            return response;

        if (repo.TryGetDictEntry<RebuildEdenData>(user.ID, MinigameRepository.TypeRebuildEden, manager.Id, out var rebuildEden) && rebuildEden != null)
        {
            List<int> rewardlist = [];
            foreach (var item in req.MissionIds)
            {
                var mission = GameData.Instance.EventRebuildEdenMissionTable.Values.FirstOrDefault(x => x.Id == item);
                if (mission != null)
                    rewardlist.Add(mission.RewardId);

                if (rebuildEden.MissionDatas.TryGetValue(item, out var missionData))
                    missionData.IsReceived = true;
            }

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeRebuildEden, manager.Id, rebuildEden);
            db.SaveChanges();

            response.Reward = RewardUtils.RegisterRewardsForUserDou(user, rewardlist);
            if (rewardlist.Count > 0)
                UserSessionStore.SaveUser(user);
        }

        return response;
    }

    // --- SortOut ---

    public ResGetArcadeSortOut GetSortOut(ulong userId)
    {
        var data = repo.GetSingle<ArcadeSortOutData>(userId, MinigameRepository.TypeSortOut);
        var response = new ResGetArcadeSortOut
        {
            HighScore = data.HighScore,
            TotalAccumulatedScore = data.TotalAccumulatedScore
        };
        response.AccumulatedBoxSortOutCounts.AddRange(
            MiniGameHelper.ToProtoList<NetBoxSortOutCount, BoxSortOutCount>(data.AccumulatedBoxSortOutCounts));
        response.MissionRewarded.AddRange(data.MissionRewarded);
        return response;
    }

    public ResArcadeSortOutResult SubmitSortOutResult(User user, ReqArcadeSortOutResult req)
    {
        var data = repo.GetSingle<ArcadeSortOutData>(user.ID, MinigameRepository.TypeSortOut);

        foreach (var item in req.PlayInfo.BoxSortOutCounts)
        {
            var box = data.AccumulatedBoxSortOutCounts.FirstOrDefault(x => x.BoxId == item.BoxId);
            if (box != null)
            {
                box.Count += item.Count;
            }
            else
            {
                data.AccumulatedBoxSortOutCounts.Add(new BoxSortOutCount
                {
                    BoxId = item.BoxId,
                    Count = item.Count
                });
            }
        }

        int newscore = req.Point;
        if (newscore > data.HighScore)
        {
            data.HighScore = newscore;
            if (user.Guild.guildId > 0)
                MiniGameHelper.InsertOrUpdate(req.ArcadeId, user.ID, user.Guild.guildId.Value, newscore, 0);

            user.AddTrigger(Trigger.EventSortOutPointMax, newscore, 0);
        }

        data.TotalAccumulatedScore += newscore;
        repo.SaveSingle(user.ID, MinigameRepository.TypeSortOut, data);
        db.SaveChanges();

        user.AddTrigger(Trigger.EventSortOutClear, 1, 0);
        return new ResArcadeSortOutResult();
    }

    public ResCompleteArcadeSortOutMission CompleteSortOutMission(User user, ReqCompleteArcadeSortOutMission req)
    {
        var data = repo.GetSingle<ArcadeSortOutData>(user.ID, MinigameRepository.TypeSortOut);
        List<int> rewardlist = [];
        foreach (var item in req.MissionIds)
        {
            var mission = GameData.Instance.EventSortOutMissionTable.Values.FirstOrDefault(x => x.Id == item);
            if (mission != null)
                rewardlist.Add(mission.RewardId);
        }

        NetRewardData ret = RewardUtils.RegisterRewardsForUserDou(user, rewardlist);
        data.MissionRewarded.AddRangeUnique(req.MissionIds);
        repo.SaveSingle(user.ID, MinigameRepository.TypeSortOut, data);
        db.SaveChanges();

        if (rewardlist.Count > 0)
            UserSessionStore.SaveUser(user);

        return new ResCompleteArcadeSortOutMission { Reward = ret };
    }

    public ResGetArcadeSortOutRanking GetSortOutRanking(User user, ReqGetArcadeSortOutRanking req)
    {
        var response = new ResGetArcadeSortOutRanking
        {
            UserGuildRanking = new()
            {
                Rank = 0,
                Score = 0,
                User = LobbyHandler.CreateWholeUserDataFromDbUser(user)
            }
        };

        PopulateGuildLeaderboard(
            user,
            req.ArcadeId,
            0,
            response.GuildRankingList,
            entry => response.UserGuildRanking = entry,
            (rank, score, wholeUser) => new NetArcadeSortOutRankingData
            {
                Rank = rank,
                Score = score,
                User = wholeUser
            });

        return response;
    }

    // --- StellarBlade ---

    private static EventSBManagerRecord_Raw? GetArcadeStellarBladeManager() =>
        GameData.Instance.EventSBManagerTable.Values.FirstOrDefault(s => s.MinigameType == MiniGameSystemType.Arcade);

    private static int ComputeStellarBladeScore(
        int dealtDamage, int damageTaken, int perfectDodgeCount, int perfectGuardCount, int potionUsedCount) =>
        dealtDamage * 4 - damageTaken + 5000 * perfectDodgeCount + 10000 * perfectGuardCount - 1000 * potionUsedCount;

    private static void UpdateStellarBladeStatistics(
        StellarBladeDatas stellar, int stageId, Duration duration, long damageTaken,
        int perfectDodgeCount, int perfectGuardCount, int potionUsedCount, bool incrementKillCount)
    {
        if (stellar.StatisticsData.TryGetValue(stageId, out var netStatistics))
        {
            if (incrementKillCount)
                netStatistics.TotalKillCount += 1;
            netStatistics.TotalPerfectDodgeCount += perfectDodgeCount;
            netStatistics.TotalPerfectGuardCount += perfectGuardCount;
            netStatistics.TotalDamageTaken += damageTaken;
            netStatistics.TotalPotionUsedCount += potionUsedCount;
            netStatistics.MinDuration = netStatistics.MinDuration.Min(duration);
        }
        else
        {
            stellar.StatisticsData.TryAdd(stageId, new StatisticsData
            {
                StageId = stageId,
                MinDuration = duration,
                TotalDamageTaken = damageTaken,
                TotalKillCount = incrementKillCount ? 1 : 0,
                TotalPerfectDodgeCount = perfectDodgeCount,
                TotalPerfectGuardCount = perfectGuardCount,
                TotalPotionUsedCount = potionUsedCount
            });
        }
    }

    public ResArcadeGetStellarBladeMissionData GetStellarBladeMission(ulong userId, ReqArcadeGetStellarBladeMissionData req)
    {
        var stellar = MiniGameHelper.InitStellarBladeData(userId, req.ArcadeManagerId, repo, db);
        var response = new ResArcadeGetStellarBladeMissionData();
        response.AchievementMissionDataList.AddRange(
            MiniGameHelper.ToProtoList<NetStellarBladeMissionData, StellarBladeMissionData>(stellar.MissionData));
        return response;
    }

    public ResArcadeEnterStellarBladeTitle EnterStellarBladeTitle(ulong userId, ReqArcadeEnterStellarBladeTitle req)
    {
        var response = new ResArcadeEnterStellarBladeTitle();
        NetMiniGameStellarBladeRankingData? rank = MiniGameHelper.GetSBUserRank(userId, req.ArcadeManagerId);

        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            response.UserRankingData = rank ?? new NetMiniGameStellarBladeRankingData
            {
                Rank = 0,
                Score = 0,
                User = LobbyHandler.CreateWholeUserDataFromDbUser(userId)
            };
            response.TutorialListTableIds.AddRange(stellar.TutorialList);
            response.SbItemIdList.AddRange(stellar.SbItemIdList);
            response.LastEnteredStageId = stellar.LastEnteredStageId;
            response.CharacterData = stellar.CharacterData.ToProto();
            response.CurrencyList.AddRange(
                MiniGameHelper.ToProtoList<NetStellarBladeCurrency, StellarBladeCurrency>(stellar.Currency));
            response.AchievementMissionDataList.AddRange(
                MiniGameHelper.ToProtoList<NetStellarBladeMissionData, StellarBladeMissionData>(stellar.MissionData));
        }

        return response;
    }

    public ResArcadeEnterStellarBladeStage EnterStellarBladeStage(ulong userId, ReqArcadeEnterStellarBladeStage req)
    {
        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            stellar.LastEnteredStageId = req.StageId;
            repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();
        }

        return new ResArcadeEnterStellarBladeStage();
    }

    public ResArcadeClearStellarBladeStage ClearStellarBladeStage(User user, ReqArcadeClearStellarBladeStage req)
    {
        var response = new ResArcadeClearStellarBladeStage();
        NetStellarBladeRewardData ret = new() { Reward = new() { PassPoint = { } } };
        NetStellarBladeRewardData fret = new() { Reward = new() { PassPoint = { } } };

        var stage = GameData.Instance.EventSBStageTable.Values.FirstOrDefault(s => s.Id == req.StageId);
        var sbm = GetArcadeStellarBladeManager();

        int score = ComputeStellarBladeScore(
            req.DealtDamage, req.DamageTaken, req.PerfectDodgeCount, req.PerfectGuardCount, req.PotionUsedCount);

        if (repo.TryGetDictEntry<StellarBladeDatas>(user.ID, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            bool firstClear = !stellar.StatisticsData.ContainsKey(req.StageId);
            UpdateStellarBladeStatistics(
                stellar, req.StageId, req.Duration, req.DamageTaken,
                req.PerfectDodgeCount, req.PerfectGuardCount, req.PotionUsedCount, incrementKillCount: true);

            if (!firstClear)
            {
                if (stellar.BestStageDatas.TryGetValue(req.StageId, out var sBStage))
                {
                    response.IsBestScore = score > sBStage.BestScore;
                    if (response.IsBestScore) sBStage.BestScore = score;

                    response.IsBestDealtDamage = req.DealtDamage > sBStage.BestDealtDamage;
                    if (response.IsBestDealtDamage) sBStage.BestDealtDamage = req.DealtDamage;

                    response.IsBestDuration = req.Duration.LessThan(sBStage.BestDuration);
                    if (response.IsBestDuration) sBStage.BestDuration = req.Duration;
                }
            }
            else
            {
                if (stage != null)
                {
                    foreach (var item in stage.StageFirstClearReward)
                    {
                        if (item.StageFirstClearReward > 0)
                            MiniGameHelper.GetReward(ref fret, ref stellar, item.StageFirstClearReward);
                    }
                }

                stellar.BestStageDatas.TryAdd(req.StageId, new SBStageDatas
                {
                    BestDealtDamage = req.DealtDamage,
                    BestDuration = req.Duration,
                    BestScore = score
                });

                response.IsBestDealtDamage = true;
                response.IsBestDuration = true;
                response.IsBestScore = true;
            }

            response.FirstClearReward = fret;

            if (stage != null)
            {
                MiniGameHelper.GetReward(ref ret, ref stellar, stage.StageClearReward);
                MiniGameHelper.GetReward(ref ret, ref stellar, stage.StageFailureReward, score);
            }
            response.ClearReward = ret;

            if (sbm != null)
            {
                ReadOnlySpan<SBMissionType> types =
                [
                    SBMissionType.StageClear,
                    SBMissionType.PerfectGuard,
                    SBMissionType.Damage,
                    SBMissionType.PerfectDodge,
                    SBMissionType.PlayTime,
                    SBMissionType.UsePotion
                ];

                foreach (var type in types)
                {
                    MiniGameHelper.UpMission(
                        ref stellar,
                        missionType: type,
                        missionGroupId: sbm.MissionGroupId,
                        stage: req.StageId,
                        damage: req.DealtDamage,
                        usePotion: req.PotionUsedCount,
                        PerfectGuard: req.PerfectGuardCount,
                        PerfectDodge: req.PerfectDodgeCount,
                        playTime: (int)req.Duration.Seconds);
                }
            }

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();

            MiniGameHelper.InsertOrUpdate(req.ArcadeManagerId, user.ID, user.Guild.guildId ?? 0, score, 0);
        }

        response.Score = score;
        response.TruncatedDuration = req.Duration;
        return response;
    }

    public ResArcadeFailureStellarBladeStage FailStellarBladeStage(ulong userId, ReqArcadeFailureStellarBladeStage req)
    {
        var response = new ResArcadeFailureStellarBladeStage();
        NetStellarBladeRewardData ret = new();

        var sbm = GetArcadeStellarBladeManager();
        var stage = GameData.Instance.EventSBStageTable.Values.FirstOrDefault(s => s.Id == req.StageId);

        int score = ComputeStellarBladeScore(
            req.DealtDamage, req.DamageTaken, req.PerfectDodgeCount, req.PerfectGuardCount, req.PotionUsedCount);

        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            UpdateStellarBladeStatistics(
                stellar, req.StageId, req.Duration, req.DamageTaken,
                req.PerfectDodgeCount, req.PerfectGuardCount, req.PotionUsedCount, incrementKillCount: false);

            if (sbm != null)
            {
                ReadOnlySpan<SBMissionType> types =
                [
                    SBMissionType.PerfectGuard,
                    SBMissionType.Damage,
                    SBMissionType.PerfectDodge,
                    SBMissionType.PlayTime,
                    SBMissionType.UsePotion
                ];

                foreach (var type in types)
                {
                    MiniGameHelper.UpMission(
                        ref stellar,
                        missionType: type,
                        missionGroupId: sbm.MissionGroupId,
                        stage: req.StageId,
                        damage: req.DealtDamage,
                        usePotion: req.PotionUsedCount,
                        PerfectGuard: req.PerfectGuardCount,
                        PerfectDodge: req.PerfectDodgeCount,
                        playTime: (int)req.Duration.Seconds);
                }
            }

            if (stage != null)
                MiniGameHelper.GetReward(ref ret, ref stellar, stage.StageFailureReward, score);

            repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();
        }

        response.RewardData = ret;
        response.Score = score;
        return response;
    }

    public ResArcadeBuyStellarBladeShopItem BuyStellarBladeShopItem(ulong userId, ReqArcadeBuyStellarBladeShopItem req)
    {
        var sbm = GetArcadeStellarBladeManager();

        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            var shop = GameData.Instance.EventSBShopTable.Values.FirstOrDefault(x => x.Id == req.ShopId);
            if (shop != null)
            {
                var gold = stellar.Currency.FirstOrDefault(x => x.CurrencyType == (int)SBCurrencyType.Gold);
                if (gold != null)
                    gold.Amount -= shop.ItemPrice;
                stellar.SbItemIdList.AddUnique(shop.ItemId);

                if (sbm != null)
                    MiniGameHelper.UpMission(ref stellar, missionType: SBMissionType.BuyItem, missionGroupId: sbm.MissionGroupId, buyCount: 1);

                repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
                db.SaveChanges();
            }
        }

        return new ResArcadeBuyStellarBladeShopItem();
    }

    public ResArcadeEnhanceStellarBladeCharacter EnhanceStellarBladeCharacter(ulong userId, ReqArcadeEnhanceStellarBladeCharacter req)
    {
        var sbm = GetArcadeStellarBladeManager();

        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            var enhanceData = stellar.CharacterData.EnhanceDataList
                .FirstOrDefault(c => c.EnhanceType == req.EnhanceType);

            if (enhanceData != null && sbm != null)
            {
                var ent = GameData.Instance.EventSBCharacterEnhanceTable.Values.FirstOrDefault(x =>
                    x.GroupId == sbm.EnhanceGroupId &&
                    x.EnhanceType == (SBCharacterEnhanceType)req.EnhanceType &&
                    x.EnhanceLevel == req.CurrentLevel + 1);

                if (ent != null)
                {
                    var cost = stellar.Currency.FirstOrDefault(c => c.CurrencyType == (int)ent.EnhanceMaterialItemType);
                    if (cost != null)
                        cost.Amount -= ent.EnhanceMaterialItemValue;

                    enhanceData.EnhanceLevel = req.CurrentLevel + 1;
                    repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
                    db.SaveChanges();
                }
            }
        }

        return new ResArcadeEnhanceStellarBladeCharacter();
    }

    public ResArcadeSetStellarBladeDefaultAttackSkill SetStellarBladeDefaultAttackSkill(
        ulong userId, ReqArcadeSetStellarBladeDefaultAttackSkill req)
    {
        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            stellar.CharacterData.DefaultAttackSkillId = req.AttackSkillId;
            repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();
        }

        return new ResArcadeSetStellarBladeDefaultAttackSkill();
    }

    public ResArcadeEquipStellarBladeExoSpine EquipStellarBladeExoSpine(ulong userId, ReqArcadeEquipStellarBladeExoSpine req)
    {
        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            stellar.CharacterData.ExoSpineSbItemId = req.ExoSpineItemId;
            repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();
        }

        return new ResArcadeEquipStellarBladeExoSpine();
    }

    public ResArcadeEquipStellarBladeGear EquipStellarBladeGear(ulong userId, ReqArcadeEquipStellarBladeGear req)
    {
        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            switch (req.Slot)
            {
                case GearSlot.Slot1:
                    stellar.CharacterData.Gear1SbItemId = req.SbItemId;
                    break;
                case GearSlot.Slot2:
                    stellar.CharacterData.Gear2SbItemId = req.SbItemId;
                    break;
            }

            repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();
        }

        return new ResArcadeEquipStellarBladeGear();
    }

    public ResArcadeRecordStellarBladeTutorial RecordStellarBladeTutorial(ulong userId, ReqArcadeRecordStellarBladeTutorial req)
    {
        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            stellar.TutorialList.AddRangeUnique(req.TutorialListIds);
            repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();
        }

        return new ResArcadeRecordStellarBladeTutorial();
    }

    public ResArcadeLearnStellarBladeSkill LearnStellarBladeSkill(ulong userId, ReqArcadeLearnStellarBladeSkill req)
    {
        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            var skilltable = GameData.Instance.EventSBCharacterSkillTable.Values.FirstOrDefault(x => x.Id == req.SkillId);
            var currency = stellar.Currency.FirstOrDefault(c => c.CurrencyType == (int)SBCurrencyType.SkillPoint);
            if (currency != null && skilltable != null)
                currency.Amount -= skilltable.NeedLearnSkillpoint;

            List<int> list = [.. stellar.CharacterData.LearnedSkillIdList];
            list.AddUnique(req.SkillId);
            stellar.CharacterData.LearnedSkillIdList.Clear();
            stellar.CharacterData.LearnedSkillIdList.AddRange(list);

            repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();
        }

        return new ResArcadeLearnStellarBladeSkill();
    }

    public ResArcadeResetStellarBladeSkill ResetStellarBladeSkill(ulong userId, ReqArcadeResetStellarBladeSkill req)
    {
        var sbm = GetArcadeStellarBladeManager();
        var charrd = sbm != null
            ? GameData.Instance.EventSBCharacterTable.Values.FirstOrDefault(c => c.Id == sbm.CharacterId)
            : null;

        if (charrd != null &&
            repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) &&
            stellar != null)
        {
            int totalSkillPoints = stellar.CharacterData.LearnedSkillIdList
                .Select(id => GameData.Instance.EventSBCharacterSkillTable.Values.FirstOrDefault(s => s.Id == id))
                .Where(skill => skill != null)
                .Sum(skill => skill!.NeedLearnSkillpoint);

            List<int> learnskilllist = GameData.Instance.EventSBCharacterSkillTable.Values
                .Where(s => s.GroupId == charrd.SkillGroupId && s.IsDefaultLearn)
                .Select(s => s.Id)
                .ToList();

            stellar.CharacterData.LearnedSkillIdList.Clear();
            stellar.CharacterData.LearnedSkillIdList.AddRange(learnskilllist);

            var currency = stellar.Currency.FirstOrDefault(c => c.CurrencyType == (int)SBCurrencyType.SkillPoint);
            if (currency != null)
                currency.Amount += totalSkillPoints;

            repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();
        }

        return new ResArcadeResetStellarBladeSkill();
    }

    public ResArcadeAcquireStellarBladeAchievementReward AcquireStellarBladeAchievement(
        User user, ReqArcadeAcquireStellarBladeAchievementReward req)
    {
        var response = new ResArcadeAcquireStellarBladeAchievementReward();
        NetStellarBladeRewardData ret = new();

        if (repo.TryGetDictEntry<StellarBladeDatas>(user.ID, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) && stellar != null)
        {
            List<int> list = [];
            foreach (var item in req.AchievementIdList)
            {
                var mission = GameData.Instance.EventSBMissionTable.Values.FirstOrDefault(x => x.Id == item);
                if (mission != null)
                {
                    list.AddRange(MiniGameHelper.GetMissionReward(ref ret, ref stellar, user, mission));
                    var missdate = stellar.MissionData.FirstOrDefault(x => x.MissionId == mission.Id);
                    if (missdate != null)
                        missdate.IsReceived = true;
                }
            }

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, stellar);
            db.SaveChanges();

            ret.Reward = RewardUtils.RegisterRewardsForUserDou(user, list);
            if (list.Count > 0)
                UserSessionStore.SaveUser(user);

            response.RewardData = ret;
            response.UpdatedAchievementMissionDataList.AddRange(
                MiniGameHelper.ToProtoList<NetStellarBladeMissionData, StellarBladeMissionData>(stellar.MissionData));
        }

        return response;
    }

    public ResArcadeGetStellarBladeStatistics GetStellarBladeStatistics(ulong userId, ReqArcadeGetStellarBladeStatistics req)
    {
        var response = new ResArcadeGetStellarBladeStatistics();

        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, req.ArcadeManagerId, out var stellar) &&
            stellar != null && stellar.StatisticsData.Count > 0)
        {
            var statistdata = MiniGameHelper
                .ToProtoDict<int, ResArcadeGetStellarBladeStatistics.Types.NetStatisticsData, StatisticsData>(stellar.StatisticsData);
            response.StatisticsList.AddRange(statistdata.Values);
        }

        return response;
    }

    public ResArcadeGetStellarBladeUnionRanking GetStellarBladeUnionRanking(ulong userId, ReqArcadeGetStellarBladeUnionRanking req)
    {
        var response = new ResArcadeGetStellarBladeUnionRanking();
        var allBoard = MiniGameHelper.GetArcadeBoard(req.ArcadeManagerId).ToList();

        if (allBoard.Count > 0)
        {
            foreach (var item in allBoard)
            {
                if (UserSessionStore.GetUser(item.Record.UserId) is not User rankedUser)
                    continue;

                var rankdata = new NetMiniGameStellarBladeRankingData
                {
                    Rank = item.Rank,
                    Score = item.Record.Score,
                    User = LobbyHandler.CreateWholeUserDataFromDbUser(rankedUser)
                };
                response.RankingDataList.Add(rankdata);
                if (item.Record.UserId == userId)
                    response.UserRankingData = rankdata;
            }
        }
        else
        {
            response.UserRankingData = new NetMiniGameStellarBladeRankingData
            {
                Rank = 99,
                Score = 0,
                User = LobbyHandler.CreateWholeUserDataFromDbUser(userId)
            };
        }

        return response;
    }

    // --- TowerDefense ---

    public ResGetArcadeTowerDefenseData GetTowerDefense(ulong userId, ReqGetArcadeTowerDefenseData req)
    {
        var data = MiniGameHelper.InitTDData(req.ArcadeManagerId, userId, repo, db);
        var response = new ResGetArcadeTowerDefenseData
        {
            ChallengeMaxScore = data.ChallengeMaxScore,
            UpgradeCurrency = data.UpgradeCurrency
        };
        response.ClearedStageIdList.AddRange(data.ClearedStageIdList);
        response.ClearedTutorialIdList.AddRange(data.ClearedTutorialIdList);
        response.MissionProgressList.AddRange(
            MiniGameHelper.ToProtoList<NetArcadeTowerDefenseMissionProgress, ArcadeTowerDefenseMissionProgress>(data.MissionProgressList));
        response.UpgradeIdList.AddRange(data.UpgradeIdList);
        return response;
    }

    public ResClearArcadeTowerDefenseTutorial ClearTowerDefenseTutorial(ulong userId, ReqClearArcadeTowerDefenseTutorial req)
    {
        if (repo.TryGetDictEntry<TowerDefenseData>(userId, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, out var data) && data != null)
        {
            data.ClearedTutorialIdList.AddRangeUnique(req.TutorialIds);
            repo.SaveDictEntry(userId, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, data);
            db.SaveChanges();
        }

        return new ResClearArcadeTowerDefenseTutorial();
    }

    public ResEnterArcadeTowerDefenseStage EnterTowerDefenseStage(User user, ReqEnterArcadeTowerDefenseStage req)
    {
        if (repo.TryGetDictEntry<TowerDefenseData>(user.ID, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, out var data) && data != null)
        {
            data.LastEnteredStageId = req.StageId;
            repo.SaveDictEntry(user.ID, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, data);
            db.SaveChanges();
        }

        user.AddTrigger(Trigger.EventTowerDefensePlayCheck, 1, req.StageId);
        return new ResEnterArcadeTowerDefenseStage();
    }

    public ResFinishArcadeTowerDefense FinishTowerDefense(User user, ReqFinishArcadeTowerDefense req)
    {
        var response = new ResFinishArcadeTowerDefense();
        NetRewardData ret = new() { PassPoint = new() };
        bool rewardGranted = false;

        if (repo.TryGetDictEntry<TowerDefenseData>(user.ID, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, out var data) && data != null)
        {
            data.ClearedTutorialIdList.AddRangeUnique(req.TutorialIds);

            if (req.Score > data.ChallengeMaxScore)
            {
                data.ChallengeMaxScore = req.Score;
                if (user.Guild.guildId > 0)
                    MiniGameHelper.InsertOrUpdate(req.ArcadeManagerId, user.ID, user.Guild.guildId.Value, req.Score, 0);
            }

            if (req.IsWin)
            {
                if (!data.ClearedStageIdList.Contains(req.StageId))
                {
                    data.ClearedStageIdList.AddUnique(req.StageId);
                    var clear = GameData.Instance.EventTowerDefenseStageTable.Values.FirstOrDefault(x => x.Id == req.StageId);
                    if (clear != null && clear.StageFirstClearReward > 0)
                    {
                        ret = RewardUtils.RegisterRewardsForUser(user, clear.StageFirstClearReward);
                        rewardGranted = true;
                        data.UpgradeCurrency += clear.StageFirstClearUpgradeCurrencyReward;
                        response.UpgradeCurrencyAmount = clear.StageFirstClearUpgradeCurrencyReward;
                    }
                }
            }
            else
            {
                response.UpgradeCurrencyAmount = 0;
            }

            response.CurrentUpgradeCurrency = data.UpgradeCurrency;
            repo.SaveDictEntry(user.ID, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, data);
            db.SaveChanges();

            if (rewardGranted)
                UserSessionStore.SaveUser(user);
        }

        response.Reward = ret;
        return response;
    }

    public ResSetArcadeTowerDefenseUpgrade UpgradeTowerDefense(ulong userId, ReqSetArcadeTowerDefenseUpgrade req)
    {
        var response = new ResSetArcadeTowerDefenseUpgrade();

        if (repo.TryGetDictEntry<TowerDefenseData>(userId, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, out var data) && data != null)
        {
            var upcost = GameData.Instance.EventTowerDefenseUpgradeListTable.Values.FirstOrDefault(x => x.Id == req.UpgradeId);
            if (upcost != null)
                data.UpgradeCurrency -= upcost.UpgradeCost;

            data.UpgradeIdList.AddUnique(req.UpgradeId);
            repo.SaveDictEntry(userId, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, data);
            db.SaveChanges();

            response.UpgradeCurrency = data.UpgradeCurrency;
        }

        return response;
    }

    public ResResetArcadeTowerDefenseUpgrade ResetTowerDefenseUpgrade(ulong userId, ReqResetArcadeTowerDefenseUpgrade req)
    {
        var response = new ResResetArcadeTowerDefenseUpgrade();

        if (repo.TryGetDictEntry<TowerDefenseData>(userId, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, out var data) && data != null)
        {
            List<int> list = data.UpgradeIdList;
            if (list.Count > 0)
            {
                int totalCost = GameData.Instance.EventTowerDefenseUpgradeListTable
                    .Where(x => list.Contains(x.Key))
                    .Sum(x => x.Value.UpgradeCost);
                data.UpgradeIdList = [];
                data.UpgradeCurrency += totalCost;
                repo.SaveDictEntry(userId, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, data);
                db.SaveChanges();

                response.UpgradeCurrency = data.UpgradeCurrency;
            }
        }

        return response;
    }

    public ResUpdateArcadeTowerDefenseMissionProgress UpdateTowerDefenseMissionProgress(
        ulong userId, ReqUpdateArcadeTowerDefenseMissionProgress req)
    {
        var response = new ResUpdateArcadeTowerDefenseMissionProgress();

        if (repo.TryGetDictEntry<TowerDefenseData>(userId, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, out var data) && data != null)
        {
            foreach (var item in req.ProgressUpdateList)
            {
                var pro = data.MissionProgressList.FirstOrDefault(x => x.MissionUid == item.MissionUid);
                if (pro != null && pro.ReceivedAt == null)
                    pro.Progress += item.ProgressCount;
            }

            repo.SaveDictEntry(userId, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, data);
            db.SaveChanges();

            response.MissionProgressList.AddRange(
                MiniGameHelper.ToProtoList<NetArcadeTowerDefenseMissionProgress, ArcadeTowerDefenseMissionProgress>(data.MissionProgressList));
        }

        return response;
    }

    public ResCompleteArcadeTowerDefenseMission CompleteTowerDefenseMission(
        User user, ReqCompleteArcadeTowerDefenseMission req)
    {
        var response = new ResCompleteArcadeTowerDefenseMission();

        if (repo.TryGetDictEntry<TowerDefenseData>(user.ID, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, out var data) && data != null)
        {
            List<int> rewardlist = [];
            foreach (var item in req.MissionUidList)
            {
                var commiss = data.MissionProgressList.FirstOrDefault(x => x.MissionUid == item);
                if (commiss != null)
                {
                    commiss.ReceivedAt = DateTime.UtcNow.Date.ToTimestamp();
                    var mission = GameData.Instance.EventTowerDefenseMissionTable.Values.FirstOrDefault(x => x.Id == item);
                    if (mission != null)
                    {
                        switch (mission.RewardType)
                        {
                            case EventTowerDefenseMissionRewardType.DailyPoint:
                                var dailylist = GameData.Instance.EventTowerDefenseMissionTable.Values
                                    .Where(x => x.MissionCategory == EventTowerDefenseMissionCategory.DailyPoint);
                                foreach (var citem in dailylist)
                                {
                                    var pro = data.MissionProgressList.FirstOrDefault(x => x.MissionUid == citem.Id);
                                    if (pro != null && pro.ReceivedAt == null)
                                        pro.Progress += mission.RewardValue;
                                }
                                break;
                            case EventTowerDefenseMissionRewardType.Item:
                                rewardlist.Add(mission.RewardValue);
                                break;
                        }
                    }
                }
            }

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeTowerDefense, req.ArcadeManagerId, data);
            db.SaveChanges();

            NetRewardData ret = RewardUtils.RegisterRewardsForUserDou(user, rewardlist);
            if (rewardlist.Count > 0)
                UserSessionStore.SaveUser(user);

            response.MissionProgressList.AddRange(
                MiniGameHelper.ToProtoList<NetArcadeTowerDefenseMissionProgress, ArcadeTowerDefenseMissionProgress>(data.MissionProgressList));
            response.Reward = ret;
        }

        return response;
    }

    public ResGetArcadeTowerDefenseRanking GetTowerDefenseRanking(User user, ReqGetArcadeTowerDefenseRanking req)
    {
        var response = new ResGetArcadeTowerDefenseRanking { UserGuildRanking = new() };

        PopulateGuildLeaderboard(
            user,
            req.ArcadeManagerId,
            0,
            response.GuildRankingList,
            entry => response.UserGuildRanking = entry,
            (rank, score, wholeUser) => new NetArcadeTowerDefenseRankingData
            {
                Rank = rank,
                Score = score,
                User = wholeUser
            });

        return response;
    }

    // --- TTS ---

    private static void InitTtsMissions(TtsDatas ttsDatas)
    {
        foreach (var item in GameData.Instance.EventTTSMissionTable.Values)
        {
            ttsDatas.MissionData.TryAdd(item.Id, new MiniGameTtsMissionData
            {
                IsReceived = false,
                MissionId = item.Id,
                Progress = 0
            });
        }
    }

    public ResEnterMiniGameTtsTitle EnterTts(User user, ReqEnterMiniGameTtsTitle req)
    {
        var response = new ResEnterMiniGameTtsTitle();
        List<int> unlockSong = GameData.Instance.EventTTSSongManagerTable.Values.Select(x => x.Id).ToList();

        if (!repo.TryGetDictEntry<TtsDatas>(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) || ttsData == null)
        {
            ttsData = new TtsDatas();
            ttsData.UnlockSongId.AddRange(unlockSong);
            ttsData.PurchasedAlbumIds.AddRange([1001, 1002]);
            InitTtsMissions(ttsData);
        }

        response.HasFinishedTutorial = ttsData.IsFinishTutorial;
        response.LastPlayedDifficulty = ttsData.LastDifficulty;
        var missionList = MiniGameHelper.ToProtoDict<int, NetMiniGameTtsMissionData, MiniGameTtsMissionData>(ttsData.MissionData);
        response.MissionDataList.AddRange(missionList.Values);
        response.NewUnlockedEventTtsSongManagerTableIds.AddRange(ttsData.UnlockSongId);
        response.PlayCount = ttsData.AllPlayCount;
        response.PurchasedAlbumIds.AddRange(ttsData.PurchasedAlbumIds);
        response.ServerTimeStamp = Timestamp.FromDateTime(DateTime.UtcNow);
        response.SongPlayDataList.AddRange(
            MiniGameHelper.ToProtoList<NetMiniGameTtsSongPlayData, MiniGameTtsSongPlayData>(ttsData.SongPlayData));
        response.TotalPlayTime = ttsData.TotalPlayTime;
        response.AvailableEventTtsSongManagerTableIds.AddRange(ttsData.UnlockSongId);
        response.HasSelectProceedOnExpertAlertPopUp = true;

        var noticeid = GameData.Instance.EventTTSProductNoticeManagerTable.Values
            .FirstOrDefault(x => x.NoticeDate.AddDays(1) > DateTime.Now);
        if (noticeid != null)
            response.NewEventTtsProductNoticeManagerTableId = noticeid.Id;

        if (ttsData.SkinData.BackgroundSkinObjectId == 0)
        {
            var skin = GameData.Instance.EventTTSSkinObjectTable.Values.Where(x => x.IsFree).ToList();
            NetUserMiniGameTtsSkinData ttsSkinData = new()
            {
                BackgroundSkinObjectId = skin.FirstOrDefault(x => x.SkinObjectType == EventTTSSkinObjectType.BG)?.Id ?? 0,
                NoteSkinObjectId = skin.FirstOrDefault(x => x.SkinObjectType == EventTTSSkinObjectType.Note)?.Id ?? 0
            };
            var charskin = skin.Where(x => x.SkinObjectType == EventTTSSkinObjectType.Character).ToList();
            if (charskin.Count == 3)
            {
                ttsSkinData.FirstCharacterSkinObjectId = charskin[0].Id;
                ttsSkinData.SecondCharacterSkinObjectId = charskin[1].Id;
                ttsSkinData.ThirdCharacterSkinObjectId = charskin[2].Id;
            }

            ttsData.SkinData = MiniGameHelper.FromProto<UserMiniGameTtsSkinData, NetUserMiniGameTtsSkinData>(ttsSkinData);
            response.UserSkinData = ttsSkinData;
            foreach (var item in skin)
                ttsData.BuySkinObject.Add(item.Id);
        }
        else
        {
            response.UserSkinData = MiniGameHelper.ToProto<NetUserMiniGameTtsSkinData, UserMiniGameTtsSkinData>(ttsData.SkinData);
        }

        response.ExpertUnlockResult = ttsData.PurchasedAlbumIds.Count > 0
            ? MiniGameTtsExpertUnlockResult.AlreadyUnlocked
            : MiniGameTtsExpertUnlockResult.NotUnlocked;

        var rank = new RankData();
        var myfrank = rank.TtsRankDatas.TotalGetUserRank((long)user.ID, MiniGameTtsRankingType.Friend);
        response.MyFriendTotalRankData = myfrank != null
            ? new NetMyMiniGameTtsTotalRankData { Position = myfrank.Position, Score = myfrank.Score }
            : new NetMyMiniGameTtsTotalRankData { Position = 999, Score = 0 };

        var myurank = rank.TtsRankDatas.TotalGetUserRank((long)user.ID, MiniGameTtsRankingType.Union);
        response.MyUnionTotalRankData = myurank != null
            ? new NetMyMiniGameTtsTotalRankData { Position = myurank.Position, Score = myurank.Score }
            : new NetMyMiniGameTtsTotalRankData { Position = 999, Score = 0 };

        InitTtsMissions(ttsData);

        if (ttsData.UnlockSongId.Count < unlockSong.Count)
        {
            ttsData.PurchasedAlbumIds.AddRangeUnique([1001, 1002]);
            ttsData.UnlockSongId.AddRangeUnique(unlockSong);
        }

        repo.SaveDictEntry(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
        db.SaveChanges();

        response.UserData = LobbyHandler.CreateWholeUserDataFromDbUser(user);
        return response;
    }

    public ResCompleteMiniGameTtsMission CompleteTtsMission(User user, ReqCompleteMiniGameTtsMission req)
    {
        var response = new ResCompleteMiniGameTtsMission();

        if (repo.TryGetDictEntry<TtsDatas>(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            List<int> rewardIds = [];
            foreach (int item in req.EventTtsMissionTableIdList)
            {
                var mission = GameData.Instance.EventTTSMissionTable.Values.FirstOrDefault(m => m.Id == item);
                user.AddUnique(ttsData.MissionCompleteList, item);

                if (ttsData.MissionData.TryGetValue(item, out var miss))
                    miss.IsReceived = true;

                if (mission != null)
                    rewardIds.Add(mission.RewardId);
            }

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
            db.SaveChanges();

            NetRewardData ret = RewardUtils.RegisterRewardsForUserDou(user, rewardIds);
            if (rewardIds.Count > 0)
                UserSessionStore.SaveUser(user);

            response.RewardData = ret;
            response.Result = MiniGameTtsMissionCompleteResult.Success;
            response.UpdatedMissionDataList.AddRange(
                ttsData.MissionData.Values.Select(MiniGameHelper.ToProto<NetMiniGameTtsMissionData, MiniGameTtsMissionData>));
        }

        return response;
    }

    public ResSaveMiniGameTtsPlayTime SaveTtsPlayTime(ulong userId, ReqSaveMiniGameTtsPlayTime req)
    {
        if (repo.TryGetDictEntry<TtsDatas>(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            ttsData.TotalPlayTime = req.TotalPlayTime;
            repo.SaveDictEntry(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
            db.SaveChanges();
        }

        return new ResSaveMiniGameTtsPlayTime();
    }

    public ResGetMiniGameTtsProfile GetTtsProfile(ulong userId, ReqGetMiniGameTtsProfile req)
    {
        var response = new ResGetMiniGameTtsProfile();
        var rank = new RankData();

        NetMyMiniGameTtsTotalRankData mytotal = new();
        var myrank = rank.TtsRankDatas.TotalGetUserRank((long)userId, MiniGameTtsRankingType.Server);
        if (myrank != null)
        {
            mytotal.Score = myrank.Score;
            mytotal.Position = myrank.Position;
        }

        if (repo.TryGetDictEntry<TtsDatas>(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            response.PlayCountList.AddRange(
                ttsData.SongPlayCount.Select(MiniGameHelper.ToProto<NetMiniGameTtsSongPlayCount, MiniGameTtsSongPlayCount>));
        }

        response.MyServerRankData = mytotal;
        return response;
    }

    public ResEnterMiniGameTtsPackageShop EnterTtsShop(ulong userId, ReqEnterMiniGameTtsPackageShop req)
    {
        if (repo.TryGetDictEntry<TtsDatas>(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            var shopMissions = GameData.Instance.EventTTSMissionTable.Values
                .Where(s => s.MissionType == EventTTSMissionType.EnterTTSPackageShop)
                .ToList();

            if (shopMissions.Count > 0)
            {
                foreach (var item in shopMissions)
                {
                    if (ttsData.MissionData.TryGetValue(item.Id, out var missionData))
                        missionData.Progress += 1;
                }

                repo.SaveDictEntry(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
                db.SaveChanges();
            }
        }

        return new ResEnterMiniGameTtsPackageShop();
    }

    public ResBuyMiniGameTtsSkin BuyTtsSkin(User user, ReqBuyMiniGameTtsSkin req)
    {
        var response = new ResBuyMiniGameTtsSkin();

        if (repo.TryGetDictEntry<TtsDatas>(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            var skinobj = GameData.Instance.EventTTSSkinObjectTable.Values
                .FirstOrDefault(x => x.Id == req.EventTtsSkinObjectId);

            if (skinobj != null && user.SubtractCurrency((CurrencyType)skinobj.CostCurrencyId, skinobj.CostCurrencyValue))
            {
                ttsData.BuySkinObject.Add(skinobj.Id);
                repo.SaveDictEntry(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
                db.SaveChanges();
                UserSessionStore.SaveUser(user);
            }

            foreach (var item in user.Currency)
                response.Currency.Add(new NetUserCurrencyData { Type = (int)item.Key, Value = item.Value });

            response.EventTtsSkinObjectIdList.AddRange(ttsData.BuySkinObject);
        }

        return response;
    }

    public ResEnterMiniGameTtsSkin EnterTtsSkin(ulong userId, ReqEnterMiniGameTtsSkin req)
    {
        var response = new ResEnterMiniGameTtsSkin();

        if (repo.TryGetDictEntry<TtsDatas>(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
            response.EventTtsSkinObjectIdList.AddRange(ttsData.BuySkinObject);

        return response;
    }

    public ResSetMiniGameTtsSkin SetTtsSkin(ulong userId, ReqSetMiniGameTtsSkin req)
    {
        if (repo.TryGetDictEntry<TtsDatas>(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            ttsData.SkinData = MiniGameHelper.FromProto<UserMiniGameTtsSkinData, NetUserMiniGameTtsSkinData>(req.UserSkinData);
            repo.SaveDictEntry(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
            db.SaveChanges();
        }

        return new ResSetMiniGameTtsSkin();
    }

    public ResGetMiniGameTtsSongData GetTtsSong(User user, ReqGetMiniGameTtsSongData req)
    {
        var response = new ResGetMiniGameTtsSongData();

        if (repo.TryGetDictEntry<TtsDatas>(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            var frank = TtsHelper.GetUserSongRankingsWithRank((long)user.ID, req.EventTtsSongManagerTableId, MiniGameTtsRankingType.Friend);
            var urank = TtsHelper.GetUserSongRankingsWithRank((long)user.ID, req.EventTtsSongManagerTableId, MiniGameTtsRankingType.Union);

            NetMyMiniGameTtsSongRankData defaultRank = new() { Difficulty = MiniGameTtsDifficulty.Casual, Position = 0, Score = 0 };
            if (frank.Count > 0)
            {
                var (entity, rank) = frank.OrderBy(x => x.Rank).First();
                response.MyFriendSongRankDataList.Add(new NetMyMiniGameTtsSongRankData { Difficulty = entity.Difficulty, Position = rank, Score = entity.Score });
            }
            else
            {
                response.MyFriendSongRankDataList.Add(defaultRank);
            }

            if (urank.Count > 0 && user.Guild.guildId > 0)
            {
                var (entity, rank) = urank.OrderBy(x => x.Rank).First();
                response.MyUnionSongRankDataList.Add(new NetMyMiniGameTtsSongRankData { Difficulty = entity.Difficulty, Position = rank, Score = entity.Score });
            }
            else
            {
                response.MyUnionSongRankDataList.Add(defaultRank);
            }

            if (ttsData.ScoreData.Count > 0)
            {
                response.ScoreData.AddRange(
                    ttsData.ScoreData.Select(MiniGameHelper.ToProto<NetMiniGameTtsScoreData, MiniGameTtsScoreData>));
            }
            else
            {
                response.ScoreData.Add(new NetMiniGameTtsScoreData
                {
                    Difficulty = MiniGameTtsDifficulty.Casual,
                    EventTtsSongManagerTableId = req.EventTtsSongManagerTableId,
                    Score = 0
                });
            }
        }

        return response;
    }

    public ResGetMiniGameTtsSongRanking GetTtsSongRanking(ulong userId, ReqGetMiniGameTtsSongRanking req)
    {
        var response = new ResGetMiniGameTtsSongRanking();

        var entities = TtsHelper.GetBySongIdAndRankTypeWithRank(req.EventTtsSongManagerTableId, req.RankingType);
        foreach (var item in entities)
        {
            if (UserSessionStore.GetUser((ulong)item.Entity.UserId) is not User rankedUser)
                continue;

            response.TopRankDataList.Add(new NetMiniGameTtsSongRankData
            {
                Difficulty = item.Entity.Difficulty,
                Position = item.Rank,
                Score = item.Entity.Score,
                User = LobbyHandler.CreateWholeUserDataFromDbUser(rankedUser)
            });
        }

        var srank = TtsHelper.GetUserSongRankingsWithRank((long)userId, req.EventTtsSongManagerTableId, MiniGameTtsRankingType.Server);
        if (srank.Count > 0)
        {
            var best = srank.OrderBy(x => x.Rank).First();
            response.MyRankData = new NetMyMiniGameTtsSongRankData
            {
                Difficulty = best.Entity.Difficulty,
                Position = best.Rank,
                Score = best.Entity.Score
            };
        }
        else
        {
            response.MyRankData = new NetMyMiniGameTtsSongRankData
            {
                Difficulty = MiniGameTtsDifficulty.Casual,
                Position = 0,
                Score = 0
            };
        }

        return response;
    }

    public ResGetMiniGameTtsTotalRanking GetTtsTotalRanking(ulong userId, ReqGetMiniGameTtsTotalRanking req)
    {
        var response = new ResGetMiniGameTtsTotalRanking();
        var rank = new RankData();

        var entities = rank.TtsRankDatas.TotalGetLeaderboardByRankType(req.RankingType);
        if (entities.Count > 0)
            response.TopRankDataList.AddRange(entities);

        var myrank = rank.TtsRankDatas.TotalGetUserRank((long)userId, req.RankingType);
        if (myrank != null)
        {
            response.MyRankData = new NetMyMiniGameTtsTotalRankData
            {
                Position = myrank.Position,
                Score = myrank.Score
            };
        }

        return response;
    }

    public ResFinishMiniGameTtsPlay FinishTts(User user, ReqFinishMiniGameTtsPlay req)
    {
        var response = new ResFinishMiniGameTtsPlay();

        if (repo.TryGetDictEntry<TtsDatas>(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            var predata = ttsData.SongPlayData
                .FirstOrDefault(x => x.EventTtsSongManagerTableId == req.EventTtsSongManagerTableId && x.Difficulty == req.Difficulty);
            if (predata != null)
                response.PreviousSongPlayData = MiniGameHelper.ToProto<NetMiniGameTtsSongPlayData, MiniGameTtsSongPlayData>(predata);

            ttsData.AllPlayCount += 1;
            ttsData.TotalScore += req.Score;

            long nowTicks = DateTime.Now.Ticks;
            foreach (var rankType in (ReadOnlySpan<MiniGameTtsRankingType>)
                [MiniGameTtsRankingType.Server, MiniGameTtsRankingType.Friend, MiniGameTtsRankingType.Union])
            {
                TtsHelper.InsertOrUpdate(new SongRankData
                {
                    Difficulty = req.Difficulty,
                    IsDeleted = 0,
                    RankType = rankType,
                    Score = req.Score,
                    SongId = req.EventTtsSongManagerTableId,
                    UserId = (long)user.ID,
                    UpdateTime = nowTicks
                });
            }

            var rank = new RankData();
            rank.TtsRankDatas.InsertOrUpdate((long)user.ID, MiniGameTtsRankingType.Server, ttsData.TotalScore);
            rank.TtsRankDatas.InsertOrUpdate((long)user.ID, MiniGameTtsRankingType.Friend, ttsData.TotalScore);
            rank.TtsRankDatas.InsertOrUpdate((long)user.ID, MiniGameTtsRankingType.Union, ttsData.TotalScore);

            AddTtsScoreData(req, ttsData);

            if (req.IsCleared)
            {
                user.AddUnique(ttsData.BadgeSongId, req.EventTtsSongManagerTableId);
                var badge = new Dictionary<int, MiniGameTtsBadgeData>();
                badge.TryAdd(req.EventTtsSongManagerTableId, new MiniGameTtsBadgeData
                {
                    HasEntered = true,
                    HasNewSongBadge = true,
                    HasReceivableReward = false
                });
                ttsData.BadgeData.TryAdd(req.Difficulty, badge);
            }

            ttsData.TotalPlayTime = req.TotalPlayTime;
            ttsData.LastDifficulty = req.Difficulty;

            response.IsNewRecord = IsTtsNewRecord(req.EventTtsSongManagerTableId, req.Difficulty, req.Score, ttsData);
            response.PlayCount = ttsData.AllPlayCount;

            UpdateTtsMissions(ttsData, req);

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
            db.SaveChanges();

            user.AddTrigger(Trigger.EventMiniGameTTSPlayCheck, 1, req.EventTtsSongManagerTableId);
        }

        return response;
    }

    public ResResumeMiniGameTtsPlay ResumeTts(ulong userId, ReqResumeMiniGameTtsPlay req)
    {
        if (repo.TryGetDictEntry<TtsDatas>(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            ttsData.TotalPlayTime = TtsHelper.Subtract(ttsData.TotalPlayTime, req.PauseDuration);
            repo.SaveDictEntry(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
            db.SaveChanges();
        }

        return new ResResumeMiniGameTtsPlay();
    }

    public ResFinishMiniGameTtsTutorial FinishTtsTutorial(ulong userId, ReqFinishMiniGameTtsTutorial req)
    {
        if (repo.TryGetDictEntry<TtsDatas>(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, out var ttsData) && ttsData != null)
        {
            ttsData.IsFinishTutorial = true;
            repo.SaveDictEntry(userId, MinigameRepository.TypeTts, req.EventTtsManagerTableId, ttsData);
            db.SaveChanges();
        }

        return new ResFinishMiniGameTtsTutorial();
    }

    private static bool IsTtsNewRecord(int songId, MiniGameTtsDifficulty difficulty, int score, TtsDatas ttsData)
    {
        var existing = ttsData.ScoreData.FirstOrDefault(x =>
            x.EventTtsSongManagerTableId == songId &&
            x.Difficulty == difficulty);
        return existing != null && score > existing.Score;
    }

    private static void UpdateTtsMissions(TtsDatas ttsData, ReqFinishMiniGameTtsPlay req)
    {
        List<EventTTSMissionType> label =
        [
            EventTTSMissionType.MusicPlayCount,
            EventTTSMissionType.ScoreAccumulate
        ];

        if ((int)req.Rank >= (int)MiniGameTtsRank.A)
            label.Add(EventTTSMissionType.MusicClearCountByRankA);
        else if ((int)req.Rank >= (int)MiniGameTtsRank.S)
            label.Add(EventTTSMissionType.MusicClearCountByRankS);
        else if ((int)req.Rank >= (int)MiniGameTtsRank.Splus)
            label.Add(EventTTSMissionType.MusicClearCountByRankSS);

        if (req.GreatCount > 0)
        {
            label.Add(EventTTSMissionType.NoteRankCountByGreat);
        }
        else if (req.PerfectCount > 0)
        {
            label.Add(EventTTSMissionType.NoteRankCountByGreat);
            label.Add(EventTTSMissionType.NoteRankCountByPerfect);
        }
        else if (req.PerfectPlusCount > 0)
        {
            label.Add(EventTTSMissionType.NoteRankCountByGreat);
            label.Add(EventTTSMissionType.NoteRankCountByPerfect);
            label.Add(EventTTSMissionType.NoteRankCountByPerfectPlus);
        }

        List<int> specificMusic =
        [
            .. GameData.Instance.EventTTSMissionTable.Values
                .Where(s => s.MissionType == EventTTSMissionType.SpecificMusicPlayCount)
                .SelectMany(s => s.MissionValue)
        ];

        if (specificMusic.Contains(req.EventTtsSongManagerTableId))
            label.Add(EventTTSMissionType.SpecificMusicPlayCount);

        var skinlist = GameData.Instance.EventTTSSkinObjectTable.Values
            .Where(x => !x.IsFree)
            .Select(x => x.Id)
            .ToList();
        List<int> userlist = [];
        userlist.AddUnique(ttsData.SkinData.FirstCharacterSkinObjectId);
        userlist.AddUnique(ttsData.SkinData.SecondCharacterSkinObjectId);
        userlist.AddUnique(ttsData.SkinData.ThirdCharacterSkinObjectId);
        if (skinlist.Any(userlist.Contains))
            label.Add(EventTTSMissionType.AnyMusicPlayCountWithSkinObject);

        var matchedItems = skinlist.Intersect(userlist).ToList();

        foreach (var item in label)
        {
            switch (item)
            {
                case EventTTSMissionType.MusicClearCountByRankA:
                    IncrementUnreceivedTtsMissions(ttsData, EventTTSMissionType.MusicClearCountByRankA, 1);
                    break;
                case EventTTSMissionType.NoteRankCountByGreat:
                    IncrementUnreceivedTtsMissions(
                        ttsData,
                        EventTTSMissionType.NoteRankCountByGreat,
                        req.GreatCount + req.PerfectCount + req.PerfectPlusCount);
                    break;
                case EventTTSMissionType.ScoreAccumulate:
                    foreach (var miss in GameData.Instance.EventTTSMissionTable.Values.Where(s => s.MissionType == EventTTSMissionType.ScoreAccumulate))
                    {
                        if (ttsData.MissionData.TryGetValue(miss.Id, out var vale) && !vale.IsReceived)
                            vale.Progress = (int)ttsData.TotalScore;
                    }
                    break;
                case EventTTSMissionType.MusicPlayCount:
                    IncrementUnreceivedTtsMissions(ttsData, EventTTSMissionType.MusicPlayCount, 1);
                    break;
                case EventTTSMissionType.SpecificMusicPlayCount:
                    foreach (var miss in GameData.Instance.EventTTSMissionTable.Values.Where(s =>
                        s.MissionType == EventTTSMissionType.SpecificMusicPlayCount &&
                        s.MissionValue.Contains(req.EventTtsSongManagerTableId)))
                    {
                        if (ttsData.MissionData.TryGetValue(miss.Id, out var vale) && !vale.IsReceived)
                            vale.Progress += 1;
                    }
                    break;
                case EventTTSMissionType.AnyMusicPlayCountWithSkinObject:
                    foreach (var aitem in matchedItems)
                    {
                        foreach (var miss in GameData.Instance.EventTTSMissionTable.Values.Where(s =>
                            s.MissionType == EventTTSMissionType.AnyMusicPlayCountWithSkinObject &&
                            s.MissionValue.Contains(aitem)))
                        {
                            if (ttsData.MissionData.TryGetValue(miss.Id, out var vale) && !vale.IsReceived)
                                vale.Progress += 1;
                        }
                    }
                    break;
            }
        }
    }

    private static void IncrementUnreceivedTtsMissions(TtsDatas ttsData, EventTTSMissionType missionType, int delta)
    {
        foreach (var miss in GameData.Instance.EventTTSMissionTable.Values.Where(s => s.MissionType == missionType))
        {
            if (ttsData.MissionData.TryGetValue(miss.Id, out var vale) && !vale.IsReceived)
                vale.Progress += delta;
        }
    }

    private static void AddTtsScoreData(ReqFinishMiniGameTtsPlay req, TtsDatas ttsData)
    {
        var existing = ttsData.ScoreData.FirstOrDefault(x =>
            x.EventTtsSongManagerTableId == req.EventTtsSongManagerTableId &&
            x.Difficulty == req.Difficulty);
        if (existing != null)
        {
            existing.Score = Math.Max(existing.Score, req.Score);
        }
        else
        {
            ttsData.ScoreData.Add(new MiniGameTtsScoreData
            {
                Difficulty = req.Difficulty,
                EventTtsSongManagerTableId = req.EventTtsSongManagerTableId,
                Score = req.Score
            });
        }

        var playrecord = ttsData.SongPlayCount.FirstOrDefault(p =>
            p.EventTtsSongManagerTableId == req.EventTtsSongManagerTableId &&
            p.Difficulty == req.Difficulty);
        if (playrecord != null)
        {
            playrecord.PlayCount += 1;
        }
        else
        {
            ttsData.SongPlayCount.Add(new MiniGameTtsSongPlayCount
            {
                Difficulty = req.Difficulty,
                EventTtsSongManagerTableId = req.EventTtsSongManagerTableId,
                PlayCount = 1
            });
        }

        var playdata = ttsData.SongPlayData.FirstOrDefault(d =>
            d.EventTtsSongManagerTableId == req.EventTtsSongManagerTableId &&
            d.Difficulty == req.Difficulty);
        if (playdata != null)
        {
            playdata.ComboCount = Math.Max(playdata.ComboCount, req.ComboCount);
            playdata.Score = Math.Max(playdata.Score, req.Score);
            if (req.IsCleared)
                playdata.IsCleared = true;
            playdata.GoodCount = Math.Max(playdata.GoodCount, req.GoodCount);
            playdata.GreatCount = Math.Max(playdata.GreatCount, req.GreatCount);
            if (req.IsAllPerfect)
                playdata.IsAllPerfect = true;
            if (req.IsFullCombo)
                playdata.IsFullCombo = true;
            playdata.MissCount = Math.Min(playdata.MissCount, req.MissCount);
            playdata.PerfectCount = Math.Max(playdata.PerfectCount, req.PerfectCount);
            playdata.PerfectPlusCount = Math.Max(playdata.PerfectPlusCount, req.PerfectPlusCount);
            playdata.Rank = (MiniGameTtsRank)Math.Max((int)playdata.Rank, (int)req.Rank);
        }
        else
        {
            ttsData.SongPlayData.Add(new MiniGameTtsSongPlayData
            {
                EventTtsSongManagerTableId = req.EventTtsSongManagerTableId,
                Difficulty = req.Difficulty,
                ComboCount = req.ComboCount,
                Score = req.Score,
                IsCleared = req.IsCleared,
                GoodCount = req.GoodCount,
                GreatCount = req.GreatCount,
                IsAllPerfect = req.IsAllPerfect,
                IsFullCombo = req.IsFullCombo,
                MissCount = req.MissCount,
                PerfectCount = req.PerfectCount,
                PerfectPlusCount = req.PerfectPlusCount,
                Rank = req.Rank
            });
        }
    }
}
