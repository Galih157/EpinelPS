using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserResetStateEntityConfiguration : IEntityTypeConfiguration<UserResetStateEntity>
{
    public void Configure(EntityTypeBuilder<UserResetStateEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithOne()
            .HasForeignKey<UserResetStateEntity>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => e.UserId).IsUnique();
    }
}

public class UserCompletedDailyMissionEntityConfiguration : IEntityTypeConfiguration<UserCompletedDailyMissionEntity>
{
    public void Configure(EntityTypeBuilder<UserCompletedDailyMissionEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.MissionId }).IsUnique();
    }
}

public class UserCompletedWeeklyMissionEntityConfiguration : IEntityTypeConfiguration<UserCompletedWeeklyMissionEntity>
{
    public void Configure(EntityTypeBuilder<UserCompletedWeeklyMissionEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.MissionId }).IsUnique();
    }
}

public class UserDailyTowerCountEntityConfiguration : IEntityTypeConfiguration<UserDailyTowerCountEntity>
{
    public void Configure(EntityTypeBuilder<UserDailyTowerCountEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.CorporationType }).IsUnique();
    }
}

public class UserDailyCounselCountEntityConfiguration : IEntityTypeConfiguration<UserDailyCounselCountEntity>
{
    public void Configure(EntityTypeBuilder<UserDailyCounselCountEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.KeyId }).IsUnique();
    }
}
