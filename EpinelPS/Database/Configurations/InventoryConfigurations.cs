using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class ItemEntityConfiguration : IEntityTypeConfiguration<ItemEntity>
{
    public void Configure(EntityTypeBuilder<ItemEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.Isn }).IsUnique();
        entity.HasIndex(e => new { e.UserId, e.ItemType });
    }
}

public class CharacterEntityConfiguration : IEntityTypeConfiguration<CharacterEntity>
{
    public void Configure(EntityTypeBuilder<CharacterEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.Csn }).IsUnique();
    }
}

public class ItemCharacterAssignmentEntityConfiguration : IEntityTypeConfiguration<ItemCharacterAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<ItemCharacterAssignmentEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Item)
            .WithMany(i => i.CharacterAssignments)
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Character)
            .WithMany(c => c.ItemAssignments)
            .HasForeignKey(e => new { e.UserId, e.CharacterCsn })
            .HasPrincipalKey(c => new { c.UserId, c.Csn })
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.ItemId, e.Position }).IsUnique();
        entity.HasIndex(e => new { e.ItemId, e.CharacterCsn }).IsUnique();
    }
}

public class EquipmentAwakeningEntityConfiguration : IEntityTypeConfiguration<EquipmentAwakeningEntity>
{
    public void Configure(EntityTypeBuilder<EquipmentAwakeningEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Item)
            .WithMany(i => i.EquipmentAwakenings)
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => e.ItemId);
    }
}
