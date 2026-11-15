using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserEvents")]
public class UserEvent
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int EventId { get; set; }
    public int Diff { get; set; }
    public int LastStage { get; set; }
    public int LastDay { get; set; }
    public int FreeTicket { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserEventCompletedScenario> CompletedScenarios { get; set; } = new List<UserEventCompletedScenario>();
    public ICollection<UserEventClearedStage> ClearedStages { get; set; } = new List<UserEventClearedStage>();
}

[Table("UserEventCompletedScenarios")]
public class UserEventCompletedScenario
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(26)]
    public string UserEventId { get; set; } = "";

    public string ScenarioId { get; set; } = "";

    public GameUser User { get; set; } = null!;
    public UserEvent UserEvent { get; set; } = null!;
}

[Table("UserEventClearedStages")]
public class UserEventClearedStage
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(26)]
    public string UserEventId { get; set; } = "";

    public int StageId { get; set; }

    public GameUser User { get; set; } = null!;
    public UserEvent UserEvent { get; set; } = null!;
}

[Table("UserLoginEvents")]
public class UserLoginEvent
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int EventId { get; set; }
    public int LastDay { get; set; }
    public long LastDate { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserLoginEventDay> Days { get; set; } = new List<UserLoginEventDay>();
}

[Table("UserLoginEventDays")]
public class UserLoginEventDay
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(26)]
    public string UserLoginEventId { get; set; } = "";

    public int Day { get; set; }

    public GameUser User { get; set; } = null!;
    public UserLoginEvent UserLoginEvent { get; set; } = null!;
}

[Table("UserEventMissions")]
public class UserEventMission
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int EventId { get; set; }
    public int LastDay { get; set; }
    public long LastDate { get; set; }
    public bool AllClear { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserEventMissionEntry> Entries { get; set; } = new List<UserEventMissionEntry>();
}

[Table("UserEventMissionEntries")]
public class UserEventMissionEntry
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(26)]
    public string UserEventMissionId { get; set; } = "";

    public bool IsDaily { get; set; }
    public int MissionId { get; set; }

    public GameUser User { get; set; } = null!;
    public UserEventMission UserEventMission { get; set; } = null!;
}

[Table("UserEventShopBuys")]
public class UserEventShopBuy
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int EventId { get; set; }
    public int ProductTid { get; set; }
    public int BuyCount { get; set; }

    public GameUser User { get; set; } = null!;
}
