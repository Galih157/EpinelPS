using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database.Domains;

public sealed class MinigameStore : IUserDomainStore
{
    private sealed record MinigameSpec(
        string Type,
        Action<User> Reset,
        Func<User, IEnumerable<(int ArcadeId, object Value)>> ReadEntries,
        Action<User, int, string> ApplyEntry);

    private static MinigameSpec Single<T>(string type, Func<User, T?> get, Action<User, T> set)
        where T : class, new() =>
        new(
            type,
            u => set(u, new T()),
            u => get(u) is { } val ? [(0, val)] : [],
            (u, _, json) => set(u, JsonConvert.DeserializeObject<T>(json, DomainStoreHelpers.JsonSettings) ?? new T()));

    private static MinigameSpec Dict<T>(string type, Func<User, Dictionary<int, T>> get)
        where T : class, new() =>
        new(
            type,
            u => get(u).Clear(),
            u => get(u).Select(kvp => (kvp.Key, (object)kvp.Value)),
            (u, arcadeId, json) =>
            {
                var val = JsonConvert.DeserializeObject<T>(json, DomainStoreHelpers.JsonSettings);
                if (val != null)
                    get(u)[arcadeId] = val;
            });

    private static readonly MinigameSpec[] MinigameSpecs =
    [
        Single(nameof(User.ArcadeInTheMirrorData), u => u.ArcadeInTheMirrorData, (u, v) => u.ArcadeInTheMirrorData = v),
        Single(nameof(User.BBQInfoData), u => u.BBQInfoData, (u, v) => u.BBQInfoData = v),
        Dict(nameof(User.BtgData), u => u.BtgData),
        Dict(nameof(User.BubbleMarchDatas), u => u.BubbleMarchDatas),
        Single(nameof(User.DDRDatas), u => u.DDRDatas, (u, v) => u.DDRDatas = v),
        Single(nameof(User.DessertRushData), u => u.DessertRushData, (u, v) => u.DessertRushData = v),
        Dict(nameof(User.IsLandBreakerDatas), u => u.IsLandBreakerDatas),
        Dict(nameof(User.MiniGameAzxInfo), u => u.MiniGameAzxInfo),
        Dict(nameof(User.MiniGameScenarios), u => u.MiniGameScenarios),
        Dict(nameof(User.MiniGameStoryChoice), u => u.MiniGameStoryChoice),
        Dict(nameof(User.Nksv2Datas), u => u.Nksv2Datas),
        Single(nameof(User.PirateCafeData), u => u.PirateCafeData, (u, v) => u.PirateCafeData = v),
        Dict(nameof(User.RebuildEdenDatas), u => u.RebuildEdenDatas),
        Single(nameof(User.SortOutData), u => u.SortOutData, (u, v) => u.SortOutData = v),
        Dict(nameof(User.StellarBladeDatas), u => u.StellarBladeDatas),
        Dict(nameof(User.TTSGameData), u => u.TTSGameData),
        Dict(nameof(User.TowerDefenseDatas), u => u.TowerDefenseDatas),
        Single(nameof(User.ArcadePlaySodaInfoList), u => u.ArcadePlaySodaInfoList, (u, v) => u.ArcadePlaySodaInfoList = v),
        Single(nameof(User.PlaySodaInfoData), u => u.PlaySodaInfoData, (u, v) => u.PlaySodaInfoData = v),
    ];

    private static readonly Dictionary<string, MinigameSpec> MinigameSpecsByType =
        MinigameSpecs.ToDictionary(s => s.Type);

    public void Load(GameContext db, User user)
    {
        foreach (var spec in MinigameSpecs)
            spec.Reset(user);

        var rows = db.Set<UserMinigameEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == user.ID)
            .ToList();

        foreach (var row in rows)
        {
            if (MinigameSpecsByType.TryGetValue(row.MinigameType, out var spec))
                spec.ApplyEntry(user, row.ArcadeId, row.DataJson);
        }
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;
        long now = DateTime.UtcNow.Ticks;

        var desired = new List<(string MinigameType, int ArcadeId, string DataJson)>();
        foreach (var spec in MinigameSpecs)
        {
            foreach (var (arcadeId, value) in spec.ReadEntries(user))
            {
                desired.Add((spec.Type, arcadeId, JsonConvert.SerializeObject(value, DomainStoreHelpers.JsonSettings)));
            }
        }

        var minigameSet = db.Set<UserMinigameEntity>();
        var existing = minigameSet
            .Where(x => x.UserId == userId)
            .ToList();

        DomainStoreHelpers.SyncCollection(
            minigameSet,
            existing,
            desired,
            e => (e.MinigameType, e.ArcadeId),
            d => (d.MinigameType, d.ArcadeId),
            d => new UserMinigameEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                MinigameType = d.MinigameType,
                ArcadeId = d.ArcadeId,
                DataJson = d.DataJson,
                UpdatedAt = now
            },
            (e, d) =>
            {
                if (e.DataJson != d.DataJson)
                {
                    e.DataJson = d.DataJson;
                    e.UpdatedAt = now;
                }
            });
    }
}
