namespace EpinelPS.Models;

public class GameUser
{
    public ulong ID { get; set; }
    public string Nickname { get; set; } = "Player";
    public string Description { get; set; } = "";
    public DateTime LastAction { get; set; }

    public int ProfileIconId { get; set; } = 30100;
    public bool ProfileIconIsPrism { get; set; }
    public int ProfileFrame { get; set; } = 1;
    public int TitleId { get; set; } = 1;

    public int LastNormalStageCleared { get; set; }
    public int LastStoryStageCleared { get; set; }
    public int LastHardStageCleared { get; set; }
    public int LastClearedDifficulty { get; set; }

    public DateTime LastReset { get; set; } = DateTime.MinValue;
    public DateTime LastWeeklyReset { get; set; } = DateTime.MinValue;

    public int LastTriggerId { get; set; }
    public long LastMessageId { get; set; }
    public long LastBadgeSeq { get; set; }
    public DateTime BattleTime { get; set; } = DateTime.UtcNow;

    public int CurrentLiberateCharacterId { get; set; }
    public bool DailyDiscountUsed { get; set; }
    public bool SickPullsEnabled { get; set; }

    public int UserLevel { get; set; } = 1;
    public int ExperiencePoint { get; set; }
    public int? GuildId { get; set; }
    public long? GuildLeaveAt { get; set; }

    public ICollection<TriggerModelNew> Triggers { get; set; } = new List<TriggerModelNew>();
    public ICollection<UserBan> Bans { get; set; } = new List<UserBan>();
}
