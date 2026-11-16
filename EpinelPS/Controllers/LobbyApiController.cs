using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.Controllers;

[Route("v1")]
[ApiController]
public class LobbyApiController : ControllerBase
{
    [HttpPost]
    [Route("{**all}", Order = int.MaxValue)]
    [Consumes("application/octet-stream+protobuf")]
    [Produces("application/octet-stream+protobuf")]
    public ActionResult<ResGetNow> CatchAll(string all)
    {
        Logging.WriteLine($"[LobbyApiController] Unhandled POST {HttpContext.Request.Path.Value}", LogType.Error);
        return Ok(new ResGetNow());
    }
}
