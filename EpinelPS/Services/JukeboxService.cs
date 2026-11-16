using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;

namespace EpinelPS.Services;

public sealed class JukeboxService(CosmeticsRepository repo, GameContext db)
{
    public ResGetJukeboxPlaylist GetPlaylist(ulong userId)
    {
        var response = new ResGetJukeboxPlaylist
        {
            FavoriteSongs = repo.GetFavoriteSongs(userId)
        };
        response.Playlists.AddRange(repo.GetPlaylists(userId));
        return response;
    }

    public ResRecordJukeboxPlayHistory RecordPlayHistory()
        => new();

    public ResAddJukeboxFavoriteSong AddFavoriteSong(ulong userId, ReqAddJukeboxFavoriteSong req)
    {
        var favoriteSongs = repo.GetFavoriteSongs(userId);
        if (!favoriteSongs.Songs.Any(s => s.JukeboxTableId == req.JukeboxTableId))
        {
            favoriteSongs.Songs.Add(new NetJukeboxPlaylistSong
            {
                JukeboxTableId = req.JukeboxTableId,
                Order = favoriteSongs.Songs.Count == 0 ? 1 : favoriteSongs.Songs.Max(s => s.Order) + 1
            });
            repo.SaveFavoriteSongs(userId, favoriteSongs.Songs);
            db.SaveChanges();
        }

        return new ResAddJukeboxFavoriteSong { FavoriteSongs = favoriteSongs };
    }

    public ResSetJukeboxFavoriteSongs SetFavoriteSongs(ulong userId, ReqSetJukeboxFavoriteSongs req)
    {
        repo.SaveFavoriteSongs(userId, req.Songs);
        db.SaveChanges();

        var favoriteSongs = new NetJukeboxFavorite();
        favoriteSongs.Songs.AddRange(req.Songs);
        var response = new ResSetJukeboxFavoriteSongs { FavoriteSongs = favoriteSongs };

        foreach (var location in new[] { NetJukeboxLocation.Lobby, NetJukeboxLocation.CommanderRoom })
        {
            var setting = repo.GetJukeboxSetting(userId, location);
            if (setting.Type == NetJukeboxBgmType.JukeboxFavorite)
            {
                response.JukeboxBgm.Add(JukeboxUtils.BuildCurrentBgm(userId, location, repo));
                break;
            }
        }

        return response;
    }

    public ResAddSongJukeboxPlaylist AddSongToPlaylist(ulong userId, ReqAddSongJukeboxPlaylist req)
    {
        var response = new ResAddSongJukeboxPlaylist();
        var playlist = repo.GetPlaylist(userId, req.JukeboxPlaylistUid);
        if (playlist != null && !playlist.Songs.Any(s => s.JukeboxTableId == req.JukeboxTableId))
        {
            playlist.Songs.Add(new NetJukeboxPlaylistSong
            {
                JukeboxTableId = req.JukeboxTableId,
                Order = playlist.Songs.Count == 0 ? 1 : playlist.Songs.Max(s => s.Order) + 1
            });
            repo.SavePlaylist(userId, playlist);
            db.SaveChanges();
            response.Playlist = playlist;
        }
        return response;
    }

    public ResCreateJukeboxPlaylist CreatePlaylist(ulong userId, ReqCreateJukeboxPlaylist req)
    {
        var playlist = new NetJukeboxPlaylist
        {
            JukeboxPlaylistUid = repo.GetNextPlaylistUid(userId),
            Title = req.Title
        };
        repo.SavePlaylist(userId, playlist);
        db.SaveChanges();
        return new ResCreateJukeboxPlaylist { Playlist = playlist };
    }

    public ResDeleteJukeboxPlaylist DeletePlaylist(ulong userId, ReqDeleteJukeboxPlaylist req)
    {
        var response = new ResDeleteJukeboxPlaylist();
        if (repo.DeletePlaylist(userId, req.JukeboxPlaylistUid))
        {
            var resetLocations = new List<NetJukeboxLocation>();
            foreach (var location in new[] { NetJukeboxLocation.Lobby, NetJukeboxLocation.CommanderRoom })
            {
                var setting = repo.GetJukeboxSetting(userId, location);
                if (setting.Type == NetJukeboxBgmType.JukeboxPlaylist && setting.TableId == req.JukeboxPlaylistUid)
                {
                    setting.Type = NetJukeboxBgmType.JukeboxTableId;
                    setting.TableId = location == NetJukeboxLocation.Lobby ? 2 : 5;
                    setting.IsShuffle = false;
                    repo.SetJukeboxSetting(userId, setting);
                    resetLocations.Add(location);
                }
            }

            db.SaveChanges();

            foreach (var location in resetLocations)
            {
                response.JukeboxBgm.Add(JukeboxUtils.BuildCurrentBgm(userId, location, repo));
            }
        }
        return response;
    }

    public ResSetJukeboxPlaylist SetPlaylist(ulong userId, ReqSetJukeboxPlaylist req)
    {
        var response = new ResSetJukeboxPlaylist();
        var playlist = repo.GetPlaylist(userId, req.JukeboxPlaylistUid);
        if (playlist != null)
        {
            playlist.Songs.Clear();
            playlist.Songs.AddRange(req.Songs);
            repo.SavePlaylist(userId, playlist);
            db.SaveChanges();
            response.Playlist = playlist;

            foreach (var location in new[] { NetJukeboxLocation.Lobby, NetJukeboxLocation.CommanderRoom })
            {
                var setting = repo.GetJukeboxSetting(userId, location);
                if (setting.Type == NetJukeboxBgmType.JukeboxPlaylist && setting.TableId == req.JukeboxPlaylistUid)
                {
                    response.JukeboxBgm.Add(JukeboxUtils.BuildCurrentBgm(userId, location, repo));
                    break;
                }
            }
        }
        return response;
    }

    public ResSetJukeboxPlaylistTitle SetPlaylistTitle(ulong userId, ReqSetJukeboxPlaylistTitle req)
    {
        var response = new ResSetJukeboxPlaylistTitle();
        var playlist = repo.GetPlaylist(userId, req.JukeboxPlaylistUid);
        if (playlist != null)
        {
            playlist.Title = req.Title;
            repo.SavePlaylist(userId, playlist);
            db.SaveChanges();
            response.Playlist = playlist;
        }
        return response;
    }

    public ResSetJukeboxBgmFavorite SetBgmFavorite(ulong userId, ReqSetJukeboxBgmFavorite req)
    {
        var setting = repo.GetJukeboxSetting(userId, req.Location);
        setting.Type = NetJukeboxBgmType.JukeboxFavorite;
        setting.IsShuffle = req.IsShuffle;
        repo.SetJukeboxSetting(userId, setting);
        db.SaveChanges();
        return new ResSetJukeboxBgmFavorite();
    }

    public ResSetJukeboxBgmPlaylist SetBgmPlaylist(ulong userId, ReqSetJukeboxBgmPlaylist req)
    {
        if (repo.GetPlaylist(userId, req.JukeboxPlaylistUid) != null)
        {
            var setting = repo.GetJukeboxSetting(userId, req.Location);
            setting.Type = NetJukeboxBgmType.JukeboxPlaylist;
            setting.TableId = (int)req.JukeboxPlaylistUid;
            setting.IsShuffle = req.IsShuffle;
            repo.SetJukeboxSetting(userId, setting);
            db.SaveChanges();
        }
        return new ResSetJukeboxBgmPlaylist();
    }

    public ResSetJukeboxBgmTableId SetTableId(ulong userId, ReqSetJukeboxBgmTableId req)
    {
        if (req.Location is NetJukeboxLocation.CommanderRoom or NetJukeboxLocation.Lobby)
        {
            var setting = repo.GetJukeboxSetting(userId, req.Location);
            setting.TableId = req.JukeboxTableId;
            setting.Type = NetJukeboxBgmType.JukeboxTableId;
            repo.SetJukeboxSetting(userId, setting);
            db.SaveChanges();
        }
        return new ResSetJukeboxBgmTableId();
    }

    public ResGetJukeboxRewardedData GetJukeboxRewards(ulong userId)
    {
        var response = new ResGetJukeboxRewardedData();
        response.JukeboxMissionTidList.AddRange(repo.GetClaimedJukeboxRewards(userId));
        return response;
    }

    public ResObtainTriggerRewardForJukebox ObtainTriggerRewardForJukebox(User user, ReqObtainTriggerRewardForJukebox req)
    {
        var response = new ResObtainTriggerRewardForJukebox();
        List<NetRewardData> rewards = [];
        var claimed = repo.GetClaimedJukeboxRewards(user.ID).ToHashSet();
        bool anyClaimed = false;

        foreach (int tid in req.TidList.Distinct())
        {
            if (claimed.Contains(tid))
                continue;
            if (!GameData.Instance.TriggerTable.TryGetValue(tid, out var trigger))
                continue;
            if (trigger.Trigger != Trigger.ObtainJukeboxTheme)
                continue;
            if (JukeboxUtils.CountOwnedSongsByTheme(user.ID, trigger.ConditionId, repo) < trigger.ConditionValue)
                continue;

            RewardRecord? reward = GameData.Instance.GetRewardTableEntry(trigger.RewardId);
            if (reward == null)
                continue;

            rewards.Add(RewardUtils.RegisterRewardsForUser(user, reward));
            if (repo.ClaimJukeboxReward(user.ID, tid))
                anyClaimed = true;
            claimed.Add(tid);
        }

        response.Reward = NetUtils.MergeRewards(rewards, user);

        if (anyClaimed)
        {
            db.SaveChanges();
            UserSessionStore.SaveUser(user);
        }

        return response;
    }
}
