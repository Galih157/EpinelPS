using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Repositories;

public sealed class BondLiberationRepository(GameContext db)
{
    private const string BondKindFlushableWatchedDialog = "FlushableWatchedDialog";
    private const string BondKindObtainedRewardLevel = "ObtainedRewardLevel";
    private const string BondKindCounselDialogComplete = "CounselDialogComplete";

    // --- Bonds ---

    public List<NetUserAttractiveData> GetBonds(ulong userId)
    {
        return db.Set<UserBond>()
            .AsNoTracking()
            .Include(x => x.ProgressItems)
            .Where(x => x.UserId == userId)
            .AsEnumerable()
            .Select(MapBond)
            .ToList();
    }

    public NetUserAttractiveData? GetBond(ulong userId, int nameCode)
    {
        var row = db.Set<UserBond>()
            .AsNoTracking()
            .Include(x => x.ProgressItems)
            .FirstOrDefault(x => x.UserId == userId && x.NameCode == nameCode);

        return row != null ? MapBond(row) : null;
    }

    public void EnsureBond(ulong userId, int nameCode, int defaultLv = 1)
    {
        var bondSet = db.Set<UserBond>();
        if (bondSet.Local.Any(x => x.UserId == userId && x.NameCode == nameCode) ||
            bondSet.AsNoTracking().Any(x => x.UserId == userId && x.NameCode == nameCode))
        {
            return;
        }

        UserDbHelper.GetOrCreateGameUser(db, userId);
        bondSet.Add(new UserBond
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            NameCode = nameCode,
            Lv = defaultLv
        });
        db.SaveChanges();
    }

    public void UpsertBond(ulong userId, NetUserAttractiveData bond)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var bondSet = db.Set<UserBond>();
        var progressSet = db.Set<UserBondProgressItem>();

        var row = bondSet
            .Include(x => x.ProgressItems)
            .FirstOrDefault(x => x.UserId == userId && x.NameCode == bond.NameCode);

        var desiredItems = GetBondProgressItems(bond).ToHashSet();

        if (row == null)
        {
            row = new UserBond
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                NameCode = bond.NameCode,
                Lv = bond.Lv,
                Exp = bond.Exp,
                CanCounselToday = bond.CanCounselToday,
                IsFavorites = bond.IsFavorites,
                CounseledCount = bond.CounseledCount,
                CompleteRewardStatus = (int)bond.CompleteRewardStatus
            };
            bondSet.Add(row);

            foreach (var item in desiredItems)
            {
                progressSet.Add(new UserBondProgressItem
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    BondId = row.Id,
                    Kind = item.Kind,
                    ItemId = item.ItemId
                });
            }
        }
        else
        {
            row.Lv = bond.Lv;
            row.Exp = bond.Exp;
            row.CanCounselToday = bond.CanCounselToday;
            row.IsFavorites = bond.IsFavorites;
            row.CounseledCount = bond.CounseledCount;
            row.CompleteRewardStatus = (int)bond.CompleteRewardStatus;

            var existingItems = row.ProgressItems.Select(p => (p.Kind, p.ItemId)).ToHashSet();
            foreach (var stale in row.ProgressItems.Where(p => !desiredItems.Contains((p.Kind, p.ItemId))).ToList())
                progressSet.Remove(stale);
            foreach (var item in desiredItems.Where(i => !existingItems.Contains(i)))
            {
                progressSet.Add(new UserBondProgressItem
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    BondId = row.Id,
                    Kind = item.Kind,
                    ItemId = item.ItemId
                });
            }
        }

        db.SaveChanges();
    }

    // --- FavoriteItems ---

    public List<NetUserFavoriteItemData> GetFavoriteItems(ulong userId)
    {
        return db.Set<UserFavoriteItem>()
            .AsNoTracking()
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
    }

    public NetUserFavoriteItemData? GetFavoriteItem(ulong userId, long favoriteItemId)
    {
        var x = db.Set<UserFavoriteItem>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId && x.FavoriteItemId == favoriteItemId);

        return x == null ? null : new NetUserFavoriteItemData
        {
            FavoriteItemId = x.FavoriteItemId,
            Tid = x.Tid,
            Csn = x.Csn,
            Lv = x.Lv,
            Exp = x.Exp
        };
    }

    public void AddFavoriteItem(ulong userId, NetUserFavoriteItemData item)
    {
        UpsertFavoriteItem(userId, item);
    }

    public void UpsertFavoriteItem(ulong userId, NetUserFavoriteItemData item)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserFavoriteItem>();
        var row = set.FirstOrDefault(x => x.UserId == userId && x.FavoriteItemId == item.FavoriteItemId);
        if (row == null)
        {
            set.Add(new UserFavoriteItem
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                FavoriteItemId = item.FavoriteItemId,
                Tid = item.Tid,
                Csn = item.Csn,
                Lv = item.Lv,
                Exp = item.Exp
            });
        }
        else
        {
            row.Tid = item.Tid;
            row.Csn = item.Csn;
            row.Lv = item.Lv;
            row.Exp = item.Exp;
        }

        db.SaveChanges();
    }

    public void RemoveFavoriteItem(ulong userId, long favoriteItemId)
    {
        var set = db.Set<UserFavoriteItem>();
        var row = set.FirstOrDefault(x => x.UserId == userId && x.FavoriteItemId == favoriteItemId);
        if (row != null)
        {
            set.Remove(row);
            db.SaveChanges();
        }
    }

    public void SaveFavoriteItems(ulong userId, IEnumerable<NetUserFavoriteItemData> items)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserFavoriteItem>();
        var existingById = set.Where(x => x.UserId == userId).ToDictionary(x => x.FavoriteItemId);
        var desiredById = items.GroupBy(i => i.FavoriteItemId).ToDictionary(g => g.Key, g => g.Last());

        foreach (var stale in existingById.Values.Where(e => !desiredById.ContainsKey(e.FavoriteItemId)).ToList())
            set.Remove(stale);

        foreach (var (id, src) in desiredById)
        {
            if (existingById.TryGetValue(id, out var entity))
            {
                entity.Tid = src.Tid;
                entity.Csn = src.Csn;
                entity.Lv = src.Lv;
                entity.Exp = src.Exp;
            }
            else
            {
                set.Add(new UserFavoriteItem
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    FavoriteItemId = src.FavoriteItemId,
                    Tid = src.Tid,
                    Csn = src.Csn,
                    Lv = src.Lv,
                    Exp = src.Exp
                });
            }
        }

        db.SaveChanges();
    }

    // --- FavoriteItemQuests ---

    public List<NetUserFavoriteItemQuestData> GetFavoriteItemQuests(ulong userId)
    {
        return db.Set<UserFavoriteItemQuest>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetUserFavoriteItemQuestData
            {
                QuestId = x.QuestId,
                Clear = x.Clear,
                Received = x.Received
            })
            .ToList();
    }

    public NetUserFavoriteItemQuestData? GetFavoriteItemQuest(ulong userId, int questId)
    {
        var x = db.Set<UserFavoriteItemQuest>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId && x.QuestId == questId);

        return x == null ? null : new NetUserFavoriteItemQuestData
        {
            QuestId = x.QuestId,
            Clear = x.Clear,
            Received = x.Received
        };
    }

    public void UpsertFavoriteItemQuest(ulong userId, NetUserFavoriteItemQuestData quest)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var set = db.Set<UserFavoriteItemQuest>();
        var row = set.FirstOrDefault(x => x.UserId == userId && x.QuestId == quest.QuestId);
        if (row == null)
        {
            set.Add(new UserFavoriteItemQuest
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                QuestId = quest.QuestId,
                Clear = quest.Clear,
                Received = quest.Received
            });
        }
        else
        {
            row.Clear = quest.Clear;
            row.Received = quest.Received;
        }

        db.SaveChanges();
    }

    // --- Liberation ---

    public Dictionary<int, NetLiberateData> GetAllLiberations(ulong userId)
    {
        return db.Set<UserLiberation>()
            .AsNoTracking()
            .Include(x => x.Missions)
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.CharacterId, MapLiberation);
    }

    public NetLiberateData? GetLiberation(ulong userId, int characterId)
    {
        var row = db.Set<UserLiberation>()
            .AsNoTracking()
            .Include(x => x.Missions)
            .FirstOrDefault(x => x.UserId == userId && x.CharacterId == characterId);

        return row != null ? MapLiberation(row) : null;
    }

    public void UpsertLiberation(ulong userId, NetLiberateData data)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var libSet = db.Set<UserLiberation>();
        var missionSet = db.Set<UserLiberationMission>();

        var row = libSet
            .Include(x => x.Missions)
            .FirstOrDefault(x => x.UserId == userId && x.CharacterId == data.CharacterId);

        var desiredMissions = data.MissionData
            .GroupBy(m => m.Id)
            .ToDictionary(g => g.Key, g => g.Last());

        if (row == null)
        {
            row = new UserLiberation
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                CharacterId = data.CharacterId,
                Step = data.Step,
                ProgressPoint = data.ProgressPoint,
                RewardedCount = data.RewardedCount,
                IsCompleted = data.IsCompleted
            };
            libSet.Add(row);

            foreach (var m in desiredMissions.Values)
            {
                missionSet.Add(new UserLiberationMission
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    LiberationId = row.Id,
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
        }
        else
        {
            row.Step = data.Step;
            row.ProgressPoint = data.ProgressPoint;
            row.RewardedCount = data.RewardedCount;
            row.IsCompleted = data.IsCompleted;

            var existingById = row.Missions.ToDictionary(m => m.MissionId);
            foreach (var stale in row.Missions.Where(m => !desiredMissions.ContainsKey(m.MissionId)).ToList())
                missionSet.Remove(stale);

            foreach (var (missionId, src) in desiredMissions)
            {
                if (existingById.TryGetValue(missionId, out var entity))
                {
                    entity.MissionTid = src.MissionTid;
                    entity.LiberateCharacterId = src.LiberateCharacterId;
                    entity.MissionState = (int)src.MissionState;
                    entity.CreatedAt = src.CreatedAt;
                    entity.TriggerStartAt = src.TriggerStartAt;
                    entity.TriggerEndAt = src.TriggerEndAt;
                    entity.ReceivedAt = src.ReceivedAt;
                }
                else
                {
                    missionSet.Add(new UserLiberationMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LiberationId = row.Id,
                        MissionId = src.Id,
                        MissionTid = src.MissionTid,
                        LiberateCharacterId = src.LiberateCharacterId,
                        MissionState = (int)src.MissionState,
                        CreatedAt = src.CreatedAt,
                        TriggerStartAt = src.TriggerStartAt,
                        TriggerEndAt = src.TriggerEndAt,
                        ReceivedAt = src.ReceivedAt
                    });
                }
            }
        }

        db.SaveChanges();
    }

    public List<int> GetOpenLiberateTypes(ulong userId)
    {
        return db.Set<UserOpenLiberateType>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.LiberateTypeId)
            .ToList();
    }

    public void AddOpenLiberateType(ulong userId, int liberateTypeId)
    {
        var set = db.Set<UserOpenLiberateType>();
        if (set.Local.Any(x => x.UserId == userId && x.LiberateTypeId == liberateTypeId) ||
            set.AsNoTracking().Any(x => x.UserId == userId && x.LiberateTypeId == liberateTypeId))
        {
            return;
        }

        UserDbHelper.GetOrCreateGameUser(db, userId);
        set.Add(new UserOpenLiberateType
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            LiberateTypeId = liberateTypeId
        });
        db.SaveChanges();
    }

    private static NetUserAttractiveData MapBond(UserBond b)
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
    }

    private static NetLiberateData MapLiberation(UserLiberation row)
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
        return data;
    }

    private static IEnumerable<(string Kind, int ItemId)> GetBondProgressItems(NetUserAttractiveData bond)
    {
        return bond.FlushableWatchedDialogIds.Select(id => (BondKindFlushableWatchedDialog, id))
            .Concat(bond.ObtainedRewardLevels.Select(id => (BondKindObtainedRewardLevel, id)))
            .Concat(bond.CounselDialogCompleteIds.Select(id => (BondKindCounselDialogComplete, id)));
    }
}
