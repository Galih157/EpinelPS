using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EpinelPS.Models;

[Table("TtsSongScores")]
public class TtsSongScoreEntity
{
    [Key]
    public long Id { get; set; }

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
    public long Id { get; set; }

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
    public long Id { get; set; }

    public string MinigameType { get; set; } = "";
    public int ArcadeId { get; set; }
    public int ModeId { get; set; }
    public ulong UserId { get; set; }
    public long GuildId { get; set; }
    public long Score { get; set; }
    public long UpdatedAt { get; set; }

    public GameUser User { get; set; } = null!;
}
