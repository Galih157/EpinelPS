using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EpinelPS.Database.Configurations;

public class SoloRaidEntityConfiguration : IEntityTypeConfiguration<SoloRaidEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.RaidId }).IsUnique();
    }
}

public class SoloRaidLevelEntityConfiguration : IEntityTypeConfiguration<SoloRaidLevelEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidLevelEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.SoloRaid)
            .WithMany(r => r.Levels)
            .HasForeignKey(e => e.SoloRaidId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidId, e.RaidLevel, e.Type, e.IsOpen }).IsUnique();
    }
}

public class SoloRaidLogEntityConfiguration : IEntityTypeConfiguration<SoloRaidLogEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidLogEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.SoloRaidLevel)
            .WithMany(l => l.Logs)
            .HasForeignKey(e => e.SoloRaidLevelId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidLevelId, e.Sequence }).IsUnique();
    }
}

public class SoloRaidLogCharacterEntityConfiguration : IEntityTypeConfiguration<SoloRaidLogCharacterEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidLogCharacterEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.SoloRaidLog)
            .WithMany(l => l.Characters)
            .HasForeignKey(e => e.SoloRaidLogId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidLogId, e.Slot }).IsUnique();
    }
}

public class SoloRaidMuseumStageEntityConfiguration : IEntityTypeConfiguration<SoloRaidMuseumStageEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidMuseumStageEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.UserId, e.StageId }).IsUnique();
    }
}

public class SoloRaidMuseumModeEntityConfiguration : IEntityTypeConfiguration<SoloRaidMuseumModeEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidMuseumModeEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Stage)
            .WithMany(s => s.Modes)
            .HasForeignKey(e => e.SoloRaidMuseumStageId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidMuseumStageId, e.ModeType }).IsUnique();
    }
}

public class SoloRaidMuseumMissionClaimEntityConfiguration : IEntityTypeConfiguration<SoloRaidMuseumMissionClaimEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidMuseumMissionClaimEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Stage)
            .WithMany(s => s.MissionClaims)
            .HasForeignKey(e => e.SoloRaidMuseumStageId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidMuseumStageId, e.ModeType, e.MissionId }).IsUnique();
    }
}

public class SoloRaidMuseumTeamEntityConfiguration : IEntityTypeConfiguration<SoloRaidMuseumTeamEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidMuseumTeamEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Stage)
            .WithMany(s => s.Teams)
            .HasForeignKey(e => e.SoloRaidMuseumStageId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidMuseumStageId, e.TeamNumber }).IsUnique();
    }
}

public class SoloRaidMuseumTeamSlotEntityConfiguration : IEntityTypeConfiguration<SoloRaidMuseumTeamSlotEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidMuseumTeamSlotEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Team)
            .WithMany(t => t.Slots)
            .HasForeignKey(e => e.SoloRaidMuseumTeamId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidMuseumTeamId, e.Slot }).IsUnique();
    }
}

public class SoloRaidMuseumOpenTeamEntityConfiguration : IEntityTypeConfiguration<SoloRaidMuseumOpenTeamEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidMuseumOpenTeamEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Mode)
            .WithMany(m => m.OpenTeams)
            .HasForeignKey(e => e.SoloRaidMuseumModeId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidMuseumModeId, e.TeamNumber }).IsUnique();
        entity.HasIndex(e => new { e.SoloRaidMuseumModeId, e.Sequence }).IsUnique();
    }
}

public class SoloRaidMuseumLogEntityConfiguration : IEntityTypeConfiguration<SoloRaidMuseumLogEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidMuseumLogEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Mode)
            .WithMany(m => m.Logs)
            .HasForeignKey(e => e.SoloRaidMuseumModeId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidMuseumModeId, e.LogSet, e.TeamNumber }).IsUnique();
        entity.HasIndex(e => new { e.SoloRaidMuseumModeId, e.LogSet, e.Sequence }).IsUnique();
    }
}

public class SoloRaidMuseumLogCharacterEntityConfiguration : IEntityTypeConfiguration<SoloRaidMuseumLogCharacterEntity>
{
    public void Configure(EntityTypeBuilder<SoloRaidMuseumLogCharacterEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Log)
            .WithMany(l => l.Characters)
            .HasForeignKey(e => e.SoloRaidMuseumLogId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.SoloRaidMuseumLogId, e.Slot }).IsUnique();
    }
}
