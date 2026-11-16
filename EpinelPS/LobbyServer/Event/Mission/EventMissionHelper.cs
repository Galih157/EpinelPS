
using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Newtonsoft.Json;
using static EpinelPS.ResGetEventMissionClearList.Types;

namespace EpinelPS.LobbyServer.Event.Mission;

public static class EventMissionHelper
{
    private static readonly ILog log = LogManager.GetLogger(typeof(EventMissionHelper));

    public static RepeatedField<NetEventMissionClearData> GetCleared(User user, int eventId, EventsRepository repo)
    {
        var clearData = new RepeatedField<NetEventMissionClearData>();
        var userEvent = repo.GetEventMission(user.ID, eventId);
        if (userEvent == null) return clearData;
        log.Debug($"GetClear UserEvent: {JsonConvert.SerializeObject(userEvent)}");
        int dateDay = user.GetDateDay();
        // Check if it's a new day, reset daily missions
        if (userEvent.LastDay != dateDay)
        {
            ResetUserDailyMission(user.ID, eventId, userEvent, dateDay, repo);
        }

        foreach (var id in userEvent.DailyMissionIdList)
        {
            clearData.Add(new NetEventMissionClearData()
            {
                EventId = eventId,
                EventMissionId = id,
                CreatedAt = userEvent.LastDate
            });
        }

        foreach (var id in userEvent.MissionIdList)
        {
            clearData.Add(new NetEventMissionClearData()
            {
                EventId = eventId,
                EventMissionId = id,
                CreatedAt = userEvent.LastDate
            });
        }
        return clearData;
    }

    public static RepeatedField<NestEventMissionClear> GetClearedList(User user, RepeatedField<int> eventIds, EventsRepository repo)
    {
        var clearDatas = new RepeatedField<NestEventMissionClear>();
        if (eventIds.Count == 0) return clearDatas;
        foreach (var eventId in eventIds)
        {
            var clearData = new NestEventMissionClear
            {
                EventId = eventId
            };
            clearData.EventMissionClearList.AddRange(GetCleared(user, eventId, repo));
        }

        return clearDatas;
    }


    /// <summary>
    /// Obtain reward for event mission
    /// </summary>
    /// <param name="user"></param>
    /// <param name="reward"></param>
    /// <param name="eventId"></param>
    /// <param name="missionIds"></param>
    /// <param name="timeStamp"></param>
    /// <param name="repo"></param>
    public static void ObtainReward(User user, ref NetRewardData reward, int eventId, RepeatedField<int> missionIds, Timestamp timeStamp, EventsRepository repo)
    {
        EventMissionData userEvent = repo.GetEventMission(user.ID, eventId) ?? new EventMissionData();
        log.Debug($"ObtainReward UserEvent Before: {JsonConvert.SerializeObject(userEvent)}");

        int dateDay = user.GetDateDay();
        // Check if it's a new day, reset daily missions
        if (userEvent.LastDay != dateDay)
        {
            log.Debug($"ObtainReward New Day: {dateDay}");
            ResetUserDailyMission(user.ID, eventId, userEvent, dateDay, repo);
        }

        var userMissionIds = userEvent.MissionIdList ?? [];
        var userDailyMissionIds = userEvent.DailyMissionIdList ?? [];

        var eventMissionRecords = GameData.Instance.EventMissionListTable.Values.Where(em =>
            missionIds.Contains(em.Id)
            && !userMissionIds.Contains(em.Id)
            && !userDailyMissionIds.Contains(em.Id)).ToList();
        if (eventMissionRecords.Count == 0) return;
        log.Debug($"ObtainReward Event Mission Records: {JsonConvert.SerializeObject(eventMissionRecords)}");

        List<Reward_Data> rewards = [];
        foreach (var mission in eventMissionRecords)
        {
            if (mission.RewardId == 0)
            {
                if (mission.RewardPointValue > 0)
                {
                    user.AddTrigger(Trigger.PointRewardEvent, mission.RewardPointValue, mission.Group);
                }
                continue;
            }
            user.AddTrigger(Trigger.MissionClearEvent, 1, mission.Group);
            var rewardRecord = GameData.Instance.GetRewardTableEntry(mission.RewardId);
            if (rewardRecord is null || rewardRecord.Rewards.Length == 0) continue;
            foreach (var item in rewardRecord.Rewards)
            {
                var itemIndex = rewards.FindIndex(x => x.RewardId == item.RewardId);
                if (itemIndex >= 0)
                    rewards[itemIndex].RewardValue += item.RewardValue;
                else
                    rewards.Add(item);
            }
        }

        // if (rewards.Count == 0) return;
        log.Debug($"ObtainReward Rewards: {JsonConvert.SerializeObject(rewards)}");
        // Add rewards to user
        foreach (var r in rewards)
        {
            RewardUtils.AddSingleObject(user, ref reward, r.RewardId, r.RewardType, r.RewardValue);
        }

        // Add mission ids to user
        var groupIds = eventMissionRecords.Select(x => x.Group).Distinct();
        var categoryRecords = GameData.Instance.EventMissionCategoryTable.Values.Where(ec => groupIds.Contains(ec.MissionListGroup)).ToList();
        if (categoryRecords.Count == 0) return;
        foreach (var mission in eventMissionRecords)
        {
            var categoryRecord = categoryRecords.FirstOrDefault(ec => ec.MissionListGroup == mission.Group);
            if (categoryRecord is null) continue;
            if (categoryRecord.InitType == EventMissionInitType.Daily)
            {
                userEvent.DailyMissionIdList.Add(mission.Id);
            }
            else
            {
                userEvent.MissionIdList.Add(mission.Id);
            }
        }

        foreach (var item in reward.Item)
        {
            user.AddTrigger(Trigger.ObtainEventCurrencyMaterial, item.Count, item.Tid);
        }

        userEvent.LastDate = timeStamp.ToDateTime().Ticks;
        repo.UpsertEventMission(user.ID, eventId, userEvent);
        log.Debug($"ObtainReward UserEvent After: {JsonConvert.SerializeObject(userEvent)}");

        UserSessionStore.SaveUser(user);
    }

    private static void ResetUserDailyMission(ulong userId, int eventId, EventMissionData userEvent, int dateDay, EventsRepository repo)
    {
        if (userEvent.LastDay == dateDay) return;
        userEvent.DailyMissionIdList = [];
        userEvent.LastDay = dateDay;
        repo.UpsertEventMission(userId, eventId, userEvent);
    }
}