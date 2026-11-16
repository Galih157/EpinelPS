using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EpinelPS.Models;

[Table("UserCurrencies")]
public class UserCurrencyEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int CurrencyType { get; set; }
    public long Amount { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserMainQuests")]
public class UserMainQuestEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int QuestTid { get; set; }
    public bool ReceivedReward { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserTowerProgress")]
public class UserTowerProgressEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int CorporationType { get; set; }
    public int ClearedFloor { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserClearedTutorials")]
public class UserClearedTutorialEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int TutorialGroupId { get; set; }
    public int TutorialId { get; set; }
    public int VersionGroup { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserContentsUnlocks")]
public class UserContentsUnlockEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int ContentsOpenId { get; set; }
    public bool ButtonAnimationPlayed { get; set; }
    public bool PopupAnimationPlayed { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserViewedNoticePopups")]
public class UserViewedNoticePopupEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int CategoryId { get; set; }
    public int NoticePopupTableId { get; set; }

    public GameUser User { get; set; } = null!;
}
