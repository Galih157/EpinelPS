using System.Linq.Expressions;
using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Repositories;

/// <summary>
/// Repository for character, item, item-character assignment, and equipment awakening persistence.
/// Mutations stage changes on <see cref="GameContext"/> without calling <c>SaveChanges</c> so callers
/// can commit multiple operations in a single unit of work.
/// </summary>
public sealed class InventoryRepository(GameContext db)
{
    // --- Session Bulk Sync (Characters & Items; preserves EquipmentAwakenings) ---

    public void LoadUserInventory(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.Characters = GetCharacters(user.ID);
        user.Items = GetItems(user.ID);
    }

    public void SaveUserInventory(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ulong userId = user.ID;
        UserDbHelper.GetOrCreateGameUser(db, userId);

        // Characters first so ItemCharacterAssignments FK is satisfied
        var characters = user.Characters ?? [];
        var validCharacterCsns = new HashSet<long>();
        SyncCollection(
            db.Set<CharacterEntity>(),
            db.Set<CharacterEntity>().Where(c => c.UserId == userId),
            characters,
            c => c.Csn,
            c => (long)c.Csn,
            c =>
            {
                validCharacterCsns.Add(c.Csn);
                return new CharacterEntity
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    Csn = c.Csn,
                    Tid = c.Tid,
                    CostumeId = c.CostumeId,
                    Level = c.Level,
                    UltimateLevel = c.UltimateLevel,
                    Skill1Lvl = c.Skill1Lvl,
                    Skill2Lvl = c.Skill2Lvl,
                    Grade = c.Grade,
                    IsMainForce = c.IsMainForce
                };
            },
            (entity, c) =>
            {
                validCharacterCsns.Add(c.Csn);
                entity.Tid = c.Tid;
                entity.CostumeId = c.CostumeId;
                entity.Level = c.Level;
                entity.UltimateLevel = c.UltimateLevel;
                entity.Skill1Lvl = c.Skill1Lvl;
                entity.Skill2Lvl = c.Skill2Lvl;
                entity.Grade = c.Grade;
                entity.IsMainForce = c.IsMainForce;
            });

        // Items
        var items = user.Items ?? [];
        var itemsByIsn = new Dictionary<long, ItemEntity>();
        SyncCollection(
            db.Set<ItemEntity>(),
            db.Set<ItemEntity>().Where(i => i.UserId == userId),
            items,
            i => i.Isn,
            i => i.Isn,
            i =>
            {
                var entity = new ItemEntity
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    Isn = i.Isn,
                    ItemType = i.ItemType,
                    Csn = i.Csn,
                    Count = i.Count,
                    Level = i.Level,
                    Exp = i.Exp,
                    Position = i.Position,
                    Corp = i.Corp
                };
                itemsByIsn[i.Isn] = entity;
                return entity;
            },
            (entity, i) =>
            {
                entity.ItemType = i.ItemType;
                entity.Csn = i.Csn;
                entity.Count = i.Count;
                entity.Level = i.Level;
                entity.Exp = i.Exp;
                entity.Position = i.Position;
                entity.Corp = i.Corp;
                itemsByIsn[i.Isn] = entity;
            });

        // ItemCharacterAssignments (skipping any Csn not in validCharacterCsns)
        var existingAssignments = db.Set<ItemCharacterAssignmentEntity>()
            .Where(a => a.UserId == userId)
            .ToList();

        var desiredAssignments = new List<ItemCharacterAssignmentEntity>();
        foreach (var item in items)
        {
            if (!itemsByIsn.TryGetValue(item.Isn, out var itemEntity))
                continue;

            var seenCsns = new HashSet<long>();
            int pos = 0;
            foreach (long csn in item.CsnList ?? [])
            {
                if (!validCharacterCsns.Contains(csn) || !seenCsns.Add(csn))
                    continue;

                desiredAssignments.Add(new ItemCharacterAssignmentEntity
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    ItemId = itemEntity.Id,
                    CharacterCsn = csn,
                    Position = pos++
                });
            }
        }

        var existingAssignmentSet = existingAssignments
            .Select(a => (a.ItemId, a.CharacterCsn, a.Position))
            .ToHashSet();
        var desiredAssignmentSet = desiredAssignments
            .Select(a => (a.ItemId, a.CharacterCsn, a.Position))
            .ToHashSet();

        if (!existingAssignmentSet.SetEquals(desiredAssignmentSet))
        {
            db.Set<ItemCharacterAssignmentEntity>().RemoveRange(existingAssignments);
            db.Set<ItemCharacterAssignmentEntity>().AddRange(desiredAssignments);
        }
    }

    // --- Characters ---

    public List<CharacterModel> GetCharacters(ulong userId)
    {
        return GetMergedEntities<CharacterEntity>(
                c => c.UserId == userId,
                c => c.UserId == userId,
                c => c.Id)
            .Select(MapToCharacterModel)
            .ToList();
    }

    public CharacterModel? GetCharacterByTid(ulong userId, int tid)
    {
        var matchingTids = ResolveNameCodeTids(tid);
        var entity = GetMergedEntities<CharacterEntity>(
                c => c.UserId == userId && matchingTids.Contains(c.Tid),
                c => c.UserId == userId && matchingTids.Contains(c.Tid),
                c => c.Id)
            .FirstOrDefault();

        return entity != null ? MapToCharacterModel(entity) : null;
    }

    public CharacterModel? GetCharacterByCsn(ulong userId, long csn)
    {
        if (csn <= 0)
            return null;

        var entity = GetMergedEntities<CharacterEntity>(
                c => c.UserId == userId && c.Csn == csn,
                c => c.UserId == userId && c.Csn == csn,
                c => c.Id)
            .FirstOrDefault();

        return entity != null ? MapToCharacterModel(entity) : null;
    }

    public bool HasCharacter(ulong userId, int tid)
    {
        var matchingTids = ResolveNameCodeTids(tid);
        return GetMergedEntities<CharacterEntity>(
                c => c.UserId == userId && matchingTids.Contains(c.Tid),
                c => c.UserId == userId && matchingTids.Contains(c.Tid),
                c => c.Id)
            .Count > 0;
    }

    public long GenerateNextCsn(ulong userId)
    {
        var existingCsns = GetMergedEntities<CharacterEntity>(
                c => c.UserId == userId,
                c => c.UserId == userId,
                c => c.Id)
            .Select(c => c.Csn);

        return ComputeNextSerial(existingCsns);
    }

    public CharacterModel AddCharacter(ulong userId, CharacterModel character)
    {
        ArgumentNullException.ThrowIfNull(character);
        UserDbHelper.GetOrCreateGameUser(db, userId);

        long csn = character.Csn > 0 ? character.Csn : GenerateNextCsn(userId);
        if (character.Csn <= 0 && csn <= int.MaxValue)
            character.Csn = (int)csn;

        var entity = new CharacterEntity
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            Csn = csn,
            Tid = character.Tid,
            CostumeId = character.CostumeId,
            Level = character.Level,
            UltimateLevel = character.UltimateLevel,
            Skill1Lvl = character.Skill1Lvl,
            Skill2Lvl = character.Skill2Lvl,
            Grade = character.Grade,
            IsMainForce = character.IsMainForce
        };

        db.Set<CharacterEntity>().Add(entity);
        return MapToCharacterModel(entity);
    }

    public bool UpdateCharacter(ulong userId, CharacterModel character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (character.Csn <= 0)
            return false;

        long csn = character.Csn;
        var entity = FindTrackedEntity<CharacterEntity>(
            c => c.UserId == userId && c.Csn == csn,
            c => c.UserId == userId && c.Csn == csn);

        if (entity == null)
            return false;

        entity.Tid = character.Tid;
        entity.CostumeId = character.CostumeId;
        entity.Level = character.Level;
        entity.UltimateLevel = character.UltimateLevel;
        entity.Skill1Lvl = character.Skill1Lvl;
        entity.Skill2Lvl = character.Skill2Lvl;
        entity.Grade = character.Grade;
        entity.IsMainForce = character.IsMainForce;
        return true;
    }

    // --- Items & Item Character Assignments ---

    public List<DbItemData> GetItems(ulong userId)
    {
        bool hasTrackerChanges = db.ChangeTracker.Entries<ItemEntity>().Any(e => e.Entity.UserId == userId)
            || db.ChangeTracker.Entries<ItemCharacterAssignmentEntity>().Any(e => e.Entity.UserId == userId);

        if (!hasTrackerChanges)
        {
            return db.Set<ItemEntity>()
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .Include(i => i.CharacterAssignments)
                .AsEnumerable()
                .Select(i => MapToDbItemData(
                    i,
                    i.CharacterAssignments.OrderBy(a => a.Position).Select(a => a.CharacterCsn).ToList()))
                .ToList();
        }

        var items = GetMergedEntities<ItemEntity>(
            i => i.UserId == userId,
            i => i.UserId == userId,
            i => i.Id);

        var assignmentsByItemId = GetMergedEntities<ItemCharacterAssignmentEntity>(
                a => a.UserId == userId,
                a => a.UserId == userId,
                a => a.Id)
            .GroupBy(a => a.ItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.Position).Select(a => a.CharacterCsn).ToList());

        return items
            .Select(i => MapToDbItemData(i, assignmentsByItemId.GetValueOrDefault(i.Id) ?? []))
            .ToList();
    }

    public DbItemData? GetItemByIsn(ulong userId, long isn)
    {
        var item = GetMergedEntities<ItemEntity>(
                i => i.UserId == userId && i.Isn == isn,
                i => i.UserId == userId && i.Isn == isn,
                i => i.Id)
            .FirstOrDefault();

        if (item == null)
            return null;

        var csnList = GetAssignmentsForItemId(userId, item.Id);
        return MapToDbItemData(item, csnList);
    }

    public DbItemData? GetItemByType(ulong userId, int itemType)
    {
        var item = GetMergedEntities<ItemEntity>(
                i => i.UserId == userId && i.ItemType == itemType,
                i => i.UserId == userId && i.ItemType == itemType,
                i => i.Id)
            .FirstOrDefault();

        if (item == null)
            return null;

        var csnList = GetAssignmentsForItemId(userId, item.Id);
        return MapToDbItemData(item, csnList);
    }

    public DbItemData? GetItemByType(ulong userId, int itemType, int level, int corp)
    {
        var item = GetMergedEntities<ItemEntity>(
                i => i.UserId == userId && i.ItemType == itemType && i.Level == level && i.Corp == corp,
                i => i.UserId == userId && i.ItemType == itemType && i.Level == level && i.Corp == corp,
                i => i.Id)
            .FirstOrDefault();

        if (item == null)
            return null;

        var csnList = GetAssignmentsForItemId(userId, item.Id);
        return MapToDbItemData(item, csnList);
    }

    public List<DbItemData> GetItemsByType(ulong userId, int itemType)
    {
        var items = GetMergedEntities<ItemEntity>(
            i => i.UserId == userId && i.ItemType == itemType,
            i => i.UserId == userId && i.ItemType == itemType,
            i => i.Id);

        if (items.Count == 0)
            return [];

        var itemIds = items.Select(i => i.Id).ToHashSet();
        var assignmentsByItemId = GetMergedEntities<ItemCharacterAssignmentEntity>(
                a => a.UserId == userId && itemIds.Contains(a.ItemId),
                a => a.UserId == userId && itemIds.Contains(a.ItemId),
                a => a.Id)
            .GroupBy(a => a.ItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.Position).Select(a => a.CharacterCsn).ToList());

        return items
            .Select(i => MapToDbItemData(i, assignmentsByItemId.GetValueOrDefault(i.Id) ?? []))
            .ToList();
    }

    public long GenerateNextIsn(ulong userId)
    {
        var existingIsns = GetMergedEntities<ItemEntity>(
                i => i.UserId == userId,
                i => i.UserId == userId,
                i => i.Id)
            .Select(i => i.Isn);

        return ComputeNextSerial(existingIsns);
    }

    public DbItemData AddOrIncrementItem(
        ulong userId,
        int itemType,
        int count,
        int level = 0,
        int exp = 0,
        int position = 0,
        int corp = 0,
        long csn = 0,
        bool stackable = true)
    {
        return AddOrIncrementItem(
            userId,
            new DbItemData
            {
                ItemType = itemType,
                Count = count,
                Level = level,
                Exp = exp,
                Position = position,
                Corp = corp,
                Csn = csn
            },
            stackable);
    }

    public DbItemData AddOrIncrementItem(ulong userId, DbItemData item, bool stackable = true)
    {
        ArgumentNullException.ThrowIfNull(item);
        UserDbHelper.GetOrCreateGameUser(db, userId);

        if (stackable)
        {
            int itemType = item.ItemType;
            int level = item.Level;
            int corp = item.Corp;
            long csn = item.Csn;

            var existing = FindTrackedEntity<ItemEntity>(
                i => i.UserId == userId && i.ItemType == itemType && i.Level == level && i.Corp == corp && i.Csn == csn,
                i => i.UserId == userId && i.ItemType == itemType && i.Level == level && i.Corp == corp && i.Csn == csn);

            if (existing != null)
            {
                existing.Count += item.Count;
                if (item.Isn <= 0)
                    item.Isn = existing.Isn;

                var csnList = GetAssignmentsForItemId(userId, existing.Id);
                return MapToDbItemData(existing, csnList);
            }
        }

        long isn = item.Isn > 0 ? item.Isn : GenerateNextIsn(userId);
        if (item.Isn <= 0)
            item.Isn = isn;

        var entity = new ItemEntity
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            Isn = isn,
            ItemType = item.ItemType,
            Csn = item.Csn,
            Count = item.Count,
            Level = item.Level,
            Exp = item.Exp,
            Position = item.Position,
            Corp = item.Corp
        };

        db.Set<ItemEntity>().Add(entity);

        if (item.CsnList is { Count: > 0 })
        {
            SetItemAssignments(userId, isn, item.CsnList);
            return MapToDbItemData(entity, GetAssignmentsForItemId(userId, entity.Id));
        }

        return MapToDbItemData(entity, []);
    }

    public int RemoveItemBySerialNumber(ulong userId, long isn, int count, bool removeWhenEmpty = false)
    {
        if (count <= 0)
            return 0;

        var entity = FindTrackedEntity<ItemEntity>(
            i => i.UserId == userId && i.Isn == isn,
            i => i.UserId == userId && i.Isn == isn);

        if (entity == null)
            return 0;

        int actualRemoved = Math.Min(Math.Max(entity.Count, 0), count);
        entity.Count = Math.Max(0, entity.Count - count);

        if (removeWhenEmpty && entity.Count <= 0)
        {
            // Clean up any unsaved child entities referencing this item before removing it
            var assignmentSet = db.Set<ItemCharacterAssignmentEntity>();
            foreach (var localAssignment in assignmentSet.Local.Where(a => a.ItemId == entity.Id).ToList())
                assignmentSet.Remove(localAssignment);

            var awakeningSet = db.Set<EquipmentAwakeningEntity>();
            foreach (var localAwakening in awakeningSet.Local.Where(a => a.ItemId == entity.Id).ToList())
                awakeningSet.Remove(localAwakening);

            db.Set<ItemEntity>().Remove(entity);
        }

        return actualRemoved;
    }

    public bool UpdateItem(ulong userId, DbItemData item, bool syncAssignments = false)
    {
        ArgumentNullException.ThrowIfNull(item);

        long isn = item.Isn;
        var entity = FindTrackedEntity<ItemEntity>(
            i => i.UserId == userId && i.Isn == isn,
            i => i.UserId == userId && i.Isn == isn);

        if (entity == null)
            return false;

        entity.ItemType = item.ItemType;
        entity.Csn = item.Csn;
        entity.Count = item.Count;
        entity.Level = item.Level;
        entity.Exp = item.Exp;
        entity.Position = item.Position;
        entity.Corp = item.Corp;

        if (syncAssignments)
            SetItemAssignments(userId, isn, item.CsnList ?? []);

        return true;
    }

    public bool SetItemAssignments(ulong userId, long isn, IEnumerable<long> characterCsns)
    {
        ArgumentNullException.ThrowIfNull(characterCsns);

        var itemEntity = FindTrackedEntity<ItemEntity>(
            i => i.UserId == userId && i.Isn == isn,
            i => i.UserId == userId && i.Isn == isn);

        if (itemEntity == null)
            return false;

        UserDbHelper.GetOrCreateGameUser(db, userId);

        var validCsns = GetMergedEntities<CharacterEntity>(
                c => c.UserId == userId,
                c => c.UserId == userId,
                c => c.Id)
            .Select(c => c.Csn)
            .ToHashSet();

        var desiredCsns = new List<long>();
        var seenCsns = new HashSet<long>();
        foreach (long csn in characterCsns)
        {
            if (csn > 0 && validCsns.Contains(csn) && seenCsns.Add(csn))
                desiredCsns.Add(csn);
        }

        var assignmentSet = db.Set<ItemCharacterAssignmentEntity>();
        var existingAssignments = assignmentSet
            .Where(a => a.UserId == userId && a.ItemId == itemEntity.Id)
            .ToList();

        foreach (var local in assignmentSet.Local)
        {
            if (local.UserId == userId && local.ItemId == itemEntity.Id &&
                !existingAssignments.Any(e => e.Id == local.Id))
            {
                existingAssignments.Add(local);
            }
        }

        existingAssignments = existingAssignments
            .Where(a => db.Entry(a).State != EntityState.Deleted)
            .ToList();

        var currentOrdered = existingAssignments
            .OrderBy(a => a.Position)
            .Select(a => a.CharacterCsn)
            .ToList();

        if (currentOrdered.SequenceEqual(desiredCsns))
            return true;

        if (existingAssignments.Count > 0)
            assignmentSet.RemoveRange(existingAssignments);

        for (int pos = 0; pos < desiredCsns.Count; pos++)
        {
            assignmentSet.Add(new ItemCharacterAssignmentEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                ItemId = itemEntity.Id,
                CharacterCsn = desiredCsns[pos],
                Position = pos
            });
        }

        return true;
    }

    // --- Equipment Awakenings ---

    public List<EquipmentAwakeningData> GetAwakenings(ulong userId)
    {
        bool hasTrackerChanges = db.ChangeTracker.Entries<EquipmentAwakeningEntity>().Any()
            || db.ChangeTracker.Entries<ItemEntity>().Any(e => e.Entity.UserId == userId);

        if (!hasTrackerChanges)
        {
            return db.Set<EquipmentAwakeningEntity>()
                .AsNoTracking()
                .Where(a => a.Item.UserId == userId)
                .Select(a => new EquipmentAwakeningData
                {
                    Isn = a.Item.Isn,
                    IsNewData = a.IsNewData,
                    Option = new NetEquipmentAwakeningOption
                    {
                        Option1Id = a.Option1Id,
                        Option1Lock = a.Option1Lock,
                        IsOption1DisposableLock = a.IsOption1DisposableLock,
                        Option2Id = a.Option2Id,
                        Option2Lock = a.Option2Lock,
                        IsOption2DisposableLock = a.IsOption2DisposableLock,
                        Option3Id = a.Option3Id,
                        Option3Lock = a.Option3Lock,
                        IsOption3DisposableLock = a.IsOption3DisposableLock
                    }
                })
                .ToList();
        }

        var itemIdToIsn = GetMergedEntities<ItemEntity>(
                i => i.UserId == userId,
                i => i.UserId == userId,
                i => i.Id)
            .ToDictionary(i => i.Id, i => i.Isn);

        if (itemIdToIsn.Count == 0)
            return [];

        var itemIds = itemIdToIsn.Keys.ToHashSet();
        var awakenings = GetMergedEntities<EquipmentAwakeningEntity>(
            a => itemIds.Contains(a.ItemId),
            a => a.Item.UserId == userId,
            a => a.Id);

        return awakenings
            .Where(a => itemIdToIsn.ContainsKey(a.ItemId))
            .Select(a => MapToAwakeningData(a, itemIdToIsn[a.ItemId]))
            .ToList();
    }

    public EquipmentAwakeningData? GetAwakeningByIsn(ulong userId, long isn, bool? isNewData = null)
    {
        return GetAwakeningsByIsn(userId, isn)
            .Where(a => !isNewData.HasValue || a.IsNewData == isNewData.Value)
            .OrderBy(a => a.IsNewData)
            .FirstOrDefault();
    }

    public List<EquipmentAwakeningData> GetAwakeningsByIsn(ulong userId, long isn)
    {
        var itemEntity = GetMergedEntities<ItemEntity>(
                i => i.UserId == userId && i.Isn == isn,
                i => i.UserId == userId && i.Isn == isn,
                i => i.Id)
            .FirstOrDefault();

        if (itemEntity == null)
            return [];

        string itemId = itemEntity.Id;
        return GetMergedEntities<EquipmentAwakeningEntity>(
                a => a.ItemId == itemId,
                a => a.ItemId == itemId,
                a => a.Id)
            .OrderBy(a => a.IsNewData)
            .Select(a => MapToAwakeningData(a, isn))
            .ToList();
    }

    public bool SaveAwakening(ulong userId, EquipmentAwakeningData awakening)
    {
        ArgumentNullException.ThrowIfNull(awakening);

        var itemEntity = FindTrackedEntity<ItemEntity>(
            i => i.UserId == userId && i.Isn == awakening.Isn,
            i => i.UserId == userId && i.Isn == awakening.Isn);

        if (itemEntity == null)
            return false;

        UserDbHelper.GetOrCreateGameUser(db, userId);

        string itemId = itemEntity.Id;
        bool isNewData = awakening.IsNewData;
        var opt = awakening.Option ?? new NetEquipmentAwakeningOption();

        var existing = FindTrackedEntity<EquipmentAwakeningEntity>(
            a => a.ItemId == itemId && a.IsNewData == isNewData,
            a => a.ItemId == itemId && a.IsNewData == isNewData);

        if (existing == null)
        {
            db.Set<EquipmentAwakeningEntity>().Add(new EquipmentAwakeningEntity
            {
                Id = UlidGenerator.NewUlid(),
                ItemId = itemId,
                IsNewData = isNewData,
                Option1Id = opt.Option1Id,
                Option1Lock = opt.Option1Lock,
                IsOption1DisposableLock = opt.IsOption1DisposableLock,
                Option2Id = opt.Option2Id,
                Option2Lock = opt.Option2Lock,
                IsOption2DisposableLock = opt.IsOption2DisposableLock,
                Option3Id = opt.Option3Id,
                Option3Lock = opt.Option3Lock,
                IsOption3DisposableLock = opt.IsOption3DisposableLock
            });
        }
        else
        {
            existing.Option1Id = opt.Option1Id;
            existing.Option1Lock = opt.Option1Lock;
            existing.IsOption1DisposableLock = opt.IsOption1DisposableLock;
            existing.Option2Id = opt.Option2Id;
            existing.Option2Lock = opt.Option2Lock;
            existing.IsOption2DisposableLock = opt.IsOption2DisposableLock;
            existing.Option3Id = opt.Option3Id;
            existing.Option3Lock = opt.Option3Lock;
            existing.IsOption3DisposableLock = opt.IsOption3DisposableLock;
        }

        return true;
    }

    public int DeleteAwakening(ulong userId, long isn, bool? isNewData = null)
    {
        var itemEntity = FindTrackedEntity<ItemEntity>(
            i => i.UserId == userId && i.Isn == isn,
            i => i.UserId == userId && i.Isn == isn);

        if (itemEntity == null)
            return 0;

        string itemId = itemEntity.Id;
        var set = db.Set<EquipmentAwakeningEntity>();
        var matches = set
            .Where(a => a.ItemId == itemId && (!isNewData.HasValue || a.IsNewData == isNewData.Value))
            .ToList();

        foreach (var local in set.Local)
        {
            if (local.ItemId == itemId &&
                (!isNewData.HasValue || local.IsNewData == isNewData.Value) &&
                !matches.Any(m => m.Id == local.Id))
            {
                matches.Add(local);
            }
        }

        matches = matches
            .Where(a => db.Entry(a).State != EntityState.Deleted)
            .ToList();

        if (matches.Count > 0)
            set.RemoveRange(matches);

        return matches.Count;
    }

    // --- Helpers ---

    private static void SyncCollection<TEntity, TKey, TSource>(
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

    private List<long> GetAssignmentsForItemId(ulong userId, string itemId)
    {
        return GetMergedEntities<ItemCharacterAssignmentEntity>(
                a => a.UserId == userId && a.ItemId == itemId,
                a => a.UserId == userId && a.ItemId == itemId,
                a => a.Id)
            .OrderBy(a => a.Position)
            .Select(a => a.CharacterCsn)
            .ToList();
    }

    private List<TEntity> GetMergedEntities<TEntity>(
        Func<TEntity, bool> localPredicate,
        Expression<Func<TEntity, bool>> dbPredicate,
        Func<TEntity, string> keySelector) where TEntity : class
    {
        var tracked = db.ChangeTracker.Entries<TEntity>()
            .Where(e => localPredicate(e.Entity))
            .ToList();

        if (tracked.Count == 0)
        {
            return db.Set<TEntity>()
                .AsNoTracking()
                .Where(dbPredicate)
                .ToList();
        }

        var deletedKeys = new HashSet<string>();
        var trackedByKey = new Dictionary<string, TEntity>();
        var added = new List<TEntity>();

        foreach (var entry in tracked)
        {
            if (entry.State == EntityState.Deleted)
                deletedKeys.Add(keySelector(entry.Entity));
            else if (entry.State == EntityState.Added)
                added.Add(entry.Entity);
            else if (entry.State != EntityState.Detached)
                trackedByKey[keySelector(entry.Entity)] = entry.Entity;
        }

        var dbRows = db.Set<TEntity>()
            .AsNoTracking()
            .Where(dbPredicate)
            .ToList();

        var result = new List<TEntity>(dbRows.Count + added.Count);
        foreach (var row in dbRows)
        {
            string key = keySelector(row);
            if (deletedKeys.Contains(key))
                continue;

            result.Add(trackedByKey.TryGetValue(key, out var modified) ? modified : row);
        }

        result.AddRange(added);
        return result;
    }

    private TEntity? FindTrackedEntity<TEntity>(
        Func<TEntity, bool> localPredicate,
        Expression<Func<TEntity, bool>> dbPredicate) where TEntity : class
    {
        var set = db.Set<TEntity>();
        var local = set.Local.FirstOrDefault(localPredicate);
        if (local != null)
            return local;

        bool deletedInTracker = db.ChangeTracker.Entries<TEntity>()
            .Any(e => e.State == EntityState.Deleted && localPredicate(e.Entity));
        if (deletedInTracker)
            return null;

        return set.FirstOrDefault(dbPredicate);
    }

    private static HashSet<int> ResolveNameCodeTids(int tid)
    {
        if (GameData.Instance?.CharacterTable != null &&
            GameData.Instance.CharacterTable.TryGetValue(tid, out var inputRecord))
        {
            int targetNameCode = inputRecord.NameCode;
            return GameData.Instance.CharacterTable
                .Where(kvp => kvp.Value.NameCode == targetNameCode)
                .Select(kvp => kvp.Key)
                .ToHashSet();
        }

        return [tid];
    }

    private static long ComputeNextSerial(IEnumerable<long> existingValues)
    {
        var existing = existingValues.ToHashSet();
        long max = 0L;
        foreach (long val in existing)
        {
            if (val > max && val <= int.MaxValue)
                max = val;
        }

        long next = max < int.MaxValue ? max + 1 : 1L;
        while (existing.Contains(next))
            next++;

        return next;
    }

    private static CharacterModel MapToCharacterModel(CharacterEntity c) => new()
    {
        Csn = (int)c.Csn,
        Tid = c.Tid,
        CostumeId = c.CostumeId,
        Level = c.Level,
        UltimateLevel = c.UltimateLevel,
        Skill1Lvl = c.Skill1Lvl,
        Skill2Lvl = c.Skill2Lvl,
        Grade = c.Grade,
        IsMainForce = c.IsMainForce
    };

    private static DbItemData MapToDbItemData(ItemEntity i, List<long> csnList) => new()
    {
        Isn = i.Isn,
        ItemType = i.ItemType,
        Csn = i.Csn,
        Count = i.Count,
        Level = i.Level,
        Exp = i.Exp,
        Position = i.Position,
        Corp = i.Corp,
        CsnList = csnList
    };

    private static EquipmentAwakeningData MapToAwakeningData(EquipmentAwakeningEntity a, long isn) => new()
    {
        Isn = isn,
        IsNewData = a.IsNewData,
        Option = new NetEquipmentAwakeningOption
        {
            Option1Id = a.Option1Id,
            Option1Lock = a.Option1Lock,
            IsOption1DisposableLock = a.IsOption1DisposableLock,
            Option2Id = a.Option2Id,
            Option2Lock = a.Option2Lock,
            IsOption2DisposableLock = a.IsOption2DisposableLock,
            Option3Id = a.Option3Id,
            Option3Lock = a.Option3Lock,
            IsOption3DisposableLock = a.IsOption3DisposableLock
        }
    };
}
