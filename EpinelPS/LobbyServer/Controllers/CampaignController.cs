using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Services;
using EpinelPS.Interfaces;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using EpinelPS.LobbyServer.Stage;

namespace EpinelPS.LobbyServer.Controllers;

/// <summary>
/// Controller for game startup and asset information retrival
/// </summary>
[ApiController]
public class CampaignController(
    IUserService db,
    TeamRepository teamRepo,
    ProgressRepository progressRepo,
    GameContext gameDb) : Controller
{
    [Route("/v1/shutdownflags/campaignpackage/getall")]
    [HttpPost]
    public ActionResult<ResCampaignPackageGetAllShutdownFlags> GetUnlocked([FromBodyProtobuf] ReqCampaignPackageGetAllShutdownFlags req)
    {
        User? user = db.GetUser();
        if (user == null) return Problem(type: NetUtils.InvalidSessionErrorType);

        // TODO
        return new ResCampaignPackageGetAllShutdownFlags();
    }

    [Route("/v1/campaign/getfield")]
    [HttpPost]
    public ActionResult<ResGetCampaignFieldData> GetCampaignField([FromBodyProtobuf] ReqGetCampaignFieldData req)
    {
        User? user = db.GetUser();
        if (user == null) return Problem(type: NetUtils.InvalidSessionErrorType);

        ClearStage.EnsureDefaultCharacters(user);

        ResGetCampaignFieldData response = new()
        {
            Field = StageQuestService.CreateFieldInfo(user.ID, req.MapId, progressRepo, gameDb, out bool bossEntered),
            Team = new NetUserTeamData() { LastContentsTeamNumber = 1, Type = 1 }
        };

        if (teamRepo.GetUserTeam(user.ID, 1) is NetUserTeamData userTeam && userTeam.Teams.Count > 0)
        {
            response.Team = userTeam.Clone();
            foreach (NetTeamData t in response.Team.Teams)
            {
                response.TeamPositions.Add(new NetCampaignTeamPosition()
                {
                    TeamNumber = t.TeamNumber,
                    Type = response.Team.Type,
                    Position = new NetVector3() { }
                });
            }
        }
        else
        {
            NetTeamData team = new() { TeamNumber = 1 };
            team.Slots.Add(new NetTeamSlot() { Slot = 1, Value = 47263455 });
            team.Slots.Add(new NetTeamSlot() { Slot = 2, Value = 47273456 });
            team.Slots.Add(new NetTeamSlot() { Slot = 3, Value = 47263457 });
            team.Slots.Add(new NetTeamSlot() { Slot = 4, Value = 47263458 });
            team.Slots.Add(new NetTeamSlot() { Slot = 5, Value = 47263459 });
            response.Team.Teams.Add(team);

            response.TeamPositions.Add(new NetCampaignTeamPosition() { TeamNumber = 1, Type = 1, Position = new NetVector3() { } });
        }

        string? resultingJson = progressRepo.GetMapJson(user.ID, req.MapId);
        if (resultingJson == null)
        {
            resultingJson = "";
            progressRepo.SaveMapJson(user.ID, req.MapId, resultingJson);
            gameDb.SaveChanges();
        }

        response.Json = resultingJson;
        return response;
    }

    [Route("/v1/campaign/savefield")]
    [HttpPost]
    public ActionResult<ResSaveCampaignField> SaveField([FromBodyProtobuf] ReqSaveCampaignField req)
    {
        User? user = db.GetUser();
        if (user == null) return Problem(type: NetUtils.InvalidSessionErrorType);

        ResSaveCampaignField response = new();

        progressRepo.SaveMapJson(user.ID, req.MapId, req.Json);
        gameDb.SaveChanges();
        return response;
    }

    [Route("/v1/campaign/savefieldobject")]
    [HttpPost]
    public ActionResult<ResSaveCampaignFieldObject> SaveObject([FromBodyProtobuf] ReqSaveCampaignFieldObject req)
    {
        User? user = db.GetUser();
        if (user == null) return Problem(type: NetUtils.InvalidSessionErrorType);

        ResSaveCampaignFieldObject response = new();

        Logging.WriteLine($"save {req.MapId} with {req.FieldObject.PositionId}", LogType.Debug);

        FieldInfoNew field = progressRepo.GetFieldInfo(user.ID, req.MapId) ?? new FieldInfoNew();

        field.CompletedObjects.Add(new NetFieldObject() { PositionId = req.FieldObject.PositionId, Json = req.FieldObject.Json, Type = req.FieldObject.Type });
        progressRepo.SaveFieldInfo(user.ID, req.MapId, field);
        gameDb.SaveChanges();

        return response;
    }

    [Route("/v1/campaign/getfieldobjectitemsnum")]
    [HttpPost]
    public ActionResult<ResGetCampaignFieldObjectItemsNum> GetFieldObjectCountTotal([FromBodyProtobuf] ReqGetCampaignFieldObjectItemsNum req)
    {
        User? user = db.GetUser();
        if (user == null) return Problem(type: NetUtils.InvalidSessionErrorType);

        ResGetCampaignFieldObjectItemsNum response = new();

        foreach (KeyValuePair<string, FieldInfoNew> map in progressRepo.GetFieldInfoMap(user.ID))
        {
            response.FieldObjectItemsNum.Add(new NetCampaignFieldObjectItemsNum()
            {
                MapId = map.Key,
                Count = map.Value.CompletedObjects.Where(x => x.Type == 1).Count()
            });
        }

        return response;
    }

    [Route("/v1/campaign/obtain/item")]
    [HttpPost]
    public ActionResult<ResObtainCampaignItem> ObtainItem([FromBodyProtobuf] ReqObtainCampaignItem req)
    {
        User? user = db.GetUser();
        if (user == null) return Problem(type: NetUtils.InvalidSessionErrorType);

        ResObtainCampaignItem response = new();

        FieldInfoNew field = progressRepo.GetFieldInfo(user.ID, req.MapId) ?? new FieldInfoNew();

        foreach (NetFieldObject item in field.CompletedObjects)
        {
            if (item.PositionId == req.FieldObject.PositionId)
            {
                Logging.WriteLine("attempted to collect campaign field object twice!", LogType.WarningAntiCheat);
                return Problem(type: NetUtils.AnticheatError);
            }
        }

        // Register and return reward
        var map = GameData.Instance.MapData[req.MapId];

        var position = map.ItemSpawner.Where(x => x.PositionId == req.FieldObject.PositionId).FirstOrDefault() ?? throw new Exception("bad position Id");

        FieldItemRecord positionReward = GameData.Instance.FieldItems[position.ItemId];
        RewardRecord reward = GameData.Instance.GetRewardTableEntry(positionReward.TypeValue) ?? throw new Exception("failed to get reward");
        response.Reward = RewardUtils.RegisterRewardsForUser(user, reward);

        // HIde it from the field
        field.CompletedObjects.Add(new NetFieldObject() { PositionId = req.FieldObject.PositionId, Type = req.FieldObject.Type });
        progressRepo.SaveFieldInfo(user.ID, req.MapId, field);
        gameDb.SaveChanges();

        UserSessionStore.SaveUser(user);
        return response;
    }
}
