using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class ResetController(IUserService userService, ResetService service) : ControllerBase
{
    // --- Interception ---

    [HttpPost("/v1/intercept/Anomalous/Data")]
    public ActionResult<ResInterceptAnomalousData> GetAnomalousData([FromBodyProtobuf] ReqInterceptAnomalousData req)
        => new ResInterceptAnomalousData { InterceptAnomalousManagerId = 101, TodayRemainingTickets = 5 };

    [HttpPost("/v1/intercept/Anomalous/Enter")]
    public ActionResult<ResEnterInterceptAnomalous> EnterAnomalousIntercept(
        [FromBodyProtobuf] ReqEnterInterceptAnomalous req)
        => new ResEnterInterceptAnomalous();

    [HttpPost("/v1/intercept/Anomalous/FastClear")]
    public ActionResult<ResFastClearInterceptAnomalous> AnomalousFastClear(
        [FromBodyProtobuf] ReqFastClearInterceptAnomalous req)
        => new ResFastClearInterceptAnomalous();

    [HttpPost("/v1/intercept/Anomalous/Finish")]
    public ActionResult<ResFinishInterceptAnomalous> FinishAnomalousIntercept(
        [FromBodyProtobuf] ReqFinishInterceptAnomalous req)
        => new ResFinishInterceptAnomalous();

    [HttpPost("/v1/intercept/get")]
    public ActionResult<ResGetInterceptData> GetInterceptData([FromBodyProtobuf] ReqGetInterceptData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetInterceptData(userId);
    }

    [HttpPost("/v1/intercept/check")]
    public ActionResult<ResCheckClearInterceptToday> CheckClearInterceptToday(
        [FromBodyProtobuf] ReqCheckClearInterceptToday req)
        => new ResCheckClearInterceptToday { Clear = true };

    [HttpPost("/v1/intercept/clear")]
    public ActionResult<ResClearIntercept> ClearIntercept([FromBodyProtobuf] ReqClearIntercept req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearIntercept(user, req);
    }

    [HttpPost("/v1/intercept/enter")]
    public ActionResult<ResEnterIntercept> EnterIntercept([FromBodyProtobuf] ReqEnterIntercept req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterIntercept(user);
    }

    [HttpPost("/v1/intercept/fastclear")]
    public ActionResult<ResFastClearIntercept> FastClearIntercept([FromBodyProtobuf] ReqFastClearIntercept req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FastClearIntercept(userId);
    }

    // --- Missions (Daily & Weekly) ---

    [HttpPost("/v1/mission/obtain/daily")]
    public ActionResult<ResObtainDailyMissionReward> ObtainDaily(
        [FromBodyProtobuf] ReqObtainDailyMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainDailyMissionReward(user, req);
    }

    [HttpPost("/v1/mission/obtain/weekly")]
    public ActionResult<ResObtainWeeklyMissionReward> ObtainWeekly(
        [FromBodyProtobuf] ReqObtainWeeklyMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainWeeklyMissionReward(user, req);
    }

    [HttpPost("/v1/mission/getrewarded/daily")]
    public ActionResult<ResGetDailyRewardedData> GetDailyRewards([FromBodyProtobuf] ReqGetDailyRewardedData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDailyRewards(user);
    }

    [HttpPost("/v1/mission/getrewarded/weekly")]
    public ActionResult<ResGetWeeklyRewardedData> GetWeeklyRewards([FromBodyProtobuf] ReqGetWeeklyRewardedData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetWeeklyRewards(user);
    }

    // --- Tower ---

    [HttpPost("/v1/tower/cleartower")]
    public ActionResult<ResClearTower> ClearTower([FromBodyProtobuf] ReqClearTower req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearTower(user, req);
    }

    [HttpPost("/v1/tower/entertower")]
    public ActionResult<ResEnterTower> EnterTower([FromBodyProtobuf] ReqEnterTower req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterTower(user);
    }

    [HttpPost("/v1/tower/gettowerdata")]
    public ActionResult<ResGetTowerData> GetTowerData([FromBodyProtobuf] ReqGetTowerData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetTowerData(user);
    }

    // --- Simulation Room ---

    [HttpPost("/v1/simroom/clearbattle")]
    public ActionResult<ResClearSimRoomBattle> ClearBattle([FromBodyProtobuf] ReqClearSimRoomBattle req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearSimRoomBattle(user, req);
    }

    [HttpPost("/v1/simroom/fastclearbattle")]
    public ActionResult<ResFastClearSimRoomBattle> FastClearBattle([FromBodyProtobuf] ReqFastClearSimRoomBattle req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FastClearSimRoomBattle(user, req);
    }

    [HttpPost("/v1/simroom/enterbattle")]
    public ActionResult<ResEnterSimRoomBattle> EnterBattle([FromBodyProtobuf] ReqEnterSimRoomBattle req)
        => new ResEnterSimRoomBattle { Result = SimRoomResult.Success };

    [HttpPost("/v1/simroom/getacquirebufffunction")]
    public ActionResult<ResGetSimRoomAcquireBuffFunction> GetAcquireBuffFunction(
        [FromBodyProtobuf] ReqGetSimRoomAcquireBuffFunction req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetAcquireBuffFunction(user, req);
    }

    [HttpPost("/v1/simroom/get")]
    public ActionResult<ResGetSimRoom> GetSimRoom([FromBodyProtobuf] ReqGetSimRoom req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetSimRoom(user);
    }

    [HttpPost("/v1/simroom/popup-check/infinite")]
    public ActionResult<ResInfinitePopupCheck> InfinitePopupCheck([FromBodyProtobuf] ReqInfinitePopupCheck req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.InfinitePopupCheck(userId);
    }

    [HttpPost("/v1/simroom/proceedbufffunction")]
    public ActionResult<ResProceedSimRoomBuffFunction> ProceedBuffFunction(
        [FromBodyProtobuf] ReqProceedSimRoomBuffFunction req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ProceedBuffFunction(user, req);
    }

    [HttpPost("/v1/simroom/proceednikkefunction")]
    public ActionResult<ResProceedSimRoomNikkeFunction> ProceedNikkeFunction(
        [FromBodyProtobuf] ReqProceedSimRoomNikkeFunction req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ProceedNikkeFunction(user, req);
    }

    [HttpPost("/v1/simroom/proceedskipfunction")]
    public ActionResult<ResProceedSimRoomSkipFunction> ProceedSkipFunction(
        [FromBodyProtobuf] ReqProceedSimRoomSkipFunction req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ProceedSkipFunction(user, req);
    }

    [HttpPost("/v1/simroom/quit")]
    public ActionResult<ResQuitSimRoom> QuitSimRoom([FromBodyProtobuf] ReqQuitSimRoom req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.QuitSimRoom(userId, req);
    }

    [HttpPost("/v1/simroom/selectbuff")]
    public ActionResult<ResSelectSimRoomBuff> SelectBuff([FromBodyProtobuf] ReqSelectSimRoomBuff req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SelectSimRoomBuff(user, req);
    }

    [HttpPost("/v1/simroom/selectdifficulty")]
    public ActionResult<ResSelectSimRoomDifficulty> SelectDifficulty(
        [FromBodyProtobuf] ReqSelectSimRoomDifficulty req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SelectDifficulty(user, req);
    }

    [HttpPost("/v1/simroom/selectevent")]
    public ActionResult<ResSelectSimRoomEvent> SelectEvent([FromBodyProtobuf] ReqSelectSimRoomEvent req)
        => new ResSelectSimRoomEvent { Result = SimRoomResult.Success };

    [HttpPost("/v1/simroom/selectselectionevent")]
    public ActionResult<ResSelectSimRoomSelectionEvent> SelectSelectionEvent(
        [FromBodyProtobuf] ReqSelectSimRoomSelectionEvent req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SelectSelectionEvent(user, req);
    }

    [HttpPost("/v1/simroom/simplemode/selectbuff")]
    public ActionResult<ResSelectSimRoomSimpleModeBuff> SimpleModeSelectBuff(
        [FromBodyProtobuf] ReqSelectSimRoomSimpleModeBuff req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SimpleModeSelectBuff(userId, req);
    }

    [HttpPost("/v1/simroom/simplemode/setskipoption")]
    public ActionResult<ResSetSimRoomSimpleModeSkipOption> SimpleModeSetSkipOption(
        [FromBodyProtobuf] ReqSetSimRoomSimpleModeSkipOption req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SimpleModeSetSkipOption(userId, req);
    }

    [HttpPost("/v1/simroom/simplemode/skipall")]
    public ActionResult<ResSkipAllSimRoomSimpleMode> SimpleModeSkipAll(
        [FromBodyProtobuf] ReqSkipAllSimRoomSimpleMode req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SimpleModeSkipAll(user);
    }

    [HttpPost("/v1/simroom/simplemode/skipbuffselection")]
    public ActionResult<ResSkipSimRoomSimpleModeBuffSelection> SimpleModeSkipBuffSelection(
        [FromBodyProtobuf] ReqSkipSimRoomSimpleModeBuffSelection req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SimpleModeSkipBuffSelection(userId);
    }

    [HttpPost("/v1/simroom/simplemode/start")]
    public ActionResult<ResStartSimRoomSimpleMode> SimpleModeStart(
        [FromBodyProtobuf] ReqStartSimRoomSimpleMode req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SimpleModeStart(user);
    }
}
