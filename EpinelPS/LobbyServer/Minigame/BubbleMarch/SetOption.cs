using EpinelPS.Data;
using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Minigame.BubbleMarch;

[GameRequest("/arcade/bubblemarch/setoption")]
public class SetOption : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetArcadeBubbleMarchOption req = await ReadData<ReqSetArcadeBubbleMarchOption>();
        User user = GetUser();
        ResSetArcadeBubbleMarchOption response = new();

        ArcadeManagerRecord_Raw? arcade = GameData.Instance.ArcadeManagerTable.Values
             .Where(x => x.GameType == ArcadeGameType.BubbleMarch).FirstOrDefault();

        if (user.BubbleMarchDatas.TryGetValue(arcade.GameManagerId, out var marchData))
        {
            marchData.LevelHideOptionActive = req.LevelHideOptionIsActive;
        }
        // TODO
        UserSessionStore.SaveAll();
        await WriteDataAsync(response);
    }
}