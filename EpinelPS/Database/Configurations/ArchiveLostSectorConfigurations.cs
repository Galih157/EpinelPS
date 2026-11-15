using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserCompletedAchievementConfiguration : IEntityTypeConfiguration<UserCompletedAchievement>
{
    public void Configure(EntityTypeBuilder<UserCompletedAchievement> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.AchievementId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserSideStoryStageConfiguration : IEntityTypeConfiguration<UserSideStoryStage>
{
    public void Configure(EntityTypeBuilder<UserSideStoryStage> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.StageId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserArchiveUnlockConfiguration : IEntityTypeConfiguration<UserArchiveUnlock>
{
    public void Configure(EntityTypeBuilder<UserArchiveUnlock> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.Kind, e.TargetId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserLostSectorConfiguration : IEntityTypeConfiguration<UserLostSector>
{
    public void Configure(EntityTypeBuilder<UserLostSector> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.SectorId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserLostSectorClearedStageConfiguration : IEntityTypeConfiguration<UserLostSectorClearedStage>
{
    public void Configure(EntityTypeBuilder<UserLostSectorClearedStage> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.LostSectorId, e.StageKey }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.LostSector).WithMany(ls => ls.ClearedStages).HasForeignKey(e => e.LostSectorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserLostSectorObjectConfiguration : IEntityTypeConfiguration<UserLostSectorObject>
{
    public void Configure(EntityTypeBuilder<UserLostSectorObject> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.LostSectorId, e.ObjectKey }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.LostSector).WithMany(ls => ls.Objects).HasForeignKey(e => e.LostSectorId).OnDelete(DeleteBehavior.Cascade);
    }
}
