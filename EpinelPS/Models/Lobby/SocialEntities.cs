using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserMessages")]
public class UserMessage
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public long Seq { get; set; }
    public string ConversationId { get; set; } = "";
    public string MessageId { get; set; } = "";
    public long CreatedAt { get; set; }
    public int State { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserPickedMessages")]
public class UserPickedMessage
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public string ConversationId { get; set; } = "";
    public long CreatedAtSeconds { get; set; }
    public int CreatedAtNanos { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserBadges")]
public class UserBadge
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public long Seq { get; set; }
    public string Location { get; set; } = "";
    public int BadgeContent { get; set; }
    public string BadgeGuid { get; set; } = "";

    public GameUser User { get; set; } = null!;
}
