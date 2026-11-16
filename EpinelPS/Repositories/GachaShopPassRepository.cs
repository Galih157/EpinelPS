using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Repositories;

public sealed class GachaShopPassRepository(GameContext db)
{
    // --- Gacha Pulls & Pity ---

    public void AddGachaPullCount(User user, int bannerId, int pullCount)
    {
        int premiumBanner = GameData.Instance.gachaTypes
            .Where(gt => gt.Value.Type == GachaPremiumType.GachaPremium)
            .Select(gt => gt.Key)
            .FirstOrDefault();

        if (premiumBanner != 0 && bannerId == premiumBanner)
            user.AddTrigger(Trigger.GachaPremium, pullCount);

        UserDbHelper.GetOrCreateGameUser(db, user.ID);

        var set = db.Set<UserGachaBannerPull>();
        var row = set.Local.FirstOrDefault(x => x.UserId == user.ID && x.BannerId == bannerId)
                  ?? set.FirstOrDefault(x => x.UserId == user.ID && x.BannerId == bannerId);

        if (row == null)
        {
            set.Add(new UserGachaBannerPull
            {
                UserId = user.ID,
                BannerId = bannerId,
                MaxPulls = pullCount
            });
        }
        else
        {
            row.MaxPulls += pullCount;
        }
    }

    public int GetGachaCountForType(ulong userId, GachaPremiumType gachaType)
    {
        var bannerIds = GameData.Instance.gachaTypes
            .Where(gt => gt.Value.Type == gachaType)
            .Select(gt => gt.Key)
            .ToList();

        if (bannerIds.Count == 0)
            return 0;

        return db.Set<UserGachaBannerPull>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && bannerIds.Contains(x.BannerId))
            .Sum(x => (int?)x.MaxPulls) ?? 0;
    }

    public int GetGachaTotalCount(ulong userId)
    {
        return db.Set<UserGachaBannerPull>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Sum(x => (int?)x.MaxPulls) ?? 0;
    }

    public void AddPityExecuteCount(ulong userId, int pityBannerId = 101, int pullCount = 1)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserGachaPityCount>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.BannerId == pityBannerId)
                  ?? set.FirstOrDefault(x => x.UserId == userId && x.BannerId == pityBannerId);

        if (row == null)
        {
            set.Add(new UserGachaPityCount
            {
                UserId = userId,
                BannerId = pityBannerId,
                ExecuteCount = pullCount
            });
        }
        else
        {
            row.ExecuteCount += pullCount;
        }
    }

    public int GetPityExecuteCount(ulong userId, int pityBannerId)
    {
        return db.Set<UserGachaPityCount>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.BannerId == pityBannerId)
            .Sum(x => (int?)x.ExecuteCount) ?? 0;
    }

    // --- Selectup & Wishlist ---

    public Dictionary<int, int> GetSelectupChoices(ulong userId)
    {
        return db.Set<UserGachaSelectupChoice>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.BannerId, x => x.CharacterTid);
    }

    public int GetOrAddSelectupChoice(ulong userId, int bannerId, int defaultSelectupId)
    {
        var set = db.Set<UserGachaSelectupChoice>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.BannerId == bannerId)
                  ?? set.FirstOrDefault(x => x.UserId == userId && x.BannerId == bannerId);

        if (row != null)
            return row.CharacterTid;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        set.Add(new UserGachaSelectupChoice
        {
            UserId = userId,
            BannerId = bannerId,
            CharacterTid = defaultSelectupId
        });
        return defaultSelectupId;
    }

    public void SetSelectupChoice(ulong userId, int bannerId, int selectupId)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserGachaSelectupChoice>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.BannerId == bannerId)
                  ?? set.FirstOrDefault(x => x.UserId == userId && x.BannerId == bannerId);

        if (row == null)
        {
            set.Add(new UserGachaSelectupChoice
            {
                UserId = userId,
                BannerId = bannerId,
                CharacterTid = selectupId
            });
        }
        else
        {
            row.CharacterTid = selectupId;
        }
    }

    public List<CharacterWishlistData> GetWishlist(ulong userId)
    {
        return db.Set<UserCharacterWishlistEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new CharacterWishlistData
            {
                BannerId = x.BannerId,
                CharacterId = x.CharacterId
            })
            .ToList();
    }

    public List<CharacterRecord> GetWishlistCharacters(ulong userId, int bannerId = 1)
    {
        var ids = db.Set<UserCharacterWishlistEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.BannerId == bannerId)
            .Select(x => x.CharacterId)
            .ToList();

        return ids
            .Where(id => GameData.Instance.CharacterTable.ContainsKey(id))
            .Select(id => GameData.Instance.CharacterTable[id])
            .ToList();
    }

    public void ReplaceWishlist(ulong userId, List<CharacterWishlistData> wishlist)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserCharacterWishlistEntity>();
        var existing = set.Where(x => x.UserId == userId).ToList();
        var desired = wishlist
            .Select(w => (w.BannerId, w.CharacterId))
            .Distinct()
            .ToHashSet();

        foreach (var row in existing)
        {
            if (!desired.Contains((row.BannerId, row.CharacterId)))
                set.Remove(row);
        }

        var existingKeys = existing
            .Select(x => (x.BannerId, x.CharacterId))
            .ToHashSet();

        foreach (var key in desired)
        {
            if (existingKeys.Contains(key))
                continue;

            set.Add(new UserCharacterWishlistEntity
            {
                UserId = userId,
                BannerId = key.BannerId,
                CharacterId = key.CharacterId
            });
        }
    }

    // --- Daily Free Pulls & Daily Reset ---

    public bool HasDailyFreePull(ulong userId, int gachaId)
    {
        return db.Set<UserGachaDailyFreePull>().Local.Any(x => x.UserId == userId && x.GachaId == gachaId)
               || db.Set<UserGachaDailyFreePull>().AsNoTracking().Any(x => x.UserId == userId && x.GachaId == gachaId);
    }

    public void AddDailyFreePull(ulong userId, int gachaId)
    {
        if (HasDailyFreePull(userId, gachaId))
            return;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        db.Set<UserGachaDailyFreePull>().Add(new UserGachaDailyFreePull
        {
            UserId = userId,
            GachaId = gachaId
        });
    }

    public void ResetDailyGacha(ulong userId)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        gameUser.DailyDiscountUsed = false;

        var set = db.Set<UserGachaDailyFreePull>();
        var rows = set.Where(x => x.UserId == userId).ToList();
        if (rows.Count > 0)
            set.RemoveRange(rows);
    }

    // --- Payback ---

    public Dictionary<int, GachaPaybackData> GetPaybackMap(ulong userId)
    {
        return db.Set<UserGachaPayback>()
            .AsNoTracking()
            .Include(x => x.Rewards)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.GachaId,
                MapPayback);
    }

    public GachaPaybackData? GetPaybackData(ulong userId, int gachaId)
    {
        var row = db.Set<UserGachaPayback>()
            .AsNoTracking()
            .Include(x => x.Rewards)
            .FirstOrDefault(x => x.UserId == userId && x.GachaId == gachaId);

        return row != null ? MapPayback(row) : null;
    }

    public GachaPaybackData IncrementPaybackCount(ulong userId, int gachaId, int count)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserGachaPayback>();
        var row = set.Include(x => x.Rewards)
            .FirstOrDefault(x => x.UserId == userId && x.GachaId == gachaId);

        if (row == null)
        {
            row = new UserGachaPayback
            {
                UserId = userId,
                GachaId = gachaId,
                GachaCount = count
            };
            set.Add(row);
        }
        else
        {
            row.GachaCount += count;
        }

        return MapPayback(row);
    }

    public void AddPaybackRewardedSteps(ulong userId, int gachaId, IEnumerable<int> stepIds)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserGachaPayback>();
        var row = set.Include(x => x.Rewards)
            .FirstOrDefault(x => x.UserId == userId && x.GachaId == gachaId);

        if (row == null)
        {
            row = new UserGachaPayback
            {
                UserId = userId,
                GachaId = gachaId,
                GachaCount = 0
            };
            set.Add(row);
            db.SaveChanges();
        }

        var rewardSet = db.Set<UserGachaPaybackReward>();
        var existing = row.Rewards.Select(r => r.StepId).ToHashSet();
        foreach (var local in rewardSet.Local.Where(r => r.PaybackId == row.Id))
            existing.Add(local.StepId);

        foreach (int stepId in stepIds.Distinct())
        {
            if (!existing.Add(stepId))
                continue;

            rewardSet.Add(new UserGachaPaybackReward
            {
                UserId = userId,
                Payback = row,
                StepId = stepId
            });
        }
    }

    private static GachaPaybackData MapPayback(UserGachaPayback row) => new()
    {
        GachaId = row.GachaId,
        GachaCount = row.GachaCount,
        RewardedStepList = row.Rewards.Select(r => r.StepId).ToList()
    };

    // --- Normal Shop ---

    public NormalShopState? GetNormalShopState(ulong userId, int shopCategory)
    {
        var row = db.Set<UserNormalShopState>()
            .AsNoTracking()
            .Include(x => x.Orders)
            .Include(x => x.PinnedBuys)
            .FirstOrDefault(x => x.UserId == userId && x.ShopCategory == shopCategory);

        if (row == null)
            return null;

        return new NormalShopState
        {
            RenewCount = row.RenewCount,
            RenewAt = row.RenewAt,
            NextRenewAt = row.NextRenewAt,
            ProductIdsByOrder = row.Orders.ToDictionary(o => o.OrderIndex, o => o.ProductId),
            BuyCountsByOrder = row.Orders.ToDictionary(o => o.OrderIndex, o => o.BuyCount),
            PinnedBuyCounts = row.PinnedBuys.ToDictionary(p => p.ProductId, p => p.BuyCount)
        };
    }

    public void SaveNormalShopState(ulong userId, int shopCategory, NormalShopState state)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserNormalShopState>();
        var row = set.Include(x => x.Orders)
            .Include(x => x.PinnedBuys)
            .FirstOrDefault(x => x.UserId == userId && x.ShopCategory == shopCategory);

        if (row == null)
        {
            row = new UserNormalShopState
            {
                UserId = userId,
                ShopCategory = shopCategory,
                RenewCount = state.RenewCount,
                RenewAt = state.RenewAt,
                NextRenewAt = state.NextRenewAt
            };
            set.Add(row);
            db.SaveChanges();
        }
        else
        {
            row.RenewCount = state.RenewCount;
            row.RenewAt = state.RenewAt;
            row.NextRenewAt = state.NextRenewAt;
        }

        var orderSet = db.Set<UserNormalShopOrder>();
        var orderIndices = state.ProductIdsByOrder.Keys.Union(state.BuyCountsByOrder.Keys).ToHashSet();
        var existingOrders = row.Orders.ToDictionary(o => o.OrderIndex);

        foreach (var stale in row.Orders.Where(o => !orderIndices.Contains(o.OrderIndex)).ToList())
            orderSet.Remove(stale);

        foreach (int idx in orderIndices)
        {
            int productId = state.ProductIdsByOrder.GetValueOrDefault(idx, 0);
            int buyCount = state.BuyCountsByOrder.GetValueOrDefault(idx, 0);
            if (existingOrders.TryGetValue(idx, out var order))
            {
                order.ProductId = productId;
                order.BuyCount = buyCount;
            }
            else
            {
                orderSet.Add(new UserNormalShopOrder
                {
                    UserId = userId,
                    ShopState = row,
                    OrderIndex = idx,
                    ProductId = productId,
                    BuyCount = buyCount
                });
            }
        }

        var pinnedSet = db.Set<UserNormalShopPinnedBuy>();
        var pinnedCounts = state.PinnedBuyCounts ?? [];
        var existingPinned = row.PinnedBuys.ToDictionary(p => p.ProductId);

        foreach (var stale in row.PinnedBuys.Where(p => !pinnedCounts.ContainsKey(p.ProductId)).ToList())
            pinnedSet.Remove(stale);

        foreach (var kvp in pinnedCounts)
        {
            if (existingPinned.TryGetValue(kvp.Key, out var pinned))
            {
                pinned.BuyCount = kvp.Value;
            }
            else
            {
                pinnedSet.Add(new UserNormalShopPinnedBuy
                {
                    UserId = userId,
                    ShopState = row,
                    ProductId = kvp.Key,
                    BuyCount = kvp.Value
                });
            }
        }
    }

    // --- Pass ---

    public PassData? GetPassData(ulong userId, int passId)
    {
        var row = db.Set<UserPass>()
            .AsNoTracking()
            .Include(x => x.Ranks)
            .Include(x => x.Missions)
            .FirstOrDefault(x => x.UserId == userId && x.PassId == passId);

        if (row == null)
            return null;

        return new PassData
        {
            PassId = row.PassId,
            PassPoint = row.PassPoint,
            PassSkipCount = row.PassSkipCount,
            PremiumActive = row.PremiumActive,
            LastCompleteAt = row.LastCompleteAt ?? "",
            PassRankList = row.Ranks.Select(r => new PassRankData
            {
                PassRank = r.PassRank,
                IsNormalRewarded = r.IsNormalRewarded,
                IsPremiumRewarded = r.IsPremiumRewarded
            }).ToList(),
            PassMissionList = row.Missions.Select(m => new PassMissionData
            {
                PassMissionId = m.PassMissionId,
                IsComplete = m.IsComplete
            }).ToList()
        };
    }

    public void SavePassData(ulong userId, PassData passData)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserPass>();
        var row = set.Include(x => x.Ranks)
            .Include(x => x.Missions)
            .FirstOrDefault(x => x.UserId == userId && x.PassId == passData.PassId);

        if (row == null)
        {
            row = new UserPass
            {
                UserId = userId,
                PassId = passData.PassId,
                PassPoint = passData.PassPoint,
                PassSkipCount = passData.PassSkipCount,
                PremiumActive = passData.PremiumActive,
                LastCompleteAt = passData.LastCompleteAt ?? ""
            };
            set.Add(row);
            db.SaveChanges();
        }
        else
        {
            row.PassPoint = passData.PassPoint;
            row.PassSkipCount = passData.PassSkipCount;
            row.PremiumActive = passData.PremiumActive;
            row.LastCompleteAt = passData.LastCompleteAt ?? "";
        }

        var rankSet = db.Set<UserPassRank>();
        var desiredRanks = passData.PassRankList
            .GroupBy(r => r.PassRank)
            .Select(g => g.Last())
            .ToDictionary(r => r.PassRank);
        var existingRanks = row.Ranks.ToDictionary(r => r.PassRank);

        foreach (var stale in row.Ranks.Where(r => !desiredRanks.ContainsKey(r.PassRank)).ToList())
            rankSet.Remove(stale);

        foreach (var rank in desiredRanks.Values)
        {
            if (existingRanks.TryGetValue(rank.PassRank, out var existingRank))
            {
                existingRank.IsNormalRewarded = rank.IsNormalRewarded;
                existingRank.IsPremiumRewarded = rank.IsPremiumRewarded;
            }
            else
            {
                rankSet.Add(new UserPassRank
                {
                    UserId = userId,
                    UserPass = row,
                    PassRank = rank.PassRank,
                    IsNormalRewarded = rank.IsNormalRewarded,
                    IsPremiumRewarded = rank.IsPremiumRewarded
                });
            }
        }

        var missionSet = db.Set<UserPassMission>();
        var desiredMissions = passData.PassMissionList
            .GroupBy(m => m.PassMissionId)
            .Select(g => g.Last())
            .ToDictionary(m => m.PassMissionId);
        var existingMissions = row.Missions.ToDictionary(m => m.PassMissionId);

        foreach (var stale in row.Missions.Where(m => !desiredMissions.ContainsKey(m.PassMissionId)).ToList())
            missionSet.Remove(stale);

        foreach (var mission in desiredMissions.Values)
        {
            if (existingMissions.TryGetValue(mission.PassMissionId, out var existingMission))
            {
                existingMission.IsComplete = mission.IsComplete;
            }
            else
            {
                missionSet.Add(new UserPassMission
                {
                    UserId = userId,
                    UserPass = row,
                    PassMissionId = mission.PassMissionId,
                    IsComplete = mission.IsComplete
                });
            }
        }
    }
}
