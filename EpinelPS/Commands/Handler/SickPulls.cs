using EpinelPS.Database;
using EpinelPS.Commands.Core;
using EpinelPS.Commands.Binding;

namespace EpinelPS.Commands.Handler;

public class SickPullsParameter : ICommandParameters
{
    static ParameterDescriptor[] ICommandParameters.Descriptors => [];
}

public class SickPullsHandler(IExecutionContext context) : BaseHandler<SickPullsParameter>(context)
{
    public override string Name => "sick-pulls";
    public override string Description => "Toggle equal chances for all characters to be pulled";

    protected async override Task<HandleResult> ExecuteAsync(SickPullsParameter parameters)
    {
        if (context.SelectedUser == null)
            return new HandleResult(false, "No user selected");

        var gameUser = UserDbHelper.GetOrCreateGameUser(context.SelectedUser.ID);
        bool nextSickPulls = !gameUser.SickPullsEnabled;
        UserDbHelper.UpdateGameUser(context.SelectedUser.ID, u => u.SickPullsEnabled = nextSickPulls);
        var sdkUser = UserDbHelper.GetSdkUser(context.SelectedUser.ID);
        return new HandleResult(true, $"Sick pulls set to {nextSickPulls} for user {sdkUser?.Email ?? context.SelectedUser.ID.ToString()}");
    }
}
