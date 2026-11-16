using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class GuildController(IUserService userService, GuildService service) : ControllerBase
{
    // --- Guild ---

    [HttpPost("/v1/guild/recommendlist")]
    public ActionResult<ResRecommendGuildList> GetRecommendList([FromBodyProtobuf] ReqRecommendGuildList req)
        => new ResRecommendGuildList();

    [HttpPost("/v1/guild/create")]
    public ActionResult<ResCreateGuild> CreateGuild([FromBodyProtobuf] ReqCreateGuild req)
        => service.CreateGuild();

    [HttpPost("/v1/guild/get")]
    public ActionResult<ResGetGuild> GetGuild([FromBodyProtobuf] ReqGetGuild req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetGuild(user, req);
    }

    [HttpPost("/v1/guild/search")]
    public ActionResult<ResSearchGuildList> SearchGuild([FromBodyProtobuf] ReqSearchGuildList req)
        => service.SearchGuild();

    [HttpPost("/v1/guild/publicinfo")]
    public ActionResult<ResGetGuildPublicInfo> GetGuildPublicInfo([FromBodyProtobuf] ReqGetGuildPublicInfo req)
        => service.GetGuildPublicInfo();

    [HttpPost("/v1/guild/join")]
    public ActionResult<ResJoinGuild> JoinGuild([FromBodyProtobuf] ReqJoinGuild req)
        => new ResJoinGuild { Result = JoinGuildResult.Ok };

    [HttpPost("/v1/guild/leave")]
    public ActionResult<ResLeaveGuild> LeaveGuild([FromBodyProtobuf] ReqLeaveGuild req)
        => service.LeaveGuild();

    [HttpPost("/v1/guild/getlog")]
    public ActionResult<ResGetGuildLog> GetGuildLog([FromBodyProtobuf] ReqGetGuildLog req)
        => new ResGetGuildLog { Result = GetGuildLogResult.Ok };

    [HttpPost("/v1/guild/chat/get")]
    public ActionResult<ResGetGuildChatList> GetGuildChatList([FromBodyProtobuf] ReqGetGuildChatList req)
        => new ResGetGuildChatList { Result = GetGuildChatListResult.Success };

    [HttpPost("/v1/guild/chat/send")]
    public ActionResult<ResSendGuildChat> SendGuildChat([FromBodyProtobuf] ReqSendGuildChat req)
        => new ResSendGuildChat { Result = GuildChatSendResult.Success };

    [HttpPost("/v1/guild/chat/delete")]
    public ActionResult<ResDeleteGuildChat> DeleteGuildChat([FromBodyProtobuf] ReqDeleteGuildChat req)
        => new ResDeleteGuildChat { Result = DeleteGuildChatResult.Success };

    [HttpPost("/v1/guild/joinrequestlist")]
    public ActionResult<ResGuildJoinRequestList> GetGuildJoinRequestList(
        [FromBodyProtobuf] ReqGuildJoinRequestList req)
        => new ResGuildJoinRequestList();

    [HttpPost("/v1/guild/confirmjoinrequest")]
    public ActionResult<ResConfirmGuildJoinRequest> ConfirmGuildJoinRequest(
        [FromBodyProtobuf] ReqConfirmGuildJoinRequest req)
        => new ResConfirmGuildJoinRequest();

    [HttpPost("/v1/guild/allconfirmjoinrequest")]
    public ActionResult<ResAllConfirmGuildJoinRequest> AllConfirmGuildJoinRequest(
        [FromBodyProtobuf] ReqAllConfirmGuildJoinRequest req)
        => new ResAllConfirmGuildJoinRequest();

    [HttpPost("/v1/guild/kickmember")]
    public ActionResult<ResKickGuildMember> KickGuildMember([FromBodyProtobuf] ReqKickGuildMember req)
        => new ResKickGuildMember { Result = KickGuildMemberResult.Ok };

    [HttpPost("/v1/guild/changemanager")]
    public ActionResult<ResChangeGuildManager> ChangeGuildManager([FromBodyProtobuf] ReqChangeGuildManager req)
        => new ResChangeGuildManager { Result = ChangeGuildManagerResult.Ok };

    [HttpPost("/v1/guild/sendmail")]
    public ActionResult<ResSendGuildMail> SendGuildMail([FromBodyProtobuf] ReqSendGuildMail req)
        => new ResSendGuildMail();

    [HttpPost("/v1/guild/setname")]
    public ActionResult<ResSetGuildName> SetGuildName([FromBodyProtobuf] ReqSetGuildName req)
        => new ResSetGuildName { Result = SetGuildNameResult.Okay };

    [HttpPost("/v1/guild/setnameforfree")]
    public ActionResult<ResSetGuildNameForFree> SetGuildNameForFree([FromBodyProtobuf] ReqSetGuildNameForFree req)
        => new ResSetGuildNameForFree { Result = SetGuildNameResult.Okay };

    [HttpPost("/v1/guild/updatenotice")]
    public ActionResult<ResUpdateGuildNotice> UpdateGuildNotice([FromBodyProtobuf] ReqUpdateGuildNotice req)
        => service.UpdateGuildNotice();

    [HttpPost("/v1/guild/updatesettings2")]
    public ActionResult<ResUpdateGuildSettings2> UpdateGuildSettings([FromBodyProtobuf] ReqUpdateGuildSettings2 req)
        => service.UpdateGuildSettings();

    // --- UnionRaid ---

    [HttpPost("/v1/guild/unionraid/get")]
    public ActionResult<ResGetUnionRaidData> GetUnionRaidData([FromBodyProtobuf] ReqGetUnionRaidData req)
        => service.GetUnionRaidData();

    [HttpPost("/v1/guild/unionraid/getstepinfo")]
    public ActionResult<ResGetUnionRaidStepInfo> GetUnionRaidStepInfo([FromBodyProtobuf] ReqGetUnionRaidStepInfo req)
        => service.GetUnionRaidStepInfo();

    [HttpPost("/v1/guild/unionraid/get/nowplaying")]
    public ActionResult<ResGetUnionRaidNowPlaying> GetUnionRaidNowPlaying(
        [FromBodyProtobuf] ReqGetUnionRaidNowPlaying req)
        => new ResGetUnionRaidNowPlaying
        {
            NowPlaying = false,
            PeriodResult = UnionRaidPeriodResult.Success
        };

    [HttpPost("/v1/guild/unionraid/getjoindata")]
    public ActionResult<ResGetUnionRaidJoinData> GetUnionRaidJoinData([FromBodyProtobuf] ReqGetUnionRaidJoinData req)
        => service.GetUnionRaidJoinData();

    [HttpPost("/v1/guild/unionraid/hard/getstepinfo")]
    public ActionResult<ResGetUnionRaidHardStepInfo> GetUnionRaidHardStepInfo(
        [FromBodyProtobuf] ReqGetUnionRaidHardStepInfo req)
        => new ResGetUnionRaidHardStepInfo { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/hard/get/nowplaying")]
    public ActionResult<ResGetUnionRaidHardNowPlaying> GetUnionRaidHardNowPlaying(
        [FromBodyProtobuf] ReqGetUnionRaidHardNowPlaying req)
        => new ResGetUnionRaidHardNowPlaying
        {
            NowPlaying = false,
            PeriodResult = UnionRaidPeriodResult.Success
        };

    [HttpPost("/v1/guild/unionraid/bosspopup/get")]
    public ActionResult<ResGetUnionRaidBossPopupData> GetUnionRaidBossPopup(
        [FromBodyProtobuf] ReqGetUnionRaidBossPopupData req)
        => new ResGetUnionRaidBossPopupData { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/bosspopup/set")]
    public ActionResult<ResSetUnionRaidBossPopupData> SetUnionRaidBossPopup(
        [FromBodyProtobuf] ReqSetUnionRaidBossPopupData req)
        => new ResSetUnionRaidBossPopupData { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/memberlog")]
    public ActionResult<ResGetUnionRaidMemberLog> GetUnionRaidMemberLog(
        [FromBodyProtobuf] ReqGetUnionRaidMemberLog req)
        => new ResGetUnionRaidMemberLog { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/log/step")]
    public ActionResult<ResGetUnionRaidStepBattleLog> GetUnionRaidStepBattleLog(
        [FromBodyProtobuf] ReqGetUnionRaidStepBattleLog req)
        => new ResGetUnionRaidStepBattleLog { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/log/user")]
    public ActionResult<ResGetUnionRaidUserBattleLog> GetUnionRaidUserBattleLog(
        [FromBodyProtobuf] ReqGetUnionRaidUserBattleLog req)
        => new ResGetUnionRaidUserBattleLog { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/log/whole")]
    public ActionResult<ResGetUnionRaidWholeBattleLog> GetUnionRaidWholeBattleLog(
        [FromBodyProtobuf] ReqGetUnionRaidWholeBattleLog req)
        => new ResGetUnionRaidWholeBattleLog { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/enter")]
    public ActionResult<ResEnterUnionRaid> EnterUnionRaid([FromBodyProtobuf] ReqEnterUnionRaid req)
        => new ResEnterUnionRaid();

    [HttpPost("/v1/guild/unionraid/setdamage")]
    public ActionResult<ResSetUnionRaidDamage> SetUnionRaidDamage([FromBodyProtobuf] ReqSetUnionRaidDamage req)
        => new ResSetUnionRaidDamage { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/practice/enter")]
    public ActionResult<ResEnterUnionRaidPractice> EnterUnionRaidPractice(
        [FromBodyProtobuf] ReqEnterUnionRaidPractice req)
        => new ResEnterUnionRaidPractice { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/pratice/setdamage")]
    public ActionResult<ResSetUnionRaidPracticeDamage> SetUnionRaidPracticeDamage(
        [FromBodyProtobuf] ReqSetUnionRaidPracticeDamage req)
        => new ResSetUnionRaidPracticeDamage { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/hard/enter")]
    public ActionResult<ResEnterUnionRaidHard> EnterUnionRaidHard([FromBodyProtobuf] ReqEnterUnionRaidHard req)
        => new ResEnterUnionRaidHard();

    [HttpPost("/v1/guild/unionraid/hard/setdamage")]
    public ActionResult<ResSetUnionRaidHardDamage> SetUnionRaidHardDamage(
        [FromBodyProtobuf] ReqSetUnionRaidHardDamage req)
        => new ResSetUnionRaidHardDamage { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/hard/practice/enter")]
    public ActionResult<ResEnterUnionRaidHardPractice> EnterUnionRaidHardPractice(
        [FromBodyProtobuf] ReqEnterUnionRaidHardPractice req)
        => new ResEnterUnionRaidHardPractice { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/pratice/setdamage/hard")]
    public ActionResult<ResSetUnionRaidHardPracticeDamage> SetUnionRaidHardPracticeDamage(
        [FromBodyProtobuf] ReqSetUnionRaidHardPracticeDamage req)
        => new ResSetUnionRaidHardPracticeDamage { PeriodResult = UnionRaidPeriodResult.Success };

    [HttpPost("/v1/guild/unionraid/ranking/world")]
    public ActionResult<ResListUnionRaidWorldRanking> ListUnionRaidWorldRanking(
        [FromBodyProtobuf] ReqListUnionRaidWorldRanking req)
        => new ResListUnionRaidWorldRanking { PeriodResult = UnionRaidPeriodResult.Success };

    // --- ShootingRange ---

    [HttpPost("/v1/shootingrange/v2/get")]
    public ActionResult<ResGetShootingRangeRankingV2> GetShootingRangeRanking(
        [FromBodyProtobuf] ReqGetShootingRangeRankingV2 req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetShootingRangeRanking(user);
    }

    [HttpPost("/v1/shootingrange/v2/set")]
    public ActionResult<ResSetShootingRangeScoreV2> SetShootingRangeScore(
        [FromBodyProtobuf] ReqSetShootingRangeScoreV2 req)
        => service.SetShootingRangeScore(req);

    [HttpPost("/v1/shootingrange/getbattlelog")]
    public ActionResult<ResGetShootingRangeBattleLog> GetShootingRangeBattleLog(
        [FromBodyProtobuf] ReqGetShootingRangeBattleLog req)
        => new ResGetShootingRangeBattleLog();
}
