using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserMinigameEntityConfiguration : IEntityTypeConfiguration<UserMinigameEntity>
{
    public void Configure(EntityTypeBuilder<UserMinigameEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.MinigameType, e.ArcadeId }).IsUnique();
    }
}
