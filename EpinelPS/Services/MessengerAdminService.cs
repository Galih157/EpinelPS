using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Messenger;
using EpinelPS.Models.Admin;
using EpinelPS.Repositories;

namespace EpinelPS.Services;

public class MessengerAdminService(SocialMailRepository repo, GameContext db)
{
    public MessengerManagementModel BuildModel(User user)
    {
        List<NetMessage> userMessages = repo.GetMessages(user.ID);
        Dictionary<string, int> conversationCounts = userMessages
            .GroupBy(message => message.ConversationId)
            .ToDictionary(group => group.Key, group => group.Count());

        List<MessengerMessageModel> messages = userMessages
            .OrderByDescending(message => message.Seq)
            .Select(message => Describe(user, message, conversationCounts[message.ConversationId]))
            .ToList();

        return new MessengerManagementModel
        {
            UserId = user.ID,
            MessageCount = messages.Count,
            ConversationCount = conversationCounts.Count,
            RoomCount = messages.Where(message => !string.IsNullOrEmpty(message.RoomId)).Select(message => message.RoomId).Distinct().Count(),
            Messages = messages,
            History = repo.GetRepairHistory(user.ID)
        };
    }

    public MessengerMessageModel Describe(User user, NetMessage message)
    {
        int count = repo.GetMessages(user.ID).Count(item => item.ConversationId == message.ConversationId);
        return Describe(user, message, count);
    }

    public bool TryCreate(User user, CreateMessengerMessageModel input, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(input.DialogId) || !GameData.Instance.Messages.TryGetValue(input.DialogId, out MessengerDialogRecord? dialog))
        {
            error = "The dialog ID does not exist in MessengerDialogTable.";
            return false;
        }

        if (repo.HasMessageId(user.ID, dialog.Id ?? ""))
        {
            error = "This dialog message already exists for the user.";
            return false;
        }

        NetMessage message = repo.CreateMessage(user.ID, dialog.ConversationId ?? "", dialog.Id ?? "", input.State);
        repo.AddRepairAuditEntry(user.ID, "Created", message, "Created manually from MessengerDialogTable. No trigger was added.");
        db.SaveChanges();
        return true;
    }

    public bool TryUpdateState(User user, UpdateMessengerMessageModel input, out string error)
    {
        error = "";
        if (input.State is < 0 or > 2)
        {
            error = "State must be 0, 1, or 2.";
            return false;
        }

        var entity = repo.GetTrackedMessageBySeq(user.ID, input.Seq);
        if (entity == null)
        {
            error = "The message no longer exists.";
            return false;
        }

        var snapshot = SocialMailRepository.MapToNetMessage(entity);
        repo.AddRepairAuditEntry(user.ID, "State updated", snapshot, $"State changed from {entity.State} to {input.State}.");
        entity.State = input.State;
        db.SaveChanges();
        return true;
    }

    public bool TryDelete(User user, long seq, out string error)
    {
        error = "";
        var entity = repo.GetTrackedMessageBySeq(user.ID, seq);
        if (entity == null)
        {
            error = "The message no longer exists.";
            return false;
        }

        var snapshot = SocialMailRepository.MapToNetMessage(entity);
        MessengerMessageModel description = Describe(user, snapshot);
        if (!description.CanSafelyDelete)
        {
            error = "Only a lone opener in a currently locked room can be removed here. This record needs manual investigation.";
            return false;
        }

        repo.AddRepairAuditEntry(user.ID, "Deleted", snapshot, description.Status);
        repo.RemoveMessage(entity);
        db.SaveChanges();
        return true;
    }

    public bool TryRestore(User user, string auditId, out string error)
    {
        error = "";
        MessengerRepairAuditEntry? entry = repo.GetRepairAuditEntry(user.ID, auditId, "Deleted");
        if (entry == null)
        {
            error = "The deleted-message audit record was not found.";
            return false;
        }

        if (repo.HasMessageSeq(user.ID, entry.Message.Seq))
        {
            error = "A message with this sequence number already exists.";
            return false;
        }

        NetMessage restored = repo.CreateMessage(
            user.ID,
            entry.Message.ConversationId ?? "",
            entry.Message.MessageId ?? "",
            entry.Message.State,
            explicitSeq: entry.Message.Seq,
            explicitCreatedAt: entry.Message.CreatedAt);
        repo.AddRepairAuditEntry(user.ID, "Restored", restored, $"Restored deleted audit entry {entry.Id}.");
        db.SaveChanges();
        return true;
    }

    private static MessengerMessageModel Describe(User user, NetMessage message, int conversationMessageCount)
    {
        bool staticDialogFound = GameData.Instance.Messages.TryGetValue(message.MessageId, out MessengerDialogRecord? dialog);
        MessengerConditionTriggerRecord? condition = GameData.Instance.MessageConditions.Values
            .FirstOrDefault(item => item.Tid == message.ConversationId);
        string roomId = dialog?.RoomId ?? "";
        bool roomUnlocked = staticDialogFound && MessengerAccessValidator.IsRoomUnlockSatisfied(user, roomId);
        bool isOpener = dialog?.IsOpener ?? false;
        bool canSafelyDelete = staticDialogFound && isOpener && conversationMessageCount == 1 && !roomUnlocked;
        bool? triggerListSatisfied = condition == null
            ? null
            : condition.MessageType is MessageType.RandomMessage or MessageType.DailyMessage
                ? null
                : MessengerTriggerUtils.IsTriggerListSatisfied(user, condition.TriggerList);

        string status = !staticDialogFound
            ? "Static dialog is missing; do not delete automatically."
            : canSafelyDelete
                ? "Safe repair candidate: lone opener in a locked room."
                : conversationMessageCount > 1
                    ? "Conversation has progressed; manual investigation required."
                    : roomUnlocked
                        ? "Room is currently unlocked."
                        : "Not an opener; manual investigation required.";

        DateTime? createdAtUtc = message.CreatedAt > 0 ? new DateTime(message.CreatedAt, DateTimeKind.Utc) : null;
        return new MessengerMessageModel
        {
            Seq = message.Seq,
            ConversationId = message.ConversationId,
            MessageId = message.MessageId,
            RoomId = roomId,
            State = message.State,
            CreatedAtUtc = createdAtUtc,
            StaticDialogFound = staticDialogFound,
            IsOpener = isOpener,
            ConditionId = condition?.Id,
            MessageType = condition?.MessageType.ToString() ?? "—",
            TriggerCount = condition?.TriggerList?.Count ?? 0,
            TriggerListSatisfied = triggerListSatisfied,
            RoomUnlocked = roomUnlocked,
            CanSafelyDelete = canSafelyDelete,
            Status = status
        };
    }
}
