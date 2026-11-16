using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class TeamController(IUserService userService, TeamService teamService) : ControllerBase
{
    [HttpPost("/v1/team/get")]
    public ActionResult<ResGetTeamData> GetTeamData([FromBodyProtobuf] ReqGetTeamData req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return teamService.GetTeamData(user);
    }

    [HttpPost("/v1/team/setteam")]
    public ActionResult<ResSetTeam> SetTeam([FromBodyProtobuf] ReqSetTeam req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return teamService.SetTeam(user, req);
    }

    [HttpPost("/v1/team/support-character/list-used-count")]
    public ActionResult<ResListSupportCharacterUsedCount> ListSupportCharacterCount(
        [FromBodyProtobuf] ReqListSupportCharacterUsedCount req)
    {
        return teamService.ListSupportCharacterCount(req);
    }

    [HttpPost("/v1/user/setprofileteam")]
    public ActionResult<ResSetProfileTeam> SetProfileTeam([FromBodyProtobuf] ReqSetProfileTeam req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return teamService.SetProfileTeam(userId, req);
    }
}
