using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class OutpostController(IUserService userService, OutpostService service) : ControllerBase
{
    // --- Outpost Overview & Battle Rewards ---

    [HttpPost("/v1/outpost/getoutpostdata")]
    public ActionResult<ResGetOutpostData> GetOutpostData([FromBodyProtobuf] ReqGetOutpostData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetOutpostData(user);
    }

    [HttpPost("/v1/outpost/showoutpostbattlereward")]
    public ActionResult<ResShowOutpostBattleReward> ShowBattleReward(
        [FromBodyProtobuf] ReqShowOutpostBattleReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ShowBattleReward(user);
    }

    [HttpPost("/v1/outpost/obtainoutpostbattlereward")]
    public ActionResult<ResObtainOutpostBattleReward> ObtainOutpostReward(
        [FromBodyProtobuf] ReqObtainOutpostBattleReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainOutpostReward(user);
    }

    [HttpPost("/v1/outpost/obtainfastbattlereward")]
    public ActionResult<ResObtainFastBattleReward> DoWipeout([FromBodyProtobuf] ReqObtainFastBattleReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.DoWipeout(user);
    }

    // --- Outpost Buildings ---

    [HttpPost("/v1/outpost/building")]
    public ActionResult<ResBuilding> BuildBuilding([FromBodyProtobuf] ReqBuilding req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.BuildBuilding(user, req);
    }

    [HttpPost("/v1/Outpost/MoveBuilding")]
    public ActionResult<ResMoveBuilding> MoveBuilding([FromBodyProtobuf] ReqMoveBuilding req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.MoveBuilding(userId, req);
    }

    [HttpPost("/v1/outpost/buildingisdone")]
    public ActionResult<ResBuildingIsDone> SetBuildingDone([FromBodyProtobuf] ReqBuildingIsDone req)
        => new ResBuildingIsDone();

    // --- InfraCore ---

    [HttpPost("/v1/infracore/check")]
    public ActionResult<ResCheckReceiveInfraCoreReward> CheckInfracore(
        [FromBodyProtobuf] ReqCheckReceiveInfraCoreReward req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CheckInfracore(userId);
    }

    [HttpPost("/v1/infracore/reward")]
    public ActionResult<ResObtainInfraCoreReward> ObtainInfracoreReward(
        [FromBodyProtobuf] ReqObtainInfraCoreReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainInfracoreReward(user);
    }

    // --- Tactic Academy & Sudden Events ---

    [HttpPost("/v1/outpost/tactic/get")]
    public ActionResult<ResGetTacticAcademyData> GetTacticAcademyData(
        [FromBodyProtobuf] ReqGetTacticAcademyData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetTacticAcademyData(userId);
    }

    [HttpPost("/v1/outpost/tactic/clearlesson")]
    public ActionResult<ResTacticAcademyClearLesson> ClearTacticAcademyLesson(
        [FromBodyProtobuf] ReqTacticAcademyClearLesson req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearTacticAcademyLesson(user, req);
    }

    [HttpPost("/v1/outpost/suddenevent/start")]
    public ActionResult<ResStartSuddenEvent> StartSuddenEvent([FromBodyProtobuf] ReqStartSuddenEvent req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.StartSuddenEvent(user, req);
    }

    // --- Recycle Room ---

    [HttpPost("/v1/outpost/recycleroom/get")]
    public ActionResult<ResGetRecycleRoomData> GetRecycleRoomData([FromBodyProtobuf] ReqGetRecycleRoomData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetRecycleRoomData(userId);
    }

    [HttpPost("/v1/outpost/RecycleRoom/RunResearch")]
    public ActionResult<ResRecycleRunResearch> RunResearch([FromBodyProtobuf] ReqRecycleRunResearch req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RunResearch(userId, req);
    }

    [HttpPost("/v1/outpost/RecycleRoom/LevelUpResearch")]
    public ActionResult<ResRecycleLevelUpResearch> LevelUpResearch(
        [FromBodyProtobuf] ReqRecycleLevelUpResearch req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.LevelUpResearch(user, req);
    }

    [HttpPost("/v1/outpost/RecycleRoom/PersonalResearchLevelUp")]
    public ActionResult<ResPersonalResearchRecycleLevelUp> PersonalResearchLevelUp(
        [FromBodyProtobuf] ReqPersonalResearchRecycleLevelUp req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.PersonalResearchLevelUp(user, req);
    }

    // --- Dispatch ---

    [HttpPost("/v1/outpost/dispatch/get")]
    public ActionResult<ResGetDispatchList> GetDispatchList([FromBodyProtobuf] ReqGetDispatchList req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDispatchList(user);
    }

    [HttpPost("/v1/Outpost/ResetDispatchList")]
    public ActionResult<ResResetDispatchList> ResetDispatchList([FromBodyProtobuf] ReqResetDispatchList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResetDispatchList(userId);
    }

    [HttpPost("/v1/outpost/rundispatch")]
    public ActionResult<ResRunDispatch> RunDispatch([FromBodyProtobuf] ReqRunDispatch req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RunDispatch(userId, req);
    }

    [HttpPost("/v1/outpost/rundispatchlist")]
    public ActionResult<ResRunDispatchList> RunDispatchList([FromBodyProtobuf] ReqRunDispatchList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RunDispatchList(userId, req);
    }

    [HttpPost("/v1/outpost/canceldispatch")]
    public ActionResult<ResCancelDispatch> CancelDispatch([FromBodyProtobuf] ReqCancelDispatch req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CancelDispatch(userId, req);
    }

    [HttpPost("/v1/outpost/obtaindispatchreward")]
    public ActionResult<ResObtainDispatchReward> ObtainDispatchReward(
        [FromBodyProtobuf] ReqObtainDispatchReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainDispatchReward(user, req);
    }

    [HttpPost("/v1/outpost/dispatch/deleteselect")]
    public ActionResult<ResDeleteDispatchSelection> DeleteDispatch(
        [FromBodyProtobuf] ReqDeleteDispatchSelection req)
        => service.DeleteDispatch(req);

    // --- Synchro Device ---

    [HttpPost("/v1/character/synchrodevice/get")]
    public ActionResult<ResGetSynchroData> GetSynchroDevice([FromBodyProtobuf] ReqGetSynchroData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetSynchroDevice(user);
    }

    [HttpPost("/v1/character/SynchroDevice/Regist")]
    public ActionResult<ResSynchroRegister> RegisterSynchroDevice([FromBodyProtobuf] ReqSynchroRegister req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RegisterSynchroDevice(user, req);
    }

    [HttpPost("/v1/character/SynchroDevice/Unregist")]
    public ActionResult<ResSynchroUnregist> UnregisterSynchroDevice([FromBodyProtobuf] ReqSynchroUnregist req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UnregisterSynchroDevice(user, req);
    }

    [HttpPost("/v1/character/synchrodevice/addslot")]
    public ActionResult<ResSynchroAddSlot> SynchroAddSlot([FromBodyProtobuf] ReqSynchroAddSlot req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SynchroAddSlot(userId, req);
    }

    [HttpPost("/v1/character/synchrodevice/addslotbyitem")]
    public ActionResult<ResSynchroAddSlot> SynchroAddSlotByItem([FromBodyProtobuf] ReqSynchroAddSlot req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SynchroAddSlotByItem(userId);
    }

    [HttpPost("/v1/character/synchrodevice/registoneclick")]
    public ActionResult<ResSynchroRegisterOneClick> SynchroRegisterOneClick(
        [FromBodyProtobuf] ReqSynchroRegisterOneClick req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SynchroRegisterOneClick(user, req);
    }

    [HttpPost("/v1/character/SynchroDevice/Change")]
    public ActionResult<ResSynchroChange> ChangeSynchroDevice([FromBodyProtobuf] ReqSynchroChange req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ChangeSynchroDevice(user);
    }

    [HttpPost("/v1/character/SynchroDevice/LevelUp")]
    public ActionResult<ResSynchroLevelUp> SynchroLevelUp([FromBodyProtobuf] ReqSynchroLevelUp req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SynchroLevelUp(user);
    }

    [HttpPost("/v1/character/synchrodevice/deviceoneclick")]
    public ActionResult<ResSynchroDeviceOneClick> SynchroDeviceOneClick(
        [FromBodyProtobuf] ReqSynchroDeviceOneClick req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SynchroDeviceOneClick(user, req);
    }

    [HttpPost("/v1/character/synchrodevice/oneclick")]
    public ActionResult<ResSynchroOneClick> SynchroOneClick([FromBodyProtobuf] ReqSynchroOneClick req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SynchroOneClick(user, req);
    }
}
