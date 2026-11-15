using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class UserGachaBannerPullConfiguration : IEntityTypeConfiguration<UserGachaBannerPull>
{
    public void Configure(EntityTypeBuilder<UserGachaBannerPull> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.BannerId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserGachaPityCountConfiguration : IEntityTypeConfiguration<UserGachaPityCount>
{
    public void Configure(EntityTypeBuilder<UserGachaPityCount> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.BannerId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserGachaSelectupChoiceConfiguration : IEntityTypeConfiguration<UserGachaSelectupChoice>
{
    public void Configure(EntityTypeBuilder<UserGachaSelectupChoice> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.BannerId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserGachaDailyFreePullConfiguration : IEntityTypeConfiguration<UserGachaDailyFreePull>
{
    public void Configure(EntityTypeBuilder<UserGachaDailyFreePull> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.GachaId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserGachaPaybackConfiguration : IEntityTypeConfiguration<UserGachaPayback>
{
    public void Configure(EntityTypeBuilder<UserGachaPayback> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.GachaId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserGachaPaybackRewardConfiguration : IEntityTypeConfiguration<UserGachaPaybackReward>
{
    public void Configure(EntityTypeBuilder<UserGachaPaybackReward> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.PaybackId, e.StepId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Payback).WithMany(p => p.Rewards).HasForeignKey(e => e.PaybackId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserNormalShopStateConfiguration : IEntityTypeConfiguration<UserNormalShopState>
{
    public void Configure(EntityTypeBuilder<UserNormalShopState> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.ShopCategory }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserNormalShopOrderConfiguration : IEntityTypeConfiguration<UserNormalShopOrder>
{
    public void Configure(EntityTypeBuilder<UserNormalShopOrder> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.ShopStateId, e.OrderIndex }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.ShopState).WithMany(s => s.Orders).HasForeignKey(e => e.ShopStateId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserNormalShopPinnedBuyConfiguration : IEntityTypeConfiguration<UserNormalShopPinnedBuy>
{
    public void Configure(EntityTypeBuilder<UserNormalShopPinnedBuy> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.ShopStateId, e.ProductId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.ShopState).WithMany(s => s.PinnedBuys).HasForeignKey(e => e.ShopStateId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserPassConfiguration : IEntityTypeConfiguration<UserPass>
{
    public void Configure(EntityTypeBuilder<UserPass> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserId, e.PassId }).IsUnique();
        entity.Property(e => e.LastCompleteAt).HasDefaultValue("");
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserPassRankConfiguration : IEntityTypeConfiguration<UserPassRank>
{
    public void Configure(EntityTypeBuilder<UserPassRank> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserPassId, e.PassRank }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.UserPass).WithMany(p => p.Ranks).HasForeignKey(e => e.UserPassId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserPassMissionConfiguration : IEntityTypeConfiguration<UserPassMission>
{
    public void Configure(EntityTypeBuilder<UserPassMission> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => new { e.UserPassId, e.PassMissionId }).IsUnique();
        entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.UserPass).WithMany(p => p.Missions).HasForeignKey(e => e.UserPassId).OnDelete(DeleteBehavior.Cascade);
    }
}
