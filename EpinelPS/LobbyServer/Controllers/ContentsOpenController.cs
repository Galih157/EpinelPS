using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Services;
using EpinelPS.Interfaces;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

/// <summary>
/// Controller for unlocking
/// </summary>
[ApiController]
public class ContentsOpen(
    IUserService db,
    ProgressRepository progressRepo,
    GameContext gameDb) : Controller
{
    [Route("/v1/contentsopen/get/unlock")]
    [HttpPost]
    public ActionResult<ResGetContentsOpenUnlockInfo> GetUnlocked([FromBodyProtobuf] ReqGetContentsOpenUnlockInfo req)
    {
        if (db.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        ResGetContentsOpenUnlockInfo response = new();
        Dictionary<int, UnlockData> unlocked = progressRepo.GetContentsUnlocked(userId);

        if (unlocked.Count == 0)
        {
            // These Always returned as true by official server
            // Fixes "Recruitment unlocked" during chapter 0
            // TODO: Don't hardcode this, maybe its in GameData
            int[] defaultIds = [2, 3, 4, 6, 15, 16, 18, 19];
            foreach (int id in defaultIds)
            {
                progressRepo.UpsertContentsUnlock(userId, id, true, true);
            }
            gameDb.SaveChanges();
            unlocked = progressRepo.GetContentsUnlocked(userId);
        }

        foreach (KeyValuePair<int, UnlockData> item in unlocked.OrderBy(x => x.Key))
        {
            response.ContentsOpenUnlockInfoList.Add(new NetContentsOpenUnlockInfo()
            {
                ContentsOpenTableId = item.Key,
                IsUnlockButtonPlayed = item.Value.ButtonAnimationPlayed,
                IsUnlockPopupPlayed = item.Value.PopupAnimationPlayed,
            });
        }

        return response;
    }

    [Route("/v1/contentsopen/set/unlock/button")]
    [HttpPost]
    public ActionResult<ResSetContentsOpenUnlockButtonPlay> SetButtonUnlocked([FromBodyProtobuf] ReqSetContentsOpenUnlockButtonPlay req)
    {
        if (db.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        UpdateUnlockFlags(userId, req.ContentsOpenTableIds, isButton: true);
        return new ResSetContentsOpenUnlockButtonPlay();
    }

    [Route("/v1/contentsopen/set/unlock/popup")]
    [HttpPost]
    public ActionResult<ResSetContentsOpenUnlockPopupPlay> SetButtonUnlocked([FromBodyProtobuf] ReqSetContentsOpenUnlockPopupPlay req)
    {
        if (db.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        UpdateUnlockFlags(userId, req.ContentsOpenTableIds, isButton: false);
        return new ResSetContentsOpenUnlockPopupPlay();
    }

    private void UpdateUnlockFlags(ulong userId, IEnumerable<int> contentsOpenTableIds, bool isButton)
    {
        Dictionary<int, UnlockData> unlocked = progressRepo.GetContentsUnlocked(userId);
        bool changed = false;

        foreach (int item in contentsOpenTableIds)
        {
            unlocked.TryGetValue(item, out UnlockData? existing);
            bool buttonPlayed = isButton || (existing?.ButtonAnimationPlayed ?? false);
            bool popupPlayed = !isButton || (existing?.PopupAnimationPlayed ?? false);

            if (existing != null &&
                existing.ButtonAnimationPlayed == buttonPlayed &&
                existing.PopupAnimationPlayed == popupPlayed)
            {
                continue;
            }

            progressRepo.UpsertContentsUnlock(userId, item, buttonPlayed, popupPlayed);
            unlocked[item] = new UnlockData(buttonPlayed, popupPlayed);
            changed = true;
        }

        if (changed)
            gameDb.SaveChanges();
    }
}
