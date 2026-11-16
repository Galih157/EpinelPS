using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Repositories;

public sealed class EventsRepository(GameContext db)
{
    // --- Events ---

    public Dictionary<int, EventData> GetAllEvents(ulong userId)
    {
        return db.Set<UserEvent>()
            .AsNoTracking()
            .Include(x => x.CompletedScenarios)
            .Include(x => x.ClearedStages)
            .Where(x => x.UserId == userId)
            .AsSplitQuery()
            .ToDictionary(x => x.EventId, MapEvent);
    }

    public EventData? GetEvent(ulong userId, int eventId)
    {
        var row = db.Set<UserEvent>()
            .AsNoTracking()
            .Include(x => x.CompletedScenarios)
            .Include(x => x.ClearedStages)
            .AsSplitQuery()
            .FirstOrDefault(x => x.UserId == userId && x.EventId == eventId);

        return row != null ? MapEvent(row) : null;
    }

    public EventData GetOrCreateEvent(ulong userId, int eventId, Func<EventData>? factory = null)
    {
        return GetEvent(userId, eventId) ?? (factory?.Invoke() ?? new EventData());
    }

    public bool HasCompletedEventScenario(ulong userId, string scenarioId)
    {
        return db.Set<UserEventCompletedScenario>()
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.ScenarioId == scenarioId);
    }

    public void UpsertEvent(ulong userId, int eventId, EventData data)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var eventSet = db.Set<UserEvent>();
        var scenarioSet = db.Set<UserEventCompletedScenario>();
        var stageSet = db.Set<UserEventClearedStage>();

        var row = eventSet
            .Include(x => x.CompletedScenarios)
            .Include(x => x.ClearedStages)
            .FirstOrDefault(x => x.UserId == userId && x.EventId == eventId);

        var desiredScenarios = data.CompletedScenarios.Distinct().ToHashSet();
        var desiredStages = data.ClearedStages.Distinct().ToHashSet();

        if (row == null)
        {
            row = new UserEvent
            {
                UserId = userId,
                EventId = eventId,
                Diff = data.Diff,
                LastStage = data.LastStage,
                LastDay = data.LastDay,
                FreeTicket = data.FreeTicket
            };
            eventSet.Add(row);

            foreach (string scenarioId in desiredScenarios)
            {
                scenarioSet.Add(new UserEventCompletedScenario
                {
                    UserId = userId,
                    UserEvent = row,
                    ScenarioId = scenarioId
                });
            }

            foreach (int stageId in desiredStages)
            {
                stageSet.Add(new UserEventClearedStage
                {
                    UserId = userId,
                    UserEvent = row,
                    StageId = stageId
                });
            }
        }
        else
        {
            row.Diff = data.Diff;
            row.LastStage = data.LastStage;
            row.LastDay = data.LastDay;
            row.FreeTicket = data.FreeTicket;

            var existingScenarios = row.CompletedScenarios.Select(s => s.ScenarioId).ToHashSet();
            foreach (var stale in row.CompletedScenarios.Where(s => !desiredScenarios.Contains(s.ScenarioId)).ToList())
                scenarioSet.Remove(stale);
            foreach (string scenarioId in desiredScenarios.Where(id => !existingScenarios.Contains(id)))
            {
                scenarioSet.Add(new UserEventCompletedScenario
                {
                    UserId = userId,
                    UserEvent = row,
                    ScenarioId = scenarioId
                });
            }

            var existingStages = row.ClearedStages.Select(s => s.StageId).ToHashSet();
            foreach (var stale in row.ClearedStages.Where(s => !desiredStages.Contains(s.StageId)).ToList())
                stageSet.Remove(stale);
            foreach (int stageId in desiredStages.Where(id => !existingStages.Contains(id)))
            {
                stageSet.Add(new UserEventClearedStage
                {
                    UserId = userId,
                    UserEvent = row,
                    StageId = stageId
                });
            }
        }

        db.SaveChanges();
    }

    // --- LoginEvents ---

    public LoginEventData? GetLoginEvent(ulong userId, int eventId)
    {
        var row = db.Set<UserLoginEvent>()
            .AsNoTracking()
            .Include(x => x.Days)
            .FirstOrDefault(x => x.UserId == userId && x.EventId == eventId);

        if (row == null)
            return null;

        return new LoginEventData
        {
            LastDay = row.LastDay,
            LastDate = row.LastDate,
            Days = row.Days.Select(d => d.Day).ToList()
        };
    }

    public void UpsertLoginEvent(ulong userId, int eventId, LoginEventData data)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var loginSet = db.Set<UserLoginEvent>();
        var daySet = db.Set<UserLoginEventDay>();

        var row = loginSet
            .Include(x => x.Days)
            .FirstOrDefault(x => x.UserId == userId && x.EventId == eventId);

        var desiredDays = data.Days.Distinct().ToHashSet();

        if (row == null)
        {
            row = new UserLoginEvent
            {
                UserId = userId,
                EventId = eventId,
                LastDay = data.LastDay,
                LastDate = data.LastDate
            };
            loginSet.Add(row);

            foreach (int day in desiredDays)
            {
                daySet.Add(new UserLoginEventDay
                {
                    UserId = userId,
                    UserLoginEvent = row,
                    Day = day
                });
            }
        }
        else
        {
            row.LastDay = data.LastDay;
            row.LastDate = data.LastDate;

            var existingDays = row.Days.Select(d => d.Day).ToHashSet();
            foreach (var stale in row.Days.Where(d => !desiredDays.Contains(d.Day)).ToList())
                daySet.Remove(stale);
            foreach (int day in desiredDays.Where(d => !existingDays.Contains(d)))
            {
                daySet.Add(new UserLoginEventDay
                {
                    UserId = userId,
                    UserLoginEvent = row,
                    Day = day
                });
            }
        }

        db.SaveChanges();
    }

    // --- EventMissions ---

    public EventMissionData? GetEventMission(ulong userId, int eventId)
    {
        var row = db.Set<UserEventMission>()
            .AsNoTracking()
            .Include(x => x.Entries)
            .FirstOrDefault(x => x.UserId == userId && x.EventId == eventId);

        if (row == null)
            return null;

        return new EventMissionData
        {
            LastDay = row.LastDay,
            LastDate = row.LastDate,
            AllClear = row.AllClear,
            MissionIdList = row.Entries.Where(e => !e.IsDaily).Select(e => e.MissionId).ToList(),
            DailyMissionIdList = row.Entries.Where(e => e.IsDaily).Select(e => e.MissionId).ToList()
        };
    }

    public void UpsertEventMission(ulong userId, int eventId, EventMissionData data)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var missionSet = db.Set<UserEventMission>();
        var entrySet = db.Set<UserEventMissionEntry>();

        var row = missionSet
            .Include(x => x.Entries)
            .FirstOrDefault(x => x.UserId == userId && x.EventId == eventId);

        var desiredEntries = data.MissionIdList.Select(id => (IsDaily: false, MissionId: id))
            .Concat(data.DailyMissionIdList.Select(id => (IsDaily: true, MissionId: id)))
            .ToHashSet();

        if (row == null)
        {
            row = new UserEventMission
            {
                UserId = userId,
                EventId = eventId,
                LastDay = data.LastDay,
                LastDate = data.LastDate,
                AllClear = data.AllClear
            };
            missionSet.Add(row);

            foreach (var entry in desiredEntries)
            {
                entrySet.Add(new UserEventMissionEntry
                {
                    UserId = userId,
                    UserEventMission = row,
                    IsDaily = entry.IsDaily,
                    MissionId = entry.MissionId
                });
            }
        }
        else
        {
            row.LastDay = data.LastDay;
            row.LastDate = data.LastDate;
            row.AllClear = data.AllClear;

            var existingEntries = row.Entries.Select(e => (e.IsDaily, e.MissionId)).ToHashSet();
            foreach (var stale in row.Entries.Where(e => !desiredEntries.Contains((e.IsDaily, e.MissionId))).ToList())
                entrySet.Remove(stale);
            foreach (var entry in desiredEntries.Where(e => !existingEntries.Contains(e)))
            {
                entrySet.Add(new UserEventMissionEntry
                {
                    UserId = userId,
                    UserEventMission = row,
                    IsDaily = entry.IsDaily,
                    MissionId = entry.MissionId
                });
            }
        }

        db.SaveChanges();
    }

    // --- EventShopBuys ---

    public List<EventShopProductData> GetEventShopBuys(ulong userId, int eventId)
    {
        return db.Set<UserEventShopBuy>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.EventId == eventId)
            .Select(x => new EventShopProductData
            {
                ProductTid = x.ProductTid,
                BuyCount = x.BuyCount
            })
            .ToList();
    }

    public int AddEventShopBuyCount(ulong userId, int eventId, int productTid, int quantity)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserEventShopBuy>();
        var row = set.FirstOrDefault(x => x.UserId == userId && x.EventId == eventId && x.ProductTid == productTid);
        if (row == null)
        {
            row = new UserEventShopBuy
            {
                UserId = userId,
                EventId = eventId,
                ProductTid = productTid,
                BuyCount = quantity
            };
            set.Add(row);
        }
        else
        {
            row.BuyCount += quantity;
        }

        db.SaveChanges();
        return row.BuyCount;
    }

    private static EventData MapEvent(UserEvent x) => new()
    {
        Diff = x.Diff,
        LastStage = x.LastStage,
        LastDay = x.LastDay,
        FreeTicket = x.FreeTicket,
        CompletedScenarios = x.CompletedScenarios.Select(s => s.ScenarioId).ToList(),
        ClearedStages = x.ClearedStages.Select(s => s.StageId).ToList()
    };
}
