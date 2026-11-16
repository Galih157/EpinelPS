using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
[Route("/v1/mail")]
public sealed class MailController(IUserService userService, MailService mailService) : ControllerBase
{
    [HttpPost("get")]
    public ActionResult<ResGetMailData> GetMail([FromBodyProtobuf] ReqGetMailData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return mailService.GetMails(userId);
    }

    [HttpPost("obtain2")]
    public ActionResult<ResObtainMail2> Obtain2([FromBodyProtobuf] ReqObtainMail2 req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return mailService.ObtainMail(user, req);
    }

    [HttpPost("allobtain")]
    public ActionResult<ResAllObtainMail> AllObtain([FromBodyProtobuf] ReqAllObtainMail req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return mailService.ObtainAllMails(user);
    }

    [HttpPost("deletereadmail")]
    public ActionResult<ResDeleteReadMail> DeleteReadMail([FromBodyProtobuf] ReqDeleteReadMail req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return mailService.DeleteReadMails(userId);
    }
}
