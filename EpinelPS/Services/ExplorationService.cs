using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.Services;

public sealed class ExplorationService(ExplorationRepository repo, TeamRepository teamRepo, ResetRepository resetRepo, ProgressRepository progressRepo)
{
    private const int LostSectorTeamType = 10;

    // --- Archive ---

    public async Task<ResGetArchiveRecord> GetArchivesAsync(ulong userId, CancellationToken ct = default)
    {
        var response = new ResGetArchiveRecord();
        List<ArchiveRecordManagerRecord> records = [.. GameData.Instance.archiveRecordManagerTable.Values];
        List<int> allIds = [.. records.Select(record => record.Id)];

        response.ArchiveRecordManagerList.AddRange(allIds);

        var (recordIds, eventQuestIds) = await repo.GetArchiveUnlocksAsync(userId, ct);

        if (GameConfig.Root.ArchiveUnlockAll == true)
        {
            response.UnlockedArchiveRecordList.AddRange(allIds);
        }
        else
        {
            response.UnlockedArchiveRecordList.AddRange(recordIds);
        }

        List<ArchiveRecordManagerRecord> eventQuestRecords = [.. records
            .Where(record => record.RecordType == ArchiveRecordType.EventQuest)];

        response.ArchiveEventQuest = new();
        if (GameConfig.Root.ArchiveUnlockAll == true)
        {
            response.ArchiveEventQuest.UnlockedArchiveRecordManagerEventQuestIdList
                .AddRange(eventQuestRecords.Select(record => record.Id));
        }
        else
        {
            response.ArchiveEventQuest.UnlockedArchiveRecordManagerEventQuestIdList
                .AddRange(eventQuestIds);
        }

        return response;
    }

    public async Task<ResUnlockArchiveRecord> UnlockArchiveRecordAsync(
        User user, ReqUnlockArchiveRecord req, CancellationToken ct = default)
    {
        var (userItem, reward) = await UnlockArchiveInternalAsync(
            user, req.ArchiveRecordId, ExplorationRepository.ArchiveKindRecord, ct);

        return new ResUnlockArchiveRecord
        {
            UserItem = userItem,
            Reward = reward
        };
    }

    public async Task<ResUnlockArchiveEventQuest> UnlockArchiveEventQuestAsync(
        User user, ReqUnlockArchiveEventQuest req, CancellationToken ct = default)
    {
        var (userItem, reward) = await UnlockArchiveInternalAsync(
            user, req.ArchiveRecordManagerId, ExplorationRepository.ArchiveKindEventQuest, ct);

        return new ResUnlockArchiveEventQuest
        {
            UserItem = userItem,
            Reward = reward
        };
    }

    private async Task<(NetUserItemData? UserItem, NetRewardData? Reward)> UnlockArchiveInternalAsync(
        User user, int archiveRecordId, string kind, CancellationToken ct)
    {
        ArchiveRecordManagerRecord record = GameData.Instance.archiveRecordManagerTable.GetValueOrDefault(archiveRecordId)
            ?? throw new BadHttpRequestException($"Unknown archive record {archiveRecordId}", 400);

        if (await repo.IsArchiveUnlockedAsync(user.ID, kind, record.Id, ct))
            return (null, null);

        NetUserItemData? userItem = null;
        NetRewardData? reward = null;
        bool userMutated = false;

        if (GameConfig.Root.ArchiveUnlockAll != true)
        {
            DbItemData material = user.Items.FirstOrDefault(item => item.ItemType == record.UnlockTicketId)
                ?? throw new BadHttpRequestException($"Missing archive unlock ticket {record.UnlockTicketId}", 400);

            List<NetUserItemData> updatedItems = [];
            if (!EquipmentUtils.DeductMaterials(material, record.UnlockTicketCount, user, updatedItems))
                throw new BadHttpRequestException("Not enough archive unlock tickets", 400);

            userItem = updatedItems[0];
            if (record.UnlockRewardId > 0)
                reward = RewardUtils.RegisterRewardsForUser(user, record.UnlockRewardId);
            userMutated = true;
        }

        await repo.UnlockArchiveAsync(user.ID, kind, record.Id, ct);
        if (userMutated)
            UserSessionStore.SaveUser(user);

        return (userItem, reward);
    }

    // --- Mission Achievements ---

    public async Task<ResGetAchievementRewardedData> GetAchievementRewardedDataAsync(
        ulong userId, CancellationToken ct = default)
    {
        var response = new ResGetAchievementRewardedData();
        response.Ids.AddRange(await repo.GetCompletedAchievementsAsync(userId, ct));
        return response;
    }

    public async Task<ResGetRewardedData> GetAllMissionRewardsAsync(
        User user, CancellationToken ct = default)
    {
        var response = new ResGetRewardedData();
        response.AchievementIds.Add(await repo.GetCompletedAchievementsAsync(user.ID, ct));
        response.WeeklyIds.Add(resetRepo.GetCompletedWeeklyMissions(user.ID));
        response.DailyIds.Add(resetRepo.GetCompletedDailyMissions(user.ID));
        return response;
    }

    public async Task<ResObtainAchievementReward> ObtainAchievementAsync(
        User user, ReqObtainAchievementReward req, CancellationToken ct = default)
    {
        var completedSet = await repo.GetCompletedAchievementSetAsync(user.ID, ct);
        List<NetRewardData> rewards = [];
        List<int> newlyCompleted = [];
        int totalPoints = 0;

        foreach (int item in req.TidList)
        {
            if (completedSet.Contains(item)) continue;

            if (!GameData.Instance.TriggerTable.TryGetValue(item, out TriggerRecord? key))
                throw new Exception("unknown TID");

            RewardRecord rewardRecord = GameData.Instance.GetRewardTableEntry(key.RewardId)
                ?? throw new Exception("unable to lookup reward");

            rewards.Add(RewardUtils.RegisterRewardsForUser(user, rewardRecord));
            completedSet.Add(item);
            newlyCompleted.Add(item);
            totalPoints++;
        }

        user.AddTrigger(Trigger.PointRewardAchievement, totalPoints);

        var response = new ResObtainAchievementReward
        {
            Reward = NetUtils.MergeRewards(rewards, user)
        };

        if (newlyCompleted.Count > 0)
            await repo.AddCompletedAchievementsAsync(user.ID, newlyCompleted, ct);

        UserSessionStore.SaveUser(user);
        return response;
    }

    // --- SideStory ---

    public async Task<ResListSideStory> ListSideStoryAsync(ulong userId, CancellationToken ct = default)
    {
        var (completed, viewed) = await repo.GetSideStoryStagesAsync(userId, ct);
        var response = new ResListSideStory();

        foreach (int item in completed)
        {
            response.SideStoryStageDataList.Add(new NetSideStoryStageData
            {
                SideStoryStageId = item,
                ClearedAt = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }

        response.ViewedSideStoryIds.AddRange(viewed);
        return response;
    }

    public async Task<ResClearSideStoryStage> ClearSideStoryStageAsync(
        User user, int sideStoryStageId, CancellationToken ct = default)
    {
        var response = new ResClearSideStoryStage();
        if (await repo.MarkSideStoryCompletedAsync(user.ID, sideStoryStageId, ct))
        {
            if (GameData.Instance.SidestoryRewardTable.TryGetValue(sideStoryStageId, out SideStoryStageRecord? value))
            {
                RewardRecord? rewardData = GameData.Instance.GetRewardTableEntry(value.FirstClearReward);
                if (rewardData != null)
                {
                    response.Reward = RewardUtils.RegisterRewardsForUser(user, rewardData);
                    UserSessionStore.SaveUser(user);
                }
            }
        }

        return response;
    }

    public async Task<ResSetViewSideStory> SetSideStoryViewedAsync(
        ulong userId, IEnumerable<int> stageIds, CancellationToken ct = default)
    {
        await repo.MarkSideStoriesViewedAsync(userId, stageIds, ct);
        return new ResSetViewSideStory();
    }

    // --- LostSector ---

    public async Task<ResGetLostSectorData> GetLostSectorDataAsync(
        User user, CancellationToken ct = default)
    {
        var sectors = await repo.GetAllLostSectorsAsync(user.ID, ct);
        var response = new ResGetLostSectorData();

        foreach (var item in GameData.Instance.LostSector)
        {
            if (item.Value.OpenConditionType == ContentOpenType.Stage &&
                progressRepo.IsStageCompleted(user.ID, item.Value.OpenConditionValue))
            {
                response.ClearStages.Add(new NetFieldStageData { StageId = item.Value.OpenConditionValue });
            }

            if (sectors.TryGetValue(item.Key, out LostSectorData? val))
            {
                var map = GameData.Instance.MapData[item.Value.FieldId];
                response.LostSector.Add(ToNetUserLostSectorData(item.Key, val, map.StageSpawner.Count));
            }
        }

        return response;
    }

    public async Task<ResGetLostSectorFieldData> GetFieldAsync(
        ulong userId, int sectorId, CancellationToken ct = default)
    {
        LostSectorData f = await repo.GetLostSectorAsync(userId, sectorId, ct) ?? new LostSectorData();
        var response = new ResGetLostSectorFieldData
        {
            Field = new NetFieldObjectData(),
            Json = f.Json
        };

        foreach (var item in f.Objects)
        {
            response.Field.Objects.Add(new NetFieldObject
            {
                ActionAt = item.Value.ActionAt,
                Json = item.Value.Json,
                PositionId = item.Key,
                Type = item.Value.Type
            });
        }

        foreach (var item in f.ClearedStages)
        {
            response.Field.Stages.Add(new NetFieldStageData
            {
                PositionId = item.Key,
                StageId = item.Value
            });
        }

        if (teamRepo.GetUserTeam(userId, LostSectorTeamType) is NetUserTeamData team)
            response.Team = team;

        return response;
    }

    public async Task<ResOpenLostSector> OpenAsync(
        ulong userId, int sectorId, CancellationToken ct = default)
    {
        var val = await repo.GetLostSectorAsync(userId, sectorId, ct);
        if (val == null)
        {
            val = new LostSectorData { IsOpen = true };
            await repo.UpsertLostSectorAsync(userId, sectorId, val, ct);
        }

        return new ResOpenLostSector
        {
            Lostsector = ToNetUserLostSectorData(sectorId, val)
        };
    }

    public async Task<ResPlayLostSector> PlayAsync(
        ulong userId, int sectorId, CancellationToken ct = default)
    {
        var sector = await repo.GetLostSectorAsync(userId, sectorId, ct) ?? new LostSectorData();
        sector.IsPlaying = true;
        await repo.UpsertLostSectorAsync(userId, sectorId, sector, ct);

        return new ResPlayLostSector
        {
            Lostsector = ToNetUserLostSectorData(sectorId, sector)
        };
    }

    public async Task<ResSaveLostSectorField> SaveFieldAsync(
        ulong userId, ReqSaveLostSectorField req, CancellationToken ct = default)
    {
        var sector = await repo.GetLostSectorAsync(userId, req.SectorId, ct) ?? new LostSectorData();
        sector.Json = req.Json;
        await repo.UpsertLostSectorAsync(userId, req.SectorId, sector, ct);
        return new ResSaveLostSectorField();
    }

    public async Task<ResSaveLostSectorFieldObject> SaveFieldObjectAsync(
        ulong userId, ReqSaveLostSectorFieldObject req, CancellationToken ct = default)
    {
        var sector = await repo.GetLostSectorAsync(userId, req.SectorId, ct) ?? new LostSectorData();
        sector.Objects[req.Object.PositionId] = req.Object;

        if (req.Object.TeamPosition.Count != 0)
        {
            sector.TeamPositions.Clear();
            sector.TeamPositions.AddRange(req.Object.TeamPosition);
        }

        await repo.UpsertLostSectorAsync(userId, req.SectorId, sector, ct);
        return new ResSaveLostSectorFieldObject();
    }

    public async Task<ResClearLostSectorStage> ClearStageAsync(
        ulong userId, ReqClearLostSectorStage req, CancellationToken ct = default)
    {
        if (req.BattleResult == 1)
            await ClearLostSectorStageAsync(userId, req.StageId, ct);

        return new ResClearLostSectorStage();
    }

    public async Task<ResFastClearLostSectorStage> FastClearStageAsync(
        ulong userId, int stageId, CancellationToken ct = default)
    {
        await ClearLostSectorStageAsync(userId, stageId, ct);
        return new ResFastClearLostSectorStage();
    }

    public async Task<ResObtainLostSectorItem> ObtainItemAsync(
        User user, ReqObtainLostSectorItem req, CancellationToken ct = default)
    {
        LostSectorData lostSectorUser = await repo.GetLostSectorAsync(user.ID, req.SectorId, ct)
            ?? throw new Exception($"Lost sector {req.SectorId} not found");

        lostSectorUser.ObtainedRewards++;
        lostSectorUser.Objects[req.Object.PositionId] = req.Object;

        var map = GameData.Instance.MapData[GameData.Instance.LostSector[req.SectorId].FieldId];
        var rewardEntry = map.ItemSpawner.FirstOrDefault(x => x.PositionId == req.Object.PositionId)
            ?? throw new Exception("cannot find reward");

        FieldItemRecord positionReward = GameData.Instance.FieldItems[rewardEntry.ItemId];
        var response = new ResObtainLostSectorItem
        {
            Reward = RewardUtils.RegisterRewardsForUser(user, positionReward.TypeValue),
            BoxCount = lostSectorUser.ObtainedRewards
        };

        if (positionReward.IsFinalReward)
            lostSectorUser.ReceivedFinalReward = true;

        await repo.UpsertLostSectorAsync(user.ID, req.SectorId, lostSectorUser, ct);
        UserSessionStore.SaveUser(user);
        return response;
    }

    public async Task<ResLostSectorPerfectReward> GetPerfectRewardAsync(
        User user, int sectorId, CancellationToken ct = default)
    {
        var response = new ResLostSectorPerfectReward();
        var lostSectorData = await repo.GetLostSectorAsync(user.ID, sectorId, ct);

        if (lostSectorData != null)
        {
            lostSectorData.CompletedPerfectly = true;
            LostSectorRecord sectorInfo = GameData.Instance.LostSector[sectorId];
            response.Reward = RewardUtils.RegisterRewardsForUser(user, sectorInfo.ExplorationReward);

            await repo.UpsertLostSectorAsync(user.ID, sectorId, lostSectorData, ct);
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    private async Task ClearLostSectorStageAsync(ulong userId, int stageId, CancellationToken ct)
    {
        int sectorId = GameData.Instance.LostSectorStages[stageId].Sector;
        LostSectorRecord sectorData = GameData.Instance.LostSector[sectorId];
        var mapInfo = GameData.Instance.MapData[sectorData.FieldId];
        var stage = mapInfo.StageSpawner.FirstOrDefault(x => x.StageId == stageId)
            ?? throw new Exception("cannot find stage in map data");

        var sector = await repo.GetLostSectorAsync(userId, sectorId, ct) ?? new LostSectorData();
        sector.ClearedStages[stage.PositionId] = stageId;
        await repo.UpsertLostSectorAsync(userId, sectorId, sector, ct);
    }

    private static NetUserLostSectorData ToNetUserLostSectorData(
        int sectorId, LostSectorData data, int maxClearStageCount = 0)
    {
        return new NetUserLostSectorData
        {
            IsOpen = data.IsOpen,
            SectorId = sectorId,
            IsPlaying = data.IsPlaying,
            CurrentClearStageCount = data.ClearedStages.Count,
            RewardCount = data.ObtainedRewards,
            IsFinalReward = data.ReceivedFinalReward,
            IsPerfectReward = data.CompletedPerfectly,
            MaxClearStageCount = maxClearStageCount
        };
    }
}
