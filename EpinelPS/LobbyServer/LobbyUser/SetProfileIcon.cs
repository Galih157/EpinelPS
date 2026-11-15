using EpinelPS.Database;

namespace EpinelPS.LobbyServer.LobbyUser;

[GameRequest("/user/setprofileicon")]
public class SetProfileIcon : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetProfileIcon req = await ReadData<ReqSetProfileIcon>();
        UserDbHelper.UpdateGameUser(UserId, u =>
        {
            u.ProfileIconId = req.Icon;
            u.ProfileIconIsPrism = req.IsPrism;
        });
        ResSetProfileIcon response = new();

        await WriteDataAsync(response);
    }
}
