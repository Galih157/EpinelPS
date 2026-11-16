using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserLobbyPrivateBannerEntityConfiguration : IEntityTypeConfiguration<UserLobbyPrivateBannerEntity>
{
    public void Configure(EntityTypeBuilder<UserLobbyPrivateBannerEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.BannerId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserJukeboxPlaylistEntityConfiguration : IEntityTypeConfiguration<UserJukeboxPlaylistEntity>
{
    public void Configure(EntityTypeBuilder<UserJukeboxPlaylistEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.IsFavorite, e.PlaylistUid }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserCharacterWishlistEntityConfiguration : IEntityTypeConfiguration<UserCharacterWishlistEntity>
{
    public void Configure(EntityTypeBuilder<UserCharacterWishlistEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.BannerId, e.CharacterId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserMailEntityConfiguration : IEntityTypeConfiguration<UserMailEntity>
{
    public void Configure(EntityTypeBuilder<UserMailEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.Msn }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserMessengerRepairHistoryEntityConfiguration : IEntityTypeConfiguration<UserMessengerRepairHistoryEntity>
{
    public void Configure(EntityTypeBuilder<UserMessengerRepairHistoryEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.AuditId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
