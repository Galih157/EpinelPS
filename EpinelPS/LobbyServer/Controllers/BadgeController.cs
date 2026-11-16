using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
[Route("/v1/badge")]
public sealed class BadgeController(IUserService userService, BadgeService badgeService) : ControllerBase
{
    [HttpPost("sync")]
    public ActionResult<ResSyncBadge> Sync([FromBodyProtobuf] ReqSyncBadge req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return badgeService.SyncBadges(userId);
    }

    [HttpPost("delete")]
    public ActionResult<ResDeleteBadge> Delete([FromBodyProtobuf] ReqDeleteBadge req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return badgeService.DeleteBadges(userId, req);
    }

    [HttpPost("permanentcontent")]
    public ActionResult<ResPermanentContentBadgeData> PermanentContent([FromBodyProtobuf] ReqPermanentContentBadgeData req)
    {
        if (userService.GetUserId() is null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return badgeService.GetPermanentContent();
    }
}
