using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class MinigameArcadeController(IUserService userService, MinigameArcadeService service) : ControllerBase
{
    // --- nksv2 ---

    [HttpPost("/v1/arcade/nksv2/accumulaterankingscore")]
    public ActionResult<ResAccumulateMiniGameNKSV2RankingScore> AccumulateNksv2RankingScore(
        [FromBodyProtobuf] ReqAccumulateMiniGameNKSV2RankingScore req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AccumulateNksv2RankingScore(user, req);
    }

    [HttpPost("/v1/arcade/nksv2/completemission")]
    public ActionResult<ResCompleteNKSV2Mission> CompleteNksv2Mission(
        [FromBodyProtobuf] ReqCompleteNKSV2Mission req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompleteNksv2Mission(user, req);
    }

    [HttpPost("/v1/arcade/nksv2/enterstage")]
    public ActionResult<ResEnterMiniGameNKSV2Stage> EnterNksv2Stage(
        [FromBodyProtobuf] ReqEnterMiniGameNKSV2Stage req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterNksv2Stage(user);
    }

    [HttpPost("/v1/arcade/nksv2/get")]
    public ActionResult<ResGetMiniGameNKSV2Data> GetNksv2([FromBodyProtobuf] ReqGetMiniGameNKSV2Data req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetNksv2(userId, req);
    }

    [HttpPost("/v1/arcade/nksv2/getranking")]
    public ActionResult<ResGetMiniGameNKSV2Ranking> GetNksv2Ranking(
        [FromBodyProtobuf] ReqGetMiniGameNKSV2Ranking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetNksv2Ranking(user, req);
    }

    [HttpPost("/v1/arcade/nksv2/recordlog/skilltreeupgrade")]
    public ActionResult<ResRecordMiniGameNKSV2SkillTreeUpgradeLog> RecordNksv2SkillTreeUpgradeLog(
        [FromBodyProtobuf] ReqRecordMiniGameNKSV2SkillTreeUpgradeLog req)
        => new ResRecordMiniGameNKSV2SkillTreeUpgradeLog();

    [HttpPost("/v1/arcade/nksv2/recordlog/stageplay")]
    public ActionResult<ResRecordMiniGameNKSV2StagePlayLog> RecordNksv2StagePlayLog(
        [FromBodyProtobuf] ReqRecordMiniGameNKSV2StagePlayLog req)
        => new ResRecordMiniGameNKSV2StagePlayLog();

    [HttpPost("/v1/arcade/nksv2/recordlog/unlockcharacter")]
    public ActionResult<ResRecordMiniGameNKSV2CharacterUnlockLog> RecordNksv2CharacterUnlockLog(
        [FromBodyProtobuf] ReqRecordMiniGameNKSV2CharacterUnlockLog req)
        => new ResRecordMiniGameNKSV2CharacterUnlockLog();

    [HttpPost("/v1/arcade/nksv2/scenario/complete")]
    public ActionResult<ResCompleteNKSV2Scenario> CompleteNksv2Scenario(
        [FromBodyProtobuf] ReqCompleteNKSV2Scenario req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompleteNksv2Scenario(userId, req);
    }

    [HttpPost("/v1/arcade/nksv2/scenario/get")]
    public ActionResult<ResGetNKSV2Scenario> GetNksv2Scenario([FromBodyProtobuf] ReqGetNKSV2Scenario req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetNksv2Scenario(userId, req);
    }

    [HttpPost("/v1/arcade/nksv2/updatemissionprogress")]
    public ActionResult<ResUpdateNKSV2MissionProgress> UpdateNksv2MissionProgress(
        [FromBodyProtobuf] ReqUpdateNKSV2MissionProgress req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UpdateNksv2MissionProgress(userId, req);
    }

    [HttpPost("/v1/arcade/nksv2/uploadprogress")]
    public ActionResult<ResUploadMiniGameNKSV2Progress> UploadNksv2Progress(
        [FromBodyProtobuf] ReqUploadMiniGameNKSV2Progress req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UploadNksv2Progress(userId, req);
    }

    // --- PirateCafe ---

    [HttpPost("/v1/arcade/PirateCafe/clear")]
    public ActionResult<ResClearArcadePirateCafe> ClearPirateCafe(
        [FromBodyProtobuf] ReqClearArcadePirateCafe req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearPirateCafe(user, req);
    }

    [HttpPost("/v1/arcade/PirateCafe/get")]
    public ActionResult<ResGetArcadePirateCafeData> GetPirateCafe(
        [FromBodyProtobuf] ReqGetArcadePirateCafeData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetPirateCafe(userId, req);
    }

    [HttpPost("/v1/arcade/PirateCafe/getranking")]
    public ActionResult<ResGetArcadePirateCafeRanking> GetPirateCafeRanking(
        [FromBodyProtobuf] ReqGetArcadePirateCafeRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetPirateCafeRanking(user, req);
    }

    [HttpPost("/v1/arcade/PirateCafe/levelup")]
    public ActionResult<ResLevelUpArcadePirateCafeSkill> LevelUpPirateCafe(
        [FromBodyProtobuf] ReqLevelUpArcadePirateCafeSkill req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.LevelUpPirateCafe(userId, req);
    }

    [HttpPost("/v1/arcade/PirateCafe/obtainmissionreward")]
    public ActionResult<ResObtainArcadePirateCafeMissionReward> ObtainPirateCafeMissionReward(
        [FromBodyProtobuf] ReqObtainArcadePirateCafeMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainPirateCafeMissionReward(user, req);
    }

    // --- PlaySoda ---

    [HttpPost("/v1/arcade/enterlog")]
    public ActionResult<ResEnterArcadeLog> EnterArcadeLog([FromBodyProtobuf] ReqEnterArcadeLog req)
        => new ResEnterArcadeLog();

    [HttpPost("/v1/arcade/play-soda/challenge/enter")]
    public ActionResult<ResEnterArcadePlaySodaChallengeStage> EnterPlaySodaChallenge(
        [FromBodyProtobuf] ReqEnterArcadePlaySodaChallengeStage req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterPlaySodaChallenge(userId, req);
    }

    [HttpPost("/v1/arcade/play-soda/challenge/finish/catchcoin")]
    public ActionResult<ResFinishArcadePlaySodaCatchCoinChallenge> FinishPlaySodaCatchCoin(
        [FromBodyProtobuf] ReqFinishArcadePlaySodaCatchCoinChallenge req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishPlaySodaCatchCoin(userId, req);
    }

    [HttpPost("/v1/arcade/play-soda/challenge/finish/climbing")]
    public ActionResult<ResFinishArcadePlaySodaClimbingChallenge> FinishPlaySodaClimbing(
        [FromBodyProtobuf] ReqFinishArcadePlaySodaClimbingChallenge req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishPlaySodaClimbing(userId, req);
    }

    [HttpPost("/v1/arcade/play-soda/challenge/finish/running")]
    public ActionResult<ResFinishArcadePlaySodaRunningChallenge> FinishPlaySodaRunning(
        [FromBodyProtobuf] ReqFinishArcadePlaySodaRunningChallenge req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishPlaySodaRunning(userId, req);
    }

    [HttpPost("/v1/arcade/play-soda/challenge/finish/smash")]
    public ActionResult<ResFinishArcadePlaySodaSmashChallenge> FinishPlaySodaSmash(
        [FromBodyProtobuf] ReqFinishArcadePlaySodaSmashChallenge req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishPlaySodaSmash(userId, req);
    }

    [HttpPost("/v1/arcade/play-soda/challenge/finish/stackup")]
    public ActionResult<ResFinishArcadePlaySodaStackUpChallenge> FinishPlaySodaStackUp(
        [FromBodyProtobuf] ReqFinishArcadePlaySodaStackUpChallenge req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishPlaySodaStackUp(userId, req);
    }

    [HttpPost("/v1/arcade/play-soda/challenge/info")]
    public ActionResult<ResGetArcadePlaySodaChallengeModeInfo> GetPlaySodaChallengeInfo(
        [FromBodyProtobuf] ReqGetArcadePlaySodaChallengeModeInfo req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetPlaySodaChallengeInfo(user);
    }

    [HttpPost("/v1/arcade/play-soda/challenge/obtain-point-reward")]
    public ActionResult<ResObtainArcadePlaySodaPointReward> ObtainPlaySodaPointReward(
        [FromBodyProtobuf] ReqObtainArcadePlaySodaPointReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainPlaySodaPointReward(user, req);
    }

    // --- RebuildEden ---

    [HttpPost("/v1/arcade/rebuildeden/get")]
    public ActionResult<ResGetArcadeRebuildEdenData> GetRebuildEden(
        [FromBodyProtobuf] ReqGetArcadeRebuildEdenData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetRebuildEden(userId);
    }

    [HttpPost("/v1/arcade/rebuildeden/log/buildbuilding")]
    public ActionResult<ResLogBuildArcadeRebuildEdenBuilding> LogRebuildEdenBuildBuilding(
        [FromBodyProtobuf] ReqLogBuildArcadeRebuildEdenBuilding req)
        => new ResLogBuildArcadeRebuildEdenBuilding();

    [HttpPost("/v1/arcade/rebuildeden/log/completequest")]
    public ActionResult<ResLogCompleteArcadeRebuildEdenQuest> LogRebuildEdenCompleteQuest(
        [FromBodyProtobuf] ReqLogCompleteArcadeRebuildEdenQuest req)
        => new ResLogCompleteArcadeRebuildEdenQuest();

    [HttpPost("/v1/arcade/rebuildeden/log/destroybuilding")]
    public ActionResult<ResLogDestroyArcadeRebuildEdenBuilding> LogRebuildEdenDestroyBuilding(
        [FromBodyProtobuf] ReqLogDestroyArcadeRebuildEdenBuilding req)
        => new ResLogDestroyArcadeRebuildEdenBuilding();

    [HttpPost("/v1/arcade/rebuildeden/log/dispatch")]
    public ActionResult<ResLogArcadeRebuildEdenDispatch> LogRebuildEdenDispatch(
        [FromBodyProtobuf] ReqLogArcadeRebuildEdenDispatch req)
        => new ResLogArcadeRebuildEdenDispatch();

    [HttpPost("/v1/arcade/rebuildeden/log/fireunit")]
    public ActionResult<ResLogFireArcadeRebuildEdenUnit> LogRebuildEdenFireUnit(
        [FromBodyProtobuf] ReqLogFireArcadeRebuildEdenUnit req)
        => new ResLogFireArcadeRebuildEdenUnit();

    [HttpPost("/v1/arcade/rebuildeden/log/hireunit")]
    public ActionResult<ResLogHireArcadeRebuildEdenUnit> LogRebuildEdenHireUnit(
        [FromBodyProtobuf] ReqLogHireArcadeRebuildEdenUnit req)
        => new ResLogHireArcadeRebuildEdenUnit();

    [HttpPost("/v1/arcade/rebuildeden/log/resetbuildingperk")]
    public ActionResult<ResLogResetArcadeRebuildEdenBuildingPerk> LogRebuildEdenResetBuildingPerk(
        [FromBodyProtobuf] ReqLogResetArcadeRebuildEdenBuildingPerk req)
        => new ResLogResetArcadeRebuildEdenBuildingPerk();

    [HttpPost("/v1/arcade/rebuildeden/log/selectbuildingperk")]
    public ActionResult<ResLogSelectArcadeRebuildEdenBuildingPerk> LogRebuildEdenSelectBuildingPerk(
        [FromBodyProtobuf] ReqLogSelectArcadeRebuildEdenBuildingPerk req)
        => new ResLogSelectArcadeRebuildEdenBuildingPerk();

    [HttpPost("/v1/arcade/rebuildeden/log/setunittobuilding")]
    public ActionResult<ResLogSetArcadeRebuildEdenUnitToBuilding> LogRebuildEdenSetUnitToBuilding(
        [FromBodyProtobuf] ReqLogSetArcadeRebuildEdenUnitToBuilding req)
        => new ResLogSetArcadeRebuildEdenUnitToBuilding();

    [HttpPost("/v1/arcade/rebuildeden/log/summary")]
    public ActionResult<ResLogArcadeRebuildEdenSummary> LogRebuildEdenSummary(
        [FromBodyProtobuf] ReqLogArcadeRebuildEdenSummary req)
        => new ResLogArcadeRebuildEdenSummary();

    [HttpPost("/v1/arcade/rebuildeden/log/upgradebuilding")]
    public ActionResult<ResLogUpgradeArcadeRebuildEdenBuilding> LogRebuildEdenUpgradeBuilding(
        [FromBodyProtobuf] ReqLogUpgradeArcadeRebuildEdenBuilding req)
        => new ResLogUpgradeArcadeRebuildEdenBuilding();

    [HttpPost("/v1/arcade/rebuildeden/obtainachievementmissionreward")]
    public ActionResult<ResObtainRebuildEdenAchievementMissionReward> ObtainRebuildEdenAchievementMissionReward(
        [FromBodyProtobuf] ReqObtainRebuildEdenAchievementMissionReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainRebuildEdenAchievementMissionReward(user, req);
    }

    [HttpPost("/v1/arcade/rebuildeden/save")]
    public ActionResult<ResSaveRebuildEdenData> SaveRebuildEden([FromBodyProtobuf] ReqSaveRebuildEdenData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SaveRebuildEden(userId, req);
    }

    // --- SortOut ---

    [HttpPost("/v1/arcade/sortout/completemission")]
    public ActionResult<ResCompleteArcadeSortOutMission> CompleteSortOutMission(
        [FromBodyProtobuf] ReqCompleteArcadeSortOutMission req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompleteSortOutMission(user, req);
    }

    [HttpPost("/v1/arcade/sortout/get")]
    public ActionResult<ResGetArcadeSortOut> GetSortOut([FromBodyProtobuf] ReqGetArcadeSortOut req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetSortOut(userId);
    }

    [HttpPost("/v1/arcade/sortout/getranking")]
    public ActionResult<ResGetArcadeSortOutRanking> GetSortOutRanking(
        [FromBodyProtobuf] ReqGetArcadeSortOutRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetSortOutRanking(user, req);
    }

    [HttpPost("/v1/arcade/sortout/result")]
    public ActionResult<ResArcadeSortOutResult> SubmitSortOutResult([FromBodyProtobuf] ReqArcadeSortOutResult req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SubmitSortOutResult(user, req);
    }

    [HttpPost("/v1/arcade/sortout/start")]
    public ActionResult<ResArcadeSortOutStart> StartSortOut([FromBodyProtobuf] ReqArcadeSortOutStart req)
        => new ResArcadeSortOutStart();

    // --- StellarBlade ---

    [HttpPost("/v1/arcade/stellar-blade/achievement/acquire")]
    public ActionResult<ResArcadeAcquireStellarBladeAchievementReward> AcquireStellarBladeAchievement(
        [FromBodyProtobuf] ReqArcadeAcquireStellarBladeAchievementReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AcquireStellarBladeAchievement(user, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/buy")]
    public ActionResult<ResArcadeBuyStellarBladeShopItem> BuyStellarBladeShopItem(
        [FromBodyProtobuf] ReqArcadeBuyStellarBladeShopItem req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.BuyStellarBladeShopItem(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/character/enhance")]
    public ActionResult<ResArcadeEnhanceStellarBladeCharacter> EnhanceStellarBladeCharacter(
        [FromBodyProtobuf] ReqArcadeEnhanceStellarBladeCharacter req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnhanceStellarBladeCharacter(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/default-attack-skill/set")]
    public ActionResult<ResArcadeSetStellarBladeDefaultAttackSkill> SetStellarBladeDefaultAttackSkill(
        [FromBodyProtobuf] ReqArcadeSetStellarBladeDefaultAttackSkill req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetStellarBladeDefaultAttackSkill(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/exo-spine/equip")]
    public ActionResult<ResArcadeEquipStellarBladeExoSpine> EquipStellarBladeExoSpine(
        [FromBodyProtobuf] ReqArcadeEquipStellarBladeExoSpine req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EquipStellarBladeExoSpine(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/gear/equip")]
    public ActionResult<ResArcadeEquipStellarBladeGear> EquipStellarBladeGear(
        [FromBodyProtobuf] ReqArcadeEquipStellarBladeGear req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EquipStellarBladeGear(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/mission/get")]
    public ActionResult<ResArcadeGetStellarBladeMissionData> GetStellarBladeMission(
        [FromBodyProtobuf] ReqArcadeGetStellarBladeMissionData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetStellarBladeMission(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/record/tutorial")]
    public ActionResult<ResArcadeRecordStellarBladeTutorial> RecordStellarBladeTutorial(
        [FromBodyProtobuf] ReqArcadeRecordStellarBladeTutorial req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RecordStellarBladeTutorial(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/set-first/tutorial")]
    public ActionResult<ResArcadeRecordStellarBladeTutorial> SetStellarBladeFirstTutorial(
        [FromBodyProtobuf] ReqArcadeRecordStellarBladeTutorial req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RecordStellarBladeTutorial(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/skill/learn")]
    public ActionResult<ResArcadeLearnStellarBladeSkill> LearnStellarBladeSkill(
        [FromBodyProtobuf] ReqArcadeLearnStellarBladeSkill req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.LearnStellarBladeSkill(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/skill/reset")]
    public ActionResult<ResArcadeResetStellarBladeSkill> ResetStellarBladeSkill(
        [FromBodyProtobuf] ReqArcadeResetStellarBladeSkill req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResetStellarBladeSkill(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/stage/clear")]
    public ActionResult<ResArcadeClearStellarBladeStage> ClearStellarBladeStage(
        [FromBodyProtobuf] ReqArcadeClearStellarBladeStage req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearStellarBladeStage(user, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/stage/enter")]
    public ActionResult<ResArcadeEnterStellarBladeStage> EnterStellarBladeStage(
        [FromBodyProtobuf] ReqArcadeEnterStellarBladeStage req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterStellarBladeStage(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/stage/failure")]
    public ActionResult<ResArcadeFailureStellarBladeStage> FailStellarBladeStage(
        [FromBodyProtobuf] ReqArcadeFailureStellarBladeStage req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FailStellarBladeStage(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/statistics/get")]
    public ActionResult<ResArcadeGetStellarBladeStatistics> GetStellarBladeStatistics(
        [FromBodyProtobuf] ReqArcadeGetStellarBladeStatistics req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetStellarBladeStatistics(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/title/enter")]
    public ActionResult<ResArcadeEnterStellarBladeTitle> EnterStellarBladeTitle(
        [FromBodyProtobuf] ReqArcadeEnterStellarBladeTitle req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterStellarBladeTitle(userId, req);
    }

    [HttpPost("/v1/arcade/stellar-blade/union-ranking")]
    public ActionResult<ResArcadeGetStellarBladeUnionRanking> GetStellarBladeUnionRanking(
        [FromBodyProtobuf] ReqArcadeGetStellarBladeUnionRanking req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetStellarBladeUnionRanking(userId, req);
    }

    // --- TowerDefense ---

    [HttpPost("/v1/arcade/towerdefense/cleartutorial")]
    public ActionResult<ResClearArcadeTowerDefenseTutorial> ClearTowerDefenseTutorial(
        [FromBodyProtobuf] ReqClearArcadeTowerDefenseTutorial req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearTowerDefenseTutorial(userId, req);
    }

    [HttpPost("/v1/arcade/towerdefense/completemission")]
    public ActionResult<ResCompleteArcadeTowerDefenseMission> CompleteTowerDefenseMission(
        [FromBodyProtobuf] ReqCompleteArcadeTowerDefenseMission req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompleteTowerDefenseMission(user, req);
    }

    [HttpPost("/v1/arcade/towerdefense/enterstage")]
    public ActionResult<ResEnterArcadeTowerDefenseStage> EnterTowerDefenseStage(
        [FromBodyProtobuf] ReqEnterArcadeTowerDefenseStage req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterTowerDefenseStage(user, req);
    }

    [HttpPost("/v1/arcade/towerdefense/finish")]
    public ActionResult<ResFinishArcadeTowerDefense> FinishTowerDefense(
        [FromBodyProtobuf] ReqFinishArcadeTowerDefense req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishTowerDefense(user, req);
    }

    [HttpPost("/v1/arcade/towerdefense/get")]
    public ActionResult<ResGetArcadeTowerDefenseData> GetTowerDefense(
        [FromBodyProtobuf] ReqGetArcadeTowerDefenseData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetTowerDefense(userId, req);
    }

    [HttpPost("/v1/arcade/towerdefense/getranking")]
    public ActionResult<ResGetArcadeTowerDefenseRanking> GetTowerDefenseRanking(
        [FromBodyProtobuf] ReqGetArcadeTowerDefenseRanking req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetTowerDefenseRanking(user, req);
    }

    [HttpPost("/v1/arcade/towerdefense/resetupgrade")]
    public ActionResult<ResResetArcadeTowerDefenseUpgrade> ResetTowerDefenseUpgrade(
        [FromBodyProtobuf] ReqResetArcadeTowerDefenseUpgrade req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResetTowerDefenseUpgrade(userId, req);
    }

    [HttpPost("/v1/arcade/towerdefense/updatemissionprogress")]
    public ActionResult<ResUpdateArcadeTowerDefenseMissionProgress> UpdateTowerDefenseMissionProgress(
        [FromBodyProtobuf] ReqUpdateArcadeTowerDefenseMissionProgress req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UpdateTowerDefenseMissionProgress(userId, req);
    }

    [HttpPost("/v1/arcade/towerdefense/upgrade")]
    public ActionResult<ResSetArcadeTowerDefenseUpgrade> UpgradeTowerDefense(
        [FromBodyProtobuf] ReqSetArcadeTowerDefenseUpgrade req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UpgradeTowerDefense(userId, req);
    }

    // --- TTS ---

    [HttpPost("/v1/MiniGame/TTS/Enter")]
    public ActionResult<ResEnterMiniGameTtsTitle> EnterTts([FromBodyProtobuf] ReqEnterMiniGameTtsTitle req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterTts(user, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Mission/Complete")]
    public ActionResult<ResCompleteMiniGameTtsMission> CompleteTtsMission(
        [FromBodyProtobuf] ReqCompleteMiniGameTtsMission req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompleteTtsMission(user, req);
    }

    [HttpPost("/v1/MiniGame/TTS/PlayTime/Save")]
    public ActionResult<ResSaveMiniGameTtsPlayTime> SaveTtsPlayTime(
        [FromBodyProtobuf] ReqSaveMiniGameTtsPlayTime req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SaveTtsPlayTime(userId, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Profile/Get")]
    public ActionResult<ResGetMiniGameTtsProfile> GetTtsProfile(
        [FromBodyProtobuf] ReqGetMiniGameTtsProfile req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetTtsProfile(userId, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Expert/PopUp/Select/Proceed")]
    public ActionResult<ResSelectProceedOnMiniGameTtsExpertAlertPopUp> SelectTtsProceed(
        [FromBodyProtobuf] ReqSelectProceedOnMiniGameTtsExpertAlertPopUp req)
        => new ResSelectProceedOnMiniGameTtsExpertAlertPopUp();

    [HttpPost("/v1/MiniGame/TTS/Shop/Enter")]
    public ActionResult<ResEnterMiniGameTtsPackageShop> EnterTtsShop(
        [FromBodyProtobuf] ReqEnterMiniGameTtsPackageShop req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterTtsShop(userId, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Skin/Buy")]
    public ActionResult<ResBuyMiniGameTtsSkin> BuyTtsSkin([FromBodyProtobuf] ReqBuyMiniGameTtsSkin req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.BuyTtsSkin(user, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Skin/Enter")]
    public ActionResult<ResEnterMiniGameTtsSkin> EnterTtsSkin([FromBodyProtobuf] ReqEnterMiniGameTtsSkin req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.EnterTtsSkin(userId, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Skin/Set")]
    public ActionResult<ResSetMiniGameTtsSkin> SetTtsSkin([FromBodyProtobuf] ReqSetMiniGameTtsSkin req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetTtsSkin(userId, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Song/Get")]
    public ActionResult<ResGetMiniGameTtsSongData> GetTtsSong([FromBodyProtobuf] ReqGetMiniGameTtsSongData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetTtsSong(user, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Ranking/Song/Get")]
    public ActionResult<ResGetMiniGameTtsSongRanking> GetTtsSongRanking(
        [FromBodyProtobuf] ReqGetMiniGameTtsSongRanking req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetTtsSongRanking(userId, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Ranking/Total/Get")]
    public ActionResult<ResGetMiniGameTtsTotalRanking> GetTtsTotalRanking(
        [FromBodyProtobuf] ReqGetMiniGameTtsTotalRanking req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetTtsTotalRanking(userId, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Finish")]
    public ActionResult<ResFinishMiniGameTtsPlay> FinishTts([FromBodyProtobuf] ReqFinishMiniGameTtsPlay req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishTts(user, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Pause")]
    public ActionResult<ResPauseMiniGameTtsPlay> PauseTts([FromBodyProtobuf] ReqPauseMiniGameTtsPlay req)
        => new ResPauseMiniGameTtsPlay();

    [HttpPost("/v1/MiniGame/TTS/Resume")]
    public ActionResult<ResResumeMiniGameTtsPlay> ResumeTts([FromBodyProtobuf] ReqResumeMiniGameTtsPlay req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResumeTts(userId, req);
    }

    [HttpPost("/v1/MiniGame/TTS/Start")]
    public ActionResult<ResStartMiniGameTtsPlay> StartTts([FromBodyProtobuf] ReqStartMiniGameTtsPlay req)
        => new ResStartMiniGameTtsPlay();

    [HttpPost("/v1/MiniGame/TTS/Tutorial/Finish")]
    public ActionResult<ResFinishMiniGameTtsTutorial> FinishTtsTutorial(
        [FromBodyProtobuf] ReqFinishMiniGameTtsTutorial req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.FinishTtsTutorial(userId, req);
    }
}
