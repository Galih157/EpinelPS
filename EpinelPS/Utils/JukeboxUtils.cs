using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Repositories;

namespace EpinelPS.Utils;

public static class JukeboxUtils
{
    /// <summary>
    /// Songs available to the user: default BGMs plus everything unlocked
    /// (unlocks are granted through RewardType.Bgm rewards, e.g. field items).
    /// </summary>
    public static List<int> GetUnlockedSongs(User user, CosmeticsRepository? repo = null)
        => GetUnlockedSongs(user.ID, repo);

    public static List<int> GetUnlockedSongs(ulong userId, CosmeticsRepository? repo = null)
    {
        if (repo == null)
        {
            using var db = GameContext.CreateNew();
            return GetUnlockedSongs(userId, new CosmeticsRepository(db));
        }

        var owned = repo.GetUnlockedCosmetics(userId, CosmeticsRepository.CategoryJukeboxBgm).ToHashSet();
        var result = new List<int>();
        foreach (var song in GameData.Instance.jukeboxListDataRecords.Values)
        {
            if (song.IsDefaultBgm || owned.Contains(song.Id))
                result.Add(song.Id);
        }
        return result;
    }

    /// <summary>
    /// Number of songs of a theme the user owns (default BGMs included).
    /// </summary>
    public static int CountOwnedSongsByTheme(User user, int theme, CosmeticsRepository? repo = null)
        => CountOwnedSongsByTheme(user.ID, theme, repo);

    public static int CountOwnedSongsByTheme(ulong userId, int theme, CosmeticsRepository? repo = null)
    {
        if (repo == null)
        {
            using var db = GameContext.CreateNew();
            return CountOwnedSongsByTheme(userId, theme, new CosmeticsRepository(db));
        }

        var owned = repo.GetUnlockedCosmetics(userId, CosmeticsRepository.CategoryJukeboxBgm).ToHashSet();
        int count = 0;
        foreach (var song in GameData.Instance.jukeboxListDataRecords.Values)
        {
            if (song.Theme != theme)
                continue;
            if (song.IsDefaultBgm || owned.Contains(song.Id))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Builds the client's view of the current BGM setting for a location.
    /// </summary>
    public static NetJukeboxBgm BuildCurrentBgm(User user, NetJukeboxLocation location, CosmeticsRepository? repo = null)
        => BuildCurrentBgm(user.ID, location, repo);

    public static NetJukeboxBgm BuildCurrentBgm(ulong userId, NetJukeboxLocation location, CosmeticsRepository? repo = null)
    {
        if (repo == null)
        {
            using var db = GameContext.CreateNew();
            return BuildCurrentBgm(userId, location, new CosmeticsRepository(db));
        }

        var setting = repo.GetJukeboxSetting(userId, location);
        var bgm = new NetJukeboxBgm { Location = location, Type = setting.Type, IsShuffle = setting.IsShuffle };
        switch (setting.Type)
        {
            case NetJukeboxBgmType.JukeboxPlaylist:
                var playlist = repo.GetPlaylist(userId, setting.TableId);
                if (playlist != null)
                {
                    bgm.JukeboxPlaylist = playlist;
                }
                else
                {
                    bgm.Type = NetJukeboxBgmType.JukeboxTableId;
                    bgm.JukeboxTableId = setting.TableId;
                }
                break;
            case NetJukeboxBgmType.JukeboxFavorite:
                bgm.JukeboxFavorite = repo.GetFavoriteSongs(userId);
                break;
            default:
                bgm.JukeboxTableId = setting.TableId;
                break;
        }
        return bgm;
    }
}
