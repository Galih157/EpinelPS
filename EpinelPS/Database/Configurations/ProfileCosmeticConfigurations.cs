using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserProfileCardDecorationConfiguration : IEntityTypeConfiguration<UserProfileCardDecoration>
{
    public void Configure(EntityTypeBuilder<UserProfileCardDecoration> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserProfileCardStickerConfiguration : IEntityTypeConfiguration<UserProfileCardSticker>
{
    public void Configure(EntityTypeBuilder<UserProfileCardSticker> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.Position }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserUnlockedCosmeticConfiguration : IEntityTypeConfiguration<UserUnlockedCosmetic>
{
    public void Configure(EntityTypeBuilder<UserUnlockedCosmetic> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.Category, e.CosmeticId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserJukeboxSettingConfiguration : IEntityTypeConfiguration<UserJukeboxSetting>
{
    public void Configure(EntityTypeBuilder<UserJukeboxSetting> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.Location }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserClaimedJukeboxRewardConfiguration : IEntityTypeConfiguration<UserClaimedJukeboxReward>
{
    public void Configure(EntityTypeBuilder<UserClaimedJukeboxReward> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.TriggerId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserWallpaperSlotConfiguration : IEntityTypeConfiguration<UserWallpaperSlot>
{
    public void Configure(EntityTypeBuilder<UserWallpaperSlot> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.OrderIndex }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserWallpaperBackgroundConfiguration : IEntityTypeConfiguration<UserWallpaperBackground>
{
    public void Configure(EntityTypeBuilder<UserWallpaperBackground> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.OrderIndex }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserWallpaperJukeboxConfiguration : IEntityTypeConfiguration<UserWallpaperJukebox>
{
    public void Configure(EntityTypeBuilder<UserWallpaperJukebox> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.OrderIndex }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserWallpaperFavoriteConfiguration : IEntityTypeConfiguration<UserWallpaperFavorite>
{
    public void Configure(EntityTypeBuilder<UserWallpaperFavorite> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.OrderIndex }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserWallpaperPlaylistConfiguration : IEntityTypeConfiguration<UserWallpaperPlaylist>
{
    public void Configure(EntityTypeBuilder<UserWallpaperPlaylist> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.OrderIndex }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
