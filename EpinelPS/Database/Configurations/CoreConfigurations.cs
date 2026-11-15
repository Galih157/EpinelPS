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
        entity.HasIndex(b => b.UserId);
        entity.HasOne(b => b.User)
            .WithMany(u => u.Bans)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
