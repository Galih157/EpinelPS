namespace EpinelPS.Interfaces;

public interface IUserService
{
    ulong? GetUserId();
    User? GetUser();
}