using EpinelPS.Data;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database.Domains;

public sealed class TeamStore : IUserDomainStore
{
    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        var teamGroups = db.Set<UserTeamGroupEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Slots)
            .ToList();

        user.UserTeams = new Dictionary<int, NetUserTeamData>();
        foreach (var group in teamGroups)
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

            user.UserTeams[group.TeamType] = netUserTeam;
        }

        user.RepresentationTeamDataNew = db.Set<UserRepresentationTeamSlotEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Slot)
            .Select(x => x.CharacterCsn)
            .ToArray();
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        var userTeams = user.UserTeams ?? [];
        var groupsByType = new Dictionary<int, UserTeamGroupEntity>();
        DomainStoreHelpers.SyncCollection(
            db.Set<UserTeamGroupEntity>(),
            db.Set<UserTeamGroupEntity>().Where(x => x.UserId == userId),
            userTeams,
            x => x.TeamType,
            kv => kv.Key,
            kv =>
            {
                var entity = new UserTeamGroupEntity
                {
                    UserId = userId,
                    TeamType = kv.Key,
                    LastContentsTeamNumber = kv.Value.LastContentsTeamNumber
                };
                groupsByType[kv.Key] = entity;
                return entity;
            },
            (entity, kv) =>
            {
                entity.LastContentsTeamNumber = kv.Value.LastContentsTeamNumber;
                groupsByType[kv.Key] = entity;
            });

        var desiredSlots = userTeams
            .Where(kv => groupsByType.ContainsKey(kv.Key))
            .SelectMany(kv =>
            {
                string groupId = groupsByType[kv.Key].Id;
                return (kv.Value.Teams ?? Enumerable.Empty<NetTeamData>())
                    .SelectMany(team => (team.Slots ?? Enumerable.Empty<NetTeamSlot>())
                        .Select(slot => (
                            TeamGroupId: groupId,
                            team.TeamNumber,
                            slot.Slot,
                            slot.Value,
                            slot.ValueType)));
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserTeamSlotEntity>(),
            db.Set<UserTeamSlotEntity>().Where(x => x.UserId == userId),
            desiredSlots,
            x => (x.TeamGroupId, x.TeamNumber, x.Slot),
            s => (s.TeamGroupId, s.TeamNumber, s.Slot),
            s => new UserTeamSlotEntity
            {
                UserId = userId,
                TeamGroupId = s.TeamGroupId,
                TeamNumber = s.TeamNumber,
                Slot = s.Slot,
                Value = s.Value,
                ValueType = s.ValueType
            },
            (entity, s) =>
            {
                entity.Value = s.Value;
                entity.ValueType = s.ValueType;
            });

        var repSlots = (user.RepresentationTeamDataNew ?? [])
            .Select((csn, index) => (Slot: index, CharacterCsn: csn));

        DomainStoreHelpers.SyncCollection(
            db.Set<UserRepresentationTeamSlotEntity>(),
            db.Set<UserRepresentationTeamSlotEntity>().Where(x => x.UserId == userId),
            repSlots,
            x => x.Slot,
            s => s.Slot,
            s => new UserRepresentationTeamSlotEntity
            {
                UserId = userId,
                Slot = s.Slot,
                CharacterCsn = s.CharacterCsn
            },
            (entity, s) => entity.CharacterCsn = s.CharacterCsn);
    }
}
