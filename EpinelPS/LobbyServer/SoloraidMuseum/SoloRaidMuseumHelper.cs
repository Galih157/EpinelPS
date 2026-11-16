using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.LobbyServer.SoloraidMuseum;

internal static class SoloRaidMuseumHelper
{
    private const int MaxTeamCount = 5;

    public static SoloRaidMuseumStageData GetStage(ulong userId, int stageId, SoloRaidRepository repo)
        => repo.GetOrCreateMuseumStage(userId, stageId);

    public static ResGetSoloRaidMuseumData GetData(User user, SoloRaidRepository repo)
    {
        ResGetSoloRaidMuseumData response = new();
        var existingStages = repo.GetAllMuseumStages(user.ID);
        var groups = GameData.Instance.MuseumGroupTable.Values.OrderBy(x => x.Order).ToList();
        foreach (var group in groups)
        {
            NetUserSoloRaidMuseumGroupInfo info = new() { GroupId = group.Id };
            foreach (var stage in GameData.Instance.MuseumStageTable.Values
                         .Where(x => x.GroupId == group.Id).OrderBy(x => x.Order))
            {
                var data = existingStages.TryGetValue(stage.Id, out var found)
                    ? found
                    : new SoloRaidMuseumStageData { StageId = stage.Id };
                RefreshNoLimitUnlock(data);
                info.StageModeList.Add(ToMode(data));
                info.ChallengeStageInfoList.Add(ToInfo(data, data.Challenge, false));
                info.NoLimitStageInfoList.Add(ToInfo(data, data.NoLimit, true));
            }
            response.GroupInfoList.Add(info);
        }

        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
        response.WeeklyBuff = new NetSoloRaidMuseumWeeklyBuff
        {
            CurrentMuseumWeeklyBuffGroupTableId = GameData.Instance.MuseumWeeklyBuffGroupTable.Keys.DefaultIfEmpty().Max(),
            WeeklyBuffStartDate = Timestamp.FromDateTime(DateTime.SpecifyKind(weekStart, DateTimeKind.Utc)),
            WeeklyBuffEndDate = Timestamp.FromDateTime(DateTime.SpecifyKind(weekStart.AddDays(7), DateTimeKind.Utc)),
            WeeklyBuffSeasonEndDate = Timestamp.FromDateTime(DateTime.SpecifyKind(weekStart.AddDays(7), DateTimeKind.Utc)),
        };
        return response;
    }

    public static ResGetSoloRaidMuseumGroupData GetGroupData(
        User user, int groupId, SoloRaidRepository repo, TeamRepository teamRepo)
    {
        ResGetSoloRaidMuseumGroupData response = new();
        var existingStages = repo.GetAllMuseumStages(user.ID);
        var stages = GameData.Instance.MuseumStageTable.Values
            .Where(x => x.GroupId == groupId).OrderBy(x => x.Order).ToList();
        foreach (var stage in stages)
        {
            var data = existingStages.TryGetValue(stage.Id, out var found)
                ? found
                : new SoloRaidMuseumStageData { StageId = stage.Id };
            RefreshNoLimitUnlock(data);
            var active = data.StageMode == SoloRaidMuseumStageMode.NoLimit ? data.NoLimit : data.Challenge;
            var teams = GetTeams(user, data, teamRepo).ToList();
            response.StageBattleDataList.Add(new NetSoloRaidMuseumStageBattleData
            {
                StageId = stage.Id,
                // The client also uses these values after a completed run to
                // open the result/battle flow.
                StageJoinCount = active.StageJoinCount,
                TotalDamage = active.TotalDamage,
                TotalStep = active.TotalStep,
                JoinData = active.IsInProgress ? JoinData(active) : new NetSoloRaidMuseumJoinData(),
                Teams = { teams },
            });
            // StageReadyPage.UpdateUI requires UserRanking for the open button.
            // Ranking missions also require exact 50/30/15/5/1 entries below.
            var challenge = data.Challenge;
            var rankerRankings = GetPastRankings(user, stage).ToList();
            Logging.WriteLine(
                $"[SoloRaidMuseum] groupdata stage={stage.Id}, selectedMode={data.StageMode}, " +
                $"challenge(inProgress={data.Challenge.IsInProgress},join={data.Challenge.StageJoinCount},damage={data.Challenge.TotalDamage},teams={data.Challenge.OpenTeams.Count}), " +
                $"nolimit(inProgress={data.NoLimit.IsInProgress},join={data.NoLimit.StageJoinCount},damage={data.NoLimit.TotalDamage},teams={data.NoLimit.OpenTeams.Count}), " +
                $"activeTeams={teams.Count}, userRanking(rank={pastRankingRank(challenge.BestDamage, rankerRankings)},damage={challenge.BestDamage}), " +
                $"rankers=[{string.Join(',', rankerRankings.Select(x => $"{x.Ranking}:{x.Damage}:past={(x.PastUserData != null)}:current={(x.CurrentUserData != null)}"))}]",
                LogType.Info);
            NetSoloRaidMuseumStagePastRanking pastRanking = new()
            {
                StageId = stage.Id,
                UserRanking = new NetSoloRaidMuseumUserRankingData
                {
                    Ranking = 0,
                    Damage = challenge.BestDamage,
                    CurrentUserData = LobbyHandler.CreateWholeUserDataFromDbUser(user),
                },
                TotalUserCount = Math.Max(50, rankerRankings.Count),
            };
            pastRanking.RankerRankingList.AddRange(rankerRankings);
            response.PastRankingList.Add(pastRanking);
        }
        return response;

        static long pastRankingRank(long damage, IReadOnlyCollection<NetSoloRaidMuseumRankerRankingData> rankers) =>
            rankers.FirstOrDefault(x => x.Damage == damage)?.Ranking ?? 0;
    }

    public static ResGetSoloRaidMuseumMission GetMissions(ulong userId, int stageId, SoloRaidRepository repo)
    {
        var allStages = repo.GetAllMuseumStages(userId);
        var stage = allStages.TryGetValue(stageId, out var found)
            ? found
            : new SoloRaidMuseumStageData { StageId = stageId };
        ResGetSoloRaidMuseumMission response = new();
        var missions = GameData.Instance.MuseumMissionTable.Values
            .Where(x => x.StageId == stageId).OrderBy(x => x.Order).ToList();
        foreach (var mission in missions)
        {
            var mode = mission.ModeType == MuseumStageModeType.NoLimit ? stage.NoLimit : stage.Challenge;
            var received = mission.ModeType == MuseumStageModeType.NoLimit
                ? stage.ReceivedNoLimitMissions.Contains(mission.Id)
                : stage.ReceivedChallengeMissions.Contains(mission.Id);
            var net = new NetSoloRaidMuseumMissionData
            {
                MissionId = mission.Id,
                Progress = MissionProgress(mode, mission.ConditionType),
                IsReceived = received,
            };
            if (mission.ModeType == MuseumStageModeType.NoLimit)
                response.NoLimitMissionDataList.Add(net);
            else
                response.ChallengeMissionDataList.Add(net);
        }
        Logging.WriteLine($"[SoloRaidMuseum] mission stage={stageId}, challenge={response.ChallengeMissionDataList.Count}, noLimit={response.NoLimitMissionDataList.Count}", LogType.Debug);
        var points = GetRankingPoints(allStages, stageId);
        response.MuseumTotalRankingPoint = points.total;
        response.MuseumGroupRankingPoint = points.group;
        return response;
    }

    public static (int total, int group) GetRankingPoints(ulong userId, int stageId, SoloRaidRepository repo)
        => GetRankingPoints(repo.GetAllMuseumStages(userId), stageId);

    private static (int total, int group) GetRankingPoints(
        IReadOnlyDictionary<int, SoloRaidMuseumStageData> stages, int stageId)
    {
        var groupId = GameData.Instance.MuseumStageTable.TryGetValue(stageId, out var selectedStage)
            ? selectedStage.GroupId : 0;
        var total = 0;
        var group = 0;
        foreach (var stage in stages.Values)
        {
            foreach (var missionId in stage.ReceivedChallengeMissions.Concat(stage.ReceivedNoLimitMissions))
            {
                if (!GameData.Instance.MuseumMissionTable.TryGetValue(missionId, out var mission)) continue;
                total += mission.RankingPointAmount;
                if (GameData.Instance.MuseumStageTable.TryGetValue(mission.StageId, out var missionStage) &&
                    missionStage.GroupId == groupId)
                    group += mission.RankingPointAmount;
            }
        }
        return (total, group);
    }

    public static NetUserSoloRaidMuseumStageMode SetMode(
        ulong userId, int stageId, SoloRaidMuseumStageMode mode, SoloRaidRepository repo)
    {
        var stage = GetStage(userId, stageId, repo);
        if (mode == SoloRaidMuseumStageMode.NoLimit && !stage.IsNoLimitUnlocked)
            mode = SoloRaidMuseumStageMode.Challenge;
        stage.StageMode = mode;
        repo.SaveMuseumStage(userId, stage);
        return ToMode(stage);
    }

    public static SoloRaidMuseumModeData Open(
        User user, int stageId, bool noLimit, IEnumerable<int> teams,
        SoloRaidRepository repo, TeamRepository teamRepo)
    {
        var stage = GetStage(user.ID, stageId, repo);
        var mode = noLimit ? stage.NoLimit : stage.Challenge;
        var requestedTeams = teams.Distinct().Take(MaxTeamCount - 1).ToList();
        mode.CurrentLogs = mode.Logs
            .Where(x => requestedTeams.Contains(x.TeamNumber))
            .Select(x =>
            {
                var log = CloneLog(x);
                if (log.Team.Count == 0)
                    log.Team = CreateTeamSnapshot(user, stage, log.TeamNumber, teamRepo);
                log.Step = noLimit ? GetNoLimitStep(stageId, log.Damage) : 0;
                return log;
            })
            .ToList();
        mode.StageJoinCount = mode.CurrentLogs.Count;
        mode.TotalDamage = mode.CurrentLogs.Sum(x => x.Damage);
        mode.TotalStep = mode.CurrentLogs.Sum(x => x.Step);
        mode.IsInProgress = true;
        mode.OpenTeams = requestedTeams;
        repo.SaveMuseumStage(user.ID, stage);
        return mode;
    }

    public static void Close(ulong userId, int stageId, bool noLimit, SoloRaidRepository repo)
    {
        var stage = GetStage(userId, stageId, repo);
        var mode = noLimit ? stage.NoLimit : stage.Challenge;
        Logging.WriteLine($"[SoloRaidMuseum] close stage={stageId}, noLimit={noLimit}, joinCount={mode.StageJoinCount}, currentLogs={mode.CurrentLogs.Count}, totalDamage={mode.TotalDamage}", LogType.Info);
        mode.IsInProgress = false;
        repo.SaveMuseumStage(userId, stage);
    }

    public static (SoloRaidMuseumStageData stage, SoloRaidMuseumModeData mode) SetDamage(
        User user, int stageId, bool noLimit, NetSoloRaidMuseumBattleData? battle,
        SoloRaidRepository repo, TeamRepository teamRepo, int battleResult = 0)
    {
        var stage = GetStage(user.ID, stageId, repo);
        var mode = noLimit ? stage.NoLimit : stage.Challenge;
        var clientDamage = Math.Max(0, battle?.Damage ?? 0);
        var multiplier = double.IsFinite(stage.DebugDamageMultiplier) && stage.DebugDamageMultiplier >= 1
            ? Math.Min(stage.DebugDamageMultiplier, 1000)
            : 1;
        var damage = (long)Math.Min(long.MaxValue, Math.Round(clientDamage * multiplier, MidpointRounding.AwayFromZero));
        Logging.WriteLine($"[SoloRaidMuseum] set damage stage={stageId}, noLimit={noLimit}, team={battle?.Team ?? 0}, clientDamage={clientDamage}, multiplier={multiplier:0.###}, damage={damage}, battleResult={battleResult}, beforeJoin={mode.StageJoinCount}, inProgress={mode.IsInProgress}", LogType.Info);

        // NK.BattleResult: Return (3) is an in-battle retreat and consumes the
        // attempt. Retry (4) and Maintenance (6) are replay/service failures
        // and must not create a museum record.
        if (battleResult is 4 or 6)
        {
            Logging.WriteLine($"[SoloRaidMuseum] ignored non-consuming result={battleResult}", LogType.Info);
            return (stage, mode);
        }
        if (!mode.IsInProgress)
        {
            mode.IsInProgress = true;
            mode.CurrentLogs.Clear();
            mode.OpenTeams.Clear();
        }

        var teamNumber = battle?.Team ?? 0;
        mode.CurrentLogs.RemoveAll(x => x.TeamNumber == teamNumber);
        mode.CurrentLogs.Add(new SoloRaidMuseumLogData
        {
            Damage = damage,
            Step = noLimit ? GetNoLimitStep(stageId, damage) : 0,
            TeamNumber = teamNumber,
            Team = CreateTeamSnapshot(user, stage, teamNumber, teamRepo),
        });
        mode.StageJoinCount = mode.CurrentLogs.Count;
        mode.TotalDamage = mode.CurrentLogs.Sum(x => x.Damage);
        mode.TotalStep = mode.CurrentLogs.Sum(x => x.Step);

        if (mode.StageJoinCount >= MaxTeamCount)
        {
            mode.IsInProgress = false;
            // NoLimit records are ordered by total step first. Damage only breaks
            // ties, matching the result screen's best-step/best-damage comparison.
            var isNewRecord = noLimit
                ? mode.TotalStep > mode.BestStep ||
                  mode.TotalStep == mode.BestStep && mode.TotalDamage > mode.BestDamage
                : mode.TotalDamage > mode.BestDamage;
            if (isNewRecord)
            {
                if (noLimit)
                {
                    mode.BestStep = mode.TotalStep;
                    mode.BestDamage = mode.TotalDamage;
                }
                else
                {
                    mode.BestDamage = mode.TotalDamage;
                }
                mode.Logs = mode.CurrentLogs.Select(CloneLog).ToList();
            }

            if (!noLimit)
                stage.IsNoLimitUnlocked = HasClearedChallenge(stage.StageId, mode.BestDamage);
        }
        repo.SaveMuseumStage(user.ID, stage);
        return (stage, mode);
    }

    public static IEnumerable<NetSoloRaidMuseumLog> GetLogs(
        User user, int stageId, bool noLimit, SoloRaidRepository repo, TeamRepository teamRepo, bool current = false)
    {
        var stage = GetStage(user.ID, stageId, repo);
        var mode = noLimit ? stage.NoLimit : stage.Challenge;
        // The normal log endpoint represents the current/most recently completed
        // run. Keep it available for the result screen after the fifth team.
        var logs = current && mode.CurrentLogs.Count > 0 ? mode.CurrentLogs : mode.Logs;
        return logs.Select(x =>
        {
            var snapshot = x.Team.Count > 0 ? x.Team : CreateTeamSnapshot(user, stage, x.TeamNumber, teamRepo);
            return new NetSoloRaidMuseumLog
            {
                Damage = x.Damage,
                TeamNumber = x.TeamNumber,
                Team = { snapshot.Select(ToMuseumTeamCharacter) },
            };
        });
    }

    public static NetRewardData AcquireMissions(
        User user,
        IEnumerable<int> missionIds,
        bool noLimit,
        RepeatedField<NetSoloRaidMuseumMissionData> result,
        SoloRaidRepository repo)
    {
        // Claim requests can arrive concurrently when the client retries or the
        // button is tapped repeatedly. Keep validation, marking and reward
        // mutation atomic for this user so a mission can only pay out once.
        lock (user)
        {
            NetRewardData mergedReward = new();
            List<NetRewardData> rewards = [];
            var stageCache = new Dictionary<int, SoloRaidMuseumStageData>();
            var dirtyStages = new HashSet<int>();

            foreach (var missionId in missionIds.Distinct())
            {
                if (!GameData.Instance.MuseumMissionTable.TryGetValue(missionId, out var mission))
                {
                    Logging.WriteLine($"[SoloRaidMuseum] mission claim ignored unknown id={missionId}", LogType.Warning);
                    continue;
                }

                var expectedMode = noLimit ? MuseumStageModeType.NoLimit : MuseumStageModeType.Challenge;
                if (mission.ModeType != expectedMode) continue;

                if (!stageCache.TryGetValue(mission.StageId, out var stage))
                {
                    stage = GetStage(user.ID, mission.StageId, repo);
                    stageCache[mission.StageId] = stage;
                }

                var received = noLimit ? stage.ReceivedNoLimitMissions : stage.ReceivedChallengeMissions;
                var mode = noLimit ? stage.NoLimit : stage.Challenge;
                var progress = MissionProgress(mode, mission.ConditionType);
                var target = MissionTargetValue(mission);
                if (received.Contains(missionId))
                {
                    Logging.WriteLine($"[SoloRaidMuseum] mission claim skipped already-received id={missionId}, noLimit={noLimit}", LogType.Info);
                    continue;
                }
                if (progress < target)
                {
                    Logging.WriteLine($"[SoloRaidMuseum] mission claim skipped incomplete id={missionId}, progress={progress}, target={target}", LogType.Info);
                    continue;
                }

                received.Add(missionId);
                dirtyStages.Add(mission.StageId);
                result.Add(new NetSoloRaidMuseumMissionData { MissionId = missionId, Progress = progress, IsReceived = true });
                if (mission.RewardId > 0)
                    rewards.Add(RewardUtils.RegisterRewardsForUser(user, mission.RewardId));
                Logging.WriteLine($"[SoloRaidMuseum] mission claimed id={missionId}, noLimit={noLimit}, reward={mission.RewardId}", LogType.Info);
            }

            foreach (int dirtyStageId in dirtyStages)
            {
                repo.SaveMuseumStage(user.ID, stageCache[dirtyStageId]);
            }

            if (rewards.Count > 0)
            {
                mergedReward = NetUtils.MergeRewards(rewards, user);
                UserSessionStore.SaveUser(user);
            }

            return mergedReward;
        }
    }

    public static NetUserSoloRaidMuseumStageInfo ToInfo(
        SoloRaidMuseumStageData stage, SoloRaidMuseumModeData mode, bool noLimit)
    {
        var nextMission = GameData.Instance.MuseumMissionTable.Values
            .Where(x => x.StageId == stage.StageId &&
                        x.ModeType == (noLimit ? MuseumStageModeType.NoLimit : MuseumStageModeType.Challenge))
            .OrderBy(x => x.Order)
            .FirstOrDefault(x => MissionProgress(mode, x.ConditionType) < MissionTargetValue(x));
        return new NetUserSoloRaidMuseumStageInfo
        {
            StageId = stage.StageId,
            BestDamage = mode.BestDamage,
            BestStep = mode.BestStep,
            TargetMissionId = nextMission?.Id ?? 0,
            TargetMissionProgress = nextMission is null ? 0 : MissionProgress(mode, nextMission.ConditionType),
            TargetMissionConditionValue = nextMission is null ? 0 : MissionTargetValue(nextMission),
            IsInProgress = mode.IsInProgress,
        };
    }

    public static NetSoloRaidMuseumJoinData JoinData(SoloRaidMuseumModeData mode)
    {
        NetSoloRaidMuseumJoinData result = new();
        result.CsnList.AddRange(mode.CurrentLogs.SelectMany(x => x.Team).Select(x => x.Csn).Where(x => x > 0));
        return result;
    }

    private static IEnumerable<NetTeamData> GetTeams(
        User user, SoloRaidMuseumStageData stage, TeamRepository teamRepo)
    {
        if (stage.Teams.Any(x => x.Slots.Any(slot => slot.Value > 0)))
            return stage.Teams.Select(x => x.Clone()).ToList();

        // Compatibility with data written before museum teams became stage-scoped.
        var museumTeamType = (int)TeamType.SoloRaidMuseum;
        var museumTeams = teamRepo.GetUserTeam(user.ID, museumTeamType);
        if (museumTeams != null &&
            museumTeams.Teams.Any(x => x.Slots.Any(slot => slot.Value > 0)))
            return museumTeams.Teams.Select(x => x.Clone()).ToList();

        // Only account-owned characters are safe here. Other content presets may
        // contain temporary/support CSNs which the museum client cannot resolve.
        NetTeamData fallback = new() { TeamNumber = 1 };
        var characters = user.Characters.Take(5).ToList();
        for (var i = 0; i < characters.Count; i++)
            // NetTeamSlot.ValueType == 1 means a temporary support character.
            // Account-owned character serial numbers must use the default value 0.
            fallback.Slots.Add(new NetTeamSlot { Slot = i + 1, Value = characters[i].Csn, ValueType = 0 });
        return fallback.Slots.Count > 0 ? [fallback] : [];
    }

    private static IEnumerable<NetSoloRaidMuseumRankerRankingData> GetPastRankings(
        User currentUser, MuseumStageRecord_Raw stage)
    {
        // Rank missions do not use the raw leaderboard damage as their target.
        // The client converts ranker damage through the common-stage HP group;
        // provide the same deterministic CommonHP + Trial target values used by
        // the official client model so mission display and claim validation agree.
        var rankMissions = GameData.Instance.MuseumMissionTable.Values
            .Where(x => x.StageId == stage.Id &&
                        x.ModeType == MuseumStageModeType.Challenge &&
                        x.ConditionType == MuseumMissionConditionType.GetTotalDamageRanker)
            .OrderBy(x => x.Order)
            .ToList();
        var trialTargets = GameData.Instance.MuseumMissionTable.Values
            .Where(x => x.StageId == stage.Id &&
                        x.ModeType == MuseumStageModeType.Challenge &&
                        x.ConditionType == MuseumMissionConditionType.GetTotalDamage)
            .OrderBy(x => x.ConditionValue)
            .TakeLast(rankMissions.Count)
            .Select(x => x.ConditionValue)
            .ToList();
        var commonHp = GameData.Instance.MuseumCommonStageHpGroupTable.Values
            .Where(x => x.Group == stage.SoloRaidStageHpGroup)
            .Sum(x => x.HpValue);
        Logging.WriteLine($"[SoloRaidMuseum] ranking stage={stage.Id}, rankers={rankMissions.Count}, commonHp={commonHp}", LogType.Debug);
        var currentUserData = LobbyHandler.CreateWholeUserDataFromDbUser(currentUser);

        for (var i = 0; i < rankMissions.Count; i++)
        {
            yield return new NetSoloRaidMuseumRankerRankingData
            {
                Ranking = rankMissions[i].ConditionValue,
                Damage = commonHp + trialTargets.ElementAtOrDefault(i),
                PastUserData = CreatePastUserData(currentUserData),
                CurrentUserData = currentUserData,
            };
        }
    }

    private static NetPastWholeUserData CreatePastUserData(NetWholeUserData current)
    {
        return new NetPastWholeUserData
        {
            Usn = current.Usn,
            Nickname = current.Nickname,
            Icon = current.Icon,
            IconPrism = current.IconPrism,
            Frame = current.Frame,
            UserTitleId = current.UserTitleId,
        };
    }

    public static bool RefreshNoLimitUnlock(SoloRaidMuseumStageData stage)
    {
        stage.IsNoLimitUnlocked = HasClearedChallenge(stage.StageId, stage.Challenge.BestDamage);
        return stage.IsNoLimitUnlocked;
    }

    private static bool HasClearedChallenge(int stageId, long bestDamage)
    {
        var noLimitMode = GameData.Instance.MuseumStageModeTable.Values.FirstOrDefault(x =>
            x.StageId == stageId && x.ModeType == MuseumStageModeType.NoLimit);
        var finalMission = noLimitMode is null
            ? null
            : GameData.Instance.MuseumMissionTable.GetValueOrDefault(noLimitMode.ModeOpenCondition);
        return finalMission is not null && bestDamage >= MissionTargetValue(finalMission);
    }

    private static int GetNoLimitStep(int stageId, long damage)
    {
        var mode = GameData.Instance.MuseumStageModeTable.Values.FirstOrDefault(x =>
            x.StageId == stageId && x.ModeType == MuseumStageModeType.NoLimit);
        if (mode is null) return 1;

        // MonsterStageLvChangeTable.GetStep on the client returns step 1 for zero
        // damage, otherwise the row whose damage interval contains this team's
        // damage. The final row has max=0 and is an open-ended interval.
        return GameData.Instance.MonsterStageLvChangeTable.Values
            .Where(x => x.Group == mode.MonsterStageLvChangeGroup)
            .OrderBy(x => x.ConditionValueMin)
            .LastOrDefault(x => damage >= x.ConditionValueMin)?.Step ?? 1;
    }

    public static bool HasUnreceivedMission(SoloRaidMuseumStageData stage, bool noLimit)
    {
        var mode = noLimit ? stage.NoLimit : stage.Challenge;
        var received = noLimit ? stage.ReceivedNoLimitMissions : stage.ReceivedChallengeMissions;
        var modeType = noLimit ? MuseumStageModeType.NoLimit : MuseumStageModeType.Challenge;
        return GameData.Instance.MuseumMissionTable.Values.Any(x =>
            x.StageId == stage.StageId && x.ModeType == modeType && !received.Contains(x.Id) &&
            MissionProgress(mode, x.ConditionType) >= MissionTargetValue(x));
    }

    public static long MissionTargetValue(MuseumMissionRecord_Raw mission)
    {
        if (mission.ConditionType != MuseumMissionConditionType.GetTotalDamageRanker)
            return mission.ConditionValue;

        var rankMissions = GameData.Instance.MuseumMissionTable.Values
            .Where(x => x.StageId == mission.StageId &&
                        x.ModeType == MuseumStageModeType.Challenge &&
                        x.ConditionType == MuseumMissionConditionType.GetTotalDamageRanker)
            .OrderBy(x => x.Order)
            .ToList();
        var index = rankMissions.FindIndex(x => x.Id == mission.Id);
        if (index < 0) return long.MaxValue;
        return GameData.Instance.MuseumMissionTable.Values
            .Where(x => x.StageId == mission.StageId &&
                        x.ModeType == MuseumStageModeType.Challenge &&
                        x.ConditionType == MuseumMissionConditionType.GetTotalDamage)
            .OrderBy(x => x.ConditionValue)
            .TakeLast(rankMissions.Count)
            .Select(x => x.ConditionValue)
            .ElementAtOrDefault(index);
    }

    private static List<TeamCharacterData> CreateTeamSnapshot(
        User user, SoloRaidMuseumStageData stage, int teamNumber, TeamRepository teamRepo)
    {
        var team = GetTeams(user, stage, teamRepo).FirstOrDefault(x => x.TeamNumber == teamNumber);
        if (team is null) return [];
        return team.Slots.Where(x => x.Value > 0).Select(slot =>
        {
            var character = user.Characters.FirstOrDefault(x => x.Csn == slot.Value);
            return new TeamCharacterData
            {
                Slot = slot.Slot,
                Csn = slot.Value,
                Tid = character?.Tid ?? (int)slot.Value,
                Lv = character?.Level ?? 1,
                Combat = 0,
                CostumeId = character?.CostumeId ?? 0,
            };
        }).ToList();
    }

    private static NetSoloRaidMuseumTeamCharacter ToMuseumTeamCharacter(TeamCharacterData character) => new()
    {
        Slot = character.Slot,
        Tid = character.Tid,
        Lv = character.Lv,
        Combat = character.Combat,
        CostumeId = character.CostumeId,
    };

    private static SoloRaidMuseumLogData CloneLog(SoloRaidMuseumLogData log) => new()
    {
        Damage = log.Damage,
        Step = log.Step,
        TeamNumber = log.TeamNumber,
        Team = log.Team.Select(x => new TeamCharacterData
        {
            Slot = x.Slot,
            Csn = x.Csn,
            Tid = x.Tid,
            Lv = x.Lv,
            Combat = x.Combat,
            CostumeId = x.CostumeId,
        }).ToList(),
    };

    private static NetUserSoloRaidMuseumStageMode ToMode(SoloRaidMuseumStageData stage) => new()
    {
        StageId = stage.StageId,
        StageMode = stage.StageMode,
        IsNoLimitUnlocked = stage.IsNoLimitUnlocked,
    };

    public static long MissionProgress(SoloRaidMuseumModeData mode, MuseumMissionConditionType type) =>
        type == MuseumMissionConditionType.GetTotalStageStep ? mode.BestStep : mode.BestDamage;
}
