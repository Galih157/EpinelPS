using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class CosmeticsController(IUserService userService, CosmeticsService cosmeticsService) : ControllerBase
{
    [HttpPost("/v1/User/GetWallpaper")]
    public ActionResult<ResGetWallpaper> GetWallpaper([FromBodyProtobuf] ReqGetWallpaper req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.GetWallpaper(userId);
    }

    [HttpPost("/v1/User/SetWallpaper")]
    public ActionResult<ResSetWallpaper> SetWallpaper([FromBodyProtobuf] ReqSetWallpaper req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.SetWallpaper(userId, req);
    }

    [HttpPost("/v1/user/wallpaper/buy")]
    public ActionResult<ResBuyLobbyDecoBackground> BuyWallpaper(
        [FromBodyProtobuf] ReqBuyLobbyDecoBackground req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.BuyWallpaper(userId, req);
    }

    [HttpPost("/v1/user/getwallpaperinventory")]
    public ActionResult<ResGetWallpaperInventory> GetWallpaperInventory(
        [FromBodyProtobuf] ReqGetWallpaperInventory req)
    {
        return cosmeticsService.GetWallpaperInventory();
    }

    [HttpPost("/v1/ProfileCard/Possession/Get")]
    public ActionResult<ResProfileCardObjectList> GetProfileCardPossession(
        [FromBodyProtobuf] ReqProfileCardObjectList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.GetProfileCardPossession(userId);
    }

    [HttpPost("/v1/ProfileCard/DecorationLayout/Get")]
    public ActionResult<ResProfileCardDecorationLayout> GetProfileDecoration(
        [FromBodyProtobuf] ReqProfileCardDecorationLayout req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.GetProfileDecoration(userId);
    }

    [HttpPost("/v1/ProfileCard/DecorationLayout/Save")]
    public ActionResult<ResSaveProfileCardDecorationLayout> SaveProfileDecoration(
        [FromBodyProtobuf] ReqSaveProfileCardDecorationLayout req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.SaveProfileDecoration(userId, req);
    }

    [HttpPost("/v1/ProfileCard/Buy")]
    public ActionResult<ResBuyProfileCardObject> BuyProfileCard(
        [FromBodyProtobuf] ReqBuyProfileCardObject req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.BuyProfileCard(user, req);
    }

    [HttpPost("/v1/ProfileCard/ProfileRandomBox/Open")]
    public ActionResult<ResOpenProfileRandomBox> OpenProfileRandomBox(
        [FromBodyProtobuf] ReqOpenProfileRandomBox req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.OpenProfileRandomBox(user, req);
    }

    [HttpPost("/v1/lobby/usertitle/get")]
    public ActionResult<ResGetUserTitleList> GetUserTitles([FromBodyProtobuf] ReqGetUserTitleList req)
    {
        return cosmeticsService.GetUserTitles();
    }

    [HttpPost("/v1/outpost/memorial/getmemorylist")]
    public ActionResult<ResGetMemoryList> GetMemorialMemoryList([FromBodyProtobuf] ReqGetMemoryList req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return cosmeticsService.GetMemorialMemoryList(userId);
    }
}
