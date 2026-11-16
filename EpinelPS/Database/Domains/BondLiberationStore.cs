using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database.Domains;

public sealed class BondLiberationStore : IUserDomainStore
{
    private const string BondKindFlushableWatchedDialog = "FlushableWatchedDialog";
    private const string BondKindObtainedRewardLevel = "ObtainedRewardLevel";
    private const string BondKindCounselDialogComplete = "CounselDialogComplete";

    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        var bondRows = db.Set<UserBond>().AsNoTracking()
            .Include(x => x.ProgressItems)
            .Where(x => x.UserId == userId)
            .ToList();
        user.BondInfo = bondRows.Select(b =>
        {
            var data = new NetUserAttractiveData
            {
                NameCode = b.NameCode,
                Lv = b.Lv,
                Exp = b.Exp,
                CanCounselToday = b.CanCounselToday,
                IsFavorites = b.IsFavorites,
                CounseledCount = b.CounseledCount,
                CompleteRewardStatus = (CounselDialogCompleteRewardStatus)b.CompleteRewardStatus
            };
            data.FlushableWatchedDialogIds.AddRange(b.ProgressItems.Where(p => p.Kind == BondKindFlushableWatchedDialog).Select(p => p.ItemId));
            data.ObtainedRewardLevels.AddRange(b.ProgressItems.Where(p => p.Kind == BondKindObtainedRewardLevel).Select(p => p.ItemId));
            data.CounselDialogCompleteIds.AddRange(b.ProgressItems.Where(p => p.Kind == BondKindCounselDialogComplete).Select(p => p.ItemId));
            return data;
        }).ToList();

        user.FavoriteItems = db.Set<UserFavoriteItem>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetUserFavoriteItemData
            {
                FavoriteItemId = x.FavoriteItemId,
                Tid = x.Tid,
                Csn = x.Csn,
                Lv = x.Lv,
                Exp = x.Exp
            })
            .ToList();

        user.FavoriteItemQuests = db.Set<UserFavoriteItemQuest>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetUserFavoriteItemQuestData
            {
                QuestId = x.QuestId,
                Clear = x.Clear,
                Received = x.Received
            })
            .ToList();

        var liberationRows = db.Set<UserLiberation>().AsNoTracking()
            .Include(x => x.Missions)
            .Where(x => x.UserId == userId)
            .ToList();
        user.LiberateDatas = new Dictionary<int, NetLiberateData>(liberationRows.Count);
        foreach (var row in liberationRows)
        {
            var data = new NetLiberateData
            {
                CharacterId = row.CharacterId,
                Step = row.Step,
                ProgressPoint = row.ProgressPoint,
                RewardedCount = row.RewardedCount,
                IsCompleted = row.IsCompleted
            };
            data.MissionData.AddRange(row.Missions.Select(m => new NetLiberateMissionData
            {
                Id = m.MissionId,
                MissionTid = m.MissionTid,
                LiberateCharacterId = m.LiberateCharacterId,
                MissionState = (LiberateMissionState)m.MissionState,
                CreatedAt = m.CreatedAt,
                TriggerStartAt = m.TriggerStartAt,
                TriggerEndAt = m.TriggerEndAt,
                ReceivedAt = m.ReceivedAt
            }));
            user.LiberateDatas[row.CharacterId] = data;
        }

        user.OpenLiberateTypeIdList = db.Set<UserOpenLiberateType>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.LiberateTypeId)
            .ToList();
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        DomainStoreHelpers.SyncCollection(
            db.Set<UserBond>(),
            db.Set<UserBond>().Include(x => x.ProgressItems).Where(x => x.UserId == userId).ToList(),
            user.BondInfo,
            e => e.NameCode,
            b => b.NameCode,
            b =>
            {
                var bond = new UserBond
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    NameCode = b.NameCode,
                    Lv = b.Lv,
                    Exp = b.Exp,
                    CanCounselToday = b.CanCounselToday,
                    IsFavorites = b.IsFavorites,
                    CounseledCount = b.CounseledCount,
                    CompleteRewardStatus = (int)b.CompleteRewardStatus
                };
                foreach (var item in GetBondProgressItems(b).Distinct())
                {
                    db.Set<UserBondProgressItem>().Add(new UserBondProgressItem
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        BondId = bond.Id,
                        Kind = item.Kind,
                        ItemId = item.ItemId
                    });
                }
                return bond;
            },
            (e, b) =>
            {
                e.Lv = b.Lv;
                e.Exp = b.Exp;
                e.CanCounselToday = b.CanCounselToday;
                e.IsFavorites = b.IsFavorites;
                e.CounseledCount = b.CounseledCount;
                e.CompleteRewardStatus = (int)b.CompleteRewardStatus;

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserBondProgressItem>(),
                    e.ProgressItems,
                    GetBondProgressItems(b),
                    p => (p.Kind, p.ItemId),
                    p => p,
                    p => new UserBondProgressItem
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        BondId = e.Id,
                        Kind = p.Kind,
                        ItemId = p.ItemId
                    },
                    (_, _) => { });
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserFavoriteItem>(),
            db.Set<UserFavoriteItem>().Where(x => x.UserId == userId).ToList(),
            user.FavoriteItems,
            e => e.FavoriteItemId,
            s => s.FavoriteItemId,
            s => new UserFavoriteItem
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                FavoriteItemId = s.FavoriteItemId,
                Tid = s.Tid,
                Csn = s.Csn,
                Lv = s.Lv,
                Exp = s.Exp
            },
            (e, s) =>
            {
                e.Tid = s.Tid;
                e.Csn = s.Csn;
                e.Lv = s.Lv;
                e.Exp = s.Exp;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserFavoriteItemQuest>(),
            db.Set<UserFavoriteItemQuest>().Where(x => x.UserId == userId).ToList(),
            user.FavoriteItemQuests,
            e => e.QuestId,
            s => s.QuestId,
            s => new UserFavoriteItemQuest
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                QuestId = s.QuestId,
                Clear = s.Clear,
                Received = s.Received
            },
            (e, s) =>
            {
                e.Clear = s.Clear;
                e.Received = s.Received;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserLiberation>(),
            db.Set<UserLiberation>().Include(x => x.Missions).Where(x => x.UserId == userId).ToList(),
            user.LiberateDatas,
            e => e.CharacterId,
            kvp => kvp.Key,
            kvp =>
            {
                var lib = new UserLiberation
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    CharacterId = kvp.Key,
                    Step = kvp.Value.Step,
                    ProgressPoint = kvp.Value.ProgressPoint,
                    RewardedCount = kvp.Value.RewardedCount,
                    IsCompleted = kvp.Value.IsCompleted
                };
                foreach (var m in kvp.Value.MissionData.GroupBy(x => x.Id).Select(g => g.Last()))
                {
                    db.Set<UserLiberationMission>().Add(new UserLiberationMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LiberationId = lib.Id,
                        MissionId = m.Id,
                        MissionTid = m.MissionTid,
                        LiberateCharacterId = m.LiberateCharacterId,
                        MissionState = (int)m.MissionState,
                        CreatedAt = m.CreatedAt,
                        TriggerStartAt = m.TriggerStartAt,
                        TriggerEndAt = m.TriggerEndAt,
                        ReceivedAt = m.ReceivedAt
                    });
                }
                return lib;
            },
            (e, kvp) =>
            {
                e.Step = kvp.Value.Step;
                e.ProgressPoint = kvp.Value.ProgressPoint;
                e.RewardedCount = kvp.Value.RewardedCount;
                e.IsCompleted = kvp.Value.IsCompleted;

                DomainStoreHelpers.SyncCollection(
                    db.Set<UserLiberationMission>(),
                    e.Missions,
                    kvp.Value.MissionData,
                    m => m.MissionId,
                    m => m.Id,
                    m => new UserLiberationMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LiberationId = e.Id,
                        MissionId = m.Id,
                        MissionTid = m.MissionTid,
                        LiberateCharacterId = m.LiberateCharacterId,
                        MissionState = (int)m.MissionState,
                        CreatedAt = m.CreatedAt,
                        TriggerStartAt = m.TriggerStartAt,
                        TriggerEndAt = m.TriggerEndAt,
                        ReceivedAt = m.ReceivedAt
                    },
                    (m, src) =>
                    {
                        m.MissionTid = src.MissionTid;
                        m.LiberateCharacterId = src.LiberateCharacterId;
                        m.MissionState = (int)src.MissionState;
                        m.CreatedAt = src.CreatedAt;
                        m.TriggerStartAt = src.TriggerStartAt;
                        m.TriggerEndAt = src.TriggerEndAt;
                        m.ReceivedAt = src.ReceivedAt;
                    });
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserOpenLiberateType>(),
            db.Set<UserOpenLiberateType>().Where(x => x.UserId == userId).ToList(),
            user.OpenLiberateTypeIdList,
            e => e.LiberateTypeId,
            id => id,
            id => new UserOpenLiberateType
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                LiberateTypeId = id
            },
            (_, _) => { });
    }

    private static IEnumerable<(string Kind, int ItemId)> GetBondProgressItems(NetUserAttractiveData bond)
    {
        return bond.FlushableWatchedDialogIds.Select(id => (BondKindFlushableWatchedDialog, id))
            .Concat(bond.ObtainedRewardLevels.Select(id => (BondKindObtainedRewardLevel, id)))
            .Concat(bond.CounselDialogCompleteIds.Select(id => (BondKindCounselDialogComplete, id)));
    }
}
