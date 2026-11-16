using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Stage;

public static class ClearStage
{
    public static ResClearStage CompleteStage(
        User user,
        int stageId,
        bool forceCompleteScenarios = false,
        bool recordMainQuest = true)
    {
        using var ctx = GameContext.CreateNew();
        return CompleteStage(
            user,
            stageId,
            ctx,
            new ProgressRepository(ctx),
            new OutpostRepository(ctx),
            forceCompleteScenarios,
            recordMainQuest);
    }

    public static ResClearStage CompleteStage(
        User user,
        int stageId,
        GameContext db,
        ProgressRepository progressRepo,
        OutpostRepository outpostRepo,
        bool forceCompleteScenarios = false,
        bool recordMainQuest = true)
    {
        ResClearStage response = new()
        {
            OutpostTimeRewardBuff = new()
        };
        CampaignStageRecord clearedStage = GameData.Instance.GetStageData(stageId)
            ?? throw new Exception("cleared stage cannot be null");

        string stageMapId = GameData.Instance.GetMapIdFromChapter(clearedStage.ChapterId, clearedStage.ChapterMod);

        DoQuestSpecificUserOperations(user, stageId, progressRepo, recordMainQuest);
        RewardRecord? rewardData = GameData.Instance.GetRewardTableEntry(clearedStage.RewardId);

        if (forceCompleteScenarios)
        {
            List<string> scenarios = [];
            if (!string.IsNullOrWhiteSpace(clearedStage.EnterScenario))
                scenarios.Add(clearedStage.EnterScenario);
            if (!string.IsNullOrWhiteSpace(clearedStage.ExitScenario))
                scenarios.Add(clearedStage.ExitScenario);
            if (scenarios.Count > 0)
                progressRepo.CompleteStages(user.ID, scenarios);
        }

        int oldLevel = user.userPointData.UserLevel;
        int oldOutpostLevel = outpostRepo.GetOutpostBattleLevel(user.ID).Level;

        if (rewardData != null)
            response.StageClearReward = RewardUtils.RegisterRewardsForUser(user, rewardData);
        else
            Console.WriteLine("rewardId is null for stage " + stageId);

        response.ScenarioReward = new NetRewardData() { PassPoint = new() };
        response.OutpostBattleLevelReward = new NetRewardData() { PassPoint = new() };

        // Check if user level changed, if so return the reward
        if (user.userPointData.UserLevel != oldLevel)
        {
            response.UserLevelUpReward = new NetRewardData();
            response.UserLevelUpReward.Currency.Add(new NetCurrencyData()
            {
                Type = (int)CurrencyType.FreeCash,
                Value = 30 * (user.userPointData.UserLevel - oldLevel),
                FinalValue = user.GetCurrencyVal(CurrencyType.FreeCash)
            });
        }

        // Check if outpost level changed, if so return the reward
        int currentOutpostLevel = outpostRepo.GetOutpostBattleLevel(user.ID).Level;
        if (currentOutpostLevel != oldOutpostLevel)
        {
            response.OutpostBattleLevelReward = new NetRewardData();
            response.OutpostBattleLevelReward.Currency.Add(new NetCurrencyData()
            {
                Type = (int)CurrencyType.FreeCash,
                Value = 100 * (currentOutpostLevel - oldOutpostLevel),
                FinalValue = user.GetCurrencyVal(CurrencyType.FreeCash)
            });
        }

        UserDbHelper.UpdateGameUser(user.ID, gameUser =>
        {
            if (clearedStage.StageCategory == StageCategory.Normal ||
                clearedStage.StageCategory == StageCategory.Boss ||
                clearedStage.StageCategory == StageCategory.Hard ||
                clearedStage.StageCategory == StageCategory.Story)
            {
                if (clearedStage.ChapterMod == ChapterMod.Hard)
                {
                    if (stageId > gameUser.LastHardStageCleared)
                        gameUser.LastHardStageCleared = stageId;
                }
                else if (clearedStage.ChapterMod == ChapterMod.Story)
                {
                    if (stageId > gameUser.LastStoryStageCleared)
                        gameUser.LastStoryStageCleared = stageId;
                }
                else if (clearedStage.ChapterMod == ChapterMod.Normal)
                {
                    if (stageId > gameUser.LastNormalStageCleared)
                        gameUser.LastNormalStageCleared = stageId;
                }
                else throw new NotImplementedException();
            }
            else
            {
                Logging.Warn("Unknown stage category " + clearedStage.StageCategory);
            }

            gameUser.LastClearedDifficulty = (int)clearedStage.ChapterMod;
        });

        if (clearedStage.StageType != StageType.Sub && clearedStage.ChapterMod != ChapterMod.Story)
        {
            // add outpost reward level if unlocked
            if (progressRepo.GetMainQuests(user.ID).ContainsKey(21))
            {
                response.OutpostBattle = outpostRepo.AddOutpostBattleExp(user.ID);
                if (response.OutpostBattle.IsLevelUp)
                {
                    user.AddCurrency(CurrencyType.FreeCash, 100); // todo is reward the same for all level upgrades
                }
            }
        }

        // Mark chapter as completed if boss stage was completed
        if (clearedStage.StageCategory == StageCategory.Boss && clearedStage.StageType == StageType.Main)
        {
            if (clearedStage.ChapterMod == ChapterMod.Hard)
                user.AddTrigger(Trigger.HardChapterClear, 1, clearedStage.ChapterId);
            else
                user.AddTrigger(Trigger.ChapterClear, 1, clearedStage.ChapterId);
        }

        progressRepo.CompleteStage(user.ID, stageMapId, stageId);
        db.SaveChanges();

        // Subquests like Tetra Connect require clearing every stage of a stage
        // group, the client waits for this trigger to play the ending scenario.
        if (clearedStage.GroupId != 0)
        {
            bool hasGroupClear = db.Triggers.Any(t =>
                t.UserId == user.ID &&
                t.Type == Trigger.CampaignGroupClear &&
                t.ConditionId == clearedStage.GroupId);

            if (!hasGroupClear)
            {
                var completedStageIds = progressRepo.GetFieldInfoMap(user.ID).Values
                    .SelectMany(f => f.CompletedStages)
                    .ToHashSet();

                bool groupCleared = GameData.Instance.StageDataRecords.Values
                    .Where(s => s.GroupId == clearedStage.GroupId)
                    .All(s => completedStageIds.Contains(s.Id));

                if (groupCleared)
                    user.AddTrigger(Trigger.CampaignGroupClear, 1, clearedStage.GroupId);
            }
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    private static void DoQuestSpecificUserOperations(
        User user,
        int clearedStageId,
        ProgressRepository progressRepo,
        bool recordMainQuest = true)
    {
        MainQuestRecord? quest = GameData.Instance.GetMainQuestForStageClearCondition(clearedStageId);

        user.AddTrigger(Trigger.CampaignClear, 1, clearedStageId);
        if (recordMainQuest)
        {
            if (quest != null)
            {
                progressRepo.UpsertMainQuest(user.ID, quest.Id, false);
                user.AddTrigger(Trigger.MainQuestClear, 1, quest.Id);
            }
            else
            {
                // Some stages don't have quest records but are needed by messenger conditions.
                // Record MainQuestClear with the stageId as a fallback.
                bool neededByMessenger = GameData.Instance.MessageConditions.Values.Any(c =>
                    c.TriggerList?.Any(t => t.Trigger == Trigger.MainQuestClear && t.ConditionId == clearedStageId) == true);
                if (neededByMessenger)
                {
                    user.AddTrigger(Trigger.MainQuestClear, 1, clearedStageId);
                }
            }
        }

        if (clearedStageId == 6000002 || clearedStageId == 6001004)
        {
            EnsureDefaultCharacters(user);
        }
    }

    public static void EnsureDefaultCharacters(User user)
    {
        using var ctx = GameContext.CreateNew();
        var teamRepo = new TeamRepository(ctx);
        bool hasTeam1 = teamRepo.HasTeamType(user.ID, 1);

        if (user.Characters.Count > 0 && hasTeam1)
            return;

        bool charactersAdded = false;
        if (user.Characters.Count == 0)
        {
            user.Characters.Add(new CharacterModel() { Csn = 47263455, Tid = 201001 });
            user.Characters.Add(new CharacterModel() { Csn = 47273456, Tid = 330501 });
            user.Characters.Add(new CharacterModel() { Csn = 47263457, Tid = 130201 });
            user.Characters.Add(new CharacterModel() { Csn = 47263458, Tid = 230101 });
            user.Characters.Add(new CharacterModel() { Csn = 47263459, Tid = 301201 });
            charactersAdded = true;

            var bondRepo = new BondLiberationRepository(ctx);
            bondRepo.EnsureBond(user.ID, 3001, 1);
            bondRepo.EnsureBond(user.ID, 3005, 1);

            user.AddTrigger(Trigger.ObtainCharacter, 1, 3001);
            user.AddTrigger(Trigger.ObtainCharacter, 1, 1018);
            user.AddTrigger(Trigger.ObtainCharacter, 1, 1015);
            user.AddTrigger(Trigger.ObtainCharacter, 1, 1014);
            user.AddTrigger(Trigger.ObtainCharacter, 1, 3005);
            user.AddTrigger(Trigger.ObtainCharacterNew, 1);
        }

        if (!hasTeam1)
        {
            NetTeamData team1Sub = new()
            {
                TeamNumber = 1
            };

            for (int i = 1; i <= Math.Min(5, user.Characters.Count); i++)
            {
                CharacterModel character = user.Characters[i - 1];
                team1Sub.Slots.Add(new NetTeamSlot() { Slot = i, Value = character.Csn });
            }
            teamRepo.UpsertTeamGroup(user.ID, 1, 1, [team1Sub]);
        }

        if (teamRepo.GetRepresentationTeam(user.ID).Length == 0 && user.Characters.Count >= 5)
        {
            teamRepo.SetRepresentationTeam(user.ID, [
                user.Characters[0].Csn,
                user.Characters[1].Csn,
                user.Characters[2].Csn,
                user.Characters[3].Csn,
                user.Characters[4].Csn
            ]);
        }

        ctx.SaveChanges();
        if (charactersAdded)
            UserSessionStore.SaveUser(user);
    }

    public static List<int> GetCompletedQuestsForStage(int targetStageId)
    {
        List<int> result = [];
        int currentId = 1;
        while (currentId != 0 && currentId != 9999)
        {
            if (!GameData.Instance.QuestDataRecords.TryGetValue(currentId, out var quest))
                break;

            int stageCondition = (quest.ConditionId != null && quest.ConditionId.Count > 0)
                ? quest.ConditionId[0].ConditionId
                : 0;

            if (quest.Category == Category.CampaignClear && stageCondition > targetStageId)
                break;

            result.Add(quest.Id);

            if (quest.Category == Category.CampaignClear && stageCondition == targetStageId)
                break;

            currentId = quest.NextMainQuestId;
        }
        return result;
    }

    public static void ReconcileMainQuests(User user, bool logToConsole = true)
    {
        int lastNormalStage = UserDbHelper.GetOrCreateGameUser(user.ID).LastNormalStageCleared;
        if (lastNormalStage == 0)
            return;

        List<int> validQuests = GetCompletedQuestsForStage(lastNormalStage);
        if (validQuests.Count == 0)
            return;

        HashSet<int> validSet = [.. validQuests];

        using var progressCtx = GameContext.CreateNew();
        var progressRepo = new ProgressRepository(progressCtx);
        var mainQuests = progressRepo.GetMainQuests(user.ID);

        // Also allow sequential intermediate quests (e.g. scenarios, outpost visits) that the user completed
        // after the last cleared stage along the chain, until an uncleared stage requirement is reached.
        if (validQuests.Count > 0)
        {
            int lastQuestId = validQuests[^1];
            if (GameData.Instance.QuestDataRecords.TryGetValue(lastQuestId, out var lastQuest))
            {
                int currentId = lastQuest.NextMainQuestId;
                while (currentId != 0 && currentId != 9999)
                {
                    if (!GameData.Instance.QuestDataRecords.TryGetValue(currentId, out var quest))
                        break;

                    if (quest.Category == Category.CampaignClear)
                        break;

                    if (mainQuests.ContainsKey(currentId))
                    {
                        validSet.Add(currentId);
                        currentId = quest.NextMainQuestId;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // Remove any quests that are beyond the user's progress
        List<int> toRemove = mainQuests.Keys.Where(k => !validSet.Contains(k)).ToList();
        if (toRemove.Count > 0)
            progressRepo.RemoveMainQuests(user.ID, toRemove);

        // Ensure all valid quests up to current progress are present
        bool anyAdded = false;
        foreach (int questId in validQuests)
        {
            if (!mainQuests.ContainsKey(questId))
            {
                progressRepo.UpsertMainQuest(user.ID, questId, false);
                anyAdded = true;
            }
        }

        if (toRemove.Count > 0 || anyAdded)
            progressCtx.SaveChanges();

        // Clean up any stale MainQuestClear triggers in database
        try
        {
            var badTriggers = progressCtx.Triggers
                .Where(t => t.UserId == user.ID && t.Type == Trigger.MainQuestClear && t.ConditionId <= 9999 && !validSet.Contains(t.ConditionId))
                .ToList();
            if (badTriggers.Count > 0)
            {
                progressCtx.Triggers.RemoveRange(badTriggers);
                progressCtx.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Logging.Warn($"Failed to clean up stale triggers for user {user.ID}: {ex.Message}");
        }

        // Reconcile ChapterClear triggers for all completed normal chapters
        try
        {
            var existingChapterTriggers = progressCtx.Triggers
                .Where(t => t.UserId == user.ID && t.Type == Trigger.ChapterClear)
                .Select(t => t.ConditionId)
                .ToHashSet();

            var chapters = GameData.Instance.ChapterCampaignData.Values;
            int maxChapter = chapters.Count > 0 ? chapters.Max(c => c.Chapter) : 0;

            for (int c = 1; c <= maxChapter; c++)
            {
                List<CampaignStageRecord> chapterStages = AdminCommands.GetNormalMainStages(c);
                CampaignStageRecord? bossStage = chapterStages.LastOrDefault();
                if (bossStage != null && progressRepo.IsStageCompleted(user.ID, bossStage.Id))
                {
                    if (!existingChapterTriggers.Contains(bossStage.ChapterId))
                    {
                        user.AddTrigger(Trigger.ChapterClear, 1, bossStage.ChapterId, logToConsole);
                        existingChapterTriggers.Add(bossStage.ChapterId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Warn($"Failed to reconcile ChapterClear triggers for user {user.ID}: {ex.Message}");
        }
    }
}
