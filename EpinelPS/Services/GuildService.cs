using EpinelPS.Database;
using EpinelPS.LobbyServer;
using Google.Protobuf.WellKnownTypes;

namespace EpinelPS.Services;

public sealed class GuildService(GameContext db)
{
    private static readonly int[] ShootingRangeIds = [1, 3, 21, 5, 7, 23, 9, 11, 25, 13, 15, 27, 17, 19, 29];

    private static NetGuildData CreateDefaultGuild(int joinLevel = 1) => new()
    {
        Gsn = 10001,
        Name = "Counters",
        Emblem = 1,
        Locale = "en",
        JoinType = 0,
        JoinLevel = joinLevel,
        Grade = 3,
        UnionRaidTier = 6,
        UnionRaidTierNumber = 0
    };

    private static NetUnionRaidGuildInfo CreateDefaultUnionRaidGuildInfo() => new()
    {
        Gsn = 10001,
        AchieveLevel = 1,
        AchieveStep = 1,
        LeftHp = 100000000
    };

    private static NetUnionRaidJoinData CreateDefaultUnionRaidJoinData() => new()
    {
        PlayCount = 0,
        HardPlayCount = 0
    };

    private static NetShootingRangeRankingData CreateDefaultShootingRangeEntry(NetWholeUserData wholeUser) => new()
    {
        Rank = 1,
        Score = 0,
        User = wholeUser
    };

    // --- Guild ---

    public ResCreateGuild CreateGuild()
    {
        // Safe no-op: acknowledge request with success, no guild mutation or save corruption
        return new ResCreateGuild
        {
            Result = CreateGuildResult.Okay,
            Guild = CreateDefaultGuild()
        };
    }

    public ResGetGuild GetGuild(User user, ReqGetGuild req)
    {
        ResGetGuild response = new()
        {
            CanChangeUnionNameForFree = true,
            IsSimple = req.IsSimple,
            Guild = CreateDefaultGuild()
        };

        var gameUser = UserDbHelper.GetOrCreateGameUser(db, user.ID);
        var sdkUser = db.SdkUsers.Find(user.ID);
        DateTime now = DateTime.UtcNow;

        response.Members.Add(new NetGuildMemberData
        {
            Usn = (long)user.ID,
            Type = 1,
            JoinedAt = now.Ticks,
            Nickname = gameUser.Nickname ?? sdkUser?.PlayerName ?? "Commander",
            Lv = user.userPointData?.UserLevel ?? 1,
            Server = 1001,
            LastActionAt = now.Ticks,
            Icon = gameUser.ProfileIconId > 0 ? gameUser.ProfileIconId : 30100,
            Frame = gameUser.ProfileFrame > 0 ? gameUser.ProfileFrame : 1,
            TeamCombat = 100000,
            UserTitleId = gameUser.TitleId > 0 ? gameUser.TitleId : 1,
            UserTitleDisplayData = new NetUserTitleDisplayData { Count = 0 },
            SendMailAt = Timestamp.FromDateTime(now)
        });

        return response;
    }

    public ResSearchGuildList SearchGuild()
    {
        ResSearchGuildList response = new();
        response.Guilds.Add(new NetSimpleGuildData
        {
            Gsn = 10001,
            Name = "Counters",
            Emblem = 1,
            Grade = 3,
            MemberCount = 1,
            UnionRaidTier = 6,
            UnionRaidTierNumber = 0
        });
        return response;
    }

    public ResGetGuildPublicInfo GetGuildPublicInfo()
    {
        return new ResGetGuildPublicInfo
        {
            Guild = CreateDefaultGuild(joinLevel: 0)
        };
    }

    public ResLeaveGuild LeaveGuild()
    {
        return new ResLeaveGuild
        {
            Result = LeaveGuildResult.Ok,
            GuildLeaveAt = DateTime.UtcNow.Ticks
        };
    }

    public ResUpdateGuildNotice UpdateGuildNotice()
    {
        return new ResUpdateGuildNotice
        {
            Result = SetGuildNoticeResult.Okay,
            BanEndTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };
    }

    public ResUpdateGuildSettings2 UpdateGuildSettings()
    {
        return new ResUpdateGuildSettings2
        {
            Result = SetGuildDescResult.Okay,
            BanEndTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };
    }

    // --- UnionRaid ---

    public ResGetUnionRaidData GetUnionRaidData()
    {
        DateTime now = DateTime.UtcNow;

        return new ResGetUnionRaidData
        {
            Data = new NetUnionRaidData
            {
                UnionRaidState = UnionRaidState.Ready,
                UnionRaidManagerTid = 1000001,
                TotalRank = 999999,
                NormalClearDay = 0,
                CurrentStatus = CreateDefaultUnionRaidGuildInfo(),
                UnionRaidJoinData = CreateDefaultUnionRaidJoinData(),
                PeriodData = new NetUnionRaidPeriodData
                {
                    VisibleDate = now.AddDays(-1).Ticks,
                    StartDate = now.AddDays(7).Ticks,
                    EndDate = now.AddDays(14).Ticks,
                    SettleDate = now.AddDays(15).Ticks,
                    DisableDate = now.AddDays(21).Ticks
                },
                UnionRaidRanking = new NetUnionRaidRankInfo
                {
                    Ranking = 999999,
                    TotalDamage = 0,
                    NormalClearDay = 0
                }
            }
        };
    }

    public ResGetUnionRaidStepInfo GetUnionRaidStepInfo()
    {
        return new ResGetUnionRaidStepInfo
        {
            Info = CreateDefaultUnionRaidGuildInfo(),
            PeriodResult = UnionRaidPeriodResult.Success,
            NormalClearDay = 0
        };
    }

    public ResGetUnionRaidJoinData GetUnionRaidJoinData()
    {
        return new ResGetUnionRaidJoinData
        {
            UnionRaidJoinData = CreateDefaultUnionRaidJoinData()
        };
    }

    // --- ShootingRange ---

    public ResGetShootingRangeRankingV2 GetShootingRangeRanking(User user)
    {
        ResGetShootingRangeRankingV2 response = new()
        {
            Result = ShootingRangeRankingResult.GetShootingRangeRankingResultSuccess,
            IsBan = false
        };

        var wholeUser = LobbyHandler.CreateWholeUserDataFromDbUser(user);

        // 15 distinct (Element, Distance) modes
        foreach (int rangeId in ShootingRangeIds)
        {
            var totalData = new NetShootingRangeRankingTotalData
            {
                ShootingRangeId = rangeId,
                UserGuildRanking = CreateDefaultShootingRangeEntry(wholeUser)
            };

            totalData.GuildRankingList.Add(CreateDefaultShootingRangeEntry(wholeUser));
            response.RankingList.Add(totalData);
        }

        return response;
    }

    public ResSetShootingRangeScoreV2 SetShootingRangeScore(ReqSetShootingRangeScoreV2 req)
    {
        return new ResSetShootingRangeScoreV2
        {
            IsNewRecord = true,
            HighestScore = req.Score,
            Result = ShootingRangeRankingResult.GetShootingRangeRankingResultSuccess,
            IsBan = false
        };
    }
}
