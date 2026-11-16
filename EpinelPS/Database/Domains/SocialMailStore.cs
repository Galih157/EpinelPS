using EpinelPS.Models;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Database.Domains;

public sealed class SocialMailStore : IUserDomainStore
{
    public void Load(GameContext db, User user)
    {
        ulong userId = user.ID;

        user.MessengerData = db.Set<UserMessage>().AsNoTracking()
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

        user.PickedMessages = db.Set<UserPickedMessage>().AsNoTracking()
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

        user.Badges = db.Set<UserBadge>().AsNoTracking()
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

        var mailRows = db.Set<UserMailEntity>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToList();
        user.MailDatas = new Dictionary<long, NetUserMailData>(mailRows.Count);
        foreach (var row in mailRows)
        {
            var mail = new NetUserMailData
            {
                Msn = row.Msn,
                Sender = row.Sender,
                Type = row.Type,
                State = row.State,
                Title = string.IsNullOrWhiteSpace(row.TitleJson) || row.TitleJson == "null"
                    ? null
                    : JsonConvert.DeserializeObject<LocaleableText>(row.TitleJson),
                Text = string.IsNullOrWhiteSpace(row.TextJson) || row.TextJson == "null"
                    ? null
                    : JsonConvert.DeserializeObject<LocaleableText>(row.TextJson),
                CreatedAt = row.CreatedAt,
                Nickname = row.Nickname ?? "",
                AppNoticeId = row.AppNoticeId ?? "",
                HasReward = row.HasReward,
                Period = row.Period
            };
            if (!string.IsNullOrWhiteSpace(row.ItemsJson))
            {
                var items = JsonConvert.DeserializeObject<List<NetMailRewardItem>>(row.ItemsJson);
                if (items != null)
                    mail.Items.AddRange(items);
            }
            user.MailDatas[row.Msn] = mail;
        }

        var repairRows = db.Set<UserMessengerRepairHistoryEntity>().AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();
        user.MessengerRepairHistory = repairRows.Select(row => new MessengerRepairAuditEntry
        {
            Id = row.AuditId,
            CreatedAtUtc = new DateTime(row.CreatedAtUtc, DateTimeKind.Utc),
            Action = row.Action ?? "",
            Note = row.Note ?? "",
            Message = string.IsNullOrWhiteSpace(row.MessageJson)
                ? new NetMessage()
                : JsonConvert.DeserializeObject<NetMessage>(row.MessageJson) ?? new NetMessage()
        }).ToList();
    }

    public void Save(GameContext db, User user)
    {
        ulong userId = user.ID;

        DomainStoreHelpers.SyncCollection(
            db.Set<UserMessage>(),
            db.Set<UserMessage>().Where(x => x.UserId == userId).ToList(),
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

        DomainStoreHelpers.SyncCollection(
            db.Set<UserPickedMessage>(),
            db.Set<UserPickedMessage>().Where(x => x.UserId == userId).ToList(),
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

        DomainStoreHelpers.SyncCollection(
            db.Set<UserBadge>(),
            db.Set<UserBadge>().Where(x => x.UserId == userId).ToList(),
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

        var desiredMails = (user.MailDatas ?? []).Select(kvp =>
        {
            var m = kvp.Value;
            long msn = m.Msn != 0 ? m.Msn : kvp.Key;
            return (Msn: msn, Mail: m);
        });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserMailEntity>(),
            db.Set<UserMailEntity>().Where(x => x.UserId == userId).ToList(),
            desiredMails,
            e => e.Msn,
            s => s.Msn,
            s => new UserMailEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                Msn = s.Msn,
                Sender = s.Mail.Sender,
                Type = s.Mail.Type,
                State = s.Mail.State,
                TitleJson = s.Mail.Title != null ? JsonConvert.SerializeObject(s.Mail.Title) : "",
                TextJson = s.Mail.Text != null ? JsonConvert.SerializeObject(s.Mail.Text) : "",
                CreatedAt = s.Mail.CreatedAt,
                Nickname = s.Mail.Nickname ?? "",
                AppNoticeId = s.Mail.AppNoticeId ?? "",
                HasReward = s.Mail.HasReward,
                ItemsJson = JsonConvert.SerializeObject(s.Mail.Items.ToList()),
                Period = s.Mail.Period
            },
            (e, s) =>
            {
                e.Sender = s.Mail.Sender;
                e.Type = s.Mail.Type;
                e.State = s.Mail.State;
                e.TitleJson = s.Mail.Title != null ? JsonConvert.SerializeObject(s.Mail.Title) : "";
                e.TextJson = s.Mail.Text != null ? JsonConvert.SerializeObject(s.Mail.Text) : "";
                e.CreatedAt = s.Mail.CreatedAt;
                e.Nickname = s.Mail.Nickname ?? "";
                e.AppNoticeId = s.Mail.AppNoticeId ?? "";
                e.HasReward = s.Mail.HasReward;
                e.ItemsJson = JsonConvert.SerializeObject(s.Mail.Items.ToList());
                e.Period = s.Mail.Period;
            });

        DomainStoreHelpers.SyncCollection(
            db.Set<UserMessengerRepairHistoryEntity>(),
            db.Set<UserMessengerRepairHistoryEntity>().Where(x => x.UserId == userId).ToList(),
            user.MessengerRepairHistory ?? [],
            e => e.AuditId,
            a => a.Id,
            a => new UserMessengerRepairHistoryEntity
            {
                Id = UlidGenerator.NewUlid(),
                UserId = userId,
                AuditId = a.Id,
                CreatedAtUtc = a.CreatedAtUtc.Ticks,
                Action = a.Action ?? "",
                Note = a.Note ?? "",
                MessageJson = JsonConvert.SerializeObject(a.Message ?? new NetMessage())
            },
            (e, a) =>
            {
                e.CreatedAtUtc = a.CreatedAtUtc.Ticks;
                e.Action = a.Action ?? "";
                e.Note = a.Note ?? "";
                e.MessageJson = JsonConvert.SerializeObject(a.Message ?? new NetMessage());
            });
    }
}
