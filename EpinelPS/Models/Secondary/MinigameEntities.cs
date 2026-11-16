using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EpinelPS.Models;

[Table("UserMinigames")]
public class UserMinigameEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public string MinigameType { get; set; } = "";
    public int ArcadeId { get; set; }
    public string DataJson { get; set; } = "";
    public long UpdatedAt { get; set; }

    public GameUser User { get; set; } = null!;
}
