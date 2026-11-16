using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;

namespace EpinelPS.Services;

public sealed class CosmeticsService(CosmeticsRepository repo, GameContext db)
{
    public ResGetWallpaper GetWallpaper(ulong userId)
    {
        var response = new ResGetWallpaper();
        var (slots, backgrounds, jukeboxes, favorites, playlists) = repo.GetWallpapers(userId);

        response.WallpaperList.AddRange(slots);
        response.WallpaperPlaylistList.AddRange(playlists);
        response.WallpaperJukeboxList.AddRange(jukeboxes);
        response.WallpaperBackgroundList.AddRange(backgrounds);
        response.WallpaperFavoriteList.AddRange(favorites);
        response.OwnedLobbyDecoBackgroundIdList.AddRange(
            repo.GetUnlockedCosmetics(userId, CosmeticsRepository.CategoryLobbyDecoBackground));
        response.JukeboxIdList.AddRange(JukeboxUtils.GetUnlockedSongs(userId, repo));
        return response;
    }

    public ResSetWallpaper SetWallpaper(ulong userId, ReqSetWallpaper req)
    {
        repo.SaveWallpapers(userId, req);
        db.SaveChanges();
        return new ResSetWallpaper();
    }

    public ResBuyLobbyDecoBackground BuyWallpaper(ulong userId, ReqBuyLobbyDecoBackground req)
    {
        if (repo.UnlockCosmetic(userId, CosmeticsRepository.CategoryLobbyDecoBackground, req.LobbyDecoBackgroundId))
            db.SaveChanges();

        var response = new ResBuyLobbyDecoBackground();
        response.OwnedLobbyDecoBackgroundIdList.AddRange(
            repo.GetUnlockedCosmetics(userId, CosmeticsRepository.CategoryLobbyDecoBackground));
        return response;
    }

    public ResGetWallpaperInventory GetWallpaperInventory()
    {
        var response = new ResGetWallpaperInventory();
        var wallpaperIds = GameData.Instance.LiveWallpaperTable
            .Where(w => w.Value.LivewallpaperType != Livewallpaper_type.SkillCutScene)
            .Select(w => w.Key);
        response.LivewallpaperIds.AddRange(wallpaperIds);
        return response;
    }

    public ResProfileCardObjectList GetProfileCardPossession(ulong userId)
    {
        var rareCards = GameData.Instance.ProfileCardObjectTable.Values
            .Where(x => x.GradeType == ProfileCardObjectGradeType.R)
            .Select(c => c.Id);

        if (repo.UnlockCosmetics(userId, CosmeticsRepository.CategoryProfileCard, rareCards) > 0)
            db.SaveChanges();

        var response = new ResProfileCardObjectList();
        foreach (int id in repo.GetUnlockedCosmetics(userId, CosmeticsRepository.CategoryProfileCard))
        {
            if (!GameData.Instance.ProfileCardObjectTable.TryGetValue(id, out var card))
                continue;

            switch (card.ObjectType)
            {
                case ObjectType.BackGround:
                    response.BackgroundIds.Add(id);
                    break;
                case ObjectType.Sticker:
                    response.StickerIds.Add(id);
                    break;
            }
        }
        return response;
    }

    public ResProfileCardDecorationLayout GetProfileDecoration(ulong userId)
        => new() { Layout = repo.GetProfileCardDecoration(userId) };

    public ResSaveProfileCardDecorationLayout SaveProfileDecoration(ulong userId, ReqSaveProfileCardDecorationLayout req)
    {
        repo.SaveProfileCardDecoration(userId, req.Layout);
        db.SaveChanges();
        return new ResSaveProfileCardDecorationLayout();
    }

    public ResBuyProfileCardObject BuyProfileCard(User user, ReqBuyProfileCardObject req)
    {
        var response = new ResBuyProfileCardObject();
        GameData.Instance.ProfileCardObjectTable.TryGetValue(req.ObjectTid, out var card);
        if (repo.UnlockCosmetic(user.ID, CosmeticsRepository.CategoryProfileCard, card!.Id))
            db.SaveChanges();

        GameData.Instance.itemMaterialTable.TryGetValue(card.RequireItemId, out var requiredItem);
        var requiredItemInInventory = user.Items.First(x => x.ItemType == requiredItem!.Id);
        requiredItemInInventory.Count -= card.RequireItemValue;
        response.ProfileCardTicketMaterialSync = NetUtils.UserItemDataToNet(requiredItemInInventory);

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResOpenProfileRandomBox OpenProfileRandomBox(User user, ReqOpenProfileRandomBox req)
    {
        var response = new ResOpenProfileRandomBox();
        DbItemData box = user.Items.FirstOrDefault(x => x.Isn == req.Isn)
            ?? throw new InvalidDataException("cannot find box with isn " + req.Isn);
        GameData.Instance.ConsumableItems.TryGetValue(box.ItemType, out var item);
        var pcoTable = GameData.Instance.ProfileCardObjectTable.Values.ToList();

        if (req.NumOpens * item!.UseFragCost > box.Count)
            throw new Exception("count mismatch");

        box.Count -= req.NumOpens * item.UseFragCost;
        if (box.Count == 0)
            user.Items.Remove(box);

        var randomReward = NetUtils.UseLootBox(user, box.ItemType, req.NumOpens);
        bool unlockedNewCard = false;

        foreach (int card in randomReward.ProfileCardObjects)
        {
            if (repo.HasUnlockedCosmetic(user.ID, CosmeticsRepository.CategoryProfileCard, card))
            {
                var cardMeta = pcoTable.FirstOrDefault(c => c.Id == card);
                GameData.Instance.itemMaterialTable.TryGetValue(cardMeta!.ExchangeItemId, out var exchangeItem);
                var ret = new NetRewardData();
                RewardUtils.AddSingleObject(user, ref ret, exchangeItem!.Id, RewardType.Item, cardMeta.ExchangeItemValue);
                response.ProfileCardTicketMaterialSync.Add(ret.UserItems.First());
                response.OpeningResult.Add(new ProfileRandomBoxSingleOpeningResult
                {
                    ObjectTid = card,
                    ExchangedForTicketMaterial = true
                });
            }
            else
            {
                if (repo.UnlockCosmetic(user.ID, CosmeticsRepository.CategoryProfileCard, card))
                    unlockedNewCard = true;
                response.OpeningResult.Add(new ProfileRandomBoxSingleOpeningResult
                {
                    ObjectTid = card,
                    ExchangedForTicketMaterial = false
                });
            }
        }

        if (unlockedNewCard)
            db.SaveChanges();

        response.ProfileCardTicketMaterialSync.Add(NetUtils.UserItemDataToNet(box));
        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResGetUserTitleList GetUserTitles()
    {
        var response = new ResGetUserTitleList();
        foreach (int titleId in GameData.Instance.userTitleRecords.Keys)
        {
            response.UserTitleList.Add(new ResGetUserTitleList.Types.NetUserTitle { UserTitleId = titleId });
        }
        return response;
    }

    public ResGetMemoryList GetMemorialMemoryList(ulong userId)
    {
        var response = new ResGetMemoryList();
        response.MemoryList.AddRange(repo.GetUnlockedCosmetics(userId, CosmeticsRepository.CategoryMemorial));
        return response;
    }
}
