using EpinelPS.Database;
using EpinelPS.Repositories;

namespace EpinelPS.LobbyServer.Event.Minigame.ScenarioChoice;

public class ScenarioChoiceMain
{
    private static ScenarioChoiceData Convert(MiniGameStoryChoice val)
    {
        var d = new ScenarioChoiceData();
        d.ChosenChoices.AddRange(val.Choices);
        d.ViewedScenarioSceneList.AddRange(val.SeenChoices);
        d.LastSeenEventScenarioDialogTableGroupId = val.SeenChoices.LastOrDefault() ?? "";

        return d;
    }

    public static ScenarioChoiceData GetData(ulong userId, int id, MinigameRepository repo, GameContext db)
    {
        if (repo.TryGetDictEntry<MiniGameStoryChoice>(userId, MinigameRepository.TypeMiniGameStoryChoice, id, out MiniGameStoryChoice? val)
            && val != null)
        {
            return Convert(val);
        }

        MiniGameStoryChoice c = new() { Id = id };
        repo.SaveDictEntry(userId, MinigameRepository.TypeMiniGameStoryChoice, id, c);
        db.SaveChanges();
        return Convert(c);
    }

    public static ScenarioChoiceData Update(
        ulong userId,
        int id,
        string scenarioScene,
        Google.Protobuf.Collections.RepeatedField<string> chosenChoices,
        MinigameRepository repo,
        GameContext db)
    {
        if (!repo.TryGetDictEntry<MiniGameStoryChoice>(userId, MinigameRepository.TypeMiniGameStoryChoice, id, out MiniGameStoryChoice? val)
            || val == null)
        {
            val = new MiniGameStoryChoice { Id = id };
        }

        val.Choices = chosenChoices.ToList();

        foreach (var item in val.Choices)
        {
            if (!val.SeenChoices.Contains(item))
                val.SeenChoices.Add(item);
        }

        repo.SaveDictEntry(userId, MinigameRepository.TypeMiniGameStoryChoice, id, val);
        db.SaveChanges();

        return Convert(val);
    }
}
