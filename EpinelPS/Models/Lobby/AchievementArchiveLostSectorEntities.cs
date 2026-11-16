using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EpinelPS.Models;

[Table("UserCompletedAchievements")]
public class UserCompletedAchievement
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int AchievementId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserSideStoryStages")]
public class UserSideStoryStage
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int StageId { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsViewed { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserArchiveUnlocks")]
public class UserArchiveUnlock
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }

    [MaxLength(64)]
    public string Kind { get; set; } = "";

    public int TargetId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserLostSectors")]
public class UserLostSector
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int SectorId { get; set; }
    public bool IsOpen { get; set; }
    public bool IsPlaying { get; set; }
    public string StateJson { get; set; } = "";
    public string TeamPositionsJson { get; set; } = "[]";
    public int ObtainedRewards { get; set; }
    public bool ReceivedFinalReward { get; set; }
    public bool CompletedPerfectly { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserLostSectorClearedStage> ClearedStages { get; set; } = new List<UserLostSectorClearedStage>();
    public ICollection<UserLostSectorObject> Objects { get; set; } = new List<UserLostSectorObject>();
}

[Table("UserLostSectorClearedStages")]
public class UserLostSectorClearedStage
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }

    public long LostSectorId { get; set; }

    public string StageKey { get; set; } = "";
    public int ClearState { get; set; }

    public GameUser User { get; set; } = null!;
    public UserLostSector LostSector { get; set; } = null!;
}

[Table("UserLostSectorObjects")]
public class UserLostSectorObject
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }

    public long LostSectorId { get; set; }

    public string ObjectKey { get; set; } = "";
    public long ActionAt { get; set; }
    public int Type { get; set; }
    public string PositionId { get; set; } = "";
    public string Json { get; set; } = "";
    public string TeamPositionsJson { get; set; } = "[]";

    public GameUser User { get; set; } = null!;
    public UserLostSector LostSector { get; set; } = null!;
}
