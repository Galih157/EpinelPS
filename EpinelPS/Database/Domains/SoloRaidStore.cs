using EpinelPS.Data;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database.Domains;

public sealed class SoloRaidStore : IUserDomainStore
{
    private const string MuseumLogSetCurrent = "Current";
    private const string MuseumLogSetBest = "Best";

    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        var raids = db.Set<SoloRaidEntity>()
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Include(r => r.Levels)
                .ThenInclude(l => l.Logs)
                    .ThenInclude(g => g.Characters)
            .AsSplitQuery()
            .ToList();

        user.SoloRaidData = new Dictionary<int, SoloRaidInfo>();
        foreach (var raid in raids)
        {
            user.SoloRaidData[raid.RaidId] = new SoloRaidInfo
            {
                RaidId = raid.RaidId,
                RaidOpenCount = raid.RaidOpenCount,
                TrialCount = raid.TrialCount,
                LastDateDay = raid.LastDateDay,
                SoloRaidLevels = raid.Levels
                    .OrderBy(l => l.RaidLevel)
                    .ThenBy(l => l.Type)
                    .ThenBy(l => l.IsOpen)
                    .Select(l => new SoloRaidLevelData
                    {
                        RaidLevel = l.RaidLevel,
                        RaidJoinCount = l.RaidJoinCount,
                        Hp = l.Hp,
                        TotalDamage = l.TotalDamage,
                        IsClear = l.IsClear,
                        Status = (SoloRaidStatus)l.Status,
                        Type = (SoloRaidType)l.Type,
                        IsOpen = l.IsOpen,
                        Logs = l.Logs
                            .OrderBy(g => g.Sequence)
                            .Select(g => new SoloRaidLogData
                            {
                                Damage = g.Damage,
                                Kill = g.Kill,
                                Team = g.Characters
                                    .OrderBy(c => c.Slot)
                                    .Select(c => new TeamCharacterData
                                    {
                                        Slot = c.Slot,
                                        Csn = c.Csn,
                                        Tid = c.Tid,
                                        Lv = c.Lv,
                                        Combat = c.Combat,
                                        CostumeId = c.CostumeId
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        var stages = db.Set<SoloRaidMuseumStageEntity>()
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Include(s => s.MissionClaims)
            .Include(s => s.Teams)
                .ThenInclude(t => t.Slots)
            .Include(s => s.Modes)
                .ThenInclude(m => m.OpenTeams)
            .Include(s => s.Modes)
                .ThenInclude(m => m.Logs)
                    .ThenInclude(l => l.Characters)
            .AsSplitQuery()
            .ToList();

        user.SoloRaidMuseumData = new Dictionary<int, SoloRaidMuseumStageData>();
        foreach (var stage in stages)
        {
            var challengeEntity = stage.Modes.FirstOrDefault(m => m.ModeType == (int)SoloRaidMuseumStageMode.Challenge);
            var noLimitEntity = stage.Modes.FirstOrDefault(m => m.ModeType == (int)SoloRaidMuseumStageMode.NoLimit);

            var stageData = new SoloRaidMuseumStageData
            {
                StageId = stage.StageId,
                StageMode = (SoloRaidMuseumStageMode)stage.StageMode,
                IsNoLimitUnlocked = stage.IsNoLimitUnlocked,
                DebugDamageMultiplier = stage.DebugDamageMultiplier,
                Challenge = MapMuseumModeToDomain(challengeEntity),
                NoLimit = MapMuseumModeToDomain(noLimitEntity),
                ReceivedChallengeMissions = stage.MissionClaims
                    .Where(c => c.ModeType == (int)SoloRaidMuseumStageMode.Challenge)
                    .Select(c => c.MissionId)
                    .ToList(),
                ReceivedNoLimitMissions = stage.MissionClaims
                    .Where(c => c.ModeType == (int)SoloRaidMuseumStageMode.NoLimit)
                    .Select(c => c.MissionId)
                    .ToList(),
                Teams = stage.Teams
                    .OrderBy(t => t.TeamNumber)
                    .Select(t =>
                    {
                        var netTeam = new NetTeamData { TeamNumber = t.TeamNumber };
                        foreach (var slot in t.Slots.OrderBy(s => s.Slot))
                        {
                            netTeam.Slots.Add(new NetTeamSlot
                            {
                                Slot = slot.Slot,
                                Value = slot.Value,
                                ValueType = slot.ValueType
                            });
                        }
                        return netTeam;
                    })
                    .ToList()
            };

            user.SoloRaidMuseumData[stage.StageId] = stageData;
        }
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        var raidSet = db.Set<SoloRaidEntity>();
        var stageSet = db.Set<SoloRaidMuseumStageEntity>();

        var existingRaids = raidSet
            .Where(r => r.UserId == userId)
            .Include(r => r.Levels)
                .ThenInclude(l => l.Logs)
                    .ThenInclude(g => g.Characters)
            .AsSplitQuery()
            .ToList();

        var existingStages = stageSet
            .Where(s => s.UserId == userId)
            .Include(s => s.MissionClaims)
            .Include(s => s.Teams)
                .ThenInclude(t => t.Slots)
            .Include(s => s.Modes)
                .ThenInclude(m => m.OpenTeams)
            .Include(s => s.Modes)
                .ThenInclude(m => m.Logs)
                    .ThenInclude(l => l.Characters)
            .AsSplitQuery()
            .ToList();

        if (existingRaids.Count > 0 || existingStages.Count > 0)
        {
            if (existingRaids.Count > 0)
                raidSet.RemoveRange(existingRaids);
            if (existingStages.Count > 0)
                stageSet.RemoveRange(existingStages);

            db.SaveChanges();
        }

        if (user.SoloRaidData != null && user.SoloRaidData.Count > 0)
        {
            var newRaids = new List<SoloRaidEntity>(user.SoloRaidData.Count);
            foreach (var (raidIdKey, info) in user.SoloRaidData)
            {
                if (info == null)
                    continue;

                var raidEntity = new SoloRaidEntity
                {
                    UserId = userId,
                    RaidId = info.RaidId != 0 ? info.RaidId : raidIdKey,
                    RaidOpenCount = info.RaidOpenCount,
                    TrialCount = info.TrialCount,
                    LastDateDay = info.LastDateDay
                };

                var seenLevels = new HashSet<(int RaidLevel, int Type, bool IsOpen)>();
                foreach (var level in info.SoloRaidLevels ?? Enumerable.Empty<SoloRaidLevelData>())
                {
                    int typeInt = (int)level.Type;
                    if (!seenLevels.Add((level.RaidLevel, typeInt, level.IsOpen)))
                        continue;

                    var levelEntity = new SoloRaidLevelEntity
                    {
                        SoloRaidId = raidEntity.Id,
                        RaidLevel = level.RaidLevel,
                        RaidJoinCount = level.RaidJoinCount,
                        Hp = level.Hp,
                        TotalDamage = level.TotalDamage,
                        IsClear = level.IsClear,
                        Status = (int)level.Status,
                        Type = typeInt,
                        IsOpen = level.IsOpen
                    };

                    var logs = level.Logs ?? [];
                    for (int seq = 0; seq < logs.Count; seq++)
                    {
                        var log = logs[seq];
                        var logEntity = new SoloRaidLogEntity
                        {
                            SoloRaidLevelId = levelEntity.Id,
                            Sequence = seq,
                            Damage = log.Damage,
                            Kill = log.Kill
                        };

                        var seenSlots = new HashSet<int>();
                        foreach (var ch in log.Team ?? Enumerable.Empty<TeamCharacterData>())
                        {
                            if (!seenSlots.Add(ch.Slot))
                                continue;

                            logEntity.Characters.Add(new SoloRaidLogCharacterEntity
                            {
                                SoloRaidLogId = logEntity.Id,
                                Slot = ch.Slot,
                                Csn = ch.Csn,
                                Tid = ch.Tid,
                                Lv = ch.Lv,
                                Combat = ch.Combat,
                                CostumeId = ch.CostumeId
                            });
                        }

                        levelEntity.Logs.Add(logEntity);
                    }

                    raidEntity.Levels.Add(levelEntity);
                }

                newRaids.Add(raidEntity);
            }

            if (newRaids.Count > 0)
                raidSet.AddRange(newRaids);
        }

        if (user.SoloRaidMuseumData != null && user.SoloRaidMuseumData.Count > 0)
        {
            var newStages = new List<SoloRaidMuseumStageEntity>(user.SoloRaidMuseumData.Count);
            foreach (var (stageIdKey, stage) in user.SoloRaidMuseumData)
            {
                if (stage == null)
                    continue;

                var stageEntity = new SoloRaidMuseumStageEntity
                {
                    UserId = userId,
                    StageId = stage.StageId != 0 ? stage.StageId : stageIdKey,
                    StageMode = (int)stage.StageMode,
                    IsNoLimitUnlocked = stage.IsNoLimitUnlocked,
                    DebugDamageMultiplier = stage.DebugDamageMultiplier
                };

                stageEntity.Modes.Add(MapMuseumModeToEntity(stageEntity.Id, SoloRaidMuseumStageMode.Challenge, stage.Challenge));
                stageEntity.Modes.Add(MapMuseumModeToEntity(stageEntity.Id, SoloRaidMuseumStageMode.NoLimit, stage.NoLimit));

                foreach (int missionId in (stage.ReceivedChallengeMissions ?? Enumerable.Empty<int>()).Distinct())
                {
                    stageEntity.MissionClaims.Add(new SoloRaidMuseumMissionClaimEntity
                    {
                        SoloRaidMuseumStageId = stageEntity.Id,
                        ModeType = (int)SoloRaidMuseumStageMode.Challenge,
                        MissionId = missionId
                    });
                }

                foreach (int missionId in (stage.ReceivedNoLimitMissions ?? Enumerable.Empty<int>()).Distinct())
                {
                    stageEntity.MissionClaims.Add(new SoloRaidMuseumMissionClaimEntity
                    {
                        SoloRaidMuseumStageId = stageEntity.Id,
                        ModeType = (int)SoloRaidMuseumStageMode.NoLimit,
                        MissionId = missionId
                    });
                }

                var seenTeamNumbers = new HashSet<int>();
                foreach (var team in stage.Teams ?? Enumerable.Empty<NetTeamData>())
                {
                    if (!seenTeamNumbers.Add(team.TeamNumber))
                        continue;

                    var teamEntity = new SoloRaidMuseumTeamEntity
                    {
                        SoloRaidMuseumStageId = stageEntity.Id,
                        TeamNumber = team.TeamNumber
                    };

                    var seenSlots = new HashSet<int>();
                    foreach (var slot in team.Slots ?? Enumerable.Empty<NetTeamSlot>())
                    {
                        if (!seenSlots.Add(slot.Slot))
                            continue;

                        teamEntity.Slots.Add(new SoloRaidMuseumTeamSlotEntity
                        {
                            SoloRaidMuseumTeamId = teamEntity.Id,
                            Slot = slot.Slot,
                            Value = slot.Value,
                            ValueType = slot.ValueType
                        });
                    }

                    stageEntity.Teams.Add(teamEntity);
                }

                newStages.Add(stageEntity);
            }

            if (newStages.Count > 0)
                stageSet.AddRange(newStages);
        }
    }

    private static SoloRaidMuseumModeData MapMuseumModeToDomain(SoloRaidMuseumModeEntity? entity)
    {
        if (entity == null)
            return new SoloRaidMuseumModeData();

        return new SoloRaidMuseumModeData
        {
            StageJoinCount = entity.StageJoinCount,
            TotalDamage = entity.TotalDamage,
            TotalStep = entity.TotalStep,
            BestDamage = entity.BestDamage,
            BestStep = entity.BestStep,
            IsInProgress = entity.IsInProgress,
            OpenTeams = entity.OpenTeams
                .OrderBy(o => o.Sequence)
                .Select(o => o.TeamNumber)
                .ToList(),
            CurrentLogs = MapMuseumLogsToDomain(entity.Logs.Where(l => l.LogSet == MuseumLogSetCurrent)),
            Logs = MapMuseumLogsToDomain(entity.Logs.Where(l => l.LogSet == MuseumLogSetBest))
        };
    }

    private static List<SoloRaidMuseumLogData> MapMuseumLogsToDomain(IEnumerable<SoloRaidMuseumLogEntity> logs)
    {
        return logs
            .OrderBy(l => l.Sequence)
            .Select(l => new SoloRaidMuseumLogData
            {
                Damage = l.Damage,
                Step = l.Step,
                TeamNumber = l.TeamNumber,
                Team = l.Characters
                    .OrderBy(c => c.Slot)
                    .Select(c => new TeamCharacterData
                    {
                        Slot = c.Slot,
                        Csn = c.Csn,
                        Tid = c.Tid,
                        Lv = c.Lv,
                        Combat = c.Combat,
                        CostumeId = c.CostumeId
                    })
                    .ToList()
            })
            .ToList();
    }

    private static SoloRaidMuseumModeEntity MapMuseumModeToEntity(
        string stageEntityId,
        SoloRaidMuseumStageMode modeType,
        SoloRaidMuseumModeData? modeData)
    {
        modeData ??= new SoloRaidMuseumModeData();

        var modeEntity = new SoloRaidMuseumModeEntity
        {
            SoloRaidMuseumStageId = stageEntityId,
            ModeType = (int)modeType,
            StageJoinCount = modeData.StageJoinCount,
            TotalDamage = modeData.TotalDamage,
            TotalStep = modeData.TotalStep,
            BestDamage = modeData.BestDamage,
            BestStep = modeData.BestStep,
            IsInProgress = modeData.IsInProgress
        };

        int openSeq = 0;
        var seenOpenTeams = new HashSet<int>();
        foreach (int teamNumber in modeData.OpenTeams ?? Enumerable.Empty<int>())
        {
            if (!seenOpenTeams.Add(teamNumber))
                continue;

            modeEntity.OpenTeams.Add(new SoloRaidMuseumOpenTeamEntity
            {
                SoloRaidMuseumModeId = modeEntity.Id,
                TeamNumber = teamNumber,
                Sequence = openSeq++
            });
        }

        AddMuseumLogEntities(modeEntity, MuseumLogSetCurrent, modeData.CurrentLogs);
        AddMuseumLogEntities(modeEntity, MuseumLogSetBest, modeData.Logs);

        return modeEntity;
    }

    private static void AddMuseumLogEntities(
        SoloRaidMuseumModeEntity modeEntity,
        string logSet,
        List<SoloRaidMuseumLogData>? logs)
    {
        if (logs == null || logs.Count == 0)
            return;

        int seq = 0;
        var seenTeamNumbers = new HashSet<int>();
        foreach (var log in logs)
        {
            if (!seenTeamNumbers.Add(log.TeamNumber))
                continue;

            var logEntity = new SoloRaidMuseumLogEntity
            {
                SoloRaidMuseumModeId = modeEntity.Id,
                LogSet = logSet,
                TeamNumber = log.TeamNumber,
                Sequence = seq++,
                Damage = log.Damage,
                Step = log.Step
            };

            var seenSlots = new HashSet<int>();
            foreach (var ch in log.Team ?? Enumerable.Empty<TeamCharacterData>())
            {
                if (!seenSlots.Add(ch.Slot))
                    continue;

                logEntity.Characters.Add(new SoloRaidMuseumLogCharacterEntity
                {
                    SoloRaidMuseumLogId = logEntity.Id,
                    Slot = ch.Slot,
                    Csn = ch.Csn,
                    Tid = ch.Tid,
                    Lv = ch.Lv,
                    Combat = ch.Combat,
                    CostumeId = ch.CostumeId
                });
            }

            modeEntity.Logs.Add(logEntity);
        }
    }
}
