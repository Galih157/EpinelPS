using EpinelPS.Database;

namespace EpinelPS.LobbyServer.LobbyUser;

[GameRequest("/user/setprofiledata")]
public class SetProfileData : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetProfileData req = await ReadData<ReqSetProfileData>();
        UserDbHelper.UpdateGameUser(UserId, u =>
        {
            u.ProfileIconId = req.Icon;
            u.ProfileIconIsPrism = req.IsPrism;
            u.ProfileFrame = req.Frame;
        });
        ResSetProfileData response = new();

        await WriteDataAsync(response);
    }
}
