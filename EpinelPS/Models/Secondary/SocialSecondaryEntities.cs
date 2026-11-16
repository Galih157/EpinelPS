using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EpinelPS.Models;

[Table("UserLobbyPrivateBanners")]
public class UserLobbyPrivateBannerEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int BannerId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserJukeboxPlaylists")]
public class UserJukeboxPlaylistEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public long PlaylistUid { get; set; }
    public bool IsFavorite { get; set; }
    public string Title { get; set; } = "";
    public string SongsJson { get; set; } = "[]";

    public GameUser User { get; set; } = null!;
}

[Table("UserCharacterWishlists")]
public class UserCharacterWishlistEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public int BannerId { get; set; }
    public int CharacterId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserMails")]
public class UserMailEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public long Msn { get; set; }
    public int Sender { get; set; }
    public int Type { get; set; }
    public int State { get; set; }
    public string TitleJson { get; set; } = "";
    public string TextJson { get; set; } = "";
    public long CreatedAt { get; set; }
    public string Nickname { get; set; } = "";
    public string AppNoticeId { get; set; } = "";
    public bool HasReward { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public int Period { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserMessengerRepairHistory")]
public class UserMessengerRepairHistoryEntity
{
    [Key]
    public long Id { get; set; }

    public ulong UserId { get; set; }
    public string AuditId { get; set; } = "";
    public long CreatedAtUtc { get; set; }
    public string Action { get; set; } = "";
    public string Note { get; set; } = "";
    public string MessageJson { get; set; } = "{}";

    public GameUser User { get; set; } = null!;
}
