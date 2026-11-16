using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserMinigames")]
public class UserMinigameEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public string MinigameType { get; set; } = "";
    public int ArcadeId { get; set; }
    public string DataJson { get; set; } = "";
    public long UpdatedAt { get; set; }

    public GameUser User { get; set; } = null!;
}
