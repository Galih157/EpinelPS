using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.Collections;
using log4net;
using Newtonsoft.Json;

namespace EpinelPS.LobbyServer.Soloraid;

public class SoloRaidHelper
{
    private static readonly ILog log = LogManager.GetLogger(typeof(SoloRaidHelper));

    /// <summary>
    /// Open a solo raid
    /// </summary>
    public static int OpenSoloRaid(User user, int raidId, int raidLevel, SoloRaidRepository repo, SoloRaidType type = SoloRaidType.Normal)
    {
        if (raidId == 0) raidId = GetRaidId();

        // Check if the raid manager exists, if not, exit
        if (!GameData.Instance.SoloRaidManagerTable.TryGetValue(raidId, out var manager)) return 0;
        log.Debug($"Fond SoloRaidManager: RaidId: {raidId}, SoloRaidManager: {JsonConvert.SerializeObject(manager)}");

        // Get the preset for the raid level
        var preset = GameData.Instance.SoloRaidPresetTable.Values.FirstOrDefault(r =>
            r.PresetGroupId == manager.MonsterPreset && r.WaveOrder == raidLevel);
        if (preset is null) return 0; // If the preset is null, exit
        log.Debug($"Fond SoloRaidPreset: PresetGroupId: {manager.MonsterPreset}, WaveOrder: {raidLevel}, SoloRaidPreset: {JsonConvert.SerializeObject(preset)}");

        var statEnhanceId = GetStatEnhanceIdByWave(preset.Wave); // Get the stat enhance id for the wave
        if (statEnhanceId == 0) return 0; // If the stat enhance id is 0, exit

        // Get the stat enhance data for the raid level
        var statEnhance = GameData.Instance.MonsterStatEnhanceTable.Values.Where(m => m.Lv == preset.MonsterStageLv && m.GroupId == statEnhanceId);
        log.Debug($"Fond MonsterStatEnhance: Lv: {preset.MonsterStageLv}, GroupId: {statEnhanceId}, MonsterStatEnhance: {JsonConvert.SerializeObject(statEnhance)}");

        var levelHp = statEnhance.Sum(m => m.LevelHp); // Monster level hp = statEnhance.Sum(m => m.LevelHp)

        var raid = GetSoloRaidData(user, raidId, repo, isAdd: true)!;
        switch (type)
        {
            case SoloRaidType.Normal:
                raid.RaidOpenCount++; // If the raid is a normal raid, increment the raid open count
                break;
            case SoloRaidType.Trial:
                raid.TrialCount++; // If the raid is a trial raid, increment the trial count
                break;
            case SoloRaidType.Practice:
                break;
            default:
                Logging.Warn($"Unknown SoloRaidType: {type}");
                break;
        }

        var level = GetSoloRaidLevelData(raid, raidLevel, type, isOpen: true);
        // Reset the level data
        if (level != null)
        {
            raid.SoloRaidLevels.Remove(level);
        }
        level = new SoloRaidLevelData
        {
            RaidLevel = raidLevel,
            Hp = levelHp,
            Type = type,
            Status = SoloRaidStatus.Alive,
            IsClear = false,
            IsOpen = true,
        };
        raid.SoloRaidLevels.Add(level);

        repo.SaveSoloRaid(user.ID, raidId, raid);
        return type == SoloRaidType.Trial ? raid.TrialCount : raid.RaidOpenCount;
    }

    public static void CloseSoloRaid(User user, int raidId, int raidLevel, SoloRaidRepository repo, SoloRaidType type = SoloRaidType.Normal)
    {
        Logging.WriteLine($"CloseSoloRaid: raidId: {raidId}, raidLevel: {raidLevel}, type: {type}");
        if (raidId == 0) raidId = GetRaidId();

        var raid = GetSoloRaidData(user, raidId, repo, isAdd: false);
        if (raid is null) return;

        // Reset the raid data
        if (type == SoloRaidType.Normal) raid.RaidOpenCount--; // If the raid is a normal raid, decrement the raid open count
        if (raid.RaidOpenCount < 0) raid.RaidOpenCount = 0; // If the raid open count is less than 0, set it to 0
        if (type == SoloRaidType.Trial) raid.TrialCount--;
        if (raid.TrialCount < 0) raid.TrialCount = 0;

        var level = GetSoloRaidLevelData(raid, raidLevel, type, isOpen: true);
        if (level is not null) raid.SoloRaidLevels.Remove(level); // If the level is not null, remove it from the raid data
        if (type == SoloRaidType.Trial && level is not null)
        {
            // If the raid is a trial raid and the level is not null, check if the level is the highest damage level
            // If the current level's damage is higher than the previous level, update the status and remove the old level
            level.Status = SoloRaidStatus.Kill;
            level.IsClear = true;
            level.IsOpen = false;
            var oldLevel = GetSoloRaidLevelData(raid, raidLevel, SoloRaidType.Trial, isOpen: false);
            if (oldLevel is null)
            {
                raid.SoloRaidLevels.Add(level);
            }
            else if (level.TotalDamage > oldLevel.TotalDamage)
            {
                raid.SoloRaidLevels.Remove(oldLevel);
                raid.SoloRaidLevels.Add(level);
            }
        }

        repo.SaveSoloRaid(user.ID, raidId, raid);
    }

    /// <summary>
    /// Gets the user solo raid info
    /// </summary>
    public static NetUserSoloRaidInfo GetUserSoloRaidInfo(User user, SoloRaidRepository repo)
    {
        int SoloRaidManagerTid = GetRaidId();
        NetUserSoloRaidInfo info = new()
        {
            SoloRaidManagerTid = SoloRaidManagerTid,
            LastClearLevel = 0, // set the last clear level
            RaidOpenCount = 0, // set the raid open count
            Period = GetSoloRaidPeriod(),
        };

        var raidData = GetSoloRaidData(user, SoloRaidManagerTid, repo);
        if (raidData is null) return info;
        info.RaidOpenCount = raidData.RaidOpenCount;

        int lastClearLevel = GetLastClearLevelId(raidData);
        info.LastClearLevel = lastClearLevel;

        var openLevelData = raidData.SoloRaidLevels.Where(r => r.IsOpen).OrderBy(x => x.RaidLevel).FirstOrDefault();
        if (openLevelData is not null) info.LastOpenRaid = new NetSoloRaid { Level = openLevelData.RaidLevel, Type = openLevelData.Type };

        var trialLevelData = GetSoloRaidLevelData(raidData, 8, SoloRaidType.Trial);
        if (trialLevelData is null) return info;

        info.TrialCount = raidData.TrialCount;
        info.TrialDamage = trialLevelData.TotalDamage;

        return info;
    }

    public static void SetDamage(User user, ref ResSetSoloRaidDamage response, ReqSetSoloRaidDamage req, SoloRaidRepository repo)
    {
        (int rewardId, int FirstRewardId, var levelData) =
            SetDamage(user, req.RaidLevel, req.Damage, req.AntiCheatBattleData.WaveId, req.AntiCheatBattleData.Characters, repo);

        if (rewardId > 0) response.Reward = RewardUtils.RegisterRewardsForUser(user, rewardId);
        if (FirstRewardId > 0) response.FirstClearReward = RewardUtils.RegisterRewardsForUser(user, FirstRewardId);
        if (rewardId > 0 || FirstRewardId > 0)
            UserSessionStore.SaveUser(user);

        response.Info = new NetNormalSoloRaid
        {
            Level = req.RaidLevel,
            Hp = levelData.Hp - levelData.TotalDamage,
        };

        response.RaidJoinCount = levelData.RaidJoinCount;
        response.Status = levelData.Status;
        response.PeriodResult = SoloRaidPeriodResult.Success;
        response.JoinData = GetJoinData(levelData);
    }

    public static void SetDamagePractice(User user, ref ResSetSoloRaidPracticeDamage response, ReqSetSoloRaidPracticeDamage req, SoloRaidRepository repo)
    {
        (_, _, var levelData) =
            SetDamage(user, req.RaidLevel, req.Damage, req.AntiCheatBattleData.WaveId, req.AntiCheatBattleData.Characters, repo, SoloRaidType.Practice);

        response.Info = new NetPracticeSoloRaid
        {
            Level = req.RaidLevel,
            Hp = levelData.Hp - levelData.TotalDamage,
        };

        response.RaidJoinCount = levelData.RaidJoinCount;
        response.Status = levelData.Status;
        response.PeriodResult = SoloRaidPeriodResult.Success;
        response.JoinData = GetJoinData(levelData);
    }

    public static void SetDamageTrial(User user, ref ResSetSoloRaidTrialDamage response, ReqSetSoloRaidTrialDamage req, SoloRaidRepository repo)
    {
        (_, _, var levelData) =
            SetDamage(user, req.RaidLevel, req.Damage, req.AntiCheatBattleData.WaveId, req.AntiCheatBattleData.Characters, repo, SoloRaidType.Trial);

        response.Info = new NetTrialSoloRaid
        {
            Level = req.RaidLevel,
            Damage = levelData.TotalDamage,
        };

        response.RaidJoinCount = levelData.RaidJoinCount;
        response.Status = levelData.Status;
        response.PeriodResult = SoloRaidPeriodResult.Success;
        response.JoinData = GetJoinData(levelData);
    }

    public static void GetLevelInfo(User user, ref ResGetLevelSoloRaid response, int raidLevel, SoloRaidRepository repo)
    {
        int raidId = GetRaidId();

        response.PeriodResult = SoloRaidPeriodResult.Success;
        var raidData = GetSoloRaidData(user, raidId, repo, isAdd: false);
        if (raidData is null) return;
        var levelData = GetSoloRaidLevelData(raidData, raidLevel, SoloRaidType.Normal, true);
        if (levelData is null) return;

        response.Raid = new NetNormalSoloRaid
        {
            Level = raidLevel,
            Hp = levelData.Hp - levelData.TotalDamage,
        };
        response.RaidJoinCount = levelData.RaidJoinCount;
        response.JoinData = GetJoinData(levelData);
    }

    public static void GetLevelPracticeInfo(User user, ref ResGetLevelPracticeSoloRaid response, int raidLevel, SoloRaidRepository repo)
    {
        int raidId = GetRaidId();

        response.PeriodResult = SoloRaidPeriodResult.Success;
        var raidData = GetSoloRaidData(user, raidId, repo, isAdd: false);
        if (raidData is null) return;
        var levelData = GetSoloRaidLevelData(raidData, raidLevel, SoloRaidType.Practice, true);
        if (levelData is null) return;

        response.Raid = new NetPracticeSoloRaid
        {
            Level = raidLevel,
            Hp = levelData.Hp - levelData.TotalDamage,
        };
        response.RaidJoinCount = levelData.RaidJoinCount;
        response.PeriodResult = SoloRaidPeriodResult.Success;
        response.JoinData = GetJoinData(levelData);
    }

    public static void GetLevelTrialInfo(User user, ref ResGetLevelTrialSoloRaid response, int raidLevel, SoloRaidRepository repo)
    {
        int raidId = GetRaidId();

        response.PeriodResult = SoloRaidPeriodResult.Success;
        var raidData = GetSoloRaidData(user, raidId, repo, isAdd: false);
        if (raidData is null) return;
        var levelData = GetSoloRaidLevelData(raidData, raidLevel, SoloRaidType.Trial, true);
        if (levelData is null) return;

        response.Raid = new NetTrialSoloRaid
        {
            Level = raidLevel,
            Damage = levelData.TotalDamage,
        };
        response.RaidJoinCount = levelData.RaidJoinCount;
        response.PeriodResult = SoloRaidPeriodResult.Success;
        response.JoinData = GetJoinData(levelData);
    }

    public static void GetSoloRaidLog(User user, ref ResGetSoloRaidLogs response, int raidId, int raidLevel, SoloRaidRepository repo)
    {
        if (raidId == 0) raidId = GetRaidId();
        var raidData = GetSoloRaidData(user, raidId, repo);
        if (raidData is null) return;

        var levelData = GetSoloRaidLevelData(raidData, raidLevel, SoloRaidType.Normal);
        if (levelData is not null && levelData.Logs.Count > 0)
        {
            response.Logs.AddRange(levelData.Logs.Select(x => x.ToNet()));
        }
        levelData = GetSoloRaidLevelData(raidData, raidLevel, SoloRaidType.Normal, true);
        if (levelData is not null && levelData.Logs.Count > 0)
        {
            response.Logs.AddRange(levelData.Logs.Select(x => x.ToNet()));
        }

        var practiceLevelData = GetSoloRaidLevelData(raidData, raidLevel, type: SoloRaidType.Practice);
        if (practiceLevelData is not null && practiceLevelData.Logs.Count > 0)
        {
            response.PracticeLogs.AddRange(practiceLevelData.Logs.Select(x => x.ToNet()));
        }
        practiceLevelData = GetSoloRaidLevelData(raidData, raidLevel, type: SoloRaidType.Practice, true);
        if (practiceLevelData is not null && practiceLevelData.Logs.Count > 0)
        {
            response.PracticeLogs.AddRange(practiceLevelData.Logs.Select(x => x.ToNet()));
        }
    }

    public static void FastBattle(User user, ref ResFastBattleSoloRaid response, int raidId, int raidLevel, int clearCount, SoloRaidRepository repo)
    {
        var raidData = GetSoloRaidData(user, raidId, repo, isAdd: true)!;
        raidData.RaidOpenCount += clearCount;
        repo.SaveSoloRaid(user.ID, raidId, raidData);

        response.PeriodResult = SoloRaidPeriodResult.Success;
        if (GameData.Instance.SoloRaidManagerTable.TryGetValue(raidId, out var manager))
        {
            var presetData = GameData.Instance.SoloRaidPresetTable.Values.FirstOrDefault(r =>
                r.PresetGroupId == manager.MonsterPreset && r.WaveOrder == raidLevel);
            if (presetData is not null)
            {
                NetRewardData reward = new();
                for (int i = 0; i < clearCount; i++)
                {
                    var newReward = RewardUtils.RegisterRewardsForUser(user, presetData.RewardId);
                    reward.MergeFrom(newReward);
                }
                response.Reward = reward;
                UserSessionStore.SaveUser(user);
            }
        }
        response.RaidOpenCount = raidData.RaidOpenCount;
    }

    public static (int rewardId, int FirstRewardId, SoloRaidLevelData levelData)
        SetDamage(User user, int raidLevel, long damage, int waveId,
            RepeatedField<NetAntiCheatCharacter> characters, SoloRaidRepository repo, SoloRaidType type = SoloRaidType.Normal)
    {
        int rewardId = 0;
        int FirstRewardId = 0;
        int raidId = GetRaidId();

        var raidData = GetSoloRaidData(user, raidId, repo, isAdd: true)!;
        var levelData = GetSoloRaidLevelData(raidData, raidLevel, type, isOpen: true);
        var oldLevel = GetSoloRaidLevelData(raidData, raidLevel, type, isOpen: false);

        // Remove existing level data 
        if (levelData is not null)
            raidData.SoloRaidLevels.Remove(levelData);
        else
            levelData = new SoloRaidLevelData() { RaidLevel = raidLevel, Type = type, IsOpen = true };

        levelData.TotalDamage += damage;
        levelData.RaidJoinCount++;

        if (type == SoloRaidType.Trial || raidLevel == 8)
        {
            if (levelData.RaidJoinCount == 5)
            {
                levelData.Status = SoloRaidStatus.Kill;
                levelData.IsClear = true;
                levelData.IsOpen = false;
            }
            else
            {
                levelData.Status = SoloRaidStatus.Alive;
            }
        }
        else if (levelData.TotalDamage >= levelData.Hp)
        {
            levelData.TotalDamage = levelData.Hp;
            levelData.Status = SoloRaidStatus.Kill;
            if (type == SoloRaidType.Normal)
            {
                var presetData = GameData.Instance.SoloRaidPresetTable.Values.FirstOrDefault(r =>
                    r.Wave == waveId && r.WaveOrder == raidLevel);
                if (presetData is not null)
                {
                    bool isFirstClear = oldLevel is null;
                    rewardId = presetData.RewardId;
                    FirstRewardId = !isFirstClear ? 0 : presetData.FirstClearRewardId;
                }
            }

            levelData.IsClear = true;
            levelData.IsOpen = false;
        }
        else
        {
            levelData.Status = SoloRaidStatus.Alive;
        }

        SoloRaidLogData logData = new()
        {
            Damage = damage,
            Kill = levelData.Status == SoloRaidStatus.Kill,
        };

        foreach (var item in characters)
        {
            int costumeId = user.Characters.FirstOrDefault(c => c.Tid == item.Tid)?.CostumeId ?? 0;
            logData.Team.Add(new TeamCharacterData
            {
                Slot = item.Slot,
                Tid = item.Tid,
                Csn = item.Csn,
                Lv = item.CharacterSpec.Level,
                Combat = (int)item.CharacterSpec.Combat,
                CostumeId = costumeId,
            });
        }

        levelData.Logs.Add(logData);

        // If the level is not open, remove the old level data
        if (!levelData.IsOpen && oldLevel is not null)
        {
            raidData.SoloRaidLevels.Remove(oldLevel);
        }
        // If the level is not open and join count is 5, do not add to levels
        if (!(levelData.RaidJoinCount == 5 && levelData.IsOpen))
        {
            raidData.SoloRaidLevels.Add(levelData);
        }
        repo.SaveSoloRaid(user.ID, raidId, raidData);

        return (rewardId, FirstRewardId, levelData);
    }

    public static NetSoloRaidJoinData GetJoinData(SoloRaidLevelData? levelData)
    {
        var joinData = new NetSoloRaidJoinData();
        if (levelData is null || levelData.Logs.Count <= 0) return joinData;
        foreach (var item in levelData.Logs)
        {
            joinData.CsnList.AddRange(item.Team.Select(l => l.Csn));
        }

        return joinData;
    }

    /// <summary>
    /// Gets the solo raid data for the user
    /// </summary>
    public static SoloRaidInfo? GetSoloRaidData(User user, int raidId, SoloRaidRepository repo, bool isAdd = false)
    {
        var raidData = repo.GetSoloRaid(user.ID, raidId);
        if (raidData is null)
        {
            return isAdd ? new SoloRaidInfo { RaidId = raidId, LastDateDay = user.GetDateDay() } : null;
        }

        if (ResetOpenCount(user, raidData))
            repo.SaveSoloRaid(user.ID, raidId, raidData);

        return raidData;
    }

    private static int GetLastClearLevelId(SoloRaidInfo raidData)
    {
        var cleared = raidData.SoloRaidLevels.Where(r => r.IsClear && r.Type == SoloRaidType.Normal);
        return cleared.Any() ? cleared.Max(r => r.RaidLevel) : 0;
    }

    public static SoloRaidLevelData? GetSoloRaidLevelData(SoloRaidInfo raidData, int raidLevel, SoloRaidType type, bool isOpen = false)
    {
        return raidData.SoloRaidLevels.FirstOrDefault(r => r.RaidLevel == raidLevel && r.Type == type && r.IsOpen == isOpen);
    }

    public static bool ResetOpenCount(User user, SoloRaidInfo raidData)
    {
        int newDateDay = user.GetDateDay();
        if (newDateDay <= raidData.LastDateDay) return false;
        Logging.WriteLine($"Reset OpenCount: LastDateDay: {raidData.LastDateDay}, NewDateDay: {newDateDay}, Old: {raidData.RaidOpenCount}, New: 0 ", LogType.Warning);
        raidData.LastDateDay = newDateDay;
        raidData.RaidOpenCount = 0;
        raidData.TrialCount = 0;
        return true;
    }

    /// <summary>
    /// Gets the solo raid period data
    /// </summary>
    /// <returns></returns>
    public static NetSoloRaidPeriodData GetSoloRaidPeriod()
    {
        DateTime utcNow = DateTime.UtcNow.Date;
        return new NetSoloRaidPeriodData
        {
            VisibleDate = utcNow.AddDays(-10).Ticks,
            StartDate = utcNow.AddDays(-5).Ticks,
            EndDate = utcNow.AddDays(5).Ticks,
            DisableDate = utcNow.AddDays(10).Ticks,
            SettleDate = utcNow.AddDays(15).Ticks,
        };
    }

    private static int GetStatEnhanceIdByWave(int wave)
    {
        // Get the intercept data for the wave, if not found, return 0
        if (!GameData.Instance.WaveIntercept001Table.TryGetValue(wave, out var intercept)) return 0;
        log.Debug($"Fond WaveIntercept001: Wave: {wave}, WaveIntercept: {JsonConvert.SerializeObject(intercept)}");
        if (intercept.TargetList!.Count == 0) return 0;

        var monsterId = intercept.TargetList[0]; // monsterId is the first element of the TargetList
        // Get the monster data for the monsterId, if not found, return 0
        if (!GameData.Instance.MonsterTable.TryGetValue(monsterId, out var monster)) return 0;
        monster.SkillData = [];
        log.Debug($"Fond Monster: MonsterId: {monsterId}, Monster: {JsonConvert.SerializeObject(monster)}");
        return monster.StatenhanceId;
    }

    public static int GetRaidId()
    {
        return GameData.Instance.SoloRaidManagerTable.Keys.Max();
    }
}