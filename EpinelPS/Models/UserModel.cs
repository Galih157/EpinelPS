using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
using Newtonsoft.Json;

namespace EpinelPS.Models;

public class ClearedTutorialData
{
    public int Id { get; set; }
    public int VersionGroup;
}
public class User
{
    // User info
    [Obsolete]
    public string? Username { get; set; }
    [Obsolete]
    public string? Password { get; set; }
    [Obsolete]
    public string? PlayerName { get; set; }
    public ulong ID { get; set; }
    [Obsolete]
    public long RegisterTime { get; set; }
    public int LastNormalStageCleared { get; set; }
    public int LastStoryStageCleared { get; set; }
    public int LastHardStageCleared { get; set; }
    public int LastClearedDifficulty { get; set; }
    [Obsolete]
    public string? Nickname { get; set; }
    public string Description { get; set; } = "";
    public int ProfileIconId { get; set; } = 30100;
    public bool ProfileIconIsPrism { get; set; } = false;
    public int ProfileFrame { get; set; } = 1;
    [Obsolete]
    public bool IsAdmin { get; set; } = false;
    public bool sickpulls { get; set; } = false;
    public bool IsBanned { get; set; } = false;
    public int TitleId { get; set; } = 1;
    public DateTime BanStart { get; set; }
    public DateTime BanEnd { get; set; }
    public int BanId { get; set; } = 0;
    public DateTime LastReset { get; set; } = DateTime.MinValue;
    public DateTime LastWeeklyReset { get; set; } = DateTime.MinValue;

    // Game data
    [JsonIgnore] public Dictionary<CurrencyType, long> Currency { get; set; } = new() {
            { CurrencyType.ContentStamina, 2 }
        };

    [JsonIgnore] public List<DbItemData> Items { get; set; } = [];
    [JsonIgnore] public List<CharacterModel> Characters { get; set; } = [];

    [JsonIgnore] public UserPointData userPointData { get; set; } = new();
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    public DateTime BattleTime { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    [Obsolete]
    public List<TriggerModel> Triggers { get; set; } = [];
    public int LastTriggerId { get; set; } = 1;
    public ulong LastMessageId { get; set; } = 1;
    public long LastBadgeSeq { get; set; } = 1;

    [JsonIgnore] public GuildData Guild { get; set; } = new();

    public int CurCharacterIdId { get; set; } = 0;
    
    public TriggerModelNew AddTrigger(Trigger type, int value, int conditionId = 0, bool logToConsole = true)
    {
        using var ctx = GameContext.CreateNew();
        TriggerModelNew t = new()
        {
            Type = type,
            ConditionId = conditionId,
            CreatedAt = DateTime.UtcNow.AddHours(9).Ticks,
            Value = value
        };

        var gameUser = ctx.Users.Find(ID) ?? throw new InvalidDataException("user not found in Users table");
        gameUser.Triggers.Add(t);
        ctx.SaveChanges();

        // Messenger creation must evaluate the exact trigger that was just
        // persisted. Passing only the trigger type can miss conditions whose
        // ConditionId/Value are specific to this event.
        MessengerMessageCreator.OnTriggerAdded(this, t, logToConsole);

        return t;
    }

    public BadgeModel AddBadge(BadgeContents type, string location)
    {
        using var ctx = GameContext.CreateNew();
        var repo = new Repositories.SocialMailRepository(ctx);
        var badge = repo.AddBadge(ID, type, location);
        ctx.SaveChanges();
        return badge;
    }


    public void AddUnique<T>(List<T> list, T item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }

    public int GenerateUniqueItemId()
    {
        var num = Rng.RandomId();

        while (Items.Any(x => x.Isn == num))
        {
            num = Rng.RandomId();
        }

        return num;
    }
    public int GenerateUniqueCharacterId()
    {
        var num = Rng.RandomId();

        while (Characters.Any(x => x.Csn == num))
        {
            num = Rng.RandomId();
        }

        return num;
    }
    public long GetCurrencyVal(CurrencyType type)
    {
        if (Currency.TryGetValue(type, out long value))
            return value;
        else
        {
            Currency.Add(type, 0);
            return 0;
        }
    }
    public void AddCurrency(CurrencyType type, long val)
    {
        if (!Currency.TryAdd(type, val)) Currency[type] += val;
    }
    public bool SubtractCurrency(CurrencyType type, long val)
    {
        if (type == CurrencyType.FreeCash)
        {
            if (Currency.ContainsKey(type))
            {
                if (Currency[type] < val)
                {
                    long diff = val - Currency[type];
                    if (Currency.ContainsKey(CurrencyType.ChargeCash))
                    {
                        if (Currency[CurrencyType.ChargeCash] > diff)
                        {
                            Currency[type] = 0;
                            Currency[CurrencyType.ChargeCash] -= diff;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
        if (Currency.ContainsKey(type)) Currency[type] -= val;
        else return false;

        if (Currency[type] < 0)
        {
            Currency[type] += val;
            return false;
        }
        return true;
    }
    public bool CanSubtractCurrency(CurrencyType type, long val)
    {
        if (Currency.ContainsKey(type))
        {
            if (Currency[type] >= val) return true;
            else return false;
        }
        else
        {
            if (val == 0) return true;
            else return false;
        }
    }

    public bool HasCharacter(int c)
    {
        // Step 1: Get the 'NameCode' of the input character with Tid 'c'
        if (GameData.Instance.CharacterTable.TryGetValue(c, out var inputCharacterRecord))
        {
            int targetNameCode = inputCharacterRecord.NameCode;
            // Step 2: Find all character IDs in 'characterTable' that have the same 'NameCode'
            var matchingCharacterIds = GameData.Instance.CharacterTable.Where(kvp => kvp.Value.NameCode == targetNameCode).Select(kvp => kvp.Key).ToHashSet();

            // Step 3: Check if any of your owned characters have a 'Tid' in the set of matching IDs
            return Characters.Any(ownedCharacter => matchingCharacterIds.Contains(ownedCharacter.Tid));

        }
        else
        {   // The character with Tid 'c' does not exist in 'characterTable'
            return false;
        }
    }

    public CharacterModel? GetCharacter(int c)
    {
        // Step 1: Get the 'NameCode' of the input character with Tid 'c'
        if (GameData.Instance.CharacterTable.TryGetValue(c, out var inputCharacterRecord))
        {
            int targetNameCode = inputCharacterRecord.NameCode;
            // Step 2: Find all character IDs in 'characterTable' that have the same 'NameCode'
            var matchingCharacterIds = GameData.Instance.CharacterTable.Where(kvp => kvp.Value.NameCode == targetNameCode).Select(kvp => kvp.Key).ToHashSet();

            // Step 3: Check if any of your owned characters have a 'Tid' in the set of matching IDs
            return Characters.Where(ownedCharacter => matchingCharacterIds.Contains(ownedCharacter.Tid)).FirstOrDefault();

        }
        else
        {   // The character with Tid 'c' does not exist in 'characterTable'
            return null;
        }
    }

    public CharacterModel? GetCharacterBySerialNumber(long value)
    {
        if (value == 0) return null;
        return Characters.Where(x => x.Csn == value).FirstOrDefault();
    }

    internal bool GetSynchro(long csn)
    {
        using var ctx = GameContext.CreateNew();
        return new Repositories.OutpostRepository(ctx).GetSynchroSlots(ID).Any(x => x.CharacterSerialNumber == csn);
    }
    internal int GetCharacterLevel(int csn)
    {
        var c = GetCharacterBySerialNumber(csn) ?? throw new Exception("failed to lookup character");
        return GetCharacterLevel(csn, c.Level);
    }
    internal int GetCharacterLevel(int csn, int characterLevel)
    {
        using var ctx = GameContext.CreateNew();
        var repo = new Repositories.OutpostRepository(ctx);
        if (repo.GetSynchroSlots(ID).Any(x => x.CharacterSerialNumber == csn))
            return repo.GetSynchroLevel(ID, Characters);
        return characterLevel;
    }
    internal int GetSynchroLevel()
    {
        using var ctx = GameContext.CreateNew();
        return new Repositories.OutpostRepository(ctx).GetSynchroLevel(ID, Characters);
    }

    internal int GetMaxAttractiveLevel(int nameCode)
    {
        int maxStars = 0;
        foreach (var c in Characters)
        {
            if (GameData.Instance.CharacterTable.TryGetValue(c.Tid, out var cr) && cr.NameCode == nameCode)
            {
                int stars = Math.Min(c.Grade, 3);
                if (stars > maxStars) maxStars = stars;
            }
        }
        int cap = 10 + Math.Min(maxStars, 2) * 10;
        foreach (var kvp in GameData.Instance.CharacterTable.Values)
        {
            if (kvp.NameCode == nameCode && (kvp.Corporation == CorporationType.PILGRIM || kvp.CorporationSubType == CorporationSubType.OVERSPEC) && maxStars >= 3)
            {
                cap = 40;
                break;
            }
        }
        return cap;
    }

    /// <summary>
    /// Removes the specified amount of items by their ID. Returns the amount of items removed.
    /// </summary>
    /// <param name="isn"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int RemoveItemBySerialNumber(long isn, int count)
    {
        int removed = 0;
        foreach (var item in Items.ToList())
        {
            if (count == 0)
                break;

            if (item.Isn == isn)
            {
                removed++;
                item.Count -= count;

                if (item.Count < 0)
                {
                    item.Count = 0;
                }
            }
        }

        return removed;
    }

    public NetMessage CreateMessage(MessengerDialogRecord r, int state = 0)
        => CreateMessage(r.ConversationId ?? "", r.Id ?? "", state);

    public NetMessage CreateMessage(string conversationId, string messageId, int state = 0)
    {
        using var ctx = GameContext.CreateNew();
        var repo = new Repositories.SocialMailRepository(ctx);
        var msg = repo.CreateMessage(ID, conversationId, messageId, state);
        ctx.SaveChanges();
        return msg;
    }

    private bool ShouldResetUser()
    {
        var nowLocal = DateTime.UtcNow;

        // Compute the last reset threshold (most recent 2 PM before or at nowLocal)
        DateTime todayResetTime = new(
            nowLocal.Year,
            nowLocal.Month,
            nowLocal.Day,
            ServerConfigStore.ResetHourUtcTime, 0, 0
        );

        if (nowLocal < todayResetTime)
        {
            todayResetTime = todayResetTime.AddDays(-1);
        }

        // If user's last reset was before the last scheduled 2 PM, they need reset
        return UserDbHelper.GetOrCreateGameUser(ID).LastReset < todayResetTime;
    }

    private bool ShouldResetWeekly()
    {
        var nowLocal = DateTime.UtcNow;

        // Calculate the most recent Tuesday reset time
        DayOfWeek currentDay = nowLocal.DayOfWeek;
        int daysSinceTuesday = ((int)currentDay - (int)DayOfWeek.Tuesday + 7) % 7;

        // Get the date of the most recent Tuesday
        DateTime thisTuesday = nowLocal.Date.AddDays(-daysSinceTuesday);

        // Compute the weekly reset time
        DateTime weeklyResetTime = new(
            thisTuesday.Year,
            thisTuesday.Month,
            thisTuesday.Day,
            ServerConfigStore.ResetHourUtcTime, 0, 0
        );

        // If nowLocal is before the weekly reset time, subtract a week
        if (nowLocal < weeklyResetTime)
        {
            weeklyResetTime = weeklyResetTime.AddDays(-7);
        }

        // If user's last reset was before the last scheduled 2 PM, they need reset
        return UserDbHelper.GetOrCreateGameUser(ID).LastWeeklyReset < weeklyResetTime;
    }

    /*public void ResetDataIfNeeded()
    {
        if (!ShouldResetUser()) return;

        Logging.WriteLine("Resetting user...", LogType.Warning);

        LastReset = DateTime.UtcNow;
        ResetableData = new();
        UserSessionStore.SaveUser(this);
    }*/

    public void ResetDataIfNeeded()
    {
        bool needsSave = false;
        InfraCoreGradeRecord? infracore;
        using (var ctx = GameContext.CreateNew())
        {
            var outpostRepo = new Repositories.OutpostRepository(ctx);
            var (infraExp, infraLvl) = outpostRepo.GetInfraCore(ID);
            if (infraExp > 0)
            {
                int correctLvl = GameData.Instance.GetInfraCoreLev(infraExp);
                if (infraLvl < correctLvl)
                {
                    infraLvl = correctLvl;
                    outpostRepo.SetInfraCore(ID, infraExp, infraLvl);
                }
            }

            infracore = GameData.Instance.GetInfracoreGrade(infraLvl);
        }

        // Check weekly reset
        if (ShouldResetWeekly())
        {
            Logging.WriteLine("Resetting weekly user data...", LogType.Warning);

            UserDbHelper.UpdateGameUser(ID, u => u.LastWeeklyReset = DateTime.UtcNow);
            using (var ctx = GameContext.CreateNew())
            {
                new Repositories.ResetRepository(ctx).PerformWeeklyReset(ID);
                ctx.SaveChanges();
            }

            // Weekly stamina reset: base 2 + InfraCore bonus (FL[4] = StaminaMaxCount)
            if (infracore != null)
                Currency[CurrencyType.ContentStamina] = 2 + infracore.FunctionList[4].Function;

            needsSave = true;
        }

        // Check daily reset
        if (ShouldResetUser())
        {
            Logging.WriteLine("Resetting daily user data...", LogType.Warning);

            UserDbHelper.UpdateGameUser(ID, u => u.LastReset = DateTime.UtcNow);
            int dispatchCount = GetDispatchCount() + infracore.FunctionList[1].Function;
            int counselCount = 3 + infracore.FunctionList[2].Function;
            using (var ctx = GameContext.CreateNew())
            {
                var outpostRepo = new Repositories.OutpostRepository(ctx);
                var dispatchState = outpostRepo.GetDispatchState(ID);
                dispatchState.DispatchResetCount = 0;
                outpostRepo.SaveDispatchState(ID, dispatchState);

                new Repositories.ResetRepository(ctx).PerformDailyReset(ID, dispatchCount, counselCount);
                new Repositories.GachaShopPassRepository(ctx).ResetDailyGacha(ID);
                ctx.SaveChanges();
            }

            needsSave = true;
        }

        if (needsSave)
        {
            UserSessionStore.SaveUser(this);
        }
    }
    public int GetDateDay()
    {
        // +4 每天4点重新计算 yyyyMMdd
        DateTime dateTime = DateTime.UtcNow.AddHours(4);
        return dateTime.Year * 10000 + dateTime.Month * 100 + dateTime.Day;
    }

    public int GetDispatchCount()
    {
        using var ctx = GameContext.CreateNew();
        var dispatchState = new Repositories.OutpostRepository(ctx).GetDispatchState(ID);

        int dis1 = GameData.Instance.DispatchBoardTable.Values
            .Where(x => x.DispatchType == DispatchType.Dispatch && x.DispatchBoardLv == dispatchState.DispatchLv).FirstOrDefault().DispatchMax;
        int dis2 = GameData.Instance.DispatchBoardTable.Values.Where(x => x.DispatchType == DispatchType.DispatchCollection && x.DispatchBoardLv == dispatchState.DispatchCollectionLv).FirstOrDefault()?.DispatchMax ?? 0;
        int dis3 = GameData.Instance.DispatchBoardTable.Values
            .Where(x => x.DispatchType == DispatchType.DispatchFavorite && x.DispatchBoardLv == dispatchState.DispatchFavoriteLv).FirstOrDefault()?.DispatchMax ?? 0;

        return dis1 + dis2 + dis3;
    }
    public static int GenerateMsn()
    {
        long timestamp = DateTime.UtcNow.Ticks;
        int seed = (int)(timestamp & 0x7FFFFFFF) ^ (int)(timestamp >> 32);
        var random = new Random(seed);
        return random.Next(100000000, 1000000000);
    }
}

public class MessengerRepairAuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = "";
    public string Note { get; set; } = "";
    public NetMessage Message { get; set; } = new();
}
