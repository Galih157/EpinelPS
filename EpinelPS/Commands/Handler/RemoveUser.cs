using EpinelPS.Database;
using EpinelPS.Commands.Core;
using EpinelPS.Commands.Binding;

namespace EpinelPS.Commands.Handler;

public class RemoveUserParameter : ICommandParameters
{
    static ParameterDescriptor[] ICommandParameters.Descriptors => [];
}

public class RemoveUserHandler(IExecutionContext context) : BaseHandler<RemoveUserParameter>(context)
{
    public override string Name => "remove-user";
    public override string Description => "Remove the selected user";

    protected async override Task<HandleResult> ExecuteAsync(RemoveUserParameter parameters)
    {
        if (context.SelectedUser == null)
            return new HandleResult(false, "No user selected");

        var sdkUser = UserDbHelper.GetSdkUser(context.SelectedUser.ID);
        var displayName = sdkUser?.Email ?? context.SelectedUser.ID.ToString();
        Console.WriteLine($"Are you sure you want to remove user {displayName} (ID: {context.SelectedUser.ID})? This action cannot be undone. (y/n)");
        var confirmation = Console.ReadLine();
        if (confirmation?.ToLower() != "y")
        {
            return new HandleResult(false, "User removal cancelled");
        }

        using (var db = GameContext.CreateNew())
        {
            var sdk = db.SdkUsers.Find(context.SelectedUser.ID);
            if (sdk != null) db.SdkUsers.Remove(sdk);
            var game = db.Users.Find(context.SelectedUser.ID);
            if (game != null) db.Users.Remove(game);
            await db.SaveChangesAsync();
        }

        JsonDb.Instance.Users.Remove(context.SelectedUser);
        JsonDb.Save();
        context.SelectedUser = null;
        return new HandleResult(true, $"User {displayName} removed successfully");
    }
}
