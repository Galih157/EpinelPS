using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Event;
using EpinelPS.LobbyServer.Event.Mission;
using EpinelPS.LobbyServer.Event.Shop;
using EpinelPS.LobbyServer.Event.StoryEvent;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.Services;

public sealed class EventsService(
    EventsRepository repo,
    TeamRepository teamRepo,
    ProgressRepository progressRepo,
    GameContext db)
{
    // --- Archive Story Dungeon & Scenarios ---

    public ResClearArchiveStage ClearArchiveStage(ulong userId, ReqClearArchiveStage req)
    {
        var eventData = repo.GetEvent(userId, req.EventId)
            ?? throw new Exception($"Event with ID {req.EventId} not found.");

        if (req.BattleResult == 1 && !eventData.ClearedStages.Contains(req.StageId))
        {
            eventData.ClearedStages.Add(req.StageId);
            eventData.LastStage = req.StageId;
            repo.UpsertEvent(userId, req.EventId, eventData);
        }

        return new ResClearArchiveStage();
    }

    public ResFastClearArchiveStage FastClearArchiveStage(ulong userId, ReqFastClearArchiveStage req)
    {
        var eventData = repo.GetEvent(userId, req.EventId)
            ?? throw new Exception($"Event with ID {req.EventId} not found.");

        if (!eventData.ClearedStages.Contains(req.StageId))
        {
            eventData.ClearedStages.Add(req.StageId);
            eventData.LastStage = req.StageId;
            repo.UpsertEvent(userId, req.EventId, eventData);
        }

        return new ResFastClearArchiveStage();
    }

    public ResCompleteArchiveScenario CompleteArchiveScenario(ulong userId, ReqCompleteArchiveScenario req)
    {
        CompleteScenarioInternal(userId, req.EventId, req.ScenarioId);
        return new ResCompleteArchiveScenario();
    }

    public ResGetArchiveStoryDungeon GetArchiveStoryDungeon(ulong userId, int eventId)
    {
        var eventData = repo.GetEvent(userId, eventId);
        if (eventData == null)
        {
            eventData = new EventData
            {
                CompletedScenarios = [],
                Diff = 0,
                LastStage = 0
            };
            repo.UpsertEvent(userId, eventId, eventData);
        }

        var response = new ResGetArchiveStoryDungeon
        {
            TeamData = new NetUserTeamData
            {
                LastContentsTeamNumber = 1,
                Type = 1
            }
        };

        response.LastClearedArchiveStageList.Add(new NetLastClearedArchiveStage
        {
            DifficultyId = eventData.Diff,
            StageId = eventData.LastStage
        });

        return response;
    }

    public ResGetNonResettableArchiveScenario GetNonResettableArchiveScenario(ulong userId, int eventId)
    {
        var response = new ResGetNonResettableArchiveScenario();
        var evtData = repo.GetEvent(userId, eventId);
        if (evtData != null)
        {
            response.ScenarioIdList.AddRange(evtData.CompletedScenarios);
        }
        return response;
    }

    public ResGetResettableArchiveScenario GetResettableArchiveScenario(ulong userId, int eventId)
    {
        var response = new ResGetResettableArchiveScenario();
        var evtData = repo.GetEvent(userId, eventId);

        foreach (ArchiveEventStoryRecord record in GameData.Instance.archiveEventStoryRecords.Values)
        {
            if (record.EventId == eventId && !string.IsNullOrEmpty(record.PrologueScenario))
            {
                if (evtData != null && evtData.CompletedScenarios.Contains(record.PrologueScenario))
                {
                    response.ScenarioIdList.Add(record.PrologueScenario);
                }
                break;
            }
        }

        return response;
    }

    public ResGetArchiveMessenger GetArchiveMessenger(int groupId)
    {
        var response = new ResGetArchiveMessenger();
        if (GameData.Instance.archiveMessengerConditionRecords.TryGetValue(groupId, out ArchiveMessengerConditionRecord? conditionRecord))
        {
            foreach (var condition in conditionRecord.ArchiveMessengerConditionList)
            {
                response.ArchiveMessageList.Add(new NetArchiveMessage
                {
                    ConditionId = condition.ConditionId,
                    MessageId = conditionRecord.Tid
                });
            }
        }
        return response;
    }

    // --- Event Core & Scenarios ---

    public ResExistEventScenario CheckScenarioExists(ReqExistEventScenario req)
    {
        var response = new ResExistEventScenario();
        foreach (string? item in req.ScenarioGroupIds)
            response.ExistGroupIds.Add(item);
        return response;
    }

    public ResSetEventScenarioComplete CompleteEventScenario(ulong userId, ReqSetEventScenarioComplete req)
    {
        CompleteScenarioInternal(userId, req.EventId, req.ScenarioId);
        return new ResSetEventScenarioComplete();
    }

    public ResGetEventScenarioData GetEventScenario(ulong userId, int eventId)
    {
        var response = new ResGetEventScenarioData();
        var data = repo.GetEvent(userId, eventId);
        if (data != null)
        {
            response.ScenarioIdList.AddRange(data.CompletedScenarios);
        }
        return response;
    }

    public ResGetEventList ListEvents(User user)
    {
        var response = new ResGetEventList();
        EventHelper.AddEvents(user, ref response);
        return response;
    }

    public ResGetJoinedEvent GetJoinedEvents(User user)
    {
        var response = new ResGetJoinedEvent();
        EventHelper.AddJoinedEvents(user, ref response);
        return response;
    }

    public ResChallengeEventStageData GetChallengeStage(ulong userId, int eventId)
    {
        var response = new ResChallengeEventStageData
        {
            RemainTicket = 3,
            TeamData = new NetUserTeamData { Type = (int)TeamType.StoryEvent }
        };

        if (teamRepo.GetUserTeam(userId, (int)TeamType.StoryEvent) is NetUserTeamData teamData)
        {
            response.TeamData = teamData;
        }

        var eventData = repo.GetEvent(userId, eventId);
        if (eventData == null)
        {
            eventData = new EventData { LastStage = 0 };
            repo.UpsertEvent(userId, eventId, eventData);
        }

        response.LastClearedEventStageList.Add(new NetLastClearedEventStageData
        {
            DifficultyId = eventData.Diff,
            StageId = eventData.LastStage
        });

        return response;
    }

    // --- Login Events ---

    public ResLoginEventData GetLoginEvent(ulong userId, int eventId)
    {
        var response = new ResLoginEventData
        {
            EndDate = DateTime.Now.AddDays(13).Ticks,
            DisableDate = DateTime.Now.AddDays(13).Ticks,
            LastAttendance = new LoginEventAttendance()
        };

        var loginEventData = repo.GetLoginEvent(userId, eventId);
        if (loginEventData == null)
        {
            loginEventData = new LoginEventData();
            repo.UpsertLoginEvent(userId, eventId, loginEventData);
        }

        int day = 1;
        GameData.Instance.LoginEventTable.Values.Where(ev => ev.EventId == eventId).ToList().ForEach(ev =>
        {
            loginEventData.LastDay = day++;
            response.RewardHistories.Add(new LoginEventRewardHistory
            {
                IsReceived = loginEventData.Days.Contains(ev.Day),
                Day = ev.Day
            });
        });

        response.LastAttendance.Day = loginEventData.LastDay;
        response.LastAttendance.AttendanceDate = loginEventData.LastDate;

        return response;
    }

    public ResObtainLoginEventReward ReceiveLoginEvent(User user, ReqObtainLoginEventReward req)
    {
        var response = new ResObtainLoginEventReward();
        var loginEventData = repo.GetLoginEvent(user.ID, req.EventId);
        if (loginEventData == null)
        {
            loginEventData = new LoginEventData();
            repo.UpsertLoginEvent(user.ID, req.EventId, loginEventData);
        }

        GameData.Instance.LoginEventTable.Values
            .Where(ev => ev.EventId == req.EventId && ev.Day == req.Day)
            .ToList()
            .ForEach(ev =>
            {
                response.Reward = RewardUtils.RegisterRewardsForUser(user, ev.RewardId);
            });

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResObtainAllLoginEventReward ReceiveAllLoginEvent(User user, int eventId)
    {
        var response = new ResObtainAllLoginEventReward();
        var loginEventData = repo.GetLoginEvent(user.ID, eventId) ?? new LoginEventData();
        NetRewardData rewardData = new();

        GameData.Instance.LoginEventTable.Values.Where(ev => ev.EventId == eventId).ToList().ForEach(ev =>
        {
            if (!loginEventData.Days.Contains(ev.Day))
            {
                loginEventData.Days.Add(ev.Day);
                RewardRecord reward = GameData.Instance.GetRewardTableEntry(ev.RewardId)
                    ?? throw new Exception($"unknown reward Id {ev.RewardId}");
                foreach (var item in reward.Rewards)
                {
                    if (item.RewardType != RewardType.None)
                    {
                        RewardUtils.AddSingleObject(user, ref rewardData, item.RewardId, item.RewardType, item.RewardValue);
                    }
                }
            }
        });

        response.Reward = rewardData;
        repo.UpsertLoginEvent(user.ID, eventId, loginEventData);
        UserSessionStore.SaveUser(user);
        return response;
    }

    // --- Story Event Dungeon ---

    public ResStoryDungeonEventData GetStoryDungeon(User user, int eventId)
    {
        var eventData = repo.GetEvent(user.ID, eventId);
        if (eventData == null)
        {
            eventData = new EventData { LastDay = user.GetDateDay(), FreeTicket = 5 };
            repo.UpsertEvent(user.ID, eventId, eventData);
        }

        var response = new ResStoryDungeonEventData
        {
            RemainTicket = EventStoryHelper.GetTicket(user, eventId, repo),
            TeamData = new NetUserTeamData { Type = (int)TeamType.StoryEvent }
        };

        if (teamRepo.GetUserTeam(user.ID, (int)TeamType.StoryEvent) is NetUserTeamData teamData)
        {
            response.TeamData = teamData;
        }

        foreach (var stageId in eventData.ClearedStages)
        {
            response.LastClearedEventStageList.Add(new NetLastClearedEventStageData { StageId = stageId });
        }
        response.LastClearedEventStageList.Add(new NetLastClearedEventStageData { StageId = eventData.LastStage });

        return response;
    }

    public ResClearEventStage ClearEventStage(User user, ReqClearEventStage req)
    {
        var response = new ResClearEventStage();
        NetRewardData reward = new();
        NetRewardData bonusReward = new();

        ClearEventStageHelper.ClearStage(user, req.StageId, ref reward, ref bonusReward, req.BattleResult, 1);

        var eventData = repo.GetEvent(user.ID, req.EventId);
        if (eventData != null && req.BattleResult == 1)
        {
            if (!eventData.ClearedStages.Contains(req.StageId))
                eventData.ClearedStages.Add(req.StageId);
            if (eventData.LastStage < req.StageId)
                eventData.LastStage = req.StageId;
            eventData.Diff = 0;
            repo.UpsertEvent(user.ID, req.EventId, eventData);
        }
        else if (eventData == null)
        {
            eventData = new EventData { LastStage = req.StageId, ClearedStages = [req.StageId] };
            repo.UpsertEvent(user.ID, req.EventId, eventData);
        }

        user.AddTrigger(Trigger.EventStageClear, 1, req.StageId);
        user.AddTrigger(Trigger.EventDungeonStageClear, 1, req.EventId);
        if (bonusReward.Item.Count > 0)
        {
            foreach (var item in bonusReward.Item)
            {
                user.AddTrigger(Trigger.ObtainEventCurrencyMaterial, item.Count, item.Tid);
            }
        }

        response.RemainTicket = EventStoryHelper.SubtractTicket(user, req.EventId, 1, repo);
        response.Reward = reward;
        response.BonusReward = bonusReward;

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResFastClearEventStage FastClearEventStage(User user, ReqFastClearEventStage req)
    {
        var response = new ResFastClearEventStage();
        NetRewardData reward = new();
        NetRewardData bonusReward = new();

        ClearEventStageHelper.ClearStage(user, req.StageId, ref reward, ref bonusReward, 1, req.ClearCount);

        user.AddTrigger(Trigger.EventStageClear, 1, req.StageId);
        user.AddTrigger(Trigger.EventDungeonStageClear, req.ClearCount, req.EventId);
        response.RemainTicket = EventStoryHelper.SubtractTicket(user, req.EventId, req.ClearCount, repo);

        if (bonusReward.Item.Count > 0)
        {
            foreach (var item in bonusReward.Item)
            {
                user.AddTrigger(Trigger.ObtainEventCurrencyMaterial, item.Count, item.Tid);
            }
        }

        response.Reward = reward;
        response.BonusReward = bonusReward;

        UserSessionStore.SaveUser(user);
        return response;
    }

    // --- Event Missions ---

    public ResGetEventMissionClear GetMissionClear(User user, int eventId)
    {
        var response = new ResGetEventMissionClear();
        try
        {
            response.EventMissionClearList.AddRange(EventMissionHelper.GetCleared(user, eventId, repo));
        }
        catch (Exception ex)
        {
            Logging.Warn($"GetMissionClear failed: {ex.Message}");
        }
        return response;
    }

    public ResGetEventMissionClearList GetMissionClearList(User user, ReqGetEventMissionClearList req)
    {
        var response = new ResGetEventMissionClearList();
        try
        {
            response.ResGetEventMissionClearMap.AddRange(EventMissionHelper.GetClearedList(user, req.EventIdList, repo));
        }
        catch (Exception ex)
        {
            Logging.Warn($"GetMissionClearList failed: {ex.Message}");
        }
        return response;
    }

    public ResObtainEventMissionReward ObtainMissionReward(User user, ReqObtainEventMissionReward req)
    {
        var response = new ResObtainEventMissionReward
        {
            Result = ObtainEventMissionRewardResult.Success
        };
        var reward = new NetRewardData();
        try
        {
            EventMissionHelper.ObtainReward(user, ref reward, req.EventId, req.EventMissionIdList, req.RequestTimeStamp, repo);
        }
        catch (Exception ex)
        {
            Logging.Warn($"ObtainMissionReward failed: {ex.Message}");
        }
        response.Reward = reward;
        return response;
    }

    // --- Event Shop ---

    public ResEventShopProductList GetShopProductList(ulong userId, int eventId)
    {
        var response = new ResEventShopProductList();
        try
        {
            response.Shops.Add(EventShopHelper.InitShopData(userId, eventId, repo));

            if (!GameData.Instance.ShopTable.Values.Any(s => (int)s.ShopCategory == 1))
            {
                response.Shops.Add(new NetEventShopProductData
                {
                    ShopTid = 1,
                    ShopCategory = 1
                });
            }
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Get EventShopProductList Error: {ex.Message}", LogType.Error);
        }
        return response;
    }

    public ResEventShopBuyProduct BuyShopProduct(User user, ReqEventShopBuyProduct req)
    {
        var response = new ResEventShopBuyProduct();
        try
        {
            EventShopHelper.BuyShopProduct(user, ref response, req, repo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Error buying shop product: {ex.Message}", LogType.Error);
        }
        return response;
    }

    public ResEventShopMultipleBuyProduct BuyShopMultipleProduct(User user, ReqEventShopMultipleBuyProduct req)
    {
        var response = new ResEventShopMultipleBuyProduct
        {
            Result = EventShopBuyProductResult.Success
        };
        try
        {
            EventShopHelper.BuyShopMultipleProduct(user, ref response, req, repo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"Error buying shop product: {ex.Message}", LogType.Error);
        }
        return response;
    }

    // --- Event Field ---

    public ResEnterEventField EnterEventField(User user, string mapId)
    {
        var response = new ResEnterEventField { Field = new() };

        FieldInfoNew? field = progressRepo.GetFieldInfo(user.ID, mapId);
        if (field == null)
        {
            field = new FieldInfoNew();
            progressRepo.SaveFieldInfo(user.ID, mapId, field);
            db.SaveChanges();
        }

        foreach (int stage in field.CompletedStages)
        {
            response.Field.Stages.Add(new NetFieldStageData { StageId = stage });
        }
        foreach (NetFieldObject obj in field.CompletedObjects)
        {
            if (obj != null && obj.Type == 1)
                response.Field.Objects.Add(obj);
        }

        if (progressRepo.GetMapJson(user.ID, mapId) is string mapJson)
        {
            response.Json = mapJson;
        }

        return response;
    }

    public ResListFieldPasswordDoorData ListPasswordDoor(User user, string mapId)
    {
        var response = new ResListFieldPasswordDoorData();
        if (progressRepo.GetFieldInfo(user.ID, mapId) is FieldInfoNew field)
        {
            response.AcquiredFieldPasswordIdList.AddRange(field.AcquiredPasswordList);
            response.UnlockedFieldPasswordDoorIdList.AddRange(field.UnlockedDoorList);
        }
        return response;
    }

    public ResViewEventFieldNoticePopup ViewNoticePopup(User user, ReqViewEventFieldNoticePopup req)
    {
        if (progressRepo.AddViewedNoticePopups(user.ID, req.EventFieldId, req.EventFieldNoticePopupTableIds) > 0)
        {
            db.SaveChanges();
        }
        return new ResViewEventFieldNoticePopup();
    }

    public ResAcquireFieldPassword AcquirePassword(User user, ReqAcquireFieldPassword req)
    {
        if (GameData.Instance.MapData.TryGetValue(req.MapId, out FieldMapRecord? fieldMap))
        {
            var item = fieldMap.PasswordSpawner.FirstOrDefault(x => x.PositionId == req.PositionId);
            if (item != null)
            {
                var password = GameData.Instance.FieldPasswordTable.Values.FirstOrDefault(x => x.Id == item.PasswordTableId);
                if (password != null && progressRepo.GetFieldInfo(user.ID, req.MapId) is FieldInfoNew field)
                {
                    field.AcquiredPasswordList.Add(password.Id);
                    progressRepo.SaveFieldInfo(user.ID, req.MapId, field);
                    db.SaveChanges();
                }
            }
        }
        return new ResAcquireFieldPassword();
    }

    public ResUnlockFieldPasswordDoor UnlockPasswordDoor(User user, ReqUnlockFieldPasswordDoor req)
    {
        if (GameData.Instance.MapData.TryGetValue(req.MapId, out FieldMapRecord? fieldMap))
        {
            var item = fieldMap.PasswordDoor.FirstOrDefault(x => x.PositionId == req.PositionId);
            if (item != null)
            {
                var door = GameData.Instance.FieldPasswordDoorTable.Values.FirstOrDefault(x => x.Id == item.PasswordDoorTableId);
                if (door != null && progressRepo.GetFieldInfo(user.ID, req.MapId) is FieldInfoNew field)
                {
                    field.UnlockedDoorList.Add(door.Id);
                    progressRepo.SaveFieldInfo(user.ID, req.MapId, field);
                    db.SaveChanges();
                }
            }
        }
        return new ResUnlockFieldPasswordDoor();
    }

    public ResSaveEventFieldNonResettableFieldObject SaveFieldObject(User user, ReqSaveEventFieldNonResettableFieldObject req)
    {
        EventFieldRecord? field = GameData.Instance.EventFieldTable.Values.FirstOrDefault(x => x.EventId == req.EventId);
        if (field != null)
        {
            var fieldInfo = progressRepo.GetFieldInfo(user.ID, field.FieldDesignMap);
            if (fieldInfo == null)
            {
                fieldInfo = new FieldInfoNew();
                foreach (var item in req.FieldObjects)
                {
                    fieldInfo.CompletedObjects.AddUnique(new NetFieldObject
                    {
                        PositionId = item.PositionId,
                        Json = item.Json,
                        Type = item.Type
                    });
                }
            }
            else
            {
                foreach (var item in req.FieldObjects)
                {
                    if (item.Type == 11)
                    {
                        var index = fieldInfo.CompletedObjects.FindIndex(x => x.PositionId == item.PositionId && x.Type == item.Type);
                        if (index != -1)
                        {
                            fieldInfo.CompletedObjects[index].Json = item.Json;
                        }
                        else
                        {
                            fieldInfo.CompletedObjects.Add(new NetFieldObject
                            {
                                PositionId = item.PositionId,
                                Type = item.Type,
                                Json = item.Json
                            });
                        }
                    }
                    else
                    {
                        fieldInfo.CompletedObjects.AddUnique(new NetFieldObject
                        {
                            PositionId = item.PositionId,
                            Json = item.Json,
                            Type = item.Type
                        });
                    }
                }
            }

            progressRepo.SaveFieldInfo(user.ID, field.FieldDesignMap, fieldInfo);
            db.SaveChanges();
        }

        return new ResSaveEventFieldNonResettableFieldObject();
    }

    private void CompleteScenarioInternal(ulong userId, int eventId, string scenarioId)
    {
        var evt = repo.GetOrCreateEvent(userId, eventId);
        if (!evt.CompletedScenarios.Contains(scenarioId))
        {
            evt.CompletedScenarios.Add(scenarioId);
            repo.UpsertEvent(userId, eventId, evt);
        }
    }
}
