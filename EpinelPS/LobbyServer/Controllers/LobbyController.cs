using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Interfaces;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

/// <summary>
/// Controller for lobby
/// </summary>
[ApiController]
public class LobbyController(
    IUserService UserService,
    GameContext db,
    TeamRepository teamRepo,
    CosmeticsRepository cosmeticsRepo,
    OutpostRepository outpostRepo) : Controller
{
    /// <summary>
    /// Returns latest resource base URL for version number
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    [Route("/v1/enterlobbyserver")]
    [HttpPost]
    public ActionResult<ResEnterLobbyServer> EnterLobbyServer([FromBodyProtobuf] ReqEnterLobbyServer req)
    {
        User? user = UserService.GetUser();
        if (user == null) return Problem(type: NetUtils.InvalidSessionErrorType);

        var userDB = UserDbHelper.GetOrCreateGameUser(db, user.ID);

        TimeSpan battleTime = DateTime.UtcNow - userDB.BattleTime;
        long battleTimeMs = (long)(battleTime.TotalNanoseconds / 100);

        // NOTE: Keep this in sync with GetUser code
        if (userDB.Nickname == null)
        {
            db.Users.Where(u => u.ID == user.ID).ExecuteUpdate(setters => setters.SetProperty(u => u.Nickname, "Player"));
        }

        ResEnterLobbyServer response = new()
        {
            User = LobbyHandler.CreateNetUserDataFromUser(user),
            ResetHour = ServerConfigStore.ResetHourUtcTime,
            Nickname = userDB.Nickname,
            SynchroLv = 1,
            OutpostBattleLevel = outpostRepo.GetOutpostBattleLevel(user.ID),
            OutpostBattleTime = new NetOutpostBattleTime() { MaxBattleTime = 864000000000, MaxOverBattleTime = 12096000000000, BattleTime = battleTimeMs },

            Jukeboxv2 = new NetUserJukeboxDataV2() { CommandBgm = new NetJukeboxBgm() { JukeboxTableId = cosmeticsRepo.GetJukeboxSetting(user.ID, NetJukeboxLocation.CommanderRoom).TableId, Type = NetJukeboxBgmType.JukeboxTableId, Location = NetJukeboxLocation.CommanderRoom } }
        };

        response.Jukeboxv2.JukeboxTableIds.AddRange(JukeboxUtils.GetUnlockedSongs(user.ID, cosmeticsRepo));



        foreach (KeyValuePair<CurrencyType, long> item in user.Currency)
        {
            response.Currency.Add(new NetUserCurrencyData() { Type = (int)item.Key, Value = item.Value });
        }

        var synchroSlots = outpostRepo.GetSynchroSlots(user.ID);
        var synchroCsns = synchroSlots.Where(x => x.CharacterSerialNumber != 0)
            .Select(x => x.CharacterSerialNumber)
            .ToHashSet();
        int synchroLv = outpostRepo.GetSynchroLevel(user.ID, user.Characters);

        foreach (CharacterModel item in user.Characters)
        {
            bool isSynchro = item.Csn != 0 && synchroCsns.Contains(item.Csn);
            response.Character.Add(new NetUserCharacterData() { Default = new() { Csn = item.Csn, Skill1Lv = item.Skill1Lvl, Skill2Lv = item.Skill2Lvl, CostumeId = item.CostumeId, Lv = isSynchro ? synchroLv : item.Level, Grade = item.Grade, Tid = item.Tid, UltiSkillLv = item.UltimateLevel }, IsSynchro = isSynchro });
        }

        foreach (NetUserItemData item in NetUtils.GetUserItems(user))
        {
            response.Items.Add(item);
        }

        // Add squad data if there are characters
        if (user.Characters.Count > 0)
        {
            List<CharacterModel> highestLevelCharacters = [.. user.Characters.OrderByDescending(x => x.Level).Take(5)];
            response.SynchroLv = synchroLv;

            foreach (CharacterModel? item in highestLevelCharacters)
            {
                response.SynchroStandardCharacters.Add(item.Csn);
            }

            foreach (NetUserTeamData teamInfo in teamRepo.GetUserTeams(user.ID).Values)
                response.TypeTeams.Add(teamInfo);
        }

        response.Outposts.AddRange(outpostRepo.GetOrInitializeOutpostBuildings(user.ID));

        response.LastClearedNormalMainStageId = userDB.LastNormalStageCleared;
        response.LastClearedStoryStageId = userDB.LastStoryStageCleared;
        response.LastClearedHardMainStageId = userDB.LastHardStageCleared;
        response.LastClearedMod = userDB.LastClearedDifficulty;

        response.TimeRewardBuffs.AddRange(NetUtils.GetOutpostTimeReward(user, outpostRepo));

        response.OwnedLobbyDecoBackgroundIdList.AddRange(
            cosmeticsRepo.GetUnlockedCosmetics(user.ID, CosmeticsRepository.CategoryLobbyDecoBackground));

        response.ClearLessons.AddRange(outpostRepo.GetCompletedTacticAcademyLessons(user.ID));

        return response;
    }
}
