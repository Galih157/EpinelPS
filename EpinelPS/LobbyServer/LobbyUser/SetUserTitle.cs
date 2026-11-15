using EpinelPS.Database;
namespace EpinelPS.LobbyServer.LobbyUser;

[GameRequest("/lobby/usertitle/set")]
public class SetUserTitleData : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetUserTitle req = await ReadData<ReqSetUserTitle>();
        UserDbHelper.UpdateGameUser(UserId, u => u.TitleId = req.UserTitleId);
        ResSetUserTitle response = new();

        await WriteDataAsync(response);
    }
}
