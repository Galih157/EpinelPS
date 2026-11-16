using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class StageQuestController(
    IUserService userService,
    StageQuestService service) : ControllerBase
{
    // --- Stage ---

    [HttpPost("/v1/stage/fastclearstage")]
    public ActionResult<ResFastClearCampaignStage> FastClearStage([FromBodyProtobuf] ReqFastClearCampaignStage req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FastClearStage(user, req);
    }

    [HttpPost("/v1/stage/checkclear")]
    public ActionResult<ResCheckStageClear> CheckCleared([FromBodyProtobuf] ReqCheckStageClear req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CheckCleared(userId, req);
    }

    [HttpPost("/v1/stage/get")]
    public ActionResult<ResGetStageData> GetStage([FromBodyProtobuf] ReqGetStageData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetStage(userId, req);
    }

    [HttpPost("/v1/stageclearinfo/get")]
    public ActionResult<ResGetStageClearInfo> GetStageClearInfo([FromBodyProtobuf] ReqGetStageClearInfo req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetStageClearInfo(userId);
    }

    [HttpPost("/v1/stage/clearstage")]
    public ActionResult<ResClearStage> ClearCampaignStage([FromBodyProtobuf] ReqClearStage req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearCampaignStage(user, req);
    }

    [HttpPost("/v1/stage/enterstage")]
    public ActionResult<ResEnterStage> EnterStage([FromBodyProtobuf] ReqEnterStage req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterStage(user, req);
    }

    // --- Trigger & Main Quest ---

    [HttpPost("/v1/Trigger/FinMainQuest")]
    public ActionResult<ResFinMainQuest> FinishMainQuest([FromBodyProtobuf] ReqFinMainQuest req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishMainQuest(user, req);
    }

    [HttpPost("/v1/Trigger/GetMainQuestData")]
    public ActionResult<ResGetMainQuestData> GetMainQuestData([FromBodyProtobuf] ReqGetMainQuestData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetMainQuestData(userId);
    }

    [HttpPost("/v1/trigger/obtainmainquestreward")]
    public ActionResult<ResObtainMainQuestReward> ObtainMainQuestReward(
        [FromBodyProtobuf] ReqObtainMainQuestReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainMainQuestReward(user, req);
    }

    [HttpPost("/v1/trigger/sync")]
    public ActionResult<ResSyncTrigger> SyncTriggers([FromBodyProtobuf] ReqSyncTrigger req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SyncTriggers(userId, req);
    }

    // --- Subquest ---

    [HttpPost("/v1/subquest/enrollment")]
    public ActionResult<ResEnrollmentSubQuest> EnrollSubquest([FromBodyProtobuf] ReqEnrollmentSubQuest req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnrollSubquest(user, req);
    }

    [HttpPost("/v1/subquest/list")]
    public ActionResult<ResGetSubQuestList> ListSubquests([FromBodyProtobuf] ReqGetSubQuestList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ListSubquests(userId);
    }

    [HttpPost("/v1/subquest/settrigger")]
    public ActionResult<ResSetTriggerFromSubQuest> SetSubquestTrigger(
        [FromBodyProtobuf] ReqSetTriggerFromSubQuest req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetSubquestTrigger(user, req);
    }

    // --- Storyline ---

    [HttpPost("/v1/storyline/bookmark/get")]
    public ActionResult<ResGetStorylineBookmarks> GetStorylineBookmarks(
        [FromBodyProtobuf] ReqGetStorylineBookmarks req)
        => new ResGetStorylineBookmarks();

    [HttpPost("/v1/storyline/get")]
    public ActionResult<ResGetStorylineData> GetStoryline([FromBodyProtobuf] ReqGetStorylineData req)
        => new ResGetStorylineData();

    [HttpPost("/v1/storyline/save")]
    public ActionResult<ResGetStorylineData> SaveStoryline([FromBodyProtobuf] ReqSaveRecentStoryline req)
        => new ResGetStorylineData();

    // --- Surface ---

    [HttpPost("/v1/Surface/Tutorial/Mission/Enter")]
    public ActionResult<ResEnterSurfaceMissionTutorial> EnterSurfaceTutorialMission(
        [FromBodyProtobuf] ReqEnterSurfaceMissionTutorial req)
        => new ResEnterSurfaceMissionTutorial();

    [HttpPost("/v1/Surface/Export/MaxAmount/All")]
    public ActionResult<ResListAllSurfaceCurrencyMaxAmount> GetSurfaceMaximumAmountAll(
        [FromBodyProtobuf] ReqListAllSurfaceCurrencyMaxAmount req)
        => new ResListAllSurfaceCurrencyMaxAmount();

    [HttpPost("/v1/surface/lobby/simpledata")]
    public ActionResult<ResLobbySurfaceSimpleData> GetSurfaceSimpleData(
        [FromBodyProtobuf] ReqLobbySurfaceSimpleData req)
        => new ResLobbySurfaceSimpleData();

    [HttpPost("/v1/Surface/HasFinishedTutorial")]
    public ActionResult<ResHasFinishedSurfaceTutorial> HasFinishedSurfaceTutorial(
        [FromBodyProtobuf] ReqHasFinishedSurfaceTutorial req)
        => new ResHasFinishedSurfaceTutorial();

    // --- Episode ---

    [HttpPost("/v1/episode/mission/enter")]
    public ActionResult<ResListValidEpMission> ListEpisodeMission([FromBodyProtobuf] ReqListValidEpMission req)
        => new ResListValidEpMission();
}
