using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserTeamGroupEntityConfiguration : IEntityTypeConfiguration<UserTeamGroupEntity>
{
    public void Configure(EntityTypeBuilder<UserTeamGroupEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.TeamType }).IsUnique();
    }
}

public class UserTeamSlotEntityConfiguration : IEntityTypeConfiguration<UserTeamSlotEntity>
{
    public void Configure(EntityTypeBuilder<UserTeamSlotEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.TeamGroup)
            .WithMany(g => g.Slots)
            .HasForeignKey(e => e.TeamGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.TeamGroupId, e.TeamNumber, e.Slot }).IsUnique();
    }
}

public class UserRepresentationTeamSlotEntityConfiguration : IEntityTypeConfiguration<UserRepresentationTeamSlotEntity>
{
    public void Configure(EntityTypeBuilder<UserRepresentationTeamSlotEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.Slot }).IsUnique();
    }
}
