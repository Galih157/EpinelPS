using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EpinelPS.Repositories;

public sealed class SocialMailRepository(GameContext db)
{
    // --- Badges ---
    public List<BadgeModel> GetBadges(ulong userId)
    {
        return db.Set<UserBadge>()
            .AsNoTracking()
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
    }

    public BadgeModel AddBadge(ulong userId, BadgeContents type, string location)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        long maxSeq = db.Set<UserBadge>()
            .Where(x => x.UserId == userId)
            .Select(x => (long?)x.Seq)
            .Max() ?? 0L;
        long seq = Math.Max(gameUser.LastBadgeSeq, maxSeq + 1);
        gameUser.LastBadgeSeq = seq + 1;

        string guid = Guid.NewGuid().ToString();
        db.Set<UserBadge>().Add(new UserBadge
        {
            UserId = userId,
            Seq = seq,
            Location = location ?? "",
            BadgeContent = (int)type,
            BadgeGuid = guid
        });

        return new BadgeModel
        {
            Seq = seq,
            Location = location ?? "",
            BadgeContent = type,
            BadgeGuid = guid
        };
    }

    public int DeleteBadges(ulong userId, IEnumerable<long> seqList)
    {
        var seqSet = seqList.ToHashSet();
        if (seqSet.Count == 0) return 0;

        var rows = db.Set<UserBadge>()
            .Where(x => x.UserId == userId && seqSet.Contains(x.Seq))
            .ToList();

        if (rows.Count > 0)
            db.Set<UserBadge>().RemoveRange(rows);

        return rows.Count;
    }

    // --- Mails ---
    public List<NetUserMailData> GetMails(ulong userId)
    {
        var rows = db.Set<UserMailEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return rows.Select(MapToNetMail).ToList();
    }

    public UserMailEntity? GetTrackedMail(ulong userId, long msn)
    {
        return db.Set<UserMailEntity>()
            .FirstOrDefault(x => x.UserId == userId && x.Msn == msn);
    }

    public List<UserMailEntity> GetTrackedMailsByState(ulong userId, int state)
    {
        return db.Set<UserMailEntity>()
            .Where(x => x.UserId == userId && x.State == state)
            .ToList();
    }

    public void AddMail(ulong userId, NetUserMailData mail)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);
        long msn = mail.Msn != 0 ? mail.Msn : User.GenerateMsn();
        while (db.Set<UserMailEntity>().Any(x => x.UserId == userId && x.Msn == msn))
        {
            msn = User.GenerateMsn();
        }
        mail.Msn = msn;

        db.Set<UserMailEntity>().Add(new UserMailEntity
        {
            UserId = userId,
            Msn = msn,
            Sender = mail.Sender,
            Type = mail.Type,
            State = mail.State,
            TitleJson = mail.Title != null ? JsonConvert.SerializeObject(mail.Title) : "",
            TextJson = mail.Text != null ? JsonConvert.SerializeObject(mail.Text) : "",
            CreatedAt = mail.CreatedAt,
            Nickname = mail.Nickname ?? "",
            AppNoticeId = mail.AppNoticeId ?? "",
            HasReward = mail.HasReward,
            ItemsJson = JsonConvert.SerializeObject(mail.Items.ToList()),
            Period = mail.Period
        });
    }

    public void RemoveMails(IEnumerable<UserMailEntity> entities)
    {
        db.Set<UserMailEntity>().RemoveRange(entities);
    }

    public static NetUserMailData MapToNetMail(UserMailEntity row)
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
        return mail;
    }

    // --- Messenger Messages ---
    public List<NetMessage> GetMessages(ulong userId, long minSeq = 0)
    {
        var query = db.Set<UserMessage>()
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (minSeq > 0)
            query = query.Where(x => x.Seq >= minSeq);

        return query
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
    }

    public HashSet<string> GetConversationIds(ulong userId)
    {
        return db.Set<UserMessage>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.ConversationId)
            .ToHashSet();
    }

    public bool HasConversation(ulong userId, string conversationId)
    {
        return db.Set<UserMessage>()
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.ConversationId == conversationId);
    }

    public bool HasMessageId(ulong userId, string messageId)
    {
        return db.Set<UserMessage>()
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.MessageId == messageId);
    }

    public bool HasMessageSeq(ulong userId, long seq)
    {
        return db.Set<UserMessage>()
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.Seq == seq);
    }

    public UserMessage? GetLatestTrackedMessageInConversation(ulong userId, string conversationId)
    {
        return db.Set<UserMessage>()
            .Where(x => x.UserId == userId && x.ConversationId == conversationId)
            .OrderByDescending(x => x.Seq)
            .FirstOrDefault();
    }

    public UserMessage? GetTrackedMessageByMessageId(ulong userId, string messageId)
    {
        return db.Set<UserMessage>()
            .FirstOrDefault(x => x.UserId == userId && x.MessageId == messageId);
    }

    public UserMessage? GetTrackedMessageBySeq(ulong userId, long seq)
    {
        return db.Set<UserMessage>()
            .FirstOrDefault(x => x.UserId == userId && x.Seq == seq);
    }

    public List<UserMessage> GetTrackedMessagesByConversation(ulong userId, string conversationId)
    {
        return db.Set<UserMessage>()
            .Where(x => x.UserId == userId && x.ConversationId == conversationId)
            .OrderBy(x => x.Seq)
            .ToList();
    }

    public NetMessage CreateMessage(ulong userId, string conversationId, string messageId, int state = 0, long? explicitSeq = null, long? explicitCreatedAt = null)
    {
        var gameUser = UserDbHelper.GetOrCreateGameUser(db, userId);
        long seq;
        if (explicitSeq.HasValue)
        {
            seq = explicitSeq.Value;
            gameUser.LastMessageId = Math.Max(gameUser.LastMessageId, seq + 1);
        }
        else
        {
            long maxSeq = db.Set<UserMessage>()
                .Where(x => x.UserId == userId)
                .Select(x => (long?)x.Seq)
                .Max() ?? 0L;
            seq = Math.Max(gameUser.LastMessageId, maxSeq + 1);
            gameUser.LastMessageId = seq + 1;
        }

        long createdAt = explicitCreatedAt ?? DateTime.UtcNow.Ticks;
        db.Set<UserMessage>().Add(new UserMessage
        {
            UserId = userId,
            Seq = seq,
            ConversationId = conversationId ?? "",
            MessageId = messageId ?? "",
            CreatedAt = createdAt,
            State = state
        });

        return new NetMessage
        {
            Seq = seq,
            ConversationId = conversationId ?? "",
            MessageId = messageId ?? "",
            CreatedAt = createdAt,
            State = state
        };
    }

    public void RemoveMessage(UserMessage message)
    {
        db.Set<UserMessage>().Remove(message);
    }

    public static NetMessage MapToNetMessage(UserMessage x) => new()
    {
        Seq = x.Seq,
        ConversationId = x.ConversationId,
        MessageId = x.MessageId,
        CreatedAt = x.CreatedAt,
        State = x.State
    };

    // --- Picked Messages ---
    public List<NetPickedMessage> GetPickedMessages(ulong userId)
    {
        return db.Set<UserPickedMessage>()
            .AsNoTracking()
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
    }

    public bool HasPickedConversation(ulong userId, string conversationId)
    {
        return db.Set<UserPickedMessage>()
            .AsNoTracking()
            .Any(x => x.UserId == userId && x.ConversationId == conversationId);
    }

    public void UpsertPickedMessage(ulong userId, string conversationId, Timestamp createdAt)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);
        var existing = db.Set<UserPickedMessage>()
            .FirstOrDefault(x => x.UserId == userId && x.ConversationId == conversationId);

        if (existing != null)
        {
            existing.CreatedAtSeconds = createdAt.Seconds;
            existing.CreatedAtNanos = createdAt.Nanos;
        }
        else
        {
            db.Set<UserPickedMessage>().Add(new UserPickedMessage
            {
                UserId = userId,
                ConversationId = conversationId,
                CreatedAtSeconds = createdAt.Seconds,
                CreatedAtNanos = createdAt.Nanos
            });
        }
    }

    // --- Messenger Repair History ---
    public List<MessengerRepairAuditEntry> GetRepairHistory(ulong userId)
    {
        var rows = db.Set<UserMessengerRepairHistoryEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        return rows.Select(MapToAuditEntry).ToList();
    }

    public MessengerRepairAuditEntry? GetRepairAuditEntry(ulong userId, string auditId, string action)
    {
        var row = db.Set<UserMessengerRepairHistoryEntity>()
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == userId && x.AuditId == auditId && x.Action == action);

        return row != null ? MapToAuditEntry(row) : null;
    }

    public void AddRepairAuditEntry(ulong userId, string action, NetMessage message, string note)
    {
        UserDbHelper.GetOrCreateGameUser(db, userId);
        var entry = new MessengerRepairAuditEntry
        {
            Action = action,
            Note = note,
            Message = message.Clone()
        };

        db.Set<UserMessengerRepairHistoryEntity>().Add(new UserMessengerRepairHistoryEntity
        {
            UserId = userId,
            AuditId = entry.Id,
            CreatedAtUtc = entry.CreatedAtUtc.Ticks,
            Action = entry.Action,
            Note = entry.Note,
            MessageJson = JsonConvert.SerializeObject(entry.Message)
        });
    }

    private static MessengerRepairAuditEntry MapToAuditEntry(UserMessengerRepairHistoryEntity row) => new()
    {
        Id = row.AuditId,
        CreatedAtUtc = new DateTime(row.CreatedAtUtc, DateTimeKind.Utc),
        Action = row.Action ?? "",
        Note = row.Note ?? "",
        Message = string.IsNullOrWhiteSpace(row.MessageJson)
            ? new NetMessage()
            : JsonConvert.DeserializeObject<NetMessage>(row.MessageJson) ?? new NetMessage()
    };
}
