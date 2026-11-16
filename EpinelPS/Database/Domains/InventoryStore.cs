using EpinelPS.Data;
using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database.Domains;

public sealed class InventoryStore : IUserDomainStore
{
    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        user.Characters = db.Set<CharacterEntity>()
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new CharacterModel
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
            })
            .ToList();

        var itemEntities = db.Set<ItemEntity>()
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Include(i => i.CharacterAssignments)
            .ToList();

        user.Items = itemEntities
            .Select(i => new DbItemData
            {
                Isn = i.Isn,
                ItemType = i.ItemType,
                Csn = i.Csn,
                Count = i.Count,
                Level = i.Level,
                Exp = i.Exp,
                Position = i.Position,
                Corp = i.Corp,
                CsnList = i.CharacterAssignments
                    .OrderBy(a => a.Position)
                    .Select(a => a.CharacterCsn)
                    .ToList()
            })
            .ToList();

        user.EquipmentAwakenings = db.Set<EquipmentAwakeningEntity>()
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

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        // Characters first so ItemCharacterAssignments FK is satisfied
        var characters = user.Characters ?? [];
        var validCharacterCsns = new HashSet<long>();
        DomainStoreHelpers.SyncCollection(
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
        DomainStoreHelpers.SyncCollection(
            db.Set<ItemEntity>(),
            db.Set<ItemEntity>().Where(i => i.UserId == userId),
            items,
            i => i.Isn,
            i => i.Isn,
            i =>
            {
                var entity = new ItemEntity
                {
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

        // EquipmentAwakenings (skipping orphan awakenings if item doesn't exist)
        var existingAwakenings = db.Set<EquipmentAwakeningEntity>()
            .Where(a => a.Item.UserId == userId)
            .ToList();

        var desiredAwakenings = new List<EquipmentAwakeningEntity>();
        foreach (var aw in user.EquipmentAwakenings ?? [])
        {
            if (!itemsByIsn.TryGetValue(aw.Isn, out var itemEntity))
                continue;

            var opt = aw.Option ?? new NetEquipmentAwakeningOption();
            desiredAwakenings.Add(new EquipmentAwakeningEntity
            {
                ItemId = itemEntity.Id,
                IsNewData = aw.IsNewData,
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

        db.Set<EquipmentAwakeningEntity>().RemoveRange(existingAwakenings);
        db.Set<EquipmentAwakeningEntity>().AddRange(desiredAwakenings);
    }
}
