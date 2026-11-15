using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserBondConfiguration : IEntityTypeConfiguration<UserBond>
{
    public void Configure(EntityTypeBuilder<UserBond> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.NameCode }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserBondProgressItemConfiguration : IEntityTypeConfiguration<UserBondProgressItem>
{
    public void Configure(EntityTypeBuilder<UserBondProgressItem> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.BondId, e.Kind, e.ItemId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Bond).WithMany(b => b.ProgressItems).HasForeignKey(e => e.BondId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserFavoriteItemConfiguration : IEntityTypeConfiguration<UserFavoriteItem>
{
    public void Configure(EntityTypeBuilder<UserFavoriteItem> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.FavoriteItemId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserFavoriteItemQuestConfiguration : IEntityTypeConfiguration<UserFavoriteItemQuest>
{
    public void Configure(EntityTypeBuilder<UserFavoriteItemQuest> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.QuestId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserLiberationConfiguration : IEntityTypeConfiguration<UserLiberation>
{
    public void Configure(EntityTypeBuilder<UserLiberation> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.CharacterId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserLiberationMissionConfiguration : IEntityTypeConfiguration<UserLiberationMission>
{
    public void Configure(EntityTypeBuilder<UserLiberationMission> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.LiberationId, e.MissionId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Liberation).WithMany(l => l.Missions).HasForeignKey(e => e.LiberationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserOpenLiberateTypeConfiguration : IEntityTypeConfiguration<UserOpenLiberateType>
{
    public void Configure(EntityTypeBuilder<UserOpenLiberateType> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.LiberateTypeId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
