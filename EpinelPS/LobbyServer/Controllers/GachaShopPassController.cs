using EpinelPS.Interfaces;
using EpinelPS.Services;
using EpinelPS.Utils;
using Microsoft.AspNetCore.Mvc;

namespace EpinelPS.LobbyServer.Controllers;

[ApiController]
public sealed class GachaShopPassController(
    IUserService userService,
    GachaShopPassService service) : ControllerBase
{
    // --- Gacha & ShutdownFlags ---

    [HttpPost("/v1/gacha/acquirepaybackallreward")]
    public ActionResult<ResAcquireGachaPaybackAllReward> AcquirePaybackAllRewards(
        [FromBodyProtobuf] ReqAcquireGachaPaybackAllReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AcquirePaybackAllRewards(user, req);
    }

    [HttpPost("/v1/gacha/acquirepaybackreward")]
    public ActionResult<ResAcquireGachaPaybackReward> AcquirePaybackReward(
        [FromBodyProtobuf] ReqAcquireGachaPaybackReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.AcquirePaybackReward(user, req);
    }

    [HttpPost("/v1/gacha/event/check")]
    public ActionResult<ResCheckDailyFreeGacha> CheckDailyEventGacha(
        [FromBodyProtobuf] ReqCheckDailyFreeGacha req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CheckDailyEventGacha(userId, req);
    }

    [HttpPost("/v1/gacha/execute")]
    public ActionResult<ResExecuteGacha> ExecuteGacha([FromBodyProtobuf] ReqExecuteGacha req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ExecuteGacha(user, req);
    }

    [HttpPost("/v1/gacha/event/execute")]
    public ActionResult<ResExecuteDailyFreeGacha> ExecuteDailyEventGacha(
        [FromBodyProtobuf] ReqExecuteDailyFreeGacha req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ExecuteDailyEventGacha(user, req);
    }

    [HttpPost("/v1/gacha/pity/execute")]
    public ActionResult<ResExecuteGachaPity> ExecutePityGacha([FromBodyProtobuf] ReqExecuteGachaPity req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ExecutePityGacha(user, req);
    }

    [HttpPost("/v1/Gacha/Get")]
    public ActionResult<ResGetGachaData> GetGacha([FromBodyProtobuf] ReqGetGachaData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetGacha(userId);
    }

    [HttpPost("/v1/gacha/getpayback")]
    public ActionResult<ResGetGachaPaybackData> GetPayback([FromBodyProtobuf] ReqGetGachaPaybackData req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetPayback(userId);
    }

    [HttpPost("/v1/gacha/pity/list")]
    public ActionResult<ResListGachaPityProgress> ListPity([FromBodyProtobuf] ReqListGachaPityProgress req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ListPity(userId);
    }

    [HttpPost("/v1/Gacha/SetCustom")]
    public ActionResult<ReqSetGachaCustom> SetCustomWishlistSlot([FromBodyProtobuf] ReqSetGachaCustom req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetCustomWishlistSlot(userId, req);
    }

    [HttpPost("/v1/gacha/setselectup")]
    public ActionResult<ResSetGachaSelectup> SetGachaSelectup([FromBodyProtobuf] ReqSetGachaSelectup req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.SetGachaSelectup(userId, req);
    }

    [HttpPost("/v1/shutdownflags/gacha/getall")]
    public ActionResult<ResGachaGetAllShutdownFlags> GetAllGachaShutdownFlags(
        [FromBodyProtobuf] ReqGachaGetAllShutdownFlags req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetAllGachaShutdownFlags(userId);
    }

    // --- Pass ---

    [HttpPost("/v1/pass/event/buyrank")]
    public ActionResult<ResBuyEventPassRank> BuyEventPassRank([FromBodyProtobuf] ReqBuyEventPassRank req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.BuyEventPassRank(user, req);
    }

    [HttpPost("/v1/pass/buyrank")]
    public ActionResult<ResBuyPassRank> BuyPassRank([FromBodyProtobuf] ReqBuyPassRank req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.BuyPassRank(user, req);
    }

    [HttpPost("/v1/pass/event/completemission")]
    public ActionResult<ResCompleteEventPassMission> CompleteEventPassMission(
        [FromBodyProtobuf] ReqCompleteEventPassMission req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompleteEventPassMission(user, req);
    }

    [HttpPost("/v1/pass/completemission")]
    public ActionResult<ResCompletePassMission> CompletePassMission([FromBodyProtobuf] ReqCompletePassMission req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.CompletePassMission(user, req);
    }

    [HttpPost("/v1/pass/event/getactive")]
    public ActionResult<ResGetActiveEventPassData> GetActiveEventPassData(
        [FromBodyProtobuf] ReqGetActiveEventPassData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetActiveEventPassData(user);
    }

    [HttpPost("/v1/pass/event/freereward/getactive")]
    public ActionResult<ResGetActiveFreeRewardPassData> GetActiveFreeRewardData(
        [FromBodyProtobuf] ReqGetActiveFreeRewardPassData req)
        => new ResGetActiveFreeRewardPassData();

    [HttpPost("/v1/pass/getactive")]
    public ActionResult<ResGetActivePassData> GetActivePassData([FromBodyProtobuf] ReqGetActivePassData req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetActivePassData(user);
    }

    [HttpPost("/v1/pass/event/obtainreward")]
    public ActionResult<ResObtainEventPassReward> ObtainEventPassReward(
        [FromBodyProtobuf] ReqObtainEventPassReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainEventPassReward(user, req);
    }

    [HttpPost("/v1/pass/event/obtainonereward")]
    public ActionResult<ResObtainOneEventPassReward> ObtainOneEventPassReward(
        [FromBodyProtobuf] ReqObtainOneEventPassReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainOneEventPassReward(user, req);
    }

    [HttpPost("/v1/pass/obtainonereward")]
    public ActionResult<ResObtainOnePassReward> ObtainOnePassReward([FromBodyProtobuf] ReqObtainOnePassReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainOnePassReward(user, req);
    }

    [HttpPost("/v1/pass/obtainreward")]
    public ActionResult<ResObtainPassReward> ObtainPassReward([FromBodyProtobuf] ReqObtainPassReward req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.ObtainPassReward(user, req);
    }

    // --- Shop, InApp & PackageShop ---

    [HttpPost("/v1/shop/multiple-buy")]
    public ActionResult<ResShopBuyMultipleProduct> BuyMultipleProduct(
        [FromBodyProtobuf] ReqShopBuyMultipleProduct req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.BuyMultipleProduct(user, req);
    }

    [HttpPost("/v1/shop/buy")]
    public ActionResult<ResShopBuyProduct> BuyProduct([FromBodyProtobuf] ReqShopBuyProduct req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.BuyProduct(user, req);
    }

    [HttpPost("/v1/inappshop/jupiter/getproductlist")]
    public ActionResult<ResGetJupiterProductList> GetJupiterProductList(
        [FromBodyProtobuf] ReqGetJupiterProductList req)
        => service.GetJupiterProductList(req);

    [HttpPost("/v1/productoffer/list")]
    public ActionResult<ResListSeenProductOffer> GetProductOffers([FromBodyProtobuf] ReqListSeenProductOffer req)
        => service.GetProductOffers();

    [HttpPost("/v1/shop/get")]
    public ActionResult<ResGetShop> GetShop([FromBodyProtobuf] ReqGetShop req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetShop(user, req);
    }

    [HttpPost("/v1/shop/productlist")]
    public ActionResult<ResShopProductList> GetShopProductList([FromBodyProtobuf] ReqShopProductList req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetShopProductList(user);
    }

    [HttpPost("/v1/inappshop/jupiter/issueactivitypoints")]
    public ActionResult<ResIssueJupiterActivityPoints> IssueActivityPoints(
        [FromBodyProtobuf] ReqIssueJupiterActivityPoints req)
        => new ResIssueJupiterActivityPoints { IssueDetail = "", HasActivity = false };

    [HttpPost("/v1/shop/renew")]
    public ActionResult<ResShopRenew> RenewShop([FromBodyProtobuf] ReqShopRenew req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.RenewShop(user, req);
    }

    [HttpPost("/v1/productoffer/setseen")]
    public ActionResult<ResSetSetSeenProductOffer> SeenProductOffer(
        [FromBodyProtobuf] ReqSetSetSeenProductOffer req)
        => new ResSetSetSeenProductOffer();

    [HttpPost("/v1/inappshop/jupiter/buyproduct")]
    public ActionResult<ResBuyJupiterProduct> BuyJupiterProduct([FromBodyProtobuf] ReqBuyJupiterProduct req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.BuyJupiterProduct(user, req);
    }

    [HttpPost("/v1/inappshop/getbuyproduct")]
    public ActionResult<ResGetInAppShopBuyProduct> GetBuyProduct([FromBodyProtobuf] ReqGetInAppShopBuyProduct req)
    {
        if (userService.GetUserId() is not ulong userId)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.GetInAppBuyProduct(userId, req);
    }

    [HttpPost("/v1/inappshop/jupiter/getmarketingdetail")]
    public ActionResult<ResGetJupiterMarketingDetail> GetMarketingDetail(
        [FromBodyProtobuf] ReqGetJupiterMarketingDetail req)
        => new ResGetJupiterMarketingDetail { MarketingDetail = "{}" };

    [HttpPost("/v1/inappshop/getmonthlysubscriptionreward")]
    public ActionResult<ResGetMonthlySubscriptionReward> GetMonthlySubscriptionReward(
        [FromBodyProtobuf] ReqGetMonthlySubscriptionReward req)
        => new ResGetMonthlySubscriptionReward();

    [HttpPost("/v1/inappshop/getdata")]
    public ActionResult<ResGetInAppShopData> GetInAppShopData([FromBodyProtobuf] ReqGetInAppShopData req)
        => service.GetInAppShopData();

    [HttpPost("/v1/inappshop/getreceivableproductlist")]
    public ActionResult<ResGetInAppShopReceivableProductList> GetReceivableProductList(
        [FromBodyProtobuf] ReqGetInAppShopReceivableProductList req)
        => new ResGetInAppShopReceivableProductList();

    [HttpPost("/v1/inappshop/custompackage/getsetupdata")]
    public ActionResult<ResGetCustomPackageSetupData> GetSetupDataForCustomPackage(
        [FromBodyProtobuf] ReqGetCustomPackageSetupData req)
        => new ResGetCustomPackageSetupData();

    [HttpPost("/v1/inappshop/startpurchase")]
    public ActionResult<ResStartPurchase> StartPurchase([FromBodyProtobuf] ReqStartPurchase req)
    {
        if (userService.GetUser() is not User user)
            return Problem(type: NetUtils.InvalidSessionErrorType);

        return service.StartPurchase(user, req);
    }

    [HttpPost("/v1/packageshop/getpopuppackagestate")]
    public ActionResult<ResGetPopupPackageState> GetPackagePopupState(
        [FromBodyProtobuf] ReqGetPopupPackageState req)
        => service.GetPackagePopupState();

    [HttpPost("/v1/packageshop/campaign/get")]
    public ActionResult<ResGetCampaignPackage> PackageShopGetCampaignPackage(
        [FromBodyProtobuf] ReqGetCampaignPackage req)
        => new ResGetCampaignPackage();

    [HttpPost("/v1/packageshop/setpopuppackagestate")]
    public ActionResult<ResSetPopupPackageState> SetPopupPackageState(
        [FromBodyProtobuf] ReqSetPopupPackageState req)
        => new ResSetPopupPackageState();
}
