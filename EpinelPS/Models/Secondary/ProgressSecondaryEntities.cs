using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("UserMapStates")]
public class UserMapStateEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public string MapId { get; set; } = string.Empty;
    public string StateJson { get; set; } = string.Empty;

    public GameUser User { get; set; } = null!;
}

[Table("UserFieldMaps")]
public class UserFieldMapEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public string MapId { get; set; } = string.Empty;
    public string DataJson { get; set; } = string.Empty;
    public long UpdatedAt { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserSubQuests")]
public class UserSubQuestEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int SubQuestId { get; set; }
    public bool ReceivedReward { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserStageClearHistory")]
public class UserStageClearHistoryEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int TeamCombat { get; set; }
    public long ClearedAt { get; set; }
    public string UserJson { get; set; } = string.Empty;
    public string SlotsJson { get; set; } = string.Empty;

    public GameUser User { get; set; } = null!;
}

[Table("UserClearedOutpostScenarios")]
public class UserClearedOutpostScenarioEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int ScenarioId { get; set; }

    public GameUser User { get; set; } = null!;
}

[Table("UserTacticAcademyLessons")]
public class UserTacticAcademyLessonEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int LessonId { get; set; }

    public GameUser User { get; set; } = null!;
}
