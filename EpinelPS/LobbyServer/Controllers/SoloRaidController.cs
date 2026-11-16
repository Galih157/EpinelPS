using EpinelPS.Data;
using EpinelPS.Interfaces;
using EpinelPS.LobbyServer.Soloraid;
using EpinelPS.LobbyServer.SoloraidMuseum;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class SoloRaidController(
    IUserService userService,
    SoloRaidRepository soloRaidRepo,
    TeamRepository teamRepo) : ControllerBase
{
    // --- SoloRaid ---

    [HttpPost("/v1/soloraid/get")]
    public ActionResult<ResGetSoloRaidInfo> GetInfo([FromBodyProtobuf] ReqGetSoloRaidInfo req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetSoloRaidInfo
        {
            Info = new NetUserSoloRaidInfo(),
            PeriodResult = SoloRaidPeriodResult.Success,
            BanResult = SoloRaidBanResult.Success
        };

        try
        {
            response.Info = SoloRaidHelper.GetUserSoloRaidInfo(user, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"GetSoloRaidInfo error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/getperiod")]
    public ActionResult<ResGetSoloRaidPeriod> GetPeriod([FromBodyProtobuf] ReqGetSoloRaidPeriod req)
    {
        return new ResGetSoloRaidPeriod
        {
            Period = SoloRaidHelper.GetSoloRaidPeriod()
        };
    }

    [HttpPost("/v1/soloraid/getranking")]
    public ActionResult<ResGetSoloRaidRanking> GetRanking([FromBodyProtobuf] ReqGetSoloRaidRanking req)
        => new ResGetSoloRaidRanking();

    [HttpPost("/v1/soloraid/getlevel")]
    public ActionResult<ResGetLevelSoloRaid> GetLevel([FromBodyProtobuf] ReqGetLevelSoloRaid req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetLevelSoloRaid();
        try
        {
            SoloRaidHelper.GetLevelInfo(user, ref response, req.RaidLevel, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"GetLevelSoloRaid Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/practice/getlevel")]
    public ActionResult<ResGetLevelPracticeSoloRaid> GetLevelPractice(
        [FromBodyProtobuf] ReqGetLevelPracticeSoloRaid req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetLevelPracticeSoloRaid();
        try
        {
            SoloRaidHelper.GetLevelPracticeInfo(user, ref response, req.RaidLevel, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"GetLevelPractice Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/trial/getlevel")]
    public ActionResult<ResGetLevelTrialSoloRaid> GetLevelTrial(
        [FromBodyProtobuf] ReqGetLevelTrialSoloRaid req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetLevelTrialSoloRaid();
        try
        {
            SoloRaidHelper.GetLevelTrialInfo(user, ref response, req.RaidLevel, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"GetLevelTrial Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/getlogs")]
    public ActionResult<ResGetSoloRaidLogs> GetLogs([FromBodyProtobuf] ReqGetSoloRaidLogs req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetSoloRaidLogs();
        try
        {
            SoloRaidHelper.GetSoloRaidLog(user, ref response, req.RaidId, req.RaidLevel, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"GetLogs Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/open")]
    public ActionResult<ResOpenSoloRaid> Open([FromBodyProtobuf] ReqOpenSoloRaid req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResOpenSoloRaid
        {
            PeriodResult = SoloRaidPeriodResult.Success
        };

        try
        {
            response.RaidOpenCount = SoloRaidHelper.OpenSoloRaid(user, req.RaidId, req.RaidLevel, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"OpenSoloRaid error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/practice/open")]
    public ActionResult<ResOpenSoloRaidPractice> OpenPractice([FromBodyProtobuf] ReqOpenSoloRaidPractice req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResOpenSoloRaidPractice
        {
            PeriodResult = SoloRaidPeriodResult.Success
        };

        try
        {
            SoloRaidHelper.OpenSoloRaid(user, 0, req.RaidLevel, soloRaidRepo, type: SoloRaidType.Practice);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"OpenSoloRaid error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/trial/open")]
    public ActionResult<ResOpenSoloRaidTrial> OpenTrial([FromBodyProtobuf] ReqOpenSoloRaidTrial req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResOpenSoloRaidTrial
        {
            PeriodResult = SoloRaidPeriodResult.Success,
            RaidOpenCount = 1
        };

        try
        {
            response.RaidOpenCount = SoloRaidHelper.OpenSoloRaid(user, 0, req.RaidLevel, soloRaidRepo, type: SoloRaidType.Trial);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"OpenSoloRaid error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/enter")]
    public ActionResult<ResEnterSoloRaid> Enter([FromBodyProtobuf] ReqEnterSoloRaid req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        Logging.WriteLine($"Entering solo raid {req.RaidId} at level {req.RaidLevel} for user {userId} team {req.Team} members");
        return new ResEnterSoloRaid { PeriodResult = SoloRaidPeriodResult.Success };
    }

    [HttpPost("/v1/soloraid/trial/enter")]
    public ActionResult<ResEnterSoloRaidTrial> EnterTrial([FromBodyProtobuf] ReqEnterSoloRaidTrial req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        Logging.WriteLine($"Entering solo raid {req.RaidId} at level {req.RaidLevel} for user {userId} team {req.Team} members");
        return new ResEnterSoloRaidTrial { PeriodResult = SoloRaidPeriodResult.Success };
    }

    [HttpPost("/v1/soloraid/close")]
    public ActionResult<ResCloseSoloRaid> Close([FromBodyProtobuf] ReqCloseSoloRaid req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResCloseSoloRaid { PeriodResult = SoloRaidPeriodResult.Success };
        try
        {
            SoloRaidHelper.CloseSoloRaid(user, req.RaidId, req.RaidLevel, soloRaidRepo, SoloRaidType.Normal);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"CloseSoloRaid Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/practice/close")]
    public ActionResult<ResCloseSoloRaidPractice> ClosePractice([FromBodyProtobuf] ReqCloseSoloRaidPractice req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResCloseSoloRaidPractice { PeriodResult = SoloRaidPeriodResult.Success };
        try
        {
            SoloRaidHelper.CloseSoloRaid(user, req.RaidId, req.RaidLevel, soloRaidRepo, SoloRaidType.Practice);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"CloseSoloRaidPractice Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/trial/close")]
    public ActionResult<ResCloseSoloRaidTrial> CloseTrial([FromBodyProtobuf] ReqCloseSoloRaidTrial req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResCloseSoloRaidTrial { PeriodResult = SoloRaidPeriodResult.Success };
        try
        {
            SoloRaidHelper.CloseSoloRaid(user, req.RaidId, req.RaidLevel, soloRaidRepo, SoloRaidType.Trial);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"CloseSoloRaidTrial Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/fastbattle")]
    public ActionResult<ResFastBattleSoloRaid> FastBattle([FromBodyProtobuf] ReqFastBattleSoloRaid req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResFastBattleSoloRaid();
        try
        {
            SoloRaidHelper.FastBattle(user, ref response, req.RaidId, req.RaidLevel, req.ClearCount, soloRaidRepo);
        }
        catch (Exception e)
        {
            Logging.WriteLine($"FastBattle Error {e.Message}");
        }

        return response;
    }

    [HttpPost("/v1/soloraid/setdamage")]
    public ActionResult<ResSetSoloRaidDamage> SetDamage([FromBodyProtobuf] ReqSetSoloRaidDamage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResSetSoloRaidDamage();
        try
        {
            SoloRaidHelper.SetDamage(user, ref response, req, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"SetDamage Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/practice/setdamage")]
    public ActionResult<ResSetSoloRaidPracticeDamage> SetDamagePractice(
        [FromBodyProtobuf] ReqSetSoloRaidPracticeDamage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResSetSoloRaidPracticeDamage();
        try
        {
            SoloRaidHelper.SetDamagePractice(user, ref response, req, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"SetDamagePractice Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    [HttpPost("/v1/soloraid/trial/setdamage")]
    public ActionResult<ResSetSoloRaidTrialDamage> SetDamageTrial(
        [FromBodyProtobuf] ReqSetSoloRaidTrialDamage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResSetSoloRaidTrialDamage();
        try
        {
            SoloRaidHelper.SetDamageTrial(user, ref response, req, soloRaidRepo);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"SetDamageTrial Error: {ex.Message}", LogType.Error);
        }

        return response;
    }

    // --- SoloRaidMuseum ---

    [HttpPost("/v1/soloraidmuseum/get/data")]
    public ActionResult<ResGetSoloRaidMuseumData> GetMuseumData([FromBodyProtobuf] ReqGetSoloRaidMuseumData req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return SoloRaidMuseumHelper.GetData(user, soloRaidRepo);
    }

    [HttpPost("/v1/soloraidmuseum/get/groupdata")]
    public ActionResult<ResGetSoloRaidMuseumGroupData> GetMuseumGroupData(
        [FromBodyProtobuf] ReqGetSoloRaidMuseumGroupData req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        Logging.WriteLine($"[SoloRaidMuseum] request groupdata group={req.GroupId}", LogType.Debug);
        return SoloRaidMuseumHelper.GetGroupData(user, req.GroupId, soloRaidRepo, teamRepo);
    }

    [HttpPost("/v1/soloraidmuseum/get/mission")]
    public ActionResult<ResGetSoloRaidMuseumMission> GetMuseumMission(
        [FromBodyProtobuf] ReqGetSoloRaidMuseumMission req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return SoloRaidMuseumHelper.GetMissions(userId, req.StageId, soloRaidRepo);
    }

    [HttpPost("/v1/soloraidmuseum/set/stagemode")]
    public ActionResult<ResSetSoloRaidMuseumStageMode> SetMuseumStageMode(
        [FromBodyProtobuf] ReqSetSoloRaidMuseumStageMode req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        SoloRaidMuseumHelper.SetMode(userId, req.StageId, req.StageMode, soloRaidRepo);
        return new ResSetSoloRaidMuseumStageMode();
    }

    [HttpPost("/v1/soloraidmuseum/get/rankersquad")]
    public ActionResult<ResGetSoloRaidMuseumRankerSquad> GetMuseumRankerSquad(
        [FromBodyProtobuf] ReqGetSoloRaidMuseumRankerSquad req)
        => new ResGetSoloRaidMuseumRankerSquad();

    [HttpPost("/v1/soloraidmuseum/get/mypastsquad")]
    public ActionResult<ResGetSoloRaidMuseumMyPastSquad> GetMuseumMyPastSquad(
        [FromBodyProtobuf] ReqGetSoloRaidMuseumMyPastSquad req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetSoloRaidMuseumMyPastSquad();
        response.SquadList.AddRange(SoloRaidMuseumHelper.GetLogs(user, req.StageId, false, soloRaidRepo, teamRepo));
        return response;
    }

    [HttpPost("/v1/soloraidmuseum/challenge/get/log")]
    public ActionResult<ResGetSoloRaidMuseumChallengeLog> GetMuseumChallengeLog(
        [FromBodyProtobuf] ReqGetSoloRaidMuseumChallengeLog req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetSoloRaidMuseumChallengeLog();
        response.LogList.AddRange(SoloRaidMuseumHelper.GetLogs(user, req.StageId, false, soloRaidRepo, teamRepo, true));
        return response;
    }

    [HttpPost("/v1/soloraidmuseum/challenge/get/bestlog")]
    public ActionResult<ResGetSoloRaidMuseumChallengeBestLog> GetMuseumChallengeBestLog(
        [FromBodyProtobuf] ReqGetSoloRaidMuseumChallengeBestLog req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetSoloRaidMuseumChallengeBestLog();
        response.LogList.AddRange(SoloRaidMuseumHelper.GetLogs(user, req.StageId, false, soloRaidRepo, teamRepo));
        return response;
    }

    [HttpPost("/v1/soloraidmuseum/nolimit/get/log")]
    public ActionResult<ResGetSoloRaidMuseumNoLimitLog> GetMuseumNoLimitLog(
        [FromBodyProtobuf] ReqGetSoloRaidMuseumNoLimitLog req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetSoloRaidMuseumNoLimitLog();
        response.LogList.AddRange(SoloRaidMuseumHelper.GetLogs(user, req.StageId, true, soloRaidRepo, teamRepo, true));
        return response;
    }

    [HttpPost("/v1/soloraidmuseum/nolimit/get/bestlog")]
    public ActionResult<ResGetSoloRaidMuseumNoLimitBestLog> GetMuseumNoLimitBestLog(
        [FromBodyProtobuf] ReqGetSoloRaidMuseumNoLimitBestLog req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResGetSoloRaidMuseumNoLimitBestLog();
        response.LogList.AddRange(SoloRaidMuseumHelper.GetLogs(user, req.StageId, true, soloRaidRepo, teamRepo));
        return response;
    }

    [HttpPost("/v1/soloraidmuseum/challenge/open")]
    public ActionResult<ResOpenSoloRaidMuseumChallenge> OpenMuseumChallenge(
        [FromBodyProtobuf] ReqOpenSoloRaidMuseumChallenge req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        Logging.WriteLine($"[SoloRaidMuseum] request challenge/open stage={req.StageId}, teams=[{string.Join(',', req.TeamList)}]", LogType.Info);
        var mode = SoloRaidMuseumHelper.Open(user, req.StageId, false, req.TeamList, soloRaidRepo, teamRepo);
        return new ResOpenSoloRaidMuseumChallenge
        {
            StageJoinCount = mode.StageJoinCount,
            TotalDamage = mode.TotalDamage,
            JoinData = SoloRaidMuseumHelper.JoinData(mode)
        };
    }

    [HttpPost("/v1/soloraidmuseum/nolimit/open")]
    public ActionResult<ResOpenSoloRaidMuseumNoLimit> OpenMuseumNoLimit(
        [FromBodyProtobuf] ReqOpenSoloRaidMuseumNoLimit req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        Logging.WriteLine($"[SoloRaidMuseum] request nolimit/open stage={req.StageId}, teams=[{string.Join(',', req.TeamList)}]", LogType.Info);
        var mode = SoloRaidMuseumHelper.Open(user, req.StageId, true, req.TeamList, soloRaidRepo, teamRepo);
        return new ResOpenSoloRaidMuseumNoLimit
        {
            StageJoinCount = mode.StageJoinCount,
            TotalDamage = mode.TotalDamage,
            TotalStep = mode.TotalStep,
            JoinData = SoloRaidMuseumHelper.JoinData(mode)
        };
    }

    [HttpPost("/v1/soloraidmuseum/challenge/enter")]
    public ActionResult<ResEnterSoloRaidMuseumChallenge> EnterMuseumChallenge(
        [FromBodyProtobuf] ReqEnterSoloRaidMuseumChallenge req)
        => new ResEnterSoloRaidMuseumChallenge();

    [HttpPost("/v1/soloraidmuseum/nolimit/enter")]
    public ActionResult<ResEnterSoloRaidMuseumNoLimit> EnterMuseumNoLimit(
        [FromBodyProtobuf] ReqEnterSoloRaidMuseumNoLimit req)
        => new ResEnterSoloRaidMuseumNoLimit();

    [HttpPost("/v1/soloraidmuseum/challenge/close")]
    public ActionResult<ResCloseSoloRaidMuseumChallenge> CloseMuseumChallenge(
        [FromBodyProtobuf] ReqCloseSoloRaidMuseumChallenge req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        Logging.WriteLine($"[SoloRaidMuseum] request challenge/close stage={req.StageId}", LogType.Debug);
        SoloRaidMuseumHelper.Close(userId, req.StageId, false, soloRaidRepo);
        return new ResCloseSoloRaidMuseumChallenge();
    }

    [HttpPost("/v1/soloraidmuseum/nolimit/close")]
    public ActionResult<ResCloseSoloRaidMuseumNoLimit> CloseMuseumNoLimit(
        [FromBodyProtobuf] ReqCloseSoloRaidMuseumNoLimit req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        Logging.WriteLine($"[SoloRaidMuseum] request nolimit/close stage={req.StageId}", LogType.Debug);
        SoloRaidMuseumHelper.Close(userId, req.StageId, true, soloRaidRepo);
        return new ResCloseSoloRaidMuseumNoLimit();
    }

    [HttpPost("/v1/soloraidmuseum/challenge/set/damage")]
    public ActionResult<ResSetSoloRaidMuseumChallengeDamage> SetMuseumChallengeDamage(
        [FromBodyProtobuf] ReqSetSoloRaidMuseumChallengeDamage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var (stage, mode) = SoloRaidMuseumHelper.SetDamage(
            user, req.StageId, false, req.BattleData, soloRaidRepo, teamRepo, req.BattleResult);
        return new ResSetSoloRaidMuseumChallengeDamage
        {
            StageJoinCount = mode.StageJoinCount,
            TotalDamage = mode.TotalDamage,
            JoinData = SoloRaidMuseumHelper.JoinData(mode),
            UpdatedStageInfo = SoloRaidMuseumHelper.ToInfo(stage, mode, false),
            IsNoLimitUnlocked = stage.IsNoLimitUnlocked,
            UnreceivedMission = SoloRaidMuseumHelper.HasUnreceivedMission(stage, false)
        };
    }

    [HttpPost("/v1/soloraidmuseum/nolimit/set/damage")]
    public ActionResult<ResSetSoloRaidMuseumNoLimitDamage> SetMuseumNoLimitDamage(
        [FromBodyProtobuf] ReqSetSoloRaidMuseumNoLimitDamage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var (stage, mode) = SoloRaidMuseumHelper.SetDamage(
            user, req.StageId, true, req.BattleData, soloRaidRepo, teamRepo, req.BattleResult);
        return new ResSetSoloRaidMuseumNoLimitDamage
        {
            StageJoinCount = mode.StageJoinCount,
            TotalDamage = mode.TotalDamage,
            TotalStep = mode.TotalStep,
            JoinData = SoloRaidMuseumHelper.JoinData(mode),
            UpdatedStageInfo = SoloRaidMuseumHelper.ToInfo(stage, mode, true),
            UnreceivedMission = SoloRaidMuseumHelper.HasUnreceivedMission(stage, true)
        };
    }

    [HttpPost("/v1/soloraidmuseum/challenge/acquire/mission")]
    public ActionResult<ResAcquireSoloRaidMuseumChallengeMission> AcquireMuseumChallengeMission(
        [FromBodyProtobuf] ReqAcquireSoloRaidMuseumChallengeMission req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResAcquireSoloRaidMuseumChallengeMission();
        response.Reward = SoloRaidMuseumHelper.AcquireMissions(
            user, req.MissionIdList, false, response.MissionDataList, soloRaidRepo);
        return response;
    }

    [HttpPost("/v1/soloraidmuseum/nolimit/acquire/mission")]
    public ActionResult<ResAcquireSoloRaidMuseumNoLimitMission> AcquireMuseumNoLimitMission(
        [FromBodyProtobuf] ReqAcquireSoloRaidMuseumNoLimitMission req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        var response = new ResAcquireSoloRaidMuseumNoLimitMission();
        response.Reward = SoloRaidMuseumHelper.AcquireMissions(
            user, req.MissionIdList, true, response.MissionDataList, soloRaidRepo);

        var firstMission = req.MissionIdList
            .Select(x => GameData.Instance.MuseumMissionTable.GetValueOrDefault(x))
            .FirstOrDefault(x => x is not null);
        if (firstMission is not null)
        {
            var points = SoloRaidMuseumHelper.GetRankingPoints(user.ID, firstMission.StageId, soloRaidRepo);
            response.TotalRankingPoint = points.total;
            response.GroupRankingPoint = points.group;
        }

        return response;
    }
}
