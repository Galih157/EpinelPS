using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class OutpostEntityConfiguration : IEntityTypeConfiguration<OutpostEntity>
{
    public void Configure(EntityTypeBuilder<OutpostEntity> entity)
    {
        entity.HasKey(e => e.UserId);
        entity.HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<OutpostEntity>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DispatchClearItemEntityConfiguration : IEntityTypeConfiguration<DispatchClearItemEntity>
{
    public void Configure(EntityTypeBuilder<DispatchClearItemEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Outpost)
            .WithMany(o => o.DispatchClearItems)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.DispatchId }).IsUnique();
    }
}

public class SelectableDispatchEntityConfiguration : IEntityTypeConfiguration<SelectableDispatchEntity>
{
    public void Configure(EntityTypeBuilder<SelectableDispatchEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Outpost)
            .WithMany(o => o.SelectableDispatches)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.SelectTid });
    }
}

public class DispatchEntityConfiguration : IEntityTypeConfiguration<DispatchEntity>
{
    public void Configure(EntityTypeBuilder<DispatchEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Outpost)
            .WithMany(o => o.Dispatches)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.Tid }).IsUnique();
    }
}

public class InfraCoreRewardEntityConfiguration : IEntityTypeConfiguration<InfraCoreRewardEntity>
{
    public void Configure(EntityTypeBuilder<InfraCoreRewardEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Outpost)
            .WithMany(o => o.InfraCoreRewards)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.InfraCoreLevel }).IsUnique();
    }
}

public class SynchroSlotEntityConfiguration : IEntityTypeConfiguration<SynchroSlotEntity>
{
    public void Configure(EntityTypeBuilder<SynchroSlotEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Outpost)
            .WithMany(o => o.SynchroSlots)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.Slot }).IsUnique();
    }
}

public class ResearchProgressEntityConfiguration : IEntityTypeConfiguration<ResearchProgressEntity>
{
    public void Configure(EntityTypeBuilder<ResearchProgressEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Outpost)
            .WithMany(o => o.ResearchProgress)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.ResearchId }).IsUnique();
    }
}

public class OutpostBuildingEntityConfiguration : IEntityTypeConfiguration<OutpostBuildingEntity>
{
    public void Configure(EntityTypeBuilder<OutpostBuildingEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Outpost)
            .WithMany(o => o.OutpostBuildings)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.SlotId }).IsUnique();
    }
}
