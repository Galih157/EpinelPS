using EpinelPS.Models;
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database;

public partial class GameContext : DbContext
{
    // Core & Server
    public DbSet<SdkUser> SdkUsers { get; set; }
    public DbSet<GameUser> Users { get; set; }
    public DbSet<TriggerModelNew> Triggers { get; set; }
    public DbSet<ServerSettings> ServerSettings { get; set; }
    public DbSet<ServerSecrets> ServerSecrets { get; set; }
    public DbSet<EventBanner> EventBanners { get; set; }
    public DbSet<UserBan> UserBans { get; set; }

    // Inventory, characters, and equipment awakenings
    public DbSet<ItemEntity> Items { get; set; }
    public DbSet<ItemCharacterAssignmentEntity> ItemCharacterAssignments { get; set; }
    public DbSet<CharacterEntity> Characters { get; set; }
    public DbSet<EquipmentAwakeningEntity> EquipmentAwakenings { get; set; }

    // Outpost
    public DbSet<OutpostEntity> Outposts { get; set; }
    public DbSet<DispatchClearItemEntity> DispatchClearItems { get; set; }
    public DbSet<SelectableDispatchEntity> SelectableDispatches { get; set; }
    public DbSet<DispatchEntity> Dispatches { get; set; }
    public DbSet<InfraCoreRewardEntity> InfraCoreRewards { get; set; }
    public DbSet<SynchroSlotEntity> SynchroSlots { get; set; }
    public DbSet<ResearchProgressEntity> ResearchProgress { get; set; }
    public DbSet<OutpostBuildingEntity> OutpostBuildings { get; set; }

    // Scenarios, currency, and progress unlocks
    public DbSet<UserCompletedScenarioEntity> UserCompletedScenarios { get; set; }
    public DbSet<UserCurrencyEntity> UserCurrencies { get; set; }
    public DbSet<UserMainQuestEntity> UserMainQuests { get; set; }
    public DbSet<UserTowerProgressEntity> UserTowerProgress { get; set; }
    public DbSet<UserClearedTutorialEntity> UserClearedTutorials { get; set; }
    public DbSet<UserContentsUnlockEntity> UserContentsUnlocks { get; set; }
    public DbSet<UserViewedNoticePopupEntity> UserViewedNoticePopups { get; set; }

    // Daily and weekly reset state
    public DbSet<UserResetStateEntity> UserResetStates { get; set; }
    public DbSet<UserCompletedDailyMissionEntity> UserCompletedDailyMissions { get; set; }
    public DbSet<UserCompletedWeeklyMissionEntity> UserCompletedWeeklyMissions { get; set; }
    public DbSet<UserDailyTowerCountEntity> UserDailyTowerCounts { get; set; }
    public DbSet<UserDailyCounselCountEntity> UserDailyCounselCounts { get; set; }

    // Teams and representation
    public DbSet<UserTeamGroupEntity> UserTeamGroups { get; set; }
    public DbSet<UserTeamSlotEntity> UserTeamSlots { get; set; }
    public DbSet<UserRepresentationTeamSlotEntity> UserRepresentationTeamSlots { get; set; }

    // Profile, cosmetics, wallpapers, and lobby music
    public DbSet<UserProfileCardDecoration> UserProfileCardDecorations { get; set; }
    public DbSet<UserProfileCardSticker> UserProfileCardStickers { get; set; }
    public DbSet<UserUnlockedCosmetic> UserUnlockedCosmetics { get; set; }
    public DbSet<UserJukeboxSetting> UserJukeboxSettings { get; set; }
    public DbSet<UserClaimedJukeboxReward> UserClaimedJukeboxRewards { get; set; }
    public DbSet<UserWallpaperSlot> UserWallpaperSlots { get; set; }
    public DbSet<UserWallpaperBackground> UserWallpaperBackgrounds { get; set; }
    public DbSet<UserWallpaperJukebox> UserWallpaperJukeboxes { get; set; }
    public DbSet<UserWallpaperFavorite> UserWallpaperFavorites { get; set; }
    public DbSet<UserWallpaperPlaylist> UserWallpaperPlaylists { get; set; }

    // Gacha, normal shop, and passes
    public DbSet<UserGachaBannerPull> UserGachaBannerPulls { get; set; }
    public DbSet<UserGachaPityCount> UserGachaPityCounts { get; set; }
    public DbSet<UserGachaSelectupChoice> UserGachaSelectupChoices { get; set; }
    public DbSet<UserGachaDailyFreePull> UserGachaDailyFreePulls { get; set; }
    public DbSet<UserGachaPayback> UserGachaPaybacks { get; set; }
    public DbSet<UserGachaPaybackReward> UserGachaPaybackRewards { get; set; }
    public DbSet<UserNormalShopState> UserNormalShopStates { get; set; }
    public DbSet<UserNormalShopOrder> UserNormalShopOrders { get; set; }
    public DbSet<UserNormalShopPinnedBuy> UserNormalShopPinnedBuys { get; set; }
    public DbSet<UserPass> UserPasses { get; set; }
    public DbSet<UserPassRank> UserPassRanks { get; set; }
    public DbSet<UserPassMission> UserPassMissions { get; set; }

    // Events and login events
    public DbSet<UserEvent> UserEvents { get; set; }
    public DbSet<UserEventCompletedScenario> UserEventCompletedScenarios { get; set; }
    public DbSet<UserEventClearedStage> UserEventClearedStages { get; set; }
    public DbSet<UserLoginEvent> UserLoginEvents { get; set; }
    public DbSet<UserLoginEventDay> UserLoginEventDays { get; set; }
    public DbSet<UserEventMission> UserEventMissions { get; set; }
    public DbSet<UserEventMissionEntry> UserEventMissionEntries { get; set; }
    public DbSet<UserEventShopBuy> UserEventShopBuys { get; set; }

    // Bonds, favorite items, and liberation
    public DbSet<UserBond> UserBonds { get; set; }
    public DbSet<UserBondProgressItem> UserBondProgressItems { get; set; }
    public DbSet<UserFavoriteItem> UserFavoriteItems { get; set; }
    public DbSet<UserFavoriteItemQuest> UserFavoriteItemQuests { get; set; }
    public DbSet<UserLiberation> UserLiberations { get; set; }
    public DbSet<UserLiberationMission> UserLiberationMissions { get; set; }
    public DbSet<UserOpenLiberateType> UserOpenLiberateTypes { get; set; }

    // Messenger and badges
    public DbSet<UserMessage> UserMessages { get; set; }
    public DbSet<UserPickedMessage> UserPickedMessages { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }

    // Achievements, side story, archive, and Lost Sector
    public DbSet<UserCompletedAchievement> UserCompletedAchievements { get; set; }
    public DbSet<UserSideStoryStage> UserSideStoryStages { get; set; }
    public DbSet<UserArchiveUnlock> UserArchiveUnlocks { get; set; }
    public DbSet<UserLostSector> UserLostSectors { get; set; }
    public DbSet<UserLostSectorClearedStage> UserLostSectorClearedStages { get; set; }
    public DbSet<UserLostSectorObject> UserLostSectorObjects { get; set; }

    /// <summary>
    /// GameContext instance. Should only be used in console thread.
    /// Long-lived, set once at startup via <see cref="SetInstance"/> - do NOT set this from
    /// the constructor, as DI creates short-lived scoped instances per request that get
    /// disposed when the request ends, leaving a dangling disposed instance behind.
    /// </summary>
    public static GameContext Instance { get; private set; } = null!;

    /// <summary>
    /// Static options for standalone/background GameContext instances created via <see cref="CreateNew"/>.
    /// Set once at startup via <see cref="InitializeOptions"/> - do NOT set or overwrite this from
    /// the constructor, as DI creates short-lived scoped instances per request that get
    /// disposed when the request ends, which would leave a dangling disposed IServiceProvider behind.
    /// </summary>
    public static DbContextOptions<GameContext>? Options { get; private set; }

    public GameContext(DbContextOptions<GameContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameContext).Assembly);
    }

    public static void InitializeOptions(string connectionString, string? connectionType)
    {
        var builder = new DbContextOptionsBuilder<GameContext>();
        switch (connectionType?.ToLowerInvariant())
        {
            case "sql":
                builder.UseSqlServer(connectionString);
                break;
            case "mysql":
                builder.UseMySQL(connectionString);
                break;
            case "npgsql":
                builder.UseNpgsql(connectionString);
                break;
            default:
                builder.UseSqlite(connectionString);
                break;
        }
        Options = builder.Options;
    }

    public static void SetInstance(GameContext context)
    {
        Instance = context;
    }

    public static GameContext CreateNew()
    {
        if (Options == null)
        {
            DatabasePaths.EnsureDataDirectory();
            InitializeOptions($"Data Source=\"{DatabasePaths.SqliteDatabase}\"", "sqlite");
        }
        return new GameContext(Options);
    }
}
