using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Repositories;

public sealed class MinigameRepository(GameContext db)
{
    public const string TypeArcadeInTheMirror = "ArcadeInTheMirrorData";
    public const string TypeBbqInfo = "BBQInfoData";
    public const string TypeBtg = "BtgData";
    public const string TypeBubbleMarch = "BubbleMarchDatas";
    public const string TypeDdr = "DDRDatas";
    public const string TypeDessertRush = "DessertRushData";
    public const string TypeIsLandBreaker = "IsLandBreakerDatas";
    public const string TypeMiniGameAzx = "MiniGameAzxInfo";
    public const string TypeMiniGameScenarios = "MiniGameScenarios";
    public const string TypeMiniGameStoryChoice = "MiniGameStoryChoice";
    public const string TypeNksv2 = "Nksv2Datas";
    public const string TypePirateCafe = "PirateCafeData";
    public const string TypeRebuildEden = "RebuildEdenDatas";
    public const string TypeSortOut = "SortOutData";
    public const string TypeStellarBlade = "StellarBladeDatas";
    public const string TypeTts = "TTSGameData";
    public const string TypeTowerDefense = "TowerDefenseDatas";
    public const string TypeArcadePlaySoda = "ArcadePlaySodaInfoList";
    public const string TypePlaySoda = "PlaySodaInfoData";

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public T GetSingle<T>(ulong userId, string minigameType) where T : class, new()
    {
        var set = db.Set<UserMinigameEntity>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.MinigameType == minigameType && x.ArcadeId == 0)
                  ?? set.AsNoTracking().FirstOrDefault(x => x.UserId == userId && x.MinigameType == minigameType && x.ArcadeId == 0);

        if (row == null || string.IsNullOrWhiteSpace(row.DataJson))
            return new T();

        return JsonConvert.DeserializeObject<T>(row.DataJson, JsonSettings) ?? new T();
    }

    public void SaveSingle<T>(ulong userId, string minigameType, T data) where T : class
        => SaveDictEntry(userId, minigameType, 0, data);

    public bool TryGetDictEntry<T>(ulong userId, string minigameType, int arcadeId, out T? value) where T : class
    {
        value = null;
        var set = db.Set<UserMinigameEntity>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.MinigameType == minigameType && x.ArcadeId == arcadeId)
                  ?? set.AsNoTracking().FirstOrDefault(x => x.UserId == userId && x.MinigameType == minigameType && x.ArcadeId == arcadeId);

        if (row == null || string.IsNullOrWhiteSpace(row.DataJson))
            return false;

        value = JsonConvert.DeserializeObject<T>(row.DataJson, JsonSettings);
        return value != null;
    }

    public T GetOrCreateDictEntry<T>(ulong userId, string minigameType, int arcadeId) where T : class, new()
    {
        return TryGetDictEntry<T>(userId, minigameType, arcadeId, out var existing) && existing != null
            ? existing
            : new T();
    }

    public Dictionary<int, T> GetDict<T>(ulong userId, string minigameType) where T : class
    {
        var result = new Dictionary<int, T>();
        var set = db.Set<UserMinigameEntity>();
        var rows = set.AsNoTracking()
            .Where(x => x.UserId == userId && x.MinigameType == minigameType)
            .ToList();

        foreach (var local in set.Local.Where(x => x.UserId == userId && x.MinigameType == minigameType))
        {
            if (!rows.Any(r => r.ArcadeId == local.ArcadeId))
                rows.Add(local);
            else
            {
                int idx = rows.FindIndex(r => r.ArcadeId == local.ArcadeId);
                rows[idx] = local;
            }
        }

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.DataJson))
                continue;

            var val = JsonConvert.DeserializeObject<T>(row.DataJson, JsonSettings);
            if (val != null)
                result[row.ArcadeId] = val;
        }

        return result;
    }

    public void SaveDictEntry<T>(ulong userId, string minigameType, int arcadeId, T data) where T : class
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        string json = JsonConvert.SerializeObject(data, JsonSettings);
        long now = DateTime.UtcNow.Ticks;

        var set = db.Set<UserMinigameEntity>();
        var row = set.Local.FirstOrDefault(x => x.UserId == userId && x.MinigameType == minigameType && x.ArcadeId == arcadeId)
                  ?? set.FirstOrDefault(x => x.UserId == userId && x.MinigameType == minigameType && x.ArcadeId == arcadeId);

        if (row == null)
        {
            set.Add(new UserMinigameEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                MinigameType = minigameType,
                ArcadeId = arcadeId,
                DataJson = json,
                UpdatedAt = now
            });
        }
        else if (row.DataJson != json)
        {
            row.DataJson = json;
            row.UpdatedAt = now;
        }
    }
}
