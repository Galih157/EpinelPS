using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[GameRequest("/soloraid/trial/setdamage")]
public class SetDamageTrial : LobbyMessage
{
    protected override async Task HandleAsync()
    {

        var req = await ReadData<ReqSetSoloRaidTrialDamage>();
        User user = GetUser();

        ResSetSoloRaidTrialDamage response = new();

        try
        {
            SoloRaidHelper.SetDamageTrial(user, ref response, req);
        }
        catch (Exception ex)
        {
            Logging.WriteLine($"SetDamageTrial Error: {ex.Message}", LogType.Error);
        }

        UserSessionStore.SaveAll();
        await WriteDataAsync(response);
    }
}