using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Repositories;

namespace EpinelPS.LobbyServer.Messenger;

internal static class MessengerAccessValidator
{
    public static bool CanEnter(User user, MessengerConditionTriggerRecord condition, MessengerDialogRecord opener, SocialMailRepository? repo = null)
    {
        if (condition.MessageType == MessageType.RandomMessage || condition.MessageType == MessageType.DailyMessage)
        {
            bool picked = repo != null
                ? repo.HasPickedConversation(user.ID, condition.Tid)
                : WithRepo(r => r.HasPickedConversation(user.ID, condition.Tid));
            if (!picked)
            {
                return false;
            }
        }
        else
        {
            // If the conversation was already created and exists in UserMessages,
            // the user already legitimately has access. Do not deny entry.
            bool hasConversation = repo != null
                ? repo.HasConversation(user.ID, condition.Tid)
                : WithRepo(r => r.HasConversation(user.ID, condition.Tid));
            if (!hasConversation)
            {
                if (!MessengerTriggerUtils.IsTriggerListSatisfied(user, condition.TriggerList))
                {
                    return false;
                }
            }
        }

        return IsRoomUnlockSatisfied(user, opener.RoomId);
    }

    private static bool WithRepo(Func<SocialMailRepository, bool> check)
    {
        using var db = GameContext.CreateNew();
        return check(new SocialMailRepository(db));
    }

    internal static bool IsRoomUnlockSatisfied(User user, string? roomId)
    {
        if (string.IsNullOrEmpty(roomId) || !GameData.Instance.MessengerRooms.TryGetValue(roomId, out MessengerRoomRecord? room))
        {
            return true;
        }

        bool squadSatisfied = room.UnlockConditionSquad == Squad.None || user.Characters.Any(character =>
            GameData.Instance.CharacterTable.TryGetValue(character.Tid, out CharacterRecord? record) &&
            record.Squad == room.UnlockConditionSquad);

        bool characterSatisfied = room.UnlockConditionCharacter == 0 || user.Characters.Any(character =>
            GameData.Instance.CharacterTable.TryGetValue(character.Tid, out CharacterRecord? record) &&
            record.NameCode == room.UnlockConditionCharacter);

        return squadSatisfied && characterSatisfied;
    }
}
