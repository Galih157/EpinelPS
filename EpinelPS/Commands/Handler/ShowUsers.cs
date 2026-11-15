using System.Text;
using EpinelPS.Database;
using EpinelPS.Commands.Core;
using EpinelPS.Commands.Binding;

namespace EpinelPS.Commands.Handler;

public class ShowUsersParameter : ICommandParameters
{
    static ParameterDescriptor[] ICommandParameters.Descriptors => [];
}

public class ShowUsersHandler(IExecutionContext context) : BaseHandler<ShowUsersParameter>(context)
{
    public override string Name => "show-users";
    public override string Description => "Display a list of all users in the database";

    protected async override Task<HandleResult> ExecuteAsync(ShowUsersParameter parameters)
    {
        using var db = GameContext.CreateNew();
        var sdkUsers = db.SdkUsers.ToDictionary(u => u.ID, u => u.Email);
        var gameUsers = db.Users.ToDictionary(u => u.ID, u => u.Nickname);
        var ids = sdkUsers.Keys.Union(gameUsers.Keys).OrderBy(id => id).ToList();

        if (ids.Count == 0)
            return new HandleResult(true, "No users found in the database.");

        var sb = new StringBuilder();
        sb.AppendLine("ID\t\tEmail\tNickname");
        foreach (var id in ids)
        {
            sdkUsers.TryGetValue(id, out var email);
            gameUsers.TryGetValue(id, out var nickname);
            sb.AppendLine($"{id}\t{email ?? "-"}\t{nickname ?? "-"}");
        }
        return new HandleResult(true, sb.ToString().TrimEnd());
    }
}
