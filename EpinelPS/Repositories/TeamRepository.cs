using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Repositories;

public sealed class TeamRepository(GameContext db)
{
    public Dictionary<int, NetUserTeamData> GetUserTeams(ulong userId)
    {
        var teamGroups = db.Set<UserTeamGroupEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Slots)
            .ToList();

        var result = new Dictionary<int, NetUserTeamData>(teamGroups.Count);
        foreach (var group in teamGroups)
        {
            result[group.TeamType] = MapToNetUserTeam(group);
        }
        return result;
    }

    public NetUserTeamData? GetUserTeam(ulong userId, int teamType)
    {
        var group = db.Set<UserTeamGroupEntity>()
            .AsNoTracking()
            .Include(x => x.Slots)
            .FirstOrDefault(x => x.UserId == userId && x.TeamType == teamType);

        return group != null ? MapToNetUserTeam(group) : null;
    }

    public bool HasTeamType(ulong userId, int teamType)
    {
        return db.Set<UserTeamGroupEntity>()
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.TeamType == teamType);
    }

    public void UpsertTeamGroup(ulong userId, int teamType, int lastContentsTeamNumber, IEnumerable<NetTeamData> incomingTeams)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var group = db.Set<UserTeamGroupEntity>()
            .Include(x => x.Slots)
            .FirstOrDefault(x => x.UserId == userId && x.TeamType == teamType);

        if (group == null)
        {
            group = new UserTeamGroupEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                TeamType = teamType,
                LastContentsTeamNumber = lastContentsTeamNumber
            };
            db.Set<UserTeamGroupEntity>().Add(group);
        }
        else
        {
            group.LastContentsTeamNumber = lastContentsTeamNumber;
        }

        // Group incoming teams by TeamNumber (last wins if duplicated in request)
        var teamsByNumber = incomingTeams
            .GroupBy(t => t.TeamNumber)
            .ToDictionary(g => g.Key, g => g.Last());

        if (group.Slots.Count > 0)
        {
            var toRemove = group.Slots
                .Where(s => teamsByNumber.ContainsKey(s.TeamNumber))
                .ToList();
            if (toRemove.Count > 0)
                db.Set<UserTeamSlotEntity>().RemoveRange(toRemove);
        }

        foreach (var (teamNumber, team) in teamsByNumber)
        {
            var seenSlots = new HashSet<int>();
            foreach (var slot in team.Slots)
            {
                if (!seenSlots.Add(slot.Slot))
                    continue;

                db.Set<UserTeamSlotEntity>().Add(new UserTeamSlotEntity
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    TeamGroupId = group.Id,
                    TeamNumber = teamNumber,
                    Slot = slot.Slot,
                    Value = slot.Value,
                    ValueType = slot.ValueType
                });
            }
        }
    }

    public long[] GetRepresentationTeam(ulong userId)
    {
        return db.Set<UserRepresentationTeamSlotEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Slot)
            .Select(x => x.CharacterCsn)
            .ToArray();
    }

    public void SetRepresentationTeam(ulong userId, IReadOnlyList<long> characterCsns)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);
        var set = db.Set<UserRepresentationTeamSlotEntity>();
        var existing = set.Where(x => x.UserId == userId).ToList();
        if (existing.Count > 0)
            set.RemoveRange(existing);

        for (int i = 0; i < characterCsns.Count; i++)
        {
            set.Add(new UserRepresentationTeamSlotEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Slot = i,
                CharacterCsn = characterCsns[i]
            });
        }
    }

    private static NetUserTeamData MapToNetUserTeam(UserTeamGroupEntity group)
    {
        var netUserTeam = new NetUserTeamData
        {
            Type = group.TeamType,
            LastContentsTeamNumber = group.LastContentsTeamNumber
        };

        foreach (var teamGroup in group.Slots.GroupBy(s => s.TeamNumber).OrderBy(g => g.Key))
        {
            var netTeam = new NetTeamData { TeamNumber = teamGroup.Key };
            foreach (var slot in teamGroup.OrderBy(s => s.Slot))
            {
                netTeam.Slots.Add(new NetTeamSlot
                {
                    Slot = slot.Slot,
                    Value = slot.Value,
                    ValueType = slot.ValueType
                });
            }
            netUserTeam.Teams.Add(netTeam);
        }

        return netUserTeam;
    }
}
