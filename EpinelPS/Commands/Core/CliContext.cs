using EpinelPS.Database;

namespace EpinelPS.Commands.Core;

public class CliContext : IExecutionContext
{
    public User? SelectedUser { get; set; }
    public void Save()
    {
        if (SelectedUser != null)
            UserSessionStore.SaveUser(SelectedUser);
    }
}
