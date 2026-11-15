using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("Items")]
public class ItemEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public long Isn { get; set; }
    public int ItemType { get; set; }
    public long Csn { get; set; }
    public int Count { get; set; }
    public int Level { get; set; }
    public int Exp { get; set; }
    public int Position { get; set; }
    public int Corp { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<ItemCharacterAssignmentEntity> CharacterAssignments { get; set; } = new List<ItemCharacterAssignmentEntity>();
    public ICollection<EquipmentAwakeningEntity> EquipmentAwakenings { get; set; } = new List<EquipmentAwakeningEntity>();
}

[Table("ItemCharacterAssignments")]
public class ItemCharacterAssignmentEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(26)]
    public string ItemId { get; set; } = "";

    public long CharacterCsn { get; set; }
    public int Position { get; set; }

    public GameUser User { get; set; } = null!;
    public ItemEntity Item { get; set; } = null!;
    public CharacterEntity Character { get; set; } = null!;
}

[Table("Characters")]
public class CharacterEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public long Csn { get; set; }
    public int Tid { get; set; }
    public int CostumeId { get; set; }
    public int Level { get; set; }
    public int UltimateLevel { get; set; }
    public int Skill1Lvl { get; set; }
    public int Skill2Lvl { get; set; }
    public int Grade { get; set; }
    public bool IsMainForce { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<ItemCharacterAssignmentEntity> ItemAssignments { get; set; } = new List<ItemCharacterAssignmentEntity>();
}

[Table("EquipmentAwakenings")]
public class EquipmentAwakeningEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string ItemId { get; set; } = "";

    public bool IsNewData { get; set; }
    public int Option1Id { get; set; }
    public bool Option1Lock { get; set; }
    public bool IsOption1DisposableLock { get; set; }
    public int Option2Id { get; set; }
    public bool Option2Lock { get; set; }
    public bool IsOption2DisposableLock { get; set; }
    public int Option3Id { get; set; }
    public bool Option3Lock { get; set; }
    public bool IsOption3DisposableLock { get; set; }

    public ItemEntity Item { get; set; } = null!;
}
