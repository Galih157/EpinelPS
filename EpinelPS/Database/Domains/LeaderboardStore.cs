using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database.Domains;

public static class LeaderboardStore
{
    private const string ArcadeMinigameType = "Arcade";
    private static readonly Lock SyncLock = new();

    public static void LoadAll(RankData rank)
    {
        using var db = GameContext.CreateNew();
        LoadRankData(db, rank);
    }

    public static void SaveAll(RankData rank)
    {
        using var db = GameContext.CreateNew();
        using var tx = db.Database.BeginTransaction();
        SaveRankData(db, rank);
        tx.Commit();
    }

    public static void LoadRankData(GameContext db, RankData rank)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(rank);

        lock (SyncLock)
        {
            rank.SongRankDatas.Clear();
            foreach (var group in db.Set<TtsSongScoreEntity>().AsNoTracking().ToList().GroupBy(e => e.SongId))
            {
                rank.SongRankDatas[group.Key] = group.Select(e => new SongRankData
                {
                    UserId = e.UserId,
                    SongId = e.SongId,
                    RankType = (MiniGameTtsRankingType)e.RankType,
                    Difficulty = (MiniGameTtsDifficulty)e.Difficulty,
                    Score = e.Score,
                    UpdateTime = e.UpdateTime,
                    IsDeleted = e.IsDeleted
                }).ToList();
            }

            rank.TtsRankDatas ??= new MiniGameTtsTotalRankData();
            rank.TtsRankDatas.TtsTotalRankRecords = db.Set<TtsTotalScoreEntity>()
                .AsNoTracking()
                .ToList()
                .Select(e => new MiniGameTtsTotalRankRecord
                {
                    UserId = e.UserId,
                    RankType = (MiniGameTtsRankingType)e.RankType,
                    Score = e.Score,
                    UpdatedAt = e.UpdatedAt
                })
                .ToList();

            rank.ArcadeScore.Clear();
            foreach (var group in db.Set<ArcadeScoreEntity>().AsNoTracking().ToList().GroupBy(e => e.ArcadeId))
            {
                rank.ArcadeScore[group.Key] = group.Select(e => new ArcadeScoreRecord
                {
                    Id = e.Id,
                    GuildId = e.GuildId,
                    UserId = (ulong)e.UserId,
                    Score = e.Score,
                    ArcadeId = e.ArcadeId,
                    ModeId = e.ModeId
                }).ToList();
            }
        }
    }

    public static void SaveRankData(GameContext db, RankData rank)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(rank);

        lock (SyncLock)
        {
            SaveTtsSongScores(db, rank);
            SaveTtsTotalScores(db, rank);
            var addedArcadeRecords = SaveArcadeScores(db, rank);
            SyncArcadeLeaderboards(db, rank);

            db.SaveChanges();

            foreach (var (record, entity) in addedArcadeRecords)
            {
                record.Id = entity.Id;
            }
        }
    }

    private static void SaveTtsSongScores(GameContext db, RankData rank)
    {
        var incoming = new Dictionary<(long UserId, int SongId, int RankType, int Difficulty), SongRankData>();
        foreach (var list in rank.SongRankDatas.Values)
        {
            foreach (var item in list)
            {
                incoming[(item.UserId, item.SongId, (int)item.RankType, (int)item.Difficulty)] = item;
            }
        }

        var songScoreSet = db.Set<TtsSongScoreEntity>();
        var existing = songScoreSet.ToList();

        DomainStoreHelpers.SyncCollection(
            songScoreSet,
            existing,
            incoming,
            e => (e.UserId, e.SongId, e.RankType, e.Difficulty),
            kvp => kvp.Key,
            kvp => new TtsSongScoreEntity
            {
                UserId = kvp.Key.UserId,
                SongId = kvp.Key.SongId,
                RankType = kvp.Key.RankType,
                Difficulty = kvp.Key.Difficulty,
                Score = kvp.Value.Score,
                UpdateTime = kvp.Value.UpdateTime,
                IsDeleted = kvp.Value.IsDeleted
            },
            (entity, kvp) =>
            {
                entity.Score = kvp.Value.Score;
                entity.UpdateTime = kvp.Value.UpdateTime;
                entity.IsDeleted = kvp.Value.IsDeleted;
            });
    }

    private static void SaveTtsTotalScores(GameContext db, RankData rank)
    {
        var incoming = new Dictionary<(long UserId, int RankType), MiniGameTtsTotalRankRecord>();
        if (rank.TtsRankDatas?.TtsTotalRankRecords != null)
        {
            foreach (var item in rank.TtsRankDatas.TtsTotalRankRecords)
            {
                incoming[(item.UserId, (int)item.RankType)] = item;
            }
        }

        var totalScoreSet = db.Set<TtsTotalScoreEntity>();
        var existing = totalScoreSet.ToList();

        DomainStoreHelpers.SyncCollection(
            totalScoreSet,
            existing,
            incoming,
            e => (e.UserId, e.RankType),
            kvp => kvp.Key,
            kvp => new TtsTotalScoreEntity
            {
                UserId = kvp.Key.UserId,
                RankType = kvp.Key.RankType,
                Score = kvp.Value.Score,
                UpdatedAt = kvp.Value.UpdatedAt
            },
            (entity, kvp) =>
            {
                entity.Score = kvp.Value.Score;
                entity.UpdatedAt = kvp.Value.UpdatedAt;
            });
    }

    private static List<(ArcadeScoreRecord Record, ArcadeScoreEntity Entity)> SaveArcadeScores(GameContext db, RankData rank)
    {
        var arcadeScoreSet = db.Set<ArcadeScoreEntity>();
        var existing = arcadeScoreSet.ToList();
        var existingById = existing.ToDictionary(e => e.Id);
        var seenIds = new HashSet<int>();
        var addedWithZeroId = new List<(ArcadeScoreRecord Record, ArcadeScoreEntity Entity)>();

        foreach (var list in rank.ArcadeScore.Values)
        {
            foreach (var record in list)
            {
                if (record.Id > 0 && existingById.TryGetValue(record.Id, out var entity))
                {
                    seenIds.Add(record.Id);
                    entity.GuildId = record.GuildId;
                    entity.UserId = (long)record.UserId;
                    entity.Score = record.Score;
                    entity.ArcadeId = record.ArcadeId;
                    entity.ModeId = record.ModeId;
                }
                else
                {
                    var newEntity = new ArcadeScoreEntity
                    {
                        Id = record.Id > 0 ? record.Id : 0,
                        GuildId = record.GuildId,
                        UserId = (long)record.UserId,
                        Score = record.Score,
                        ArcadeId = record.ArcadeId,
                        ModeId = record.ModeId
                    };
                    if (record.Id > 0)
                    {
                        seenIds.Add(record.Id);
                    }
                    else
                    {
                        addedWithZeroId.Add((record, newEntity));
                    }
                    arcadeScoreSet.Add(newEntity);
                }
            }
        }

        var stale = existing.Where(e => !seenIds.Contains(e.Id)).ToList();
        if (stale.Count > 0)
            arcadeScoreSet.RemoveRange(stale);

        return addedWithZeroId;
    }

    private static void SyncArcadeLeaderboards(GameContext db, RankData rank)
    {
        var incoming = new Dictionary<(ulong UserId, long GuildId, int ArcadeId, int ModeId), ArcadeScoreRecord>();
        foreach (var list in rank.ArcadeScore.Values)
        {
            foreach (var record in list)
            {
                incoming[(record.UserId, record.GuildId, record.ArcadeId, record.ModeId)] = record;
            }
        }

        foreach (var userId in incoming.Keys.Select(k => k.UserId).Distinct())
        {
            UserDbHelper.GetOrCreateGameUser(db, userId);
        }

        var leaderboardSet = db.Set<MiniGameLeaderboardEntity>();
        var existing = leaderboardSet
            .Where(e => e.MinigameType == ArcadeMinigameType)
            .ToList();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        DomainStoreHelpers.SyncCollection(
            leaderboardSet,
            existing,
            incoming,
            e => (e.UserId, e.GuildId, e.ArcadeId, e.ModeId),
            kvp => kvp.Key,
            kvp => new MiniGameLeaderboardEntity
            {
                MinigameType = ArcadeMinigameType,
                ArcadeId = kvp.Key.ArcadeId,
                ModeId = kvp.Key.ModeId,
                UserId = kvp.Key.UserId,
                GuildId = kvp.Key.GuildId,
                Score = kvp.Value.Score,
                UpdatedAt = now
            },
            (entity, kvp) =>
            {
                if (entity.Score != kvp.Value.Score)
                {
                    entity.Score = kvp.Value.Score;
                    entity.UpdatedAt = now;
                }
            });
    }
}
