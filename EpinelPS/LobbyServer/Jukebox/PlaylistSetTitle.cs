using EpinelPS.Database;

namespace EpinelPS.LobbyServer.Jukebox;

[GameRequest("/jukebox/playlist/settitle")]
public class SetJukeboxPlaylistTitle : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetJukeboxPlaylistTitle req = await ReadData<ReqSetJukeboxPlaylistTitle>();
        User user = GetUser();

        ResSetJukeboxPlaylistTitle response = new();
        var playlist = user.PlayLists.FirstOrDefault(p => p.JukeboxPlaylistUid == req.JukeboxPlaylistUid);
        if (playlist != null)
        {
            playlist.Title = req.Title;
            UserSessionStore.SaveAll();
            response.Playlist = playlist;
        }

        await WriteDataAsync(response);
    }
}
