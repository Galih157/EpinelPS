using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Character;
using EpinelPS.LobbyServer.Stage;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.Collections;

namespace EpinelPS.Services;

public sealed class InventoryCharacterService(
    GameContext db,
    InventoryRepository inventoryRepo,
    BondLiberationRepository bondRepo)
{
    private static readonly Dictionary<int, int> BoostExpTable = new()
    {
        { 7010001, 100 },
        { 7010002, 1000 },
        { 7010003, 10000 }
    };

    // --- Character ---

    public ResGetCharacterCostumeData GetCharacterCostume()
    {
        ResGetCharacterCostumeData response = new();
        response.CostumeIds.AddRange(GameData.Instance.GetAllCostumes());
        return response;
    }

    public ResGetCharacterData GetCharacterData(User user)
    {
        if (user.Characters.Count == 0 && UserDbHelper.GetOrCreateGameUser(db, user.ID).LastNormalStageCleared >= 6000002)
        {
            ClearStage.EnsureDefaultCharacters(user);
        }

        ResGetCharacterData response = new();
        foreach (CharacterModel item in user.Characters)
        {
            response.Character.Add(new NetUserCharacterData
            {
                Default = new NetUserCharacterDefaultData
                {
                    Csn = item.Csn,
                    Skill1Lv = item.Skill1Lvl,
                    Skill2Lv = item.Skill2Lvl,
                    CostumeId = item.CostumeId,
                    Lv = user.GetCharacterLevel(item.Csn, item.Level),
                    Grade = item.Grade,
                    Tid = item.Tid,
                    UltiSkillLv = item.UltimateLevel
                },
                IsSynchro = user.GetSynchro(item.Csn)
            });

            if (item.IsMainForce)
            {
                response.MainForceCsnList.Add(item.Csn);
            }
        }

        foreach (CharacterModel c in user.Characters.OrderByDescending(x => x.Level).Take(5))
        {
            response.SynchroStandardCharacters.Add(c.Csn);
        }

        return response;
    }

    public ResCharacterUpgrade UpgradeCharacter(User user, ReqCharacterUpgrade req)
    {
        ResCharacterUpgrade response = new();
        var (character, item, _) = UpgradeCharacterGrade(user, req.Csn, req.Isn, req.Count, throwIfRecordMissing: true);
        if (character != null && item != null)
        {
            response.Character = character;
            response.Items.Add(item);
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResCharacterCoreUpgrade UpgradeCharacterCore(User user, ReqCharacterCoreUpgrade req)
    {
        ResCharacterCoreUpgrade response = new();
        var (character, item, newGradeCoreId) = UpgradeCharacterGrade(user, req.Csn, req.Isn, req.Count, throwIfRecordMissing: false);
        if (character != null && item != null)
        {
            response.Character = character;
            response.Items.Add(item);

            if (IsMaxGradeCoreId(newGradeCoreId))
            {
                user.AddTrigger(Trigger.CharacterGradeMax, 1);
            }

            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResCharacterLevelUp LevelUpCharacter(User user, ReqCharacterLevelUp req)
    {
        ResCharacterLevelUp response = new();
        Dictionary<int, CharacterLevelRecord> data = GameData.Instance.GetCharacterLevelUpData();

        foreach (CharacterModel item in user.Characters.ToArray())
        {
            if (item.Csn != req.Csn)
            {
                continue;
            }

            var (requiredCredit, requiredBattleData, requiredCoreDust) = CalculateLevelUpCost(data, item.Level, req.Lv);

            if (user.CanSubtractCurrency(CurrencyType.Gold, requiredCredit) &&
                user.CanSubtractCurrency(CurrencyType.CharacterExp, requiredBattleData) &&
                user.CanSubtractCurrency(CurrencyType.CharacterExp2, requiredCoreDust))
            {
                user.SubtractCurrency(CurrencyType.Gold, requiredCredit);
                user.SubtractCurrency(CurrencyType.CharacterExp, requiredBattleData);
                user.SubtractCurrency(CurrencyType.CharacterExp2, requiredCoreDust);
                item.Level = req.Lv;
            }
            else
            {
                Logging.WriteLine("ERROR: Not enough currency for upgrade", LogType.WarningAntiCheat);
                return response;
            }

            response.Character = ToNetCharacterDefault(item);
            response.SynchroLv = user.GetSynchroLevel();

            foreach (CharacterModel c in user.Characters.OrderByDescending(x => x.Level).Take(5))
            {
                response.SynchroStandardCharacters.Add(c.Csn);
            }

            AppendUserCurrencies(user, response.Currencies);
            break;
        }

        user.AddTrigger(Trigger.CharacterLevelUpCount, 1);
        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResCharacterGrowReset ResetCharacterLevel(User user, ReqCharacterGrowReset req)
    {
        ResCharacterGrowReset response = new();
        Dictionary<int, CharacterLevelRecord> data = GameData.Instance.GetCharacterLevelUpData();

        foreach (CharacterModel item in user.Characters.ToArray())
        {
            if (item.Csn != req.Csn)
            {
                continue;
            }

            if (item.Level == 1)
            {
                Logging.WriteLine("Character level is already 1 - cannot reset", LogType.WarningAntiCheat);
                return response;
            }

            if (item.Level == 200)
            {
                Logging.WriteLine("Character level is 200 - cannot reset", LogType.WarningAntiCheat);
                return response;
            }

            var (refundCredit, refundBattleData, refundCoreDust) = CalculateLevelUpCost(data, 1, item.Level);

            user.AddCurrency(CurrencyType.Gold, refundCredit);
            user.AddCurrency(CurrencyType.CharacterExp, refundBattleData);
            user.AddCurrency(CurrencyType.CharacterExp2, refundCoreDust);
            item.Level = 1;

            response.Character = ToNetCharacterDefault(item);

            List<CharacterModel> highestLevelCharacters = [.. user.Characters.OrderByDescending(x => x.Level).Take(5)];
            response.SynchroLv = highestLevelCharacters.Last().Level;

            foreach (CharacterModel c in highestLevelCharacters)
            {
                response.SynchroStandardCharacters.Add(c.Csn);
            }

            AppendUserCurrencies(user, response.Currencies);
            break;
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResSetCharacterCostume SetCharacterCostume(User user, ReqSetCharacterCostume req)
    {
        foreach (CharacterModel item in user.Characters)
        {
            if (item.Csn == req.Csn)
            {
                item.CostumeId = req.CostumeId;
                break;
            }
        }

        UserSessionStore.SaveUser(user);
        return new ResSetCharacterCostume();
    }

    public ResSetCharacterMainForce SetCharacterMainForce(User user, ReqSetCharacterMainForce req)
    {
        foreach (CharacterModel item in user.Characters)
        {
            if (item.Csn == req.Csn)
            {
                item.IsMainForce = req.IsMainForce;
                break;
            }
        }

        UserSessionStore.SaveUser(user);
        return new ResSetCharacterMainForce();
    }

    public ResCharacterSkillLevelUp SkillLevelUp(User user, ReqCharacterSkillLevelUp req)
    {
        ResCharacterSkillLevelUp response = new();
        SkillLevelUpResult result = SkillLevelUpHelper.Upgrade(user, req.Csn, (CharacterSkillCategory)req.Category);
        response.Character = result.Character;
        response.Items.AddRange(result.Items);
        response.Currencies.AddRange(result.Currencies);

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResCharacterSkillLevelUpV2 SkillLevelUpV2(User user, ReqCharacterSkillLevelUpV2 req)
    {
        ResCharacterSkillLevelUpV2 response = new();
        SkillLevelUpResult result = SkillLevelUpHelper.Upgrade(user, req.Csn, req.SkillCategory, req.TargetLevel);
        response.Character = result.Character;
        response.Items.AddRange(result.Items);
        response.Currencies.AddRange(result.Currencies);

        UserSessionStore.SaveUser(user);
        return response;
    }

    // --- Wallet ---

    public ResGetCurrencyData GetWallet(User user)
    {
        ResGetCurrencyData response = new();
        AppendUserCurrencies(user, response.Currency);
        return response;
    }

    public ResRefreshChargeCurrencyData WalletRefreshCharge(User user)
    {
        ResRefreshChargeCurrencyData response = new()
        {
            FreeCash = new NetUserCurrencyData { Type = (int)CurrencyType.FreeCash },
            ChargeCash = new NetUserCurrencyData { Type = (int)CurrencyType.ChargeCash }
        };

        foreach (KeyValuePair<CurrencyType, long> item in user.Currency)
        {
            if (item.Key == CurrencyType.FreeCash)
            {
                response.FreeCash.Type = (int)item.Key;
                response.FreeCash.Value = item.Value;
            }
            else if (item.Key == CurrencyType.ChargeCash)
            {
                response.ChargeCash.Type = (int)item.Key;
                response.ChargeCash.Value = item.Value;
            }
        }

        return response;
    }

    // --- Inventory & Equipment ---

    public ResGetInventoryData GetInventoryData(User user)
    {
        ResGetInventoryData response = new();
        foreach (DbItemData item in user.Items)
        {
            ItemSubType itemSubType = GameData.Instance.GetItemSubType(item.ItemType);
            if (itemSubType == ItemSubType.HarmonyCube)
            {
                NetUserHarmonyCubeData harmonyCubeData = new()
                {
                    Tid = item.ItemType,
                    Lv = item.Level,
                    Isn = item.Isn
                };
                harmonyCubeData.CsnList.AddRange(item.CsnList);
                response.HarmonyCubes.Add(harmonyCubeData);
            }

            response.Items.Add(new NetUserItemData
            {
                Count = item.Count,
                Tid = item.ItemType,
                Csn = item.Csn,
                Lv = item.Level,
                Exp = item.Exp,
                Corporation = item.Corp,
                Isn = item.Isn,
                Position = item.Position
            });
        }

        foreach (EquipmentAwakeningData awakening in inventoryRepo.GetAwakenings(user.ID))
        {
            response.Awakenings.Add(new NetEquipmentAwakening
            {
                Isn = awakening.Isn,
                Option = awakening.Option
            });
        }

        return response;
    }

    public ResAllClearEquipment AllClearEquipment(User user, ReqAllClearEquipment req)
    {
        ResAllClearEquipment response = new()
        {
            Csn = req.Csn
        };

        foreach (DbItemData item in user.Items.ToArray())
        {
            if (item.Csn == req.Csn)
            {
                if (IsT10Equipment(item.ItemType))
                {
                    response.Items.Add(NetUtils.ToNet(item));
                    continue;
                }

                item.Csn = 0;
                item.Position = 0;
                response.Items.Add(NetUtils.ToNet(item));
            }
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResClearEquipment ClearEquipment(User user, ReqClearEquipment req)
    {
        ResClearEquipment response = new();

        foreach (DbItemData item in user.Items.ToArray())
        {
            if (item.Isn == req.Isn)
            {
                item.Csn = 0;
                response.Item = NetUtils.ToNet(item);
                break;
            }
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResWearEquipment WearEquipment(User user, ReqWearEquipment req)
    {
        ResWearEquipment response = new();
        int pos = NetUtils.GetItemPos(user, req.Isn);

        foreach (DbItemData item in user.Items.ToArray())
        {
            if (item.Csn == req.Csn && item.Position == pos)
            {
                item.Csn = 0;
            }
        }

        foreach (DbItemData item in user.Items.ToArray())
        {
            if (item.Isn == req.Isn)
            {
                item.Csn = req.Csn;
                item.Position = pos;
                response.Items.Add(NetUtils.ToNet(item));
                break;
            }
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResWearEquipmentList WearEquipmentList(User user, ReqWearEquipmentList req)
    {
        ResWearEquipmentList response = new();

        foreach (long item2 in req.IsnList)
        {
            int pos = NetUtils.GetItemPos(user, item2);

            DbItemData? itemToCheck = user.Items.FirstOrDefault(x => x.Isn == item2);
            if (itemToCheck != null && IsT10Equipment(itemToCheck.ItemType))
            {
                bool hasT10InPosition = user.Items.Any(x => x.Position == pos && x.Csn == req.Csn && IsT10Equipment(x.ItemType));
                if (hasT10InPosition)
                {
                    continue;
                }
            }

            itemToCheck = user.Items.FirstOrDefault(x => x.Isn == item2);
            if (itemToCheck == null)
            {
                continue;
            }

            foreach (DbItemData item in user.Items.ToArray())
            {
                if (item.Position == pos && item.Csn == req.Csn)
                {
                    if (IsT10Equipment(item.ItemType))
                    {
                        continue;
                    }

                    item.Csn = 0;
                    item.Position = 0;
                    response.Items.Add(NetUtils.ToNet(item));
                }
            }

            DbItemData? targetItem = user.Items.FirstOrDefault(x => x.Isn == item2);
            if (targetItem != null)
            {
                DbItemData equippedItem;
                if (targetItem.Count > 1)
                {
                    targetItem.Count--;
                    response.Items.Add(NetUtils.ToNet(targetItem));

                    equippedItem = new DbItemData
                    {
                        ItemType = targetItem.ItemType,
                        Isn = user.GenerateUniqueItemId(),
                        Level = targetItem.Level,
                        Exp = targetItem.Exp,
                        Count = 1,
                        Corp = targetItem.Corp,
                        Position = pos
                    };
                    user.Items.Add(equippedItem);
                }
                else
                {
                    equippedItem = targetItem;
                }

                equippedItem.Csn = req.Csn;
                equippedItem.Position = pos;
                response.Items.Add(NetUtils.ToNet(equippedItem));
            }
        }

        foreach (long requestedIsn in req.IsnList)
        {
            bool requestedItemAdded = response.Items.Any(x => x.Isn == requestedIsn);
            if (!requestedItemAdded)
            {
                DbItemData? requestedItem = user.Items.FirstOrDefault(x => x.Isn == requestedIsn);
                if (requestedItem != null)
                {
                    response.Items.Add(NetUtils.ToNet(requestedItem));
                }
                else
                {
                    response.Items.Add(new NetUserItemData
                    {
                        Isn = requestedIsn,
                        Count = 0
                    });
                }
            }
        }

        foreach (DbItemData item in user.Items)
        {
            if (item.Csn == req.Csn && item.Csn != 0)
            {
                bool alreadyAdded = response.Items.Any(x => x.Isn == item.Isn);
                if (!alreadyAdded)
                {
                    response.Items.Add(NetUtils.ToNet(item));
                }
            }
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResIncreaseExpEquip IncreaseEquipmentExp(User user, ReqIncreaseExpEquip req)
    {
        ResIncreaseExpEquip response = new();
        DbItemData destItem = user.Items.FirstOrDefault(x => x.Isn == req.Isn) ?? throw new NullReferenceException();
        int goldCost = 0;
        int modules = 0;

        foreach (NetItemData srcItem in req.ItemList)
        {
            DbItemData item = user.Items.FirstOrDefault(x => x.Isn == srcItem.Isn) ?? throw new NullReferenceException();
            item.Count -= srcItem.Count;

            (int addedExp, int addedModules) = AddEquipExp(srcItem, destItem);
            goldCost += addedExp;
            modules += addedModules;

            response.Items.Add(NetUtils.ToNet(item));
        }

        response.Currency = new NetUserCurrencyData
        {
            Type = (int)CurrencyType.Gold,
            Value = user.GetCurrencyVal(CurrencyType.Gold) - MathUtils.Clamp(goldCost, 0, CalcTotalEquipExp(destItem))
        };

        NetRewardData ret = new();
        if (modules > 0)
        {
            (int t1, int t2, int t3) = CalcBoostModules(modules);

            if (t1 > 0)
                RewardUtils.AddSingleObject(user, ref ret, 7010001, RewardType.Item, t1);
            if (t2 > 0)
                RewardUtils.AddSingleObject(user, ref ret, 7010002, RewardType.Item, t2);
            if (t3 > 0)
                RewardUtils.AddSingleObject(user, ref ret, 7010003, RewardType.Item, t3);

            response.Reward = ret;
        }

        // Target item must be in delta list so the client UI updates.
        response.Items.Add(NetUtils.ToNet(destItem));

        user.AddTrigger(Trigger.EquipItemLevelCount, 1);
        UserSessionStore.SaveUser(user);
        return response;
    }

    // --- Consumables & Pieces ---

    public ResQuickUseItem QuickUseItem(User user, ReqQuickUseItem req)
    {
        ResQuickUseItem response = new();
        List<NetUserItemData> rewardUserItems = [];
        List<NetRewardData> rewards = [];

        foreach (NetQuickUseData item in req.QuickUse)
        {
            DbItemData itemInInventory = user.Items.FirstOrDefault(x => x.Isn == item.Isn)
                ?? throw new InvalidDataException("cannot find item with isn " + item.Isn);

            int totalItems = item.Count;
            if (totalItems > itemInInventory.Count)
                throw new Exception("count mismatch");

            itemInInventory.Count -= totalItems;
            if (itemInInventory.Count == 0)
                user.Items.Remove(itemInInventory);

            ItemConsumeRecord cItem = GameData.Instance.ConsumableItems
                .FirstOrDefault(x => x.Value.Id == itemInInventory.ItemType).Value
                ?? throw new Exception("cannot find BundleBox Id " + itemInInventory.Isn);

            Console.WriteLine($"Item: {item.Isn}, type {cItem.UseType} and quickuseItemType {cItem.QuickUseType},\n count {item.Count}/{itemInInventory.Count}");
            switch (cItem.UseType)
            {
                case ItemUseType.BundleBox:
                    rewards.Add(NetUtils.UseBundleBox(user, itemInInventory.ItemType, totalItems));
                    rewardUserItems.Add(NetUtils.UserItemDataToNet(itemInInventory));
                    break;
                case ItemUseType.ItemRandomBox:
                    rewards.Add(NetUtils.UseLootBox(user, itemInInventory.ItemType, totalItems));
                    rewardUserItems.Add(NetUtils.UserItemDataToNet(itemInInventory));
                    break;
            }
        }

        NetRewardData reward = new();
        rewards.ForEach(x => reward.MergeFrom(x));
        response.Reward = reward;
        response.Reward.UserItems.AddRange(rewardUserItems);

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResUseBundleBox UseBundleBox(User user, ReqUseBundleBox req)
    {
        ResUseBundleBox response = new();

        DbItemData bundleBox = user.Items.FirstOrDefault(x => x.Isn == req.Isn)
            ?? throw new InvalidDataException("cannot find bundleBox with isn " + req.Isn);

        int totalBundleBoxes = req.Count;
        if (totalBundleBoxes > bundleBox.Count)
            throw new Exception("count mismatch");

        bundleBox.Count -= totalBundleBoxes;
        if (bundleBox.Count == 0)
            user.Items.Remove(bundleBox);

        response.Reward = NetUtils.UseBundleBox(user, bundleBox.ItemType, req.Count);
        response.Reward.UserItems.Add(NetUtils.UserItemDataToNet(bundleBox));

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResUseRandomBox UseRandomBox(User user, ReqUseRandomBox req)
    {
        ResUseRandomBox response = new();

        DbItemData box = user.Items.FirstOrDefault(x => x.Isn == req.Isn)
            ?? throw new InvalidDataException("cannot find box with isn " + req.Isn);

        if (!GameData.Instance.ConsumableItems.TryGetValue(box.ItemType, out ItemConsumeRecord? item) || item == null)
            throw new Exception("cannot find box Id " + box.ItemType);

        if (req.Count * item.UseFragCost > box.Count)
            throw new Exception("count mismatch");

        box.Count -= req.Count * item.UseFragCost;
        if (box.Count == 0)
            user.Items.Remove(box);

        response.Reward = NetUtils.UseLootBox(user, box.ItemType, req.Count);
        response.Reward.UserItems.Add(NetUtils.UserItemDataToNet(box));

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResUseSelectBox UseSelectBox(User user, ReqUseSelectBox req)
    {
        ResUseSelectBox response = new();

        DbItemData selectBox = user.Items.FirstOrDefault(x => x.Isn == req.Isn)
            ?? throw new InvalidDataException("cannot find selectBox with isn " + req.Isn);

        int totalSelectBoxes = req.Select.Sum(x => x.Count);
        if (totalSelectBoxes > selectBox.Count)
            throw new Exception("count mismatch");

        selectBox.Count -= totalSelectBoxes;
        if (selectBox.Count == 0)
            user.Items.Remove(selectBox);

        response.Reward = NetUtils.UseSelectBox(user, selectBox.ItemType, req.Select);
        response.Reward.UserItems.Add(NetUtils.UserItemDataToNet(selectBox));

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResUseTimeReward UseTimeReward(User user, ReqUseTimeReward req)
    {
        ResUseTimeReward response = new();

        DbItemData timeReward = user.Items.FirstOrDefault(x => x.Isn == req.Isn)
            ?? throw new InvalidDataException("cannot find time reward with isn " + req.Isn);

        if (req.Count > timeReward.Count)
            throw new Exception("count mismatch");

        timeReward.Count -= req.Count;
        if (timeReward.Count == 0)
            user.Items.Remove(timeReward);

        ItemConsumeRecord cItem = GameData.Instance.ConsumableItems
            .FirstOrDefault(x => x.Value.Id == timeReward.ItemType).Value
            ?? throw new Exception("cannot find box Id " + timeReward.ItemType);

        (CurrencyType itemType, long amount) = cItem.UseId switch
        {
            1 => (CurrencyType.Gold, NetUtils.GetOutpostRewardAmount(user, CurrencyType.Gold, TimeSpan.FromSeconds(cItem.UseValue).TotalMinutes, false)),
            2 => (CurrencyType.CharacterExp, NetUtils.GetOutpostRewardAmount(user, CurrencyType.CharacterExp, TimeSpan.FromSeconds(cItem.UseValue).TotalMinutes, false)),
            4 => (CurrencyType.CharacterExp2, NetUtils.GetOutpostRewardAmount(user, CurrencyType.CharacterExp2, TimeSpan.FromSeconds(cItem.UseValue).TotalMinutes, false)),
            _ => throw new Exception("unknown UseId " + cItem.UseId)
        };

        NetRewardData reward = new();
        RewardUtils.AddSingleCurrencyObject(user, ref reward, itemType, amount * req.Count);

        response.Reward = reward;
        response.Reward.UserItems.Add(NetUtils.UserItemDataToNet(timeReward));

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResUsePiece UsePiece(User user, ReqUsePiece req)
    {
        ResUsePiece response = new();

        DbItemData piece = user.Items.FirstOrDefault(x => x.Isn == req.Isn)
            ?? throw new InvalidDataException("cannot find piece with isn " + req.Isn);

        if (req.Count * 50 > piece.Count)
            throw new Exception("count mismatch");

        piece.Count -= req.Count * 50;
        if (piece.Count == 0)
            user.Items.Remove(piece);

        ItemPieceRecord pItem = GameData.Instance.PieceItems
            .FirstOrDefault(x => x.Value.Id == piece.ItemType).Value
            ?? throw new Exception("cannot find piece Id " + piece.ItemType);

        IEnumerable<GachaListProbRecord> probList = GameData.Instance.GachaGradeProb
            .Where(gradeProb => gradeProb.Key == pItem.UseId)
            .SelectMany(grade => GameData.Instance.GachaListProb.Where(list => list.Value.GroupId == grade.Value.GachaListId))
            .Select(i => i.Value);

        NetRewardData reward = new();
        IEnumerable<CharacterRecord> selectedCharacters = Enumerable.Range(1, req.Count)
            .Select(_ => SelectRandomCharacter(probList));

        int totalBodyLabels = 0;
        foreach (CharacterRecord character in selectedCharacters)
        {
            DbItemData? spareItem = user.Items.FirstOrDefault(i => i.ItemType == character.PieceId);

            if (user.GetCharacter(character.Id) is CharacterModel ownedCharacter)
            {
                int maxLimitBroken = RewardUtils.GetValueByRarity(character.OriginalRare, 0, 2, 11);
                bool canIncreaseItem = character.OriginalRare != OriginalRareType.R && ownedCharacter.Grade + (spareItem?.Count ?? 0) < maxLimitBroken;
                (int newSpareItemCount, int dissoluteCharacterCount) = canIncreaseItem ? (1, 0) : (0, 1);

                if (canIncreaseItem)
                {
                    if (spareItem != null)
                    {
                        spareItem.Count = newSpareItemCount;
                    }
                    else
                    {
                        spareItem = new DbItemData
                        {
                            ItemType = character.PieceId,
                            Csn = 0,
                            Count = newSpareItemCount,
                            Level = 0,
                            Exp = 0,
                            Position = 0,
                            Corp = 0,
                            Isn = user.GenerateUniqueItemId()
                        };
                        user.Items.Add(spareItem);
                    }

                    reward.UserItems.Add(NetUtils.UserItemDataToNet(spareItem));
                    reward.Character.Add(RewardUtils.GetNetCharacterData(ownedCharacter));
                }
                else
                {
                    int bodyLabel = RewardUtils.GetValueByRarity(character.OriginalRare, 150, 200, 6000);
                    totalBodyLabels += bodyLabel * dissoluteCharacterCount;
                    reward.Character.Add(RewardUtils.GetNetCharacterData(ownedCharacter, bodyLabel));
                }
            }
            else
            {
                int csn = user.GenerateUniqueCharacterId();
                reward.UserCharacters.Add(new NetUserCharacterDefaultData
                {
                    CostumeId = 0,
                    Csn = csn,
                    Grade = 0,
                    Lv = 1,
                    Skill1Lv = 1,
                    Skill2Lv = 1,
                    Tid = character.Id,
                    UltiSkillLv = 1
                });
                reward.Character.Add(new NetCharacterData
                {
                    Csn = user.GenerateUniqueCharacterId(),
                    Tid = character.Id
                });
                user.Characters.Add(new CharacterModel
                {
                    CostumeId = 0,
                    Csn = csn,
                    Grade = 0,
                    Level = 1,
                    Skill1Lvl = 1,
                    Skill2Lvl = 1,
                    Tid = character.Id,
                    UltimateLevel = 1
                });

                user.AddBadge(BadgeContents.NikkeNew, character.NameCode.ToString());
                user.AddTrigger(Trigger.ObtainCharacter, 1, character.NameCode);
                if (character.OriginalRare == OriginalRareType.SSR)
                {
                    user.AddTrigger(Trigger.ObtainCharacterSSR, 1);
                }
                else
                {
                    user.AddTrigger(Trigger.ObtainCharacterNew, 1);
                }

                if (character.OriginalRare == OriginalRareType.SSR || character.OriginalRare == OriginalRareType.SR)
                {
                    bondRepo.EnsureBond(user.ID, character.NameCode, 1);
                }
            }

            // Reconcile eligible openers after the new character and its triggers exist.
            MessengerMessageCreator.CreateAllEligibleOpeners(user);
            user.AddTrigger(Trigger.GachaCharacter, 0, 0);
        }

        reward.Currency.Add(new NetCurrencyData { Type = (int)CurrencyType.DissolutionPoint, Value = totalBodyLabels });
        user.AddCurrency(CurrencyType.DissolutionPoint, totalBodyLabels);
        reward.UserItems.Add(NetUtils.UserItemDataToNet(piece));

        response.Reward = reward;
        UserSessionStore.SaveUser(user);
        return response;
    }

    // --- Harmony Cubes ---

    public ResGetHarmonyCube GetHarmonyCube(User user)
    {
        ResGetHarmonyCube response = new();
        foreach (DbItemData harmonyCube in user.Items)
        {
            if (GameData.Instance.ItemHarmonyCubeTable.ContainsKey(harmonyCube.ItemType))
            {
                response.HarmonyCubes.Add(ToNetHarmonyCube(harmonyCube));
            }
        }

        return response;
    }

    public ResClearHarmonyCube ClearHarmonyCube(User user, ReqClearHarmonyCube req)
    {
        ResClearHarmonyCube response = new();

        foreach (DbItemData item in user.Items.ToArray())
        {
            if (item.Isn != req.Isn)
            {
                continue;
            }

            if (req.Csn > 0)
            {
                item.CsnList.Remove(req.Csn);

                if (item.CsnList.Count > 0)
                {
                    item.Csn = item.CsnList[0];
                    if (GameData.Instance.ItemHarmonyCubeTable.TryGetValue(item.ItemType, out ItemHarmonyCubeRecord? harmonyCubeData))
                    {
                        item.Position = harmonyCubeData.LocationId;
                    }
                }
                else
                {
                    item.Csn = 0;
                    item.Position = 0;
                }
            }
            else
            {
                item.CsnList.Clear();
                item.Csn = 0;
                item.Position = 0;
            }

            response.HarmonyCube = ToNetHarmonyCube(item);
            break;
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResLevelUpHarmonyCube LevelUpHarmonyCube(User user, ReqLevelUpHarmonyCube req)
    {
        ResLevelUpHarmonyCube response = new();
        (DbItemData harmonyCubeItem, ItemHarmonyCubeRecord harmonyCubeData) = RequireHarmonyCube(user, req.Isn);

        List<ItemHarmonyCubeLevelRecord> levelData = GetHarmonyCubeLevelData(harmonyCubeData);
        if (levelData.Count == 0)
            throw new BadHttpRequestException("No level data found for this harmony cube", 400);

        ItemHarmonyCubeLevelRecord? currentLevelData = levelData.FirstOrDefault(x => x.Level == harmonyCubeItem.Level);
        if (currentLevelData == null)
            throw new BadHttpRequestException("Current level data not found", 400);

        int requiredMaterialCount = currentLevelData.MaterialValue;
        int requiredMaterialId = currentLevelData.MaterialId;
        int requiredGold = currentLevelData.GoldValue;

        DbItemData? materialItem = user.Items.FirstOrDefault(x => x.ItemType == requiredMaterialId && x.Count >= requiredMaterialCount);
        if (materialItem == null)
            throw new BadHttpRequestException($"Not enough materials. Required: {requiredMaterialCount} of item {requiredMaterialId}", 400);

        if (user.GetCurrencyVal(CurrencyType.Gold) < requiredGold)
            throw new BadHttpRequestException($"Not enough gold. Required: {requiredGold}", 400);

        materialItem.Count -= requiredMaterialCount;
        if (materialItem.Count <= 0)
        {
            user.Items.Remove(materialItem);
        }
        else
        {
            response.Items.Add(NetUtils.UserItemDataToNet(materialItem));
        }

        user.Currency[CurrencyType.Gold] -= requiredGold;

        harmonyCubeItem.Level++;
        harmonyCubeItem.Exp = 0;

        user.AddTrigger(Trigger.HarmonyCubeLevel, harmonyCubeItem.Level, harmonyCubeItem.ItemType);
        if (harmonyCubeItem.Level >= levelData.Count)
        {
            user.AddTrigger(Trigger.HarmonyCubeLevelMax, 1, harmonyCubeItem.ItemType);
        }

        response.Items.Add(NetUtils.UserItemDataToNet(harmonyCubeItem));
        response.Currency = new NetUserCurrencyData
        {
            Type = (int)CurrencyType.Gold,
            Value = user.GetCurrencyVal(CurrencyType.Gold)
        };

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResManagementHarmonyCube ManagementHarmonyCube(User user, ReqManagementHarmonyCube req)
    {
        ResManagementHarmonyCube response = new();
        (DbItemData harmonyCubeItem, ItemHarmonyCubeRecord harmonyCubeData) = RequireHarmonyCube(user, req.Isn);

        int maxSlots = GetCurrentHarmonyCubeLevelData(harmonyCubeItem, harmonyCubeData)?.Slot ?? 1;

        foreach (long clearCsn in req.Clears)
        {
            RemoveSwapOrClearCsn(harmonyCubeItem, clearCsn);
        }

        foreach (NetWearHarmonyCubeData wearData in req.Wears)
        {
            if (wearData.SwapCsn > 0)
            {
                RemoveSwapOrClearCsn(harmonyCubeItem, wearData.SwapCsn);
            }

            if (wearData.Csn > 0)
            {
                EquipHarmonyCubeToCharacter(user, harmonyCubeItem, harmonyCubeData, wearData.Csn, maxSlots);
            }
        }

        foreach (DbItemData harmonyCube in user.Items)
        {
            if (GameData.Instance.ItemHarmonyCubeTable.ContainsKey(harmonyCube.ItemType))
            {
                response.HarmonyCubes.Add(ToNetHarmonyCube(harmonyCube));
            }
        }

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResWearHarmonyCube WearHarmonyCube(User user, ReqWearHarmonyCube req)
    {
        ResWearHarmonyCube response = new();
        (DbItemData harmonyCubeItem, ItemHarmonyCubeRecord harmonyCubeData) = RequireHarmonyCube(user, req.Isn);

        if (req.Wear == null)
            throw new BadHttpRequestException("Wear data is required", 400);

        long targetCsn = req.Wear.Csn;
        long swapCsn = req.Wear.SwapCsn;
        int maxSlots = GetCurrentHarmonyCubeLevelData(harmonyCubeItem, harmonyCubeData)?.Slot ?? 1;

        if (swapCsn > 0)
        {
            RemoveSwapOrClearCsn(harmonyCubeItem, swapCsn);
        }

        List<DbItemData> modifiedItems = [];
        if (targetCsn > 0)
        {
            modifiedItems = EquipHarmonyCubeToCharacter(user, harmonyCubeItem, harmonyCubeData, targetCsn, maxSlots);
        }
        else if (targetCsn == 0)
        {
            harmonyCubeItem.CsnList.Clear();
            harmonyCubeItem.Csn = 0;
            harmonyCubeItem.Position = 0;
        }
        else
        {
            throw new BadHttpRequestException("InvalId character CSN", 400);
        }

        foreach (DbItemData modifiedItem in modifiedItems)
        {
            response.HarmonyCubes.Add(ToNetHarmonyCube(modifiedItem));
        }

        response.HarmonyCubes.Add(ToNetHarmonyCube(harmonyCubeItem));

        UserSessionStore.SaveUser(user);
        return response;
    }

    // --- Equipment Awakening ---

    public ResEquipmentAwakening AwakeningEquipment(User user, ReqEquipmentAwakening req)
    {
        ResEquipmentAwakening response = new();

        DbItemData? equipmentToAwaken = user.Items.FirstOrDefault(x => x.Isn == req.Isn);
        if (equipmentToAwaken == null)
            return response;

        const int materialCost = 1;
        const int materialId = 7080001;

        DbItemData? material = user.Items.FirstOrDefault(x => x.ItemType == materialId);
        if (material == null || material.Count < materialCost)
            return response;

        int awakenedEquipmentTypeId = GetAwakenedEquipmentTypeId(equipmentToAwaken.ItemType);

        NetEquipmentAwakeningOption awakeningOption = new()
        {
            Option1Lock = false,
            IsOption1DisposableLock = false,
            Option2Lock = false,
            IsOption2DisposableLock = false,
            Option3Lock = false,
            IsOption3DisposableLock = false
        };

        if (GameData.Instance.ItemEquipTable.TryGetValue(awakenedEquipmentTypeId, out _))
        {
            try
            {
                GenerateAwakeningOptions(awakeningOption);
            }
            catch (InvalidOperationException ex)
            {
                Logging.WriteLine($"Failed to generate awakening options: {ex.Message}", LogType.Error);
                return response;
            }
        }

        if (!EquipmentUtils.DeductMaterials(material, materialCost, user, response.Items))
            return response;

        equipmentToAwaken.ItemType = awakenedEquipmentTypeId;
        equipmentToAwaken.Level = 0;
        equipmentToAwaken.Exp = 0;
        equipmentToAwaken.Corp = 0;

        // Persist item changes first so EquipmentAwakeningEntity FK to ItemEntity is satisfied.
        UserSessionStore.SaveUser(user);

        inventoryRepo.SaveAwakening(user.ID, new EquipmentAwakeningData
        {
            Isn = equipmentToAwaken.Isn,
            Option = CloneAwakeningOption(awakeningOption),
            IsNewData = false
        });
        db.SaveChanges();

        response.Awakening = new NetEquipmentAwakening
        {
            Isn = equipmentToAwaken.Isn,
            Option = awakeningOption
        };
        response.Items.Add(NetUtils.ToNet(equipmentToAwaken));

        return response;
    }

    public ResAwakeningChangeOption ChangeEquipmentOption(ulong userId, ReqAwakeningChangeOption req)
    {
        ResAwakeningChangeOption response = new();
        if (req.Isn <= 0)
            return response;

        List<EquipmentAwakeningData> entries = inventoryRepo.GetAwakeningsByIsn(userId, req.Isn);
        EquipmentAwakeningData? oldAwakening = entries.FirstOrDefault(x => !x.IsNewData);
        if (oldAwakening == null)
            return response;

        if (req.IsChanged)
        {
            EquipmentAwakeningData confirmedAwakening = oldAwakening;
            if (entries.Count > 1)
            {
                EquipmentAwakeningData? newEntry = entries.FirstOrDefault(x => x.IsNewData);
                if (newEntry != null)
                {
                    inventoryRepo.DeleteAwakening(userId, req.Isn, isNewData: true);
                    newEntry.IsNewData = false;
                    inventoryRepo.SaveAwakening(userId, newEntry);
                    db.SaveChanges();
                    confirmedAwakening = newEntry;
                }
            }

            response.Awakening = new NetEquipmentAwakening
            {
                Isn = confirmedAwakening.Isn,
                Option = CloneAwakeningOption(confirmedAwakening.Option)
            };
        }
        else
        {
            if (entries.Count > 1)
            {
                inventoryRepo.DeleteAwakening(userId, req.Isn, isNewData: true);
                db.SaveChanges();
            }

            response.Awakening = new NetEquipmentAwakening
            {
                Isn = oldAwakening.Isn,
                Option = CloneAwakeningOption(oldAwakening.Option)
            };
        }

        return response;
    }

    public ResGetAwakeningDetail GetEquipmentAwakeningDetail(ulong userId, ReqGetAwakeningDetail req)
    {
        ResGetAwakeningDetail response = new();
        if (req.Isn <= 0)
            return response;

        EquipmentAwakeningData? awakening = inventoryRepo.GetAwakeningByIsn(userId, req.Isn);
        if (awakening == null)
            return response;

        response.CurrentOption = CloneAwakeningOption(awakening.Option);

        NetEquipmentAwakeningOption newOption = new();
        for (int i = 1; i <= 3; i++)
        {
            int currentOptionId = GetOptionIdForSlot(awakening.Option, i);
            bool isLocked = IsOptionLocked(awakening.Option, i);
            bool isDisposableLocked = IsOptionDisposableLocked(awakening.Option, i);

            if (isLocked || isDisposableLocked)
            {
                SetOptionForSlot(newOption, i, currentOptionId, isLocked, isDisposableLocked);
                continue;
            }

            int newOptionId = GenerateNewOptionIdSameGroup(currentOptionId, requireGroup100000: false);
            SetOptionForSlot(newOption, i, newOptionId, false, false);
        }

        response.NewOption = newOption;
        return response;
    }

    public ResAwakeningResetOption ResetEquipmentOption(User user, ReqAwakeningResetOption req)
    {
        ResAwakeningResetOption response = new();

        EquipmentAwakeningData? awakening = inventoryRepo.GetAwakeningByIsn(user.ID, req.Isn);
        if (awakening == null)
            return response;

        var slotLockInfo = new (int optionId, bool isLocked, bool isDisposableLocked)[3];
        List<int> lockedOptionStateEffectIds = [];
        int lockedOptionCount = CollectSlotLockInfo(awakening.Option, slotLockInfo, lockedOptionStateEffectIds);

        int costId = GetAwakeningCostId(lockedOptionCount, defaultCostId: 100001);
        (int materialId, int materialCost) = GetAwakeningMaterialInfo(costId);

        DbItemData? material = user.Items.FirstOrDefault(x => x.ItemType == materialId);
        if (material == null || material.Count < materialCost)
        {
            Logging.WriteLine($"Insufficient materials for reset operation. Need {materialCost} of item {materialId}, but have {material?.Count ?? 0}", LogType.Warning);
            return response;
        }

        if (!EquipmentUtils.DeductMaterials(material, materialCost, user, response.Items))
        {
            Logging.WriteLine($"Insufficient materials for reset operation. Need {materialCost} of item {materialId}, but have {material?.Count ?? 0}", LogType.Warning);
            return response;
        }

        NetEquipmentAwakeningOption resetOption = new();
        ProcessResetOptionSlots(awakening, resetOption, slotLockInfo, lockedOptionStateEffectIds);

        UserSessionStore.SaveUser(user);

        inventoryRepo.SaveAwakening(user.ID, awakening);
        inventoryRepo.SaveAwakening(user.ID, new EquipmentAwakeningData
        {
            Isn = awakening.Isn,
            Option = CloneAwakeningOption(resetOption),
            IsNewData = true
        });
        db.SaveChanges();

        response.ResetOption = CloneAwakeningOption(resetOption);
        return response;
    }

    public ResAwakeningUpgradeOption UpgradeEquipmentOption(User user, ReqAwakeningUpgradeOption req)
    {
        ResAwakeningUpgradeOption response = new();

        if (req.Isn <= 0)
        {
            Logging.WriteLine($"Invalid ISN: {req.Isn}", LogType.Warning);
            return response;
        }

        EquipmentAwakeningData? awakening = inventoryRepo.GetAwakeningByIsn(user.ID, req.Isn, isNewData: false);
        if (awakening == null)
            return response;

        var slotLockInfo = new (int optionId, bool isLocked, bool isDisposableLocked)[3];
        int lockedOptionCount = CollectSlotLockInfo(awakening.Option, slotLockInfo, lockedOptionStateEffectIds: null);

        int costId = GetAwakeningCostId(lockedOptionCount, defaultCostId: 102001);
        (int materialId, int materialCost) = GetAwakeningMaterialInfo(costId);

        DbItemData? material = user.Items.FirstOrDefault(x => x.ItemType == materialId);
        if (material == null || material.Count < materialCost)
            return response;

        if (!EquipmentUtils.DeductMaterials(material, materialCost, user, response.Items))
            return response;

        NetEquipmentAwakeningOption newOption = new();
        ProcessUpgradeOptionSlots(awakening, newOption, slotLockInfo);

        UserSessionStore.SaveUser(user);

        inventoryRepo.SaveAwakening(user.ID, awakening);
        inventoryRepo.SaveAwakening(user.ID, new EquipmentAwakeningData
        {
            Isn = awakening.Isn,
            Option = CloneAwakeningOption(newOption),
            IsNewData = true
        });
        db.SaveChanges();

        response.ResetOption = CloneAwakeningOption(newOption);
        return response;
    }

    // --- Private Helpers ---

    private static (NetUserCharacterDefaultData? Character, NetUserItemData? Item, int NewGradeCoreId) UpgradeCharacterGrade(
        User user, long csn, long isn, int count, bool throwIfRecordMissing)
    {
        CharacterModel targetCharacter = user.GetCharacterBySerialNumber(csn) ?? throw new NullReferenceException();
        CharacterRecord? currentCharacter = GameData.Instance.CharacterTable.Values.FirstOrDefault(c => c.Id == targetCharacter.Tid);
        if (currentCharacter == null)
        {
            if (throwIfRecordMissing)
                throw new NullReferenceException();

            return (null, null, 0);
        }

        if (IsMaxGradeCoreId(currentCharacter.GradeCoreId))
        {
            Console.WriteLine(throwIfRecordMissing ? "cannot limit break any further!" : "warning: cannot upgrade code any further!");
            return (null, null, 0);
        }

        CharacterRecord? newCharacter = GameData.Instance.CharacterTable.Values.FirstOrDefault(
            c => c.NameCode == currentCharacter.NameCode && c.GradeCoreId == currentCharacter.GradeCoreId + count);

        if (newCharacter == null)
            return (null, null, 0);

        targetCharacter.Grade += count;
        targetCharacter.Tid = newCharacter.Id;

        DbItemData bodyItem = user.Items.FirstOrDefault(i => i.Isn == isn) ?? throw new NullReferenceException();
        user.RemoveItemBySerialNumber(isn, count);

        return (ToNetCharacterDefault(targetCharacter), NetUtils.ToNet(bodyItem), newCharacter.GradeCoreId);
    }

    private static bool IsMaxGradeCoreId(int gradeCoreId) =>
        gradeCoreId is 103 or 11 or 201;

    private static (int Credit, int BattleData, int CoreDust) CalculateLevelUpCost(
        Dictionary<int, CharacterLevelRecord> data, int fromLevel, int toLevel)
    {
        int credit = 0;
        int battleData = 0;
        int coreDust = 0;
        for (int i = fromLevel; i < toLevel; i++)
        {
            CharacterLevelRecord levelUpData = data[i];
            credit += levelUpData.Gold;
            battleData += levelUpData.CharacterExp;
            coreDust += levelUpData.CharacterExp2;
        }

        return (credit, battleData, coreDust);
    }

    private static NetUserCharacterDefaultData ToNetCharacterDefault(CharacterModel item) => new()
    {
        CostumeId = item.CostumeId,
        Csn = item.Csn,
        Lv = item.Level,
        Skill1Lv = item.Skill1Lvl,
        Skill2Lv = item.Skill2Lvl,
        UltiSkillLv = item.UltimateLevel,
        Grade = item.Grade,
        Tid = item.Tid
    };

    private static void AppendUserCurrencies(User user, RepeatedField<NetUserCurrencyData> target)
    {
        foreach (KeyValuePair<CurrencyType, long> currency in user.Currency)
        {
            target.Add(new NetUserCurrencyData { Type = (int)currency.Key, Value = currency.Value });
        }
    }

    private static bool IsT10Equipment(int itemTypeId)
    {
        string itemTypeStr = itemTypeId.ToString();
        return itemTypeStr.Length == 7 && itemTypeStr[0] == '3' && itemTypeStr.Substring(3, 2) == "10";
    }

    private static (int exp, int modules) AddEquipExp(NetItemData srcItem, DbItemData destItem)
    {
        int[] maxLevel = [0, 0, 3, 3, 4, 4, 5, 5, 5, 5];
        ItemEquipRecord? srcEquipRecord = GameData.Instance.ItemEquipTable.Values.FirstOrDefault(x => x.Id == srcItem.Tid);
        ItemEquipRecord destEquipRecord = GameData.Instance.ItemEquipTable.Values.FirstOrDefault(x => x.Id == destItem.ItemType)
            ?? throw new NullReferenceException();

        int[] expNextTable = [.. GameData.Instance.itemEquipExpTable.Values
            .Where(x => x.ItemRare == destEquipRecord.ItemRare)
            .Select(x => x.Exp)
            .OrderBy(x => x)];

        int exp = 0;
        int modules = 0;

        if (srcEquipRecord != null)
        {
            ItemEquipGradeExpRecord levelRecord = GameData.Instance.ItemEquipGradeExpTable.Values
                .FirstOrDefault(x => x.GradeCoreId == srcEquipRecord.GradeCoreId)
                ?? throw new NullReferenceException();

            exp = srcItem.Count * levelRecord.Exp;
            destItem.Exp += exp;
        }
        else
        {
            if (BoostExpTable.TryGetValue(srcItem.Tid, out int expPerModule))
            {
                exp = srcItem.Count * expPerModule;
            }

            destItem.Exp += exp;
        }

        int capLevel = maxLevel[destEquipRecord.GradeCoreId - 1];
        while (destItem.Level < capLevel && destItem.Exp >= expNextTable[destItem.Level + 1])
        {
            destItem.Exp -= expNextTable[destItem.Level + 1];
            destItem.Level++;
        }

        if (destItem.Level >= capLevel)
        {
            modules = destItem.Exp;
            destItem.Exp = 0;
        }

        return (exp, modules);
    }

    private static int CalcTotalEquipExp(DbItemData destItem)
    {
        ItemEquipRecord equipRecord = GameData.Instance.ItemEquipTable.Values.FirstOrDefault(x => x.Id == destItem.ItemType)
            ?? throw new NullReferenceException();

        int[] expNextTable = [.. GameData.Instance.itemEquipExpTable.Values
            .Where(x => x.ItemRare == equipRecord.ItemRare)
            .Select(x => x.Exp)];

        int exp = 0;
        for (int i = 1; i < expNextTable.Length; i++)
        {
            exp += expNextTable[i];
        }

        return exp;
    }

    private static (int t1, int t2, int t3) CalcBoostModules(int exp) =>
        (exp / BoostExpTable[7010001], exp / BoostExpTable[7010002], exp / BoostExpTable[7010003]);

    private static CharacterRecord SelectRandomCharacter(IEnumerable<GachaListProbRecord> charProbs)
    {
        int maxCharProb = 0;
        Dictionary<int, (int minProbInc, int maxProbEx)> charProbsTable = new();

        foreach (GachaListProbRecord charProb in charProbs)
        {
            charProbsTable.Add(charProb.Id, (maxCharProb, maxCharProb + charProb.Prob));
            maxCharProb += charProb.Prob;
        }

        int charRoll = (int)Random.Shared.NextInt64(maxCharProb);
        int selectedId = charProbsTable.First(p => charRoll >= p.Value.minProbInc && charRoll < p.Value.maxProbEx).Key;
        GachaListProbRecord selectedCharacter = charProbs.First(charP => charP.Id == selectedId);

        return GameData.Instance.CharacterTable[selectedCharacter.GachaId];
    }

    private static (DbItemData Item, ItemHarmonyCubeRecord Record) RequireHarmonyCube(User user, long isn)
    {
        DbItemData harmonyCubeItem = user.Items.FirstOrDefault(x => x.Isn == isn)
            ?? throw new BadHttpRequestException("Harmony cube not found", 404);

        if (!GameData.Instance.ItemHarmonyCubeTable.TryGetValue(harmonyCubeItem.ItemType, out ItemHarmonyCubeRecord? harmonyCubeData))
            throw new BadHttpRequestException("Item is not a harmony cube", 400);

        return (harmonyCubeItem, harmonyCubeData);
    }

    private static List<ItemHarmonyCubeLevelRecord> GetHarmonyCubeLevelData(ItemHarmonyCubeRecord harmonyCubeData) =>
        GameData.Instance.ItemHarmonyCubeLevelTable.Values
            .Where(x => x.LevelEnhanceId == harmonyCubeData.LevelEnhanceId)
            .OrderBy(x => x.Level)
            .ToList();

    private static ItemHarmonyCubeLevelRecord? GetCurrentHarmonyCubeLevelData(DbItemData harmonyCubeItem, ItemHarmonyCubeRecord harmonyCubeData) =>
        GetHarmonyCubeLevelData(harmonyCubeData).FirstOrDefault(x => x.Level == harmonyCubeItem.Level);

    private static NetUserHarmonyCubeData ToNetHarmonyCube(DbItemData harmonyCube)
    {
        NetUserHarmonyCubeData netHarmonyCube = new()
        {
            Isn = harmonyCube.Isn,
            Tid = harmonyCube.ItemType,
            Lv = harmonyCube.Level
        };

        netHarmonyCube.CsnList.AddRange(harmonyCube.CsnList);
        if (harmonyCube.Csn > 0 && !harmonyCube.CsnList.Contains(harmonyCube.Csn))
        {
            netHarmonyCube.CsnList.Add(harmonyCube.Csn);
        }

        return netHarmonyCube;
    }

    private static void RemoveSwapOrClearCsn(DbItemData harmonyCubeItem, long csn)
    {
        harmonyCubeItem.CsnList.Remove(csn);
        if (harmonyCubeItem.Csn == csn)
        {
            harmonyCubeItem.Csn = 0;
            harmonyCubeItem.Position = 0;
        }
    }

    private static List<DbItemData> EquipHarmonyCubeToCharacter(
        User user, DbItemData harmonyCubeItem, ItemHarmonyCubeRecord harmonyCubeData, long targetCsn, int maxSlots)
    {
        if (harmonyCubeItem.CsnList.Contains(targetCsn))
        {
            return [];
        }

        if (harmonyCubeItem.CsnList.Count >= maxSlots)
            throw new BadHttpRequestException($"Harmony cube slot limit reached. Current level allows {maxSlots} characters.", 400);

        CharacterModel character = user.GetCharacterBySerialNumber(targetCsn)
            ?? throw new BadHttpRequestException($"Character {targetCsn} not found", 404);

        if (!IsHarmonyCubeClassCompatible(character, harmonyCubeData))
            throw new BadHttpRequestException("Character class incompatible with harmony cube", 400);

        List<DbItemData> modifiedItems = CleanupCharacterFromAllHarmonyCubes(user, targetCsn, harmonyCubeItem.Isn);

        harmonyCubeItem.CsnList.Add(targetCsn);
        if (harmonyCubeItem.CsnList.Count == 1)
        {
            harmonyCubeItem.Csn = targetCsn;
            harmonyCubeItem.Position = harmonyCubeData.LocationId;
        }

        return modifiedItems;
    }

    private static List<DbItemData> CleanupCharacterFromAllHarmonyCubes(User user, long targetCsn, long excludeIsn)
    {
        List<DbItemData> modifiedItems = [];

        foreach (DbItemData item in user.Items.ToArray())
        {
            if (item.Isn == excludeIsn ||
                !GameData.Instance.ItemHarmonyCubeTable.TryGetValue(item.ItemType, out ItemHarmonyCubeRecord? existingHarmonyCubeData))
            {
                continue;
            }

            if (item.CsnList.Contains(targetCsn) || item.Csn == targetCsn)
            {
                item.CsnList.Remove(targetCsn);

                if (item.CsnList.Count > 0)
                {
                    item.Csn = item.CsnList[0];
                    item.Position = existingHarmonyCubeData.LocationId;
                }
                else
                {
                    item.Csn = 0;
                    item.Position = 0;
                }

                modifiedItems.Add(item);
            }
        }

        return modifiedItems;
    }

    private static bool IsHarmonyCubeClassCompatible(CharacterModel character, ItemHarmonyCubeRecord harmonyCubeData)
    {
        if (GameData.Instance.CharacterTable.TryGetValue(character.Tid, out CharacterRecord? characterData))
        {
            return harmonyCubeData.Class == CharacterClassType.All || harmonyCubeData.Class == characterData.Class;
        }

        return false;
    }

    private static int GetAwakenedEquipmentTypeId(int originalTypeId) => originalTypeId switch
    {
        3110901 => 3111001,
        3210901 => 3211001,
        3310901 => 3311001,
        3410901 => 3411001,
        3120901 => 3121001,
        3220901 => 3221001,
        3320901 => 3321001,
        3420901 => 3421001,
        3130901 => 3131001,
        3230901 => 3231001,
        3330901 => 3331001,
        3430901 => 3431001,
        _ => originalTypeId
    };

    private static NetEquipmentAwakeningOption CloneAwakeningOption(NetEquipmentAwakeningOption option) => new()
    {
        Option1Id = option.Option1Id,
        Option1Lock = option.Option1Lock,
        IsOption1DisposableLock = option.IsOption1DisposableLock,
        Option2Id = option.Option2Id,
        Option2Lock = option.Option2Lock,
        IsOption2DisposableLock = option.IsOption2DisposableLock,
        Option3Id = option.Option3Id,
        Option3Lock = option.Option3Lock,
        IsOption3DisposableLock = option.IsOption3DisposableLock
    };

    private static int CollectSlotLockInfo(
        NetEquipmentAwakeningOption option,
        (int optionId, bool isLocked, bool isDisposableLocked)[] slotLockInfo,
        List<int>? lockedOptionStateEffectIds)
    {
        int lockedOptionCount = 0;
        for (int i = 1; i <= 3; i++)
        {
            int currentOptionId = GetOptionIdForSlot(option, i);
            bool isLocked = IsOptionLocked(option, i);
            bool isDisposableLocked = IsOptionDisposableLocked(option, i);

            slotLockInfo[i - 1] = (currentOptionId, isLocked, isDisposableLocked);

            if (isLocked || isDisposableLocked)
                lockedOptionCount++;

            if (lockedOptionStateEffectIds != null && isLocked && currentOptionId != 0 &&
                !lockedOptionStateEffectIds.Contains(currentOptionId))
            {
                lockedOptionStateEffectIds.Add(currentOptionId);
            }
        }

        return lockedOptionCount;
    }

    private static void GenerateAwakeningOptions(NetEquipmentAwakeningOption option)
    {
        List<int> excludedStateEffectIds = [];
        double[] slotActivationProbabilities = [1.0, 0.5, 0.3];

        for (int i = 1; i <= 3; i++)
        {
            if (Random.Shared.NextDouble() < slotActivationProbabilities[i - 1])
            {
                int selectedOptionId = GenerateNewOptionIdInit(excludedStateEffectIds, level: 11);
                if (!excludedStateEffectIds.Contains(selectedOptionId))
                {
                    excludedStateEffectIds.Add(selectedOptionId);
                }

                SetOptionForSlot(option, i, selectedOptionId, false, false);
            }
            else
            {
                SetOptionForSlot(option, i, 0, false, false);
            }
        }
    }

    private static int GenerateNewOptionIdInit(List<int> excludedStateEffectIds, int level)
    {
        List<EquipmentOptionRecord> allAwakeningOptions = GameData.Instance.EquipmentOptionTable.Values
            .Where(x => x.EquipmentOptionGroupId == 100000 &&
                        x.StateEffectList?.Any(se => se.StateEffectLevel == level) == true)
            .ToList();

        HashSet<int> excludedEffectGroupIds = GetExcludedEffectGroupIds(excludedStateEffectIds);
        Dictionary<int, List<EquipmentOptionRecord>> optionsByEffectGroup = allAwakeningOptions
            .Where(option => !excludedEffectGroupIds.Contains(option.StateEffectGroupId))
            .GroupBy(option => option.StateEffectGroupId)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (optionsByEffectGroup.Count == 0)
            throw new InvalidOperationException("No available equipment options for awakening - this indicates a data consistency issue");

        double excludedProbabilitySum = CalculateExcludedProbabilitySumByEffectGroup(excludedEffectGroupIds, level);
        List<EffectGroupWithWeight> weightedEffectGroups = CalculateDynamicProbabilitiesForEffectGroups(optionsByEffectGroup, excludedProbabilitySum);
        int selectedEffectGroupId = SelectWeightedRandomEffectGroup(weightedEffectGroups);

        foreach (EquipmentOptionRecord option in optionsByEffectGroup[selectedEffectGroupId])
        {
            StateEffectList? stateEffect = option.StateEffectList?.FirstOrDefault(se => se.StateEffectLevel == level);
            if (stateEffect != null)
            {
                return stateEffect.StateEffectId;
            }
        }

        throw new InvalidOperationException($"No state effect found with level {level} in selected effect group - this indicates a data consistency issue");
    }

    private static void ProcessResetOptionSlots(
        EquipmentAwakeningData awakening,
        NetEquipmentAwakeningOption resetOption,
        (int optionId, bool isLocked, bool isDisposableLocked)[] slotLockInfo,
        List<int> lockedOptionStateEffectIds)
    {
        double[] slotActivationProbabilities = [1.0, 0.5, 0.3];

        for (int i = 1; i <= 3; i++)
        {
            (int currentOptionId, bool isLocked, bool isDisposableLocked) = slotLockInfo[i - 1];

            if (isLocked && !isDisposableLocked)
            {
                SetOptionForSlot(resetOption, i, currentOptionId, true, false);
            }
            else if (isLocked && isDisposableLocked)
            {
                SetOptionForSlot(resetOption, i, currentOptionId, false, false);
                UnlockDisposableOption(awakening.Option, i);
            }
            else if (Random.Shared.NextDouble() < slotActivationProbabilities[i - 1])
            {
                int newOptionId = GenerateNewOptionIdWithDynamicProbability(lockedOptionStateEffectIds);
                SetOptionForSlot(resetOption, i, newOptionId, false, false);

                if (newOptionId != 0 && !lockedOptionStateEffectIds.Contains(newOptionId))
                {
                    lockedOptionStateEffectIds.Add(newOptionId);
                }
            }
            else
            {
                SetOptionForSlot(resetOption, i, 0, false, false);
            }
        }
    }

    private static void ProcessUpgradeOptionSlots(
        EquipmentAwakeningData awakening,
        NetEquipmentAwakeningOption newOption,
        (int optionId, bool isLocked, bool isDisposableLocked)[] slotLockInfo)
    {
        for (int i = 1; i <= 3; i++)
        {
            (int currentOptionId, bool isLocked, bool isDisposableLocked) = slotLockInfo[i - 1];

            if (isLocked && !isDisposableLocked)
            {
                SetOptionForSlot(newOption, i, currentOptionId, isLocked, isDisposableLocked);
                continue;
            }

            if (isDisposableLocked)
            {
                SetOptionForSlot(newOption, i, currentOptionId, false, false);
                UnlockDisposableOption(awakening.Option, i);
                continue;
            }

            if (currentOptionId == 0)
            {
                SetOptionForSlot(newOption, i, 0, false, false);
                continue;
            }

            int newOptionId = GenerateNewOptionIdSameGroup(currentOptionId, requireGroup100000: true);
            SetOptionForSlot(newOption, i, newOptionId, false, false);
        }
    }

    private static void UnlockDisposableOption(NetEquipmentAwakeningOption option, int slot) =>
        SetOptionForSlot(option, slot, GetOptionIdForSlot(option, slot), false, false);

    private static int GenerateNewOptionIdWithDynamicProbability(List<int> excludedStateEffectIds)
    {
        HashSet<int> excludedEffectGroupIds = GetExcludedEffectGroupIds(excludedStateEffectIds);
        Dictionary<int, List<EquipmentOptionRecord>> optionsByEffectGroup = GameData.Instance.EquipmentOptionTable.Values
            .Where(x => x.EquipmentOptionGroupId == 100000 && !excludedEffectGroupIds.Contains(x.StateEffectGroupId))
            .GroupBy(option => option.StateEffectGroupId)
            .ToDictionary(g => g.Key, g => g.ToList());

        double excludedProbabilitySum = CalculateExcludedProbabilitySumByEffectGroup(excludedEffectGroupIds, level: null);
        List<EffectGroupWithWeight> weightedEffectGroups = CalculateDynamicProbabilitiesForEffectGroups(optionsByEffectGroup, excludedProbabilitySum);
        int selectedEffectGroupId = SelectWeightedRandomEffectGroup(weightedEffectGroups);

        return SelectOptionFromGroup(optionsByEffectGroup[selectedEffectGroupId]);
    }

    private static int GenerateNewOptionIdSameGroup(int currentId, bool requireGroup100000)
    {
        if (requireGroup100000)
        {
            EquipmentOptionRecord? currentOption = GameData.Instance.EquipmentOptionTable.Values
                .FirstOrDefault(option => option.StateEffectList != null && option.StateEffectList.Any(se => se.StateEffectId == currentId));

            if (currentOption == null || currentOption.EquipmentOptionGroupId != 100000)
                throw new InvalidOperationException($"Current state_effect_id {currentId} not found in any EquipmentOption");

            List<EquipmentOptionRecord> optionsInGroup = GameData.Instance.EquipmentOptionTable.Values
                .Where(x => x.EquipmentOptionGroupId == 100000 && x.StateEffectGroupId == currentOption.StateEffectGroupId)
                .ToList();

            if (optionsInGroup.Count == 0)
                throw new InvalidOperationException($"No awakening options found with state_effect_group_id {currentOption.StateEffectGroupId}");

            return SelectOptionFromGroup(optionsInGroup);
        }
        else
        {
            if (!GameData.Instance.EquipmentOptionTable.TryGetValue(currentId, out EquipmentOptionRecord? currentOption))
                return currentId;

            List<EquipmentOptionRecord> optionsInGroup = GameData.Instance.EquipmentOptionTable.Values
                .Where(x => x.EquipmentOptionGroupId == currentOption.EquipmentOptionGroupId)
                .ToList();

            if (optionsInGroup.Count == 0)
                return currentId;

            long totalRatio = optionsInGroup.Sum(x => (long)x.OptionRatio);
            if (totalRatio == 0)
                return currentId;

            long randomValue = Random.Shared.NextInt64(0, totalRatio);
            long cumulativeRatio = 0;

            foreach (EquipmentOptionRecord option in optionsInGroup)
            {
                cumulativeRatio += option.OptionRatio;
                if (randomValue < cumulativeRatio)
                {
                    return option.Id;
                }
            }

            return currentId;
        }
    }

    private static HashSet<int> GetExcludedEffectGroupIds(List<int> excludedStateEffectIds)
    {
        HashSet<int> excludedEffectGroupIds = [];
        foreach (int stateEffectId in excludedStateEffectIds)
        {
            EquipmentOptionRecord? excludedOption = GameData.Instance.EquipmentOptionTable.Values
                .FirstOrDefault(opt => opt.StateEffectList != null && opt.StateEffectList.Any(se => se.StateEffectId == stateEffectId));
            if (excludedOption != null)
            {
                excludedEffectGroupIds.Add(excludedOption.StateEffectGroupId);
            }
        }

        return excludedEffectGroupIds;
    }

    private static double CalculateExcludedProbabilitySumByEffectGroup(HashSet<int> excludedEffectGroupIds, int? level)
    {
        if (excludedEffectGroupIds.Count == 0)
            return 0.0;

        double totalExcluded = 0.0;
        foreach (int effectGroupId in excludedEffectGroupIds)
        {
            EquipmentOptionRecord? firstOption = GameData.Instance.EquipmentOptionTable.Values
                .FirstOrDefault(opt =>
                    opt.StateEffectGroupId == effectGroupId &&
                    opt.EquipmentOptionGroupId == 100000 &&
                    (!level.HasValue || opt.StateEffectList?.Any(se => se.StateEffectLevel == level.Value) == true));

            if (firstOption != null)
            {
                totalExcluded += firstOption.OptionGroupRatio / 100.0;
            }
        }

        return Math.Min(totalExcluded, 99.9);
    }

    private static List<EffectGroupWithWeight> CalculateDynamicProbabilitiesForEffectGroups(
        Dictionary<int, List<EquipmentOptionRecord>> optionsByEffectGroup,
        double excludedProbabilitySum)
    {
        List<EffectGroupWithWeight> weightedEffectGroups = [];
        double probabilityDenominator = 100.0 - excludedProbabilitySum;
        if (probabilityDenominator <= 0)
        {
            probabilityDenominator = 1.0;
        }

        foreach (KeyValuePair<int, List<EquipmentOptionRecord>> kvp in optionsByEffectGroup)
        {
            if (kvp.Value.Count == 0)
                continue;

            double baseProbability = kvp.Value[0].OptionGroupRatio / 100.0;
            double dynamicProbability = baseProbability / probabilityDenominator;
            weightedEffectGroups.Add(new EffectGroupWithWeight(kvp.Key, dynamicProbability * 1000000));
        }

        return weightedEffectGroups;
    }

    private static int SelectWeightedRandomEffectGroup(List<EffectGroupWithWeight> weightedEffectGroups)
    {
        if (weightedEffectGroups.Count == 0)
            throw new InvalidOperationException("No weighted effect groups available - this indicates a data consistency issue");

        double totalWeight = weightedEffectGroups.Sum(weg => weg.Weight);
        if (totalWeight <= 0)
            throw new InvalidOperationException("Invalid group weights - this indicates a data consistency issue");

        double randomValue = Random.Shared.NextDouble() * totalWeight;
        double cumulativeWeight = 0.0;

        foreach (EffectGroupWithWeight weightedGroup in weightedEffectGroups)
        {
            cumulativeWeight += weightedGroup.Weight;
            if (randomValue <= cumulativeWeight)
            {
                return weightedGroup.EffectGroupId;
            }
        }

        return weightedEffectGroups.Last().EffectGroupId;
    }

    private static int SelectOptionFromGroup(List<EquipmentOptionRecord> options)
    {
        if (options.Count == 0)
            throw new InvalidOperationException("No options available in group - this indicates a data consistency issue");

        long totalRatio = options.Sum(x => (long)x.OptionRatio);
        long randomValue = Random.Shared.NextInt64(0, totalRatio);
        long cumulativeRatio = 0;

        foreach (EquipmentOptionRecord option in options)
        {
            cumulativeRatio += option.OptionRatio;
            if (randomValue < cumulativeRatio)
            {
                if (option.StateEffectList == null || option.StateEffectList.Count == 0)
                    throw new InvalidOperationException($"StateEffectList is null or empty for option {option.Id}");

                int randomIndex = Random.Shared.Next(option.StateEffectList.Count);
                return option.StateEffectList[randomIndex].StateEffectId;
            }
        }

        EquipmentOptionRecord lastOption = options.Last();
        if (lastOption.StateEffectList == null || lastOption.StateEffectList.Count == 0)
            throw new InvalidOperationException($"StateEffectList is null or empty for fallback option {lastOption.Id}");

        int fallbackIndex = Random.Shared.Next(lastOption.StateEffectList.Count);
        return lastOption.StateEffectList[fallbackIndex].StateEffectId;
    }

    private static int GetAwakeningCostId(int lockedOptionCount, int defaultCostId)
    {
        if (lockedOptionCount == 0 && defaultCostId == 100001)
            return 100001;

        EquipmentOptionCostRecord? costRecord = GameData.Instance.EquipmentOptionCostTable.Values
            .FirstOrDefault(x => x.CostGroupId == 200 && x.CostLevel == lockedOptionCount);

        return costRecord?.CostId ?? defaultCostId;
    }

    private static (int materialId, int materialCost) GetAwakeningMaterialInfo(int costId)
    {
        if (GameData.Instance.costTable.TryGetValue(costId, out CostRecord? costRecord) &&
            costRecord?.Costs != null &&
            costRecord.Costs.Count > 0)
        {
            return (costRecord.Costs[0].ItemId, costRecord.Costs[0].ItemValue);
        }

        return (7080001, 1);
    }

    private static int GetOptionIdForSlot(NetEquipmentAwakeningOption option, int slot) => slot switch
    {
        1 => option.Option1Id,
        2 => option.Option2Id,
        3 => option.Option3Id,
        _ => 0
    };

    private static bool IsOptionLocked(NetEquipmentAwakeningOption option, int slot) => slot switch
    {
        1 => option.Option1Lock,
        2 => option.Option2Lock,
        3 => option.Option3Lock,
        _ => false
    };

    private static bool IsOptionDisposableLocked(NetEquipmentAwakeningOption option, int slot) => slot switch
    {
        1 => option.IsOption1DisposableLock,
        2 => option.IsOption2DisposableLock,
        3 => option.IsOption3DisposableLock,
        _ => false
    };

    private static void SetOptionForSlot(NetEquipmentAwakeningOption option, int slot, int optionId, bool locked, bool disposableLocked)
    {
        switch (slot)
        {
            case 1:
                option.Option1Id = optionId;
                option.Option1Lock = locked;
                option.IsOption1DisposableLock = disposableLocked;
                break;
            case 2:
                option.Option2Id = optionId;
                option.Option2Lock = locked;
                option.IsOption2DisposableLock = disposableLocked;
                break;
            case 3:
                option.Option3Id = optionId;
                option.Option3Lock = locked;
                option.IsOption3DisposableLock = disposableLocked;
                break;
        }
    }

    private sealed record EffectGroupWithWeight(int EffectGroupId, double Weight);
}
