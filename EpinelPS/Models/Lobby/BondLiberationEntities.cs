using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserBonds")]
public class UserBond
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int NameCode { get; set; }
    public int Lv { get; set; }
    public int Exp { get; set; }
    public bool CanCounselToday { get; set; }
    public bool IsFavorites { get; set; }
    public int CounseledCount { get; set; }
    public int CompleteRewardStatus { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserBondProgressItem> ProgressItems { get; set; } = new List<UserBondProgressItem>();
}

[Table("UserBondProgressItems")]
public class UserBondProgressItem
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(26)]
    public string BondId { get; set; } = "";

    [MaxLength(64)]
    public string Kind { get; set; } = "";

    public int ItemId { get; set; }

    public GameUser User { get; set; } = null!;
    public UserBond Bond { get; set; } = null!;
}

[Table("UserFavoriteItems")]
public class UserFavoriteItem
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public long FavoriteItemId { get; set; }
    public int Tid { get; set; }
    public long Csn { get; set; }
    public int Lv { get; set; }
    public int Exp { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserFavoriteItemQuests")]
public class UserFavoriteItemQuest
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int QuestId { get; set; }
    public bool Clear { get; set; }
    public bool Received { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserLiberations")]
public class UserLiberation
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int CharacterId { get; set; }
    public int Step { get; set; }
    public int ProgressPoint { get; set; }
    public int RewardedCount { get; set; }
    public bool IsCompleted { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserLiberationMission> Missions { get; set; } = new List<UserLiberationMission>();
}

[Table("UserLiberationMissions")]
public class UserLiberationMission
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(26)]
    public string LiberationId { get; set; } = "";

    public long MissionId { get; set; }
    public int MissionTid { get; set; }
    public int LiberateCharacterId { get; set; }
    public int MissionState { get; set; }
    public long CreatedAt { get; set; }
    public long TriggerStartAt { get; set; }
    public long TriggerEndAt { get; set; }
    public long ReceivedAt { get; set; }

    public GameUser User { get; set; } = null!;
    public UserLiberation Liberation { get; set; } = null!;
}

[Table("UserOpenLiberateTypes")]
public class UserOpenLiberateType
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int LiberateTypeId { get; set; }

    public GameUser User { get; set; } = null!;
}
