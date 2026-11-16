using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace EpinelPS.Models;

[Table("SoloRaids")]
public class SoloRaidEntity
{
    [Key]
    public long Id { get; set; }

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
    public long Id { get; set; }

    public long SoloRaidId { get; set; }

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
    public long Id { get; set; }

    public long SoloRaidLevelId { get; set; }

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
    public long Id { get; set; }

    public long SoloRaidLogId { get; set; }

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
    public long Id { get; set; }

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
    public long Id { get; set; }

    public long SoloRaidMuseumStageId { get; set; }

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
    public long Id { get; set; }

    public long SoloRaidMuseumStageId { get; set; }

    public int ModeType { get; set; }
    public int MissionId { get; set; }

    public SoloRaidMuseumStageEntity Stage { get; set; } = null!;
}

[Table("SoloRaidMuseumTeams")]
public class SoloRaidMuseumTeamEntity
{
    [Key]
    public long Id { get; set; }

    public long SoloRaidMuseumStageId { get; set; }

    public int TeamNumber { get; set; }

    public SoloRaidMuseumStageEntity Stage { get; set; } = null!;
    public ICollection<SoloRaidMuseumTeamSlotEntity> Slots { get; set; } = new List<SoloRaidMuseumTeamSlotEntity>();
}

[Table("SoloRaidMuseumTeamSlots")]
public class SoloRaidMuseumTeamSlotEntity
{
    [Key]
    public long Id { get; set; }

    public long SoloRaidMuseumTeamId { get; set; }

    public int Slot { get; set; }
    public long Value { get; set; }
    public int ValueType { get; set; }

    public SoloRaidMuseumTeamEntity Team { get; set; } = null!;
}

[Table("SoloRaidMuseumOpenTeams")]
public class SoloRaidMuseumOpenTeamEntity
{
    [Key]
    public long Id { get; set; }

    public long SoloRaidMuseumModeId { get; set; }

    public int TeamNumber { get; set; }
    public int Sequence { get; set; }

    public SoloRaidMuseumModeEntity Mode { get; set; } = null!;
}

[Table("SoloRaidMuseumLogs")]
public class SoloRaidMuseumLogEntity
{
    [Key]
    public long Id { get; set; }

    public long SoloRaidMuseumModeId { get; set; }

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
    public long Id { get; set; }

    public long SoloRaidMuseumLogId { get; set; }

    public int Slot { get; set; }
    public long Csn { get; set; }
    public int Tid { get; set; }
    public int Lv { get; set; }
    public int Combat { get; set; }
    public int CostumeId { get; set; }

    public SoloRaidMuseumLogEntity Log { get; set; } = null!;
}
