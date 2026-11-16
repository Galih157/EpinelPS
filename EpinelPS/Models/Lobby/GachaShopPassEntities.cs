using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EpinelPS.Models;

[Table("UserGachaBannerPulls")]
public class UserGachaBannerPull
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int BannerId { get; set; }
    public int MaxPulls { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserGachaPityCounts")]
public class UserGachaPityCount
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int BannerId { get; set; }
    public int ExecuteCount { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserGachaSelectupChoices")]
public class UserGachaSelectupChoice
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int BannerId { get; set; }
    public int CharacterTid { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserGachaDailyFreePulls")]
public class UserGachaDailyFreePull
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int GachaId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserGachaPaybacks")]
public class UserGachaPayback
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int GachaId { get; set; }
    public int GachaCount { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserGachaPaybackReward> Rewards { get; set; } = new List<UserGachaPaybackReward>();
}

[Table("UserGachaPaybackRewards")]
public class UserGachaPaybackReward
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }

    public long PaybackId { get; set; }

    public int StepId { get; set; }

    public GameUser User { get; set; } = null!;
    public UserGachaPayback Payback { get; set; } = null!;
}

[Table("UserNormalShopStates")]
public class UserNormalShopState
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int ShopCategory { get; set; }
    public int RenewCount { get; set; }
    public long RenewAt { get; set; }
    public long NextRenewAt { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserNormalShopOrder> Orders { get; set; } = new List<UserNormalShopOrder>();
    public ICollection<UserNormalShopPinnedBuy> PinnedBuys { get; set; } = new List<UserNormalShopPinnedBuy>();
}

[Table("UserNormalShopOrders")]
public class UserNormalShopOrder
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }

    public long ShopStateId { get; set; }

    public int OrderIndex { get; set; }
    public int ProductId { get; set; }
    public int BuyCount { get; set; }

    public GameUser User { get; set; } = null!;
    public UserNormalShopState ShopState { get; set; } = null!;
}

[Table("UserNormalShopPinnedBuys")]
public class UserNormalShopPinnedBuy
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }

    public long ShopStateId { get; set; }

    public int ProductId { get; set; }
    public int BuyCount { get; set; }

    public GameUser User { get; set; } = null!;
    public UserNormalShopState ShopState { get; set; } = null!;
}

[Table("UserPasses")]
public class UserPass
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int PassId { get; set; }
    public int PassPoint { get; set; }
    public int PassSkipCount { get; set; }
    public bool PremiumActive { get; set; }
    public string LastCompleteAt { get; set; } = "";

    public GameUser User { get; set; } = null!;
    public ICollection<UserPassRank> Ranks { get; set; } = new List<UserPassRank>();
    public ICollection<UserPassMission> Missions { get; set; } = new List<UserPassMission>();
}

[Table("UserPassRanks")]
public class UserPassRank
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }

    public long UserPassId { get; set; }

    public int PassRank { get; set; }
    public bool IsNormalRewarded { get; set; }
    public bool IsPremiumRewarded { get; set; }

    public GameUser User { get; set; } = null!;
    public UserPass UserPass { get; set; } = null!;
}

[Table("UserPassMissions")]
public class UserPassMission
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }

    public long UserPassId { get; set; }

    public int PassMissionId { get; set; }
    public bool IsComplete { get; set; }

    public GameUser User { get; set; } = null!;
    public UserPass UserPass { get; set; } = null!;
}
