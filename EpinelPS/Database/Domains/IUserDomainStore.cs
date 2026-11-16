using EpinelPS.Models;

namespace EpinelPS.Database.Domains;

public interface IUserDomainStore
{
    void Load(GameContext db, User user);
    void Save(GameContext db, User user);
}
