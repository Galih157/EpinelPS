using System.ComponentModel.DataAnnotations;

namespace EpinelPS.Models;

public class UserBan
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public bool IsBanned { get; set; }
    public DateTime BanStart { get; set; }
    public DateTime BanEnd { get; set; }
    public int BanId { get; set; }

    public GameUser User { get; set; } = null!;
}
