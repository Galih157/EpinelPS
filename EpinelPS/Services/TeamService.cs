using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Stage;
using EpinelPS.Repositories;

namespace EpinelPS.Services;

public sealed class TeamService(TeamRepository repo, SoloRaidRepository soloRaidRepo, GameContext db)
{
    public ResGetTeamData GetTeamData(User user)
    {
        if (user.Characters.Count == 0 && UserDbHelper.GetOrCreateGameUser(db, user.ID).LastNormalStageCleared >= 6000002)
        {
            ClearStage.EnsureDefaultCharacters(user);
        }

        var response = new ResGetTeamData();
        if (user.Characters.Count > 0)
        {
            foreach (var team in repo.GetUserTeams(user.ID).Values)
            {
                response.TypeTeams.Add(team);
            }
        }
        return response;
    }

    public ResSetTeam SetTeam(User user, ReqSetTeam req)
    {
        var response = new ResSetTeam { Type = req.Type };
        response.Teams.AddRange(req.Teams);

        // Museum teams are scoped by stage (TeamType 31, ContentsId = MuseumStageTable.Id)
        if (req.Type == (int)TeamType.SoloRaidMuseum && req.ContentsId > 0)
        {
            var museumStage = soloRaidRepo.GetOrCreateMuseumStage(user.ID, req.ContentsId);

            foreach (var newTeam in req.Teams)
            {
                var index = museumStage.Teams.FindIndex(x => x.TeamNumber == newTeam.TeamNumber);
                if (index >= 0)
                    museumStage.Teams[index] = newTeam.Clone();
                else
                    museumStage.Teams.Add(newTeam.Clone());
            }

            soloRaidRepo.SaveMuseumStage(user.ID, museumStage);
            return response;
        }

        int contentsId = req.Teams.Count != 0
            ? req.Teams.Select(x => x.TeamNumber).Max()
            : req.ContentsId + 1;

        repo.UpsertTeamGroup(user.ID, req.Type, contentsId, req.Teams);
        db.SaveChanges();
        return response;
    }

    public ResListSupportCharacterUsedCount ListSupportCharacterCount(ReqListSupportCharacterUsedCount req)
    {
        foreach (int item in req.TeamTypeList)
        {
            Console.WriteLine("support character used: " + item);
        }
        return new ResListSupportCharacterUsedCount();
    }

    public ResSetProfileTeam SetProfileTeam(ulong userId, ReqSetProfileTeam req)
    {
        var current = repo.GetRepresentationTeam(userId).ToList();
        while (current.Count < req.Team.Slots.Count)
            current.Add(0);

        for (int i = 0; i < req.Team.Slots.Count; i++)
        {
            current[i] = req.Team.Slots[i].Value;
        }

        repo.SetRepresentationTeam(userId, current);
        db.SaveChanges();
        return new ResSetProfileTeam();
    }
}
