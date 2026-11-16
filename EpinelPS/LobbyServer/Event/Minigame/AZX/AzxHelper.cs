using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Newtonsoft.Json;

namespace EpinelPS.LobbyServer.Event.Minigame.AZX;

public static class AzxHelper
{
    private static readonly ILog log = LogManager.GetLogger(typeof(AzxHelper));

    public static void AcquireReward(
        User user,
        MinigameRepository repo,
        GameContext db,
        ref NetRewardData reward,
        int azxId,
        RepeatedField<int> missionIds)
    {
        log.Debug($"Acquiring reward for user {user.ID}, azxId: {azxId}, missionIds: {JsonConvert.SerializeObject(missionIds)}");
        if (missionIds.Count == 0 || azxId == 0) return;
        try
        {
            var missions = GameData.Instance.EventAZXAppleGameMissionTable.Values.Where(x =>
                x.MissionRewardId > 0 && missionIds.Contains(x.Id)).ToList();
            if (missions.Count == 0) return;

            List<Reward_Data> rewardDatas = [];
            foreach (var mission in missions)
            {
                var rewardRecord = GameData.Instance.GetRewardTableEntry(mission.MissionRewardId);
                if (rewardRecord is null || rewardRecord.Rewards.Length == 0) continue;
                foreach (var rewardItem in rewardRecord.Rewards)
                {
                    if (rewardItem.RewardValue == 0) continue;
                    int itemIndex = rewardDatas.FindIndex(x => x.RewardId == rewardItem.RewardId);
                    if (itemIndex >= 0)
                        rewardDatas[itemIndex].RewardValue += rewardItem.RewardValue;
                    else
                        rewardDatas.Add(rewardItem);
                }
            }

            foreach (var rewardData in rewardDatas)
            {
                RewardUtils.AddSingleObject(user, ref reward, rewardData.RewardId, rewardData.RewardType, rewardData.RewardValue);
            }

            var azxInfo = GetAzxInfo(user.ID, azxId, repo);
            azxInfo.AchievementMissionDataList.AddRange(missionIds.Select(x =>
                new AchievementMissionData() { MissionId = x, IsReceived = true }));

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeMiniGameAzx, azxId, azxInfo);
            db.SaveChanges();

            if (rewardDatas.Count > 0)
            {
                UserSessionStore.SaveUser(user);
            }
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Acquiring reward failed: {ex.Message}", LogType.Error);
        }
    }

    public static void GetRanking(ulong userId, int azxId, MinigameRepository repo, ref ResGetMiniGameAzxRanking response)
    {
        int dateDay = GetDateDay();
        var azxInfo = GetAzxInfo(userId, azxId, repo);
        var scoreData = azxInfo.ScoreDatas.Find(x => x.DateDay == dateDay && x.AzxId == azxId);
        int score = scoreData?.HighScore ?? 0;
        Duration timeToScore = scoreData?.HighScoreTime ?? new Duration() { Seconds = 0, Nanos = 0 };

        response.UserGuildRanking = new NetMiniGameAzxRankingData()
        {
            Rank = 1,
            ScoreAndTime = new NetMiniGameAzxScoreAndTime()
            {
                Score = score,
                TimeToScore = timeToScore
            },
            User = LobbyHandler.CreateWholeUserDataFromDbUser(userId)
        };
        response.GuildRankingList.Add(new NetMiniGameAzxRankingData()
        {
            Rank = 2,
            ScoreAndTime = new NetMiniGameAzxScoreAndTime()
            {
                Score = 80000,
                TimeToScore = new Duration() { Seconds = 118, Nanos = 432877000 }
            },
            User = LobbyHandler.CreateWholeUserDataFromDbUser(userId)
        });
    }

    public static void FinishAzx(
        User user,
        MinigameRepository repo,
        GameContext db,
        ReqFinishMiniGameAzx req,
        ref ResFinishMiniGameAzx response)
    {
        try
        {
            log.Debug($"Finishing AZX for user {user.ID}, data: {JsonConvert.SerializeObject(req)}");
            int dateDay = GetDateDay();

            int score = req.ScoreAndTime.Score;
            Duration timeToScore = req.ScoreAndTime.TimeToScore;

            NetMiniGameAzxDailyMissionData dailyMissionData = new() { IsDailyRewarded = false, DailyAccumulatedScore = 0 };
            NetRewardData reward = new();
            bool rewardGranted = false;

            var azxInfo = GetAzxInfo(user.ID, req.AzxId, repo);

            if (req.CutSceneId > 0 && azxInfo.CutSceneDataList.Find(x => x.CutSceneId == req.CutSceneId) is null)
            {
                azxInfo.CutSceneDataList.Add(new CutSceneData() { CutSceneId = req.CutSceneId, IsNew = true });
            }

            var scoreDataIndex = azxInfo.ScoreDatas.FindIndex(x => x.DateDay == dateDay && x.AzxId == req.AzxId);
            if (scoreDataIndex >= 0)
            {
                if (score > 10000 && !azxInfo.ScoreDatas[scoreDataIndex].IsDailyRewarded)
                {
                    azxInfo.ScoreDatas[scoreDataIndex].IsDailyRewarded = true;
                    dailyMissionData.IsDailyRewarded = true;
                    RewardUtils.AddSingleCurrencyObject(user, ref reward, CurrencyType.FreeCash, 30);
                    rewardGranted = true;
                }
                azxInfo.ScoreDatas[scoreDataIndex].AccumulatedScore += score;
                dailyMissionData.DailyAccumulatedScore = azxInfo.ScoreDatas[scoreDataIndex].AccumulatedScore;
                if (azxInfo.ScoreDatas[scoreDataIndex].HighScore < score)
                {
                    response.IsNewHighScore = true;
                    azxInfo.ScoreDatas[scoreDataIndex].HighScore = score;
                }
                Duration? existingTime = azxInfo.ScoreDatas[scoreDataIndex].HighScoreTime;
                if (existingTime == null || existingTime.ToTimeSpan() > timeToScore.ToTimeSpan())
                {
                    response.IsNewHighScoreTime = true;
                    azxInfo.ScoreDatas[scoreDataIndex].HighScoreTime = timeToScore;
                }
            }
            else
            {
                bool isDailyRewarded = false;
                if (score > 10000)
                {
                    isDailyRewarded = true;
                    dailyMissionData.IsDailyRewarded = true;
                    RewardUtils.AddSingleCurrencyObject(user, ref reward, CurrencyType.FreeCash, 30);
                    rewardGranted = true;
                }
                response.IsNewHighScoreTime = true;
                response.IsNewHighScore = true;
                azxInfo.ScoreDatas.Add(new MiniGameAzxScoreData
                {
                    AzxId = req.AzxId,
                    DateDay = dateDay,
                    AccumulatedScore = score,
                    HighScore = score,
                    HighScoreTime = timeToScore,
                    IsDailyRewarded = isDailyRewarded
                });
                dailyMissionData.DailyAccumulatedScore = score;
            }

            azxInfo.CharacterCount ??= [];
            if (azxInfo.CharacterCount.TryGetValue(req.PlayCharacterId, out var characterCount))
                azxInfo.CharacterCount[req.PlayCharacterId] = characterCount + 1;
            else
                azxInfo.CharacterCount.Add(req.PlayCharacterId, 1);

            if (req.SkillUseCountList != null && req.SkillUseCountList.Count > 0)
            {
                azxInfo.SkillCount ??= [];
                foreach (var item in req.SkillUseCountList)
                {
                    if (azxInfo.SkillCount.TryGetValue(item.SkillId, out var skillCount))
                        azxInfo.SkillCount[item.SkillId] = skillCount + item.SkillUseCount;
                    else
                        azxInfo.SkillCount.Add(item.SkillId, item.SkillUseCount);
                }
            }

            response.DailyMissionData = dailyMissionData;
            response.Reward = reward;

            repo.SaveDictEntry(user.ID, MinigameRepository.TypeMiniGameAzx, req.AzxId, azxInfo);
            db.SaveChanges();

            if (rewardGranted)
            {
                UserSessionStore.SaveUser(user);
            }
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Finish AZX Error: {ex.Message}", LogType.Error);
        }
    }

    public static void EnterAzx(
        ulong userId,
        MinigameRepository repo,
        GameContext db,
        ref ResEnterMiniGameAzx response,
        int azxId,
        int playBoardId,
        int playCharacterId)
    {
        log.Debug($"Entering AZX AzxId: {azxId}, PlayBoardId: {playBoardId}, PlayCharacterId: {playCharacterId}");
        try
        {
            var azxInfo = GetAzxInfo(userId, azxId, repo);
            azxInfo.SelectedBoardId = playBoardId;
            azxInfo.SelectedCharacterId = playCharacterId;
            repo.SaveDictEntry(userId, MinigameRepository.TypeMiniGameAzx, azxId, azxInfo);
            db.SaveChanges();
            response.PreviousSRankCount = 0;
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Enter AZX Error: {ex.Message}", LogType.Error);
        }
    }

    public static void GetAzxData(ulong userId, int azxId, MinigameRepository repo, ref ResGetMiniGameAzxData response)
    {
        log.Debug($"Getting AZX data for user {userId}");

        var azxInfo = GetAzxInfo(userId, azxId, repo);
        azxInfo.ScoreDatas ??= [];
        azxInfo.SkillCount ??= [];
        azxInfo.CharacterCount ??= [];
        int sumScore = azxInfo.ScoreDatas.Sum(x => x.AccumulatedScore);

        log.Debug($"AZX data: {JsonConvert.SerializeObject(azxInfo)}");
        try
        {
            var missions = GameData.Instance.EventAZXAppleGameMissionTable.Values.ToList();

            foreach (var mission in missions)
            {
                bool isReceived = false;
                azxInfo.AchievementMissionDataList ??= [];
                var item = azxInfo.AchievementMissionDataList.Find(x => x.MissionId == mission.Id);
                if (item is not null) isReceived = item.IsReceived;
                if (mission.MissionType == EventAZXAppleGameMissionMissionType.GetScore)
                {
                    AddAchievementMission(ref response, mission.Id, sumScore, isReceived);
                }
                else if (mission.MissionType == EventAZXAppleGameMissionMissionType.UseSkillCount)
                {
                    int progress = 0;
                    if (azxInfo.SkillCount.TryGetValue(mission.MissionConditionId, out var skillCount)) progress = skillCount;
                    AddAchievementMission(ref response, mission.Id, progress, isReceived);
                }
                else if (mission.MissionType == EventAZXAppleGameMissionMissionType.PlayCharacterCount)
                {
                    int progress = 0;
                    if (azxInfo.CharacterCount.TryGetValue(mission.MissionConditionId, out var characterCount)) progress = characterCount;
                    AddAchievementMission(ref response, mission.Id, progress, isReceived);
                }
                else
                {
                    AddAchievementMission(ref response, mission.Id, 0, isReceived);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Get AchievementMissionDataList Error: {ex.Message}");
        }

        try
        {
            azxInfo.ConditionalBoardDataList ??= [];
            var boards = GameData.Instance.EventAZXAppleGameBoardTable.Values.ToList();
            foreach (var board in boards)
            {
                var item = azxInfo.ConditionalBoardDataList.Find(x => x.BoardId == board.Id);
                bool isUnlocked = item is not null && item.IsUnlocked;
                if (board.BoardOpenScore == 0) continue;
                response.ConditionalBoardDataList.Add(new NetMiniGameAzxConditionalBoardData()
                {
                    BoardId = board.Id,
                    Progress = sumScore,
                    IsUnlocked = isUnlocked
                });
            }
            int selectedBoardId = azxInfo.SelectedBoardId > 0 ? azxInfo.SelectedBoardId : boards.Min(x => x.Id);
            response.SelectedBoardId = selectedBoardId;
        }
        catch (Exception ex)
        {
            log.Error($"Get ConditionalBoardDataList Error: {ex.Message}");
        }

        try
        {
            azxInfo.ConditionalCharacterDataList ??= [];
            var characters = GameData.Instance.EventAZXAppleGameCharacterTable.Values.ToList();
            foreach (var character in characters)
            {
                var item = azxInfo.ConditionalCharacterDataList.Find(x => x.CharacterId == character.Id);
                bool isUnlocked = item is not null && item.IsUnlocked;
                if (character.CharacterOpenScore == 0) continue;
                response.ConditionalCharacterDataList.Add(new NetMiniGameAzxConditionalCharacterData()
                {
                    CharacterId = character.Id,
                    Progress = sumScore,
                    IsUnlocked = isUnlocked
                });
            }
            int selectedCharacterId = azxInfo.SelectedCharacterId > 0 ? azxInfo.SelectedCharacterId : characters.Min(x => x.Id);
            response.SelectedCharacterId = selectedCharacterId;
        }
        catch (Exception ex)
        {
            log.Error($"Get ConditionalCharacterDataList Error: {ex.Message}");
        }

        try
        {
            azxInfo.ConditionalSkillDataList ??= [];
            foreach (var skill in GameData.Instance.EventAZXAppleGameSkillTable.Values)
            {
                var item = azxInfo.ConditionalSkillDataList.Find(x => x.SkillId == skill.Id);
                bool isUnlocked = item is not null && item.IsUnlocked;
                if (skill.SkillOpenUseValue == 0) continue;
                response.ConditionalSkillDataList.Add(new NetMiniGameAzxConditionalSkillData()
                {
                    SkillId = skill.Id,
                    IsUnlocked = isUnlocked
                });
            }
        }
        catch (Exception ex)
        {
            log.Error($"Get ConditionalSkillDataList Error: {ex.Message}");
        }

        try
        {
            azxInfo.CutSceneDataList ??= [];
            foreach (var cutScene in GameData.Instance.EventAZXAppleGameCutSceneTable.Values)
            {
                var item = azxInfo.CutSceneDataList.Find(x => x.CutSceneId == cutScene.Id);
                if (item is not null && item.CutSceneId > 0)
                {
                    response.CutSceneList.Add(new NetMiniGameAzxCutSceneData { CutSceneId = cutScene.Id, IsNew = item.IsNew });
                }
            }
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Get CutSceneList Error: {ex.Message}", LogType.Error);
        }

        try
        {
            azxInfo.ScoreDatas ??= [];
            int dateDay = GetDateDay();
            var scoreData = azxInfo.ScoreDatas.Find(x => x.DateDay == dateDay && x.AzxId == azxId);
            if (scoreData is not null)
            {
                response.DailyMissionData = new NetMiniGameAzxDailyMissionData()
                {
                    DailyAccumulatedScore = scoreData.AccumulatedScore,
                    IsDailyRewarded = scoreData.IsDailyRewarded,
                };
            }
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Get DailyMissionData Error: {ex.Message}", LogType.Error);
        }

        response.IsTutorialConfirmed = azxInfo.IsTutorialConfirmed;
    }

    public static void SetTutorialConfirmed(ulong userId, int azxId, MinigameRepository repo, GameContext db)
    {
        log.Debug($"Setting tutorial confirmed for AZX {azxId}");
        UpdateAndSave(userId, azxId, repo, db, "Setting AZX tutorial confirmed failed", azxInfo =>
        {
            azxInfo.IsTutorialConfirmed = true;
            log.Debug($"Tutorial data after: {azxInfo.IsTutorialConfirmed}");
        });
    }

    public static void SetBoardUnlocked(ulong userId, int azxId, int boardId, MinigameRepository repo, GameContext db)
    {
        log.Debug($"Setting board {boardId} unlocked for AZX {azxId}");
        UpdateAndSave(userId, azxId, repo, db, "Setting AZX board unlocked failed", azxInfo =>
        {
            azxInfo.ConditionalBoardDataList ??= [];
            log.Debug($"Board data before: {JsonConvert.SerializeObject(azxInfo.ConditionalBoardDataList)}");
            var itemIndex = azxInfo.ConditionalBoardDataList.FindIndex(x => x.BoardId == boardId);
            if (itemIndex >= 0)
                azxInfo.ConditionalBoardDataList[itemIndex].IsUnlocked = true;
            else
                azxInfo.ConditionalBoardDataList.Add(new ConditionalBoardData() { BoardId = boardId, IsUnlocked = true });
            log.Debug($"Board data after: {JsonConvert.SerializeObject(azxInfo.ConditionalBoardDataList)}");
        });
    }

    public static void SetSkillUnlocked(ulong userId, int azxId, int skillId, MinigameRepository repo, GameContext db)
    {
        log.Debug($"Setting skill {skillId} unlocked for AZX {azxId}");
        UpdateAndSave(userId, azxId, repo, db, "Setting AZX skill unlocked failed", azxInfo =>
        {
            azxInfo.ConditionalSkillDataList ??= [];
            log.Debug($"Skill data before: {JsonConvert.SerializeObject(azxInfo.ConditionalSkillDataList)}");
            var itemIndex = azxInfo.ConditionalSkillDataList.FindIndex(x => x.SkillId == skillId);
            if (itemIndex >= 0)
                azxInfo.ConditionalSkillDataList[itemIndex].IsUnlocked = true;
            else
                azxInfo.ConditionalSkillDataList.Add(new ConditionalSkillData() { SkillId = skillId, IsUnlocked = true });
            log.Debug($"Skill data after: {JsonConvert.SerializeObject(azxInfo.ConditionalSkillDataList)}");
        });
    }

    public static void SetCharacterUnlocked(ulong userId, int azxId, int characterId, MinigameRepository repo, GameContext db)
    {
        log.Debug($"Setting character {characterId} unlocked for AZX {azxId}");
        UpdateAndSave(userId, azxId, repo, db, "Setting AZX character unlocked failed", azxInfo =>
        {
            azxInfo.ConditionalCharacterDataList ??= [];
            log.Debug($"Character data before: {JsonConvert.SerializeObject(azxInfo.ConditionalCharacterDataList)}");
            var itemIndex = azxInfo.ConditionalCharacterDataList.FindIndex(x => x.CharacterId == characterId);
            if (itemIndex >= 0)
                azxInfo.ConditionalCharacterDataList[itemIndex].IsUnlocked = true;
            else
                azxInfo.ConditionalCharacterDataList.Add(new ConditionalCharacterData() { CharacterId = characterId, IsUnlocked = true });
            log.Debug($"Character data after: {JsonConvert.SerializeObject(azxInfo.ConditionalCharacterDataList)}");
        });
    }

    public static void SetCutSceneConfirmed(ulong userId, int azxId, List<int> cutSceneIdList, MinigameRepository repo, GameContext db)
    {
        log.Debug($"Setting cutscenes confirmed for AZX {azxId}: {string.Join(", ", cutSceneIdList)}");
        UpdateAndSave(userId, azxId, repo, db, "Setting AZX cutscene confirmed failed", azxInfo =>
        {
            azxInfo.CutSceneDataList ??= [];
            log.Debug($"Cutscene data before: {JsonConvert.SerializeObject(azxInfo.CutSceneDataList)}");
            foreach (var item in cutSceneIdList)
            {
                var itemIndex = azxInfo.CutSceneDataList.FindIndex(x => x.CutSceneId == item);
                if (itemIndex >= 0)
                    azxInfo.CutSceneDataList[itemIndex].IsNew = false;
                else
                    azxInfo.CutSceneDataList.Add(new CutSceneData() { CutSceneId = item, IsNew = false });
            }
            log.Debug($"Cutscene data after: {JsonConvert.SerializeObject(azxInfo.CutSceneDataList)}");
        });
    }

    private static void UpdateAndSave(
        ulong userId,
        int azxId,
        MinigameRepository repo,
        GameContext db,
        string errorPrefix,
        Action<MiniGameAzxData> apply)
    {
        try
        {
            var azxInfo = GetAzxInfo(userId, azxId, repo);
            apply(azxInfo);
            repo.SaveDictEntry(userId, MinigameRepository.TypeMiniGameAzx, azxId, azxInfo);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"{errorPrefix}: {ex.Message}", LogType.Error);
        }
    }

    private static int GetDateDay()
    {
        // +4 每天4点重新计算
        DateTime dateTime = DateTime.UtcNow.AddHours(4);
        return dateTime.Year * 10000 + dateTime.Month * 100 + dateTime.Day;
    }

    private static void AddAchievementMission(ref ResGetMiniGameAzxData response, int missionId, int progress, bool isReceived)
    {
        response.AchievementMissionDataList.Add(new NetMiniGameAzxAchievementMissionData()
        {
            MissionId = missionId,
            Progress = progress,
            IsReceived = isReceived
        });
    }

    public static MiniGameAzxData GetAzxInfo(ulong userId, int azxId, MinigameRepository repo)
    {
        var azxInfo = repo.GetOrCreateDictEntry<MiniGameAzxData>(userId, MinigameRepository.TypeMiniGameAzx, azxId);
        log.Debug($"Getting AZX info for {azxId}, data: {JsonConvert.SerializeObject(azxInfo)}");
        return azxInfo;
    }
}
