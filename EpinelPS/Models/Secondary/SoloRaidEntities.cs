using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EpinelPS.Utils;

namespace EpinelPS.Models;

[Table("SoloRaids")]
public class SoloRaidEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int RaidId { get; set; }
    public int RaidOpenCount { get; set; }
    public int TrialCount { get; set; }
    public int LastDateDay { get; set; }

    public GameUser User { get; set; } = null!;
    public ICollection<SoloRaidLevelEntity> Levels { get; set; } = new List<SoloRaidLevelEntity>();
}

[Table("SoloRaidLevels")]
public class SoloRaidLevelEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidId { get; set; } = "";

    public int RaidLevel { get; set; }
    public int RaidJoinCount { get; set; }
    public long Hp { get; set; }
    public long TotalDamage { get; set; }
    public bool IsClear { get; set; }
    public int Status { get; set; }
    public int Type { get; set; }
    public bool IsOpen { get; set; }

    public SoloRaidEntity SoloRaid { get; set; } = null!;
    public ICollection<SoloRaidLogEntity> Logs { get; set; } = new List<SoloRaidLogEntity>();
}

[Table("SoloRaidLogs")]
public class SoloRaidLogEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidLevelId { get; set; } = "";

    public int Sequence { get; set; }
    public long Damage { get; set; }
    public bool Kill { get; set; }

    public SoloRaidLevelEntity SoloRaidLevel { get; set; } = null!;
    public ICollection<SoloRaidLogCharacterEntity> Characters { get; set; } = new List<SoloRaidLogCharacterEntity>();
}

[Table("SoloRaidLogCharacters")]
public class SoloRaidLogCharacterEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidLogId { get; set; } = "";

    public int Slot { get; set; }
    public long Csn { get; set; }
    public int Tid { get; set; }
    public int Lv { get; set; }
    public int Combat { get; set; }
    public int CostumeId { get; set; }

    public SoloRaidLogEntity SoloRaidLog { get; set; } = null!;
}

[Table("SoloRaidMuseumStages")]
public class SoloRaidMuseumStageEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    public ulong UserId { get; set; }
    public int StageId { get; set; }
    public int StageMode { get; set; }
    public bool IsNoLimitUnlocked { get; set; }
    public double DebugDamageMultiplier { get; set; } = 1;

    public GameUser User { get; set; } = null!;
    public ICollection<SoloRaidMuseumModeEntity> Modes { get; set; } = new List<SoloRaidMuseumModeEntity>();
    public ICollection<SoloRaidMuseumMissionClaimEntity> MissionClaims { get; set; } = new List<SoloRaidMuseumMissionClaimEntity>();
    public ICollection<SoloRaidMuseumTeamEntity> Teams { get; set; } = new List<SoloRaidMuseumTeamEntity>();
}

[Table("SoloRaidMuseumModes")]
public class SoloRaidMuseumModeEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidMuseumStageId { get; set; } = "";

    public int ModeType { get; set; }
    public int StageJoinCount { get; set; }
    public long TotalDamage { get; set; }
    public int TotalStep { get; set; }
    public long BestDamage { get; set; }
    public int BestStep { get; set; }
    public bool IsInProgress { get; set; }

    public SoloRaidMuseumStageEntity Stage { get; set; } = null!;
    public ICollection<SoloRaidMuseumOpenTeamEntity> OpenTeams { get; set; } = new List<SoloRaidMuseumOpenTeamEntity>();
    public ICollection<SoloRaidMuseumLogEntity> Logs { get; set; } = new List<SoloRaidMuseumLogEntity>();
}

[Table("SoloRaidMuseumMissionClaims")]
public class SoloRaidMuseumMissionClaimEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidMuseumStageId { get; set; } = "";

    public int ModeType { get; set; }
    public int MissionId { get; set; }

    public SoloRaidMuseumStageEntity Stage { get; set; } = null!;
}

[Table("SoloRaidMuseumTeams")]
public class SoloRaidMuseumTeamEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidMuseumStageId { get; set; } = "";

    public int TeamNumber { get; set; }

    public SoloRaidMuseumStageEntity Stage { get; set; } = null!;
    public ICollection<SoloRaidMuseumTeamSlotEntity> Slots { get; set; } = new List<SoloRaidMuseumTeamSlotEntity>();
}

[Table("SoloRaidMuseumTeamSlots")]
public class SoloRaidMuseumTeamSlotEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidMuseumTeamId { get; set; } = "";

    public int Slot { get; set; }
    public long Value { get; set; }
    public int ValueType { get; set; }

    public SoloRaidMuseumTeamEntity Team { get; set; } = null!;
}

[Table("SoloRaidMuseumOpenTeams")]
public class SoloRaidMuseumOpenTeamEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidMuseumModeId { get; set; } = "";

    public int TeamNumber { get; set; }
    public int Sequence { get; set; }

    public SoloRaidMuseumModeEntity Mode { get; set; } = null!;
}

[Table("SoloRaidMuseumLogs")]
public class SoloRaidMuseumLogEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidMuseumModeId { get; set; } = "";

    [MaxLength(16)]
    public string LogSet { get; set; } = "";

    public int TeamNumber { get; set; }
    public int Sequence { get; set; }
    public long Damage { get; set; }
    public int Step { get; set; }

    public SoloRaidMuseumModeEntity Mode { get; set; } = null!;
    public ICollection<SoloRaidMuseumLogCharacterEntity> Characters { get; set; } = new List<SoloRaidMuseumLogCharacterEntity>();
}

[Table("SoloRaidMuseumLogCharacters")]
public class SoloRaidMuseumLogCharacterEntity
{
    [Key]
    [MaxLength(26)]
    public string Id { get; set; } = UlidGenerator.NewUlid();

    [MaxLength(26)]
    public string SoloRaidMuseumLogId { get; set; } = "";

    public int Slot { get; set; }
    public long Csn { get; set; }
    public int Tid { get; set; }
    public int Lv { get; set; }
    public int Combat { get; set; }
    public int CostumeId { get; set; }

    public SoloRaidMuseumLogEntity Log { get; set; } = null!;
}
