using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EpinelPS.LobbyServer.Minigame;


public class MiniGameHelper
{

    /// <summary>
    /// 获取工会排行榜
    /// </summary>
    /// <param name="guildId">工会id</param>
    /// <param name="arcadeId">游戏id</param>
    /// <returns></returns>
    public static IEnumerable<(ArcadeScoreRecord Record, int Rank)> GetFullLeaderboard(long guildId, int arcadeId, int modeId = 0)
    {
        using var db = GameContext.CreateNew();
        var records = db.Set<ArcadeScoreEntity>()
            .AsNoTracking()
            .Where(x => x.ArcadeId == arcadeId && x.GuildId == guildId)
            .ToList();

        if (records.Count == 0)
            return [];

        // 筛选指定 GuildId 的记录，按 UserId 分组求和
        var grouped = records
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = (ulong)g.Key,
                TotalScore = g.Sum(x => x.Score),
                ModeId = modeId  // 如果 ModeId 可能不同，需要根据业务决定
            })
            .OrderByDescending(x => x.TotalScore)
            .ToList();

        // 计算排名
        var result = new List<(ArcadeScoreRecord Record, int Rank)>(grouped.Count);
        for (int i = 0; i < grouped.Count; i++)
        {
            var record = new ArcadeScoreRecord
            {
                UserId = grouped[i].UserId,
                Score = grouped[i].TotalScore,
                ModeId = grouped[i].ModeId,
                ArcadeId = arcadeId,
                GuildId = guildId
            };
            result.Add((record, i + 1));
        }

        return result;
    }

    public static NetMiniGameStellarBladeRankingData GetSBUserRank(ulong userid, int arcadeId)
    {
        IEnumerable<(ArcadeScoreRecord Record, int Rank)>? allBoard = GetArcadeBoard(arcadeId);

        if (allBoard.Count() > 0)
        {
            foreach (var item in allBoard)
            {
                if (item.Record.UserId == userid)
                {

                    NetMiniGameStellarBladeRankingData? rankdata = new NetMiniGameStellarBladeRankingData()
                    {
                        Rank = item.Rank,
                        Score = item.Record.Score,
                        User = LobbyHandler.CreateWholeUserDataFromDbUser(item.Record.UserId)
                    };

                    return rankdata;
                }

            }
        }

        return null;
    }

    public static (ArcadeScoreRecord Record, int Rank)? GetUserRank(ulong userId, int arcadeId)
    {
        var allBoard = GetArcadeBoard(arcadeId);
        return allBoard.FirstOrDefault(x => x.Record.UserId == userId);
    }

    public static (ArcadeScoreRecord Record, int Rank)? GetGuildUserRank(long guildId, ulong userId, int arcadeId)
    {
        var allBoard = GetArcadeBoard(arcadeId);
        return allBoard.FirstOrDefault(x => x.Record.UserId == userId && x.Record.GuildId == guildId);
    }
    /// <summary>
    /// 获取游戏排行榜
    /// </summary>    
    /// <param name="arcadeId">游戏id</param>
    /// <returns></returns>
    public static IEnumerable<(ArcadeScoreRecord Record, int Rank)> GetArcadeBoard(int arcadeId)
    {
        using var db = GameContext.CreateNew();
        var records = db.Set<ArcadeScoreEntity>()
            .AsNoTracking()
            .Where(x => x.ArcadeId == arcadeId)
            .ToList();

        if (records.Count == 0)
            return [];

        // 按 UserId 分组求和，并计算排名
        var grouped = records
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = (ulong)g.Key,
                TotalScore = g.Sum(x => x.Score),
                ModeId = g.First().ModeId,  // 如果 ModeId 可能不同，需要根据业务决定
                GuildId = g.First().GuildId
            })
            .OrderByDescending(x => x.TotalScore)
            .ToList();

        // 计算排名
        var result = new List<(ArcadeScoreRecord Record, int Rank)>(grouped.Count);
        for (int i = 0; i < grouped.Count; i++)
        {
            var record = new ArcadeScoreRecord
            {
                UserId = grouped[i].UserId,
                Score = grouped[i].TotalScore,
                ModeId = grouped[i].ModeId,
                ArcadeId = arcadeId,
                GuildId = grouped[i].GuildId
            };
            result.Add((record, i + 1));
        }

        return result;
    }


    /// <summary>
    /// 插入或更新
    /// </summary>
    public static void InsertOrUpdate(int arcadeId, ulong userId, int guildId, long score, int modeId)
    {
        const string arcadeMinigameType = "Arcade";
        using var db = GameContext.CreateNew();
        long uid = (long)userId;
        long gid = guildId;

        var existingScore = db.Set<ArcadeScoreEntity>().FirstOrDefault(x =>
            x.ArcadeId == arcadeId &&
            x.UserId == uid &&
            x.GuildId == gid &&
            x.ModeId == modeId);

        if (existingScore != null)
        {
            existingScore.Score = score;
        }
        else
        {
            db.Set<ArcadeScoreEntity>().Add(new ArcadeScoreEntity
            {
                UserId = uid,
                GuildId = gid,
                Score = score,
                ArcadeId = arcadeId,
                ModeId = modeId
            });
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var existingLeaderboard = db.Set<MiniGameLeaderboardEntity>().FirstOrDefault(x =>
            x.MinigameType == arcadeMinigameType &&
            x.ArcadeId == arcadeId &&
            x.ModeId == modeId &&
            x.UserId == userId &&
            x.GuildId == gid);

        if (existingLeaderboard != null)
        {
            if (existingLeaderboard.Score != score)
            {
                existingLeaderboard.Score = score;
                existingLeaderboard.UpdatedAt = now;
            }
        }
        else
        {
            UserDbHelper.GetOrCreateGameUser(db, userId);
            db.Set<MiniGameLeaderboardEntity>().Add(new MiniGameLeaderboardEntity
            {
                MinigameType = arcadeMinigameType,
                ArcadeId = arcadeId,
                ModeId = modeId,
                UserId = userId,
                GuildId = gid,
                Score = score,
                UpdatedAt = now
            });
        }

        db.SaveChanges();
    }


    public static void InitStellarBladeData(User user, int arcadeId)
    {
        using var ctx = GameContext.CreateNew();
        InitStellarBladeData(user.ID, arcadeId, new MinigameRepository(ctx), ctx);
    }

    public static StellarBladeDatas InitStellarBladeData(ulong userId, int arcadeId, MinigameRepository repo, GameContext ctx)
    {
        if (repo.TryGetDictEntry<StellarBladeDatas>(userId, MinigameRepository.TypeStellarBlade, arcadeId, out var existing) && existing != null)
            return existing;

        var stellar = new StellarBladeDatas();

        EventSBManagerRecord_Raw? sbm = arcadeId < 1000
            ? GameData.Instance.EventSBManagerTable.Values.FirstOrDefault(m => m.MinigameType == MiniGameSystemType.Normal)
            : GameData.Instance.EventSBManagerTable.Values.FirstOrDefault(m => m.MinigameType == MiniGameSystemType.Arcade);

        if (sbm != null)
        {
            //任务
            List<EventSBMissionRecord_Raw> alist = GameData.Instance.EventSBMissionTable.Values
                .Where(x => x.GroupId == sbm.MissionGroupId && x.MissionCategory == SBMissionCategory.Achievement)
                .ToList();
            foreach (var item in alist)
            {
                stellar.MissionData.Add(new StellarBladeMissionData
                {
                    IsReceived = false,
                    MissionId = item.Id,
                    Progress = 0
                });
            }

            List<EventSBMissionRecord_Raw> dlist = GameData.Instance.EventSBMissionTable.Values
                .Where(x => x.GroupId == sbm.MissionGroupId && x.MissionCategory == SBMissionCategory.DailyMission)
                .ToList();
            foreach (var item in dlist)
            {
                stellar.DailyMissionData.Add(new StellarBladeMissionData
                {
                    IsReceived = false,
                    MissionId = item.Id,
                    Progress = 0
                });
            }

            List<EventSBMissionRecord_Raw> plist = GameData.Instance.EventSBMissionTable.Values
                .Where(x => x.GroupId == sbm.MissionGroupId && x.MissionCategory == SBMissionCategory.DailyPoint)
                .ToList();
            foreach (var item in plist)
            {
                stellar.DailyPointMissionData.Add(new StellarBladeMissionData
                {
                    IsReceived = false,
                    MissionId = item.Id,
                    Progress = 0
                });
            }

            //资源
            foreach (var item in GameData.Instance.EventSBCurrencyTable.Values)
            {
                stellar.Currency.Add(new() { Amount = 0, CurrencyType = (int)item.CurrencyType });
            }

            //人物
            EventSBCharacterRecord_Raw? charrd = GameData.Instance.EventSBCharacterTable.Values
                .FirstOrDefault(c => c.Id == sbm.CharacterId);

            if (charrd != null)
            {
                stellar.CharacterData = new()
                {
                    DefaultAttackSkillId = charrd.DefaultAttackSkillId
                };

                List<int> learnskilllist = GameData.Instance.EventSBCharacterSkillTable.Values
                    .Where(s => s.GroupId == charrd.SkillGroupId && s.IsDefaultLearn == true)
                    .Select(s => s.Id)
                    .ToList();

                stellar.CharacterData.LearnedSkillIdList.AddRange(learnskilllist);
                foreach (SBCharacterEnhanceType type in System.Enum.GetValues<SBCharacterEnhanceType>())
                {
                    stellar.CharacterData.EnhanceDataList.Add(new StellarBladeCharacterData.NetEnhanceData() { EnhanceLevel = 1, EnhanceType = (int)type });
                }
            }

            var first = GameData.Instance.EventSBStageTable.Values
                .FirstOrDefault(s => s.GroupId == sbm.StageGroupId);

            if (first != null)
                stellar.LastEnteredStageId = first.Id;
        }

        repo.SaveDictEntry(userId, MinigameRepository.TypeStellarBlade, arcadeId, stellar);
        ctx.SaveChanges();
        return stellar;
    }


    public static void UpMission(ref StellarBladeDatas stellar, SBMissionType missionType, int missionGroupId,
        int stage = 0, int damage = 0, int buyCount = 0, int usePotion = 0, int PerfectGuard = 0, int PerfectDodge = 0,
        int dailyPoint = 0, int playTime = 0)
    {
        switch (missionType)
        {
            case SBMissionType.StageClear:
                List<EventSBMissionRecord_Raw>? list = GameData.Instance.EventSBMissionTable.Values
                    .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType && x.MissionTargetId == 0).ToList();
                foreach (var item in list)
                {
                    switch (item.MissionCategory)
                    {
                        case SBMissionCategory.DailyMission:
                            StellarBladeMissionData? dmiss = stellar.DailyMissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (dmiss?.IsReceived == false) dmiss.Progress += 1;
                            break;
                        case SBMissionCategory.DailyPoint:
                            break;
                        case SBMissionCategory.Achievement:
                            StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (miss?.IsReceived == false) miss.Progress += 1;
                            break;
                        default:
                            break;
                    }


                }
                EventSBMissionRecord_Raw? spstage = GameData.Instance.EventSBMissionTable.Values
                    .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType && x.MissionTargetId == stage).FirstOrDefault();
                if (spstage != null)
                {
                    StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == spstage.Id);
                    if (miss?.IsReceived == false) miss.Progress += 1;
                }
                break;
            case SBMissionType.PerfectGuard:
                List<EventSBMissionRecord_Raw>? glist = GameData.Instance.EventSBMissionTable.Values
                  .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType).ToList();
                foreach (var item in glist)
                {
                    switch (item.MissionCategory)
                    {
                        case SBMissionCategory.DailyMission:
                            StellarBladeMissionData? dmiss = stellar.DailyMissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (dmiss?.IsReceived == false) dmiss.Progress += PerfectGuard;
                            break;
                        case SBMissionCategory.DailyPoint:
                            break;
                        case SBMissionCategory.Achievement:
                            StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (miss?.IsReceived == false) miss.Progress += PerfectGuard;
                            break;
                        default:
                            break;
                    }
                }
                break;
            case SBMissionType.PerfectDodge:
                List<EventSBMissionRecord_Raw>? dolist = GameData.Instance.EventSBMissionTable.Values
                  .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType).ToList();
                foreach (var item in dolist)
                {
                    switch (item.MissionCategory)
                    {
                        case SBMissionCategory.DailyMission:
                            StellarBladeMissionData? dmiss = stellar.DailyMissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (dmiss?.IsReceived == false) dmiss.Progress += PerfectDodge;
                            break;
                        case SBMissionCategory.DailyPoint:
                            break;
                        case SBMissionCategory.Achievement:
                            StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (miss?.IsReceived == false) miss.Progress += PerfectDodge;
                            break;
                        default:
                            break;
                    }

                }
                break;
            case SBMissionType.Damage:
                List<EventSBMissionRecord_Raw>? dlist = GameData.Instance.EventSBMissionTable.Values
                    .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType).ToList();
                foreach (var item in dlist)
                {
                    switch (item.MissionCategory)
                    {
                        case SBMissionCategory.DailyMission:
                            StellarBladeMissionData? dmiss = stellar.DailyMissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (dmiss?.IsReceived == false) dmiss.Progress += damage;
                            break;
                        case SBMissionCategory.DailyPoint:
                            break;
                        case SBMissionCategory.Achievement:
                            StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (miss?.IsReceived == false) miss.Progress += damage;
                            break;
                        default:
                            break;
                    }
                }
                break;
            case SBMissionType.GainGoldCount:
                break;
            case SBMissionType.BuyItem:
                List<EventSBMissionRecord_Raw>? blist = GameData.Instance.EventSBMissionTable.Values
                    .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType).ToList();
                foreach (var item in blist)
                {
                    StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == item.Id);
                    if (miss?.IsReceived == false) miss.Progress += buyCount;
                }
                break;
            case SBMissionType.PlayTime:
                List<EventSBMissionRecord_Raw>? tlist = GameData.Instance.EventSBMissionTable.Values
                  .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType).ToList();
                foreach (var item in tlist)
                {
                    switch (item.MissionCategory)
                    {
                        case SBMissionCategory.DailyMission:
                            StellarBladeMissionData? dmiss = stellar.DailyMissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (dmiss?.IsReceived == false) dmiss.Progress += playTime;
                            break;
                        case SBMissionCategory.DailyPoint:
                            break;
                        case SBMissionCategory.Achievement:
                            StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (miss?.IsReceived == false) miss.Progress += playTime;
                            break;
                        default:
                            break;
                    }

                }
                break;
            case SBMissionType.UsePotion:
                List<EventSBMissionRecord_Raw>? plist = GameData.Instance.EventSBMissionTable.Values
                   .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType).ToList();
                foreach (var item in plist)
                {
                    switch (item.MissionCategory)
                    {
                        case SBMissionCategory.DailyMission:
                            StellarBladeMissionData? dmiss = stellar.DailyMissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (dmiss?.IsReceived == false) dmiss.Progress += usePotion;
                            break;
                        case SBMissionCategory.DailyPoint:
                            break;
                        case SBMissionCategory.Achievement:
                            StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == item.Id);
                            if (miss?.IsReceived == false) miss.Progress += usePotion;
                            break;
                        default:
                            break;
                    }

                }
                break;
            case SBMissionType.DailyPointReward:
                List<EventSBMissionRecord_Raw>? dailist = GameData.Instance.EventSBMissionTable.Values
                    .Where(x => x.GroupId == missionGroupId && x.MissionType == missionType).ToList();
                foreach (var item in dailist)
                {
                    StellarBladeMissionData? miss = stellar.DailyPointMissionData.FirstOrDefault(m => m.MissionId == item.Id);
                    if (miss?.IsReceived == false) miss.Progress += dailyPoint;
                }
                break;
            default:
                break;
        }
    }


    public static void GetReward(ref NetStellarBladeRewardData ret, ref StellarBladeDatas stellar, int rewardGroupId, int score = 0)
    {
        List<EventSBStageRewardRecord_Raw>? rewards = GameData.Instance.EventSBStageRewardTable.Values
            .Where(f => f.GroupId == rewardGroupId)
            .ToList();

        if (rewards.Count == 0) return;

        // 根据分数获取对应的奖励（单条或多条时按分数筛选）
        EventSBStageRewardRecord_Raw? reward = rewards.Count == 1
            ? rewards[0]
            : rewards.FirstOrDefault(r => score >= r.ScoreMin && (r.ScoreMax == 0 || score <= r.ScoreMax));

        if (reward == null) return;

        // 处理奖励
        switch (reward.RewardType)
        {
            case SBRewardType.SBCurrency:

                EventSBCurrencyRecord_Raw? ctype = GameData.Instance.EventSBCurrencyTable.Values
                    .Where(x => x.Id == reward.RewardId).FirstOrDefault();

                if (ctype == null) break;

                NetStellarBladeCurrency? existing = ret.CurrencyList.FirstOrDefault(c => c.CurrencyType == (int)ctype.CurrencyType);
                if (existing != null)
                {
                    existing.Amount += reward.RewardAmount;
                }
                else
                {
                    ret.CurrencyList.Add(new NetStellarBladeCurrency
                    {
                        CurrencyType = (int)ctype.CurrencyType,
                        Amount = reward.RewardAmount
                    });
                }

                StellarBladeCurrency? currency = stellar.Currency.FirstOrDefault(c => c.CurrencyType == (int)ctype.CurrencyType);
                if (currency != null)
                {
                    currency.Amount += reward.RewardAmount;
                }
                break;
            case SBRewardType.SBItem:
                // TODO: 处理道具奖励
                break;
        }
    }


    public static List<int> GetMissionReward(ref NetStellarBladeRewardData ret, ref StellarBladeDatas stellar, User user, EventSBMissionRecord_Raw mission)
    {
        List<int> result = new List<int>();

        int dateDay = user.GetDateDay();
        switch (mission.RewardType)
        {
            case SBMissionRewardType.DailyPoint:
                List<EventSBMissionRecord_Raw>? glist = GameData.Instance.EventSBMissionTable.Values
                    .Where(x => x.GroupId == mission.GroupId && x.MissionType == SBMissionType.DailyPointReward).ToList();
                foreach (var item in glist)
                {
                    StellarBladeMissionData? miss = stellar.MissionData.FirstOrDefault(m => m.MissionId == item.Id);
                    if (miss != null)
                    {
                        if (stellar.Today == dateDay)
                        {
                            miss.Progress += mission.RewardValue;
                        }
                        else
                        {
                            miss.Progress = mission.RewardValue;
                        }
                    }
                }
                break;
            case SBMissionRewardType.RewardId:
                result.Add(mission.RewardValue);
                break;
            case SBMissionRewardType.SBItem:
                break;
            case SBMissionRewardType.SBCurrency:
                break;
            default:
                break;
        }

        return result;
    }

    public static TowerDefenseData InitTDData(int aid, User user)
    {
        using var ctx = GameContext.CreateNew();
        return InitTDData(aid, user.ID, new MinigameRepository(ctx), ctx);
    }

    public static TowerDefenseData InitTDData(int aid, ulong userId, MinigameRepository repo, GameContext ctx)
    {
        if (repo.TryGetDictEntry<TowerDefenseData>(userId, MinigameRepository.TypeTowerDefense, aid, out var existing) && existing != null)
            return existing;

        TowerDefenseData defenseData = new TowerDefenseData
        {
            ChallengeMaxScore = 0,
            ClearedStageIdList = [],
            ClearedTutorialIdList = [],
            UpgradeCurrency = 0,
            UpgradeIdList = []
        };

        ArcadeManagerRecord_Raw? arcadeid = GameData.Instance.ArcadeManagerTable.Values
            .Where(x => x.Id == aid).FirstOrDefault();
        if (arcadeid != null)
        {
            EventTowerDefenseArcadeManagerRecord_Raw? tddata = GameData.Instance.EventTowerDefenseArcadeManagerTable.Values
                .Where(x => x.Id == arcadeid.GameManagerId).FirstOrDefault();
            if (tddata != null)
            {
                List<EventTowerDefenseMissionRecord>? misslist = GameData.Instance.EventTowerDefenseMissionTable.Values
                    .Where(x => x.ArcadeMissionGroupId == tddata.ArcadeMissionGroupId).ToList();

                foreach (var item in misslist)
                {
                    defenseData.MissionProgressList.Add(new ArcadeTowerDefenseMissionProgress()
                    {
                        CreatedAt = DateTime.UtcNow.Date.ToTimestamp(),
                        MissionTid = item.Id,
                        MissionUid = item.Id,
                        Progress = 0
                    });

                }
            }
        }

        repo.SaveDictEntry(userId, MinigameRepository.TypeTowerDefense, aid, defenseData);
        ctx.SaveChanges();

        return defenseData;
    }

    public static void GetDungeonRankByScore(DragonDungeonRunData data, int score)
    {
        var manager = GameData.Instance.EventDragonDungeonRunManagerTable.Values
            .FirstOrDefault(x => x.MinigameType == MiniGameSystemType.Arcade);
        if (manager == null) return;

        List<EventDragonDungeonRunCutSceneRecord_Raw>? scenetable = GameData.Instance.EventDragonDungeonRunCutSceneTable.Values
            .Where(x => x.CutscenePrintTiming == EventDragonDungeonRunCutScenePrintTiming.Result).ToList();

        // 排名配置
        var rankMap = new (string Rank, int TargetPoint)[]
        {
        ("S", manager.SRankTargetPoint),
        ("A", manager.ARankTargetPoint),
        ("B", manager.BRankTargetPoint),
        ("C", manager.CRankTargetPoint),
        ("D", manager.DRankTargetPoint),
        ("F", 0)
        };

        string rank = rankMap.FirstOrDefault(r => score >= r.TargetPoint).Rank;

        // 直接根据 rank 查找对应的 CutScene
        EventDragonDungeonRunCutSceneRecord_Raw? cutscene = rank switch
        {
            "S" => scenetable.FirstOrDefault(x => x.IsAppearInSRank),
            "A" => scenetable.FirstOrDefault(x => x.IsAppearInARank),
            "B" => scenetable.FirstOrDefault(x => x.IsAppearInBRank),
            "C" => scenetable.FirstOrDefault(x => x.IsAppearInCRank),
            "D" => scenetable.FirstOrDefault(x => x.IsAppearInDRank),
            "F" => scenetable.FirstOrDefault(x => x.IsAppearInFRank),
            _ => null
        };

        if (cutscene != null)
        {
            if (!data.CutSceneList.TryAdd(cutscene.Id, true))
            {
                data.CutSceneList[cutscene.Id] = false;
            }
            else
            {
                data.HasUnconfirmedAlbum = true;
            }
        }
    }


    public static NetArcadeBBQData BBQToNet(ArcadeBBQData bBQData)
    {
        NetArcadeBBQData data = new()
        {
            ArcadeId = bBQData.ArcadeId,
            HighScore = bBQData.HighScore,
            PlayCount = bBQData.PlayCount,
            TotalAccumulatedScore = bBQData.TotalAccumulatedScore
        };
        data.RecordedCutSceneList.AddRange(bBQData.RecordedCutSceneList);
        data.StepUpRewardedList.AddRange(bBQData.StepUpRewardedList);

        return data;
    }
    /// <summary>
    /// 将自定义数据转换为 Protobuf 类型
    /// </summary>
    public static TProto ToProto<TProto, TData>(TData data) where TProto : class, new()
    {
        if (data == null) return null;

        var proto = new TProto();
        var dataProps = typeof(TData).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var protoProps = typeof(TProto).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var dataProp in dataProps)
        {
            var protoProp = protoProps.FirstOrDefault(p => p.Name == dataProp.Name);
            if (protoProp == null) continue;

            // List<T> → RepeatedField<T>
            if (IsListType(dataProp.PropertyType) && IsRepeatedFieldType(protoProp.PropertyType))
            {
                var dataList = dataProp.GetValue(data) as System.Collections.IEnumerable;
                if (dataList == null) continue;

                var repeatedField = protoProp.GetValue(proto);
                if (repeatedField == null) continue;

                var addRange = repeatedField.GetType().GetMethod("AddRange");
                if (addRange != null)
                {
                    addRange.Invoke(repeatedField, [dataList]);
                }
                continue;
            }

            // 检查类型兼容性（跳过不兼容的）
            if (!protoProp.PropertyType.IsAssignableFrom(dataProp.PropertyType))
                continue;

            var value = dataProp.GetValue(data);
            protoProp.SetValue(proto, value);
        }

        return proto;
    }

    private static bool IsRepeatedFieldType(System.Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Google.Protobuf.Collections.RepeatedField<>);
    }

    private static bool IsListType(System.Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>);
    }
    /// <summary>
    /// 将 Protobuf 类型转换为自定义数据
    /// </summary>
    public static TData FromProto<TData, TProto>(TProto proto) where TData : class, new()
    {
        if (proto == null) return null;

        var data = new TData();
        var protoProps = typeof(TProto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dataProps = typeof(TData).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var protoProp in protoProps)
        {
            var dataProp = dataProps.FirstOrDefault(p => p.Name == protoProp.Name);
            if (dataProp == null) continue;

            // RepeatedField<T> → List<T>
            if (IsRepeatedFieldType(protoProp.PropertyType))
            {
                var protoList = protoProp.GetValue(proto) as System.Collections.IEnumerable;
                if (protoList == null) continue;

                var elementType = protoProp.PropertyType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = Activator.CreateInstance(listType) as System.Collections.IList;

                foreach (var item in protoList)
                {
                    list.Add(item);
                }

                dataProp.SetValue(data, list);
                continue;
            }

            // 普通类型
            if (!dataProp.PropertyType.IsAssignableFrom(protoProp.PropertyType))
                continue;

            var value = protoProp.GetValue(proto);
            dataProp.SetValue(data, value);
        }

        return data;
    }

    // ============ 列表转换 ============

    /// <summary>
    /// 转换 List<TData> → List<TProto>
    /// </summary>
    public static List<TProto> ToProtoList<TProto, TData>(List<TData> dataList) where TProto : class, new()
    {
        if (dataList == null) return null;

        var result = new List<TProto>();
        foreach (var item in dataList)
        {
            result.Add(ToProto<TProto, TData>(item));
        }
        return result;
    }

    /// <summary>
    /// 转换 List<TProto> → List<TData>
    /// </summary>
    public static List<TData> FromProtoList<TData, TProto>(List<TProto> protoList) where TData : class, new()
    {
        if (protoList == null) return null;

        var result = new List<TData>();
        foreach (var item in protoList)
        {
            result.Add(FromProto<TData, TProto>(item));
        }
        return result;
    }

    // ============ Dictionary 转换 ============

    /// <summary>
    /// 转换 Dictionary<TKey, TData> → Dictionary<TKey, TProto>
    /// </summary>
    public static Dictionary<TKey, TProto> ToProtoDict<TKey, TProto, TData>(Dictionary<TKey, TData> dict)
        where TProto : class, new() where TKey : notnull
    {
        if (dict == null) return null;

        var result = new Dictionary<TKey, TProto>();
        foreach (var kvp in dict)
        {
            result[kvp.Key] = ToProto<TProto, TData>(kvp.Value);
        }
        return result;
    }

    /// <summary>
    /// 转换 Dictionary<TKey, TProto> → Dictionary<TKey, TData>
    /// </summary>
    public static Dictionary<TKey, TData> FromProtoDict<TKey, TData, TProto>(Dictionary<TKey, TProto> dict)
        where TData : class, new() where TKey : notnull
    {
        if (dict == null) return null;

        var result = new Dictionary<TKey, TData>();
        foreach (var kvp in dict)
        {
            result[kvp.Key] = FromProto<TData, TProto>(kvp.Value);
        }
        return result;
    }


    public static bool IsLandGetComplete(Dictionary<int, MiniGameIslandBreakerMission> Missions)
    {
        if (Missions.Count > 0)
        {
            foreach (var item in Missions)
            {
                var mission = GameData.Instance.EventIslandBreakerMissionTable.Values
                    .Where(x => x.Id == item.Key).FirstOrDefault();
                if (mission != null)
                {
                    if (item.Value.Progress >= mission.ConditionValue && item.Value.Rewarded == false)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

public static class StellarBladeExtensions
{
    public static NetStellarBladeCharacterData ToProto(this StellarBladeCharacterData data)
    {
        if (data == null) return null;

        var proto = new NetStellarBladeCharacterData
        {
            DefaultAttackSkillId = data.DefaultAttackSkillId,
            Gear1SbItemId = data.Gear1SbItemId,
            Gear2SbItemId = data.Gear2SbItemId,
            ExoSpineSbItemId = data.ExoSpineSbItemId
        };

        // 添加列表
        foreach (var id in data.LearnedSkillIdList)
        {
            proto.LearnedSkillIdList.Add(id);
        }

        // 添加增强数据（嵌套类型需要单独处理）
        foreach (var enhance in data.EnhanceDataList)
        {
            proto.EnhanceDataList.Add(new NetStellarBladeCharacterData.Types.NetEnhanceData
            {
                EnhanceType = enhance.EnhanceType,
                EnhanceLevel = enhance.EnhanceLevel
            });
        }

        return proto;
    }

    public static StellarBladeCharacterData FromProto(this NetStellarBladeCharacterData proto)
    {
        if (proto == null) return null;

        var data = new StellarBladeCharacterData
        {
            DefaultAttackSkillId = proto.DefaultAttackSkillId,
            Gear1SbItemId = proto.Gear1SbItemId,
            Gear2SbItemId = proto.Gear2SbItemId,
            ExoSpineSbItemId = proto.ExoSpineSbItemId
        };

        data.LearnedSkillIdList.AddRange(proto.LearnedSkillIdList);

        foreach (var enhance in proto.EnhanceDataList)
        {
            data.EnhanceDataList.Add(new StellarBladeCharacterData.NetEnhanceData
            {
                EnhanceType = enhance.EnhanceType,
                EnhanceLevel = enhance.EnhanceLevel
            });
        }

        return data;
    }
}
