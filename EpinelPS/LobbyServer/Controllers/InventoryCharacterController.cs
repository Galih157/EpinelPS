using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class InventoryCharacterController(
    IUserService userService,
    InventoryCharacterService service) : ControllerBase
{
    // --- Character ---

    [HttpPost("/v1/character/costume/get")]
    public ActionResult<ResGetCharacterCostumeData> GetCharacterCostume(
        [FromBodyProtobuf] ReqGetCharacterCostumeData req)
        => service.GetCharacterCostume();

    [HttpPost("/v1/character/get")]
    public ActionResult<ResGetCharacterData> GetCharacterData([FromBodyProtobuf] ReqGetCharacterData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetCharacterData(user);
    }

    [HttpPost("/v1/character/upgrade")]
    public ActionResult<ResCharacterUpgrade> UpgradeCharacter([FromBodyProtobuf] ReqCharacterUpgrade req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UpgradeCharacter(user, req);
    }

    [HttpPost("/v1/character/coreupgrade")]
    public ActionResult<ResCharacterCoreUpgrade> UpgradeCharacterCore(
        [FromBodyProtobuf] ReqCharacterCoreUpgrade req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UpgradeCharacterCore(user, req);
    }

    [HttpPost("/v1/character/levelup")]
    public ActionResult<ResCharacterLevelUp> LevelUpCharacter([FromBodyProtobuf] ReqCharacterLevelUp req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.LevelUpCharacter(user, req);
    }

    [HttpPost("/v1/character/growreset")]
    public ActionResult<ResCharacterGrowReset> ResetCharacterLevel(
        [FromBodyProtobuf] ReqCharacterGrowReset req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResetCharacterLevel(user, req);
    }

    [HttpPost("/v1/character/costume/set")]
    public ActionResult<ResSetCharacterCostume> SetCharacterCostume(
        [FromBodyProtobuf] ReqSetCharacterCostume req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetCharacterCostume(user, req);
    }

    [HttpPost("/v1/character/mainforce/set")]
    public ActionResult<ResSetCharacterMainForce> SetCharacterMainForce(
        [FromBodyProtobuf] ReqSetCharacterMainForce req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetCharacterMainForce(user, req);
    }

    [HttpPost("/v1/character/skill/levelup")]
    public ActionResult<ResCharacterSkillLevelUp> SkillLevelUp(
        [FromBodyProtobuf] ReqCharacterSkillLevelUp req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SkillLevelUp(user, req);
    }

    [HttpPost("/v1/character/skill/levelupV2")]
    public ActionResult<ResCharacterSkillLevelUpV2> SkillLevelUpV2(
        [FromBodyProtobuf] ReqCharacterSkillLevelUpV2 req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SkillLevelUpV2(user, req);
    }

    // --- Wallet ---

    [HttpPost("/v1/wallet/get")]
    public ActionResult<ResGetCurrencyData> GetWallet([FromBodyProtobuf] ReqGetCurrencyData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetWallet(user);
    }

    [HttpPost("/v1/wallet/refreshcharge")]
    public ActionResult<ResRefreshChargeCurrencyData> WalletRefreshCharge(
        [FromBodyProtobuf] ReqRefreshChargeCurrencyData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.WalletRefreshCharge(user);
    }

    // --- Inventory & Equipment ---

    [HttpPost("/v1/inventory/get")]
    public ActionResult<ResGetInventoryData> GetInventoryData([FromBodyProtobuf] ReqGetInventoryData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetInventoryData(user);
    }

    [HttpPost("/v1/inventory/allclearequipment")]
    public ActionResult<ResAllClearEquipment> AllClearEquipment([FromBodyProtobuf] ReqAllClearEquipment req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AllClearEquipment(user, req);
    }

    [HttpPost("/v1/inventory/clearequipment")]
    public ActionResult<ResClearEquipment> ClearEquipment([FromBodyProtobuf] ReqClearEquipment req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearEquipment(user, req);
    }

    [HttpPost("/v1/inventory/wearequipment")]
    public ActionResult<ResWearEquipment> WearEquipment([FromBodyProtobuf] ReqWearEquipment req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.WearEquipment(user, req);
    }

    [HttpPost("/v1/inventory/wearequipmentlist")]
    public ActionResult<ResWearEquipmentList> WearEquipmentList([FromBodyProtobuf] ReqWearEquipmentList req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.WearEquipmentList(user, req);
    }

    [HttpPost("/v1/inventory/increaseexpequipment")]
    public ActionResult<ResIncreaseExpEquip> IncreaseEquipmentExp([FromBodyProtobuf] ReqIncreaseExpEquip req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.IncreaseEquipmentExp(user, req);
    }

    // --- Consumables & Pieces ---

    [HttpPost("/v1/inventory/quickuseitem")]
    public ActionResult<ResQuickUseItem> QuickUseItem([FromBodyProtobuf] ReqQuickUseItem req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.QuickUseItem(user, req);
    }

    [HttpPost("/v1/inventory/usebundlebox")]
    public ActionResult<ResUseBundleBox> UseBundleBox([FromBodyProtobuf] ReqUseBundleBox req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UseBundleBox(user, req);
    }

    [HttpPost("/v1/inventory/userandombox")]
    public ActionResult<ResUseRandomBox> UseRandomBox([FromBodyProtobuf] ReqUseRandomBox req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UseRandomBox(user, req);
    }

    [HttpPost("/v1/inventory/useselectbox")]
    public ActionResult<ResUseSelectBox> UseSelectBox([FromBodyProtobuf] ReqUseSelectBox req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UseSelectBox(user, req);
    }

    [HttpPost("/v1/inventory/usetimereward")]
    public ActionResult<ResUseTimeReward> UseTimeReward([FromBodyProtobuf] ReqUseTimeReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UseTimeReward(user, req);
    }

    [HttpPost("/v1/inventory/usepiece")]
    public ActionResult<ResUsePiece> UsePiece([FromBodyProtobuf] ReqUsePiece req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UsePiece(user, req);
    }

    // --- Harmony Cubes ---

    [HttpPost("/v1/inventory/getharmonycube")]
    public ActionResult<ResGetHarmonyCube> GetHarmonyCube([FromBodyProtobuf] ReqGetHarmonyCube req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetHarmonyCube(user);
    }

    [HttpPost("/v1/inventory/clearharmonycube")]
    public ActionResult<ResClearHarmonyCube> ClearHarmonyCube([FromBodyProtobuf] ReqClearHarmonyCube req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ClearHarmonyCube(user, req);
    }

    [HttpPost("/v1/inventory/levelupharmonycube")]
    public ActionResult<ResLevelUpHarmonyCube> LevelUpHarmonyCube(
        [FromBodyProtobuf] ReqLevelUpHarmonyCube req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.LevelUpHarmonyCube(user, req);
    }

    [HttpPost("/v1/inventory/managementharmonycube")]
    public ActionResult<ResManagementHarmonyCube> ManagementHarmonyCube(
        [FromBodyProtobuf] ReqManagementHarmonyCube req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ManagementHarmonyCube(user, req);
    }

    [HttpPost("/v1/inventory/wearharmonycube")]
    public ActionResult<ResWearHarmonyCube> WearHarmonyCube([FromBodyProtobuf] ReqWearHarmonyCube req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.WearHarmonyCube(user, req);
    }

    // --- Equipment Awakening ---

    [HttpPost("/v1/inventory/equipment/awakening")]
    public ActionResult<ResEquipmentAwakening> AwakeningEquipment(
        [FromBodyProtobuf] ReqEquipmentAwakening req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AwakeningEquipment(user, req);
    }

    [HttpPost("/v1/inventory/equipment/changeoption")]
    public ActionResult<ResAwakeningChangeOption> ChangeEquipmentOption(
        [FromBodyProtobuf] ReqAwakeningChangeOption req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ChangeEquipmentOption(userId, req);
    }

    [HttpPost("/v1/inventory/equipment/getawakeningdetail")]
    public ActionResult<ResGetAwakeningDetail> GetEquipmentAwakeningDetail(
        [FromBodyProtobuf] ReqGetAwakeningDetail req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetEquipmentAwakeningDetail(userId, req);
    }

    [HttpPost("/v1/inventory/equipment/resetoption")]
    public ActionResult<ResAwakeningResetOption> ResetEquipmentOption(
        [FromBodyProtobuf] ReqAwakeningResetOption req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ResetEquipmentOption(user, req);
    }

    [HttpPost("/v1/inventory/equipment/upgradeoption")]
    public ActionResult<ResAwakeningUpgradeOption> UpgradeEquipmentOption(
        [FromBodyProtobuf] ReqAwakeningUpgradeOption req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.UpgradeEquipmentOption(user, req);
    }
}
