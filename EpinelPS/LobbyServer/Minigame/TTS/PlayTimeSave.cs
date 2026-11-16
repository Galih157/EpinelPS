using EpinelPS.Database;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.LobbyServer.Minigame.TTS;

[GameRequest("/MiniGame/TTS/PlayTime/Save")]
public class PlayTimeSave : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSaveMiniGameTtsPlayTime req = await ReadData<ReqSaveMiniGameTtsPlayTime>();
        User user = GetUser();
        ResSaveMiniGameTtsPlayTime response = new();

        if (user.TTSGameData.TryGetValue(req.EventTtsManagerTableId, out var ttsData))
        {
            ttsData.TotalPlayTime = req.TotalPlayTime;
        }

        UserSessionStore.SaveAll();
        await WriteDataAsync(response);
    }
}