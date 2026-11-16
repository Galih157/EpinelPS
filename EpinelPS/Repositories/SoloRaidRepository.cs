using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Repositories;

public sealed class SoloRaidRepository(GameContext db)
{
    private const string MuseumLogSetCurrent = "Current";
    private const string MuseumLogSetBest = "Best";

    // --- SoloRaid ---

    public SoloRaidInfo? GetSoloRaid(ulong userId, int raidId)
    {
        var raid = QueryRaids(asNoTracking: true)
            .FirstOrDefault(r => r.UserId == userId && r.RaidId == raidId);

        return raid != null ? MapRaidToDomain(raid) : null;
    }

    public void SaveSoloRaid(ulong userId, int raidId, SoloRaidInfo info)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var raidSet = db.Set<SoloRaidEntity>();
        var existing = QueryRaids(asNoTracking: false)
            .FirstOrDefault(r => r.UserId == userId && r.RaidId == raidId);

        if (existing != null)
        {
            raidSet.Remove(existing);
            db.SaveChanges();
        }

        int resolvedRaidId = info.RaidId != 0 ? info.RaidId : raidId;
        var raidEntity = new SoloRaidEntity
        {
            UserId = userId,
            RaidId = resolvedRaidId,
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

        raidSet.Add(raidEntity);
        db.SaveChanges();
    }

    // --- SoloRaidMuseum ---

    public Dictionary<int, SoloRaidMuseumStageData> GetAllMuseumStages(ulong userId)
    {
        var stages = QueryMuseumStages(asNoTracking: true)
            .Where(s => s.UserId == userId)
            .ToList();

        var result = new Dictionary<int, SoloRaidMuseumStageData>(stages.Count);
        foreach (var stage in stages)
        {
            result[stage.StageId] = MapStageToDomain(stage);
        }
        return result;
    }

    public SoloRaidMuseumStageData? GetMuseumStage(ulong userId, int stageId)
    {
        var stage = QueryMuseumStages(asNoTracking: true)
            .FirstOrDefault(s => s.UserId == userId && s.StageId == stageId);

        return stage != null ? MapStageToDomain(stage) : null;
    }

    public SoloRaidMuseumStageData GetOrCreateMuseumStage(ulong userId, int stageId)
    {
        return GetMuseumStage(userId, stageId) ?? new SoloRaidMuseumStageData { StageId = stageId };
    }

    public void SaveMuseumStage(ulong userId, SoloRaidMuseumStageData stage)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var stageSet = db.Set<SoloRaidMuseumStageEntity>();
        var existing = QueryMuseumStages(asNoTracking: false)
            .FirstOrDefault(s => s.UserId == userId && s.StageId == stage.StageId);

        if (existing != null)
        {
            stageSet.Remove(existing);
            db.SaveChanges();
        }

        var stageEntity = new SoloRaidMuseumStageEntity
        {
            UserId = userId,
            StageId = stage.StageId,
            StageMode = (int)stage.StageMode,
            IsNoLimitUnlocked = stage.IsNoLimitUnlocked,
            DebugDamageMultiplier = stage.DebugDamageMultiplier
        };

        stageEntity.Modes.Add(MapMuseumModeToEntity(SoloRaidMuseumStageMode.Challenge, stage.Challenge));
        stageEntity.Modes.Add(MapMuseumModeToEntity(SoloRaidMuseumStageMode.NoLimit, stage.NoLimit));

        foreach (int missionId in (stage.ReceivedChallengeMissions ?? Enumerable.Empty<int>()).Distinct())
        {
            stageEntity.MissionClaims.Add(new SoloRaidMuseumMissionClaimEntity
            {
                ModeType = (int)SoloRaidMuseumStageMode.Challenge,
                MissionId = missionId
            });
        }

        foreach (int missionId in (stage.ReceivedNoLimitMissions ?? Enumerable.Empty<int>()).Distinct())
        {
            stageEntity.MissionClaims.Add(new SoloRaidMuseumMissionClaimEntity
            {
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
                TeamNumber = team.TeamNumber
            };

            var seenSlots = new HashSet<int>();
            foreach (var slot in team.Slots ?? Enumerable.Empty<NetTeamSlot>())
            {
                if (!seenSlots.Add(slot.Slot))
                    continue;

                teamEntity.Slots.Add(new SoloRaidMuseumTeamSlotEntity
                {
                    Slot = slot.Slot,
                    Value = slot.Value,
                    ValueType = slot.ValueType
                });
            }

            stageEntity.Teams.Add(teamEntity);
        }

        stageSet.Add(stageEntity);
        db.SaveChanges();
    }

    private IQueryable<SoloRaidEntity> QueryRaids(bool asNoTracking)
    {
        IQueryable<SoloRaidEntity> query = db.Set<SoloRaidEntity>();
        if (asNoTracking)
            query = query.AsNoTracking();

        return query
            .Include(r => r.Levels)
                .ThenInclude(l => l.Logs)
                    .ThenInclude(g => g.Characters)
            .AsSplitQuery();
    }

    private IQueryable<SoloRaidMuseumStageEntity> QueryMuseumStages(bool asNoTracking)
    {
        IQueryable<SoloRaidMuseumStageEntity> query = db.Set<SoloRaidMuseumStageEntity>();
        if (asNoTracking)
            query = query.AsNoTracking();

        return query
            .Include(s => s.MissionClaims)
            .Include(s => s.Teams)
                .ThenInclude(t => t.Slots)
            .Include(s => s.Modes)
                .ThenInclude(m => m.OpenTeams)
            .Include(s => s.Modes)
                .ThenInclude(m => m.Logs)
                    .ThenInclude(l => l.Characters)
            .AsSplitQuery();
    }

    private static SoloRaidInfo MapRaidToDomain(SoloRaidEntity raid)
    {
        return new SoloRaidInfo
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

    private static SoloRaidMuseumStageData MapStageToDomain(SoloRaidMuseumStageEntity stage)
    {
        var challengeEntity = stage.Modes.FirstOrDefault(m => m.ModeType == (int)SoloRaidMuseumStageMode.Challenge);
        var noLimitEntity = stage.Modes.FirstOrDefault(m => m.ModeType == (int)SoloRaidMuseumStageMode.NoLimit);

        return new SoloRaidMuseumStageData
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
        SoloRaidMuseumStageMode modeType,
        SoloRaidMuseumModeData? modeData)
    {
        modeData ??= new SoloRaidMuseumModeData();

        var modeEntity = new SoloRaidMuseumModeEntity
        {
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
