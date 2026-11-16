using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database.Domains;

public sealed class EventsStore : IUserDomainStore
{
    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        user.EventInfo = db.Set<UserEvent>().AsNoTracking()
            .Include(x => x.CompletedScenarios)
            .Include(x => x.ClearedStages)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.EventId,
                x => new EventData
                {
                    Diff = x.Diff,
                    LastStage = x.LastStage,
                    LastDay = x.LastDay,
                    FreeTicket = x.FreeTicket,
                    CompletedScenarios = x.CompletedScenarios.Select(s => s.ScenarioId).ToList(),
                    ClearedStages = x.ClearedStages.Select(s => s.StageId).ToList()
                });

        user.LoginEventInfo = db.Set<UserLoginEvent>().AsNoTracking()
            .Include(x => x.Days)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.EventId,
                x => new LoginEventData
                {
                    LastDay = x.LastDay,
                    LastDate = x.LastDate,
                    Days = x.Days.Select(d => d.Day).ToList()
                });

        user.EventMissionInfo = db.Set<UserEventMission>().AsNoTracking()
            .Include(x => x.Entries)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.EventId,
                x => new EventMissionData
                {
                    LastDay = x.LastDay,
                    LastDate = x.LastDate,
                    AllClear = x.AllClear,
                    MissionIdList = x.Entries.Where(e => !e.IsDaily).Select(e => e.MissionId).ToList(),
                    DailyMissionIdList = x.Entries.Where(e => e.IsDaily).Select(e => e.MissionId).ToList()
                });

        user.EventShopBuyCountInfo = db.Set<UserEventShopBuy>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .AsEnumerable()
            .GroupBy(x => x.EventId)
            .ToDictionary(
                g => g.Key,
                g => new EventShopBuyCountData
                {
                    EventId = g.Key,
                    datas = g.Select(p => new EventShopProductData
                    {
                        ProductTid = p.ProductTid,
                        BuyCount = p.BuyCount
                    }).ToList()
                });
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        DomainStoreHelpers.SyncCollection(
            db.Set<UserEvent>(),
            db.Set<UserEvent>().Include(x => x.CompletedScenarios).Include(x => x.ClearedStages).Where(x => x.UserId == userId).ToList(),
            user.EventInfo,
            e => e.EventId,
            kvp => kvp.Key,
            kvp =>
            {
                var ev = new UserEvent
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    EventId = kvp.Key,
                    Diff = kvp.Value.Diff,
                    LastStage = kvp.Value.LastStage,
                    LastDay = kvp.Value.LastDay,
                    FreeTicket = kvp.Value.FreeTicket
                };
                foreach (string scenarioId in kvp.Value.CompletedScenarios.Distinct())
                {
                    db.Set<UserEventCompletedScenario>().Add(new UserEventCompletedScenario
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventId = ev.Id,
                        ScenarioId = scenarioId
                    });
                }
                foreach (int stageId in kvp.Value.ClearedStages.Distinct())
                {
                    db.Set<UserEventClearedStage>().Add(new UserEventClearedStage
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventId = ev.Id,
                        StageId = stageId
                    });
                }
                return ev;
            },
            (e, kvp) =>
            {
                e.Diff = kvp.Value.Diff;
                e.LastStage = kvp.Value.LastStage;
                e.LastDay = kvp.Value.LastDay;
                e.FreeTicket = kvp.Value.FreeTicket;

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserEventCompletedScenario>(),
                    e.CompletedScenarios,
                    kvp.Value.CompletedScenarios,
                    s => s.ScenarioId,
                    id => id,
                    id => new UserEventCompletedScenario
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventId = e.Id,
                        ScenarioId = id
                    },
                    (_, _) => { });

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserEventClearedStage>(),
                    e.ClearedStages,
                    kvp.Value.ClearedStages,
                    s => s.StageId,
                    id => id,
                    id => new UserEventClearedStage
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventId = e.Id,
                        StageId = id
                    },
                    (_, _) => { });
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserLoginEvent>(),
            db.Set<UserLoginEvent>().Include(x => x.Days).Where(x => x.UserId == userId).ToList(),
            user.LoginEventInfo,
            e => e.EventId,
            kvp => kvp.Key,
            kvp =>
            {
                var ev = new UserLoginEvent
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    EventId = kvp.Key,
                    LastDay = kvp.Value.LastDay,
                    LastDate = kvp.Value.LastDate
                };
                foreach (int day in kvp.Value.Days.Distinct())
                {
                    db.Set<UserLoginEventDay>().Add(new UserLoginEventDay
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserLoginEventId = ev.Id,
                        Day = day
                    });
                }
                return ev;
            },
            (e, kvp) =>
            {
                e.LastDay = kvp.Value.LastDay;
                e.LastDate = kvp.Value.LastDate;

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserLoginEventDay>(),
                    e.Days,
                    kvp.Value.Days,
                    d => d.Day,
                    day => day,
                    day => new UserLoginEventDay
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserLoginEventId = e.Id,
                        Day = day
                    },
                    (_, _) => { });
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserEventMission>(),
            db.Set<UserEventMission>().Include(x => x.Entries).Where(x => x.UserId == userId).ToList(),
            user.EventMissionInfo,
            e => e.EventId,
            kvp => kvp.Key,
            kvp =>
            {
                var em = new UserEventMission
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    EventId = kvp.Key,
                    LastDay = kvp.Value.LastDay,
                    LastDate = kvp.Value.LastDate,
                    AllClear = kvp.Value.AllClear
                };
                var entries = kvp.Value.MissionIdList.Select(id => (IsDaily: false, MissionId: id))
                    .Concat(kvp.Value.DailyMissionIdList.Select(id => (IsDaily: true, MissionId: id)))
                    .Distinct();
                foreach (var entry in entries)
                {
                    db.Set<UserEventMissionEntry>().Add(new UserEventMissionEntry
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventMissionId = em.Id,
                        IsDaily = entry.IsDaily,
                        MissionId = entry.MissionId
                    });
                }
                return em;
            },
            (e, kvp) =>
            {
                e.LastDay = kvp.Value.LastDay;
                e.LastDate = kvp.Value.LastDate;
                e.AllClear = kvp.Value.AllClear;

                var entries = kvp.Value.MissionIdList.Select(id => (IsDaily: false, MissionId: id))
                    .Concat(kvp.Value.DailyMissionIdList.Select(id => (IsDaily: true, MissionId: id)));
                DomainStoreHelpers.SyncCollection(
                    db.Set<UserEventMissionEntry>(),
                    e.Entries,
                    entries,
                    m => (m.IsDaily, m.MissionId),
                    m => m,
                    m => new UserEventMissionEntry
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventMissionId = e.Id,
                        IsDaily = m.IsDaily,
                        MissionId = m.MissionId
                    },
                    (_, _) => { });
            });

        var desiredShopBuys = user.EventShopBuyCountInfo
            .SelectMany(kvp => kvp.Value.datas.Select(p => (EventId: kvp.Key, p.ProductTid, p.BuyCount)));
        DomainStoreHelpers.SyncCollection(
            db.Set<UserEventShopBuy>(),
            db.Set<UserEventShopBuy>().Where(x => x.UserId == userId).ToList(),
            desiredShopBuys,
            e => (e.EventId, e.ProductTid),
            s => (s.EventId, s.ProductTid),
            s => new UserEventShopBuy
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                EventId = s.EventId,
                ProductTid = s.ProductTid,
                BuyCount = s.BuyCount
            },
            (e, s) => e.BuyCount = s.BuyCount);
    }
}
