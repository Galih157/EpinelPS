using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserTeamGroups")]
public class UserTeamGroupEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int TeamType { get; set; }
    public int LastContentsTeamNumber { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<UserTeamSlotEntity> Slots { get; set; } = new List<UserTeamSlotEntity>();
}

[Table("UserTeamSlots")]
public class UserTeamSlotEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(26)]
    public string TeamGroupId { get; set; } = "";

    public int TeamNumber { get; set; }
    public int Slot { get; set; }
    public long Value { get; set; }
    public int ValueType { get; set; }

    public GameUser User { get; set; } = null!;
    public UserTeamGroupEntity TeamGroup { get; set; } = null!;
}

[Table("UserRepresentationTeamSlots")]
public class UserRepresentationTeamSlotEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int Slot { get; set; }
    public long CharacterCsn { get; set; }

    public GameUser User { get; set; } = null!;
}
