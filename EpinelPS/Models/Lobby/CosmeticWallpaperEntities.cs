using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserProfileCardDecorations")]
public class UserProfileCardDecoration
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public bool ShowCharacterSpine { get; set; }
    public int BackgroundId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserProfileCardStickers")]
public class UserProfileCardSticker
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int Position { get; set; }
    public int StickerId { get; set; }
    public int Layer { get; set; }
    public float NormalizedX { get; set; }
    public float NormalizedY { get; set; }
    public float RotationRadian { get; set; }
    public float Scale { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserUnlockedCosmetics")]
public class UserUnlockedCosmetic
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }

    [MaxLength(64)]
    public string Category { get; set; } = "";

    public int CosmeticId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserJukeboxSettings")]
public class UserJukeboxSetting
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int Location { get; set; }
    public int BgmType { get; set; }
    public int TableId { get; set; }
    public bool IsShuffle { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserClaimedJukeboxRewards")]
public class UserClaimedJukeboxReward
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int TriggerId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserWallpaperSlots")]
public class UserWallpaperSlot
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int OrderIndex { get; set; }
    public int Type { get; set; }
    public int WallpaperId { get; set; }
    public bool UseCharacterBackground { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserWallpaperBackgrounds")]
public class UserWallpaperBackground
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int OrderIndex { get; set; }
    public int LobbyDecoBackgroundId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserWallpaperJukeboxes")]
public class UserWallpaperJukebox
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int OrderIndex { get; set; }
    public int JukeboxListTableId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserWallpaperFavorites")]
public class UserWallpaperFavorite
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int OrderIndex { get; set; }
    public bool IsShuffled { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserWallpaperPlaylists")]
public class UserWallpaperPlaylist
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int OrderIndex { get; set; }
    public long PlaylistUId { get; set; }
    public bool IsShuffled { get; set; }

    public GameUser User { get; set; } = null!;
}
