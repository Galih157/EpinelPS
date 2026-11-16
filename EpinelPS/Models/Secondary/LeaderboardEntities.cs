using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("TtsSongScores")]
public class TtsSongScoreEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public long UserId { get; set; }
    public int SongId { get; set; }
    public int RankType { get; set; }
    public int Difficulty { get; set; }
    public int Score { get; set; }
    public long UpdateTime { get; set; }
    public int IsDeleted { get; set; }
}

[Table("TtsTotalScores")]
public class TtsTotalScoreEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public long UserId { get; set; }
    public int RankType { get; set; }
    public long Score { get; set; }
    public long UpdatedAt { get; set; }
}

[Table("ArcadeScores")]
public class ArcadeScoreEntity
{
    [Key]
    public int Id { get; set; }

    public long GuildId { get; set; }
    public long UserId { get; set; }
    public long Score { get; set; }
    public int ArcadeId { get; set; }
    public int ModeId { get; set; }
}

[Table("MiniGameLeaderboards")]
public class MiniGameLeaderboardEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public string MinigameType { get; set; } = "";
    public int ArcadeId { get; set; }
    public int ModeId { get; set; }
    public ulong UserId { get; set; }
    public long GuildId { get; set; }
    public long Score { get; set; }
    public long UpdatedAt { get; set; }

    public GameUser User { get; set; } = null!;
}
