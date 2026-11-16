using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database.Domains;

internal static class DomainStoreHelpers
{
    internal static readonly JsonSerializerSettings JsonSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    internal static void SyncCollection<TEntity, TKey, TSource>(
        DbSet<TEntity> dbSet,
        IEnumerable<TEntity> existingRows,
        IEnumerable<TSource> desiredItems,
        Func<TEntity, TKey> entityKey,
        Func<TSource, TKey> sourceKey,
        Func<TSource, TEntity> createEntity,
        Action<TEntity, TSource> updateEntity)
        where TEntity : class
        where TKey : notnull
    {
        var existingByKey = existingRows.ToDictionary(entityKey);
        var seenKeys = new HashSet<TKey>();

        foreach (var item in desiredItems)
        {
            var key = sourceKey(item);
            if (!seenKeys.Add(key))
                continue;

            if (existingByKey.TryGetValue(key, out var existing))
            {
                updateEntity(existing, item);
            }
            else
            {
                dbSet.Add(createEntity(item));
            }
        }

        foreach (var (key, existing) in existingByKey)
        {
            if (!seenKeys.Contains(key))
            {
                dbSet.Remove(existing);
            }
        }
    }
}
