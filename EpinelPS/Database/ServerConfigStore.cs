using System.Globalization;
using EpinelPS.Models;
using EpinelPS.Utils;
using Paseto;
using Paseto.Builder;

namespace EpinelPS.Database;

public static class ServerConfigStore
{
    private static readonly Lock SyncLock = new();
    private static bool _initialized;

    private static ServerSettings _settings = new();
    private static ServerSecrets _secrets = new();
    private static List<int> _activeEventBannerIds = [];

    public static LogType LogLevel
    {
        get { EnsureLoaded(); return _settings.LogLevel; }
    }

    public static int MaxInterceptionCount
    {
        get { EnsureLoaded(); return _settings.MaxInterceptionCount; }
    }

    public static int ResetHourUtcTime
    {
        get { EnsureLoaded(); return _settings.ResetHourUtcTime; }
    }

    public static byte[] LauncherTokenKey
    {
        get { EnsureLoaded(); return _secrets.LauncherTokenKey; }
    }

    public static byte[] EncryptionTokenKey
    {
        get { EnsureLoaded(); return _secrets.EncryptionTokenKey; }
    }

    public static List<int> ActiveEventBannerIds
    {
        get
        {
            EnsureLoaded();
            lock (SyncLock)
            {
                return [.. _activeEventBannerIds];
            }
        }
    }

    public static void InitializeFromContext(GameContext context)
    {
        lock (SyncLock)
        {
            bool hasJsonDb = File.Exists(DatabasePaths.JsonConfiguration);

            var settings = context.ServerSettings.Find(1);
            if (settings == null)
            {
                settings = new ServerSettings
                {
                    Id = 1,
                    LogLevel = hasJsonDb ? JsonDb.Instance.LogLevel : LogType.Debug,
                    MaxInterceptionCount = hasJsonDb ? JsonDb.Instance.MaxInterceptionCount : 3,
                    ResetHourUtcTime = hasJsonDb ? JsonDb.Instance.ResetHourUtcTime : 20
                };
                context.ServerSettings.Add(settings);
            }

            var secrets = context.ServerSecrets.Find(1);
            if (secrets == null)
            {
                byte[] launcherKey = hasJsonDb && JsonDb.Instance.LauncherTokenKey.Length > 0
                    ? JsonDb.Instance.LauncherTokenKey
                    : new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Local).GenerateSymmetricKey().Key.ToArray();

                byte[] encryptionKey = hasJsonDb && JsonDb.Instance.EncryptionTokenKey.Length > 0
                    ? JsonDb.Instance.EncryptionTokenKey
                    : new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Local).GenerateSymmetricKey().Key.ToArray();

                secrets = new ServerSecrets
                {
                    Id = 1,
                    LauncherTokenKey = launcherKey,
                    EncryptionTokenKey = encryptionKey
                };
                context.ServerSecrets.Add(secrets);
            }
            else
            {
                bool updated = false;
                if (secrets.LauncherTokenKey.Length == 0)
                {
                    secrets.LauncherTokenKey = new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Local).GenerateSymmetricKey().Key.ToArray();
                    updated = true;
                }
                if (secrets.EncryptionTokenKey.Length == 0)
                {
                    secrets.EncryptionTokenKey = new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Local).GenerateSymmetricKey().Key.ToArray();
                    updated = true;
                }
                if (updated)
                    context.ServerSecrets.Update(secrets);
            }

            if (!context.EventBanners.Any() && hasJsonDb && JsonDb.Instance.ActiveEventBannerIds.Count > 0)
            {
                foreach (int bannerId in JsonDb.Instance.ActiveEventBannerIds.Distinct())
                {
                    context.EventBanners.Add(new EventBanner
                    {
                        BannerId = bannerId.ToString(CultureInfo.InvariantCulture),
                        IsActive = true
                    });
                }
            }

            context.SaveChanges();

            _settings = settings;
            _secrets = secrets;
            _activeEventBannerIds = context.EventBanners
                .Where(b => b.IsActive)
                .AsEnumerable()
                .Select(b => int.TryParse(b.BannerId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : 0)
                .Where(id => id != 0)
                .ToList();

            Logging.SetOutputLevel(_settings.LogLevel);
            _initialized = true;
        }
    }

    public static void SetLogLevel(GameContext context, LogType level)
    {
        lock (SyncLock)
        {
            var settings = context.ServerSettings.Find(1) ?? new ServerSettings { Id = 1 };
            settings.LogLevel = level;
            if (context.Entry(settings).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                context.ServerSettings.Add(settings);
            context.SaveChanges();

            _settings.LogLevel = level;
            Logging.SetOutputLevel(level);
        }
    }

    public static void SetActiveEventBanners(GameContext context, IEnumerable<int> activeBannerIds)
    {
        var distinctIds = activeBannerIds.Distinct().ToList();
        var activeStrings = distinctIds
            .Select(id => id.ToString(CultureInfo.InvariantCulture))
            .ToHashSet(StringComparer.Ordinal);

        lock (SyncLock)
        {
            var existing = context.EventBanners.ToList();
            foreach (var banner in existing)
            {
                banner.IsActive = activeStrings.Contains(banner.BannerId);
            }

            var existingIds = existing.Select(b => b.BannerId).ToHashSet(StringComparer.Ordinal);
            foreach (string idStr in activeStrings)
            {
                if (!existingIds.Contains(idStr))
                {
                    context.EventBanners.Add(new EventBanner
                    {
                        BannerId = idStr,
                        IsActive = true
                    });
                }
            }

            context.SaveChanges();
            _activeEventBannerIds = distinctIds;
        }
    }

    private static void EnsureLoaded()
    {
        if (_initialized) return;

        lock (SyncLock)
        {
            if (_initialized) return;
            using var context = GameContext.CreateNew();
            InitializeFromContext(context);
        }
    }
}
