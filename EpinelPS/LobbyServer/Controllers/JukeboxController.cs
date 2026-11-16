using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class JukeboxController(IUserService userService, JukeboxService jukeboxService) : ControllerBase
{
    [HttpPost("/v1/jukebox/playlist/get")]
    public ActionResult<ResGetJukeboxPlaylist> GetPlaylist([FromBodyProtobuf] ReqGetJukeboxPlaylist req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.GetPlaylist(userId);
    }

    [HttpPost("/v1/jukebox/record/playhistory")]
    public ActionResult<ResRecordJukeboxPlayHistory> RecordPlayHistory(
        [FromBodyProtobuf] ReqRecordJukeboxPlayHistory req)
    {
        return jukeboxService.RecordPlayHistory();
    }

    [HttpPost("/v1/jukebox/favorite/addsong")]
    public ActionResult<ResAddJukeboxFavoriteSong> AddFavoriteSong(
        [FromBodyProtobuf] ReqAddJukeboxFavoriteSong req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.AddFavoriteSong(userId, req);
    }

    [HttpPost("/v1/jukebox/favorite/set")]
    public ActionResult<ResSetJukeboxFavoriteSongs> SetFavoriteSongs(
        [FromBodyProtobuf] ReqSetJukeboxFavoriteSongs req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.SetFavoriteSongs(userId, req);
    }

    [HttpPost("/v1/jukebox/playlist/addsong")]
    public ActionResult<ResAddSongJukeboxPlaylist> AddSongToPlaylist(
        [FromBodyProtobuf] ReqAddSongJukeboxPlaylist req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.AddSongToPlaylist(userId, req);
    }

    [HttpPost("/v1/jukebox/playlist/create")]
    public ActionResult<ResCreateJukeboxPlaylist> CreatePlaylist(
        [FromBodyProtobuf] ReqCreateJukeboxPlaylist req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.CreatePlaylist(userId, req);
    }

    [HttpPost("/v1/jukebox/playlist/delete")]
    public ActionResult<ResDeleteJukeboxPlaylist> DeletePlaylist(
        [FromBodyProtobuf] ReqDeleteJukeboxPlaylist req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.DeletePlaylist(userId, req);
    }

    [HttpPost("/v1/jukebox/playlist/set")]
    public ActionResult<ResSetJukeboxPlaylist> SetPlaylist([FromBodyProtobuf] ReqSetJukeboxPlaylist req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.SetPlaylist(userId, req);
    }

    [HttpPost("/v1/jukebox/playlist/settitle")]
    public ActionResult<ResSetJukeboxPlaylistTitle> SetPlaylistTitle(
        [FromBodyProtobuf] ReqSetJukeboxPlaylistTitle req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.SetPlaylistTitle(userId, req);
    }

    [HttpPost("/v1/jukebox/set/favorite")]
    public ActionResult<ResSetJukeboxBgmFavorite> SetBgmFavorite(
        [FromBodyProtobuf] ReqSetJukeboxBgmFavorite req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.SetBgmFavorite(userId, req);
    }

    [HttpPost("/v1/jukebox/set/playlist")]
    public ActionResult<ResSetJukeboxBgmPlaylist> SetBgmPlaylist(
        [FromBodyProtobuf] ReqSetJukeboxBgmPlaylist req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.SetBgmPlaylist(userId, req);
    }

    [HttpPost("/v1/jukebox/set/tableid")]
    public ActionResult<ResSetJukeboxBgmTableId> SetTableId([FromBodyProtobuf] ReqSetJukeboxBgmTableId req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.SetTableId(userId, req);
    }

    [HttpPost("/v1/mission/getrewarded/jukebox")]
    public ActionResult<ResGetJukeboxRewardedData> GetJukeboxRewards(
        [FromBodyProtobuf] ReqGetJukeboxRewardedData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.GetJukeboxRewards(userId);
    }

    [HttpPost("/v1/Trigger/ObtainTriggerRewardForJukebox")]
    public ActionResult<ResObtainTriggerRewardForJukebox> ObtainTriggerRewardForJukebox(
        [FromBodyProtobuf] ReqObtainTriggerRewardForJukebox req)
    {
        User? user = userService.GetUser();
        if (user == null)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return jukeboxService.ObtainTriggerRewardForJukebox(user, req);
    }
}
