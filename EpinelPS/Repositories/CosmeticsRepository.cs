using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Repositories;

public sealed class CosmeticsRepository(GameContext db)
{
    public const string CategoryCostume = "Costume";
    public const string CategoryFrame = "Frame";
    public const string CategoryTitle = "Title";
    public const string CategoryProfileCard = "ProfileCard";
    public const string CategoryMemorial = "Memorial";
    public const string CategoryBackground = "Background";
    public const string CategoryIcon = "Icon";
    public const string CategorySticker = "Sticker";
    public const string CategoryLiveWallpaper = "LiveWallpaper";
    public const string CategoryLobbyDecoBackground = "LobbyDecoBackground";
    public const string CategoryJukeboxBgm = "JukeboxBgm";
    public const string CategoryJukeboxTheme = "JukeboxTheme";

    // --- Unlocked Cosmetics ---
    public List<int> GetUnlockedCosmetics(ulong userId, string category)
    {
        return db.Set<UserUnlockedCosmetic>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Category == category)
            .Select(x => x.CosmeticId)
            .ToList();
    }

    public int CountUnlockedCosmetics(ulong userId, string category)
    {
        return db.Set<UserUnlockedCosmetic>()
            .AsNoTracking()
            .Count(x => x.UserId == userId && x.Category == category);
    }

    public bool HasUnlockedCosmetic(ulong userId, string category, int cosmeticId)
    {
        return db.Set<UserUnlockedCosmetic>().Local.Any(x =>
                   x.UserId == userId && x.Category == category && x.CosmeticId == cosmeticId) ||
               db.Set<UserUnlockedCosmetic>().AsNoTracking().Any(x =>
                   x.UserId == userId && x.Category == category && x.CosmeticId == cosmeticId);
    }

    public bool UnlockCosmetic(ulong userId, string category, int cosmeticId)
    {
        if (HasUnlockedCosmetic(userId, category, cosmeticId))
            return false;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        db.Set<UserUnlockedCosmetic>().Add(new UserUnlockedCosmetic
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            Category = category,
            CosmeticId = cosmeticId
        });
        return true;
    }

    public int UnlockCosmetics(ulong userId, string category, IEnumerable<int> cosmeticIds)
    {
        var existing = db.Set<UserUnlockedCosmetic>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Category == category)
            .Select(x => x.CosmeticId)
            .ToHashSet();

        foreach (var local in db.Set<UserUnlockedCosmetic>().Local)
        {
            if (local.UserId == userId && local.Category == category)
                existing.Add(local.CosmeticId);
        }

        int added = 0;
        bool ensuredUser = false;
        foreach (int id in cosmeticIds.Distinct())
        {
            if (!existing.Add(id))
                continue;

            if (!ensuredUser)
            {
                UserDbHelper.GetOrCreateGameUser(db, userId);
                ensuredUser = true;
            }

            db.Set<UserUnlockedCosmetic>().Add(new UserUnlockedCosmetic
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Category = category,
                CosmeticId = id
            });
            added++;
        }

        return added;
    }

    // --- Profile Card Decoration ---
    public ProfileCardDecorationLayout GetProfileCardDecoration(ulong userId)
    {
        var deco = db.Set<UserProfileCardDecoration>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId);

        if (deco == null || deco.BackgroundId == 0)
        {
            return new ProfileCardDecorationLayout
            {
                BackgroundId = 301001,
                ShowCharacterSpine = true
            };
        }

        var stickerRows = db.Set<UserProfileCardSticker>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Position)
            .ToList();

        var layout = new ProfileCardDecorationLayout
        {
            ShowCharacterSpine = deco.ShowCharacterSpine,
            BackgroundId = deco.BackgroundId
        };
        layout.StickerPlacements.AddRange(stickerRows.Select(s => new StickerPlacement
        {
            StickerId = s.StickerId,
            Layer = s.Layer,
            Normalizedx = s.NormalizedX,
            Normalizedy = s.NormalizedY,
            RotationRadian = s.RotationRadian,
            Scale = s.Scale
        }));

        return layout;
    }

    public void SaveProfileCardDecoration(ulong userId, ProfileCardDecorationLayout layout)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        var existingDeco = db.Set<UserProfileCardDecoration>().FirstOrDefault(x => x.UserId == userId);
        if (existingDeco == null)
        {
            db.Set<UserProfileCardDecoration>().Add(new UserProfileCardDecoration
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                ShowCharacterSpine = layout.ShowCharacterSpine,
                BackgroundId = layout.BackgroundId
            });
        }
        else
        {
            existingDeco.ShowCharacterSpine = layout.ShowCharacterSpine;
            existingDeco.BackgroundId = layout.BackgroundId;
        }

        var stickerSet = db.Set<UserProfileCardSticker>();
        var existingStickers = stickerSet.Where(x => x.UserId == userId).ToList();
        if (existingStickers.Count > 0)
            stickerSet.RemoveRange(existingStickers);

        for (int idx = 0; idx < layout.StickerPlacements.Count; idx++)
        {
            var s = layout.StickerPlacements[idx];
            stickerSet.Add(new UserProfileCardSticker
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Position = idx,
                StickerId = s.StickerId,
                Layer = s.Layer,
                NormalizedX = s.Normalizedx,
                NormalizedY = s.Normalizedy,
                RotationRadian = s.RotationRadian,
                Scale = s.Scale
            });
        }
    }

    // --- Jukebox Settings, Rewards & Playlists ---
    public JukeBoxSetting GetJukeboxSetting(ulong userId, NetJukeboxLocation location)
    {
        var row = db.Set<UserJukeboxSetting>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId && x.Location == (int)location);

        if (row == null)
        {
            return new JukeBoxSetting
            {
                Location = location,
                Type = NetJukeboxBgmType.JukeboxTableId,
                TableId = location == NetJukeboxLocation.Lobby ? 2 : 5
            };
        }

        return new JukeBoxSetting
        {
            Location = location,
            Type = (NetJukeboxBgmType)row.BgmType,
            TableId = row.TableId,
            IsShuffle = row.IsShuffle
        };
    }

    public void SetJukeboxSetting(ulong userId, JukeBoxSetting setting)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        int loc = (int)setting.Location;
        var existing = db.Set<UserJukeboxSetting>().FirstOrDefault(x => x.UserId == userId && x.Location == loc);
        if (existing == null)
        {
            db.Set<UserJukeboxSetting>().Add(new UserJukeboxSetting
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Location = loc,
                BgmType = (int)setting.Type,
                TableId = setting.TableId,
                IsShuffle = setting.IsShuffle
            });
        }
        else
        {
            existing.BgmType = (int)setting.Type;
            existing.TableId = setting.TableId;
            existing.IsShuffle = setting.IsShuffle;
        }
    }

    public List<int> GetClaimedJukeboxRewards(ulong userId)
    {
        return db.Set<UserClaimedJukeboxReward>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.TriggerId)
            .ToList();
    }

    public bool ClaimJukeboxReward(ulong userId, int triggerId)
    {
        bool exists = db.Set<UserClaimedJukeboxReward>().Local.Any(x => x.UserId == userId && x.TriggerId == triggerId) ||
                      db.Set<UserClaimedJukeboxReward>().AsNoTracking().Any(x => x.UserId == userId && x.TriggerId == triggerId);
        if (exists)
            return false;

        UserDbHelper.GetOrCreateGameUser(db, userId);
        db.Set<UserClaimedJukeboxReward>().Add(new UserClaimedJukeboxReward
        {
            Id = UlidGenerator.NewUlid(),
            UserId = userId,
            TriggerId = triggerId
        });
        return true;
    }

    public NetJukeboxFavorite GetFavoriteSongs(ulong userId)
    {
        var row = db.Set<UserJukeboxPlaylistEntity>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId && x.IsFavorite);

        var fav = new NetJukeboxFavorite();
        if (row != null && !string.IsNullOrWhiteSpace(row.SongsJson))
        {
            var songs = JsonConvert.DeserializeObject<List<NetJukeboxPlaylistSong>>(row.SongsJson);
            if (songs != null)
                fav.Songs.AddRange(songs);
        }
        return fav;
    }

    public void SaveFavoriteSongs(ulong userId, IEnumerable<NetJukeboxPlaylistSong> songs)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        string json = JsonConvert.SerializeObject(songs.ToList());
        var existing = db.Set<UserJukeboxPlaylistEntity>()
            .FirstOrDefault(x => x.UserId == userId && x.IsFavorite && x.PlaylistUid == 0);

        if (existing == null)
        {
            db.Set<UserJukeboxPlaylistEntity>().Add(new UserJukeboxPlaylistEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                PlaylistUid = 0,
                IsFavorite = true,
                Title = "",
                SongsJson = json
            });
        }
        else
        {
            existing.SongsJson = json;
        }
    }

    public List<NetJukeboxPlaylist> GetPlaylists(ulong userId)
    {
        var rows = db.Set<UserJukeboxPlaylistEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsFavorite)
            .OrderBy(x => x.PlaylistUid)
            .ToList();

        return rows.Select(MapToPlaylist).ToList();
    }

    public NetJukeboxPlaylist? GetPlaylist(ulong userId, long playlistUid)
    {
        var row = db.Set<UserJukeboxPlaylistEntity>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId && !x.IsFavorite && x.PlaylistUid == playlistUid);

        return row != null ? MapToPlaylist(row) : null;
    }

    public long GetNextPlaylistUid(ulong userId)
    {
        long maxUid = db.Set<UserJukeboxPlaylistEntity>()
            .Where(x => x.UserId == userId && !x.IsFavorite)
            .Select(x => (long?)x.PlaylistUid)
            .Max() ?? 0L;
        return maxUid + 1;
    }

    public void SavePlaylist(ulong userId, NetJukeboxPlaylist playlist)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        string json = JsonConvert.SerializeObject(playlist.Songs.ToList());
        var existing = db.Set<UserJukeboxPlaylistEntity>()
            .FirstOrDefault(x => x.UserId == userId && !x.IsFavorite && x.PlaylistUid == playlist.JukeboxPlaylistUid);

        if (existing == null)
        {
            db.Set<UserJukeboxPlaylistEntity>().Add(new UserJukeboxPlaylistEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                PlaylistUid = playlist.JukeboxPlaylistUid,
                IsFavorite = false,
                Title = playlist.Title ?? "",
                SongsJson = json
            });
        }
        else
        {
            existing.Title = playlist.Title ?? "";
            existing.SongsJson = json;
        }
    }

    public bool DeletePlaylist(ulong userId, long playlistUid)
    {
        var existing = db.Set<UserJukeboxPlaylistEntity>()
            .FirstOrDefault(x => x.UserId == userId && !x.IsFavorite && x.PlaylistUid == playlistUid);

        if (existing == null)
            return false;

        db.Set<UserJukeboxPlaylistEntity>().Remove(existing);
        return true;
    }

    private static NetJukeboxPlaylist MapToPlaylist(UserJukeboxPlaylistEntity row)
    {
        var playlist = new NetJukeboxPlaylist
        {
            JukeboxPlaylistUid = row.PlaylistUid,
            Title = row.Title ?? ""
        };
        if (!string.IsNullOrWhiteSpace(row.SongsJson))
        {
            var songs = JsonConvert.DeserializeObject<List<NetJukeboxPlaylistSong>>(row.SongsJson);
            if (songs != null)
                playlist.Songs.AddRange(songs);
        }
        return playlist;
    }

    // --- Wallpapers ---
    public (
        NetWallpaperData[] Slots,
        NetWallpaperBackground[] Backgrounds,
        NetWallpaperJukebox[] Jukeboxes,
        NetWallpaperJukeboxFavorite[] Favorites,
        NetWallpaperPlaylist[] Playlists) GetWallpapers(ulong userId)
    {
        var slots = db.Set<UserWallpaperSlot>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperData
            {
                Order = x.OrderIndex,
                Type = (LiveWallpaperType)x.Type,
                Id = x.WallpaperId,
                UseCharacterBackground = x.UseCharacterBackground
            })
            .ToArray();

        var backgrounds = db.Set<UserWallpaperBackground>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperBackground
            {
                Order = x.OrderIndex,
                LobbyDecoBackgroundId = x.LobbyDecoBackgroundId
            })
            .ToArray();

        var jukeboxes = db.Set<UserWallpaperJukebox>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperJukebox
            {
                Order = x.OrderIndex,
                JukeboxListTableId = x.JukeboxListTableId
            })
            .ToArray();

        var favorites = db.Set<UserWallpaperFavorite>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperJukeboxFavorite
            {
                Order = x.OrderIndex,
                IsShuffled = x.IsShuffled
            })
            .ToArray();

        var playlists = db.Set<UserWallpaperPlaylist>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperPlaylist
            {
                Order = x.OrderIndex,
                PlaylistUId = x.PlaylistUId,
                IsShuffled = x.IsShuffled
            })
            .ToArray();

        return (slots, backgrounds, jukeboxes, favorites, playlists);
    }

    public void SaveWallpapers(ulong userId, ReqSetWallpaper req)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);

        ReplaceRows(
            userId,
            req.WallpaperList.GroupBy(x => x.Order).Select(g => g.First()),
            s => new UserWallpaperSlot
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                Type = (int)s.Type,
                WallpaperId = s.Id,
                UseCharacterBackground = s.UseCharacterBackground
            },
            e => e.UserId == userId);

        ReplaceRows(
            userId,
            req.WallpaperBackgroundList.GroupBy(x => x.Order).Select(g => g.First()),
            s => new UserWallpaperBackground
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                LobbyDecoBackgroundId = s.LobbyDecoBackgroundId
            },
            e => e.UserId == userId);

        ReplaceRows(
            userId,
            req.WallpaperJukeboxList.GroupBy(x => x.Order).Select(g => g.First()),
            s => new UserWallpaperJukebox
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                JukeboxListTableId = s.JukeboxListTableId
            },
            e => e.UserId == userId);

        ReplaceRows(
            userId,
            req.WallpaperFavoriteList.GroupBy(x => x.Order).Select(g => g.First()),
            s => new UserWallpaperFavorite
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                IsShuffled = s.IsShuffled
            },
            e => e.UserId == userId);

        ReplaceRows(
            userId,
            req.WallpaperPlaylistList.GroupBy(x => x.Order).Select(g => g.First()),
            s => new UserWallpaperPlaylist
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                PlaylistUId = s.PlaylistUId,
                IsShuffled = s.IsShuffled
            },
            e => e.UserId == userId);
    }

    private void ReplaceRows<TEntity, TSource>(
        ulong userId,
        IEnumerable<TSource> items,
        Func<TSource, TEntity> map,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> filter)
        where TEntity : class
    {
        var set = db.Set<TEntity>();
        var existing = set.Where(filter).ToList();
        if (existing.Count > 0)
            set.RemoveRange(existing);

        foreach (var item in items)
            set.Add(map(item));
    }

    // --- Lobby Private Banners ---
    public List<int> GetLobbyPrivateBannerIds(ulong userId)
    {
        return db.Set<UserLobbyPrivateBannerEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.BannerId)
            .ToList();
    }
}
