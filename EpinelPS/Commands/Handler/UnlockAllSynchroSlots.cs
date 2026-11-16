using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Commands.Core;
using EpinelPS.Commands.Binding;

namespace EpinelPS.Commands.Handler;

public class UnlockAllSynchroSlotsParameter : ICommandParameters
{
    static ParameterDescriptor[] ICommandParameters.Descriptors => [];
}

public class UnlockAllSynchroSlotsHandler(IExecutionContext context) : BaseHandler<UnlockAllSynchroSlotsParameter>(context)
{
    public override string Name => "unlock-all-synchro-slots";
    public override string Description => "Unlock all synchro device slots for the selected user";

    protected async override Task<HandleResult> ExecuteAsync(UnlockAllSynchroSlotsParameter parameters)
    {
        if (context.SelectedUser == null)
            return new HandleResult(false, "No user selected");

        using var db = GameContext.CreateNew();
        new OutpostRepository(db).UnlockAllSynchroSlots(context.SelectedUser.ID);
        return new HandleResult(true, $"Unlocked all {OutpostRepository.MaxSynchroSlots} synchro slots");
    }
}
