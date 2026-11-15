
using Microsoft.EntityFrameworkCore;

namespace EpinelPS.Database;

public class GameContext : DbContext
{
    /// <summary>
    /// Contains user login information
    /// </summary>
    public DbSet<SdkUser> SdkUsers { get; set; }

    /// <summary>
    /// Contains in-game user information
    /// </summary>
    public DbSet<GameUser> Users { get; set; }

    /// <summary>
    /// Contains trigger information
    /// </summary>
    public DbSet<TriggerModelNew> Triggers { get; set; }

    public DbSet<ServerSettings> ServerSettings { get; set; }
    public DbSet<ServerSecrets> ServerSecrets { get; set; }
    public DbSet<EventBanner> EventBanners { get; set; }
    public DbSet<UserBan> UserBans { get; set; }

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

        modelBuilder.Entity<SdkUser>()
            .HasIndex(u => u.Email);

        modelBuilder.Entity<UserBan>(entity =>
        {
            entity.HasIndex(b => b.UserId);
            entity.HasOne(b => b.User)
                .WithMany(u => u.Bans)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
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
