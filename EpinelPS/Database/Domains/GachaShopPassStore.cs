using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database.Domains;

public sealed class GachaShopPassStore : IUserDomainStore
{
    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        user.GachaBannerMaxPulls = db.Set<UserGachaBannerPull>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.BannerId, x => x.MaxPulls);

        user.GachaPityBannerExecuteCount = db.Set<UserGachaPityCount>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.BannerId, x => x.ExecuteCount);

        user.GachaSelectupChoices = db.Set<UserGachaSelectupChoice>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.BannerId, x => x.CharacterTid);

        user.GachaDailyFreePulls = db.Set<UserGachaDailyFreePull>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.GachaId)
            .ToList();

        user.GachaPaybackData = db.Set<UserGachaPayback>().AsNoTracking()
            .Include(x => x.Rewards)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.GachaId,
                x => new GachaPaybackData
                {
                    GachaId = x.GachaId,
                    GachaCount = x.GachaCount,
                    RewardedStepList = x.Rewards.Select(r => r.StepId).ToList()
                });

        var shopRows = db.Set<UserNormalShopState>().AsNoTracking()
            .Include(x => x.Orders)
            .Include(x => x.PinnedBuys)
            .Where(x => x.UserId == userId)
            .ToList();
        user.NormalShopStates = new Dictionary<int, NormalShopState>(shopRows.Count);
        foreach (var row in shopRows)
        {
            var state = new NormalShopState
            {
                RenewCount = row.RenewCount,
                RenewAt = row.RenewAt,
                NextRenewAt = row.NextRenewAt,
                ProductIdsByOrder = row.Orders.ToDictionary(o => o.OrderIndex, o => o.ProductId),
                BuyCountsByOrder = row.Orders.ToDictionary(o => o.OrderIndex, o => o.BuyCount),
                PinnedBuyCounts = row.PinnedBuys.ToDictionary(p => p.ProductId, p => p.BuyCount)
            };
            user.NormalShopStates[row.ShopCategory] = state;
        }

        user.UserPassInfo = db.Set<UserPass>().AsNoTracking()
            .Include(x => x.Ranks)
            .Include(x => x.Missions)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.PassId,
                x => new PassData
                {
                    PassId = x.PassId,
                    PassPoint = x.PassPoint,
                    PassSkipCount = x.PassSkipCount,
                    PremiumActive = x.PremiumActive,
                    LastCompleteAt = x.LastCompleteAt ?? "",
                    PassRankList = x.Ranks.Select(r => new PassRankData
                    {
                        PassRank = r.PassRank,
                        IsNormalRewarded = r.IsNormalRewarded,
                        IsPremiumRewarded = r.IsPremiumRewarded
                    }).ToList(),
                    PassMissionList = x.Missions.Select(m => new PassMissionData
                    {
                        PassMissionId = m.PassMissionId,
                        IsComplete = m.IsComplete
                    }).ToList()
                });

        user.CharacterWishlist = db.Set<UserCharacterWishlistEntity>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new CharacterWishlistData
            {
                BannerId = x.BannerId,
                CharacterId = x.CharacterId
            })
            .ToList();
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        DomainStoreHelpers.SyncCollection(
            db.Set<UserGachaBannerPull>(),
            db.Set<UserGachaBannerPull>().Where(x => x.UserId == userId).ToList(),
            user.GachaBannerMaxPulls,
            e => e.BannerId,
            kvp => kvp.Key,
            kvp => new UserGachaBannerPull
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                BannerId = kvp.Key,
                MaxPulls = kvp.Value
            },
            (e, kvp) => e.MaxPulls = kvp.Value);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserGachaPityCount>(),
            db.Set<UserGachaPityCount>().Where(x => x.UserId == userId).ToList(),
            user.GachaPityBannerExecuteCount,
            e => e.BannerId,
            kvp => kvp.Key,
            kvp => new UserGachaPityCount
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                BannerId = kvp.Key,
                ExecuteCount = kvp.Value
            },
            (e, kvp) => e.ExecuteCount = kvp.Value);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserGachaSelectupChoice>(),
            db.Set<UserGachaSelectupChoice>().Where(x => x.UserId == userId).ToList(),
            user.GachaSelectupChoices,
            e => e.BannerId,
            kvp => kvp.Key,
            kvp => new UserGachaSelectupChoice
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                BannerId = kvp.Key,
                CharacterTid = kvp.Value
            },
            (e, kvp) => e.CharacterTid = kvp.Value);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserGachaDailyFreePull>(),
            db.Set<UserGachaDailyFreePull>().Where(x => x.UserId == userId).ToList(),
            user.GachaDailyFreePulls,
            e => e.GachaId,
            id => id,
            id => new UserGachaDailyFreePull
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                GachaId = id
            },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserGachaPayback>(),
            db.Set<UserGachaPayback>().Include(x => x.Rewards).Where(x => x.UserId == userId).ToList(),
            user.GachaPaybackData,
            e => e.GachaId,
            kvp => kvp.Key,
            kvp =>
            {
                var payback = new UserGachaPayback
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    GachaId = kvp.Key,
                    GachaCount = kvp.Value.GachaCount
                };
                foreach (int stepId in kvp.Value.RewardedStepList.Distinct())
                {
                    db.Set<UserGachaPaybackReward>().Add(new UserGachaPaybackReward
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        PaybackId = payback.Id,
                        StepId = stepId
                    });
                }
                return payback;
            },
            (e, kvp) =>
            {
                e.GachaCount = kvp.Value.GachaCount;
                DomainStoreHelpers.SyncCollection(
                    db.Set<UserGachaPaybackReward>(),
                    e.Rewards,
                    kvp.Value.RewardedStepList,
                    r => r.StepId,
                    stepId => stepId,
                    stepId => new UserGachaPaybackReward
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        PaybackId = e.Id,
                        StepId = stepId
                    },
                    (_, _) => { });
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserNormalShopState>(),
            db.Set<UserNormalShopState>().Include(x => x.Orders).Include(x => x.PinnedBuys).Where(x => x.UserId == userId).ToList(),
            user.NormalShopStates,
            e => e.ShopCategory,
            kvp => kvp.Key,
            kvp =>
            {
                var shopState = new UserNormalShopState
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    ShopCategory = kvp.Key,
                    RenewCount = kvp.Value.RenewCount,
                    RenewAt = kvp.Value.RenewAt,
                    NextRenewAt = kvp.Value.NextRenewAt
                };
                foreach (int orderIndex in kvp.Value.ProductIdsByOrder.Keys.Union(kvp.Value.BuyCountsByOrder.Keys))
                {
                    db.Set<UserNormalShopOrder>().Add(new UserNormalShopOrder
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        ShopStateId = shopState.Id,
                        OrderIndex = orderIndex,
                        ProductId = kvp.Value.ProductIdsByOrder.GetValueOrDefault(orderIndex, 0),
                        BuyCount = kvp.Value.BuyCountsByOrder.GetValueOrDefault(orderIndex, 0)
                    });
                }
                foreach (var pinned in kvp.Value.PinnedBuyCounts)
                {
                    db.Set<UserNormalShopPinnedBuy>().Add(new UserNormalShopPinnedBuy
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        ShopStateId = shopState.Id,
                        ProductId = pinned.Key,
                        BuyCount = pinned.Value
                    });
                }
                return shopState;
            },
            (e, kvp) =>
            {
                e.RenewCount = kvp.Value.RenewCount;
                e.RenewAt = kvp.Value.RenewAt;
                e.NextRenewAt = kvp.Value.NextRenewAt;

                var orderIndices = kvp.Value.ProductIdsByOrder.Keys.Union(kvp.Value.BuyCountsByOrder.Keys);
                DomainStoreHelpers.SyncCollection(
                    db.Set<UserNormalShopOrder>(),
                    e.Orders,
                    orderIndices,
                    o => o.OrderIndex,
                    idx => idx,
                    idx => new UserNormalShopOrder
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        ShopStateId = e.Id,
                        OrderIndex = idx,
                        ProductId = kvp.Value.ProductIdsByOrder.GetValueOrDefault(idx, 0),
                        BuyCount = kvp.Value.BuyCountsByOrder.GetValueOrDefault(idx, 0)
                    },
                    (o, idx) =>
                    {
                        o.ProductId = kvp.Value.ProductIdsByOrder.GetValueOrDefault(idx, 0);
                        o.BuyCount = kvp.Value.BuyCountsByOrder.GetValueOrDefault(idx, 0);
                    });

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserNormalShopPinnedBuy>(),
                    e.PinnedBuys,
                    kvp.Value.PinnedBuyCounts,
                    p => p.ProductId,
                    p => p.Key,
                    p => new UserNormalShopPinnedBuy
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        ShopStateId = e.Id,
                        ProductId = p.Key,
                        BuyCount = p.Value
                    },
                    (p, src) => p.BuyCount = src.Value);
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserPass>(),
            db.Set<UserPass>().Include(x => x.Ranks).Include(x => x.Missions).Where(x => x.UserId == userId).ToList(),
            user.UserPassInfo,
            e => e.PassId,
            kvp => kvp.Key,
            kvp =>
            {
                var pass = new UserPass
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    PassId = kvp.Key,
                    PassPoint = kvp.Value.PassPoint,
                    PassSkipCount = kvp.Value.PassSkipCount,
                    PremiumActive = kvp.Value.PremiumActive,
                    LastCompleteAt = kvp.Value.LastCompleteAt ?? ""
                };
                foreach (var rank in kvp.Value.PassRankList.GroupBy(r => r.PassRank).Select(g => g.Last()))
                {
                    db.Set<UserPassRank>().Add(new UserPassRank
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserPassId = pass.Id,
                        PassRank = rank.PassRank,
                        IsNormalRewarded = rank.IsNormalRewarded,
                        IsPremiumRewarded = rank.IsPremiumRewarded
                    });
                }
                foreach (var mission in kvp.Value.PassMissionList.GroupBy(m => m.PassMissionId).Select(g => g.Last()))
                {
                    db.Set<UserPassMission>().Add(new UserPassMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserPassId = pass.Id,
                        PassMissionId = mission.PassMissionId,
                        IsComplete = mission.IsComplete
                    });
                }
                return pass;
            },
            (e, kvp) =>
            {
                e.PassPoint = kvp.Value.PassPoint;
                e.PassSkipCount = kvp.Value.PassSkipCount;
                e.PremiumActive = kvp.Value.PremiumActive;
                e.LastCompleteAt = kvp.Value.LastCompleteAt ?? "";

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserPassRank>(),
                    e.Ranks,
                    kvp.Value.PassRankList,
                    r => r.PassRank,
                    r => r.PassRank,
                    r => new UserPassRank
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserPassId = e.Id,
                        PassRank = r.PassRank,
                        IsNormalRewarded = r.IsNormalRewarded,
                        IsPremiumRewarded = r.IsPremiumRewarded
                    },
                    (r, src) =>
                    {
                        r.IsNormalRewarded = src.IsNormalRewarded;
                        r.IsPremiumRewarded = src.IsPremiumRewarded;
                    });

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserPassMission>(),
                    e.Missions,
                    kvp.Value.PassMissionList,
                    m => m.PassMissionId,
                    m => m.PassMissionId,
                    m => new UserPassMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserPassId = e.Id,
                        PassMissionId = m.PassMissionId,
                        IsComplete = m.IsComplete
                    },
                    (m, src) => m.IsComplete = src.IsComplete);
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserCharacterWishlistEntity>(),
            db.Set<UserCharacterWishlistEntity>().Where(x => x.UserId == userId).ToList(),
            user.CharacterWishlist ?? [],
            e => (e.BannerId, e.CharacterId),
            w => (w.BannerId, w.CharacterId),
            w => new UserCharacterWishlistEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                BannerId = w.BannerId,
                CharacterId = w.CharacterId
            },
            (_, _) => { });
    }
}
