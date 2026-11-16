using EpinelPS.Database;
using EpinelPS.LobbyServer;
using Microsoft.EntityFrameworkCore;
using System.Reflection.PortableExecutable;

namespace EpinelPS.Models;

public class RankData
{
    public Dictionary<int, List<SongRankData>> SongRankDatas { get; set; } = [];
    public MiniGameTtsTotalRankData TtsRankDatas { get; set; } = new();
    public Dictionary<int,List<ArcadeScoreRecord>> ArcadeScore { get; set; } = [];

}


public class MiniGameTtsTotalRankData
{
    public List<MiniGameTtsTotalRankRecord> TtsTotalRankRecords { get; set; } = [];

    private static MiniGameTtsTotalRankRecord ToRecord(TtsTotalScoreEntity e) => new()
    {
        UserId = e.UserId,
        RankType = (MiniGameTtsRankingType)e.RankType,
        Score = e.Score,
        UpdatedAt = e.UpdatedAt
    };

    /// <summary>
    /// 根据 UserId + RankType 查找记录
    /// </summary>
    public MiniGameTtsTotalRankRecord? FindRecord(long userId, MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        var entity = db.Set<TtsTotalScoreEntity>()
            .AsNoTracking()
            .FirstOrDefault(r => r.UserId == userId && r.RankType == rt);

        return entity != null ? ToRecord(entity) : null;
    }

    /// <summary>
    /// 插入或更新记录（UPSERT）
    /// </summary>
    public void InsertOrUpdate(long userId, MiniGameTtsRankingType rankType, long score)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        long now = DateTime.UtcNow.Ticks;

        var existing = db.Set<TtsTotalScoreEntity>()
            .FirstOrDefault(r => r.UserId == userId && r.RankType == rt);

        if (existing != null)
        {
            // 更新现有记录
            existing.Score = score;
            existing.UpdatedAt = now;
        }
        else
        {
            // 插入新记录
            db.Set<TtsTotalScoreEntity>().Add(new TtsTotalScoreEntity
            {
                UserId = userId,
                RankType = rt,
                Score = score,
                UpdatedAt = now
            });
        }

        db.SaveChanges();
    }

    /// <summary>
    /// 获取用户特定类型的排名（按分数降序计算）
    /// </summary>
    public (long Score, int Rank)? GetUserRank(long userId, MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        var ranked = db.Set<TtsTotalScoreEntity>()
            .AsNoTracking()
            .Where(r => r.RankType == rt)
            .OrderByDescending(r => r.Score)
            .ToList();

        var record = ranked.FirstOrDefault(r => r.UserId == userId);
        if (record == null) return null;

        int rank = ranked.IndexOf(record) + 1;
        return (record.Score, rank);
    }

    /// <summary>
    /// 获取指定类型的全部排行榜
    /// </summary>
    /// <param name="rankType">类型0=Union, 1=Friend, 2=Server</param>
    /// <returns></returns>
    public List<NetMiniGameTtsTotalRankData> TotalGetLeaderboardByRankType(MiniGameTtsRankingType rankType)
        => GetLeaderboardForResponse(rankType);

    /// <summary>
    /// 获取用户在指定类型中的排名
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="rankType">类型0=Union, 1=Friend, 2=Server</param>
    /// <returns></returns>
    public NetMiniGameTtsTotalRankData? TotalGetUserRank(long userId, MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;

        // 获取该类型下所有记录，按分数降序排列
        var ranked = db.Set<TtsTotalScoreEntity>()
            .AsNoTracking()
            .Where(x => x.RankType == rt)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (ranked.Count == 0)
            return null;

        // 查找用户记录
        var userRecord = ranked.FirstOrDefault(x => x.UserId == userId);
        if (userRecord == null)
            return null;

        // 计算排名
        int rankPosition = ranked.IndexOf(userRecord) + 1;

        // 构建返回数据
        return new NetMiniGameTtsTotalRankData
        {
            User = LobbyHandler.CreateWholeUserDataFromDbUser((ulong)userRecord.UserId),
            Score = userRecord.Score,
            Position = rankPosition
        };
    }


    /// <summary>
    /// 获取排行榜（指定类型的前N名）
    /// </summary>
    public List<(MiniGameTtsTotalRankRecord Record, int Rank)> TotalGetLeaderboardByRankType(
        MiniGameTtsRankingType rankType,
        int? limit = null)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;

        IQueryable<TtsTotalScoreEntity> query = db.Set<TtsTotalScoreEntity>()
            .AsNoTracking()
            .Where(r => r.RankType == rt)
            .OrderByDescending(r => r.Score);

        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        var ranked = query.ToList();
        var result = new List<(MiniGameTtsTotalRankRecord, int)>(ranked.Count);
        for (int i = 0; i < ranked.Count; i++)
        {
            result.Add((ToRecord(ranked[i]), i + 1));
        }
        return result;
    }


    /// <summary>
    /// 获取指定类型的排行榜（用于返回 NetMiniGameTtsTotalRankData）
    /// </summary>
    public List<NetMiniGameTtsTotalRankData> GetLeaderboardForResponse(
        MiniGameTtsRankingType rankType,
        int? limit = null)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;

        IQueryable<TtsTotalScoreEntity> query = db.Set<TtsTotalScoreEntity>()
            .AsNoTracking()
            .Where(r => r.RankType == rt)
            .OrderByDescending(r => r.Score);

        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        var ranked = query.ToList();
        var result = new List<NetMiniGameTtsTotalRankData>(ranked.Count);
        for (int i = 0; i < ranked.Count; i++)
        {
            var data = new NetMiniGameTtsTotalRankData
            {
                User = LobbyHandler.CreateWholeUserDataFromDbUser((ulong)ranked[i].UserId),
                Score = ranked[i].Score,
                Position = i + 1
            };
            result.Add(data);
        }
        return result;
    }

    /// <summary>
    /// 获取用户在指定类型中的分数
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="rankType">类型0=Union, 1=Friend, 2=Server</param>
    /// <returns></returns>
    public long TotalGetUserScore(long userId, MiniGameTtsRankingType rankType)
        => GetUserScore(userId, rankType);


    /// <summary>
    /// 获取排行榜总人数
    /// </summary>
    /// <param name="rankType">类型0=Union, 1=Friend, 2=Server</param>
    /// <returns></returns>
    public int TotalGetTotalCount(MiniGameTtsRankingType rankType)
        => GetRecordCount(rankType);


    /// <summary>
    /// 删除用户在指定类型中的记录
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="rankType">类型0=Union, 1=Friend, 2=Server</param>
    public void TotalDeleteUserRank(long userId, MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        var records = db.Set<TtsTotalScoreEntity>()
            .Where(x => x.UserId == userId && x.RankType == rt)
            .ToList();

        if (records.Count > 0)
        {
            db.Set<TtsTotalScoreEntity>().RemoveRange(records);
            db.SaveChanges();
        }
    }



    /// <summary>
    /// 删除指定用户的所有记录
    /// </summary>
    public int DeleteAllByUser(long userId)
    {
        using var db = GameContext.CreateNew();
        var records = db.Set<TtsTotalScoreEntity>()
            .Where(r => r.UserId == userId)
            .ToList();

        if (records.Count == 0)
            return 0;

        db.Set<TtsTotalScoreEntity>().RemoveRange(records);
        db.SaveChanges();
        return records.Count;
    }

    /// <summary>
    /// 检查是否存在指定记录
    /// </summary>
    public bool Exists(long userId, MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        return db.Set<TtsTotalScoreEntity>().Any(r => r.UserId == userId && r.RankType == rt);
    }

    /// <summary>
    /// 获取用户在指定类型中的分数（不存在时返回 0）
    /// </summary>
    public long GetUserScore(long userId, MiniGameTtsRankingType rankType)
    {
        return FindRecord(userId, rankType)?.Score ?? 0;
    }

    /// <summary>
    /// 获取指定类型的记录总数
    /// </summary>
    public int GetRecordCount(MiniGameTtsRankingType rankType)
    {
        using var db = GameContext.CreateNew();
        int rt = (int)rankType;
        return db.Set<TtsTotalScoreEntity>().Count(r => r.RankType == rt);
    }
}