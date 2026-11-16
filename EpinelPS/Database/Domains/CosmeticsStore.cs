using EpinelPS.Models;
using EpinelPS.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database.Domains;

public sealed class CosmeticsStore : IUserDomainStore
{
    private const string CosmeticCostume = "Costume";
    private const string CosmeticFrame = "Frame";
    private const string CosmeticTitle = "Title";
    private const string CosmeticProfileCard = "ProfileCard";
    private const string CosmeticMemorial = "Memorial";
    private const string CosmeticBackground = "Background";
    private const string CosmeticIcon = "Icon";
    private const string CosmeticSticker = "Sticker";
    private const string CosmeticLiveWallpaper = "LiveWallpaper";
    private const string CosmeticLobbyDecoBackground = "LobbyDecoBackground";
    private const string CosmeticJukeboxBgm = "JukeboxBgm";
    private const string CosmeticJukeboxTheme = "JukeboxTheme";

    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        var profileDeco = db.Set<UserProfileCardDecoration>().AsNoTracking().FirstOrDefault(x => x.UserId == userId);
        if (profileDeco != null)
        {
            user.ProfileCardDecoration.ShowCharacterSpine = profileDeco.ShowCharacterSpine;
            user.ProfileCardDecoration.BackgroundId = profileDeco.BackgroundId;
        }

        var stickerRows = db.Set<UserProfileCardSticker>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Position)
            .ToList();
        user.ProfileCardDecoration.StickerPlacements.Clear();
        user.ProfileCardDecoration.StickerPlacements.AddRange(stickerRows.Select(s => new StickerPlacement
        {
            StickerId = s.StickerId,
            Layer = s.Layer,
            Normalizedx = s.NormalizedX,
            Normalizedy = s.NormalizedY,
            RotationRadian = s.RotationRadian,
            Scale = s.Scale
        }));

        var cosmetics = db.Set<UserUnlockedCosmetic>().AsNoTracking().Where(x => x.UserId == userId).ToList();
        user.CostumeList = cosmetics.Where(x => x.Category == CosmeticCostume).Select(x => x.CosmeticId).ToList();
        user.FrameList = cosmetics.Where(x => x.Category == CosmeticFrame).Select(x => x.CosmeticId).ToList();
        user.TitleList = cosmetics.Where(x => x.Category == CosmeticTitle).Select(x => x.CosmeticId).ToList();
        user.ProfileCardsData = cosmetics.Where(x => x.Category == CosmeticProfileCard).Select(x => x.CosmeticId).ToList();
        user.Memorial = cosmetics.Where(x => x.Category == CosmeticMemorial).Select(x => x.CosmeticId).ToList();
        user.BackgroundList = cosmetics.Where(x => x.Category == CosmeticBackground).Select(x => x.CosmeticId).ToList();
        user.IconList = cosmetics.Where(x => x.Category == CosmeticIcon).Select(x => x.CosmeticId).ToList();
        user.StickerList = cosmetics.Where(x => x.Category == CosmeticSticker).Select(x => x.CosmeticId).ToList();
        user.LiveWallpaperList = cosmetics.Where(x => x.Category == CosmeticLiveWallpaper).Select(x => x.CosmeticId).ToList();
        user.LobbyDecoBackgroundList = cosmetics.Where(x => x.Category == CosmeticLobbyDecoBackground).Select(x => x.CosmeticId).ToList();
        user.JukeboxBgm = cosmetics.Where(x => x.Category == CosmeticJukeboxBgm).Select(x => x.CosmeticId).ToList();
        user.JukeboxThemeList = cosmetics.Where(x => x.Category == CosmeticJukeboxTheme).Select(x => x.CosmeticId).ToList();

        var jukeboxRows = db.Set<UserJukeboxSetting>().AsNoTracking().Where(x => x.UserId == userId).ToList();
        foreach (var row in jukeboxRows)
        {
            var setting = new JukeBoxSetting
            {
                Location = (NetJukeboxLocation)row.Location,
                Type = (NetJukeboxBgmType)row.BgmType,
                TableId = row.TableId,
                IsShuffle = row.IsShuffle
            };
            if (row.Location == (int)NetJukeboxLocation.Lobby)
                user.LobbyMusic = setting;
            else if (row.Location == (int)NetJukeboxLocation.CommanderRoom)
                user.CommanderMusic = setting;
        }

        user.ClaimedJukeboxRewardTriggers = db.Set<UserClaimedJukeboxReward>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.TriggerId)
            .ToList();

        user.WallpaperList = db.Set<UserWallpaperSlot>().AsNoTracking()
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

        user.WallpaperBackground = db.Set<UserWallpaperBackground>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperBackground
            {
                Order = x.OrderIndex,
                LobbyDecoBackgroundId = x.LobbyDecoBackgroundId
            })
            .ToArray();

        user.WallpaperJukeboxList = db.Set<UserWallpaperJukebox>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperJukebox
            {
                Order = x.OrderIndex,
                JukeboxListTableId = x.JukeboxListTableId
            })
            .ToArray();

        user.WallpaperFavoriteList = db.Set<UserWallpaperFavorite>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperJukeboxFavorite
            {
                Order = x.OrderIndex,
                IsShuffled = x.IsShuffled
            })
            .ToArray();

        user.WallpaperPlaylistList = db.Set<UserWallpaperPlaylist>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperPlaylist
            {
                Order = x.OrderIndex,
                PlaylistUId = x.PlaylistUId,
                IsShuffled = x.IsShuffled
            })
            .ToArray();

        user.LobbyPrivateBannerIds = db.Set<UserLobbyPrivateBannerEntity>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.BannerId)
            .ToList();

        var playlistRows = db.Set<UserJukeboxPlaylistEntity>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToList();
        user.PlayLists = new List<NetJukeboxPlaylist>();
        user.FavoriteSongs = new NetJukeboxFavorite();
        foreach (var row in playlistRows)
        {
            var songs = string.IsNullOrWhiteSpace(row.SongsJson)
                ? new List<NetJukeboxPlaylistSong>()
                : JsonConvert.DeserializeObject<List<NetJukeboxPlaylistSong>>(row.SongsJson) ?? new List<NetJukeboxPlaylistSong>();

            if (row.IsFavorite)
            {
                user.FavoriteSongs.Songs.AddRange(songs);
            }
            else
            {
                var playlist = new NetJukeboxPlaylist
                {
                    JukeboxPlaylistUid = row.PlaylistUid,
                    Title = row.Title ?? ""
                };
                playlist.Songs.AddRange(songs);
                user.PlayLists.Add(playlist);
            }
        }
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        var existingDeco = db.Set<UserProfileCardDecoration>().FirstOrDefault(x => x.UserId == userId);
        if (existingDeco == null)
        {
            db.Set<UserProfileCardDecoration>().Add(new UserProfileCardDecoration
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                ShowCharacterSpine = user.ProfileCardDecoration.ShowCharacterSpine,
                BackgroundId = user.ProfileCardDecoration.BackgroundId
            });
        }
        else
        {
            existingDeco.ShowCharacterSpine = user.ProfileCardDecoration.ShowCharacterSpine;
            existingDeco.BackgroundId = user.ProfileCardDecoration.BackgroundId;
        }

        DomainStoreHelpers.SyncCollection(
            db.Set<UserProfileCardSticker>(),
            db.Set<UserProfileCardSticker>().Where(x => x.UserId == userId).ToList(),
            user.ProfileCardDecoration.StickerPlacements.Select((s, idx) => (Position: idx, Sticker: s)),
            e => e.Position,
            s => s.Position,
            s => new UserProfileCardSticker
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Position = s.Position,
                StickerId = s.Sticker.StickerId,
                Layer = s.Sticker.Layer,
                NormalizedX = s.Sticker.Normalizedx,
                NormalizedY = s.Sticker.Normalizedy,
                RotationRadian = s.Sticker.RotationRadian,
                Scale = s.Sticker.Scale
            },
            (e, s) =>
            {
                e.StickerId = s.Sticker.StickerId;
                e.Layer = s.Sticker.Layer;
                e.NormalizedX = s.Sticker.Normalizedx;
                e.NormalizedY = s.Sticker.Normalizedy;
                e.RotationRadian = s.Sticker.RotationRadian;
                e.Scale = s.Sticker.Scale;
            });

        var desiredCosmetics = (user.CostumeList ?? []).Select(id => (Category: CosmeticCostume, CosmeticId: id))
            .Concat((user.FrameList ?? []).Select(id => (Category: CosmeticFrame, CosmeticId: id)))
            .Concat((user.TitleList ?? []).Select(id => (Category: CosmeticTitle, CosmeticId: id)))
            .Concat((user.ProfileCardsData ?? []).Select(id => (Category: CosmeticProfileCard, CosmeticId: id)))
            .Concat((user.Memorial ?? []).Select(id => (Category: CosmeticMemorial, CosmeticId: id)))
            .Concat((user.BackgroundList ?? []).Select(id => (Category: CosmeticBackground, CosmeticId: id)))
            .Concat((user.IconList ?? []).Select(id => (Category: CosmeticIcon, CosmeticId: id)))
            .Concat((user.StickerList ?? []).Select(id => (Category: CosmeticSticker, CosmeticId: id)))
            .Concat((user.LiveWallpaperList ?? []).Select(id => (Category: CosmeticLiveWallpaper, CosmeticId: id)))
            .Concat((user.LobbyDecoBackgroundList ?? []).Select(id => (Category: CosmeticLobbyDecoBackground, CosmeticId: id)))
            .Concat((user.JukeboxBgm ?? []).Select(id => (Category: CosmeticJukeboxBgm, CosmeticId: id)))
            .Concat((user.JukeboxThemeList ?? []).Select(id => (Category: CosmeticJukeboxTheme, CosmeticId: id)));

        DomainStoreHelpers.SyncCollection(
            db.Set<UserUnlockedCosmetic>(),
            db.Set<UserUnlockedCosmetic>().Where(x => x.UserId == userId).ToList(),
            desiredCosmetics,
            e => (e.Category, e.CosmeticId),
            s => s,
            s => new UserUnlockedCosmetic
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Category = s.Category,
                CosmeticId = s.CosmeticId
            },
            (_, _) => { });

        var desiredJukebox = new (int Location, JukeBoxSetting Setting)[]
        {
            ((int)NetJukeboxLocation.Lobby, user.LobbyMusic),
            ((int)NetJukeboxLocation.CommanderRoom, user.CommanderMusic)
        };
        DomainStoreHelpers.SyncCollection(
            db.Set<UserJukeboxSetting>(),
            db.Set<UserJukeboxSetting>().Where(x => x.UserId == userId).ToList(),
            desiredJukebox,
            e => e.Location,
            s => s.Location,
            s => new UserJukeboxSetting
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Location = s.Location,
                BgmType = (int)s.Setting.Type,
                TableId = s.Setting.TableId,
                IsShuffle = s.Setting.IsShuffle
            },
            (e, s) =>
            {
                e.BgmType = (int)s.Setting.Type;
                e.TableId = s.Setting.TableId;
                e.IsShuffle = s.Setting.IsShuffle;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserClaimedJukeboxReward>(),
            db.Set<UserClaimedJukeboxReward>().Where(x => x.UserId == userId).ToList(),
            user.ClaimedJukeboxRewardTriggers,
            e => e.TriggerId,
            id => id,
            id => new UserClaimedJukeboxReward
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                TriggerId = id
            },
            (_, _) => { });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserWallpaperSlot>(),
            db.Set<UserWallpaperSlot>().Where(x => x.UserId == userId).ToList(),
            user.WallpaperList,
            e => e.OrderIndex,
            s => s.Order,
            s => new UserWallpaperSlot
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                Type = (int)s.Type,
                WallpaperId = s.Id,
                UseCharacterBackground = s.UseCharacterBackground
            },
            (e, s) =>
            {
                e.Type = (int)s.Type;
                e.WallpaperId = s.Id;
                e.UseCharacterBackground = s.UseCharacterBackground;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserWallpaperBackground>(),
            db.Set<UserWallpaperBackground>().Where(x => x.UserId == userId).ToList(),
            user.WallpaperBackground,
            e => e.OrderIndex,
            s => s.Order,
            s => new UserWallpaperBackground
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                LobbyDecoBackgroundId = s.LobbyDecoBackgroundId
            },
            (e, s) => e.LobbyDecoBackgroundId = s.LobbyDecoBackgroundId);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserWallpaperJukebox>(),
            db.Set<UserWallpaperJukebox>().Where(x => x.UserId == userId).ToList(),
            user.WallpaperJukeboxList,
            e => e.OrderIndex,
            s => s.Order,
            s => new UserWallpaperJukebox
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                JukeboxListTableId = s.JukeboxListTableId
            },
            (e, s) => e.JukeboxListTableId = s.JukeboxListTableId);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserWallpaperFavorite>(),
            db.Set<UserWallpaperFavorite>().Where(x => x.UserId == userId).ToList(),
            user.WallpaperFavoriteList,
            e => e.OrderIndex,
            s => s.Order,
            s => new UserWallpaperFavorite
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                IsShuffled = s.IsShuffled
            },
            (e, s) => e.IsShuffled = s.IsShuffled);

        DomainStoreHelpers.SyncCollection(
            db.Set<UserWallpaperPlaylist>(),
            db.Set<UserWallpaperPlaylist>().Where(x => x.UserId == userId).ToList(),
            user.WallpaperPlaylistList,
            e => e.OrderIndex,
            s => s.Order,
            s => new UserWallpaperPlaylist
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                OrderIndex = s.Order,
                PlaylistUId = s.PlaylistUId,
                IsShuffled = s.IsShuffled
            },
            (e, s) =>
            {
                e.PlaylistUId = s.PlaylistUId;
                e.IsShuffled = s.IsShuffled;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserLobbyPrivateBannerEntity>(),
            db.Set<UserLobbyPrivateBannerEntity>().Where(x => x.UserId == userId).ToList(),
            user.LobbyPrivateBannerIds ?? [],
            e => e.BannerId,
            id => id,
            id => new UserLobbyPrivateBannerEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                BannerId = id
            },
            (_, _) => { });

        var favoriteSongs = user.FavoriteSongs != null
            ? user.FavoriteSongs.Songs.ToList()
            : new List<NetJukeboxPlaylistSong>();
        var desiredPlaylists = (user.PlayLists ?? [])
            .Select(p => (
                IsFavorite: false,
                PlaylistUid: p.JukeboxPlaylistUid,
                Title: p.Title ?? "",
                SongsJson: JsonConvert.SerializeObject(p.Songs.ToList())))
            .Append((
                IsFavorite: true,
                PlaylistUid: 0L,
                Title: "",
                SongsJson: JsonConvert.SerializeObject(favoriteSongs)));

        DomainStoreHelpers.SyncCollection(
            db.Set<UserJukeboxPlaylistEntity>(),
            db.Set<UserJukeboxPlaylistEntity>().Where(x => x.UserId == userId).ToList(),
            desiredPlaylists,
            e => (e.IsFavorite, e.PlaylistUid),
            s => (s.IsFavorite, s.PlaylistUid),
            s => new UserJukeboxPlaylistEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                PlaylistUid = s.PlaylistUid,
                IsFavorite = s.IsFavorite,
                Title = s.Title,
                SongsJson = s.SongsJson
            },
            (e, s) =>
            {
                e.Title = s.Title;
                e.SongsJson = s.SongsJson;
            });
    }
}
