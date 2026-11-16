using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Minigame.TTS;

[GameRequest("/MiniGame/TTS/Skin/Set")]
public class SkinSet : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetMiniGameTtsSkin req = await ReadData<ReqSetMiniGameTtsSkin>();
        User user = GetUser();
        ResSetMiniGameTtsSkin response = new();

        if (user.TTSGameData.TryGetValue(req.EventTtsManagerTableId, out var ttsData))
        {
            ttsData.SkinData = MiniGameHelper.FromProto<UserMiniGameTtsSkinData, NetUserMiniGameTtsSkinData>(req.UserSkinData);
        }
        // TODO
        UserSessionStore.SaveAll();
        await WriteDataAsync(response);
    }
}