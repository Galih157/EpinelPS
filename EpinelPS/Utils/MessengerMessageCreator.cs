using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.LobbyServer.Messenger;
using EpinelPS.Models;
using EpinelPS.Repositories;

namespace EpinelPS.Utils;

/// <summary>
/// Creates Messenger opener records when a real gameplay event makes a fixed
/// conversation eligible. This is deliberately called from event handlers,
/// never from /messenger/get.
/// </summary>
public static class MessengerMessageCreator
{
    public static void OnTriggerAdded(User user, TriggerModelNew currentTrigger, bool logToConsole = true)
    {
        using var ctx = GameContext.CreateNew();
        var repo = new SocialMailRepository(ctx);
        var existingConvIds = repo.GetConversationIds(user.ID);
        var clearedConditionIds = LoadClearedMessageConditions(ctx, user.ID);
        var subQuests = new ProgressRepository(ctx).GetSubQuests(user.ID);

        int sameTypeCandidates = 0;
        int exactCandidates = 0;
        int satisfiedCandidates = 0;
        int createdCount = 0;

        foreach (MessengerConditionTriggerRecord condition in GameData.Instance.MessageConditions.Values.OrderBy(c => c.Id))
        {
            if (condition.MessageType != MessageType.Message || string.IsNullOrEmpty(condition.Tid))
                continue;

            // Empty conditions are intentionally not auto-created. They are
            // commonly room/group definitions and must be entered explicitly.
            if (condition.TriggerList == null || condition.TriggerList.Count == 0)
                continue;

            if (!condition.TriggerList.Any(item => item.Trigger == currentTrigger.Type))
                continue;

            sameTypeCandidates++;

            if (!condition.TriggerList.Any(item =>
                    item.Trigger == currentTrigger.Type &&
                    item.ConditionId == currentTrigger.ConditionId &&
                    currentTrigger.Value >= item.ConditionValue))
                continue;

            exactCandidates++;
            if (!MessengerTriggerUtils.IsTriggerListSatisfied(user, condition.TriggerList))
            {
                Logging.WriteLine($"[Messenger] Trigger candidate not satisfied: user={user.ID}, Trigger={currentTrigger.Type}, ConditionId={currentTrigger.ConditionId}, MessengerCondition={condition.Id}, Tid={condition.Tid}", LogType.Debug, logToConsole);
                continue;
            }

            satisfiedCandidates++;
            if (CreateOpener(user, condition, repo, existingConvIds, clearedConditionIds, subQuests, logToConsole))
                createdCount++;
        }

        if (sameTypeCandidates > 0)
        {
            Logging.WriteLine($"[Messenger] Trigger evaluation: user={user.ID}, Trigger={currentTrigger.Type}, ConditionId={currentTrigger.ConditionId}, Value={currentTrigger.Value}, SameType={sameTypeCandidates}, Exact={exactCandidates}, Satisfied={satisfiedCandidates}, Created={createdCount}", LogType.Info, logToConsole);
        }

        createdCount += CreateEligibleSubquestOpeners(user, repo, existingConvIds, clearedConditionIds, subQuests, logToConsole);
        if (createdCount > 0)
            ctx.SaveChanges();
    }

    public static bool CreateForCondition(User user, MessengerConditionTriggerRecord condition, bool logToConsole = true)
    {
        if (condition.MessageType is MessageType.RandomMessage or MessageType.DailyMessage)
            return false;
        if (string.IsNullOrEmpty(condition.Tid))
            return false;
        if (condition.TriggerList == null || condition.TriggerList.Count == 0 ||
            !MessengerTriggerUtils.IsTriggerListSatisfied(user, condition.TriggerList))
            return false;

        using var ctx = GameContext.CreateNew();
        var repo = new SocialMailRepository(ctx);
        var existingConvIds = repo.GetConversationIds(user.ID);
        var clearedConditionIds = LoadClearedMessageConditions(ctx, user.ID);
        var subQuests = new ProgressRepository(ctx).GetSubQuests(user.ID);

        if (!CreateOpener(user, condition, repo, existingConvIds, clearedConditionIds, subQuests, logToConsole))
            return false;

        ctx.SaveChanges();
        return true;
    }

    /// <summary>
    /// Re-evaluates fixed conversations after a change which can unlock a
    /// room without itself appearing in the conversation's TriggerList (for
    /// example, granting the first character from a Squad through admin).
    /// This creates only normal conversations whose recorded progression and
    /// room requirements are already satisfied; it never writes triggers.
    /// Also reconciles eligible subquest openers and enrolls them.
    /// </summary>
    public static int CreateAllEligibleOpeners(User user, bool logToConsole = true)
    {
        using var ctx = GameContext.CreateNew();
        var repo = new SocialMailRepository(ctx);
        var existingConvIds = repo.GetConversationIds(user.ID);
        var clearedConditionIds = LoadClearedMessageConditions(ctx, user.ID);
        var subQuests = new ProgressRepository(ctx).GetSubQuests(user.ID);

        int createdCount = 0;

        foreach (MessengerConditionTriggerRecord condition in GameData.Instance.MessageConditions.Values.OrderBy(c => c.Id))
        {
            if (condition.MessageType is MessageType.RandomMessage or MessageType.DailyMessage)
                continue;
            if (string.IsNullOrEmpty(condition.Tid))
                continue;
            if (condition.TriggerList == null || condition.TriggerList.Count == 0 ||
                !MessengerTriggerUtils.IsTriggerListSatisfied(user, condition.TriggerList))
                continue;

            if (CreateOpener(user, condition, repo, existingConvIds, clearedConditionIds, subQuests, logToConsole))
                createdCount++;
        }

        createdCount += CreateEligibleSubquestOpeners(user, repo, existingConvIds, clearedConditionIds, subQuests, logToConsole);

        if (createdCount > 0)
        {
            ctx.SaveChanges();
        }

        Logging.WriteLine($"[Messenger] Eligible opener reconciliation: user={user.ID}, Created={createdCount}", LogType.Info, logToConsole);
        return createdCount;
    }

    /// <summary>
    /// Evaluates all subquests and creates starting openers in UserMessages
    /// for any subquest whose stage/quest triggers and prerequisite subquests are satisfied.
    /// Preserves subquests for players to activate and play in BlaBla.
    /// </summary>
    public static int CreateEligibleSubquestOpeners(User user, bool logToConsole = true)
    {
        using var ctx = GameContext.CreateNew();
        var repo = new SocialMailRepository(ctx);
        var existingConvIds = repo.GetConversationIds(user.ID);
        var clearedConditionIds = LoadClearedMessageConditions(ctx, user.ID);
        var subQuests = new ProgressRepository(ctx).GetSubQuests(user.ID);

        int createdCount = CreateEligibleSubquestOpeners(user, repo, existingConvIds, clearedConditionIds, subQuests, logToConsole);
        if (createdCount > 0)
            ctx.SaveChanges();

        return createdCount;
    }

    private static int CreateEligibleSubquestOpeners(
        User user,
        SocialMailRepository repo,
        HashSet<string> existingConvIds,
        HashSet<int> clearedConditionIds,
        IReadOnlyDictionary<int, bool> subQuests,
        bool logToConsole)
    {
        int createdCount = 0;

        foreach (SubQuestRecord subQuest in GameData.Instance.Subquests.Values.OrderBy(s => s.Id))
        {
            if (string.IsNullOrEmpty(subQuest.ConversationId))
                continue;

            // If subquest is already completed, do not re-create
            if (subQuests.TryGetValue(subQuest.Id, out bool completed) && completed)
                continue;

            // If opener message already exists, do not re-create
            if (existingConvIds.Contains(subQuest.ConversationId))
                continue;

            // Prerequisite check: If BeforeSubQuestId > 0, it must be completed
            if (subQuest.BeforeSubQuestId > 0)
            {
                if (!subQuests.TryGetValue(subQuest.BeforeSubQuestId, out bool prevDone) || !prevDone)
                    continue;
            }

            // Do not auto-unlock dummy subquests with no triggers and no prerequisite
            bool hasMeaningfulTrigger = subQuest.TriggerList != null && subQuest.TriggerList.Any(t => t.Trigger != Trigger.None);
            if (!hasMeaningfulTrigger && subQuest.BeforeSubQuestId == 0)
                continue;

            // Condition triggers check (e.g. CampaignClear of required stage)
            if (!MessengerTriggerUtils.IsTriggerListSatisfied(user, subQuest.TriggerList))
                continue;

            KeyValuePair<string, MessengerDialogRecord> opener = GameData.Instance.Messages.FirstOrDefault(item =>
                item.Value.ConversationId == subQuest.ConversationId && item.Value.IsOpener);
            if (opener.Value == null)
            {
                opener = GameData.Instance.Messages
                    .Where(item => item.Value.ConversationId == subQuest.ConversationId)
                    .OrderBy(item => item.Key)
                    .FirstOrDefault();
            }
            if (opener.Value == null)
                continue;

            if (!MessengerAccessValidator.IsRoomUnlockSatisfied(user, opener.Value.RoomId))
                continue;

            // Avoid spawning into a room that already has an active unread conversation
            if (!string.IsNullOrEmpty(opener.Value.RoomId) &&
                HasActiveUnclearedConversationInRoom(opener.Value.RoomId, existingConvIds, clearedConditionIds, subQuests))
                continue;

            // Create the starting opener message in BlaBla
            repo.CreateMessage(user.ID, opener.Value.ConversationId ?? "", opener.Value.Id ?? "");
            existingConvIds.Add(subQuest.ConversationId);

            createdCount++;
            Logging.WriteLine($"[Messenger] Created subquest opener: user={user.ID}, SubQuestId={subQuest.Id}, Tid={subQuest.ConversationId}", LogType.Info, logToConsole);
        }

        return createdCount;
    }

    /// <summary>
    /// Checks whether a room already has an active conversation that
    /// has not been entered/cleared by the user. Prevents dialogue interleaving and soft-locks.
    /// </summary>
    public static bool HasActiveUnclearedConversationInRoom(User user, string roomId)
    {
        if (string.IsNullOrEmpty(roomId))
            return false;

        using var ctx = GameContext.CreateNew();
        var repo = new SocialMailRepository(ctx);
        var existingConvIds = repo.GetConversationIds(user.ID);
        if (existingConvIds.Count == 0)
            return false;

        var clearedConditionIds = LoadClearedMessageConditions(ctx, user.ID);
        var subQuests = new ProgressRepository(ctx).GetSubQuests(user.ID);
        return HasActiveUnclearedConversationInRoom(roomId, existingConvIds, clearedConditionIds, subQuests);
    }

    private static bool HasActiveUnclearedConversationInRoom(
        string roomId,
        IReadOnlyCollection<string> existingConvIds,
        HashSet<int> clearedConditionIds,
        IReadOnlyDictionary<int, bool> subQuests)
    {
        if (string.IsNullOrEmpty(roomId) || existingConvIds.Count == 0)
            return false;

        foreach (string convId in existingConvIds)
        {
            var dialog = GameData.Instance.Messages.Values.FirstOrDefault(d => d.ConversationId == convId);
            if (dialog == null || dialog.RoomId != roomId)
                continue;

            var cond = GameData.Instance.MessageConditions.Values.FirstOrDefault(c => c.Tid == convId);
            if (cond != null)
            {
                if (!clearedConditionIds.Contains(cond.Id))
                    return true;
            }
            else
            {
                var sub = GameData.Instance.Subquests.Values.FirstOrDefault(s => s.ConversationId == convId);
                if (sub != null)
                {
                    bool isDone = subQuests.TryGetValue(sub.Id, out bool done) && done;
                    if (!isDone)
                        return true;
                }
            }
        }

        return false;
    }

    private static HashSet<int> LoadClearedMessageConditions(GameContext ctx, ulong userId)
    {
        return ctx.Triggers
            .Where(t => t.UserId == userId && t.Type == Trigger.MessageClear)
            .Select(t => t.ConditionId)
            .ToHashSet();
    }

    private static bool CreateOpener(
        User user,
        MessengerConditionTriggerRecord condition,
        SocialMailRepository repo,
        HashSet<string> existingConvIds,
        HashSet<int> clearedConditionIds,
        IReadOnlyDictionary<int, bool> subQuests,
        bool logToConsole = true)
    {
        if (existingConvIds.Contains(condition.Tid))
        {
            Logging.WriteLine($"[Messenger] Opener already exists: user={user.ID}, MessengerCondition={condition.Id}, Tid={condition.Tid}", LogType.Debug, logToConsole);
            return false;
        }

        KeyValuePair<string, MessengerDialogRecord> opener = GameData.Instance.Messages.FirstOrDefault(item =>
            item.Value.ConversationId == condition.Tid && item.Value.IsOpener);
        if (opener.Value == null)
        {
            Logging.WriteLine($"[Messenger] Opener is missing from static data: user={user.ID}, MessengerCondition={condition.Id}, Tid={condition.Tid}", LogType.Warning, logToConsole);
            return false;
        }

        if (!MessengerAccessValidator.IsRoomUnlockSatisfied(user, opener.Value.RoomId))
        {
            Logging.WriteLine($"[Messenger] Opener room is locked: user={user.ID}, MessengerCondition={condition.Id}, Tid={condition.Tid}, RoomId={opener.Value.RoomId}", LogType.Debug, logToConsole);
            return false;
        }

        // Avoid spawning multiple active unread conversations in the same room simultaneously.
        // A room should only have one active conversation at a time to prevent UI breaks.
        if (!string.IsNullOrEmpty(opener.Value.RoomId) &&
            HasActiveUnclearedConversationInRoom(opener.Value.RoomId, existingConvIds, clearedConditionIds, subQuests))
        {
            Logging.WriteLine($"[Messenger] Room {opener.Value.RoomId} already has an active unread conversation; postponing Tid={condition.Tid}", LogType.Debug, logToConsole);
            return false;
        }

        repo.CreateMessage(user.ID, opener.Value.ConversationId ?? "", opener.Value.Id ?? "");
        existingConvIds.Add(condition.Tid);
        Logging.WriteLine($"[Messenger] Created opener from trigger: user={user.ID}, Tid={condition.Tid}, RoomId={opener.Value.RoomId}", LogType.Info, logToConsole);
        return true;
    }
}
