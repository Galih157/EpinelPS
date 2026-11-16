using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class BondLiberationController(
    IUserService userService,
    BondLiberationService bondLiberationService) : ControllerBase
{
    // --- Character Attractive & Counsel ---

    [HttpPost("/v1/character/attractive/get")]
    public ActionResult<ResGetAttractiveList> GetAttractiveList([FromBodyProtobuf] ReqGetAttractiveList req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.GetAttractiveList(user);
    }

    [HttpPost("/v1/character/attractive/obtainreward")]
    public ActionResult<ResObtainAttractiveReward> ObtainAttractiveReward(
        [FromBodyProtobuf] ReqObtainAttractiveReward req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ObtainAttractiveReward(user, req);
    }

    [HttpPost("/v1/character/attractive/setfavorites")]
    public ActionResult<ResSetAttractiveFavorites> SetAttractiveFavorites(
        [FromBodyProtobuf] ReqSetAttractiveFavorites req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.SetAttractiveFavorites(userId, req);
    }

    [HttpPost("/v1/character/attractive/check")]
    public ActionResult<ResCounseledBefore> CheckAttractive([FromBodyProtobuf] ReqCounseledBefore req)
        => new ResCounseledBefore();

    [HttpPost("/v1/character/counsel/check")]
    public ActionResult<ResCounseledBefore> CheckCounsel([FromBodyProtobuf] ReqCounseledBefore req)
        => new ResCounseledBefore { IsCounseledBefore = false };

    [HttpPost("/v1/character/attractive/counsel")]
    public ActionResult<ResCharacterCounsel> Counsel([FromBodyProtobuf] ReqCharacterCounsel req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.Counsel(user, req);
    }

    [HttpPost("/v1/character/counsel/answer/list")]
    public ActionResult<ResListAnsweredAttractiveCounsel> ListAnsweredCounsel(
        [FromBodyProtobuf] ReqListAnsweredAttractiveCounsel req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ListAnsweredCounsel(userId, req.NameCode);
    }

    [HttpPost("/v1/character/attractive/present")]
    public ActionResult<ResCharacterPresent> Present([FromBodyProtobuf] ReqCharacterPresent req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.Present(user, req);
    }

    [HttpPost("/v1/character/counsel/quick")]
    public ActionResult<ResCharacterQuickCounsel> QuickCounsel([FromBodyProtobuf] ReqCharacterQuickCounsel req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.QuickCounsel(user, req);
    }

    // --- FavoriteItems & Quests ---

    [HttpPost("/v1/favoriteitem/list")]
    public ActionResult<ResListFavoriteItem> ListFavoriteItems([FromBodyProtobuf] ReqListFavoriteItem req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ListFavoriteItems(userId);
    }

    [HttpPost("/v1/favoriteitem/library")]
    public ActionResult<ResGetFavoriteItemLibrary> GetFavoriteItemLibrary(
        [FromBodyProtobuf] ReqGetFavoriteItemLibrary req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.GetFavoriteItemLibrary(userId);
    }

    [HttpPost("/v1/favoriteitem/equip")]
    public ActionResult<ResEquipFavoriteItem> EquipFavoriteItem([FromBodyProtobuf] ReqEquipFavoriteItem req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.EquipFavoriteItem(user, req);
    }

    [HttpPost("/v1/favoriteitem/increaseexp")]
    public ActionResult<ResIncreaseExpFavoriteItem> IncreaseExpFavoriteItem(
        [FromBodyProtobuf] ReqIncreaseExpFavoriteItem req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.IncreaseExpFavoriteItem(user, req);
    }

    [HttpPost("/v1/favoriteitem/exchange")]
    public ActionResult<ResEquipFavoriteItem> UpgradeFavoriteItem([FromBodyProtobuf] ReqIncreaseExpFavoriteItem req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.UpgradeFavoriteItem(user, req);
    }

    [HttpPost("/v1/favoriteitem/quest/list")]
    public ActionResult<ResListFavoriteItemQuest> ListFavoriteItemQuests(
        [FromBodyProtobuf] ReqListFavoriteItemQuest req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ListFavoriteItemQuests(userId);
    }

    [HttpPost("/v1/favoriteitem/quest/start")]
    public ActionResult<ResStartFavoriteItemQuest> StartFavoriteItemQuest(
        [FromBodyProtobuf] ReqStartFavoriteItemQuest req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.StartFavoriteItemQuest(userId, req.FavoriteItemQuestId);
    }

    [HttpPost("/v1/favoriteitem/quest/stage/enter")]
    public ActionResult<ResEnterFavoriteItemQuestStage> EnterFavoriteItemQuestStage(
        [FromBodyProtobuf] ReqEnterFavoriteItemQuestStage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.EnterFavoriteItemQuestStage(user, req.StageId);
    }

    [HttpPost("/v1/favoriteitem/quest/stage/clear")]
    public ActionResult<ResClearFavoriteItemQuestStage> ClearFavoriteItemQuestStage(
        [FromBodyProtobuf] ReqClearFavoriteItemQuestStage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ClearFavoriteItemQuestStage(user, req);
    }

    [HttpPost("/v1/favoriteitem/quest/finish")]
    public ActionResult<ResFinishFavoriteItemQuest> FinishFavoriteItemQuest(
        [FromBodyProtobuf] ReqFinishFavoriteItemQuest req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.FinishFavoriteItemQuest(userId, req.FavoriteItemQuestId);
    }

    [HttpPost("/v1/favoriteitem/quest/obtain")]
    public ActionResult<ResObtainFavoriteItemQuestReward> ObtainFavoriteItemQuestReward(
        [FromBodyProtobuf] ReqObtainFavoriteItemQuestReward req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ObtainFavoriteItemQuestReward(user, req.QuestId);
    }

    // --- Liberation ---

    [HttpPost("/v1/liberate/get")]
    public ActionResult<ResGetLiberateData> GetLiberateData([FromBodyProtobuf] ReqGetLiberateData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.GetLiberateData(userId);
    }

    [HttpPost("/v1/liberate/getprogresslist")]
    public ActionResult<ResGetLiberateProgressList> GetProgressList(
        [FromBodyProtobuf] ReqGetLiberateProgressList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.GetProgressList(userId);
    }

    [HttpPost("/v1/liberate/choosecharacter")]
    public ActionResult<ResChooseLiberateCharacter> ChooseCharacter(
        [FromBodyProtobuf] ReqChooseLiberateCharacter req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ChooseCharacter(userId, req.CharacterId);
    }

    [HttpPost("/v1/liberate/completemission")]
    public ActionResult<ResCompleteLiberateMission> CompleteMission(
        [FromBodyProtobuf] ReqCompleteLiberateMission req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.CompleteMission(userId, req);
    }

    [HttpPost("/v1/liberate/obtainstepreward")]
    public ActionResult<ResObtainLiberateStepReward> ObtainStepReward(
        [FromBodyProtobuf] ReqObtainLiberateStepReward req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ObtainStepReward(user);
    }

    [HttpPost("/v1/liberate/resetmission")]
    public ActionResult<ResResetLiberateMission> ResetMission([FromBodyProtobuf] ReqResetLiberateMission req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return bondLiberationService.ResetMission(userId, req);
    }
}
