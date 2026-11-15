using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserEventConfiguration : IEntityTypeConfiguration<UserEvent>
{
    public void Configure(EntityTypeBuilder<UserEvent> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.EventId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserEventCompletedScenarioConfiguration : IEntityTypeConfiguration<UserEventCompletedScenario>
{
    public void Configure(EntityTypeBuilder<UserEventCompletedScenario> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserEventId, e.ScenarioId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.UserEvent).WithMany(ev => ev.CompletedScenarios).HasForeignKey(e => e.UserEventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserEventClearedStageConfiguration : IEntityTypeConfiguration<UserEventClearedStage>
{
    public void Configure(EntityTypeBuilder<UserEventClearedStage> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserEventId, e.StageId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.UserEvent).WithMany(ev => ev.ClearedStages).HasForeignKey(e => e.UserEventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserLoginEventConfiguration : IEntityTypeConfiguration<UserLoginEvent>
{
    public void Configure(EntityTypeBuilder<UserLoginEvent> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.EventId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserLoginEventDayConfiguration : IEntityTypeConfiguration<UserLoginEventDay>
{
    public void Configure(EntityTypeBuilder<UserLoginEventDay> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserLoginEventId, e.Day }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.UserLoginEvent).WithMany(ev => ev.Days).HasForeignKey(e => e.UserLoginEventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserEventMissionConfiguration : IEntityTypeConfiguration<UserEventMission>
{
    public void Configure(EntityTypeBuilder<UserEventMission> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.EventId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserEventMissionEntryConfiguration : IEntityTypeConfiguration<UserEventMissionEntry>
{
    public void Configure(EntityTypeBuilder<UserEventMissionEntry> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserEventMissionId, e.IsDaily, e.MissionId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.UserEventMission).WithMany(m => m.Entries).HasForeignKey(e => e.UserEventMissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserEventShopBuyConfiguration : IEntityTypeConfiguration<UserEventShopBuy>
{
    public void Configure(EntityTypeBuilder<UserEventShopBuy> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.EventId, e.ProductTid }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
