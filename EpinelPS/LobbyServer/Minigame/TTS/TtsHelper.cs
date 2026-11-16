using EpinelPS.Database;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using System.Reflection.PortableExecutable;


namespace EpinelPS.LobbyServer.Minigame.TTS;

public static class TtsHelper
{
   
    public static Duration Add(this Duration left, Duration right)
    {
        return Duration.FromTimeSpan(left.ToTimeSpan()+ right.ToTimeSpan());
    }

    public static Duration Subtract(this Duration left, Duration right)
    {
        return Duration.FromTimeSpan(left.ToTimeSpan() - right.ToTimeSpan());
    }

    private static SongRankData ToSongRankData(TtsSongScoreEntity e) => new()
    {
        UserId = e.UserId,
        SongId = e.SongId,
        RankType = (MiniGameTtsRankingType)e.RankType,
        Difficulty = (MiniGameTtsDifficulty)e.Difficulty,
        Score = e.Score,
        UpdateTime = e.UpdateTime,
        IsDeleted = e.IsDeleted
    };

    // 插入或更新（UPSERT）
    public static void InsertOrUpdate(SongRankData entity)
    {
        using var db = GameContext.CreateNew();
        int rankType = (int)entity.RankType;
        int difficulty = (int)entity.Difficulty;

        var existing = db.Set<TtsSongScoreEntity>().FirstOrDefault(x =>
            x.UserId == entity.UserId &&
            x.SongId == entity.SongId &&
            x.RankType == rankType &&
            x.Difficulty == difficulty);

        if (existing != null)
        {
            // 存在则更新（保留 Id）
            existing.Score = entity.Score;
            existing.UpdateTime = entity.UpdateTime;
            existing.IsDeleted = entity.IsDeleted;
        }
        else
        {
            // 不存在则添加
            db.Set<TtsSongScoreEntity>().Add(new TtsSongScoreEntity
            {
                UserId = entity.UserId,
                SongId = entity.SongId,
                RankType = rankType,
                Difficulty = difficulty,
                Score = entity.Score,
                UpdateTime = entity.UpdateTime,
                IsDeleted = entity.IsDeleted
            });
        }

        db.SaveChanges();
    }

    // 查询单条记录
    public static SongRankData? Get(SqlSongRankKey key)
    {
        using var db = GameContext.CreateNew();
        int rankType = (int)key.RankType;
        int difficulty = (int)key.Difficulty;

        var entity = db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .FirstOrDefault(x =>
                x.UserId == key.UserId &&
                x.SongId == key.SongId &&
                x.RankType == rankType &&
                x.Difficulty == difficulty);

        return entity != null ? ToSongRankData(entity) : null;
    }

    // 获取排行榜（带动态排名）
    /// <summary>
    /// 获取排行榜（带动态排名）
    /// </summary>
    /// <param name="songId"></param>
    /// <param name="rankType"></param>
    /// <param name="difficulty"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    public static List<(SongRankData Entity, int Rank)> GetLeaderboardWithRank(
    int songId,
    MiniGameTtsRankingType rankType,
    MiniGameTtsDifficulty difficulty,
    int limit = 100)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        int diff = (int)difficulty;

        IQueryable<TtsSongScoreEntity> query = db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .Where(x => x.SongId == songId &&
                        x.RankType == rt &&
                        x.Difficulty == diff &&
                        x.IsDeleted == 0)
            .OrderByDescending(x => x.Score);

        if (limit > 0)
            query = query.Take(limit);

        var filtered = query.ToList();
        var result = new List<(SongRankData Entity, int Rank)>(filtered.Count);
        for (int i = 0; i < filtered.Count; i++)
        {
            result.Add((ToSongRankData(filtered[i]), i + 1));
        }

        return result;
    }

    // 获取某个歌曲下所有未删除的排名数据
    public static List<SongRankData> GetBySongId(int songId)
    {
        using var db = GameContext.CreateNew();
        return db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .Where(x => x.SongId == songId && x.IsDeleted == 0)
            .ToList()
            .Select(ToSongRankData)
            .ToList();
    }

    // 查询所有未删除的数据
    public static List<SongRankData> GetAll()
    {
        using var db = GameContext.CreateNew();
        return db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .Where(x => x.IsDeleted == 0)
            .ToList()
            .Select(ToSongRankData)
            .ToList();
    }

    // 获取某个歌曲、某个排名类型、某个难度的排行榜（所有用户的分数排名）
    public static List<SongRankData> GetLeaderboard(int songId, MiniGameTtsRankingType rankType, MiniGameTtsDifficulty difficulty, int limit = 100)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        int diff = (int)difficulty;

        IQueryable<TtsSongScoreEntity> query = db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .Where(x => x.SongId == songId &&
                        x.RankType == rt &&
                        x.Difficulty == diff &&
                        x.IsDeleted == 0)
            .OrderByDescending(x => x.Score);

        if (limit > 0)
            query = query.Take(limit);

        return query.ToList().Select(ToSongRankData).ToList();
    }




    /// <summary>
    /// 获取个人在某个歌曲、某个排名类型下的所有难度数据（带排名）
    /// </summary>
    public static List<(SongRankData Entity, int Rank)> GetUserSongRankingsWithRank(long userId, int songId, MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;

        var songRanks = db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .Where(x => x.SongId == songId && x.RankType == rt && x.IsDeleted == 0)
            .ToList();

        if (songRanks.Count == 0)
            return [];

        // 获取该用户在该歌曲、该排名类型下的所有难度记录
        var userRecords = songRanks
            .Where(x => x.UserId == userId)
            .ToList();

        if (userRecords.Count == 0)
            return [];

        var result = new List<(SongRankData Entity, int Rank)>();

        foreach (var userRecord in userRecords)
        {
            // 计算该用户在该难度下的排名
            var sorted = songRanks
                .Where(x => x.Difficulty == userRecord.Difficulty)
                .OrderByDescending(x => x.Score)
                .ToList();

            int rankPosition = sorted.IndexOf(userRecord) + 1;
            result.Add((ToSongRankData(userRecord), rankPosition));
        }

        // 按难度排序返回
        return result.OrderBy(x => x.Entity.Difficulty).ToList();
    }

    /// <summary>
    /// 获取单个用户的单条记录（带排名）
    /// </summary>
    public static (SongRankData? Entity, int Rank)? GetUserRank(
        long userId,
        int songId,
        MiniGameTtsRankingType rankType,
        MiniGameTtsDifficulty difficulty)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        int diff = (int)difficulty;

        // 计算排名
        var sorted = db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .Where(x => x.SongId == songId &&
                        x.RankType == rt &&
                        x.Difficulty == diff &&
                        x.IsDeleted == 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        // 获取该用户在指定歌曲、排名类型、难度下的记录
        var userRecord = sorted.FirstOrDefault(x => x.UserId == userId);
        if (userRecord == null)
            return null;

        int rankPosition = sorted.IndexOf(userRecord) + 1;

        return (ToSongRankData(userRecord), rankPosition);
    }

    // 软删除
    public static void SoftDelete(SqlSongRankKey key)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)key.RankType;
        int diff = (int)key.Difficulty;

        var record = db.Set<TtsSongScoreEntity>().FirstOrDefault(x =>
            x.UserId == key.UserId &&
            x.SongId == key.SongId &&
            x.RankType == rt &&
            x.Difficulty == diff);

        if (record != null)
        {
            record.IsDeleted = 1;
            record.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            db.SaveChanges();
        }
    }

    // 软删除（批量删除某个歌曲、排名类型、难度下的所有记录）
    public static void SoftDelete(int songId, MiniGameTtsRankingType rankType, MiniGameTtsDifficulty difficulty)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        int diff = (int)difficulty;

        var records = db.Set<TtsSongScoreEntity>()
            .Where(x => x.SongId == songId && x.RankType == rt && x.Difficulty == diff)
            .ToList();

        if (records.Count == 0)
            return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var record in records)
        {
            record.IsDeleted = 1;
            record.UpdateTime = now;
        }

        db.SaveChanges();
    }

    // 软删除指定用户的特定记录
    public static void SoftDeleteUserRecord(SqlSongRankKey key) => SoftDelete(key);

    // 硬删除（物理删除）
    public static void HardDelete(SqlSongRankKey key)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)key.RankType;
        int diff = (int)key.Difficulty;

        var records = db.Set<TtsSongScoreEntity>()
            .Where(x => x.UserId == key.UserId &&
                        x.SongId == key.SongId &&
                        x.RankType == rt &&
                        x.Difficulty == diff)
            .ToList();

        if (records.Count > 0)
        {
            db.Set<TtsSongScoreEntity>().RemoveRange(records);
            db.SaveChanges();
        }
    }



    /// <summary>
    /// 获取某个歌曲、某个排名类型下的所有难度数据（每个难度单独排名）
    /// </summary>
    public static List<(SongRankData Entity, int Rank)> GetBySongIdAndRankTypeWithRank(
        int songId,
        MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        var result = new List<(SongRankData Entity, int Rank)>();

        var songRanks = db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .Where(x => x.SongId == songId && x.RankType == rt && x.IsDeleted == 0)
            .ToList();

        if (songRanks.Count == 0)
            return result;

        // 获取该歌曲、该排名类型下的所有难度
        var difficulties = songRanks
            .Select(x => x.Difficulty)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        foreach (var difficulty in difficulties)
        {
            // 获取该难度下的所有记录，按分数降序排列
            var sorted = songRanks
                .Where(x => x.Difficulty == difficulty)
                .OrderByDescending(x => x.Score)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                result.Add((ToSongRankData(sorted[i]), i + 1));
            }
        }

        return result;
    }


    /// <summary>
    /// 获取某个歌曲、某个排名类型下的所有难度数据（按难度分组，每个难度单独排名）
    /// </summary>
    public static Dictionary<MiniGameTtsDifficulty, List<(SongRankData Entity, int Rank)>>
        GetBySongIdAndRankTypeGrouped(int songId, MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        var result = new Dictionary<MiniGameTtsDifficulty, List<(SongRankData Entity, int Rank)>>();

        var songRanks = db.Set<TtsSongScoreEntity>()
            .AsNoTracking()
            .Where(x => x.SongId == songId && x.RankType == rt && x.IsDeleted == 0)
            .ToList();

        if (songRanks.Count == 0)
            return result;

        // 获取该歌曲、该排名类型下的所有难度
        var difficulties = songRanks
            .Select(x => (MiniGameTtsDifficulty)x.Difficulty)
            .Distinct()
            .ToList();

        foreach (var difficulty in difficulties)
        {
            int diff = (int)difficulty;
            // 获取该难度下的所有记录，按分数降序排列
            var sorted = songRanks
                .Where(x => x.Difficulty == diff)
                .OrderByDescending(x => x.Score)
                .ToList();

            var list = new List<(SongRankData Entity, int Rank)>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                list.Add((ToSongRankData(sorted[i]), i + 1));
            }

            result[difficulty] = list;
        }

        return result;
    }
}
