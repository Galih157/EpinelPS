using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Services;

public sealed class LobbyUserMiscService(
    GameContext db,
    ProgressRepository progressRepo,
    OutpostRepository outpostRepo,
    ResetRepository resetRepo,
    GachaShopPassRepository gachaRepo,
    EventsRepository eventsRepo,
    CosmeticsRepository cosmeticsRepo,
    TeamRepository teamRepo)
{
    public ResGetAllFeatureFlags GetAllFeatureFlags()
    {
        var response = new ResGetAllFeatureFlags();
        response.Flags.AddRange([
            new NetFeatureFlag { FeatureKind = NetFeatureKind.UnionRaid, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.CooperationEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.RookieArena, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.SimulationRoom, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.DailyEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.Intercept, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.Attendance, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.Dice, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.StoryDungeonEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.Tower, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.LostSector, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.Subscription, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.ArchiveEventSystem, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.ViewShortCut, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.PollEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.ComeBackUserEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.EventPass, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.FieldHubEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.ShopEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.MissionEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.ChargeGachaEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.MiniGameSortOut, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.CharacterSkillResetEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.EventQuest, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.RewardUpEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.SdbattleEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.TextAdventure, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.ChallengeModeEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.DailyFreeGachaEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.BoxGachaEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.DiceEvent, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.Bbqtycoon, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.Ce002MiniGame, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.SoloRaid, IsOpen = true },
            new NetFeatureFlag { FeatureKind = NetFeatureKind.SoloRaidMuseum, IsOpen = true }
        ]);
        return response;
    }

    public ResGetContentsOpenData GetContentsData(ulong userId)
    {
        // Load all map stage clears once instead of querying per ContentsOpen condition.
        var completedStages = progressRepo.GetFieldInfoMap(userId).Values
            .SelectMany(f => f.CompletedStages)
            .ToHashSet();

        List<int> stages = [];
        foreach (var item in GameData.Instance.ContentsOpenTable)
        {
            foreach (var condition in item.Value.OpenCondition)
            {
                if (condition.OpenConditionType == ContentsOpenCondition.StageClear &&
                    !stages.Contains(condition.OpenConditionValue) &&
                    completedStages.Contains(condition.OpenConditionValue))
                {
                    stages.Add(condition.OpenConditionValue);
                }
            }
        }

        // Required to show mission UI and burst sidebar UI in battle view.
        int[] specialStages = [6000001, 6000003];
        foreach (int item in specialStages)
        {
            if (!stages.Contains(item) && completedStages.Contains(item))
                stages.Add(item);
        }

        var response = new ResGetContentsOpenData
        {
            MaxGachaCount = gachaRepo.GetGachaTotalCount(userId),
            MaxGachaPremiumCount = gachaRepo.GetGachaCountForType(userId, GachaPremiumType.GachaPremium),
            TutorialGachaPlayCount = gachaRepo.GetGachaCountForType(userId, GachaPremiumType.GachaTutorial)
        };
        response.ClearStageList.AddRange(stages);
        response.ClearSimRoomChapterList.AddRange(GetClearSimRoomChapterList(userId));
        return response;
    }

    private List<int> GetClearSimRoomChapterList(ulong userId)
    {
        var clearSimRoomChapterList = new List<int>();
        try
        {
            var simRoomData = resetRepo.GetSimRoomData(userId);
            int currentDifficulty = simRoomData.CurrentDifficulty;
            int currentChapter = simRoomData.CurrentChapter;
            if (currentDifficulty > 0 && currentChapter > 0)
            {
                var chapters = GameData.Instance.SimulationRoomChapterTable.Values
                    .Where(c => c.DifficultyId <= currentDifficulty)
                    .ToList();

                foreach (var chapter in chapters)
                {
                    if (chapter.DifficultyId < currentDifficulty ||
                        (chapter.DifficultyId == currentDifficulty && chapter.Chapter <= currentChapter))
                    {
                        clearSimRoomChapterList.Add(chapter.Id);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Logging.Warn($"GetClearSimRoomChapterList error: {e.Message}");
        }
        return clearSimRoomChapterList;
    }

    public ResGetProfileFrame GetProfileFrame()
    {
        var response = new ResGetProfileFrame();
        foreach (var frameRecord in GameData.Instance.userFrameTable.Values)
        {
            response.Frames.Add(frameRecord.Id);
        }
        return response;
    }

    public ResGetScenarioList GetScenarioList(ulong userId)
    {
        var response = new ResGetScenarioList();
        response.ScenarioList.AddRange(progressRepo.GetCompletedScenarios(userId));
        return response;
    }

    public ResGetUserData GetUser(User user, ReqGetUserData req)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, user.ID);
        TimeSpan battleTime = DateTime.UtcNow - gameUser.BattleTime;
        long battleTimeMs = (long)(battleTime.TotalNanoseconds / 100);

        var response = new ResGetUserData
        {
            User = LobbyHandler.CreateNetUserDataFromUser(user),
            ResetHour = ServerConfigStore.ResetHourUtcTime,
            OutpostBattleTime = new NetOutpostBattleTime
            {
                MaxBattleTime = 864000000000,
                MaxOverBattleTime = 12096000000000,
                BattleTime = battleTimeMs
            },
            OutpostBattleLevel = outpostRepo.GetOutpostBattleLevel(user.ID),
            IsSimple = req.IsSimple
        };

        bool needsSave = false;

        // Auto-affiliate with Counters union (GSN 10001) so the Union Shop is unlocked without manual edits.
        long gsn = user.Guild?.guildId ?? 0;
        if (gsn <= 0)
        {
            gsn = 10001;
            user.Guild ??= new GuildData();
            user.Guild.guildId = 10001;
            user.Guild.LeaveAt = 0;
            needsSave = true;
        }
        response.Gsn = gsn;

        // Ensure player has at least 100,000 Union Chips (GuildCoin) for the Union Shop.
        const long defaultUnionChips = 100000;
        if (user.GetCurrencyVal(CurrencyType.GuildCoin) < defaultUnionChips)
        {
            user.Currency[CurrencyType.GuildCoin] = defaultUnionChips;
            needsSave = true;
        }

        if (needsSave)
        {
            UserSessionStore.SaveUser(user);
        }

        foreach (KeyValuePair<CurrencyType, long> item in user.Currency)
        {
            response.Currency.Add(new NetUserCurrencyData { Type = (int)item.Key, Value = item.Value });
        }

        response.RepresentationTeam = NetUtils.GetDisplayedTeam(user);
        response.LastClearedNormalMainStageId = gameUser.LastNormalStageCleared;
        response.LastClearedStoryStageId = gameUser.LastStoryStageCleared;
        response.LastClearedHardMainStageId = gameUser.LastHardStageCleared;
        response.LastClearedMod = gameUser.LastClearedDifficulty;

        // Replace any stale tutorial entries populated by CreateNetUserDataFromUser with authoritative DB rows.
        response.User.Tutorials.Clear();
        foreach (KeyValuePair<int, ClearedTutorialData> item in progressRepo.GetClearedTutorials(user.ID))
        {
            response.User.Tutorials.Add(new NetTutorialData
            {
                GroupId = item.Key,
                LastClearedTid = item.Value.Id,
                LastClearedVersion = item.Value.VersionGroup
            });
        }

        response.CommanderRoomJukeboxBgm = new NetJukeboxBgm
        {
            JukeboxTableId = cosmeticsRepo.GetJukeboxSetting(user.ID, NetJukeboxLocation.CommanderRoom).TableId,
            Type = NetJukeboxBgmType.JukeboxTableId,
            Location = NetJukeboxLocation.CommanderRoom
        };
        response.LobbyJukeboxBgm = new NetJukeboxBgm
        {
            JukeboxTableId = cosmeticsRepo.GetJukeboxSetting(user.ID, NetJukeboxLocation.Lobby).TableId,
            Type = NetJukeboxBgmType.JukeboxTableId,
            Location = NetJukeboxLocation.Lobby
        };

        return response;
    }

    public ResGetProfileData GetUserProfile(ReqGetProfileData req)
    {
        var response = new ResGetProfileData();
        User? user = UserSessionStore.GetUser((ulong)req.TargetUsn);
        if (user == null)
            return response;

        var targetGameUser = UserDbHelper.GetOrCreateGameUser(db, user.ID);
        var (_, infraCoreLv) = outpostRepo.GetInfraCore(user.ID);
        var (_, synchroDeviceLevel) = outpostRepo.GetSynchroDeviceState(user.ID);
        var synchroSlots = outpostRepo.GetSynchroSlots(user.ID);
        int maxCharLv = user.Characters.Count > 0 ? user.Characters.Max(c => c.Level) : 0;

        response.Data = new NetProfileData
        {
            User = LobbyHandler.CreateWholeUserDataFromDbUser(user),
            LastActionAt = DateTimeOffset.UtcNow.Ticks,
            CostumeCount = cosmeticsRepo.CountUnlockedCosmetics(user.ID, CosmeticsRepository.CategoryCostume),
            InfraCoreLv = infraCoreLv,
            LastCampaignNormalStageId = targetGameUser.LastNormalStageCleared,
            LastCampaignHardStageId = targetGameUser.LastHardStageCleared,
            OutpostOpenState = progressRepo.GetMainQuests(user.ID).ContainsKey(25),
            Desc = targetGameUser.Description,
            SynchroLv = maxCharLv < 200 ? maxCharLv : synchroDeviceLevel,
            SynchroSlotCount = synchroSlots.Count(x => x.CharacterSerialNumber != 0),
            JukeboxCount = cosmeticsRepo.CountUnlockedCosmetics(user.ID, CosmeticsRepository.CategoryJukeboxBgm)
        };

        response.Data.CharacterCount.Add(new NetCharacterCount { Count = user.Characters.Count });

        long[] repTeam = teamRepo.GetRepresentationTeam(user.ID);
        for (int i = 0; i < repTeam.Length; i++)
        {
            CharacterModel? c = user.GetCharacterBySerialNumber(repTeam[i]);
            if (c != null)
            {
                response.Data.ProfileTeam.Add(new NetProfileTeamSlot
                {
                    Slot = i + 1,
                    Default = new()
                    {
                        CostumeId = c.CostumeId,
                        Csn = c.Csn,
                        Grade = c.Grade,
                        Lv = c.Level,
                        Skill1Lv = c.Skill1Lvl,
                        Skill2Lv = c.Skill2Lvl,
                        Tid = c.Tid,
                        UltiSkillLv = c.UltimateLevel
                    }
                });
            }
        }

        foreach (var research in outpostRepo.GetResearchProgress(user.ID))
        {
            response.Data.Recycle.Add(new NetUserRecycleRoomData
            {
                Tid = research.Key,
                Lv = research.Value.Level
            });
        }

        var completedLessons = outpostRepo.GetCompletedTacticAcademyLessons(user.ID);
        if (completedLessons.Count > 0)
        {
            int lastLessonId = completedLessons.Last();
            response.Data.LastTacticAcademyClass = GameData.Instance.GetTacticAcademyLesson(lastLessonId).GroupId;
            response.Data.LastTacticAcademyLesson = lastLessonId;
        }

        return response;
    }

    public ResExistScenario GetUserScenarioExist(ulong userId, ReqExistScenario req)
    {
        var completedScenarios = progressRepo.GetCompletedScenarios(userId).ToHashSet();
        var response = new ResExistScenario();

        foreach (string item in req.ScenarioGroupIds)
        {
            if (completedScenarios.Contains(item) || eventsRepo.HasCompletedEventScenario(userId, item))
            {
                response.ExistGroupIds.Add(item);
            }
        }

        return response;
    }

    public ResGetUserTitleCounterList GetUserTitleCounter()
    {
        var response = new ResGetUserTitleCounterList();
        for (int sub = 1; sub <= 5; sub++)
        {
            response.UserTitleCounterList.Add(new ResGetUserTitleCounterList.Types.NetUserTitleCounter
            {
                Condition = 23,
                SubCondition = sub,
                Count = 10
            });
        }
        return response;
    }

    public ResSetNickname SetNickname(ulong userId, ReqSetNickname req)
    {
        UpdateNickname(userId, req.Nickname);
        return new ResSetNickname
        {
            Result = SetNicknameResult.Okay,
            Nickname = req.Nickname
        };
    }

    public ResSetNicknameFree SetNicknameFree(ulong userId, ReqSetNicknameFree req)
    {
        UpdateNickname(userId, req.Nickname);
        return new ResSetNicknameFree
        {
            Result = SetNicknameResult.Okay,
            Nickname = req.Nickname
        };
    }

    public ResSetNicknameInTutorial SetNicknameInTutorial(ulong userId, ReqSetNicknameInTutorial req)
    {
        UpdateNickname(userId, req.Nickname);
        return new ResSetNicknameInTutorial
        {
            Result = SetNicknameResult.Okay,
            Nickname = req.Nickname
        };
    }

    public ResSetProfileData SetProfileData(ulong userId, ReqSetProfileData req)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.ProfileIconId = req.Icon;
        gameUser.ProfileIconIsPrism = req.IsPrism;
        gameUser.ProfileFrame = req.Frame;
        db.SaveChanges();
        return new ResSetProfileData();
    }

    public ResSetProfileDesc SetProfileDescription(ulong userId, ReqSetProfileDesc req)
    {
        db.Users
            .Where(u => u.ID == userId)
            .ExecuteUpdate(setters => setters.SetProperty(u => u.Description, req.Desc));

        return new ResSetProfileDesc
        {
            Result = SetProfileDescResult.Okay
        };
    }

    public ResSetProfileIcon SetProfileIcon(ulong userId, ReqSetProfileIcon req)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.ProfileIconId = req.Icon;
        gameUser.ProfileIconIsPrism = req.IsPrism;
        db.SaveChanges();
        return new ResSetProfileIcon();
    }

    public ResSetScenarioComplete SetScenarioCompleted(User user, ReqSetScenarioComplete req)
    {
        var response = new ResSetScenarioComplete
        {
            Reward = new NetRewardData()
        };

        if (progressRepo.CompleteStage(user.ID, req.ScenarioId))
        {
            db.SaveChanges();
        }

        if (GameData.Instance.ScenarioRewards.TryGetValue(req.ScenarioId, out ScenarioRewardsRecord? record))
        {
            response.Reward = RewardUtils.RegisterRewardsForUser(user, record.RewardId);
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResSetUserTitle SetUserTitle(ulong userId, ReqSetUserTitle req)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.TitleId = req.UserTitleId;
        db.SaveChanges();
        return new ResSetUserTitle();
    }

    public ResSetTutorial SetTutorial(ulong userId, ReqSetTutorial req)
    {
        var cleared = GameData.Instance.GetTutorialDataById(req.LastClearedTid);
        progressRepo.UpsertTutorial(userId, cleared.GroupId, cleared.Id, cleared.VersionGroup);
        db.SaveChanges();
        return new ResSetTutorial();
    }

    public ResUserOnlineStateLog RecordUserOnlineStateLog(User user)
    {
        user.LastLogin = DateTime.UtcNow;
        return new ResUserOnlineStateLog();
    }

    public ResGetArena GetArena(User user) => new()
    {
        BanInfo = CreateNotImplementedArenaBanInfo(),
        User = new NetArenaData { User = LobbyHandler.CreateWholeUserDataFromDbUser(user) }
    };

    public ResGetArenaBanInfo GetArenaBanInfo() => new()
    {
        RookieArenaBanInfo = CreateNotImplementedArenaBanInfo(),
        SpecialArenaBanInfo = CreateNotImplementedArenaBanInfo()
    };

    public ResShowSpecialArenaReward ShowSpecialArenaReward() => new()
    {
        IsBan = true,
        BanInfo = CreateNotImplementedArenaBanInfo()
    };

    public ResGetSpecialArena GetSpecialArena(User user) => new()
    {
        BanInfo = CreateNotImplementedArenaBanInfo(),
        User = new NetArenaData { User = LobbyHandler.CreateWholeUserDataFromDbUser(user) }
    };

    private void UpdateNickname(ulong userId, string nickname)
    {
        db.Users
            .Where(u => u.ID == userId)
            .ExecuteUpdate(setters => setters.SetProperty(u => u.Nickname, nickname));
    }

    private static NetArenaBanInfo CreateNotImplementedArenaBanInfo()
    {
        var now = DateTimeOffset.UtcNow;
        return new NetArenaBanInfo
        {
            Description = "Not Implemented",
            StartAt = Timestamp.FromDateTimeOffset(now),
            EndAt = Timestamp.FromDateTimeOffset(now.AddYears(10))
        };
    }
}
