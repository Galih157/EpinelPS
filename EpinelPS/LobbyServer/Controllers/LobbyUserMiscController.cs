using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class LobbyUserMiscController(
    IUserService userService,
    LobbyUserMiscService service) : ControllerBase
{
    // --- LobbyUser ---

    [HttpPost("/v1/lobby/usertitle/acquire")]
    public ActionResult<ResAcquireUserTitle> AcquireUserTitle([FromBodyProtobuf] ReqAcquireUserTitle req)
    {
        if (userService.GetUserId() is null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return new ResAcquireUserTitle();
    }

    [HttpPost("/v1/featureflags/all/get")]
    public ActionResult<ResGetAllFeatureFlags> GetAllFeatureFlags([FromBodyProtobuf] ReqGetAllFeatureFlags req)
        => service.GetAllFeatureFlags();

    [HttpPost("/v1/user/getcontentsdata")]
    public ActionResult<ResGetContentsOpenData> GetContentsData([FromBodyProtobuf] ReqGetContentsOpenData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetContentsData(userId);
    }

    [HttpPost("/v1/user/getfieldtalklist")]
    public ActionResult<ResGetFieldTalkList> GetFieldTalkList([FromBodyProtobuf] ReqGetFieldTalkList req)
    {
        if (userService.GetUserId() is null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return new ResGetFieldTalkList();
    }

    [HttpPost("/v1/User/GetProfileFrame")]
    public ActionResult<ResGetProfileFrame> GetProfileFrame([FromBodyProtobuf] ReqGetProfileFrame req)
        => service.GetProfileFrame();

    [HttpPost("/v1/User/GetScenarioList")]
    public ActionResult<ResGetScenarioList> GetScenarioList([FromBodyProtobuf] ReqGetScenarioList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetScenarioList(userId);
    }

    [HttpPost("/v1/user/speciallobbyslot/get")]
    public ActionResult<ResGetSpecialLobbySlotData> GetSpecialLobbySlotData(
        [FromBodyProtobuf] ReqGetSpecialLobbySlotData req)
        => new ResGetSpecialLobbySlotData();

    [HttpPost("/v1/User/Get")]
    public ActionResult<ResGetUserData> GetUser([FromBodyProtobuf] ReqGetUserData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetUser(user, req);
    }

    [HttpPost("/v1/User/GetProfile")]
    public ActionResult<ResGetProfileData> GetUserProfile([FromBodyProtobuf] ReqGetProfileData req)
    {
        if (userService.GetUser() is null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetUserProfile(req);
    }

    [HttpPost("/v1/user/scenario/exist")]
    public ActionResult<ResExistScenario> GetUserScenarioExist([FromBodyProtobuf] ReqExistScenario req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetUserScenarioExist(userId, req);
    }

    [HttpPost("/v1/lobby/usertitlecounter/get")]
    public ActionResult<ResGetUserTitleCounterList> GetUserTitleCounter(
        [FromBodyProtobuf] ReqGetUserTitleCounterList req)
        => service.GetUserTitleCounter();

    [HttpPost("/v1/mail/read")]
    public ActionResult<ResReadMail> ReadMail([FromBodyProtobuf] ReqReadMail req)
        => new ResReadMail();

    [HttpPost("/v1/record/notice")]
    public ActionResult<ResRecordNoticeLog> MarkNoticeRead([FromBodyProtobuf] ReqRecordNoticeLog req)
    {
        if (userService.GetUserId() is null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return new ResRecordNoticeLog();
    }

    [HttpPost("/v1/user/setnickname")]
    public ActionResult<ResSetNickname> SetNickname([FromBodyProtobuf] ReqSetNickname req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetNickname(userId, req);
    }

    [HttpPost("/v1/user/setnicknamefree")]
    public ActionResult<ResSetNicknameFree> SetNicknameFree([FromBodyProtobuf] ReqSetNicknameFree req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetNicknameFree(userId, req);
    }

    [HttpPost("/v1/User/SetNickNameInTutorial")]
    public ActionResult<ResSetNicknameInTutorial> SetNicknameInTutorial(
        [FromBodyProtobuf] ReqSetNicknameInTutorial req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetNicknameInTutorial(userId, req);
    }

    [HttpPost("/v1/user/setprofiledata")]
    public ActionResult<ResSetProfileData> SetProfileData([FromBodyProtobuf] ReqSetProfileData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetProfileData(userId, req);
    }

    [HttpPost("/v1/user/setprofiledesc")]
    public ActionResult<ResSetProfileDesc> SetProfileDescription([FromBodyProtobuf] ReqSetProfileDesc req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetProfileDescription(userId, req);
    }

    [HttpPost("/v1/user/setprofileicon")]
    public ActionResult<ResSetProfileIcon> SetProfileIcon([FromBodyProtobuf] ReqSetProfileIcon req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetProfileIcon(userId, req);
    }

    [HttpPost("/v1/User/SetScenarioComplete")]
    public ActionResult<ResSetScenarioComplete> SetScenarioCompleted([FromBodyProtobuf] ReqSetScenarioComplete req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetScenarioCompleted(user, req);
    }

    [HttpPost("/v1/lobby/usertitle/set")]
    public ActionResult<ResSetUserTitle> SetUserTitle([FromBodyProtobuf] ReqSetUserTitle req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetUserTitle(userId, req);
    }

    [HttpPost("/v1/tutorial/set")]
    public ActionResult<ResSetTutorial> SetTutorial([FromBodyProtobuf] ReqSetTutorial req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetTutorial(userId, req);
    }

    [HttpPost("/v1/lobby/usertitle/unmark-badge")]
    public ActionResult<ResUnMarkUserTitleBadge> UnmarkUserTitleBadge(
        [FromBodyProtobuf] ReqUnMarkUserTitleBadge req)
    {
        if (userService.GetUserId() is null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return new ResUnMarkUserTitleBadge();
    }

    // --- Misc ---

    [HttpPost("/v1/featureflags/get")]
    public ActionResult<ResGetFeatureFlag> GetFeatureFlags([FromBodyProtobuf] ReqGetFeatureFlag req)
        => new ResGetFeatureFlag { IsOpen = true };

    [HttpPost("/v1/maintenancenotice")]
    public ActionResult<ResMaintenanceNotice> GetMaintenanceNotice([FromBodyProtobuf] ReqMaintenanceNotice req)
        => new ResMaintenanceNotice();

    [HttpPost("/v1/system/sentry/getparams")]
    public ActionResult<ResGetSentryParams> GetSentryParams([FromBodyProtobuf] ReqGetSentryParams req)
        => new ResGetSentryParams { SamplingRate = 1E-06, TraceSamplingRate = 1E-06 };

    [HttpPost("/v1/getserverinfo")]
    public ActionResult<ResGetServerInfo> GetServerInfo([FromBodyProtobuf] ReqGetServerInfo req)
        => new ResGetServerInfo { MatchUrl = "https://global-match.nikke-kr.com", WorldId = 84 };

    [HttpPost("/v1/useronlinestatelog")]
    public ActionResult<ResUserOnlineStateLog> GetUserOnlineStateLog([FromBodyProtobuf] ReqUserOnlineStateLog req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RecordUserOnlineStateLog(user);
    }

    [HttpPost("/v1/lobby/retroactive")]
    public ActionResult<ResRetroactive> LobbyRetroactive([FromBodyProtobuf] ReqRetroactive req)
    {
        if (userService.GetUserId() is null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return new ResRetroactive();
    }

    // --- Ranking ---

    [HttpPost("/v1/ranking/rankachievementtop")]
    public ActionResult<ResGetRankAchievementTop> GetRankAchievementTop(
        [FromBodyProtobuf] ReqGetRankAchievementTop req)
        => new ResGetRankAchievementTop();

    [HttpPost("/v1/ranking/alltoprank")]
    public ActionResult<ResGetAllTopRank> GetTopRanks([FromBodyProtobuf] ReqGetAllTopRank req)
        => new ResGetAllTopRank();

    [HttpPost("/v1/ranking/updateserverreward")]
    public ActionResult<ResUpdateRankingServerReward> UpdateServerReward(
        [FromBodyProtobuf] ReqUpdateRankingServerReward req)
        => new ResUpdateRankingServerReward();

    // --- Arena ---

    [HttpPost("/v1/arena/get")]
    public ActionResult<ResGetArena> GetArena([FromBodyProtobuf] ReqGetArena req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetArena(user);
    }

    [HttpPost("/v1/arena/getbaninfo")]
    public ActionResult<ResGetArenaBanInfo> GetArenaBanInfo([FromBodyProtobuf] ReqGetArenaBanInfo req)
        => service.GetArenaBanInfo();

    [HttpPost("/v1/arena/champion/get")]
    public ActionResult<ResGetChampionArena> GetChampion([FromBodyProtobuf] ReqGetChampionArena req)
        => new ResGetChampionArena { Schedule = new NetChampionArenaSchedule() };

    [HttpPost("/v1/arena/special/showreward")]
    public ActionResult<ResShowSpecialArenaReward> ShowSpecialArenaReward(
        [FromBodyProtobuf] ReqShowSpecialArenaReward req)
        => service.ShowSpecialArenaReward();

    [HttpPost("/v1/arena/special/get")]
    public ActionResult<ResGetSpecialArena> GetSpecialArena([FromBodyProtobuf] ReqGetSpecialArena req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetSpecialArena(user);
    }

    // --- Friend & PartyMatch ---

    [HttpPost("/v1/friend/get")]
    public ActionResult<ResGetFriendData> GetFriends([FromBodyProtobuf] ReqGetFriendData req)
        => new ResGetFriendData();

    [HttpPost("/v1/partymatch/listinvitation")]
    public ActionResult<ResListInvitation> ListPartyMatchInvitations([FromBodyProtobuf] ReqListInvitation req)
    {
        if (userService.GetUserId() is null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return new ResListInvitation();
    }
}
