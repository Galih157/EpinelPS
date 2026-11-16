using EpinelPS.Data;
using EpinelPS.Repositories;

namespace EpinelPS.LobbyServer.Outpost.Dispatch;

public class DispatchHelper
{
    public static T SelectItemByProbability<T>(List<T> items, Func<T, double> probabilitySelector)
    {
        if (!items.Any())
            return default!;

        double totalProb = items.Sum(probabilitySelector);

        if (totalProb <= 0)
            return default!;

        double randomValue = Random.Shared.NextDouble() * totalProb;

        double cumulativeProb = 0;

        foreach (var item in items)
        {
            cumulativeProb += probabilitySelector(item);

            if (randomValue <= cumulativeProb)
            {
                return item;
            }
        }

        return items.Last();
    }

    public static void SyncDispatchLevel(ulong userId, OutpostRepository repo)
    {
        var completed = repo.GetCompletedTacticAcademyLessons(userId);
        int count = 0;
        foreach (var lid in completed)
        {
            var lesson = GameData.Instance.GetTacticAcademyLesson(lid);
            if (lesson != null && lesson.LessonType == LessonType.Dispatch)
                count++;
        }

        int newLv = Math.Max(1, count);
        var state = repo.GetDispatchState(userId);
        if (newLv != state.DispatchLv)
        {
            state.DispatchLv = newLv;
            state.DispatchCollectionLv = newLv;
            state.DispatchFavoriteLv = newLv;
            state.Today = 0;
            repo.SaveDispatchState(userId, state);
        }
    }
}
