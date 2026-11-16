using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Messenger;
using EpinelPS.LobbyServer.Stage;
using EpinelPS.Repositories;
using EpinelPS.Utils;

namespace EpinelPS.Services;

public sealed class StageQuestService(
    GameContext db,
    ProgressRepository progressRepo,
    OutpostRepository outpostRepo)
{
    // --- Stage ---

    public ResFastClearCampaignStage FastClearStage(User user, ReqFastClearCampaignStage req)
    {
        Console.WriteLine($"Stage {req.CampaignStageId} completed using quick battle");

        ResClearStage rsp = ClearStage.CompleteStage(user, req.CampaignStageId, db, progressRepo, outpostRepo);

        ResFastClearCampaignStage response = new()
        {
            OutpostBattle = rsp.OutpostBattle,
            OutpostBattleLevelReward = rsp.OutpostBattleLevelReward,
            StageClearReward = rsp.StageClearReward,
            UserLevelUpReward = rsp.UserLevelUpReward,
            OutpostTimeRewardBuff = new()
        };

        if (rsp.OutpostTimeRewardBuff != null)
        {
            response.OutpostTimeRewardBuff.TimeRewardBuffs.AddRange(rsp.OutpostTimeRewardBuff.TimeRewardBuffs);
        }

        return response;
    }

    public ResCheckStageClear CheckCleared(ulong userId, ReqCheckStageClear req)
    {
        var response = new ResCheckStageClear();
        var completedStages = progressRepo.GetFieldInfoMap(userId).Values
            .SelectMany(f => f.CompletedStages)
            .ToHashSet();

        foreach (int stageId in req.StageIds.Distinct())
        {
            if (completedStages.Contains(stageId))
                response.ClearedStageIds.Add(stageId);
        }

        return response;
    }

    public ResGetStageData GetStage(ulong userId, ReqGetStageData req)
    {
        string mapId = GameData.Instance.GetMapIdFromChapter(req.Chapter, (ChapterMod)req.Mod);

        return new ResGetStageData
        {
            Field = CreateFieldInfo(userId, mapId, progressRepo, db, out bool bossEntered),
            HasChapterBossEntered = bossEntered,
            SquadData = ""
        };
    }

    public static NetFieldObjectData CreateFieldInfo(
        ulong userId,
        string mapId,
        ProgressRepository progressRepo,
        GameContext db,
        out bool bossEntered)
    {
        FieldInfoNew? fieldInfo = progressRepo.GetFieldInfo(userId, mapId);
        if (fieldInfo == null)
        {
            fieldInfo = new FieldInfoNew();
            progressRepo.SaveFieldInfo(userId, mapId, fieldInfo);
            db.SaveChanges();
        }

        NetFieldObjectData result = new();
        foreach (int stage in fieldInfo.CompletedStages)
            result.Stages.Add(new NetFieldStageData { StageId = stage });

        foreach (NetFieldObject obj in fieldInfo.CompletedObjects)
            result.Objects.Add(obj);

        bossEntered = fieldInfo.BossEntered;
        return result;
    }

    public ResGetStageClearInfo GetStageClearInfo(ulong userId)
    {
        var response = new ResGetStageClearInfo();
        response.Historys.AddRange(progressRepo.GetStageClearHistory(userId));
        return response;
    }

    public ResClearStage ClearCampaignStage(User user, ReqClearStage req)
    {
        Console.WriteLine($"Stage {req.StageId} completed, result is {req.BattleResult}");

        // TODO: check if user has already cleared this stage
        if (req.BattleResult == 1)
        {
            return ClearStage.CompleteStage(user, req.StageId, db, progressRepo, outpostRepo);
        }

        return new ResClearStage();
    }

    public ResEnterStage EnterStage(User user, ReqEnterStage req)
    {
        CampaignStageRecord clearedStage = GameData.Instance.GetStageData(req.StageId)
            ?? throw new Exception("cleared stage cannot be null");
        string map = GameData.Instance.GetMapIdFromChapter(clearedStage.ChapterId, clearedStage.ChapterMod);

        if (clearedStage.StageCategory == StageCategory.Boss)
        {
            // When entering a boss stage, unlock boss information in campaign
            var existingField = progressRepo.GetFieldInfo(user.ID, map);
            var fieldInfo = existingField ?? new FieldInfoNew();
            if (existingField == null || !fieldInfo.BossEntered)
            {
                fieldInfo.BossEntered = true;
                progressRepo.SaveFieldInfo(user.ID, map, fieldInfo);
                db.SaveChanges();
            }
        }

        user.AddTrigger(Trigger.CampaignStart, 1, req.StageId);
        return new ResEnterStage();
    }

    // --- Trigger & Main Quest ---

    public ResFinMainQuest FinishMainQuest(User user, ReqFinMainQuest req)
    {
        Console.WriteLine("Complete quest: " + req.Tid);

        var completedQuest = GameData.Instance.GetMainQuestByTableId(req.Tid)
            ?? throw new Exception("Quest not found");

        progressRepo.UpsertMainQuest(user.ID, req.Tid, false);
        db.SaveChanges();

        if (completedQuest.ConditionId != null && completedQuest.ConditionId.Count > 0)
        {
            user.AddTrigger(Trigger.CampaignClear, 1, completedQuest.ConditionId[0].ConditionId);
        }
        user.AddTrigger(Trigger.MainQuestClear, 1, completedQuest.Id);

        return new ResFinMainQuest();
    }

    public ResGetMainQuestData GetMainQuestData(ulong userId)
    {
        var response = new ResGetMainQuestData();
        foreach (KeyValuePair<int, bool> item in progressRepo.GetMainQuests(userId))
        {
            response.MainQuestList.Add(new NetMainQuestData { IsReceived = item.Value, Tid = item.Key });
        }
        return response;
    }

    public ResObtainMainQuestReward ObtainMainQuestReward(User user, ReqObtainMainQuestReward req)
    {
        var response = new ResObtainMainQuestReward();
        List<NetRewardData> rewards = [];
        var mainQuests = progressRepo.GetMainQuests(user.ID);
        bool anyUpdated = false;

        foreach (KeyValuePair<int, bool> item in mainQuests)
        {
            // give only rewards for things that were completed and not claimed already
            if (!item.Value && req.TidList.Contains(item.Key))
            {
                progressRepo.UpsertMainQuest(user.ID, item.Key, true);
                anyUpdated = true;

                MainQuestRecord questInfo = GameData.Instance.GetMainQuestByTableId(item.Key)
                    ?? throw new Exception("failed to lookup quest Id " + item.Key);
                RewardRecord reward = GameData.Instance.GetRewardTableEntry(questInfo.RewardId)
                    ?? throw new Exception("failed to lookup reward Id " + questInfo.RewardId);
                rewards.Add(RewardUtils.RegisterRewardsForUser(user, reward));
            }
        }

        if (anyUpdated)
            db.SaveChanges();

        response.Reward = NetUtils.MergeRewards(rewards, user);

        foreach (NetItemData? item in response.Reward.Item)
        {
            Console.WriteLine($"item: {item.Tid} {item.Isn} {item.Count}");
        }

        if (anyUpdated)
            UserSessionStore.SaveUser(user);

        return response;
    }

    public ResSyncTrigger SyncTriggers(ulong userId, ReqSyncTrigger req)
    {
        var response = new ResSyncTrigger();
        Logging.WriteLine($"[TriggerSync] User {userId} requested trigger sync from seq {req.Seq}", LogType.Debug);

        long maxId = db.Triggers
            .Where(x => x.UserId == userId)
            .Select(x => (long?)x.Id)
            .Max() ?? 0;

        long effectiveSeq = req.Seq;
        if (effectiveSeq > maxId)
        {
            // The client's cached sequence is ahead of the database (e.g. after trigger cleanup or save restore).
            // Request the client to restart trigger sync from 0.
            Logging.WriteLine($"[TriggerSync] Client seq ({req.Seq}) > maxId ({maxId}) for user {userId}; requesting restart from 0", LogType.Info);
            response.Restart = true;
            effectiveSeq = 0;
        }

        // Look for triggers past that amount in ascending ID order
        TriggerModelNew[] newTriggers = [.. db.Triggers
            .Where(x => x.Id > effectiveSeq && x.UserId == userId)
            .OrderBy(x => x.Id)];

        int triggerCount = 0;
        foreach (TriggerModelNew item in newTriggers)
        {
            triggerCount++;
            response.Triggers.Add(item.ToNet());

            if (triggerCount >= 2000)
            {
                response.HasRemainData = true;
                break;
            }
        }

        return response;
    }

    // --- Subquest ---

    public ResEnrollmentSubQuest EnrollSubquest(User user, ReqEnrollmentSubQuest req)
    {
        if (!GameData.Instance.Subquests.TryGetValue(req.SubquestId, out SubQuestRecord? subQuest))
            throw new Exception("no such subquest: " + req.SubquestId);

        var subQuests = progressRepo.GetSubQuests(user.ID);
        if (subQuest.BeforeSubQuestId > 0 &&
            (!subQuests.TryGetValue(subQuest.BeforeSubQuestId, out bool previousCompleted) || !previousCompleted))
            throw new Exception("subquest prerequisite is not completed: " + subQuest.BeforeSubQuestId);

        if (!MessengerTriggerUtils.IsTriggerListSatisfied(user, subQuest.TriggerList))
            throw new Exception("subquest conditions are not satisfied: " + req.SubquestId);

        if (!subQuests.ContainsKey(req.SubquestId))
        {
            progressRepo.UpsertSubQuest(user.ID, req.SubquestId, false);
            db.SaveChanges();
        }

        return new ResEnrollmentSubQuest
        {
            SubquestData = new NetSubQuestData
            {
                CreatedAt = DateTime.UtcNow.Ticks,
                IsReceived = false,
                SubQuestId = req.SubquestId
            }
        };
    }

    public ResGetSubQuestList ListSubquests(ulong userId)
    {
        var response = new ResGetSubQuestList();
        foreach (KeyValuePair<int, bool> item in progressRepo.GetSubQuests(userId))
        {
            response.SubquestList.Add(new NetSubQuestData
            {
                CreatedAt = DateTime.UtcNow.Ticks,
                SubQuestId = item.Key,
                IsReceived = item.Value
            });
        }
        return response;
    }

    public ResSetTriggerFromSubQuest SetSubquestTrigger(User user, ReqSetTriggerFromSubQuest req)
    {
        if (!GameData.Instance.Subquests.TryGetValue(req.SubquestId, out SubQuestRecord? record))
            throw new Exception("no such subquest: " + req.SubquestId);

        user.AddTrigger(record.ClearTrigger, record.ClearConditionValue, record.ClearConditionId);
        user.AddTrigger(Trigger.SubQuestClear, 1, req.SubquestId);

        return new ResSetTriggerFromSubQuest();
    }
}
