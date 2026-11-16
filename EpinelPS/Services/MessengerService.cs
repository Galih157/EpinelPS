using System.Runtime.InteropServices;
using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Messenger;
using EpinelPS.Repositories;
using EpinelPS.Utils;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.Services;

public sealed class MessengerService(SocialMailRepository repo, GameContext db, ProgressRepository progressRepo)
{
    public ResGetMessages GetMessages(ulong userId, ReqGetMessages req)
    {
        var response = new ResGetMessages();
        foreach (NetMessage message in repo.GetMessages(userId, req.Seq))
        {
            response.Messages.Add(message);
        }
        return response;
    }

    public ResGetPickedMessageList GetPickedMessages(ulong userId)
    {
        var response = new ResGetPickedMessageList();
        DateTime cutoff = DateTime.UtcNow.AddHours(-24);

        foreach (NetPickedMessage pick in repo.GetPickedMessages(userId))
        {
            if (pick.CreatedAt != null && pick.CreatedAt.ToDateTime() > cutoff)
            {
                response.Data.Add(pick);
            }
        }

        Logging.WriteLine($"[Messenger] Picked/get: returning {response.Data.Count} picks for user {userId}", LogType.Debug);
        return response;
    }

    public ResEnterMessengerDialog EnterMessenger(User user, ReqEnterMessengerDialog req)
    {
        var response = new ResEnterMessengerDialog();

        if (!GameData.Instance.MessageConditions.TryGetValue(req.Tid, out MessengerConditionTriggerRecord? opener))
        {
            throw new BadHttpRequestException($"Message condition {req.Tid} not found", 404);
        }

        KeyValuePair<string, MessengerDialogRecord> conversation = GameData.Instance.Messages.FirstOrDefault(x =>
            x.Value.ConversationId == opener.Tid && x.Value.IsOpener);

        if (conversation.Value == null)
        {
            conversation = GameData.Instance.Messages.FirstOrDefault(x =>
                x.Value.ConversationId == opener.Tid);

            if (conversation.Value == null)
            {
                throw new BadHttpRequestException($"No conversation found for {opener.Tid}", 404);
            }
        }

        if (!MessengerAccessValidator.CanEnter(user, opener, conversation.Value, repo))
        {
            Logging.WriteLine($"[Messenger] Enter denied: user={user.ID}, Tid={opener.Tid}, RoomId={conversation.Value.RoomId}", LogType.Warning);
            throw new BadHttpRequestException($"Messenger conversation {opener.Tid} is not available for this user", 403);
        }

        var existingMessage = repo.GetLatestTrackedMessageInConversation(user.ID, opener.Tid);
        bool addedTrigger = false;

        if (existingMessage != null)
        {
            response.Message = SocialMailRepository.MapToNetMessage(existingMessage);
        }
        else
        {
            response.Message = repo.CreateMessage(user.ID, conversation.Value.ConversationId ?? "", conversation.Value.Id ?? "");
            db.SaveChanges();
            user.AddTrigger(Trigger.MessageClear, 1, req.Tid);
            addedTrigger = true;
        }

        // Entering an already-created opener is still a real conversation
        // entry. Record MessageClear exactly once for this condition.
        if (!addedTrigger && !db.Triggers.Any(trigger => trigger.UserId == user.ID &&
            trigger.Type == Trigger.MessageClear && trigger.ConditionId == req.Tid))
        {
            user.AddTrigger(Trigger.MessageClear, 1, req.Tid);
        }

        // Reconcile eligible openers now that this conversation has been cleared
        MessengerMessageCreator.CreateAllEligibleOpeners(user);

        Logging.WriteLine($"[Messenger] Enter: user={user.ID}, Tid={opener.Tid}, RoomId={conversation.Value.RoomId}", LogType.Info);
        return response;
    }

    public ResProceedMessage Proceed(User user, ReqProceedMessage req)
    {
        var response = new ResProceedMessage();

        KeyValuePair<string, MessengerDialogRecord>? msgToSave = GameData.Instance.Messages.FirstOrDefault(x => x.Key == req.MessageId);
        if (msgToSave == null || msgToSave.Value.Value == null)
        {
            return response;
        }

        string convId = msgToSave.Value.Value.ConversationId ?? "";

        // Check if this conversation belongs to an already-completed subquest's end conversation
        var subQuest = GameData.Instance.Subquests.Values.FirstOrDefault(s =>
            !string.IsNullOrEmpty(s.EndMessengerConversationId) && s.EndMessengerConversationId == convId);

        bool isSubQuestDone = subQuest != null &&
            progressRepo.GetSubQuests(user.ID).TryGetValue(subQuest.Id, out bool done) && done;

        int state;
        if (isSubQuestDone)
        {
            // Subquest reward has already been claimed: maintain completed state (State = 2)
            // so the client knows this commission dialog is finished and does not re-pop the "Completed" banner.
            state = 2;
        }
        else if (msgToSave.Value.Value.MessageType == MessengerMessageType.Reward || msgToSave.Value.Value.RewardId != 0)
        {
            state = 1;
        }
        else
        {
            state = 0;
        }

        var existingMessage = repo.GetTrackedMessageByMessageId(user.ID, req.MessageId);
        if (existingMessage != null)
        {
            if (state == 2 && existingMessage.State != 2)
            {
                existingMessage.State = 2;
                db.SaveChanges();
            }
            else if (state == 1 && existingMessage.State == 0)
            {
                existingMessage.State = 1;
                db.SaveChanges();
            }
            response.Message = SocialMailRepository.MapToNetMessage(existingMessage);
        }
        else
        {
            response.Message = repo.CreateMessage(user.ID, convId, req.MessageId, state);
            db.SaveChanges();
        }

        return response;
    }

    public ResEnterSubQuestMessengerDialog EnterSubquest(ulong userId, ReqEnterSubQuestMessengerDialog req)
    {
        var response = new ResEnterSubQuestMessengerDialog();
        var opener = GameData.Instance.Subquests.FirstOrDefault(x => x.Key == req.SubQuestId);
        if (opener.Value == null)
        {
            Logging.Warn($"Subquest {req.SubQuestId} not found.");
            return response;
        }

        var conversation = GameData.Instance.Messages.FirstOrDefault(x =>
            x.Value.ConversationId == opener.Value.ConversationId &&
            x.Value.IsOpener);

        if (conversation.Value == null)
        {
            conversation = GameData.Instance.Messages
                .Where(x => x.Value.ConversationId == opener.Value.ConversationId)
                .OrderBy(x => x.Key)
                .FirstOrDefault();
        }

        if (conversation.Value == null)
        {
            Logging.Warn($"Subquest {req.SubQuestId} not found.");
            return response;
        }

        var existingMessage = repo.GetLatestTrackedMessageInConversation(userId, opener.Value.ConversationId ?? "");
        if (existingMessage != null)
        {
            response.Message = SocialMailRepository.MapToNetMessage(existingMessage);
        }
        else
        {
            response.Message = repo.CreateMessage(userId, conversation.Value.ConversationId ?? "", conversation.Value.Id ?? "");
            db.SaveChanges();
        }

        return response;
    }

    public ResEnterSubQuestFinMessengerDialog EnterFinishSubquest(User user, ReqEnterSubQuestFinMessengerDialog req)
    {
        var response = new ResEnterSubQuestFinMessengerDialog();

        var opener = GameData.Instance.Subquests.FirstOrDefault(x => x.Key == req.SubQuestId);
        if (opener.Value == null)
        {
            Logging.Warn($"Subquest {req.SubQuestId} not found.");
            return response;
        }

        var conversation = GameData.Instance.Messages.FirstOrDefault(x =>
            x.Value.ConversationId == opener.Value.EndMessengerConversationId && x.Value.IsOpener);

        if (conversation.Value == null)
        {
            Logging.Warn($"End conversation for subquest {req.SubQuestId} not found.");
            return response;
        }

        bool isCompleted = progressRepo.GetSubQuests(user.ID).TryGetValue(req.SubQuestId, out bool done) && done;
        var convMessages = repo.GetTrackedMessagesByConversation(user.ID, opener.Value.EndMessengerConversationId ?? "");
        bool changed = false;

        if (isCompleted && !string.IsNullOrEmpty(opener.Value.EndMessengerConversationId))
        {
            foreach (var msg in convMessages)
            {
                if (msg.State != 2)
                {
                    msg.State = 2;
                    changed = true;
                }
            }
        }

        var existingMessage = convMessages
            .OrderByDescending(message => message.Seq)
            .FirstOrDefault();

        if (existingMessage != null)
        {
            if (isCompleted && existingMessage.State != 2)
            {
                existingMessage.State = 2;
                changed = true;
            }
            if (changed)
                db.SaveChanges();

            response.Message = SocialMailRepository.MapToNetMessage(existingMessage);
        }
        else
        {
            int state = isCompleted ? 2 : (conversation.Value.MessageType == MessengerMessageType.Reward ? 1 : 0);
            response.Message = repo.CreateMessage(user.ID, conversation.Value.ConversationId ?? "", conversation.Value.Id ?? "", state);
            db.SaveChanges();
        }

        return response;
    }

    public ResFinSubQuest FinishSubquest(User user, ReqFinSubQuest req)
    {
        var response = new ResFinSubQuest();

        var subQuestEntry = GameData.Instance.Subquests.FirstOrDefault(x => x.Key == req.SubQuestId);
        var conversationEntry = GameData.Instance.Messages.FirstOrDefault(x => x.Value.Id == req.MessageId);

        int rewardId = conversationEntry.Value?.RewardId ?? 0;
        if (rewardId == 0 && subQuestEntry.Value != null)
        {
            // Fallback: look for the reward dialog in the subquest end conversation
            var rewardDialog = GameData.Instance.Messages.Values.FirstOrDefault(m =>
                m.ConversationId == subQuestEntry.Value.EndMessengerConversationId && m.RewardId != 0);
            if (rewardDialog != null)
                rewardId = rewardDialog.RewardId;
        }

        progressRepo.UpsertSubQuest(user.ID, req.SubQuestId, true);

        List<UserMessage> endConvMessages = subQuestEntry.Value != null && !string.IsNullOrEmpty(subQuestEntry.Value.EndMessengerConversationId)
            ? repo.GetTrackedMessagesByConversation(user.ID, subQuestEntry.Value.EndMessengerConversationId)
            : [];

        UserMessage? conversationRecordUser = repo.GetTrackedMessageByMessageId(user.ID, req.MessageId)
            ?? endConvMessages.FirstOrDefault(x => x.State != 0);

        if (conversationRecordUser != null)
        {
            if (conversationRecordUser.State == 2)
            {
                // already claimed, don't grant the reward again
                // Ensure all messages in this conversation stay marked as claimed (State = 2)
                foreach (var msg in endConvMessages)
                {
                    if (msg.State != 2)
                    {
                        msg.State = 2;
                    }
                }
                if (db.ChangeTracker.HasChanges())
                    db.SaveChanges();

                UserSessionStore.SaveUser(user);
                return response;
            }
            conversationRecordUser.State = 2; // mark as claimed
        }

        // Mark all messages in this end conversation as State = 2 (claimed/completed)
        foreach (var msg in endConvMessages)
        {
            msg.State = 2;
        }
        db.SaveChanges();

        if (subQuestEntry.Value != null)
        {
            if (subQuestEntry.Value.ClearTrigger != Trigger.None)
            {
                user.AddTrigger(subQuestEntry.Value.ClearTrigger, subQuestEntry.Value.ClearConditionValue, subQuestEntry.Value.ClearConditionId);
            }
            user.AddTrigger(Trigger.SubQuestClear, 1, req.SubQuestId);
        }

        if (rewardId != 0)
        {
            RewardRecord? rewardRecord = GameData.Instance.GetRewardTableEntry(rewardId);
            if (rewardRecord != null)
            {
                response.Reward = RewardUtils.RegisterRewardsForUser(user, rewardRecord);
            }
        }

        // Reconcile eligible subquests (e.g. next subquest in chain)
        MessengerMessageCreator.CreateEligibleSubquestOpeners(user);

        UserSessionStore.SaveUser(user);
        return response;
    }

    public ResPickTodayDailyMessage PickDailyMessages(User user, ReqPickTodayDailyMessage req)
    {
        PickMessages(user, req.ConditionTriggerIds, MessageType.DailyMessage, "Daily");
        return new ResPickTodayDailyMessage();
    }

    public ResPickTodayRandomMessage PickRandomMessages(User user, ReqPickTodayRandomMessage req)
    {
        PickMessages(user, req.ConditionTriggerIds, MessageType.RandomMessage, "Random");
        return new ResPickTodayRandomMessage();
    }

    private void PickMessages(User user, IEnumerable<int> conditionTriggerIds, MessageType messageType, string label)
    {
        var existingConvIds = repo.GetConversationIds(user.ID);
        var existingPicks = repo.GetPickedMessages(user.ID);

        List<int> eligibleIds = [];
        foreach (int conditionId in conditionTriggerIds)
        {
            if (!GameData.Instance.MessageConditions.TryGetValue(conditionId, out MessengerConditionTriggerRecord? record))
                continue;

            if (record.MessageType != messageType)
                continue;

            if (!MessengerTriggerUtils.IsTriggerListSatisfied(user, record.TriggerList))
                continue;

            if (existingConvIds.Contains(record.Tid))
                continue;

            eligibleIds.Add(conditionId);
        }

        int maxPicks = Math.Min(3, eligibleIds.Count);
        Random.Shared.Shuffle(CollectionsMarshal.AsSpan(eligibleIds));
        List<int> selected = eligibleIds.Take(maxPicks).ToList();

        DateTime now = DateTime.UtcNow;
        Timestamp nowTimestamp = Timestamp.FromDateTime(now);
        bool anyPicked = false;

        foreach (int conditionId in selected)
        {
            if (!GameData.Instance.MessageConditions.TryGetValue(conditionId, out MessengerConditionTriggerRecord? record))
                continue;

            bool alreadyPicked = existingPicks.Any(p =>
                p.ConversationId == record.Tid &&
                p.CreatedAt != null &&
                p.CreatedAt.ToDateTime().Date == now.Date);

            if (alreadyPicked)
                continue;

            repo.UpsertPickedMessage(user.ID, record.Tid, nowTimestamp);
            MessengerMessageCreator.CreateForCondition(user, record);
            anyPicked = true;

            Logging.WriteLine($"[Messenger] {label} pick: user {user.ID} picked condition {conditionId} (Tid={record.Tid})", LogType.Info);
        }

        if (anyPicked)
            db.SaveChanges();
    }
}
