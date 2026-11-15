using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserResetStates")]
public class UserResetStateEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int WipeoutCount { get; set; }
    public bool ClearedSimulationRoom { get; set; }
    public int InterceptionTickets { get; set; } = 3;
    public int DailyMissionPoints { get; set; }
    public int DispatchCount { get; set; }
    public int WeeklyMissionPoints { get; set; }
    public string SimRoomStateJson { get; set; } = "{}";

    public GameUser User { get; set; } = null!;
}

[Table("UserCompletedDailyMissions")]
public class UserCompletedDailyMissionEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int MissionId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserCompletedWeeklyMissions")]
public class UserCompletedWeeklyMissionEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int MissionId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserDailyTowerCounts")]
public class UserDailyTowerCountEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int CorporationType { get; set; }
    public int UsedCount { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserDailyCounselCounts")]
public class UserDailyCounselCountEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int KeyId { get; set; }
    public int Count { get; set; }

    public GameUser User { get; set; } = null!;
}
