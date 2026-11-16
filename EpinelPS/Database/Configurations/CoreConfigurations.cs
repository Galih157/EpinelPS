using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class SdkUserConfiguration : IEntityTypeConfiguration<SdkUser>
{
    public void Configure(EntityTypeBuilder<SdkUser> entity)
    {
        entity.HasIndex(u => u.Email);
    }
}

public class UserBanConfiguration : IEntityTypeConfiguration<UserBan>
{
    public void Configure(EntityTypeBuilder<UserBan> entity)
    {
        entity.ToTable("UserBans");
        entity.HasIndex(b => b.UserId);
        entity.HasOne(b => b.User)
            .WithMany(u => u.Bans)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EventBannerConfiguration : IEntityTypeConfiguration<EventBanner>
{
    public void Configure(EntityTypeBuilder<EventBanner> entity)
    {
        entity.ToTable("EventBanners");
    }
}

public class ServerSecretsConfiguration : IEntityTypeConfiguration<ServerSecrets>
{
    public void Configure(EntityTypeBuilder<ServerSecrets> entity)
    {
        entity.ToTable("ServerSecrets");
    }
}

public class ServerSettingsConfiguration : IEntityTypeConfiguration<ServerSettings>
{
    public void Configure(EntityTypeBuilder<ServerSettings> entity)
    {
        entity.ToTable("ServerSettings");
    }
}
