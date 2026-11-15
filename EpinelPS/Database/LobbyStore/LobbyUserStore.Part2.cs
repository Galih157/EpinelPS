using EpinelPS.Models;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database;

public static partial class LobbyUserStore
{
    private const string CosmeticCostume = "Costume";
    private const string CosmeticFrame = "Frame";
    private const string CosmeticTitle = "Title";
    private const string CosmeticProfileCard = "ProfileCard";
    private const string CosmeticMemorial = "Memorial";

    private const string BondKindFlushableWatchedDialog = "FlushableWatchedDialog";
    private const string BondKindObtainedRewardLevel = "ObtainedRewardLevel";
    private const string BondKindCounselDialogComplete = "CounselDialogComplete";

    private const string ArchiveKindRecord = "Record";
    private const string ArchiveKindEventQuest = "EventQuest";

    private static void LoadPart2(GameContext db, User user)
    {
        ulong userId = user.ID;

        // Group 7: Profile, cosmetics, wallpapers, and lobby music
        var profileDeco = db.UserProfileCardDecorations.AsNoTracking().FirstOrDefault(x => x.UserId == userId);
        if (profileDeco != null)
        {
            user.ProfileCardDecoration.ShowCharacterSpine = profileDeco.ShowCharacterSpine;
            user.ProfileCardDecoration.BackgroundId = profileDeco.BackgroundId;
        }

        var stickerRows = db.UserProfileCardStickers.AsNoTracking()
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

        var cosmetics = db.UserUnlockedCosmetics.AsNoTracking().Where(x => x.UserId == userId).ToList();
        user.CostumeList = cosmetics.Where(x => x.Category == CosmeticCostume).Select(x => x.CosmeticId).ToList();
        user.FrameList = cosmetics.Where(x => x.Category == CosmeticFrame).Select(x => x.CosmeticId).ToList();
        user.TitleList = cosmetics.Where(x => x.Category == CosmeticTitle).Select(x => x.CosmeticId).ToList();
        user.ProfileCardsData = cosmetics.Where(x => x.Category == CosmeticProfileCard).Select(x => x.CosmeticId).ToList();
        user.Memorial = cosmetics.Where(x => x.Category == CosmeticMemorial).Select(x => x.CosmeticId).ToList();

        var jukeboxRows = db.UserJukeboxSettings.AsNoTracking().Where(x => x.UserId == userId).ToList();
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

        user.ClaimedJukeboxRewardTriggers = db.UserClaimedJukeboxRewards.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.TriggerId)
            .ToList();

        user.WallpaperList = db.UserWallpaperSlots.AsNoTracking()
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

        user.WallpaperBackground = db.UserWallpaperBackgrounds.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperBackground
            {
                Order = x.OrderIndex,
                LobbyDecoBackgroundId = x.LobbyDecoBackgroundId
            })
            .ToArray();

        user.WallpaperJukeboxList = db.UserWallpaperJukeboxes.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperJukebox
            {
                Order = x.OrderIndex,
                JukeboxListTableId = x.JukeboxListTableId
            })
            .ToArray();

        user.WallpaperFavoriteList = db.UserWallpaperFavorites.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperJukeboxFavorite
            {
                Order = x.OrderIndex,
                IsShuffled = x.IsShuffled
            })
            .ToArray();

        user.WallpaperPlaylistList = db.UserWallpaperPlaylists.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new NetWallpaperPlaylist
            {
                Order = x.OrderIndex,
                PlaylistUId = x.PlaylistUId,
                IsShuffled = x.IsShuffled
            })
            .ToArray();

        // Group 8: Gacha, normal shop, and passes
        user.GachaBannerMaxPulls = db.UserGachaBannerPulls.AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.BannerId, x => x.MaxPulls);

        user.GachaPityBannerExecuteCount = db.UserGachaPityCounts.AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.BannerId, x => x.ExecuteCount);

        user.GachaSelectupChoices = db.UserGachaSelectupChoices.AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionary(x => x.BannerId, x => x.CharacterTid);

        user.GachaDailyFreePulls = db.UserGachaDailyFreePulls.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.GachaId)
            .ToList();

        user.GachaPaybackData = db.UserGachaPaybacks.AsNoTracking()
            .Include(x => x.Rewards)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.GachaId,
                x => new GachaPaybackData
                {
                    GachaId = x.GachaId,
                    GachaCount = x.GachaCount,
                    RewardedStepList = x.Rewards.Select(r => r.StepId).ToList()
                });

        var shopRows = db.UserNormalShopStates.AsNoTracking()
            .Include(x => x.Orders)
            .Include(x => x.PinnedBuys)
            .Where(x => x.UserId == userId)
            .ToList();
        user.NormalShopStates = new Dictionary<int, NormalShopState>(shopRows.Count);
        foreach (var row in shopRows)
        {
            var state = new NormalShopState
            {
                RenewCount = row.RenewCount,
                RenewAt = row.RenewAt,
                NextRenewAt = row.NextRenewAt,
                ProductIdsByOrder = row.Orders.ToDictionary(o => o.OrderIndex, o => o.ProductId),
                BuyCountsByOrder = row.Orders.ToDictionary(o => o.OrderIndex, o => o.BuyCount),
                PinnedBuyCounts = row.PinnedBuys.ToDictionary(p => p.ProductId, p => p.BuyCount)
            };
            user.NormalShopStates[row.ShopCategory] = state;
        }

        user.UserPassInfo = db.UserPasses.AsNoTracking()
            .Include(x => x.Ranks)
            .Include(x => x.Missions)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.PassId,
                x => new PassData
                {
                    PassId = x.PassId,
                    PassPoint = x.PassPoint,
                    PassSkipCount = x.PassSkipCount,
                    PremiumActive = x.PremiumActive,
                    LastCompleteAt = x.LastCompleteAt ?? "",
                    PassRankList = x.Ranks.Select(r => new PassRankData
                    {
                        PassRank = r.PassRank,
                        IsNormalRewarded = r.IsNormalRewarded,
                        IsPremiumRewarded = r.IsPremiumRewarded
                    }).ToList(),
                    PassMissionList = x.Missions.Select(m => new PassMissionData
                    {
                        PassMissionId = m.PassMissionId,
                        IsComplete = m.IsComplete
                    }).ToList()
                });

        // Group 9: Events and login events
        user.EventInfo = db.UserEvents.AsNoTracking()
            .Include(x => x.CompletedScenarios)
            .Include(x => x.ClearedStages)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.EventId,
                x => new EventData
                {
                    Diff = x.Diff,
                    LastStage = x.LastStage,
                    LastDay = x.LastDay,
                    FreeTicket = x.FreeTicket,
                    CompletedScenarios = x.CompletedScenarios.Select(s => s.ScenarioId).ToList(),
                    ClearedStages = x.ClearedStages.Select(s => s.StageId).ToList()
                });

        user.LoginEventInfo = db.UserLoginEvents.AsNoTracking()
            .Include(x => x.Days)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.EventId,
                x => new LoginEventData
                {
                    LastDay = x.LastDay,
                    LastDate = x.LastDate,
                    Days = x.Days.Select(d => d.Day).ToList()
                });

        user.EventMissionInfo = db.UserEventMissions.AsNoTracking()
            .Include(x => x.Entries)
            .Where(x => x.UserId == userId)
            .ToDictionary(
                x => x.EventId,
                x => new EventMissionData
                {
                    LastDay = x.LastDay,
                    LastDate = x.LastDate,
                    AllClear = x.AllClear,
                    MissionIdList = x.Entries.Where(e => !e.IsDaily).Select(e => e.MissionId).ToList(),
                    DailyMissionIdList = x.Entries.Where(e => e.IsDaily).Select(e => e.MissionId).ToList()
                });

        user.EventShopBuyCountInfo = db.UserEventShopBuys.AsNoTracking()
            .Where(x => x.UserId == userId)
            .AsEnumerable()
            .GroupBy(x => x.EventId)
            .ToDictionary(
                g => g.Key,
                g => new EventShopBuyCountData
                {
                    EventId = g.Key,
                    datas = g.Select(p => new EventShopProductData
                    {
                        ProductTid = p.ProductTid,
                        BuyCount = p.BuyCount
                    }).ToList()
                });

        // Group 10: Bonds, favorite items, and liberation
        var bondRows = db.UserBonds.AsNoTracking()
            .Include(x => x.ProgressItems)
            .Where(x => x.UserId == userId)
            .ToList();
        user.BondInfo = bondRows.Select(b =>
        {
            var data = new NetUserAttractiveData
            {
                NameCode = b.NameCode,
                Lv = b.Lv,
                Exp = b.Exp,
                CanCounselToday = b.CanCounselToday,
                IsFavorites = b.IsFavorites,
                CounseledCount = b.CounseledCount,
                CompleteRewardStatus = (CounselDialogCompleteRewardStatus)b.CompleteRewardStatus
            };
            data.FlushableWatchedDialogIds.AddRange(b.ProgressItems.Where(p => p.Kind == BondKindFlushableWatchedDialog).Select(p => p.ItemId));
            data.ObtainedRewardLevels.AddRange(b.ProgressItems.Where(p => p.Kind == BondKindObtainedRewardLevel).Select(p => p.ItemId));
            data.CounselDialogCompleteIds.AddRange(b.ProgressItems.Where(p => p.Kind == BondKindCounselDialogComplete).Select(p => p.ItemId));
            return data;
        }).ToList();

        user.FavoriteItems = db.UserFavoriteItems.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetUserFavoriteItemData
            {
                FavoriteItemId = x.FavoriteItemId,
                Tid = x.Tid,
                Csn = x.Csn,
                Lv = x.Lv,
                Exp = x.Exp
            })
            .ToList();

        user.FavoriteItemQuests = db.UserFavoriteItemQuests.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetUserFavoriteItemQuestData
            {
                QuestId = x.QuestId,
                Clear = x.Clear,
                Received = x.Received
            })
            .ToList();

        var liberationRows = db.UserLiberations.AsNoTracking()
            .Include(x => x.Missions)
            .Where(x => x.UserId == userId)
            .ToList();
        user.LiberateDatas = new Dictionary<int, NetLiberateData>(liberationRows.Count);
        foreach (var row in liberationRows)
        {
            var data = new NetLiberateData
            {
                CharacterId = row.CharacterId,
                Step = row.Step,
                ProgressPoint = row.ProgressPoint,
                RewardedCount = row.RewardedCount,
                IsCompleted = row.IsCompleted
            };
            data.MissionData.AddRange(row.Missions.Select(m => new NetLiberateMissionData
            {
                Id = m.MissionId,
                MissionTid = m.MissionTid,
                LiberateCharacterId = m.LiberateCharacterId,
                MissionState = (LiberateMissionState)m.MissionState,
                CreatedAt = m.CreatedAt,
                TriggerStartAt = m.TriggerStartAt,
                TriggerEndAt = m.TriggerEndAt,
                ReceivedAt = m.ReceivedAt
            }));
            user.LiberateDatas[row.CharacterId] = data;
        }

        user.OpenLiberateTypeIdList = db.UserOpenLiberateTypes.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.LiberateTypeId)
            .ToList();

        // Group 11: Messenger and badges
        user.MessengerData = db.UserMessages.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Seq)
            .Select(x => new NetMessage
            {
                Seq = x.Seq,
                ConversationId = x.ConversationId,
                MessageId = x.MessageId,
                CreatedAt = x.CreatedAt,
                State = x.State
            })
            .ToList();

        user.PickedMessages = db.UserPickedMessages.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new NetPickedMessage
            {
                ConversationId = x.ConversationId,
                CreatedAt = new Timestamp
                {
                    Seconds = x.CreatedAtSeconds,
                    Nanos = x.CreatedAtNanos
                }
            })
            .ToList();

        user.Badges = db.UserBadges.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Seq)
            .Select(x => new BadgeModel
            {
                Seq = x.Seq,
                Location = x.Location,
                BadgeContent = (BadgeContents)x.BadgeContent,
                BadgeGuid = x.BadgeGuid
            })
            .ToList();

        // Group 12: Achievements, side story, archive, and Lost Sector
        user.CompletedAchievements = db.UserCompletedAchievements.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.AchievementId)
            .ToList();

        var sideStoryRows = db.UserSideStoryStages.AsNoTracking().Where(x => x.UserId == userId).ToList();
        user.CompletedSideStoryStages = sideStoryRows.Where(x => x.IsCompleted).Select(x => x.StageId).ToList();
        user.ViewedSideStoryStages = sideStoryRows.Where(x => x.IsViewed).Select(x => x.StageId).ToList();

        var archiveRows = db.UserArchiveUnlocks.AsNoTracking().Where(x => x.UserId == userId).ToList();
        user.UnlockedArchiveRecordIds = archiveRows.Where(x => x.Kind == ArchiveKindRecord).Select(x => x.TargetId).ToList();
        user.UnlockedArchiveEventQuestIds = archiveRows.Where(x => x.Kind == ArchiveKindEventQuest).Select(x => x.TargetId).ToList();

        var lostSectorRows = db.UserLostSectors.AsNoTracking()
            .Include(x => x.ClearedStages)
            .Include(x => x.Objects)
            .Where(x => x.UserId == userId)
            .ToList();
        user.LostSectorData = new Dictionary<int, LostSectorData>(lostSectorRows.Count);
        foreach (var row in lostSectorRows)
        {
            var teamPositions = string.IsNullOrEmpty(row.TeamPositionsJson)
                ? []
                : JsonConvert.DeserializeObject<List<NetLostSectorTeamPosition>>(row.TeamPositionsJson) ?? [];

            var objects = new Dictionary<string, NetLostSectorFieldObject>(row.Objects.Count);
            foreach (var objRow in row.Objects)
            {
                var fieldObj = new NetLostSectorFieldObject
                {
                    ActionAt = objRow.ActionAt,
                    Type = objRow.Type,
                    PositionId = objRow.PositionId ?? "",
                    Json = objRow.Json ?? ""
                };
                var objTeamPositions = string.IsNullOrEmpty(objRow.TeamPositionsJson)
                    ? null
                    : JsonConvert.DeserializeObject<List<NetLostSectorTeamPosition>>(objRow.TeamPositionsJson);
                if (objTeamPositions != null)
                    fieldObj.TeamPosition.AddRange(objTeamPositions);

                objects[objRow.ObjectKey] = fieldObj;
            }

            user.LostSectorData[row.SectorId] = new LostSectorData
            {
                IsOpen = row.IsOpen,
                IsPlaying = row.IsPlaying,
                Json = row.StateJson ?? "",
                TeamPositions = teamPositions,
                ObtainedRewards = row.ObtainedRewards,
                ReceivedFinalReward = row.ReceivedFinalReward,
                CompletedPerfectly = row.CompletedPerfectly,
                ClearedStages = row.ClearedStages.ToDictionary(s => s.StageKey, s => s.ClearState),
                Objects = objects
            };
        }
    }

    private static void SavePart2(GameContext db, User user)
    {
        ulong userId = user.ID;

        // Group 7: Profile, cosmetics, wallpapers, and lobby music
        var existingDeco = db.UserProfileCardDecorations.FirstOrDefault(x => x.UserId == userId);
        if (existingDeco == null)
        {
            db.UserProfileCardDecorations.Add(new UserProfileCardDecoration
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

        SyncCollection(
            db.UserProfileCardStickers,
            db.UserProfileCardStickers.Where(x => x.UserId == userId).ToList(),
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

        var desiredCosmetics = user.CostumeList.Select(id => (Category: CosmeticCostume, CosmeticId: id))
            .Concat(user.FrameList.Select(id => (Category: CosmeticFrame, CosmeticId: id)))
            .Concat(user.TitleList.Select(id => (Category: CosmeticTitle, CosmeticId: id)))
            .Concat(user.ProfileCardsData.Select(id => (Category: CosmeticProfileCard, CosmeticId: id)))
            .Concat(user.Memorial.Select(id => (Category: CosmeticMemorial, CosmeticId: id)));
        SyncCollection(
            db.UserUnlockedCosmetics,
            db.UserUnlockedCosmetics.Where(x => x.UserId == userId).ToList(),
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
        SyncCollection(
            db.UserJukeboxSettings,
            db.UserJukeboxSettings.Where(x => x.UserId == userId).ToList(),
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

        SyncCollection(
            db.UserClaimedJukeboxRewards,
            db.UserClaimedJukeboxRewards.Where(x => x.UserId == userId).ToList(),
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

        SyncCollection(
            db.UserWallpaperSlots,
            db.UserWallpaperSlots.Where(x => x.UserId == userId).ToList(),
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

        SyncCollection(
            db.UserWallpaperBackgrounds,
            db.UserWallpaperBackgrounds.Where(x => x.UserId == userId).ToList(),
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

        SyncCollection(
            db.UserWallpaperJukeboxes,
            db.UserWallpaperJukeboxes.Where(x => x.UserId == userId).ToList(),
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

        SyncCollection(
            db.UserWallpaperFavorites,
            db.UserWallpaperFavorites.Where(x => x.UserId == userId).ToList(),
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

        SyncCollection(
            db.UserWallpaperPlaylists,
            db.UserWallpaperPlaylists.Where(x => x.UserId == userId).ToList(),
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

        // Group 8: Gacha, normal shop, and passes
        SyncCollection(
            db.UserGachaBannerPulls,
            db.UserGachaBannerPulls.Where(x => x.UserId == userId).ToList(),
            user.GachaBannerMaxPulls,
            e => e.BannerId,
            kvp => kvp.Key,
            kvp => new UserGachaBannerPull
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                BannerId = kvp.Key,
                MaxPulls = kvp.Value
            },
            (e, kvp) => e.MaxPulls = kvp.Value);

        SyncCollection(
            db.UserGachaPityCounts,
            db.UserGachaPityCounts.Where(x => x.UserId == userId).ToList(),
            user.GachaPityBannerExecuteCount,
            e => e.BannerId,
            kvp => kvp.Key,
            kvp => new UserGachaPityCount
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                BannerId = kvp.Key,
                ExecuteCount = kvp.Value
            },
            (e, kvp) => e.ExecuteCount = kvp.Value);

        SyncCollection(
            db.UserGachaSelectupChoices,
            db.UserGachaSelectupChoices.Where(x => x.UserId == userId).ToList(),
            user.GachaSelectupChoices,
            e => e.BannerId,
            kvp => kvp.Key,
            kvp => new UserGachaSelectupChoice
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                BannerId = kvp.Key,
                CharacterTid = kvp.Value
            },
            (e, kvp) => e.CharacterTid = kvp.Value);

        SyncCollection(
            db.UserGachaDailyFreePulls,
            db.UserGachaDailyFreePulls.Where(x => x.UserId == userId).ToList(),
            user.GachaDailyFreePulls,
            e => e.GachaId,
            id => id,
            id => new UserGachaDailyFreePull
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                GachaId = id
            },
            (_, _) => { });

        SyncCollection(
            db.UserGachaPaybacks,
            db.UserGachaPaybacks.Include(x => x.Rewards).Where(x => x.UserId == userId).ToList(),
            user.GachaPaybackData,
            e => e.GachaId,
            kvp => kvp.Key,
            kvp =>
            {
                var payback = new UserGachaPayback
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    GachaId = kvp.Key,
                    GachaCount = kvp.Value.GachaCount
                };
                foreach (int stepId in kvp.Value.RewardedStepList.Distinct())
                {
                    db.UserGachaPaybackRewards.Add(new UserGachaPaybackReward
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        PaybackId = payback.Id,
                        StepId = stepId
                    });
                }
                return payback;
            },
            (e, kvp) =>
            {
                e.GachaCount = kvp.Value.GachaCount;
                SyncCollection(
                    db.UserGachaPaybackRewards,
                    e.Rewards,
                    kvp.Value.RewardedStepList,
                    r => r.StepId,
                    stepId => stepId,
                    stepId => new UserGachaPaybackReward
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        PaybackId = e.Id,
                        StepId = stepId
                    },
                    (_, _) => { });
            });

        SyncCollection(
            db.UserNormalShopStates,
            db.UserNormalShopStates.Include(x => x.Orders).Include(x => x.PinnedBuys).Where(x => x.UserId == userId).ToList(),
            user.NormalShopStates,
            e => e.ShopCategory,
            kvp => kvp.Key,
            kvp =>
            {
                var shopState = new UserNormalShopState
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    ShopCategory = kvp.Key,
                    RenewCount = kvp.Value.RenewCount,
                    RenewAt = kvp.Value.RenewAt,
                    NextRenewAt = kvp.Value.NextRenewAt
                };
                foreach (int orderIndex in kvp.Value.ProductIdsByOrder.Keys.Union(kvp.Value.BuyCountsByOrder.Keys))
                {
                    db.UserNormalShopOrders.Add(new UserNormalShopOrder
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        ShopStateId = shopState.Id,
                        OrderIndex = orderIndex,
                        ProductId = kvp.Value.ProductIdsByOrder.GetValueOrDefault(orderIndex, 0),
                        BuyCount = kvp.Value.BuyCountsByOrder.GetValueOrDefault(orderIndex, 0)
                    });
                }
                foreach (var pinned in kvp.Value.PinnedBuyCounts)
                {
                    db.UserNormalShopPinnedBuys.Add(new UserNormalShopPinnedBuy
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        ShopStateId = shopState.Id,
                        ProductId = pinned.Key,
                        BuyCount = pinned.Value
                    });
                }
                return shopState;
            },
            (e, kvp) =>
            {
                e.RenewCount = kvp.Value.RenewCount;
                e.RenewAt = kvp.Value.RenewAt;
                e.NextRenewAt = kvp.Value.NextRenewAt;

                var orderIndices = kvp.Value.ProductIdsByOrder.Keys.Union(kvp.Value.BuyCountsByOrder.Keys);
                SyncCollection(
                    db.UserNormalShopOrders,
                    e.Orders,
                    orderIndices,
                    o => o.OrderIndex,
                    idx => idx,
                    idx => new UserNormalShopOrder
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        ShopStateId = e.Id,
                        OrderIndex = idx,
                        ProductId = kvp.Value.ProductIdsByOrder.GetValueOrDefault(idx, 0),
                        BuyCount = kvp.Value.BuyCountsByOrder.GetValueOrDefault(idx, 0)
                    },
                    (o, idx) =>
                    {
                        o.ProductId = kvp.Value.ProductIdsByOrder.GetValueOrDefault(idx, 0);
                        o.BuyCount = kvp.Value.BuyCountsByOrder.GetValueOrDefault(idx, 0);
                    });

                SyncCollection(
                    db.UserNormalShopPinnedBuys,
                    e.PinnedBuys,
                    kvp.Value.PinnedBuyCounts,
                    p => p.ProductId,
                    p => p.Key,
                    p => new UserNormalShopPinnedBuy
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        ShopStateId = e.Id,
                        ProductId = p.Key,
                        BuyCount = p.Value
                    },
                    (p, src) => p.BuyCount = src.Value);
            });

        SyncCollection(
            db.UserPasses,
            db.UserPasses.Include(x => x.Ranks).Include(x => x.Missions).Where(x => x.UserId == userId).ToList(),
            user.UserPassInfo,
            e => e.PassId,
            kvp => kvp.Key,
            kvp =>
            {
                var pass = new UserPass
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    PassId = kvp.Key,
                    PassPoint = kvp.Value.PassPoint,
                    PassSkipCount = kvp.Value.PassSkipCount,
                    PremiumActive = kvp.Value.PremiumActive,
                    LastCompleteAt = kvp.Value.LastCompleteAt ?? ""
                };
                foreach (var rank in kvp.Value.PassRankList.GroupBy(r => r.PassRank).Select(g => g.Last()))
                {
                    db.UserPassRanks.Add(new UserPassRank
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserPassId = pass.Id,
                        PassRank = rank.PassRank,
                        IsNormalRewarded = rank.IsNormalRewarded,
                        IsPremiumRewarded = rank.IsPremiumRewarded
                    });
                }
                foreach (var mission in kvp.Value.PassMissionList.GroupBy(m => m.PassMissionId).Select(g => g.Last()))
                {
                    db.UserPassMissions.Add(new UserPassMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserPassId = pass.Id,
                        PassMissionId = mission.PassMissionId,
                        IsComplete = mission.IsComplete
                    });
                }
                return pass;
            },
            (e, kvp) =>
            {
                e.PassPoint = kvp.Value.PassPoint;
                e.PassSkipCount = kvp.Value.PassSkipCount;
                e.PremiumActive = kvp.Value.PremiumActive;
                e.LastCompleteAt = kvp.Value.LastCompleteAt ?? "";

                SyncCollection(
                    db.UserPassRanks,
                    e.Ranks,
                    kvp.Value.PassRankList,
                    r => r.PassRank,
                    r => r.PassRank,
                    r => new UserPassRank
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserPassId = e.Id,
                        PassRank = r.PassRank,
                        IsNormalRewarded = r.IsNormalRewarded,
                        IsPremiumRewarded = r.IsPremiumRewarded
                    },
                    (r, src) =>
                    {
                        r.IsNormalRewarded = src.IsNormalRewarded;
                        r.IsPremiumRewarded = src.IsPremiumRewarded;
                    });

                SyncCollection(
                    db.UserPassMissions,
                    e.Missions,
                    kvp.Value.PassMissionList,
                    m => m.PassMissionId,
                    m => m.PassMissionId,
                    m => new UserPassMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserPassId = e.Id,
                        PassMissionId = m.PassMissionId,
                        IsComplete = m.IsComplete
                    },
                    (m, src) => m.IsComplete = src.IsComplete);
            });

        // Group 9: Events and login events
        SyncCollection(
            db.UserEvents,
            db.UserEvents.Include(x => x.CompletedScenarios).Include(x => x.ClearedStages).Where(x => x.UserId == userId).ToList(),
            user.EventInfo,
            e => e.EventId,
            kvp => kvp.Key,
            kvp =>
            {
                var ev = new UserEvent
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    EventId = kvp.Key,
                    Diff = kvp.Value.Diff,
                    LastStage = kvp.Value.LastStage,
                    LastDay = kvp.Value.LastDay,
                    FreeTicket = kvp.Value.FreeTicket
                };
                foreach (string scenarioId in kvp.Value.CompletedScenarios.Distinct())
                {
                    db.UserEventCompletedScenarios.Add(new UserEventCompletedScenario
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventId = ev.Id,
                        ScenarioId = scenarioId
                    });
                }
                foreach (int stageId in kvp.Value.ClearedStages.Distinct())
                {
                    db.UserEventClearedStages.Add(new UserEventClearedStage
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventId = ev.Id,
                        StageId = stageId
                    });
                }
                return ev;
            },
            (e, kvp) =>
            {
                e.Diff = kvp.Value.Diff;
                e.LastStage = kvp.Value.LastStage;
                e.LastDay = kvp.Value.LastDay;
                e.FreeTicket = kvp.Value.FreeTicket;

                SyncCollection(
                    db.UserEventCompletedScenarios,
                    e.CompletedScenarios,
                    kvp.Value.CompletedScenarios,
                    s => s.ScenarioId,
                    id => id,
                    id => new UserEventCompletedScenario
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventId = e.Id,
                        ScenarioId = id
                    },
                    (_, _) => { });

                SyncCollection(
                    db.UserEventClearedStages,
                    e.ClearedStages,
                    kvp.Value.ClearedStages,
                    s => s.StageId,
                    id => id,
                    id => new UserEventClearedStage
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventId = e.Id,
                        StageId = id
                    },
                    (_, _) => { });
            });

        SyncCollection(
            db.UserLoginEvents,
            db.UserLoginEvents.Include(x => x.Days).Where(x => x.UserId == userId).ToList(),
            user.LoginEventInfo,
            e => e.EventId,
            kvp => kvp.Key,
            kvp =>
            {
                var ev = new UserLoginEvent
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    EventId = kvp.Key,
                    LastDay = kvp.Value.LastDay,
                    LastDate = kvp.Value.LastDate
                };
                foreach (int day in kvp.Value.Days.Distinct())
                {
                    db.UserLoginEventDays.Add(new UserLoginEventDay
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserLoginEventId = ev.Id,
                        Day = day
                    });
                }
                return ev;
            },
            (e, kvp) =>
            {
                e.LastDay = kvp.Value.LastDay;
                e.LastDate = kvp.Value.LastDate;

                SyncCollection(
                    db.UserLoginEventDays,
                    e.Days,
                    kvp.Value.Days,
                    d => d.Day,
                    day => day,
                    day => new UserLoginEventDay
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserLoginEventId = e.Id,
                        Day = day
                    },
                    (_, _) => { });
            });

        SyncCollection(
            db.UserEventMissions,
            db.UserEventMissions.Include(x => x.Entries).Where(x => x.UserId == userId).ToList(),
            user.EventMissionInfo,
            e => e.EventId,
            kvp => kvp.Key,
            kvp =>
            {
                var em = new UserEventMission
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    EventId = kvp.Key,
                    LastDay = kvp.Value.LastDay,
                    LastDate = kvp.Value.LastDate,
                    AllClear = kvp.Value.AllClear
                };
                var entries = kvp.Value.MissionIdList.Select(id => (IsDaily: false, MissionId: id))
                    .Concat(kvp.Value.DailyMissionIdList.Select(id => (IsDaily: true, MissionId: id)))
                    .Distinct();
                foreach (var entry in entries)
                {
                    db.UserEventMissionEntries.Add(new UserEventMissionEntry
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventMissionId = em.Id,
                        IsDaily = entry.IsDaily,
                        MissionId = entry.MissionId
                    });
                }
                return em;
            },
            (e, kvp) =>
            {
                e.LastDay = kvp.Value.LastDay;
                e.LastDate = kvp.Value.LastDate;
                e.AllClear = kvp.Value.AllClear;

                var entries = kvp.Value.MissionIdList.Select(id => (IsDaily: false, MissionId: id))
                    .Concat(kvp.Value.DailyMissionIdList.Select(id => (IsDaily: true, MissionId: id)));
                SyncCollection(
                    db.UserEventMissionEntries,
                    e.Entries,
                    entries,
                    m => (m.IsDaily, m.MissionId),
                    m => m,
                    m => new UserEventMissionEntry
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        UserEventMissionId = e.Id,
                        IsDaily = m.IsDaily,
                        MissionId = m.MissionId
                    },
                    (_, _) => { });
            });

        var desiredShopBuys = user.EventShopBuyCountInfo
            .SelectMany(kvp => kvp.Value.datas.Select(p => (EventId: kvp.Key, p.ProductTid, p.BuyCount)));
        SyncCollection(
            db.UserEventShopBuys,
            db.UserEventShopBuys.Where(x => x.UserId == userId).ToList(),
            desiredShopBuys,
            e => (e.EventId, e.ProductTid),
            s => (s.EventId, s.ProductTid),
            s => new UserEventShopBuy
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                EventId = s.EventId,
                ProductTid = s.ProductTid,
                BuyCount = s.BuyCount
            },
            (e, s) => e.BuyCount = s.BuyCount);

        // Group 10: Bonds, favorite items, and liberation
        SyncCollection(
            db.UserBonds,
            db.UserBonds.Include(x => x.ProgressItems).Where(x => x.UserId == userId).ToList(),
            user.BondInfo,
            e => e.NameCode,
            b => b.NameCode,
            b =>
            {
                var bond = new UserBond
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    NameCode = b.NameCode,
                    Lv = b.Lv,
                    Exp = b.Exp,
                    CanCounselToday = b.CanCounselToday,
                    IsFavorites = b.IsFavorites,
                    CounseledCount = b.CounseledCount,
                    CompleteRewardStatus = (int)b.CompleteRewardStatus
                };
                foreach (var item in GetBondProgressItems(b).Distinct())
                {
                    db.UserBondProgressItems.Add(new UserBondProgressItem
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        BondId = bond.Id,
                        Kind = item.Kind,
                        ItemId = item.ItemId
                    });
                }
                return bond;
            },
            (e, b) =>
            {
                e.Lv = b.Lv;
                e.Exp = b.Exp;
                e.CanCounselToday = b.CanCounselToday;
                e.IsFavorites = b.IsFavorites;
                e.CounseledCount = b.CounseledCount;
                e.CompleteRewardStatus = (int)b.CompleteRewardStatus;

                SyncCollection(
                    db.UserBondProgressItems,
                    e.ProgressItems,
                    GetBondProgressItems(b),
                    p => (p.Kind, p.ItemId),
                    p => p,
                    p => new UserBondProgressItem
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        BondId = e.Id,
                        Kind = p.Kind,
                        ItemId = p.ItemId
                    },
                    (_, _) => { });
            });

        SyncCollection(
            db.UserFavoriteItems,
            db.UserFavoriteItems.Where(x => x.UserId == userId).ToList(),
            user.FavoriteItems,
            e => e.FavoriteItemId,
            s => s.FavoriteItemId,
            s => new UserFavoriteItem
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                FavoriteItemId = s.FavoriteItemId,
                Tid = s.Tid,
                Csn = s.Csn,
                Lv = s.Lv,
                Exp = s.Exp
            },
            (e, s) =>
            {
                e.Tid = s.Tid;
                e.Csn = s.Csn;
                e.Lv = s.Lv;
                e.Exp = s.Exp;
            });

        SyncCollection(
            db.UserFavoriteItemQuests,
            db.UserFavoriteItemQuests.Where(x => x.UserId == userId).ToList(),
            user.FavoriteItemQuests,
            e => e.QuestId,
            s => s.QuestId,
            s => new UserFavoriteItemQuest
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                QuestId = s.QuestId,
                Clear = s.Clear,
                Received = s.Received
            },
            (e, s) =>
            {
                e.Clear = s.Clear;
                e.Received = s.Received;
            });

        SyncCollection(
            db.UserLiberations,
            db.UserLiberations.Include(x => x.Missions).Where(x => x.UserId == userId).ToList(),
            user.LiberateDatas,
            e => e.CharacterId,
            kvp => kvp.Key,
            kvp =>
            {
                var lib = new UserLiberation
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    CharacterId = kvp.Key,
                    Step = kvp.Value.Step,
                    ProgressPoint = kvp.Value.ProgressPoint,
                    RewardedCount = kvp.Value.RewardedCount,
                    IsCompleted = kvp.Value.IsCompleted
                };
                foreach (var m in kvp.Value.MissionData.GroupBy(x => x.Id).Select(g => g.Last()))
                {
                    db.UserLiberationMissions.Add(new UserLiberationMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LiberationId = lib.Id,
                        MissionId = m.Id,
                        MissionTid = m.MissionTid,
                        LiberateCharacterId = m.LiberateCharacterId,
                        MissionState = (int)m.MissionState,
                        CreatedAt = m.CreatedAt,
                        TriggerStartAt = m.TriggerStartAt,
                        TriggerEndAt = m.TriggerEndAt,
                        ReceivedAt = m.ReceivedAt
                    });
                }
                return lib;
            },
            (e, kvp) =>
            {
                e.Step = kvp.Value.Step;
                e.ProgressPoint = kvp.Value.ProgressPoint;
                e.RewardedCount = kvp.Value.RewardedCount;
                e.IsCompleted = kvp.Value.IsCompleted;

                SyncCollection(
                    db.UserLiberationMissions,
                    e.Missions,
                    kvp.Value.MissionData,
                    m => m.MissionId,
                    m => m.Id,
                    m => new UserLiberationMission
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LiberationId = e.Id,
                        MissionId = m.Id,
                        MissionTid = m.MissionTid,
                        LiberateCharacterId = m.LiberateCharacterId,
                        MissionState = (int)m.MissionState,
                        CreatedAt = m.CreatedAt,
                        TriggerStartAt = m.TriggerStartAt,
                        TriggerEndAt = m.TriggerEndAt,
                        ReceivedAt = m.ReceivedAt
                    },
                    (m, src) =>
                    {
                        m.MissionTid = src.MissionTid;
                        m.LiberateCharacterId = src.LiberateCharacterId;
                        m.MissionState = (int)src.MissionState;
                        m.CreatedAt = src.CreatedAt;
                        m.TriggerStartAt = src.TriggerStartAt;
                        m.TriggerEndAt = src.TriggerEndAt;
                        m.ReceivedAt = src.ReceivedAt;
                    });
            });

        SyncCollection(
            db.UserOpenLiberateTypes,
            db.UserOpenLiberateTypes.Where(x => x.UserId == userId).ToList(),
            user.OpenLiberateTypeIdList,
            e => e.LiberateTypeId,
            id => id,
            id => new UserOpenLiberateType
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                LiberateTypeId = id
            },
            (_, _) => { });

        // Group 11: Messenger and badges
        SyncCollection(
            db.UserMessages,
            db.UserMessages.Where(x => x.UserId == userId).ToList(),
            user.MessengerData,
            e => e.Seq,
            s => s.Seq,
            s => new UserMessage
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Seq = s.Seq,
                ConversationId = s.ConversationId ?? "",
                MessageId = s.MessageId ?? "",
                CreatedAt = s.CreatedAt,
                State = s.State
            },
            (e, s) =>
            {
                e.ConversationId = s.ConversationId ?? "";
                e.MessageId = s.MessageId ?? "";
                e.CreatedAt = s.CreatedAt;
                e.State = s.State;
            });

        SyncCollection(
            db.UserPickedMessages,
            db.UserPickedMessages.Where(x => x.UserId == userId).ToList(),
            user.PickedMessages,
            e => e.ConversationId,
            s => s.ConversationId ?? "",
            s => new UserPickedMessage
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                ConversationId = s.ConversationId ?? "",
                CreatedAtSeconds = s.CreatedAt?.Seconds ?? 0,
                CreatedAtNanos = s.CreatedAt?.Nanos ?? 0
            },
            (e, s) =>
            {
                e.CreatedAtSeconds = s.CreatedAt?.Seconds ?? 0;
                e.CreatedAtNanos = s.CreatedAt?.Nanos ?? 0;
            });

        SyncCollection(
            db.UserBadges,
            db.UserBadges.Where(x => x.UserId == userId).ToList(),
            user.Badges,
            e => e.Seq,
            s => s.Seq,
            s => new UserBadge
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Seq = s.Seq,
                Location = s.Location ?? "",
                BadgeContent = (int)s.BadgeContent,
                BadgeGuid = s.BadgeGuid ?? ""
            },
            (e, s) =>
            {
                e.Location = s.Location ?? "";
                e.BadgeContent = (int)s.BadgeContent;
                e.BadgeGuid = s.BadgeGuid ?? "";
            });

        // Group 12: Achievements, side story, archive, and Lost Sector
        SyncCollection(
            db.UserCompletedAchievements,
            db.UserCompletedAchievements.Where(x => x.UserId == userId).ToList(),
            user.CompletedAchievements,
            e => e.AchievementId,
            id => id,
            id => new UserCompletedAchievement
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                AchievementId = id
            },
            (_, _) => { });

        var completedSideStorySet = user.CompletedSideStoryStages.ToHashSet();
        var viewedSideStorySet = user.ViewedSideStoryStages.ToHashSet();
        var allSideStoryIds = completedSideStorySet.Union(viewedSideStorySet);
        SyncCollection(
            db.UserSideStoryStages,
            db.UserSideStoryStages.Where(x => x.UserId == userId).ToList(),
            allSideStoryIds,
            e => e.StageId,
            id => id,
            id => new UserSideStoryStage
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                StageId = id,
                IsCompleted = completedSideStorySet.Contains(id),
                IsViewed = viewedSideStorySet.Contains(id)
            },
            (e, id) =>
            {
                e.IsCompleted = completedSideStorySet.Contains(id);
                e.IsViewed = viewedSideStorySet.Contains(id);
            });

        var desiredArchiveUnlocks = user.UnlockedArchiveRecordIds.Select(id => (Kind: ArchiveKindRecord, TargetId: id))
            .Concat(user.UnlockedArchiveEventQuestIds.Select(id => (Kind: ArchiveKindEventQuest, TargetId: id)));
        SyncCollection(
            db.UserArchiveUnlocks,
            db.UserArchiveUnlocks.Where(x => x.UserId == userId).ToList(),
            desiredArchiveUnlocks,
            e => (e.Kind, e.TargetId),
            s => s,
            s => new UserArchiveUnlock
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Kind = s.Kind,
                TargetId = s.TargetId
            },
            (_, _) => { });

        SyncCollection(
            db.UserLostSectors,
            db.UserLostSectors.Include(x => x.ClearedStages).Include(x => x.Objects).Where(x => x.UserId == userId).ToList(),
            user.LostSectorData,
            e => e.SectorId,
            kvp => kvp.Key,
            kvp =>
            {
                var ls = new UserLostSector
                {
                    Id = UlidGenerator.NewUlid(),
                    UserId = userId,
                    SectorId = kvp.Key,
                    IsOpen = kvp.Value.IsOpen,
                    IsPlaying = kvp.Value.IsPlaying,
                    StateJson = kvp.Value.Json ?? "",
                    TeamPositionsJson = JsonConvert.SerializeObject(kvp.Value.TeamPositions ?? []),
                    ObtainedRewards = kvp.Value.ObtainedRewards,
                    ReceivedFinalReward = kvp.Value.ReceivedFinalReward,
                    CompletedPerfectly = kvp.Value.CompletedPerfectly
                };
                foreach (var stage in kvp.Value.ClearedStages)
                {
                    db.UserLostSectorClearedStages.Add(new UserLostSectorClearedStage
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LostSectorId = ls.Id,
                        StageKey = stage.Key,
                        ClearState = stage.Value
                    });
                }
                foreach (var obj in kvp.Value.Objects)
                {
                    db.UserLostSectorObjects.Add(new UserLostSectorObject
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LostSectorId = ls.Id,
                        ObjectKey = obj.Key,
                        ActionAt = obj.Value.ActionAt,
                        Type = obj.Value.Type,
                        PositionId = obj.Value.PositionId ?? "",
                        Json = obj.Value.Json ?? "",
                        TeamPositionsJson = JsonConvert.SerializeObject(obj.Value.TeamPosition.ToList())
                    });
                }
                return ls;
            },
            (e, kvp) =>
            {
                e.IsOpen = kvp.Value.IsOpen;
                e.IsPlaying = kvp.Value.IsPlaying;
                e.StateJson = kvp.Value.Json ?? "";
                e.TeamPositionsJson = JsonConvert.SerializeObject(kvp.Value.TeamPositions ?? []);
                e.ObtainedRewards = kvp.Value.ObtainedRewards;
                e.ReceivedFinalReward = kvp.Value.ReceivedFinalReward;
                e.CompletedPerfectly = kvp.Value.CompletedPerfectly;

                SyncCollection(
                    db.UserLostSectorClearedStages,
                    e.ClearedStages,
                    kvp.Value.ClearedStages,
                    s => s.StageKey,
                    s => s.Key,
                    s => new UserLostSectorClearedStage
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LostSectorId = e.Id,
                        StageKey = s.Key,
                        ClearState = s.Value
                    },
                    (s, src) => s.ClearState = src.Value);

                SyncCollection(
                    db.UserLostSectorObjects,
                    e.Objects,
                    kvp.Value.Objects,
                    o => o.ObjectKey,
                    o => o.Key,
                    o => new UserLostSectorObject
                    {
                        Id = UlidGenerator.NewUlid(),
                        UserId = userId,
                        LostSectorId = e.Id,
                        ObjectKey = o.Key,
                        ActionAt = o.Value.ActionAt,
                        Type = o.Value.Type,
                        PositionId = o.Value.PositionId ?? "",
                        Json = o.Value.Json ?? "",
                        TeamPositionsJson = JsonConvert.SerializeObject(o.Value.TeamPosition.ToList())
                    },
                    (o, src) =>
                    {
                        o.ActionAt = src.Value.ActionAt;
                        o.Type = src.Value.Type;
                        o.PositionId = src.Value.PositionId ?? "";
                        o.Json = src.Value.Json ?? "";
                        o.TeamPositionsJson = JsonConvert.SerializeObject(src.Value.TeamPosition.ToList());
                    });
            });
    }

    private static IEnumerable<(string Kind, int ItemId)> GetBondProgressItems(NetUserAttractiveData bond)
    {
        return bond.FlushableWatchedDialogIds.Select(id => (BondKindFlushableWatchedDialog, id))
            .Concat(bond.ObtainedRewardLevels.Select(id => (BondKindObtainedRewardLevel, id)))
            .Concat(bond.CounselDialogCompleteIds.Select(id => (BondKindCounselDialogComplete, id)));
    }

    private static void SyncCollection<TEntity, TKey, TSource>(
        DbSet<TEntity> dbSet,
        IEnumerable<TEntity> existingRows,
        IEnumerable<TSource> desiredItems,
        Func<TEntity, TKey> entityKey,
        Func<TSource, TKey> sourceKey,
        Func<TSource, TEntity> createEntity,
        Action<TEntity, TSource> updateEntity)
        where TEntity : class
        where TKey : notnull
    {
        var existingByKey = existingRows.ToDictionary(entityKey);
        var seenKeys = new HashSet<TKey>();

        foreach (var item in desiredItems)
        {
            var key = sourceKey(item);
            if (!seenKeys.Add(key))
                continue;

            if (existingByKey.TryGetValue(key, out var existing))
            {
                updateEntity(existing, item);
            }
            else
            {
                dbSet.Add(createEntity(item));
            }
        }

        foreach (var (key, existing) in existingByKey)
        {
            if (!seenKeys.Contains(key))
            {
                dbSet.Remove(existing);
            }
        }
    }
}
