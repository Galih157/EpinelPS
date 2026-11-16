using EpinelPS.Database;
using EpinelPS.Interfaces;

namespace EpinelPS.Services;

public class UserService(IHttpContextAccessor httpContextAccessor) : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private User? _cachedUser;
    private bool _userLoaded;

    public ulong? GetUserId()
    {
        var id = _httpContextAccessor.HttpContext?.Items["UserID"];
        return id is ulong u && u != 0 ? u : null;
    }

    public User? GetUser()
    {
        if (_userLoaded)
            return _cachedUser;

        _cachedUser = GetUserId() is ulong u ? UserSessionStore.GetUser(u) : null;
        _userLoaded = true;
        return _cachedUser;
    }
}
