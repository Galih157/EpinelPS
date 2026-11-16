using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
[Route("/v1/messenger")]
public sealed class MessengerController(IUserService userService, MessengerService messengerService) : ControllerBase
{
    [HttpPost("get")]
    public ActionResult<ResGetMessages> GetMessages([FromBodyProtobuf] ReqGetMessages req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.GetMessages(userId, req);
    }

    [HttpPost("picked/get")]
    public ActionResult<ResGetPickedMessageList> GetPickedMessages([FromBodyProtobuf] ReqGetPickedMessageList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.GetPickedMessages(userId);
    }

    [HttpPost("enter")]
    public ActionResult<ResEnterMessengerDialog> EnterMessenger([FromBodyProtobuf] ReqEnterMessengerDialog req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.EnterMessenger(user, req);
    }

    [HttpPost("proceed")]
    public ActionResult<ResProceedMessage> Proceed([FromBodyProtobuf] ReqProceedMessage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.Proceed(user, req);
    }

    [HttpPost("subquest/enter")]
    public ActionResult<ResEnterSubQuestMessengerDialog> EnterSubquest([FromBodyProtobuf] ReqEnterSubQuestMessengerDialog req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.EnterSubquest(userId, req);
    }

    [HttpPost("subquestfin/enter")]
    public ActionResult<ResEnterSubQuestFinMessengerDialog> EnterFinishSubquest([FromBodyProtobuf] ReqEnterSubQuestFinMessengerDialog req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.EnterFinishSubquest(user, req);
    }

    [HttpPost("finsubquest")]
    public ActionResult<ResFinSubQuest> FinishSubquest([FromBodyProtobuf] ReqFinSubQuest req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.FinishSubquest(user, req);
    }

    [HttpPost("daily/pick")]
    public ActionResult<ResPickTodayDailyMessage> PickDaily([FromBodyProtobuf] ReqPickTodayDailyMessage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.PickDailyMessages(user, req);
    }

    [HttpPost("random/pick")]
    public ActionResult<ResPickTodayRandomMessage> PickRandom([FromBodyProtobuf] ReqPickTodayRandomMessage req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return messengerService.PickRandomMessages(user, req);
    }
}
