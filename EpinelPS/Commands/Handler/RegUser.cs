using EpinelPS.Database;
using EpinelPS.Utils;
using EpinelPS.Commands.Core;
using EpinelPS.Commands.Binding;

namespace EpinelPS.Commands.Handler;

public class RegUserParameter : ICommandParameters
{
    static ParameterDescriptor[] ICommandParameters.Descriptors => [
        Param.String(0, "email", "The email account of the new user"),
        Param.String(1, "password", "The password for the new user"),
    ];

    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
}

public class RegUserHandler(IExecutionContext context) : BaseHandler<RegUserParameter>(context)
{
    public override string Name => "reg-user";
    public override string Description => "Register a new user in the database";

    protected async override Task<HandleResult> ExecuteAsync(RegUserParameter parameters)
    {
        using var db = GameContext.CreateNew();
        if (db.SdkUsers.Any(u => u.Email.ToLower() == parameters.Email.ToLower()))
            return new HandleResult(false, $"User with email '{parameters.Email}' already exists");

        ulong userId = UserDbHelper.GenerateNextUserId(db);
        bool isAdmin = !db.SdkUsers.Any();

        db.SdkUsers.Add(new SdkUser
        {
            ID = userId,
            Email = parameters.Email,
            PasswordHash = PasswordHelper.HashMd5(parameters.Password),
            RegisterTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            PlayerName = "Player_" + Rng.RandomString(8),
            IsAdmin = isAdmin,
            LastLogin = DateTime.UtcNow
        });

        db.Users.Add(new GameUser
        {
            ID = userId
        });

        await db.SaveChangesAsync();

        if (!JsonDb.Instance.Users.Any(u => u.ID == userId))
        {
            JsonDb.Instance.Users.Add(new User { ID = userId });
            JsonDb.Save();
        }

        return new HandleResult(true, $"User '{parameters.Email}' registered successfully with ID {userId}");
    }
}
