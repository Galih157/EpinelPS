using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class ExplorationController(IUserService userService, ExplorationService explorationService) : ControllerBase
{
    // --- Archive ---

    [HttpPost("/v1/archive/get")]
    public async Task<ActionResult<ResGetArchiveRecord>> GetArchives(
        [FromBodyProtobuf] ReqGetArchiveRecord req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.GetArchivesAsync(userId, ct);
    }

    [HttpPost("/v1/archive/unlock")]
    public async Task<ActionResult<ResUnlockArchiveRecord>> UnlockArchiveRecord(
        [FromBodyProtobuf] ReqUnlockArchiveRecord req, CancellationToken ct)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.UnlockArchiveRecordAsync(user, req, ct);
    }

    [HttpPost("/v1/archive/event-quest/unlock")]
    public async Task<ActionResult<ResUnlockArchiveEventQuest>> UnlockArchiveEventQuest(
        [FromBodyProtobuf] ReqUnlockArchiveEventQuest req, CancellationToken ct)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.UnlockArchiveEventQuestAsync(user, req, ct);
    }

    // --- Mission Achievements ---

    [HttpPost("/v1/mission/getrewarded/achievement")]
    public async Task<ActionResult<ResGetAchievementRewardedData>> GetAchievementRewardedData(
        [FromBodyProtobuf] ReqGetAchievementRewardedData req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.GetAchievementRewardedDataAsync(userId, ct);
    }

    [HttpPost("/v1/mission/getrewarded/all")]
    public async Task<ActionResult<ResGetRewardedData>> GetAllMissionRewards(
        [FromBodyProtobuf] ReqGetRewardedData req, CancellationToken ct)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.GetAllMissionRewardsAsync(user, ct);
    }

    [HttpPost("/v1/mission/obtain/achievement")]
    public async Task<ActionResult<ResObtainAchievementReward>> ObtainAchievement(
        [FromBodyProtobuf] ReqObtainAchievementReward req, CancellationToken ct)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.ObtainAchievementAsync(user, req, ct);
    }

    // --- SideStory ---

    [HttpPost("/v1/sidestory/list")]
    public async Task<ActionResult<ResListSideStory>> ListSideStory(
        [FromBodyProtobuf] ReqListSideStory req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.ListSideStoryAsync(userId, ct);
    }

    [HttpPost("/v1/sidestory/stage/clear")]
    public async Task<ActionResult<ResClearSideStoryStage>> ClearSideStoryStage(
        [FromBodyProtobuf] ReqClearSideStoryStage req, CancellationToken ct)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.ClearSideStoryStageAsync(user, req.SideStoryStageId, ct);
    }

    [HttpPost("/v1/sidestory/view/set")]
    public async Task<ActionResult<ResSetViewSideStory>> SetSideStoryViewed(
        [FromBodyProtobuf] ReqSetViewSideStory req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.SetSideStoryViewedAsync(userId, req.ViewedSideStoryIds, ct);
    }

    [HttpPost("/v1/sidestory/stage/enter")]
    public ActionResult<ResEnterSideStoryStage> EnterSideStoryStage(
        [FromBodyProtobuf] ReqEnterSideStoryStage req)
        => new ResEnterSideStoryStage();

    [HttpPost("/v1/sidestory/cut/enterbattle")]
    public ActionResult<ResEnterSideStoryCutForBattle> EnterSideStoryBattle(
        [FromBodyProtobuf] ReqEnterSideStoryCutForBattle req)
        => new ResEnterSideStoryCutForBattle();

    [HttpPost("/v1/sidestory/cut/clearbattle")]
    public ActionResult<ResClearSideStoryCutForBattle> ClearSideStoryBattle(
        [FromBodyProtobuf] ReqClearSideStoryCutForBattle req)
        => new ResClearSideStoryCutForBattle();

    [HttpPost("/v1/sidestory/cut/clearscenario")]
    public ActionResult<ResClearSideStoryCutForScenario> ClearSideStoryScenario(
        [FromBodyProtobuf] ReqClearSideStoryCutForScenario req)
        => new ResClearSideStoryCutForScenario();

    [HttpPost("/v1/eventfield/sidestory/recommendation/reddot")]
    public ActionResult<ResSideStoryRecommendationReddotData> GetSideStoryRecommendationReddot(
        [FromBodyProtobuf] ReqSideStoryRecommendationReddotData req)
        => new ResSideStoryRecommendationReddotData();

    // --- LostSector ---

    [HttpPost("/v1/lostsector/get")]
    public async Task<ActionResult<ResGetLostSectorData>> GetLostSectorData(
        [FromBodyProtobuf] ReqGetLostSectorData req, CancellationToken ct)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.GetLostSectorDataAsync(user, ct);
    }

    [HttpPost("/v1/lostsector/getfield")]
    public async Task<ActionResult<ResGetLostSectorFieldData>> GetField(
        [FromBodyProtobuf] ReqGetLostSectorFieldData req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.GetFieldAsync(userId, req.SectorId, ct);
    }

    [HttpPost("/v1/lostsector/open")]
    public async Task<ActionResult<ResOpenLostSector>> OpenLostSector(
        [FromBodyProtobuf] ReqOpenLostSector req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.OpenAsync(userId, req.SectorId, ct);
    }

    [HttpPost("/v1/lostsector/play")]
    public async Task<ActionResult<ResPlayLostSector>> PlayLostSector(
        [FromBodyProtobuf] ReqPlayLostSector req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.PlayAsync(userId, req.SectorId, ct);
    }

    [HttpPost("/v1/lostsector/enterstage")]
    public ActionResult<ResEnterLostSectorStage> EnterLostSectorStage(
        [FromBodyProtobuf] ReqEnterLostSectorStage req)
        => new ResEnterLostSectorStage();

    [HttpPost("/v1/lostsector/savefield")]
    public async Task<ActionResult<ResSaveLostSectorField>> SaveLostSectorField(
        [FromBodyProtobuf] ReqSaveLostSectorField req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.SaveFieldAsync(userId, req, ct);
    }

    [HttpPost("/v1/lostsector/savefieldobject")]
    public async Task<ActionResult<ResSaveLostSectorFieldObject>> SaveLostSectorFieldObject(
        [FromBodyProtobuf] ReqSaveLostSectorFieldObject req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.SaveFieldObjectAsync(userId, req, ct);
    }

    [HttpPost("/v1/lostsector/clearstage")]
    public async Task<ActionResult<ResClearLostSectorStage>> ClearLostSectorStage(
        [FromBodyProtobuf] ReqClearLostSectorStage req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.ClearStageAsync(userId, req, ct);
    }

    [HttpPost("/v1/lostsector/fastclearstage")]
    public async Task<ActionResult<ResFastClearLostSectorStage>> FastClearLostSectorStage(
        [FromBodyProtobuf] ReqFastClearLostSectorStage req, CancellationToken ct)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.FastClearStageAsync(userId, req.StageId, ct);
    }

    [HttpPost("/v1/lostsector/obtainitem")]
    public async Task<ActionResult<ResObtainLostSectorItem>> ObtainLostSectorItem(
        [FromBodyProtobuf] ReqObtainLostSectorItem req, CancellationToken ct)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.ObtainItemAsync(user, req, ct);
    }

    [HttpPost("/v1/lostsector/perfectreward")]
    public async Task<ActionResult<ResLostSectorPerfectReward>> GetLostSectorPerfectReward(
        [FromBodyProtobuf] ReqLostSectorPerfectReward req, CancellationToken ct)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return await explorationService.GetPerfectRewardAsync(user, req.SectorId, ct);
    }
}
