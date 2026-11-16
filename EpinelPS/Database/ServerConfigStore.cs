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

    public static readonly byte[] ServerPrivateKey = Convert.FromBase64String("FSUY8Ohd942n5LWAfxn6slK3YGwc8OqmyJoJup9nNos=");
    public static readonly byte[] ServerPublicKey = Convert.FromBase64String("04hFDd1e/BOEF2h4b0MdkX2h6W5REeqyW+0r9+eSeh0=");

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
            var settings = context.Set<ServerSettings>().Find(1);
            if (settings == null)
            {
                settings = new ServerSettings
                {
                    Id = 1,
                    LogLevel = LogType.Debug,
                    MaxInterceptionCount = 3,
                    ResetHourUtcTime = 20
                };
                context.Set<ServerSettings>().Add(settings);
            }

            var secrets = context.Set<ServerSecrets>().Find(1);
            if (secrets == null)
            {
                byte[] launcherKey = new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Local).GenerateSymmetricKey().Key.ToArray();
                byte[] encryptionKey = new PasetoBuilder().Use(ProtocolVersion.V4, Purpose.Local).GenerateSymmetricKey().Key.ToArray();

                secrets = new ServerSecrets
                {
                    Id = 1,
                    LauncherTokenKey = launcherKey,
                    EncryptionTokenKey = encryptionKey
                };
                context.Set<ServerSecrets>().Add(secrets);
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
                    context.Set<ServerSecrets>().Update(secrets);
            }

            context.SaveChanges();

            _settings = settings;
            _secrets = secrets;
            _activeEventBannerIds = context.Set<EventBanner>()
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
            var settings = context.Set<ServerSettings>().Find(1) ?? new ServerSettings { Id = 1 };
            settings.LogLevel = level;
            if (context.Entry(settings).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
                context.Set<ServerSettings>().Add(settings);
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
            var existing = context.Set<EventBanner>().ToList();
            foreach (var banner in existing)
            {
                banner.IsActive = activeStrings.Contains(banner.BannerId);
            }

            var existingIds = existing.Select(b => b.BannerId).ToHashSet(StringComparer.Ordinal);
            foreach (string idStr in activeStrings)
            {
                if (!existingIds.Contains(idStr))
                {
                    context.Set<EventBanner>().Add(new EventBanner
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
