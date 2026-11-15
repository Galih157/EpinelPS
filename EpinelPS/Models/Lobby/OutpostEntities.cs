using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("Outposts")]
public class OutpostEntity
{
    [Key]
    public ulong UserId { get; set; }

    public int DispatchLv { get; set; }
    public int DispatchCollectionLv { get; set; }
    public int DispatchFavoriteLv { get; set; }
    public int DispatchResetCount { get; set; }
    public int DispatchToday { get; set; }
    public int InfraCoreExp { get; set; }
    public int InfraCoreLvl { get; set; }
    public bool SynchroDeviceUpgraded { get; set; }
    public int SynchroDeviceLevel { get; set; }
    public int OutpostBattleLevel { get; set; }
    public int OutpostBattleExp { get; set; }
    public string OutpostBuffs { get; set; } = "{}";
    public string OutpostConditionList { get; set; } = "[]";

    public GameUser User { get; set; } = null!;
    public ICollection<DispatchClearItemEntity> DispatchClearItems { get; set; } = new List<DispatchClearItemEntity>();
    public ICollection<SelectableDispatchEntity> SelectableDispatches { get; set; } = new List<SelectableDispatchEntity>();
    public ICollection<DispatchEntity> Dispatches { get; set; } = new List<DispatchEntity>();
    public ICollection<InfraCoreRewardEntity> InfraCoreRewards { get; set; } = new List<InfraCoreRewardEntity>();
    public ICollection<SynchroSlotEntity> SynchroSlots { get; set; } = new List<SynchroSlotEntity>();
    public ICollection<ResearchProgressEntity> ResearchProgress { get; set; } = new List<ResearchProgressEntity>();
    public ICollection<OutpostBuildingEntity> OutpostBuildings { get; set; } = new List<OutpostBuildingEntity>();
}

[Table("DispatchClearItems")]
public class DispatchClearItemEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int DispatchId { get; set; }

    public OutpostEntity Outpost { get; set; } = null!;
}

[Table("SelectableDispatches")]
public class SelectableDispatchEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int DispatchGroupId { get; set; }
    public int SelectTid { get; set; }
    public int SelectSlotId { get; set; }
    public bool IsRun { get; set; }
    public long StartAt { get; set; }
    public long EndAt { get; set; }

    public OutpostEntity Outpost { get; set; } = null!;
}

[Table("Dispatches")]
public class DispatchEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int Tid { get; set; }
    public int IsRun { get; set; }
    public long StartAt { get; set; }
    public long EndAt { get; set; }

    public OutpostEntity Outpost { get; set; } = null!;
}

[Table("InfraCoreRewards")]
public class InfraCoreRewardEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int InfraCoreLevel { get; set; }

    public OutpostEntity Outpost { get; set; } = null!;
}

[Table("SynchroSlots")]
public class SynchroSlotEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int Slot { get; set; }
    public long CharacterSerialNumber { get; set; }
    public long AvailableAt { get; set; }

    public OutpostEntity Outpost { get; set; } = null!;
}

[Table("ResearchProgress")]
public class ResearchProgressEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int ResearchId { get; set; }
    public int Level { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Hp { get; set; }

    public OutpostEntity Outpost { get; set; } = null!;
}

[Table("OutpostBuildings")]
public class OutpostBuildingEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int SlotId { get; set; }
    public int BuildingId { get; set; }
    public bool IsDone { get; set; }
    public long StartAt { get; set; }
    public long CompleteAt { get; set; }

    public OutpostEntity Outpost { get; set; } = null!;
}
