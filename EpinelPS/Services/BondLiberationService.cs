using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Liberate;
using EpinelPS.Repositories;
using EpinelPS.Utils;

namespace EpinelPS.Services;

public sealed class BondLiberationService(
    BondLiberationRepository repo,
    GameContext db,
    ResetRepository resetRepo,
    ProgressRepository progressRepo)
{
    // --- Character Attractive & Counsel ---

    public ResGetAttractiveList GetAttractiveList(User user)
    {
        var response = new ResGetAttractiveList
        {
            CounselAvailableCount = resetRepo.GetDailyCounselCount(user.ID, 1, 3)
        };

        foreach (NetUserAttractiveData item in repo.GetBonds(user.ID))
        {
            item.CanCounselToday = true;
            response.Attractives.Add(item);
        }

        return response;
    }

    public ResObtainAttractiveReward ObtainAttractiveReward(User user, ReqObtainAttractiveReward req)
    {
        var response = new ResObtainAttractiveReward();
        var levelUpRecord = GameData.Instance.AttractiveLevelReward
            .FirstOrDefault(x => x.Value.AttractiveLevel == req.Lv && x.Value.NameCode == req.NameCode);

        var bond = repo.GetBond(user.ID, req.NameCode);
        if (bond != null && levelUpRecord.Value != null && !bond.ObtainedRewardLevels.Contains(levelUpRecord.Value.Id))
        {
            bond.ObtainedRewardLevels.Add(levelUpRecord.Value.Id);

            RewardRecord reward = GameData.Instance.GetRewardTableEntry(levelUpRecord.Value.RewardId)
                ?? throw new Exception("failed to get reward");
            response.Reward = RewardUtils.RegisterRewardsForUser(user, reward);

            repo.UpsertBond(user.ID, bond);
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResSetAttractiveFavorites SetAttractiveFavorites(ulong userId, ReqSetAttractiveFavorites req)
    {
        var bond = repo.GetBond(userId, req.NameCode);
        if (bond != null)
        {
            bond.IsFavorites = req.IsFavorites;
            repo.UpsertBond(userId, bond);
        }

        return new ResSetAttractiveFavorites();
    }

    public ResListAnsweredAttractiveCounsel ListAnsweredCounsel(ulong userId, int nameCode)
    {
        var response = new ResListAnsweredAttractiveCounsel();
        var bond = repo.GetBond(userId, nameCode);
        if (bond != null)
        {
            response.CorrectlyAnsweredAttractiveCounselTableIdList.AddRange(bond.CounselDialogCompleteIds);
        }
        return response;
    }

    public ResCharacterCounsel Counsel(User user, ReqCharacterCounsel req)
    {
        var (attractive, exp) = CounselInternal(user, req.NameCode, req.CounselTid);
        var response = new ResCharacterCounsel
        {
            Attractive = attractive,
            Exp = exp
        };
        foreach (var currency in user.Currency)
        {
            response.Currencies.Add(new NetUserCurrencyData { Type = (int)currency.Key, Value = currency.Value });
        }
        return response;
    }

    public ResCharacterQuickCounsel QuickCounsel(User user, ReqCharacterQuickCounsel req)
    {
        var (attractive, exp) = CounselInternal(user, req.NameCode, counselTid: null);
        var response = new ResCharacterQuickCounsel
        {
            Attractive = attractive,
            Exp = exp
        };
        foreach (var currency in user.Currency)
        {
            response.Currencies.Add(new NetUserCurrencyData { Type = (int)currency.Key, Value = currency.Value });
        }
        return response;
    }

    public ResCharacterPresent Present(User user, ReqCharacterPresent req)
    {
        var response = new ResCharacterPresent();
        NetUserAttractiveData? bondInfo = repo.GetBond(user.ID, req.NameCode);
        if (bondInfo == null)
        {
            return response;
        }

        int totalExpGained = 0;
        CharacterRecord? characterRecord = GameData.Instance.CharacterTable.Values.FirstOrDefault(x => x.NameCode == req.NameCode);

        foreach (NetItemData item in req.Items)
        {
            ItemMaterialRecord? materialInfo = GameData.Instance.itemMaterialTable.GetValueOrDefault(item.Tid);
            if (materialInfo != null && materialInfo.ItemSubType == ItemSubType.AttractiveMaterial)
            {
                int expGained = materialInfo.ItemValue * item.Count;

                if (characterRecord != null)
                {
                    if (materialInfo.MaterialType == MaterialType.Corporation)
                    {
                        string corporation = materialInfo.NameLocalkey.Split('_')[2];
                        if (corporation.Equals(characterRecord.Corporation.ToString(), StringComparison.OrdinalIgnoreCase))
                            expGained *= 5;
                    }
                    else if (materialInfo.MaterialType == MaterialType.Squad)
                    {
                        string squad = materialInfo.NameLocalkey.Split('_')[2];
                        if (squad.Equals(characterRecord.Squad.ToString(), StringComparison.OrdinalIgnoreCase))
                            expGained *= 3;
                    }
                }

                totalExpGained += expGained;

                DbItemData? userItem = user.Items.FirstOrDefault(x => x.ItemType == item.Tid);
                if (userItem != null)
                {
                    userItem.Count -= item.Count;
                    if (userItem.Count <= 0)
                        user.Items.Remove(userItem);
                }
            }
        }

        int beforeLv = bondInfo.Lv;
        int beforeExp = bondInfo.Exp;

        if (bondInfo.Lv < user.GetMaxAttractiveLevel(req.NameCode))
        {
            bondInfo.Exp += totalExpGained;
            UpdateAttractiveLevel(bondInfo);
        }

        if (bondInfo.Lv >= user.GetMaxAttractiveLevel(req.NameCode) &&
            !db.Triggers.Any(t => t.UserId == user.ID && t.Type == Trigger.CharacterAttractiveLevelMax && t.ConditionId == req.NameCode))
        {
            user.AddTrigger(Trigger.CharacterAttractiveLevelMax, bondInfo.Lv, req.NameCode);
        }

        response.Attractive = bondInfo;
        response.Exp = new NetIncreaseExpData
        {
            NameCode = bondInfo.NameCode,
            BeforeLv = beforeLv,
            BeforeExp = beforeExp,
            CurrentLv = bondInfo.Lv,
            CurrentExp = bondInfo.Exp,
            GainExp = totalExpGained
        };
        response.Items.AddRange(NetUtils.GetUserItems(user));

        repo.UpsertBond(user.ID, bondInfo);
        UserSessionStore.SaveUser(user);
        return response;
    }

    private (NetUserAttractiveData Attractive, NetIncreaseExpData Exp) CounselInternal(
        User user, int nameCode, int? counselTid)
    {
        NetUserAttractiveData? currentBondInfo = repo.GetBond(user.ID, nameCode);
        NetIncreaseExpData exp;

        if (currentBondInfo != null)
        {
            int beforeLv = currentBondInfo.Lv;
            int beforeExp = currentBondInfo.Exp;

            currentBondInfo.CounseledCount++;
            if (counselTid.HasValue && !currentBondInfo.CounselDialogCompleteIds.Contains(counselTid.Value))
                currentBondInfo.CounselDialogCompleteIds.Add(counselTid.Value);
            currentBondInfo.CanCounselToday = true;

            if (currentBondInfo.Lv < user.GetMaxAttractiveLevel(nameCode))
            {
                currentBondInfo.Exp += 100;
                UpdateAttractiveLevel(currentBondInfo);
            }

            if (counselTid.HasValue &&
                currentBondInfo.Lv >= user.GetMaxAttractiveLevel(nameCode) &&
                !db.Triggers.Any(t => t.UserId == user.ID && t.Type == Trigger.CharacterAttractiveLevelMax && t.ConditionId == nameCode))
            {
                user.AddTrigger(Trigger.CharacterAttractiveLevelMax, currentBondInfo.Lv, nameCode);
            }

            exp = new NetIncreaseExpData
            {
                NameCode = currentBondInfo.NameCode,
                BeforeLv = beforeLv,
                BeforeExp = beforeExp,
                CurrentLv = currentBondInfo.Lv,
                CurrentExp = currentBondInfo.Exp,
                GainExp = 100
            };
        }
        else
        {
            currentBondInfo = new NetUserAttractiveData
            {
                NameCode = nameCode,
                Exp = 100,
                CounseledCount = 1,
                IsFavorites = false,
                CanCounselToday = true,
                Lv = 1
            };
            if (counselTid.HasValue)
                currentBondInfo.CounselDialogCompleteIds.Add(counselTid.Value);
            UpdateAttractiveLevel(currentBondInfo);

            exp = new NetIncreaseExpData
            {
                NameCode = currentBondInfo.NameCode,
                BeforeLv = 1,
                BeforeExp = 0,
                CurrentLv = 1,
                CurrentExp = 100,
                GainExp = 100
            };
        }

        repo.UpsertBond(user.ID, currentBondInfo);
        UserSessionStore.SaveUser(user);
        return (currentBondInfo, exp);
    }

    private static void UpdateAttractiveLevel(NetUserAttractiveData attractiveData)
    {
        while (attractiveData.Lv < 40)
        {
            AttractiveLevelRecord? levelInfo = GameData.Instance.AttractiveLevelTable.Values
                .FirstOrDefault(x => x.AttractiveLevel == attractiveData.Lv);
            if (levelInfo == null || attractiveData.Exp < levelInfo.AttractivePoint)
                break;

            attractiveData.Exp -= levelInfo.AttractivePoint;
            attractiveData.Lv++;
        }
    }

    // --- FavoriteItems ---

    public ResListFavoriteItem ListFavoriteItems(ulong userId)
    {
        var response = new ResListFavoriteItem();
        foreach (var item in repo.GetFavoriteItems(userId))
            response.FavoriteItems.Add(item);
        return response;
    }

    public ResGetFavoriteItemLibrary GetFavoriteItemLibrary(ulong userId)
    {
        var response = new ResGetFavoriteItemLibrary();
        long ticks = DateTime.UtcNow.Ticks;
        foreach (var item in repo.GetFavoriteItems(userId))
        {
            response.FavoriteItemLibrary.Add(new NetFavoriteItemLibraryElement
            {
                Tid = item.Tid,
                ReceivedAt = ticks
            });
        }
        return response;
    }

    public ResEquipFavoriteItem EquipFavoriteItem(User user, ReqEquipFavoriteItem req)
    {
        var favoriteItems = repo.GetFavoriteItems(user.ID);
        NetUserFavoriteItemData favoriteItemToEquip = favoriteItems.FirstOrDefault(f => f.FavoriteItemId == req.FavoriteItemId)
            ?? throw new BadHttpRequestException($"FavoriteItem with ID {req.FavoriteItemId} not found", 404);

        CharacterModel character = user.Characters.FirstOrDefault(c => c.Csn == req.Csn)
            ?? throw new BadHttpRequestException($"Character with CSN {req.Csn} not found", 404);

        NetUserFavoriteItemData? existingEquippedItem = favoriteItems.FirstOrDefault(f => f.Csn == req.Csn);
        if (existingEquippedItem != null)
            existingEquippedItem.Csn = 0;

        favoriteItemToEquip.Csn = req.Csn;
        repo.SaveFavoriteItems(user.ID, favoriteItems);

        var response = new ResEquipFavoriteItem();
        foreach (var item in favoriteItems)
            response.FavoriteItems.Add(item);
        return response;
    }

    public ResIncreaseExpFavoriteItem IncreaseExpFavoriteItem(User user, ReqIncreaseExpFavoriteItem req)
    {
        NetUserFavoriteItemData favoriteItem = repo.GetFavoriteItem(user.ID, req.FavoriteItemId)
            ?? throw new BadHttpRequestException($"FavoriteItem with ID {req.FavoriteItemId} not found", 404);

        if (req.ItemData == null)
            throw new BadHttpRequestException("No material item provided", 400);

        DbItemData userItem = user.Items.FirstOrDefault(x => x.Isn == req.ItemData.Isn)
            ?? throw new BadHttpRequestException($"Material item with ISN {req.ItemData.Isn} not found", 404);

        if (userItem.Count < req.ItemData.Count)
            throw new BadHttpRequestException($"Insufficient material. Required: {req.ItemData.Count}, Available: {userItem.Count}", 400);

        GameData.Instance.FavoriteItemTable.TryGetValue(favoriteItem.Tid, out FavoriteItemRecord? favoriteRecord);
        FavoriteItemProbabilityRecord? probabilityData = GetProbabilityData(favoriteItem.Lv, req.ItemData.Tid, favoriteRecord)
            ?? throw new BadHttpRequestException("Cannot upgrade at current level with this material", 400);

        int totalExpGained = 0;
        int actualLoopsUsed = 0;
        bool isGreatSuccess = false;

        for (int i = 0; i < req.LoopCount; i++)
        {
            actualLoopsUsed++;
            isGreatSuccess = Random.Shared.Next(0, 10000) < probabilityData.GreatSuccessRate;

            if (isGreatSuccess)
            {
                favoriteItem.Lv = probabilityData.GreatSuccessLevel;
                favoriteItem.Exp = 0;
                totalExpGained += probabilityData.Exp;
                break;
            }
            else
            {
                favoriteItem.Exp += probabilityData.Exp;
                totalExpGained += probabilityData.Exp;
                ProcessFavoriteItemLevelUp(favoriteItem);

                probabilityData = GetProbabilityData(favoriteItem.Lv, req.ItemData.Tid, favoriteRecord);
                if (probabilityData == null)
                    break;
            }
        }

        int actualUseCount = (req.ItemData.Count / req.LoopCount) * actualLoopsUsed;
        int goldCost = totalExpGained * 10;

        userItem.Count -= actualUseCount;

        if (user.GetCurrencyVal(CurrencyType.Gold) < goldCost)
            throw new BadHttpRequestException($"Insufficient gold. Required: {goldCost}, Available: {user.GetCurrencyVal(CurrencyType.Gold)}", 400);

        user.AddCurrency(CurrencyType.Gold, -goldCost);

        repo.UpsertFavoriteItem(user.ID, favoriteItem);
        UserSessionStore.SaveUser(user);

        return new ResIncreaseExpFavoriteItem
        {
            FavoriteItem = favoriteItem,
            Result = isGreatSuccess ? FavoriteItemGreatSuccessResult.GreatSuccess : FavoriteItemGreatSuccessResult.Success,
            ItemData = NetUtils.ToNet(userItem),
            LoopCount = actualLoopsUsed
        };
    }

    public ResEquipFavoriteItem UpgradeFavoriteItem(User user, ReqIncreaseExpFavoriteItem req)
    {
        var favoriteItems = repo.GetFavoriteItems(user.ID);
        NetUserFavoriteItemData rFavoriteItem = favoriteItems.FirstOrDefault(f => f.FavoriteItemId == req.FavoriteItemId)
            ?? throw new BadHttpRequestException("Favorite item not found", 400);

        int srItemTid = rFavoriteItem.Tid + 1;
        NetUserFavoriteItemData srInventoryItem = favoriteItems.FirstOrDefault(f => f.Tid == srItemTid && f.Csn == 0)
            ?? throw new BadHttpRequestException($"No SR-grade favorite item (TID: {srItemTid}) available in inventory for exchange", 400);

        (int newLevel, int remainingExp) = CalculateExpConversion(rFavoriteItem.Lv, rFavoriteItem.Exp);

        const int exchangeCost = 100000;
        long goldBefore = user.GetCurrencyVal(CurrencyType.Gold);
        if (goldBefore < exchangeCost)
            throw new BadHttpRequestException($"Insufficient gold for exchange. Required: {exchangeCost}, Available: {goldBefore}", 400);

        NetUserFavoriteItemData newSrFavoriteItem = new()
        {
            FavoriteItemId = user.GenerateUniqueItemId(),
            Tid = srItemTid,
            Csn = rFavoriteItem.Csn,
            Lv = newLevel,
            Exp = remainingExp
        };

        favoriteItems.Remove(rFavoriteItem);
        favoriteItems.Remove(srInventoryItem);
        favoriteItems.Add(newSrFavoriteItem);

        user.AddCurrency(CurrencyType.Gold, -exchangeCost);

        repo.SaveFavoriteItems(user.ID, favoriteItems);
        UserSessionStore.SaveUser(user);

        var response = new ResEquipFavoriteItem();
        foreach (var item in favoriteItems)
            response.FavoriteItems.Add(item);
        return response;
    }

    // --- FavoriteItem Quests ---

    public ResListFavoriteItemQuest ListFavoriteItemQuests(ulong userId)
    {
        var response = new ResListFavoriteItemQuest();
        foreach (var quest in repo.GetFavoriteItemQuests(userId))
            response.FavoriteItemQuests.Add(quest);
        return response;
    }

    public ResStartFavoriteItemQuest StartFavoriteItemQuest(ulong userId, int questId)
    {
        repo.UpsertFavoriteItemQuest(userId, new NetUserFavoriteItemQuestData
        {
            QuestId = questId,
            Clear = false,
            Received = false
        });
        return new ResStartFavoriteItemQuest();
    }

    public ResEnterFavoriteItemQuestStage EnterFavoriteItemQuestStage(User user, int stageId)
    {
        user.AddTrigger(Trigger.CampaignStart, 1, stageId);
        UserSessionStore.SaveUser(user);
        return new ResEnterFavoriteItemQuestStage();
    }

    public ResClearFavoriteItemQuestStage ClearFavoriteItemQuestStage(User user, ReqClearFavoriteItemQuestStage req)
    {
        FavoriteItemQuestStageRecord? stageData = GameData.Instance.GetFavoriteItemQuestStageData(req.StageId);
        if (stageData == null)
            return new ResClearFavoriteItemQuestStage();

        FavoriteItemQuestRecord? questData = GameData.Instance.GetFavoriteItemQuestTableData(req.FavoriteItemQuestId);
        if (questData != null)
        {
            var existingQuest = repo.GetFavoriteItemQuest(user.ID, req.FavoriteItemQuestId)
                ?? new NetUserFavoriteItemQuestData { QuestId = req.FavoriteItemQuestId };
            existingQuest.Clear = true;
            repo.UpsertFavoriteItemQuest(user.ID, existingQuest);

            if (questData.NextQuestId > 0 && repo.GetFavoriteItemQuest(user.ID, questData.NextQuestId) == null)
            {
                repo.UpsertFavoriteItemQuest(user.ID, new NetUserFavoriteItemQuestData
                {
                    QuestId = questData.NextQuestId,
                    Clear = false,
                    Received = false
                });
            }
        }

        string stageMapId = GameData.Instance.GetMapIdFromChapter(stageData.ChapterId, stageData.ChapterMod);
        if (progressRepo.CompleteStage(user.ID, stageMapId, req.StageId))
            db.SaveChanges();

        return new ResClearFavoriteItemQuestStage();
    }

    public ResFinishFavoriteItemQuest FinishFavoriteItemQuest(ulong userId, int questId)
    {
        if (!GameData.Instance.FavoriteItemQuestTable.TryGetValue(questId, out FavoriteItemQuestRecord? questData))
            return new ResFinishFavoriteItemQuest();

        var existingQuest = repo.GetFavoriteItemQuest(userId, questId);
        if (existingQuest != null)
        {
            if (existingQuest.Clear)
                return new ResFinishFavoriteItemQuest();

            existingQuest.Clear = true;
            repo.UpsertFavoriteItemQuest(userId, existingQuest);
        }
        else
        {
            repo.UpsertFavoriteItemQuest(userId, new NetUserFavoriteItemQuestData
            {
                QuestId = questId,
                Clear = true,
                Received = false
            });
        }

        if (questData.NextQuestId > 0 && repo.GetFavoriteItemQuest(userId, questData.NextQuestId) == null)
        {
            repo.UpsertFavoriteItemQuest(userId, new NetUserFavoriteItemQuestData
            {
                QuestId = questData.NextQuestId,
                Clear = false,
                Received = false
            });
        }

        return new ResFinishFavoriteItemQuest();
    }

    public ResObtainFavoriteItemQuestReward ObtainFavoriteItemQuestReward(User user, int questId)
    {
        FavoriteItemQuestRecord questData = GameData.Instance.GetFavoriteItemQuestTableData(questId)
            ?? throw new BadHttpRequestException("Quest not found");

        NetUserFavoriteItemQuestData? userQuest = repo.GetFavoriteItemQuest(user.ID, questId);
        if (userQuest == null || !userQuest.Clear || userQuest.Received)
            throw new BadHttpRequestException("Quest not cleared or reward already received");

        List<CharacterRecord> characterRecords = GameData.Instance.CharacterTable.Values
            .Where(c => c.NameCode == questData.NameCode).ToList();
        if (!characterRecords.Any())
            throw new Exception($"Failed to find character record with NameCode: {questData.NameCode}");

        HashSet<int> characterTIds = characterRecords.Select(c => c.Id).ToHashSet();
        CharacterModel? character = user.Characters.FirstOrDefault(c => characterTIds.Contains(c.Tid));
        int characterCsn = character?.Csn ?? 0;

        RewardRecord? reward = GameData.Instance.GetRewardTableEntry(questData.RewardId);
        if (reward?.Rewards == null || reward.Rewards.Length == 0 || reward.Rewards[0].RewardType != RewardType.FavoriteItem)
        {
            if (questData.RewardId > 0 && reward != null)
            {
                NetRewardData rewardData = RewardUtils.RegisterRewardsForUser(user, reward);
                userQuest.Received = true;
                repo.UpsertFavoriteItemQuest(user.ID, userQuest);
                UserSessionStore.SaveUser(user);
                return new ResObtainFavoriteItemQuestReward { UserReward = rewardData };
            }
            throw new Exception("FavoriteItem reward data not found for quest");
        }

        int newItemTId = reward.Rewards[0].RewardId;
        if (character != null)
        {
            var existingEquippedItem = repo.GetFavoriteItems(user.ID).FirstOrDefault(f => f.Csn == characterCsn);
            if (existingEquippedItem != null)
                repo.RemoveFavoriteItem(user.ID, existingEquippedItem.FavoriteItemId);
        }

        NetRewardData finalRewardData = RewardUtils.RegisterRewardsForUser(user, reward);

        if (character != null && finalRewardData.UserFavoriteItems.Count > 0)
        {
            var newFavoriteItem = repo.GetFavoriteItems(user.ID).LastOrDefault(f => f.Tid == newItemTId);
            if (newFavoriteItem != null)
            {
                newFavoriteItem.Csn = characterCsn;
                repo.UpsertFavoriteItem(user.ID, newFavoriteItem);
            }
        }

        userQuest.Received = true;
        repo.UpsertFavoriteItemQuest(user.ID, userQuest);
        UserSessionStore.SaveUser(user);

        return new ResObtainFavoriteItemQuestReward { UserReward = finalRewardData };
    }

    // --- Liberation ---

    public ResGetLiberateData GetLiberateData(ulong userId)
    {
        var response = new ResGetLiberateData();
        int curCharId = UserDbHelper.GetOrCreateGameUser(db, userId).CurrentLiberateCharacterId;
        var liberateData = repo.GetLiberation(userId, curCharId);

        if (liberateData != null)
        {
            if (liberateData.MissionData.Count > 0 && liberateData.MissionData[0].CreatedAt.IsOlderThan24Hours())
            {
                var firstMission = liberateData.MissionData[0];
                var currentMission = GameData.Instance.LiberateMissionTable.Values
                    .FirstOrDefault(x => x.Id == firstMission.MissionTid);

                if (currentMission == null)
                    return response;

                var subGroupIds = GameData.Instance.LiberateMissionTable.Values
                    .Where(x => x.GroupId == currentMission.GroupId)
                    .Select(x => x.SubGroupId)
                    .Distinct()
                    .ToList();

                int randomSubGroupId = subGroupIds[Random.Shared.Next(subGroupIds.Count)];
                var list = LiberateHelper.GetMissions(curCharId, currentMission.GroupId, randomSubGroupId);

                liberateData.MissionData.Clear();
                liberateData.MissionData.AddRange(list);
                liberateData.RewardedCount = 0;
                repo.UpsertLiberation(userId, liberateData);
            }

            response.LiberateData = liberateData;
            response.OpenLiberateTypeIdList.AddRange(repo.GetOpenLiberateTypes(userId));
        }

        return response;
    }

    public ResGetLiberateProgressList GetProgressList(ulong userId)
    {
        var response = new ResGetLiberateProgressList();
        foreach (var item in repo.GetAllLiberations(userId).Values)
        {
            response.LiberateProgressData.Add(new NetLiberateProgressData
            {
                CharacterId = item.CharacterId,
                IsCompleted = item.IsCompleted,
                ProgressPoint = item.ProgressPoint,
                RewardedCount = item.RewardedCount,
                Step = item.Step
            });
        }
        return response;
    }

    public ResChooseLiberateCharacter ChooseCharacter(ulong userId, int characterId)
    {
        Logging.WriteLine($"{characterId}", LogType.Info);
        LiberateCharacterRecord liberatechar = GameData.Instance.LiberateCharacterTable.Values
            .FirstOrDefault(x => x.Id == characterId)!;

        NetLiberateData libd = new()
        {
            CharacterId = liberatechar.Id,
            IsCompleted = false,
            ProgressPoint = 0,
            RewardedCount = 0,
            Step = 1
        };

        LiberateMissionGroupRecord missgroup = GameData.Instance.LiberateMissionGroupTable.Values
            .FirstOrDefault(x => x.MissionStepGroup == liberatechar.MissionStepGroup && x.MissionStep == libd.Step)!;

        List<int> subGroupIds = GameData.Instance.LiberateMissionTable.Values
            .Where(x => x.GroupId == missgroup.DefaultMissionGroupId)
            .Select(g => g.SubGroupId)
            .Distinct()
            .ToList();

        if (subGroupIds.Count > 0)
        {
            int randomSubGroupId = subGroupIds[Random.Shared.Next(subGroupIds.Count)];
            libd.MissionData.AddRange(LiberateHelper.GetMissions(characterId, missgroup.DefaultMissionGroupId, randomSubGroupId));
        }

        if (repo.GetLiberation(userId, characterId) == null)
            repo.UpsertLiberation(userId, libd);

        repo.AddOpenLiberateType(userId, liberatechar.TypeGroupId);

        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.CurrentLiberateCharacterId = characterId;
        db.SaveChanges();

        return new ResChooseLiberateCharacter { Data = libd };
    }

    public ResCompleteLiberateMission CompleteMission(ulong userId, ReqCompleteLiberateMission req)
    {
        var response = new ResCompleteLiberateMission();
        LiberateMissionRecord mission = GameData.Instance.LiberateMissionTable.Values
            .FirstOrDefault(x => x.Id == req.MissionId)!;

        int curCharId = UserDbHelper.GetOrCreateGameUser(db, userId).CurrentLiberateCharacterId;
        var liberateData = repo.GetLiberation(userId, curCharId);
        if (liberateData != null)
        {
            NetLiberateMissionData missionState = liberateData.MissionData.FirstOrDefault(x => x.Id == req.MissionId)!;
            if (missionState.CreatedAt.IsOlderThan24Hours())
            {
                missionState.MissionState = LiberateMissionState.Closed;
                response.Data = liberateData;
                response.Error = LiberateDataExpiredError.Expired;
            }
            else
            {
                missionState.MissionState = LiberateMissionState.Rewarded;
                liberateData.ProgressPoint += mission.MissionPointValue;
                liberateData.RewardedCount += 1;
                response.Data = liberateData;
                response.Error = LiberateDataExpiredError.Success;
            }

            repo.UpsertLiberation(userId, liberateData);
        }

        return response;
    }

    public ResObtainLiberateStepReward ObtainStepReward(User user)
    {
        var response = new ResObtainLiberateStepReward();
        int curCharId = UserDbHelper.GetOrCreateGameUser(db, user.ID).CurrentLiberateCharacterId;
        var liberateData = repo.GetLiberation(user.ID, curCharId);

        if (liberateData != null)
        {
            LiberateCharacterRecord liberatechar = GameData.Instance.LiberateCharacterTable.Values
                .FirstOrDefault(x => x.Id == curCharId)!;

            LiberateMissionGroupRecord group = GameData.Instance.LiberateMissionGroupTable.Values
                .FirstOrDefault(x => x.MissionStepGroup == liberatechar.MissionStepGroup && x.MissionStep == liberateData.Step)!;

            NetRewardData ret = RewardUtils.RegisterRewardsForUser(user, group.RewardId);

            if (liberateData.Step < 5)
            {
                liberateData.Step += 1;
                liberateData.ProgressPoint = 0;
            }
            else
            {
                liberateData.IsCompleted = true;
            }

            repo.UpsertLiberation(user.ID, liberateData);
            UserSessionStore.SaveUser(user);

            response.Data = liberateData;
            response.Error = LiberateDataExpiredError.Success;
            response.Reward = ret;
        }

        return response;
    }

    public ResResetLiberateMission ResetMission(ulong userId, ReqResetLiberateMission req)
    {
        var response = new ResResetLiberateMission();
        int curCharId = UserDbHelper.GetOrCreateGameUser(db, userId).CurrentLiberateCharacterId;
        var liberateData = repo.GetLiberation(userId, curCharId);

        if (liberateData != null)
        {
            NetLiberateMissionData firstMission = liberateData.MissionData.FirstOrDefault(x => x.Id == req.MissionId)!;
            LiberateMissionRecord currentMission = GameData.Instance.LiberateMissionTable.Values
                .FirstOrDefault(x => x.Id == firstMission.MissionTid)!;

            LiberateMissionGroupRecord? resetgroup = GameData.Instance.LiberateMissionGroupTable.Values
                .FirstOrDefault(x => x.DefaultMissionGroupId == currentMission.GroupId && x.MissionStep == liberateData.Step);

            List<int> missIds = GameData.Instance.LiberateMissionTable.Values
                .Where(x => x.GroupId == (resetgroup == null ? currentMission.GroupId : resetgroup.ResetMissionGroupId))
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            int randomId = missIds[Random.Shared.Next(missIds.Count)];
            long nowTicks = DateTime.UtcNow.Ticks;

            firstMission.MissionTid = randomId;
            firstMission.MissionState = LiberateMissionState.Running;
            firstMission.CreatedAt = nowTicks;
            firstMission.ReceivedAt = nowTicks;
            firstMission.TriggerStartAt = nowTicks;
            firstMission.TriggerEndAt = DateTime.UtcNow.AddHours(24).Ticks;

            repo.UpsertLiberation(userId, liberateData);

            response.Data = liberateData;
            response.Error = LiberateDataExpiredError.Success;
        }

        return response;
    }

    private static FavoriteItemProbabilityRecord? GetProbabilityData(
        int currentLevel, int materialId, FavoriteItemRecord? favoriteRecord)
    {
        return GameData.Instance.FavoriteItemProbabilityTable.Values.FirstOrDefault(record =>
            record.NeedItemId == materialId &&
            currentLevel >= record.LevelMin &&
            currentLevel <= record.LevelMax &&
            favoriteRecord!.ProbabilityGroup == record.ProbabilityGroup);
    }

    private static (int NewLevel, int RemainingExp) CalculateExpConversion(int rLevel, int rExp)
    {
        int totalSRExp = (rLevel * 1000) + rExp;
        int srLevel = totalSRExp / 3000;
        int remainingExp = totalSRExp % 3000;

        const int srMaxLevel = 15;
        if (srLevel > srMaxLevel)
        {
            srLevel = srMaxLevel;
            remainingExp = 0;
        }

        return (srLevel, remainingExp);
    }

    private static void ProcessFavoriteItemLevelUp(NetUserFavoriteItemData favoriteItem)
    {
        if (!GameData.Instance.FavoriteItemTable.TryGetValue(favoriteItem.Tid, out FavoriteItemRecord? favoriteRecord))
            return;

        var itemRarity = favoriteRecord.FavoriteRare;
        if (itemRarity == FavoriteItemRare.None)
        {
            if (favoriteItem.Tid >= 100102 && favoriteItem.Tid <= 100602 && favoriteItem.Tid % 100 == 2)
                itemRarity = FavoriteItemRare.SR;
            else if (favoriteItem.Tid >= 100101 && favoriteItem.Tid <= 100601 && favoriteItem.Tid % 100 == 1)
                itemRarity = FavoriteItemRare.R;
            else if (favoriteItem.Tid >= 200101 && favoriteItem.Tid <= 201301 && favoriteItem.Tid % 100 == 1)
                itemRarity = FavoriteItemRare.SSR;
        }

        if (itemRarity == FavoriteItemRare.SSR)
        {
            const int ssrMaxLevel = 2;
            if (favoriteItem.Lv < ssrMaxLevel)
            {
                favoriteItem.Lv++;
                favoriteItem.Exp = 0;
            }
            if (favoriteItem.Lv >= ssrMaxLevel)
                favoriteItem.Exp = 0;
            return;
        }

        var expRecords = GameData.Instance.FavoriteItemExpTable.Values
            .Where(x => x.FavoriteRare == itemRarity)
            .OrderBy(x => x.Level)
            .ToList();
        if (expRecords.Count == 0)
            return;

        const int maxLevel = 15;
        while (favoriteItem.Lv < maxLevel)
        {
            int expRequired = expRecords.FirstOrDefault(x => x.Level == favoriteItem.Lv + 1)?.NeedExp ?? 0;
            if (favoriteItem.Exp >= expRequired && expRequired > 0)
            {
                favoriteItem.Exp -= expRequired;
                favoriteItem.Lv++;
            }
            else
            {
                break;
            }
        }

        if (favoriteItem.Lv >= maxLevel)
            favoriteItem.Exp = 0;
    }
}
