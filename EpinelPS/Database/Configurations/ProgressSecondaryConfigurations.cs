using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserMapStateEntityConfiguration : IEntityTypeConfiguration<UserMapStateEntity>
{
    public void Configure(EntityTypeBuilder<UserMapStateEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.MapId }).IsUnique();
    }
}

public class UserFieldMapEntityConfiguration : IEntityTypeConfiguration<UserFieldMapEntity>
{
    public void Configure(EntityTypeBuilder<UserFieldMapEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.MapId }).IsUnique();
    }
}

public class UserSubQuestEntityConfiguration : IEntityTypeConfiguration<UserSubQuestEntity>
{
    public void Configure(EntityTypeBuilder<UserSubQuestEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.SubQuestId }).IsUnique();
    }
}

public class UserStageClearHistoryEntityConfiguration : IEntityTypeConfiguration<UserStageClearHistoryEntity>
{
    public void Configure(EntityTypeBuilder<UserStageClearHistoryEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.ClearedAt });
    }
}

public class UserClearedOutpostScenarioEntityConfiguration : IEntityTypeConfiguration<UserClearedOutpostScenarioEntity>
{
    public void Configure(EntityTypeBuilder<UserClearedOutpostScenarioEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.ScenarioId }).IsUnique();
    }
}

public class UserTacticAcademyLessonEntityConfiguration : IEntityTypeConfiguration<UserTacticAcademyLessonEntity>
{
    public void Configure(EntityTypeBuilder<UserTacticAcademyLessonEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.LessonId }).IsUnique();
    }
}
