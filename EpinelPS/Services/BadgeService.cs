using EpinelPS.Database;
using EpinelPS.Repositories;

namespace EpinelPS.Services;

public sealed class BadgeService(SocialMailRepository repo, GameContext db)
{
    public ResSyncBadge SyncBadges(ulong userId)
    {
        var response = new ResSyncBadge();
        foreach (var item in repo.GetBadges(userId))
        {
            response.BadgeList.Add(item.ToNet());
        }
        return response;
    }

    public ResDeleteBadge DeleteBadges(ulong userId, ReqDeleteBadge req)
    {
        if (repo.DeleteBadges(userId, req.BadgeSeqList) > 0)
        {
            db.SaveChanges();
        }
        return new ResDeleteBadge();
    }

    public ResPermanentContentBadgeData GetPermanentContent()
    {
        return new ResPermanentContentBadgeData
        {
            ChampionArenaBadgeData = new()
            {
                Schedule = new(),
                NextSchedule = new(),
                ChampionArenaContentsState = ChampionArenaContentsState.SeasonClosed,
                CurrentOrLastSeasonStartAt = new()
            },
            SoloRaidMuseumBadgeData = new()
        };
    }
}
