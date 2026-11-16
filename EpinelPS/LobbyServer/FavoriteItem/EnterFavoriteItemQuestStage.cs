using EpinelPS.Data;
using EpinelPS.Database;

namespace EpinelPS.LobbyServer.FavoriteItem;

[GameRequest("/favoriteitem/quest/stage/enter")]
public class EnterFavoriteItemQuestStage : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqEnterFavoriteItemQuestStage req = await ReadData<ReqEnterFavoriteItemQuestStage>();
        User user = GetUser();

        user.AddTrigger(Trigger.CampaignStart, 1, req.StageId);

        UserSessionStore.SaveAll();

        ResEnterFavoriteItemQuestStage response = new();
        await WriteDataAsync(response);
    }
}
