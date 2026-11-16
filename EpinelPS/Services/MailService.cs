using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;
using EpinelPS.Utils;

namespace EpinelPS.Services;

public sealed class MailService(SocialMailRepository repo, GameContext db)
{
    public ResGetMailData GetMails(ulong userId)
    {
        var response = new ResGetMailData();
        foreach (var item in repo.GetMails(userId))
        {
            response.Mail.Add(item);
        }
        return response;
    }

    public ResObtainMail2 ObtainMail(User user, ReqObtainMail2 req)
    {
        var response = new ResObtainMail2();
        var ret = new NetRewardData();
        long timenow = DateTime.Now.Ticks;

        var entity = repo.GetTrackedMail(user.ID, req.Msn);
        if (entity != null)
        {
            var mailData = SocialMailRepository.MapToNetMail(entity);
            foreach (var item in mailData.Items)
            {
                if (item.ExpiredAt >= timenow)
                {
                    RewardUtils.AddSingleObject(user, ref ret, item.RewardId, (RewardType)item.RewardType, item.RewardValue);
                }
            }

            entity.State = 2;
            mailData.State = 2;
            db.SaveChanges();
            UserSessionStore.SaveUser(user);

            response.Data = mailData;
            response.Result = ObtainMailResult.Success;
            response.Reward = ret;
        }

        return response;
    }

    public ResAllObtainMail ObtainAllMails(User user)
    {
        var response = new ResAllObtainMail();
        var ret = new NetRewardData();
        long timenow = DateTime.Now.Ticks;

        var unreadRows = repo.GetTrackedMailsByState(user.ID, 1);
        foreach (var entity in unreadRows)
        {
            var mailData = SocialMailRepository.MapToNetMail(entity);
            entity.State = 2;
            response.SucceedMails.Add(new Mail { Msn = entity.Msn, Type = entity.Type });

            if (mailData.HasReward)
            {
                foreach (var item in mailData.Items)
                {
                    if (item.ExpiredAt >= timenow)
                    {
                        RewardUtils.AddSingleObject(user, ref ret, item.RewardId, (RewardType)item.RewardType, item.RewardValue);
                    }
                }
            }
        }

        response.Reward = ret;
        if (unreadRows.Count > 0)
        {
            db.SaveChanges();
            UserSessionStore.SaveUser(user);
        }

        return response;
    }

    public ResDeleteReadMail DeleteReadMails(ulong userId)
    {
        var readRows = repo.GetTrackedMailsByState(userId, 2);
        if (readRows.Count > 0)
        {
            repo.RemoveMails(readRows);
            db.SaveChanges();
        }
        return new ResDeleteReadMail();
    }
}
