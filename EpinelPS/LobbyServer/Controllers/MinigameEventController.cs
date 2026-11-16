using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class MinigameEventController(IUserService userService, MinigameEventService service) : ControllerBase
{
    // --- AZX ---

    [HttpPost("/v1/event/minigame/azx/acquire/achievementmission/reward")]
    public ActionResult<ResAcquireMiniGameAzxAchievementMissionReward> AcquireAzxAchievementMissionReward(
        [FromBodyProtobuf] ReqAcquireMiniGameAzxAchievementMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AcquireAzxAchievementMissionReward(user, req);
    }

    [HttpPost("/v1/event/minigame/azx/enter")]
    public ActionResult<ResEnterMiniGameAzx> EnterAzx([FromBodyProtobuf] ReqEnterMiniGameAzx req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterAzx(userId, req);
    }

    [HttpPost("/v1/event/minigame/azx/finish")]
    public ActionResult<ResFinishMiniGameAzx> FinishAzx([FromBodyProtobuf] ReqFinishMiniGameAzx req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishAzx(user, req);
    }

    [HttpPost("/v1/event/minigame/azx/get/data")]
    public ActionResult<ResGetMiniGameAzxData> GetAzxData([FromBodyProtobuf] ReqGetMiniGameAzxData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetAzxData(userId, req.AzxId);
    }

    [HttpPost("/v1/event/minigame/azx/get/ranking")]
    public ActionResult<ResGetMiniGameAzxRanking> GetAzxRanking([FromBodyProtobuf] ReqGetMiniGameAzxRanking req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetAzxRanking(userId, req.AzxId);
    }

    [HttpPost("/v1/event/minigame/azx/get/reddot/data")]
    public ActionResult<ResGetMiniGameAzxRedDotData> GetAzxRedDotData(
        [FromBodyProtobuf] ReqGetMiniGameAzxRedDotData req)
        => new ResGetMiniGameAzxRedDotData
        {
            IsDailyMissionAvailable = true,
            AchievementMissionRewardExists = true
        };

    [HttpPost("/v1/event/minigame/azx/set/board/unlocked")]
    public ActionResult<ResSetMiniGameAzxBoardUnlocked> SetAzxBoardUnlocked(
        [FromBodyProtobuf] ReqSetMiniGameAzxBoardUnlocked req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetAzxBoardUnlocked(userId, req);
    }

    [HttpPost("/v1/event/minigame/azx/set/character/unlocked")]
    public ActionResult<ResSetMiniGameAzxCharacterUnlocked> SetAzxCharacterUnlocked(
        [FromBodyProtobuf] ReqSetMiniGameAzxCharacterUnlocked req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetAzxCharacterUnlocked(userId, req);
    }

    [HttpPost("/v1/event/minigame/azx/set/cutscene/confirmed")]
    public ActionResult<ResSetMiniGameAzxCutSceneConfirmed> SetAzxCutSceneConfirmed(
        [FromBodyProtobuf] ReqSetMiniGameAzxCutSceneConfirmed req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetAzxCutSceneConfirmed(userId, req);
    }

    [HttpPost("/v1/event/minigame/azx/set/skill/unlocked")]
    public ActionResult<ResSetMiniGameAzxSkillUnlocked> SetAzxSkillUnlocked(
        [FromBodyProtobuf] ReqSetMiniGameAzxSkillUnlocked req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetAzxSkillUnlocked(userId, req);
    }

    [HttpPost("/v1/event/minigame/azx/set/tutorial/confirmed")]
    public ActionResult<ResSetMiniGameAzxTutorialConfirmed> SetAzxTutorialConfirmed(
        [FromBodyProtobuf] ReqSetMiniGameAzxTutorialConfirmed req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetAzxTutorialConfirmed(userId);
    }

    // --- Event Minigame Stubs (CE002, CE006, Lycoris, RebuildEden) ---

    [HttpPost("/v1/event/minigame/ce002/get")]
    public ActionResult<ResGetMiniGameCe002Data> GetCe002Data([FromBodyProtobuf] ReqGetMiniGameCe002Data req)
        => new ResGetMiniGameCe002Data
        {
            Data = new()
            {
                Ce002Id = req.Ce002Id
            }
        };

    [HttpPost("/v1/event/minigame/stellar-blade/statistics/get")]
    public ActionResult<ResGetStellarBladeStatistics> GetStellarBladeStatistics(
        [FromBodyProtobuf] ReqGetStellarBladeStatistics req)
        => new ResGetStellarBladeStatistics();

    [HttpPost("/v1/event/minigame/lycoris/getdata")]
    public ActionResult<ResMiniGameLycorisDataInfo> GetLycorisData([FromBodyProtobuf] ReqMiniGameLycorisDataInfo req)
        => new ResMiniGameLycorisDataInfo
        {
            BaseData = new(),
            DailyTask = new(),
            Skills = new()
        };

    [HttpPost("/v1/event/minigame/rebuildeden/getbadge")]
    public ActionResult<ResGetRebuildEdenBadge> GetRebuildEdenBadge([FromBodyProtobuf] ReqGetRebuildEdenBadge req)
        => new ResGetRebuildEdenBadge();

    // --- IsLandBreaker ---

    [HttpPost("/v1/event/minigame/islandbreaker/buff/reset")]
    public ActionResult<ResResetMiniGameIslandBreakerBuff> ResetIslandBreakerBuff(
        [FromBodyProtobuf] ReqResetMiniGameIslandBreakerBuff req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResetIslandBreakerBuff(userId, req.IslandBreakerId);
    }

    [HttpPost("/v1/event/minigame/islandbreaker/buff/upgrade")]
    public ActionResult<ResUpgradeMiniGameIslandBreakerBuff> UpgradeIslandBreakerBuff(
        [FromBodyProtobuf] ReqUpgradeMiniGameIslandBreakerBuff req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UpgradeIslandBreakerBuff(userId, req);
    }

    [HttpPost("/v1/event/minigame/islandbreaker/character/save")]
    public ActionResult<ResSaveMiniGameIslandBreakerLastSelectedCharacter> SaveIslandBreakerCharacter(
        [FromBodyProtobuf] ReqSaveMiniGameIslandBreakerLastSelectedCharacter req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SaveIslandBreakerCharacter(userId, req);
    }

    [HttpPost("/v1/event/minigame/islandbreaker/get")]
    public ActionResult<ResGetMiniGameIslandBreaker> GetIslandBreaker(
        [FromBodyProtobuf] ReqGetMiniGameIslandBreaker req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetIslandBreaker(user, req.IslandBreakerId);
    }

    [HttpPost("/v1/event/minigame/islandbreaker/get/reddot/data")]
    public ActionResult<ResGetMiniGameIslandBreakerRedDotData> GetIslandBreakerRedDot(
        [FromBodyProtobuf] ReqGetMiniGameIslandBreakerRedDotData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetIslandBreakerRedDot(userId, req.IslandBreakerId);
    }

    [HttpPost("/v1/event/minigame/islandbreaker/mission/acquire")]
    public ActionResult<ResAcquireMiniGameIslandBreakerMissionReward> AcquireIslandBreakerMission(
        [FromBodyProtobuf] ReqAcquireMiniGameIslandBreakerMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AcquireIslandBreakerMission(user, req);
    }

    [HttpPost("/v1/event/minigame/islandbreaker/ranking/guild")]
    public ActionResult<ResGetMiniGameIslandBreakerGuildRanking> GetIslandBreakerGuildRanking(
        [FromBodyProtobuf] ReqGetMiniGameIslandBreakerGuildRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetIslandBreakerGuildRanking(user, req.IslandBreakerId);
    }

    [HttpPost("/v1/event/minigame/islandbreaker/result")]
    public ActionResult<ResMiniGameIslandBreakerResult> SubmitIslandBreakerResult(
        [FromBodyProtobuf] ReqMiniGameIslandBreakerResult req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SubmitIslandBreakerResult(user, req);
    }

    [HttpPost("/v1/event/minigame/islandbreaker/seen/save")]
    public ActionResult<ResSaveMiniGameIslandBreakerSeenContent> SaveIslandBreakerSeen(
        [FromBodyProtobuf] ReqSaveMiniGameIslandBreakerSeenContent req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SaveIslandBreakerSeen(userId, req);
    }

    // --- ScenarioChoice & Arcade Scenarios ---

    [HttpPost("/v1/event/minigame/scenariochoice/get")]
    public ActionResult<ResGetScenarioChoiceData> GetScenarioChoice([FromBodyProtobuf] ReqGetScenarioChoiceData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetScenarioChoice(userId, req.ScenarioChoiceManagerTableId);
    }

    [HttpPost("/v1/event/minigame/scenariochoice/reddot")]
    public ActionResult<ResExistScenarioChoiceRedDotData> GetScenarioChoiceRedDot(
        [FromBodyProtobuf] ReqExistScenarioChoiceRedDotData req)
        => new ResExistScenarioChoiceRedDotData();

    [HttpPost("/v1/event/minigame/scenariochoice/update")]
    public ActionResult<ResUpdateScenarioChoice> UpdateScenarioChoice([FromBodyProtobuf] ReqUpdateScenarioChoice req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UpdateScenarioChoice(userId, req);
    }

    [HttpPost("/v1/arcade/scenario/complete")]
    public ActionResult<ResCompleteArcadeScenario> CompleteArcadeScenario(
        [FromBodyProtobuf] ReqCompleteArcadeScenario req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompleteArcadeScenario(userId, req);
    }

    [HttpPost("/v1/arcade/scenario/get")]
    public ActionResult<ResGetArcadeScenario> GetArcadeScenario([FromBodyProtobuf] ReqGetArcadeScenario req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetArcadeScenario(userId, req.ArcadeId);
    }

    // --- BBQ ---

    [HttpPost("/v1/arcade/bbq/clear")]
    public ActionResult<ResClearArcadeBBQ> ClearBbq([FromBodyProtobuf] ReqClearArcadeBBQ req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearBbq(user, req);
    }

    [HttpPost("/v1/arcade/bbq/get")]
    public ActionResult<ResGetArcadeBBQData> GetBbq([FromBodyProtobuf] ReqGetArcadeBBQData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetBbq(userId, req.ArcadeId);
    }

    [HttpPost("/v1/arcade/bbq/getranking")]
    public ActionResult<ResGetArcadeBBQRanking> GetBbqRanking([FromBodyProtobuf] ReqGetArcadeBBQRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetBbqRanking(user, req.ArcadeId);
    }

    [HttpPost("/v1/arcade/bbq/obtainstepupreward")]
    public ActionResult<ResObtainArcadeBBQStepUpReward> ObtainBbqStepUpReward(
        [FromBodyProtobuf] ReqObtainArcadeBBQStepUpReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainBbqStepUpReward(user, req.StepUpRewardId);
    }

    [HttpPost("/v1/arcade/bbq/recordcutscene")]
    public ActionResult<ResRecordArcadeBBQCutScene> RecordBbqCutscene(
        [FromBodyProtobuf] ReqRecordArcadeBBQCutScene req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RecordBbqCutscene(userId, req.CutSceneTid);
    }

    // --- BTG ---

    [HttpPost("/v1/arcade/btg/clear")]
    public ActionResult<ResClearArcadeBtg> ClearBtg([FromBodyProtobuf] ReqClearArcadeBtg req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearBtg(user, req);
    }

    [HttpPost("/v1/arcade/btg/enter")]
    public ActionResult<ResEnterArcadeBtg> EnterBtg([FromBodyProtobuf] ReqEnterArcadeBtg req)
        => new ResEnterArcadeBtg();

    [HttpPost("/v1/arcade/btg/get")]
    public ActionResult<ResGetArcadeBtgData> GetBtg([FromBodyProtobuf] ReqGetArcadeBtgData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetBtg(userId, req.BtgId);
    }

    [HttpPost("/v1/arcade/btg/getmission")]
    public ActionResult<ResGetArcadeBtgMissionData> GetBtgMission([FromBodyProtobuf] ReqGetArcadeBtgMissionData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetBtgMission(userId, req.BtgId);
    }

    [HttpPost("/v1/arcade/btg/getranking")]
    public ActionResult<ResGetArcadeBtgRanking> GetBtgRanking([FromBodyProtobuf] ReqGetArcadeBtgRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetBtgRanking(user, req.BtgId);
    }

    [HttpPost("/v1/arcade/btg/mission/claim")]
    public ActionResult<ResClaimArcadeBtgMissionReward> ClaimBtgMission(
        [FromBodyProtobuf] ReqClaimArcadeBtgMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClaimBtgMission(user, req);
    }

    [HttpPost("/v1/arcade/btg/tutorial/set")]
    public ActionResult<ResSetArcadeBtgTutorialState> SetBtgTutorial(
        [FromBodyProtobuf] ReqSetArcadeBtgTutorialState req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetBtgTutorial(userId, req);
    }

    // --- BubbleMarch ---

    [HttpPost("/v1/arcade/bubblemarch/acquireachievementmissionreward")]
    public ActionResult<ResAcquireArcadeBubbleMarchAchievementMissionReward> AcquireBubbleMarchAchievementMissionReward(
        [FromBodyProtobuf] ReqAcquireArcadeBubbleMarchAchievementMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AcquireBubbleMarchAchievementMissionReward(user, req);
    }

    [HttpPost("/v1/arcade/bubblemarch/cleartutorial")]
    public ActionResult<ResClearArcadeBubbleMarchTutorial> ClearBubbleMarchTutorial(
        [FromBodyProtobuf] ReqClearArcadeBubbleMarchTutorial req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearBubbleMarchTutorial(userId, req);
    }

    [HttpPost("/v1/arcade/bubblemarch/enterchallengestage")]
    public ActionResult<ResEnterArcadeBubbleMarchChallengeStage> EnterBubbleMarchChallengeStage(
        [FromBodyProtobuf] ReqEnterArcadeBubbleMarchChallengeStage req)
        => new ResEnterArcadeBubbleMarchChallengeStage();

    [HttpPost("/v1/arcade/bubblemarch/entergeneralstage")]
    public ActionResult<ResEnterArcadeBubbleMarchGeneralStage> EnterBubbleMarchGeneralStage(
        [FromBodyProtobuf] ReqEnterArcadeBubbleMarchGeneralStage req)
        => new ResEnterArcadeBubbleMarchGeneralStage();

    [HttpPost("/v1/arcade/bubblemarch/finishchallengestage")]
    public ActionResult<ResFinishArcadeBubbleMarchChallengeStage> FinishBubbleMarchChallengeStage(
        [FromBodyProtobuf] ReqFinishArcadeBubbleMarchChallengeStage req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishBubbleMarchChallengeStage(user, req);
    }

    [HttpPost("/v1/arcade/bubblemarch/finishgeneralstage")]
    public ActionResult<ResFinishArcadeBubbleMarchGeneralStage> FinishBubbleMarchGeneralStage(
        [FromBodyProtobuf] ReqFinishArcadeBubbleMarchGeneralStage req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishBubbleMarchGeneralStage(userId, req);
    }

    [HttpPost("/v1/arcade/bubblemarch/get")]
    public ActionResult<ResGetArcadeBubbleMarchData> GetBubbleMarch([FromBodyProtobuf] ReqGetArcadeBubbleMarchData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetBubbleMarch(userId);
    }

    [HttpPost("/v1/arcade/bubblemarch/getmission")]
    public ActionResult<ResGetArcadeBubbleMarchMissionData> GetBubbleMarchMission(
        [FromBodyProtobuf] ReqGetArcadeBubbleMarchMissionData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetBubbleMarchMission(userId);
    }

    [HttpPost("/v1/arcade/bubblemarch/getranking")]
    public ActionResult<ResGetArcadeBubbleMarchRanking> GetBubbleMarchRanking(
        [FromBodyProtobuf] ReqGetArcadeBubbleMarchRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetBubbleMarchRanking(user);
    }

    [HttpPost("/v1/arcade/bubblemarch/resetbuffupgrade")]
    public ActionResult<ResResetArcadeBubbleMarchBuffUpgrade> ResetBubbleMarchBuffUpgrade(
        [FromBodyProtobuf] ReqResetArcadeBubbleMarchBuffUpgrade req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResetBubbleMarchBuffUpgrade(userId);
    }

    [HttpPost("/v1/arcade/bubblemarch/resetcharacterunlockupgrade")]
    public ActionResult<ResResetArcadeBubbleMarchCharacterUnlockUpgrade> ResetBubbleMarchCharacterUnlockUpgrade(
        [FromBodyProtobuf] ReqResetArcadeBubbleMarchCharacterUnlockUpgrade req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResetBubbleMarchCharacterUnlockUpgrade(userId);
    }

    [HttpPost("/v1/arcade/bubblemarch/setbuffupgrade")]
    public ActionResult<ResSetArcadeBubbleMarchBuffUpgrade> SetBubbleMarchBuffUpgrade(
        [FromBodyProtobuf] ReqSetArcadeBubbleMarchBuffUpgrade req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetBubbleMarchBuffUpgrade(userId, req.UpgradeId);
    }

    [HttpPost("/v1/arcade/bubblemarch/setcharacterunlockupgrade")]
    public ActionResult<ResSetArcadeBubbleMarchCharacterUnlockUpgrade> SetBubbleMarchCharacterUnlockUpgrade(
        [FromBodyProtobuf] ReqSetArcadeBubbleMarchCharacterUnlockUpgrade req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetBubbleMarchCharacterUnlockUpgrade(userId, req.UpgradeId);
    }

    [HttpPost("/v1/arcade/bubblemarch/setoption")]
    public ActionResult<ResSetArcadeBubbleMarchOption> SetBubbleMarchOption(
        [FromBodyProtobuf] ReqSetArcadeBubbleMarchOption req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetBubbleMarchOption(userId, req.LevelHideOptionIsActive);
    }

    // --- DessertRush ---

    [HttpPost("/v1/arcade/dessertrush/clear")]
    public ActionResult<ResClearArcadeDessertRush> ClearDessertRush([FromBodyProtobuf] ReqClearArcadeDessertRush req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearDessertRush(user, req);
    }

    [HttpPost("/v1/arcade/dessertrush/get")]
    public ActionResult<ResGetArcadeDessertRushData> GetDessertRush([FromBodyProtobuf] ReqGetArcadeDessertRushData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDessertRush(userId, req.ArcadeId);
    }

    [HttpPost("/v1/arcade/dessertrush/getranking")]
    public ActionResult<ResGetArcadeDessertRushRanking> GetDessertRushRanking(
        [FromBodyProtobuf] ReqGetArcadeDessertRushRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDessertRushRanking(user, req.ArcadeId);
    }

    [HttpPost("/v1/arcade/dessertrush/obtainstepupreward")]
    public ActionResult<ResObtainArcadeDessertRushStepUpReward> ObtainDessertRushStepUpReward(
        [FromBodyProtobuf] ReqObtainArcadeDessertRushStepUpReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainDessertRushStepUpReward(user, req.StepUpRewardId);
    }

    // --- DragonDungeonRun ---

    [HttpPost("/v1/arcade/ddr/album")]
    public ActionResult<ResGetArcadeDragonDungeonRunAlbum> GetDdrAlbum(
        [FromBodyProtobuf] ReqGetArcadeDragonDungeonRunAlbum req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDdrAlbum(userId);
    }

    [HttpPost("/v1/arcade/ddr/character/checked")]
    public ActionResult<ResSetCheckedArcadeDragonDungeonRunCharacter> SetDdrCharacterChecked(
        [FromBodyProtobuf] ReqSetCheckedArcadeDragonDungeonRunCharacter req)
        => new ResSetCheckedArcadeDragonDungeonRunCharacter();

    [HttpPost("/v1/arcade/ddr/lobby")]
    public ActionResult<ResEnterArcadeDragonDungeonRunLobby> EnterDdrLobby(
        [FromBodyProtobuf] ReqEnterArcadeDragonDungeonRunLobby req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterDdrLobby(user);
    }

    [HttpPost("/v1/arcade/ddr/mission")]
    public ActionResult<ResGetArcadeDragonDungeonRunMissionProgress> GetDdrMission(
        [FromBodyProtobuf] ReqGetArcadeDragonDungeonRunMissionProgress req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDdrMission(userId);
    }

    [HttpPost("/v1/arcade/ddr/mission/acquire")]
    public ActionResult<ResAcquireArcadeDragonDungeonRunMissionReward> AcquireDdrMission(
        [FromBodyProtobuf] ReqAcquireArcadeDragonDungeonRunMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AcquireDdrMission(user, req);
    }

    [HttpPost("/v1/arcade/ddr/stage/complete")]
    public ActionResult<ResCompleteArcadeDragonDungeonRunStage> CompleteDdrStage(
        [FromBodyProtobuf] ReqCompleteArcadeDragonDungeonRunStage req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompleteDdrStage(user, req);
    }

    [HttpPost("/v1/arcade/ddr/stage/enter")]
    public ActionResult<ResEnterArcadeDragonDungeonRunStage> EnterDdrStage(
        [FromBodyProtobuf] ReqEnterArcadeDragonDungeonRunStage req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterDdrStage(userId, req);
    }

    [HttpPost("/v1/arcade/ddr/statistics")]
    public ActionResult<ResGetArcadeDragonDungeonRunStatistics> GetDdrStatistics(
        [FromBodyProtobuf] ReqGetArcadeDragonDungeonRunStatistics req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDdrStatistics(userId);
    }

    [HttpPost("/v1/arcade/ddr/union-ranking")]
    public ActionResult<ResGetArcadeDragonDungeonRunUnionRanking> GetDdrUnionRanking(
        [FromBodyProtobuf] ReqGetArcadeDragonDungeonRunUnionRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDdrUnionRanking(user);
    }

    [HttpPost("/v1/arcade/ddr/unlock-state/get")]
    public ActionResult<ResGetArcadeDragonDungeonRunUnlockState> GetDdrUnlockState(
        [FromBodyProtobuf] ReqGetArcadeDragonDungeonRunUnlockState req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetDdrUnlockState(userId);
    }

    // --- InTheMirror ---

    [HttpPost("/v1/arcade/mvg/log/collectable")]
    public ActionResult<ResAcquireArcadeMvgCollectable> AcquireMvgCollectable(
        [FromBodyProtobuf] ReqAcquireArcadeMvgCollectable req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AcquireMvgCollectable(userId, req.CollectableId);
    }

    [HttpPost("/v1/arcade/mvg/log/shop")]
    public ActionResult<ResBuyFromArcadeMvgShop> BuyFromMvgShop([FromBodyProtobuf] ReqBuyFromArcadeMvgShop req)
        => new ResBuyFromArcadeMvgShop();

    [HttpPost("/v1/arcade/mvg/get")]
    public ActionResult<ResGetArcadeMvgData> GetMvgData([FromBodyProtobuf] ReqGetArcadeMvgData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetMvgData(userId);
    }

    [HttpPost("/v1/arcade/mvg/log/interaction")]
    public ActionResult<ResLogArcadeMvgInteraction> LogMvgInteraction(
        [FromBodyProtobuf] ReqLogArcadeMvgInteraction req)
        => new ResLogArcadeMvgInteraction();

    [HttpPost("/v1/arcade/mvg/log/module")]
    public ActionResult<ResEquipArcadeMvgModule> LogMvgModule([FromBodyProtobuf] ReqEquipArcadeMvgModule req)
        => new ResEquipArcadeMvgModule();

    [HttpPost("/v1/arcade/mvg/log/module/limit")]
    public ActionResult<ResIncreaseArcadeMvgModuleLimit> LogMvgModuleLimit(
        [FromBodyProtobuf] ReqIncreaseArcadeMvgModuleLimit req)
        => new ResIncreaseArcadeMvgModuleLimit();

    [HttpPost("/v1/arcade/mvg/log/state")]
    public ActionResult<ResLogArcadeMvgState> LogMvgState([FromBodyProtobuf] ReqLogArcadeMvgState req)
        => new ResLogArcadeMvgState();

    [HttpPost("/v1/arcade/mvg/log/unlock")]
    public ActionResult<ResUnlockArcadeMvg> LogMvgUnlock([FromBodyProtobuf] ReqUnlockArcadeMvg req)
        => new ResUnlockArcadeMvg();

    [HttpPost("/v1/arcade/mvg/log/upgrade")]
    public ActionResult<ResBuyFromArcadeMvgUpgradeShop> LogMvgUpgrade(
        [FromBodyProtobuf] ReqBuyFromArcadeMvgUpgradeShop req)
        => new ResBuyFromArcadeMvgUpgradeShop();

    [HttpPost("/v1/arcade/mvg/save")]
    public ActionResult<ResSaveArcadeMvgData> SaveMvgData([FromBodyProtobuf] ReqSaveArcadeMvgData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SaveMvgData(userId, req.Data);
    }

    [HttpPost("/v1/arcade/mvg/obtainachievementmissionreward")]
    public ActionResult<ResObtainArcadeMvgAchievementMissionReward> ObtainMvgAchievementMissionReward(
        [FromBodyProtobuf] ReqObtainArcadeMvgAchievementMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainMvgAchievementMissionReward(user, req);
    }

    [HttpPost("/v1/arcade/mvg/obtainquestreward")]
    public ActionResult<ResObtainArcadeMvgQuestReward> ObtainMvgQuestReward(
        [FromBodyProtobuf] ReqObtainArcadeMvgQuestReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainMvgQuestReward(user, req);
    }

    [HttpPost("/v1/arcade/mvg/newgame")]
    public ActionResult<ResStartArcadeMvgNewGame> StartMvgNewGame([FromBodyProtobuf] ReqStartArcadeMvgNewGame req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.StartMvgNewGame(userId);
    }

    [HttpPost("/v1/arcade/mvg/newgameplus")]
    public ActionResult<ResStartArcadeMvgNewGamePlus> StartMvgNewGamePlus(
        [FromBodyProtobuf] ReqStartArcadeMvgNewGamePlus req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.StartMvgNewGamePlus(userId, req.ProgressJson);
    }

    // --- IslandAdventure ---

    [HttpPost("/v1/event/minigame/islandadventure/get/currency")]
    public ActionResult<ResGetMiniGameIslandAdventureCurrency> GetIslandAdventureCurrency(
        [FromBodyProtobuf] ReqGetMiniGameIslandAdventureCurrency req)
        => new ResGetMiniGameIslandAdventureCurrency { Currency = 90000 };

    [HttpPost("/v1/event/minigame/islandadventure/get/inventory")]
    public ActionResult<ResGetMiniGameIslandAdventureInventory> GetIslandAdventureInventory(
        [FromBodyProtobuf] ReqGetMiniGameIslandAdventureInventory req)
        => new ResGetMiniGameIslandAdventureInventory();

    [HttpPost("/v1/event/minigame/islandadventure/get/fish/spotcount")]
    public ActionResult<ResMiniGameIslandAdventureFishingSpotCountHistory> GetIslandAdventureFishSpotCount(
        [FromBodyProtobuf] ReqMiniGameIslandAdventureFishingSpotCountHistory req)
        => new ResMiniGameIslandAdventureFishingSpotCountHistory();

    [HttpPost("/v1/event/minigame/islandadventure/get/fishing/stepupreward")]
    public ActionResult<ResGetIslandAdventureFishingStepUpRewardStatus> GetIslandAdventureFishingStepUpReward(
        [FromBodyProtobuf] ReqGetIslandAdventureFishingStepUpRewardStatus req)
        => new ResGetIslandAdventureFishingStepUpRewardStatus();

    [HttpPost("/v1/event/minigame/islandadventure/get/photo/stepupreward")]
    public ActionResult<ResGetIslandAdventurePhotoStepUpRewardStatus> GetIslandAdventurePhotoStepUpReward(
        [FromBodyProtobuf] ReqGetIslandAdventurePhotoStepUpRewardStatus req)
        => new ResGetIslandAdventurePhotoStepUpRewardStatus();

    [HttpPost("/v1/event/minigame/islandadventure/list/mission")]
    public ActionResult<ResGetIslandAdventureMissionProgress> ListIslandAdventureMission(
        [FromBodyProtobuf] ReqGetIslandAdventureMissionProgress req)
        => new ResGetIslandAdventureMissionProgress();

    [HttpPost("/v1/event/minigame/islandadventure/get/photo/album")]
    public ActionResult<ResMiniGameIslandAdventurePhotoAlbum> GetIslandAdventurePhotoAlbum(
        [FromBodyProtobuf] ReqMiniGameIslandAdventurePhotoAlbum req)
        => new ResMiniGameIslandAdventurePhotoAlbum();
}
