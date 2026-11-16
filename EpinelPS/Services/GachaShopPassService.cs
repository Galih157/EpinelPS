using System.Globalization;
using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Event;
using EpinelPS.LobbyServer.Messenger;
using EpinelPS.LobbyServer.Pass;
using EpinelPS.LobbyServer.Shop;
using EpinelPS.LobbyServer.Shop.InApp;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.Collections;
using log4net;
using Newtonsoft.Json;

namespace EpinelPS.Services;

public sealed class GachaShopPassService(
    GameContext db,
    GachaShopPassRepository repo,
    BondLiberationRepository bondRepo,
    CosmeticsRepository cosmeticsRepo)
{
    private const int StandardBannerId = 1;
    private const int SocialBannerId = 2;
    private const int NewPlayerSpecialBannerId = 4;

    private static readonly ILog Log = LogManager.GetLogger(typeof(GachaShopPassService));

    // --- Gacha & ShutdownFlags ---

    public ResAcquireGachaPaybackAllReward AcquirePaybackAllRewards(User user, ReqAcquireGachaPaybackAllReward req)
    {
        ResAcquireGachaPaybackAllReward response = new();
        var paybackData = repo.GetPaybackData(user.ID, req.GachaId);
        if (paybackData == null)
            return response;

        int paybackId = GameData.Instance.GachaPaybackRecords
            .Where(payback => payback.Value.GachaId == req.GachaId)
            .Select(payback => payback.Key)
            .FirstOrDefault();

        List<GachaPaybackStepRecord_Raw> steps = GameData.Instance.GachaPaybackStepRecords
            .Where(step => step.Value.PaybackId == paybackId
                           && step.Value.GachaCount <= paybackData.GachaCount
                           && !paybackData.RewardedStepList.Contains(step.Value.Step))
            .Select(step => step.Value)
            .ToList();

        if (steps.Count > 0)
        {
            response.Reward = RewardUtils.RegisterRewardsForUserDou(user, steps.Select(step => step.RewardId).ToList());
            response.RewardedStepList.AddRange(steps.Select(step => step.Step));
            repo.AddPaybackRewardedSteps(user.ID, req.GachaId, steps.Select(step => step.Step));
            db.SaveChanges();
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResAcquireGachaPaybackReward AcquirePaybackReward(User user, ReqAcquireGachaPaybackReward req)
    {
        ResAcquireGachaPaybackReward response = new();
        int paybackId = GameData.Instance.GachaPaybackRecords
            .Where(payback => payback.Value.GachaId == req.GachaId)
            .Select(payback => payback.Key)
            .FirstOrDefault();

        GachaPaybackStepRecord_Raw? step = GameData.Instance.GachaPaybackStepRecords
            .Where(s => s.Value.PaybackId == paybackId && s.Value.Step == req.Step)
            .Select(s => s.Value)
            .FirstOrDefault();

        if (step != null)
        {
            RewardRecord reward = GameData.Instance.RewardDataRecords[step.RewardId];
            repo.AddPaybackRewardedSteps(user.ID, req.GachaId, [req.Step]);
            response.Reward = RewardUtils.RegisterRewardsForUser(user, reward);
            db.SaveChanges();
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResCheckDailyFreeGacha CheckDailyEventGacha(ulong userId, ReqCheckDailyFreeGacha req)
    {
        ResCheckDailyFreeGacha response = new();
        try
        {
            var evntFree = GameData.Instance.eventManagers[GameData.Instance.gachaTypes[req.GachaId].DailyFreeGachaEventId];
            response.FreeCount = repo.HasDailyFreePull(userId, evntFree.Id) ? 0 : 1;
            response.EventData = new NetEventData
            {
                Id = evntFree.Id,
                EventSystemType = (int)evntFree.EventSystemType,
                EventVisibleDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)).Ticks,
                EventStartDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)).Ticks,
                EventEndDate = DateTime.Now.AddDays(20).Ticks,
                EventDisableDate = DateTime.Now.AddDays(20).Ticks
            };
        }
        catch
        {
            Logging.WriteLine("[CheckGachaDailyEvent] Event was not found in the database.");
        }

        return response;
    }

    public ResExecuteGacha ExecuteGacha(User user, ReqExecuteGacha req)
    {
        int numberOfPulls = req.Count == 1 ? 1 : 10;
        int bannerId = req.Tid;
        Logging.WriteLine($"Banner ID: {bannerId}");
        GachaTypeRecord gachaType = GameData.Instance.gachaTypes[bannerId];
        Logging.WriteLine($"Banner Type: {gachaType.Type}");
        Logging.WriteLine($"Currency type: {(CurrencyType)req.CurrencyType}");

        ResExecuteGacha response = new() { Reward = new NetRewardData { PassPoint = new() } };
        List<CharacterRecord> selectedCharacters = GachaUtils.ExecuteGachaPull(gachaType, numberOfPulls, user);
        bool obtainedNewCharacter = false;
        int totalBodyLabels = 0;

        foreach (CharacterRecord characterData in selectedCharacters)
        {
            NetGachaEntityData gacha = GrantGachaCharacterOrPiece(
                user,
                characterData,
                response.Characters,
                response.Items,
                ref obtainedNewCharacter,
                ref totalBodyLabels,
                out _,
                out _);

            response.Gacha.Add(gacha);
            user.AddTrigger(Trigger.GachaCharacter, 0, 0);
        }

        if (obtainedNewCharacter)
            MessengerMessageCreator.CreateAllEligibleOpeners(user);

        CurrencyType ticketType = (CurrencyType)req.CurrencyType;
        bool canUsePremiumTicket = ticketType == CurrencyType.CharPremiumTicket;
        bool canUseCustomizeTicket = ticketType == CurrencyType.CharCustomizeTicket;
        bool canUseFreeCash =
            ticketType == CurrencyType.CharPremiumTicket ||
            ticketType == CurrencyType.CharCustomizeTicket ||
            ticketType == CurrencyType.FreeCash;

        long pullsLeft = numberOfPulls;
        bool discount = req.IsDiscount;

        long usePremiumTickets = 0;
        long useCharCustomizeTickets = 0;
        long useFreeCash = 0;
        long useChargeCash = 0;
        long useFriendshipPoint = 0;

        long userPremiumTickets = user.GetCurrencyVal(CurrencyType.CharPremiumTicket);
        long userCharCustomizeTickets = user.GetCurrencyVal(CurrencyType.CharCustomizeTicket);
        long userFreeCash = user.GetCurrencyVal(CurrencyType.FreeCash);

        switch (ticketType)
        {
            case CurrencyType.ChargeCash:
            {
                var cashPrice = gachaType.GachaPriceGroup.First(g =>
                    g.GachaPriceType == 98 || g.GachaPriceType == 99);
                useChargeCash = pullsLeft * (discount ? cashPrice.DailyGachaDiscountPriceValue1 : cashPrice.GachaPriceValueCount1);
                pullsLeft = 0;
                break;
            }
            case CurrencyType.FriendshipPoint:
            {
                var fpPrice = gachaType.GachaPriceGroup.First(g => g.GachaPriceType == 4000);
                useFriendshipPoint = pullsLeft * fpPrice.GachaPriceValueCount1;
                pullsLeft = 0;
                break;
            }
        }

        if (canUsePremiumTicket)
        {
            usePremiumTickets = Math.Min(userPremiumTickets, pullsLeft);
            pullsLeft -= usePremiumTickets;
        }

        if (canUseCustomizeTicket)
        {
            useCharCustomizeTickets = Math.Min(userCharCustomizeTickets, pullsLeft);
            pullsLeft -= useCharCustomizeTickets;
        }

        if (canUseFreeCash && gachaType.Type != GachaPremiumType.GachaTutorial)
        {
            var cashPrice = gachaType.GachaPriceGroup.First(g =>
                g.GachaPriceType == 98 || g.GachaPriceType == 99);
            long costPerPull = discount ? cashPrice.DailyGachaDiscountPriceValue1 : cashPrice.GachaPriceValueCount1;
            long totalCostNeeded = pullsLeft * costPerPull;

            useFreeCash = Math.Min(userFreeCash, totalCostNeeded);
            useChargeCash = totalCostNeeded - useFreeCash;
            pullsLeft = 0;
        }

        void ApplyCurrency(CurrencyType type, long delta)
        {
            if (delta == 0) return;

            if (delta < 0)
                user.SubtractCurrency(type, -delta);
            else
                user.AddCurrency(type, delta);

            response.Currencies.Add(new NetUserCurrencyData
            {
                Type = (int)type,
                Value = user.GetCurrencyVal(type)
            });
        }

        ApplyCurrency(CurrencyType.CharPremiumTicket, -usePremiumTickets);
        ApplyCurrency(CurrencyType.CharCustomizeTicket, -useCharCustomizeTickets);
        ApplyCurrency(CurrencyType.FreeCash, -useFreeCash);
        ApplyCurrency(CurrencyType.ChargeCash, -useChargeCash);
        ApplyCurrency(CurrencyType.FriendshipPoint, -useFriendshipPoint);
        ApplyCurrency(CurrencyType.DissolutionPoint, totalBodyLabels);

        if (discount)
            UserDbHelper.GetOrCreateGameUser(db, user.ID).DailyDiscountUsed = true;

        if (bannerId == StandardBannerId)
            ApplyCurrency(CurrencyType.SilverMileageTicket, numberOfPulls);

        if (bannerId != StandardBannerId && bannerId != SocialBannerId && bannerId != NewPlayerSpecialBannerId &&
            gachaType.Type != GachaPremiumType.GachaTutorial)
            ApplyCurrency(CurrencyType.GoldMileageTicket, numberOfPulls);

        repo.AddGachaPullCount(user, bannerId, numberOfPulls);

        var prs = GameData.Instance.GachaPaybackRecords.Where(p => p.Value.GachaId == bannerId).ToDictionary();
        if (prs.Count > 0)
        {
            var pr = prs.First();
            repo.IncrementPaybackCount(user.ID, pr.Value.GachaId, req.Count);
        }

        db.SaveChanges();
        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResExecuteDailyFreeGacha ExecuteDailyEventGacha(User user, ReqExecuteDailyFreeGacha req)
    {
        const int numberOfPulls = 1;
        int bannerId = req.GachaId;
        Logging.WriteLine($"Event ID: {req.EventId}");
        Logging.WriteLine($"Banner ID: {bannerId}");
        GachaTypeRecord gachaType = GameData.Instance.gachaTypes[bannerId];
        Logging.WriteLine("Currency type: Daily free pull");

        ResExecuteDailyFreeGacha response = new();
        List<CharacterRecord> selectedCharacters = GachaUtils.ExecuteGachaPull(gachaType, numberOfPulls, user);

        foreach (CharacterRecord characterData in selectedCharacters)
        {
            DbItemData? existingItem = user.Items.FirstOrDefault(item => item.ItemType == characterData.PieceId);
            if (existingItem != null)
            {
                existingItem.Count += 1;
                response.Items.Add(ToNetItem(existingItem));
            }
            else
            {
                DbItemData newItem = new()
                {
                    ItemType = characterData.PieceId,
                    Csn = 0,
                    Count = 1,
                    Level = 0,
                    Exp = 0,
                    Position = 0,
                    Corp = 0,
                    Isn = user.GenerateUniqueItemId()
                };
                user.Items.Add(newItem);
                response.Items.Add(ToNetItem(newItem));
            }
        }

        foreach (CharacterRecord characterData in selectedCharacters)
        {
            int characterId = characterData.Id;
            int id = user.GenerateUniqueCharacterId();
            response.Gacha.Add(new NetGachaEntityData
            {
                Corporation = 1,
                PieceCount = 1,
                CurrencyValue = 5,
                Sn = id,
                Tid = characterId,
                Type = 1
            });

            if (!user.HasCharacter(characterId))
            {
                response.Characters.Add(new NetUserCharacterDefaultData
                {
                    CostumeId = 0,
                    Csn = id,
                    Grade = 0,
                    Lv = 1,
                    Skill1Lv = 1,
                    Skill2Lv = 1,
                    Tid = characterId,
                    UltiSkillLv = 1
                });

                user.Characters.Add(new CharacterModel
                {
                    CostumeId = 0,
                    Csn = id,
                    Grade = 0,
                    Level = 1,
                    Skill1Lvl = 1,
                    Skill2Lvl = 1,
                    Tid = characterId,
                    UltimateLevel = 1
                });
            }
        }

        repo.AddDailyFreePull(user.ID, req.EventId);
        repo.AddGachaPullCount(user, bannerId, numberOfPulls);
        db.SaveChanges();
        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResExecuteGachaPity ExecutePityGacha(User user, ReqExecuteGachaPity req)
    {
        ResExecuteGachaPity response = new() { Reward = new NetRewardData() };
        repo.AddPityExecuteCount(user.ID, req.GachaPityId);

        CharacterRecord characterData = GameData.Instance.CharacterTable[req.CharacterId];
        int totalBodyLabels = 0;
        bool obtainedNewCharacter = false;

        NetGachaEntityData gacha = GrantGachaCharacterOrPiece(
            user,
            characterData,
            response.Reward.UserCharacters,
            response.Reward.UserItems,
            ref obtainedNewCharacter,
            ref totalBodyLabels,
            out bool addedCharacterNetEntry,
            out bool grantedPieceBeforeIncrement);

        if (addedCharacterNetEntry || grantedPieceBeforeIncrement)
        {
            response.Reward.Character.Add(new NetCharacterData
            {
                Csn = gacha.Sn,
                CurrencyValue = gacha.CurrencyValue,
                PieceCount = addedCharacterNetEntry ? gacha.PieceCount : 0,
                Tid = characterData.Id
            });
        }

        MessengerMessageCreator.CreateAllEligibleOpeners(user);

        if (totalBodyLabels != 0)
        {
            if (totalBodyLabels < 0)
                user.SubtractCurrency(CurrencyType.DissolutionPoint, -totalBodyLabels);
            else
                user.AddCurrency(CurrencyType.DissolutionPoint, totalBodyLabels);

            response.Reward.Currency.Add(new NetCurrencyData
            {
                Type = (int)CurrencyType.DissolutionPoint,
                Value = totalBodyLabels,
                FinalValue = user.GetCurrencyVal(CurrencyType.DissolutionPoint)
            });
        }

        db.SaveChanges();

        var pityBanner = GameData.Instance.GachaPityRecords.First().Value;
        int executeCount = repo.GetPityExecuteCount(user.ID, req.GachaPityId);
        response.GachaPityProgress = new NetGachaPityData
        {
            GachaPityId = req.GachaPityId,
            PityExecuteCount = executeCount,
            TotalGachaCount = repo.GetGachaCountForType(user.ID, GachaPremiumType.GachaPremium),
            TotalUsedGachaCount = executeCount * pityBanner.NeedGachaCount
        };

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResGetGachaData GetGacha(ulong userId)
    {
        ResGetGachaData response = new();

        if (repo.GetGachaCountForType(userId, GachaPremiumType.GachaTutorial) > 0)
            response.Gacha.Add(new NetUserGachaData { GachaType = 3, PlayCount = 1 });

        response.Gacha.Add(new NetUserGachaData { GachaType = 9, PlayCount = 0 });
        response.GachaEventData.Add(new NetGachaEvent { FreeCount = 1, GachaTypeId = 9 });
        response.MultipleCustom.AddRange(
            repo.GetWishlist(userId).Select(id => new NetGachaCustomData { Type = 9, Tid = id.CharacterId }));

        if (UserDbHelper.GetOrCreateGameUser(db, userId).DailyDiscountUsed)
            response.GachaDiscountData.Add(new NetUserGachaDiscountData { GachaTypeId = 1, Count = 1 });

        var selectupList = GameData.Instance.GachaSelectupListTable.GroupBy(s => s.Value.GachaTypeId);
        foreach (var selectup in selectupList)
        {
            int selectupId = repo.GetOrAddSelectupChoice(userId, selectup.Key, selectup.First().Value.Id);
            response.GachaSelectupData.Add(new NetUserGachaSelectupData
            {
                GachaSelectupId = selectupId,
                GachaTypeId = selectup.Key
            });
        }

        db.SaveChanges();
        return response;
    }

    public ResGetGachaPaybackData GetPayback(ulong userId)
    {
        ResGetGachaPaybackData response = new();
        foreach (GachaPaybackData gpd in repo.GetPaybackMap(userId).Values)
        {
            var ngpd = new NetGachaPaybackData
            {
                GachaId = gpd.GachaId,
                GachaCount = gpd.GachaCount
            };
            if (gpd.RewardedStepList.Count > 0)
                ngpd.RewardedStepList.AddRange(gpd.RewardedStepList);

            response.PaybackDataList.Add(ngpd);
        }

        return response;
    }

    public ResListGachaPityProgress ListPity(ulong userId)
    {
        ResListGachaPityProgress response = new();
        var pityBanner = GameData.Instance.GachaPityRecords.First().Value;
        int executeCount = repo.GetPityExecuteCount(userId, pityBanner.Id);

        response.GachaPityProgress.Add(new NetGachaPityData
        {
            GachaPityId = pityBanner.Id,
            PityExecuteCount = executeCount,
            TotalGachaCount = repo.GetGachaCountForType(userId, GachaPremiumType.GachaPremium),
            TotalUsedGachaCount = executeCount * pityBanner.NeedGachaCount
        });

        return response;
    }

    public ReqSetGachaCustom SetCustomWishlistSlot(ulong userId, ReqSetGachaCustom req)
    {
        List<CharacterWishlistData> wishlist = req.Custom
            .Select(w => new CharacterWishlistData { BannerId = w.Type, CharacterId = w.Tid })
            .ToList();

        repo.ReplaceWishlist(userId, wishlist);
        db.SaveChanges();
        Logging.WriteLine($"[SetCustomWishlistSlot] Updated wishlist for user {userId}: [{string.Join(", ", wishlist)}]");
        return req;
    }

    public ResSetGachaSelectup SetGachaSelectup(ulong userId, ReqSetGachaSelectup req)
    {
        repo.SetSelectupChoice(userId, req.GachaSelectupData.GachaTypeId, req.GachaSelectupData.GachaSelectupId);
        db.SaveChanges();
        Logging.WriteLine(
            $"[SetGachaSelectSetup] Updated selection for user {userId} to GachaTypeId {req.GachaSelectupData.GachaTypeId} - GachaSelectupId {req.GachaSelectupData.GachaSelectupId}");
        return new ResSetGachaSelectup();
    }

    public ResGachaGetAllShutdownFlags GetAllGachaShutdownFlags(ulong userId)
    {
        ResGachaGetAllShutdownFlags response = new();
        if (repo.GetGachaCountForType(userId, GachaPremiumType.GachaTutorial) > 0)
            response.Unavailables.Add(3);

        return response;
    }

    // --- Pass ---

    public ResBuyEventPassRank BuyEventPassRank(User user, ReqBuyEventPassRank req)
    {
        ResBuyEventPassRank response = new();
        PassHelper.BuyRank(user, req.PassId, req.TargetPassRank, out int passPoint, out NetUserCurrencyData currency);
        response.PassRank = req.TargetPassRank;
        response.PassPoint = passPoint;
        response.Currencies.Add(currency);
        return response;
    }

    public ResBuyPassRank BuyPassRank(User user, ReqBuyPassRank req)
    {
        ResBuyPassRank response = new();
        PassHelper.BuyRank(user, req.PassId, req.TargetPassRank, out int passPoint, out NetUserCurrencyData currency);
        response.PassRank = req.TargetPassRank;
        response.PassPoint = passPoint;
        response.Currencies.Add(currency);
        return response;
    }

    public ResCompleteEventPassMission CompleteEventPassMission(User user, ReqCompleteEventPassMission req)
    {
        NetRewardData reward = new() { PassPoint = { } };
        PassHelper.CompletePassMissions(user, ref reward, req.PassId, [.. req.PassMissionList]);
        return new ResCompleteEventPassMission { Reward = reward };
    }

    public ResCompletePassMission CompletePassMission(User user, ReqCompletePassMission req)
    {
        NetRewardData reward = new() { PassPoint = { } };
        PassHelper.CompletePassMissions(user, ref reward, req.PassId, [.. req.PassMissionList]);
        return new ResCompletePassMission { Reward = reward };
    }

    public ResGetActiveEventPassData GetActiveEventPassData(User user)
    {
        ResGetActiveEventPassData response = new();
        List<LobbyPrivateBannerRecord> lobbyPrivateBanners = EventHelper.GetLobbyPrivateBannerData(user);
        Log.Debug($"Active lobby private banners: {JsonConvert.SerializeObject(lobbyPrivateBanners)}");
        if (lobbyPrivateBanners.Count <= 0)
        {
            Logging.WriteLine("No active lobby private banners found.", LogType.Warning);
            return response;
        }

        List<int> passIds = [];
        foreach (var banner in lobbyPrivateBanners)
        {
            passIds.AddRange(GameData.Instance.eventManagers.Values
                .Where(em => em.SetField == banner.EventId && em.EventSystemType == EventSystemType.EventPass)
                .Select(em => em.Id));
        }

        Log.Debug($"Active event pass IDs from banners: {JsonConvert.SerializeObject(passIds)}");
        if (passIds.Count == 0)
        {
            Logging.WriteLine("No active event pass IDs found from lobby private banners.", LogType.Warning);
            return response;
        }

        var passManagers = GameData.Instance.EventPassManagerTable.Values.Where(p => passIds.Contains(p.EventId)).ToList();
        Log.Debug($"Active event pass managers: {JsonConvert.SerializeObject(passManagers)}");
        if (passManagers.Count == 0)
        {
            Logging.WriteLine("No active event pass found.");
            return response;
        }

        foreach (var pm in passManagers)
        {
            NetPassInfo passInfo = PassHelper.GetPassInfo(user, pm.Id, pm.PassPointId);
            if (passInfo.PassId != 0 && passInfo.PassRankList.Count > 0)
                response.PassList.Add(passInfo);
        }

        return response;
    }

    public ResGetActivePassData GetActivePassData(User user)
    {
        ResGetActivePassData response = new() { PassExist = false };
        var passManager = GameData.Instance.PassManagerTable.Values.FirstOrDefault(p =>
            p.SeasonStartDate <= DateTime.UtcNow && p.SeasonEndDate >= DateTime.UtcNow);

        if (passManager != null)
        {
            Log.Debug($"Found active pass: {JsonConvert.SerializeObject(passManager)}");
            NetPassInfo passInfo = PassHelper.GetPassInfo(user, passManager.Id, passManager.PassPointId);
            if (passInfo.PassId != 0 && passInfo.PassRankList.Count > 0)
            {
                response.PassExist = true;
                response.Pass = passInfo;
            }
        }
        else
        {
            Logging.WriteLine("No active pass found.");
        }

        return response;
    }

    public ResObtainEventPassReward ObtainEventPassReward(User user, ReqObtainEventPassReward req)
    {
        NetRewardData reward = new() { PassPoint = { } };
        PassHelper.ObtainPassRewards(user, ref reward, req.PassId, [.. req.PassRank]);
        return new ResObtainEventPassReward { Reward = reward };
    }

    public ResObtainOneEventPassReward ObtainOneEventPassReward(User user, ReqObtainOneEventPassReward req)
    {
        NetRewardData reward = new() { PassPoint = { } };
        PassHelper.ObtainOnePassRewards(user, ref reward, req.PassId, req.PassRank, req.PremiumReward);
        return new ResObtainOneEventPassReward { Reward = reward };
    }

    public ResObtainOnePassReward ObtainOnePassReward(User user, ReqObtainOnePassReward req)
    {
        NetRewardData reward = new() { PassPoint = { } };
        PassHelper.ObtainOnePassRewards(user, ref reward, req.PassId, req.PassRank, req.PremiumReward);
        return new ResObtainOnePassReward { Reward = reward };
    }

    public ResObtainPassReward ObtainPassReward(User user, ReqObtainPassReward req)
    {
        NetRewardData reward = new() { PassPoint = { } };
        PassHelper.ObtainPassRewards(user, ref reward, req.PassId, [.. req.PassRank]);
        return new ResObtainPassReward { Reward = reward };
    }

    // --- Shop, InApp & PackageShop ---

    public ResShopBuyMultipleProduct BuyMultipleProduct(User user, ReqShopBuyMultipleProduct req)
    {
        ResShopBuyMultipleProduct response = new()
        {
            Product = new NetShopBuyMultipleProductData()
        };

        var requests = req.Products
            .Select(x => new NormalShopHelper.PurchaseRequest(x.ShopProductTid, x.Order, x.Quantity > 0 ? x.Quantity : 1))
            .ToList();

        if (!NormalShopHelper.TryPurchase(user, req.ShopCategory, requests, out var purchased,
                out var currencies, out var costItems))
        {
            response.Result = ShopBuyProductResult.Expired;
            return response;
        }

        response.Currencies.AddRange(currencies);
        response.Items.AddRange(costItems);
        foreach (var purchase in purchased)
        {
            response.Product.BuyCounts.Add(new NetBuyCountData
            {
                Order = purchase.Order,
                BuyCount = purchase.BuyCount
            });

            if (GrantCommonShopProduct(
                    user,
                    purchase.Product,
                    purchase.Quantity,
                    response.Product.Currency,
                    response.Product.Item,
                    response.Product.UserItems,
                    response.Product.LiveWallPapers,
                    response.Product.UserTitleList))
            {
                continue;
            }

            if (purchase.Product.GoodsType == RewardType.Character &&
                GameData.Instance.CharacterTable.TryGetValue(purchase.Product.GoodsId, out var charRecord))
            {
                int totalValue = purchase.Product.GoodsValue * purchase.Quantity;
                var userChar = user.GetCharacter(purchase.Product.GoodsId);
                if (userChar == null)
                {
                    userChar = new CharacterModel
                    {
                        Csn = user.GenerateUniqueCharacterId(),
                        Grade = 1,
                        Tid = charRecord.Id
                    };
                    user.Characters.Add(userChar);
                }

                response.Product.Character.Add(new NetCharacterData
                {
                    Csn = userChar.Csn,
                    Tid = userChar.Tid,
                    PieceCount = totalValue
                });
            }
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResShopBuyProduct BuyProduct(User user, ReqShopBuyProduct req)
    {
        ResShopBuyProduct response = new()
        {
            Product = new NetShopBuyProductData()
        };

        int quantity = req.Quantity > 0 ? req.Quantity : 1;
        var request = new NormalShopHelper.PurchaseRequest(req.ShopProductTid, req.Order, quantity);
        if (!NormalShopHelper.TryPurchase(user, req.ShopCategory, [request], out var purchased,
                out var currencies, out var costItems))
        {
            response.Result = ShopBuyProductResult.Expired;
            return response;
        }

        response.Currencies.AddRange(currencies);
        response.Item = costItems.FirstOrDefault();
        var purchase = purchased[0];
        response.Product.BuyCount = purchase.BuyCount;

        Console.WriteLine($"Bought: {purchase.Product.GoodsType}");
        if (!GrantCommonShopProduct(
                user,
                purchase.Product,
                purchase.Quantity,
                response.Product.Currency,
                response.Product.Item,
                response.Product.UserItems,
                response.Product.LiveWallPapers,
                response.Product.UserTitleList)
            && purchase.Product.GoodsType == RewardType.Character)
        {
            try
            {
                var reward = new NetRewardData();
                RewardUtils.AddSingleObject(user, ref reward, purchase.Product.GoodsId, RewardType.Character, purchase.Quantity);
                response.Product.Character.Add(reward.Character);
                response.Product.UserCharacters.AddRange(reward.UserCharacters);

                if (reward.UserItems.Count > 0)
                {
                    foreach (var spareItem in reward.Item)
                    {
                        response.Item = new NetUserItemData
                        {
                            Isn = spareItem.Isn,
                            Tid = spareItem.Tid,
                            Count = spareItem.Count,
                            Corporation = spareItem.Corporation
                        };
                        response.Product.Item.AddRange(reward.Item);
                        response.Product.UserItems.AddRange(reward.UserItems);
                        response.Result = ShopBuyProductResult.Success;
                        Console.WriteLine("Got: {0}", spareItem.Tid);
                    }
                }

                if (reward.Currency.Count > 0)
                {
                    foreach (var currency in reward.Currency)
                    {
                        response.Currencies.Add(new NetUserCurrencyData
                        {
                            Type = currency.Type,
                            Value = user.GetCurrencyVal((CurrencyType)currency.Type)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Buy Unit Ex: {0}", ex.Message);
            }
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResGetShop GetShop(User user, ReqGetShop req)
    {
        return new ResGetShop
        {
            Shop = NormalShopHelper.GetShopData(user, req.ShopCategory)
        };
    }

    public ResShopProductList GetShopProductList(User user)
    {
        ResShopProductList response = new();
        response.Shops.AddRange(NormalShopHelper.GetAllShopData(user));
        return response;
    }

    public ResShopRenew RenewShop(User user, ReqShopRenew req)
    {
        return new ResShopRenew
        {
            Shop = NormalShopHelper.GetShopData(user, req.ShopCategory, reroll: true)
        };
    }

    public ResGetJupiterProductList GetJupiterProductList(ReqGetJupiterProductList req)
    {
        ResGetJupiterProductList response = new();
        foreach (string? item in req.ProductIdList)
        {
            IEnumerable<KeyValuePair<string, MidasProductRecord>> product =
                GameData.Instance.mediasProductTable.Where(x => x.Key == item);

            if (product.Any())
            {
                MidasProductRecord? record = product.FirstOrDefault().Value;
                if (record != null)
                {
                    string normalizedCost = record.Cost.Replace(',', '.');
                    if (!decimal.TryParse(normalizedCost, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                    {
                        Logging.WriteLine(
                            $"Failed to parse '{record.Cost}' (normalized as '{normalizedCost}'). Cash shop will not work properly.",
                            LogType.Error);
                        continue;
                    }

                    long microPrice = (long)(price * 1000000);
                    response.ProductInfoList.Add(new NetJupiterProductInfo
                    {
                        CurrencyCode = "USD",
                        CurrencySymbol = "$",
                        MicroPrice = microPrice,
                        Price = record.Cost,
                        ProductId = item
                    });
                }
            }
            else
            {
                Console.WriteLine($"Missing!!!! {item}");
            }
        }

        return response;
    }

    public ResListSeenProductOffer GetProductOffers()
    {
        ResListSeenProductOffer response = new();
        foreach (KeyValuePair<int, ProductOfferRecord> item in GameData.Instance.ProductOffers)
            response.Result.Add(new NetUserProductOfferSeenHistory { ProductOfferId = item.Key });

        return response;
    }

    public ResBuyJupiterProduct BuyJupiterProduct(User user, ReqBuyJupiterProduct req)
    {
        bool success = InAppPurchaseHelper.TrySimulatePurchase(user, req.ProductId, req.ExtraData, out _);
        return new ResBuyJupiterProduct
        {
            ReferenceId = success ? $"dev-{Guid.NewGuid():N}" : string.Empty,
            RedirectUrl = string.Empty
        };
    }

    public ResGetInAppShopBuyProduct GetInAppBuyProduct(ulong userId, ReqGetInAppShopBuyProduct req)
    {
        return new ResGetInAppShopBuyProduct
        {
            Reward = InAppPurchaseHelper.TakePendingReward(userId, req.ProductId),
            PurchasePointResult = new NetInAppShopPurchasePointAcquireResult()
        };
    }

    public ResGetInAppShopData GetInAppShopData()
    {
        ResGetInAppShopData response = new();
        var now = DateTime.UtcNow;

        var activeCostumeShopGroups = GameData.Instance.CostumeShopTable.Values
            .Where(x => x.IsActive)
            .Where(x => x.StartDate == default || x.EndDate == default ||
                        (x.StartDate <= now && now <= x.EndDate))
            .Select(x => x.ShopGroupId)
            .ToHashSet();

        var activeManagers = GameData.Instance.InAppShopManagerTable.Values
            .Where(x => !x.IsHideIfNotValid ||
                        (x.ShopCategory == InAppShopCategory.CostumeShop &&
                         activeCostumeShopGroups.Contains(x.PackageShopId)))
            .Where(x => x.StartDate == default || x.EndDate == default ||
                        (x.StartDate <= now && now <= x.EndDate))
            .OrderBy(x => x.Id)
            .ToList();

        foreach (var manager in activeManagers)
        {
            response.InAppShopDataList.Add(new NetInAppShopData
            {
                Id = manager.Id,
                StartDate = manager.StartDate == default ? now.Date.Ticks : manager.StartDate.Ticks,
                EndDate = manager.EndDate == default ? now.Date.AddDays(2).Ticks : manager.EndDate.Ticks
            });
        }

        if (response.InAppShopDataList.Count == 0)
        {
            response.InAppShopDataList.Add(new NetInAppShopData
            {
                Id = 10001,
                StartDate = now.Date.Ticks,
                EndDate = now.Date.AddDays(2).Ticks
            });
        }

        return response;
    }

    public ResStartPurchase StartPurchase(User user, ReqStartPurchase req)
    {
        bool success = InAppPurchaseHelper.TrySimulatePurchase(user, req.ProductId, req.ExtraData, out _);
        return new ResStartPurchase
        {
            Result = success ? StartPurchaseResult.Ok : StartPurchaseResult.PayChannelNotAvailable,
            TransactionId = success ? $"dev-{Guid.NewGuid():N}" : string.Empty
        };
    }

    public ResGetPopupPackageState GetPackagePopupState()
    {
        ResGetPopupPackageState response = new();
        foreach (KeyValuePair<int, PopupPackageListRecord> item in GameData.Instance.PopupPackages)
            response.AppearedList.Add(item.Key);

        return response;
    }

    // --- Shared Helpers ---

    private NetGachaEntityData GrantGachaCharacterOrPiece(
        User user,
        CharacterRecord characterData,
        RepeatedField<NetUserCharacterDefaultData> charactersOut,
        RepeatedField<NetUserItemData> itemsOut,
        ref bool obtainedNewCharacter,
        ref int totalBodyLabels,
        out bool addedNewCharacter,
        out bool grantedPieceBeforeIncrement)
    {
        addedNewCharacter = false;
        grantedPieceBeforeIncrement = false;

        NetGachaEntityData gacha = new()
        {
            CurrencyValue = 0,
            Tid = characterData.Id,
            Type = 1
        };

        if (user.HasCharacter(characterData.Id))
        {
            CharacterModel character = user.GetCharacter(characterData.Id)
                ?? throw new Exception("HasCharacter() returned true, however character was null");
            DbItemData? existingItem = user.Items.FirstOrDefault(item => item.ItemType == characterData.PieceId);

            charactersOut.Add(new NetUserCharacterDefaultData
            {
                CostumeId = character.CostumeId,
                Csn = character.Csn,
                Grade = character.Grade,
                Lv = character.Level,
                UltiSkillLv = character.UltimateLevel,
                Skill1Lv = character.Skill1Lvl,
                Skill2Lv = character.Skill2Lvl,
                Tid = characterData.Id
            });

            gacha.Sn = character.Csn;
            gacha.Tid = characterData.Id;

            bool increaseItem = false;
            if (characterData.OriginalRare == OriginalRareType.SR)
            {
                if ((existingItem != null && character.Grade + existingItem.Count <= 1) ||
                    (existingItem == null && character.Grade <= 1))
                    increaseItem = true;
            }
            else if (characterData.OriginalRare == OriginalRareType.SSR)
            {
                if ((existingItem != null && character.Grade + existingItem.Count <= 10) ||
                    (existingItem == null && character.Grade <= 10))
                    increaseItem = true;
            }

            if (increaseItem)
            {
                grantedPieceBeforeIncrement = true;
                gacha.PieceCount = 1;
                if (existingItem != null)
                {
                    existingItem.Count++;
                    itemsOut.Add(ToNetItem(existingItem));
                }
                else
                {
                    DbItemData newItem = new()
                    {
                        ItemType = characterData.PieceId,
                        Csn = 0,
                        Count = 1,
                        Level = 0,
                        Exp = 0,
                        Position = 0,
                        Corp = 0,
                        Isn = user.GenerateUniqueItemId()
                    };
                    user.Items.Add(newItem);
                    itemsOut.Add(ToNetItem(newItem));
                }
            }
            else
            {
                gacha.CurrencyValue = characterData.OriginalRare == OriginalRareType.SSR
                    ? 6000
                    : (characterData.OriginalRare == OriginalRareType.SR ? 200 : 150);
                user.AddCurrency(CurrencyType.DissolutionPoint, gacha.CurrencyValue);
                totalBodyLabels += (int)gacha.CurrencyValue;
            }
        }
        else
        {
            addedNewCharacter = true;
            obtainedNewCharacter = true;
            gacha.Sn = user.GenerateUniqueCharacterId();
            charactersOut.Add(new NetUserCharacterDefaultData
            {
                CostumeId = 0,
                Csn = gacha.Sn,
                Grade = 0,
                Lv = 1,
                Skill1Lv = 1,
                Skill2Lv = 1,
                Tid = characterData.Id,
                UltiSkillLv = 1
            });

            user.Characters.Add(new CharacterModel
            {
                CostumeId = 0,
                Csn = (int)gacha.Sn,
                Grade = 0,
                Level = 1,
                Skill1Lvl = 1,
                Skill2Lvl = 1,
                Tid = characterData.Id,
                UltimateLevel = 1
            });

            user.AddBadge(BadgeContents.NikkeNew, characterData.NameCode.ToString());
            user.AddTrigger(Trigger.ObtainCharacter, 1, characterData.NameCode);
            user.AddTrigger(Trigger.ObtainCharacterNew, 1);
            if (characterData.OriginalRare == OriginalRareType.SSR)
                user.AddTrigger(Trigger.ObtainCharacterSSR, 1);

            if (characterData.OriginalRare == OriginalRareType.SSR || characterData.OriginalRare == OriginalRareType.SR)
                bondRepo.EnsureBond(user.ID, characterData.NameCode, 1);
        }

        return gacha;
    }

    private bool GrantCommonShopProduct(
        User user,
        ContentsShopProductRecord product,
        int quantity,
        RepeatedField<NetCurrencyData> currenciesOut,
        RepeatedField<NetItemData> itemsOut,
        RepeatedField<NetUserItemData> userItemsOut,
        RepeatedField<int> liveWallpapersOut,
        RepeatedField<int> userTitlesOut)
    {
        int totalValue = product.GoodsValue * quantity;

        if (product.GoodsType == RewardType.Currency)
        {
            user.AddCurrency((CurrencyType)product.GoodsId, totalValue);
            currenciesOut.Add(new NetCurrencyData
            {
                Type = product.GoodsId,
                Value = totalValue,
                FinalValue = user.GetCurrencyVal((CurrencyType)product.GoodsId)
            });
            return true;
        }

        if (product.GoodsType == RewardType.Item || product.GoodsType.ToString().StartsWith("Equipment"))
        {
            var existing = user.Items.FirstOrDefault(i => i.ItemType == product.GoodsId);
            if (existing != null)
            {
                existing.Count += totalValue;
                itemsOut.Add(NetUtils.ItemDataToNet(existing, totalValue));
                userItemsOut.Add(NetUtils.UserItemDataToNet(existing));
            }
            else
            {
                var newItem = new DbItemData
                {
                    ItemType = product.GoodsId,
                    Count = totalValue,
                    Isn = user.GenerateUniqueItemId()
                };
                user.Items.Add(newItem);
                itemsOut.Add(NetUtils.ItemDataToNet(newItem, totalValue));
                userItemsOut.Add(NetUtils.UserItemDataToNet(newItem));
            }
            return true;
        }

        if (product.GoodsType == RewardType.LiveWallpaper)
        {
            if (cosmeticsRepo.UnlockCosmetic(user.ID, CosmeticsRepository.CategoryLiveWallpaper, product.GoodsId))
                db.SaveChanges();
            liveWallpapersOut.Add(product.GoodsId);
            return true;
        }

        if (product.GoodsType == RewardType.UserTitle)
        {
            if (cosmeticsRepo.UnlockCosmetic(user.ID, CosmeticsRepository.CategoryTitle, product.GoodsId))
                db.SaveChanges();
            userTitlesOut.Add(product.GoodsId);
            return true;
        }

        return false;
    }

    private static NetUserItemData ToNetItem(DbItemData item) => new()
    {
        Tid = item.ItemType,
        Csn = item.Csn,
        Count = item.Count,
        Lv = item.Level,
        Exp = item.Exp,
        Position = item.Position,
        Isn = item.Isn
    };
}
