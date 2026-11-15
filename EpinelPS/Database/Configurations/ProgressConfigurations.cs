using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserCompletedScenarioEntityConfiguration : IEntityTypeConfiguration<UserCompletedScenarioEntity>
{
    public void Configure(EntityTypeBuilder<UserCompletedScenarioEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.ScenarioId }).IsUnique();
    }
}

public class UserCurrencyEntityConfiguration : IEntityTypeConfiguration<UserCurrencyEntity>
{
    public void Configure(EntityTypeBuilder<UserCurrencyEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.CurrencyType }).IsUnique();
    }
}

public class UserMainQuestEntityConfiguration : IEntityTypeConfiguration<UserMainQuestEntity>
{
    public void Configure(EntityTypeBuilder<UserMainQuestEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.QuestTid }).IsUnique();
    }
}

public class UserTowerProgressEntityConfiguration : IEntityTypeConfiguration<UserTowerProgressEntity>
{
    public void Configure(EntityTypeBuilder<UserTowerProgressEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.CorporationType }).IsUnique();
    }
}

public class UserClearedTutorialEntityConfiguration : IEntityTypeConfiguration<UserClearedTutorialEntity>
{
    public void Configure(EntityTypeBuilder<UserClearedTutorialEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.TutorialGroupId }).IsUnique();
    }
}

public class UserContentsUnlockEntityConfiguration : IEntityTypeConfiguration<UserContentsUnlockEntity>
{
    public void Configure(EntityTypeBuilder<UserContentsUnlockEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.ContentsOpenId }).IsUnique();
    }
}

public class UserViewedNoticePopupEntityConfiguration : IEntityTypeConfiguration<UserViewedNoticePopupEntity>
{
    public void Configure(EntityTypeBuilder<UserViewedNoticePopupEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.CategoryId, e.NoticePopupTableId }).IsUnique();
    }
}
