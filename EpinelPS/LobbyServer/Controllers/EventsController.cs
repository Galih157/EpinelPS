using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class EventsController(IUserService userService, EventsService eventsService) : ControllerBase
{
    // --- Archive Story Dungeon & Scenarios ---

    [HttpPost("/v1/archive/storydungeon/clearstage")]
    public ActionResult<ResClearArchiveStage> ClearArchiveStage([FromBodyProtobuf] ReqClearArchiveStage req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.ClearArchiveStage(userId, req);
    }

    [HttpPost("/v1/archive/storydungeon/enterstage")]
    public ActionResult<ResEnterArchiveStage> EnterArchiveStage([FromBodyProtobuf] ReqEnterArchiveStage req)
        => new ResEnterArchiveStage();

    [HttpPost("/v1/bookmark/scenario/exist")]
    public ActionResult<ResExistScenarioBookmark> CheckBookmarkScenarioExists(
        [FromBodyProtobuf] ReqExistScenarioBookmark req)
        => new ResExistScenarioBookmark();

    [HttpPost("/v1/archive/scenario/complete")]
    public ActionResult<ResCompleteArchiveScenario> CompleteArchiveScenario(
        [FromBodyProtobuf] ReqCompleteArchiveScenario req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.CompleteArchiveScenario(userId, req);
    }

    [HttpPost("/v1/archive/storydungeon/fastclearstage")]
    public ActionResult<ResFastClearArchiveStage> FastClearArchiveStage(
        [FromBodyProtobuf] ReqFastClearArchiveStage req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.FastClearArchiveStage(userId, req);
    }

    [HttpPost("/v1/archive/storydungeon/get")]
    public ActionResult<ResGetArchiveStoryDungeon> GetArchiveStoryDungeon(
        [FromBodyProtobuf] ReqGetArchiveStoryDungeon req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetArchiveStoryDungeon(userId, req.EventId);
    }

    [HttpPost("/v1/archive/minigame/getdata")]
    public ActionResult<ResGetArchiveMiniGameData> GetArchiveMinigameData(
        [FromBodyProtobuf] ReqGetArchiveMiniGameData req)
        => new ResGetArchiveMiniGameData { Json = "" };

    [HttpPost("/v1/archive/scenario/getnonresettable")]
    public ActionResult<ResGetNonResettableArchiveScenario> GetNonResettableArchiveScenario(
        [FromBodyProtobuf] ReqGetNonResettableArchiveScenario req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetNonResettableArchiveScenario(userId, req.EventId);
    }

    [HttpPost("/v1/archive/scenario/getresettable")]
    public ActionResult<ResGetResettableArchiveScenario> GetResettableArchiveScenario(
        [FromBodyProtobuf] ReqGetResettableArchiveScenario req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetResettableArchiveScenario(userId, req.EventId);
    }

    [HttpPost("/v1/archive/messenger/get")]
    public ActionResult<ResGetArchiveMessenger> GetArchiveMessenger([FromBodyProtobuf] ReqGetArchiveMessenger req)
        => eventsService.GetArchiveMessenger(req.ArchiveMessengerGroupId);

    // --- Event Core & Scenarios ---

    [HttpPost("/v1/bookmark/event/scenario/exist")]
    public ActionResult<ResExistScenarioBookmark> CheckEventBookmarkScenarioExists(
        [FromBodyProtobuf] ReqExistScenarioBookmark req)
        => new ResExistScenarioBookmark();

    [HttpPost("/v1/event/scenario/exist")]
    public ActionResult<ResExistEventScenario> CheckEventScenarioExists([FromBodyProtobuf] ReqExistEventScenario req)
        => eventsService.CheckScenarioExists(req);

    [HttpPost("/v1/event/scenario/complete")]
    public ActionResult<ResSetEventScenarioComplete> CompleteEventScenario(
        [FromBodyProtobuf] ReqSetEventScenarioComplete req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.CompleteEventScenario(userId, req);
    }

    [HttpPost("/v1/event/scenario/get")]
    public ActionResult<ResGetEventScenarioData> GetEventScenario([FromBodyProtobuf] ReqGetEventScenarioData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetEventScenario(userId, req.EventId);
    }

    [HttpPost("/v1/event/list")]
    public ActionResult<ResGetEventList> ListEvents([FromBodyProtobuf] ReqGetEventList req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.ListEvents(user);
    }

    [HttpPost("/v1/event/getjoinedevent")]
    public ActionResult<ResGetJoinedEvent> GetJoinedEvents([FromBodyProtobuf] ReqGetJoinedEvent req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetJoinedEvents(user);
    }

    [HttpPost("/v1/event/challengestage/get")]
    public ActionResult<ResChallengeEventStageData> GetChallengeStage(
        [FromBodyProtobuf] ReqChallengeEventStageData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetChallengeStage(userId, req.EventId);
    }

    [HttpPost("/v1/event/boxgacha/get")]
    public ActionResult<ResGetEventBoxGacha> GetEventBoxGacha([FromBodyProtobuf] ReqGetEventBoxGacha req)
        => new ResGetEventBoxGacha();

    [HttpPost("/v1/event/boxgacha/execute")]
    public ActionResult<ResExecuteEventBoxGacha> ExecuteEventBoxGacha([FromBodyProtobuf] ReqExecuteEventBoxGacha req)
        => new ResExecuteEventBoxGacha();

    // --- Login Events ---

    [HttpPost("/v1/event/login/get")]
    public ActionResult<ResLoginEventData> GetLoginEvent([FromBodyProtobuf] ReqLoginEventData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetLoginEvent(userId, req.EventId);
    }

    [HttpPost("/v1/event/login/receive")]
    public ActionResult<ResObtainLoginEventReward> ReceiveLoginEvent(
        [FromBodyProtobuf] ReqObtainLoginEventReward req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.ReceiveLoginEvent(user, req);
    }

    [HttpPost("/v1/event/login/allreceive")]
    public ActionResult<ResObtainAllLoginEventReward> ReceiveAllLoginEvent(
        [FromBodyProtobuf] ReqObtainAllLoginEventReward req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.ReceiveAllLoginEvent(user, req.EventId);
    }

    // --- Story Event Dungeon ---

    [HttpPost("/v1/event/storydungeon/get")]
    public ActionResult<ResStoryDungeonEventData> GetStoryDungeon([FromBodyProtobuf] ReqStoryDungeonEventData req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetStoryDungeon(user, req.EventId);
    }

    [HttpPost("/v1/event/storydungeon/enterstage")]
    public ActionResult<ResEnterEventStage> EnterEventStoryStage([FromBodyProtobuf] ReqEnterEventStage req)
        => new ResEnterEventStage();

    [HttpPost("/v1/event/storydungeon/clearstage")]
    public ActionResult<ResClearEventStage> ClearEventStage([FromBodyProtobuf] ReqClearEventStage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.ClearEventStage(user, req);
    }

    [HttpPost("/v1/event/storydungeon/fastclearstage")]
    public ActionResult<ResFastClearEventStage> FastClearEventStage([FromBodyProtobuf] ReqFastClearEventStage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.FastClearEventStage(user, req);
    }

    // --- Event Missions ---

    [HttpPost("/v1/event/mission/getclear")]
    public ActionResult<ResGetEventMissionClear> GetMissionClear([FromBodyProtobuf] ReqGetEventMissionClear req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetMissionClear(user, req.EventId);
    }

    [HttpPost("/v1/event/mission/getclearlist")]
    public ActionResult<ResGetEventMissionClearList> GetMissionClearList(
        [FromBodyProtobuf] ReqGetEventMissionClearList req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetMissionClearList(user, req);
    }

    [HttpPost("/v1/event/mission/reward")]
    public ActionResult<ResObtainEventMissionReward> ObtainMissionReward(
        [FromBodyProtobuf] ReqObtainEventMissionReward req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.ObtainMissionReward(user, req);
    }

    // --- Event Shop ---

    [HttpPost("/v1/event/shopproductlist")]
    public ActionResult<ResEventShopProductList> GetShopProductList([FromBodyProtobuf] ReqEventShopProductList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.GetShopProductList(userId, req.EventId);
    }

    [HttpPost("/v1/event/shopbuyproduct")]
    public ActionResult<ResEventShopBuyProduct> BuyShopProduct([FromBodyProtobuf] ReqEventShopBuyProduct req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.BuyShopProduct(user, req);
    }

    [HttpPost("/v1/event/shopmultiplebuyproduct")]
    public ActionResult<ResEventShopMultipleBuyProduct> BuyShopMultipleProduct(
        [FromBodyProtobuf] ReqEventShopMultipleBuyProduct req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.BuyShopMultipleProduct(user, req);
    }

    // --- Event Field / Collect System / Itabag ---

    [HttpPost("/v1/eventfield/enter")]
    public ActionResult<ResEnterEventField> EnterEventField([FromBodyProtobuf] ReqEnterEventField req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.EnterEventField(user, req.MapId);
    }

    [HttpPost("/v1/event/field/password-door/list")]
    public ActionResult<ResListFieldPasswordDoorData> ListPasswordDoor(
        [FromBodyProtobuf] ReqListFieldPasswordDoorData req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.ListPasswordDoor(user, req.MapId);
    }

    [HttpPost("/v1/event/field/noticepopup/view")]
    public ActionResult<ResViewEventFieldNoticePopup> ViewNoticePopup(
        [FromBodyProtobuf] ReqViewEventFieldNoticePopup req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.ViewNoticePopup(user, req);
    }

    [HttpPost("/v1/event/field/password/acquire")]
    public ActionResult<ResAcquireFieldPassword> AcquirePassword([FromBodyProtobuf] ReqAcquireFieldPassword req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.AcquirePassword(user, req);
    }

    [HttpPost("/v1/event/field/password-door/unlock")]
    public ActionResult<ResUnlockFieldPasswordDoor> UnlockPasswordDoor(
        [FromBodyProtobuf] ReqUnlockFieldPasswordDoor req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.UnlockPasswordDoor(user, req);
    }

    [HttpPost("/v1/eventfield/sidestory/recommendation/obtainreward")]
    public ActionResult<ResObtainSideStoryRecommendationReward> RecommendationObtainReward(
        [FromBodyProtobuf] ReqObtainSideStoryRecommendationReward req)
        => new ResObtainSideStoryRecommendationReward();

    [HttpPost("/v1/event/field/saveobject")]
    public ActionResult<ResSaveEventFieldNonResettableFieldObject> SaveFieldObject(
        [FromBodyProtobuf] ReqSaveEventFieldNonResettableFieldObject req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return eventsService.SaveFieldObject(user, req);
    }

    [HttpPost("/v1/event/collect-system/list-field")]
    public ActionResult<ResListFieldEventCollectData> ListFieldCollect(
        [FromBodyProtobuf] ReqListFieldEventCollectData req)
        => new ResListFieldEventCollectData();

    [HttpPost("/v1/Event/Itabag/RedDotData")]
    public ActionResult<ReqItabagRedDotData> GetItabagRedDot([FromBodyProtobuf] ReqItabagRedDotData req)
        => new ReqItabagRedDotData();
}
