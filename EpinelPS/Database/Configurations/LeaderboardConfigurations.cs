using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class TtsSongScoreEntityConfiguration : IEntityTypeConfiguration<TtsSongScoreEntity>
{
    public void Configure(EntityTypeBuilder<TtsSongScoreEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.SongId, e.RankType, e.Difficulty }).IsUnique();
        entity.HasIndex(e => new { e.SongId, e.RankType, e.Difficulty, e.Score });
    }
}

public class TtsTotalScoreEntityConfiguration : IEntityTypeConfiguration<TtsTotalScoreEntity>
{
    public void Configure(EntityTypeBuilder<TtsTotalScoreEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.RankType }).IsUnique();
        entity.HasIndex(e => new { e.RankType, e.Score });
    }
}

public class ArcadeScoreEntityConfiguration : IEntityTypeConfiguration<ArcadeScoreEntity>
{
    public void Configure(EntityTypeBuilder<ArcadeScoreEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.HasIndex(e => new { e.ArcadeId, e.ModeId, e.Score });
        entity.HasIndex(e => e.UserId);
    }
}

public class MiniGameLeaderboardEntityConfiguration : IEntityTypeConfiguration<MiniGameLeaderboardEntity>
{
    public void Configure(EntityTypeBuilder<MiniGameLeaderboardEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.MinigameType, e.ArcadeId, e.ModeId, e.Score });
        entity.HasIndex(e => new { e.MinigameType, e.GuildId, e.ArcadeId, e.Score });
    }
}
