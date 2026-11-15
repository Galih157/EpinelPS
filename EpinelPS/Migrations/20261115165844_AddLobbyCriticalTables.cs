using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EpinelPS.Migrations
{
    /// <inheritdoc />
    public partial class AddLobbyCriticalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Csn = table.Column<long>(type: "INTEGER", nullable: false),
                    Tid = table.Column<int>(type: "INTEGER", nullable: false),
                    CostumeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimateLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Skill1Lvl = table.Column<int>(type: "INTEGER", nullable: false),
                    Skill2Lvl = table.Column<int>(type: "INTEGER", nullable: false),
                    Grade = table.Column<int>(type: "INTEGER", nullable: false),
                    IsMainForce = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.UniqueConstraint("AK_Characters_UserId_Csn", x => new { x.UserId, x.Csn });
                    table.ForeignKey(
                        name: "FK_Characters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Isn = table.Column<long>(type: "INTEGER", nullable: false),
                    ItemType = table.Column<int>(type: "INTEGER", nullable: false),
                    Csn = table.Column<long>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Exp = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Corp = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outposts",
                columns: table => new
                {
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DispatchLv = table.Column<int>(type: "INTEGER", nullable: false),
                    DispatchCollectionLv = table.Column<int>(type: "INTEGER", nullable: false),
                    DispatchFavoriteLv = table.Column<int>(type: "INTEGER", nullable: false),
                    DispatchResetCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DispatchToday = table.Column<int>(type: "INTEGER", nullable: false),
                    InfraCoreExp = table.Column<int>(type: "INTEGER", nullable: false),
                    InfraCoreLvl = table.Column<int>(type: "INTEGER", nullable: false),
                    SynchroDeviceUpgraded = table.Column<bool>(type: "INTEGER", nullable: false),
                    SynchroDeviceLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    OutpostBattleLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    OutpostBattleExp = table.Column<int>(type: "INTEGER", nullable: false),
                    OutpostBuffs = table.Column<string>(type: "TEXT", nullable: false),
                    OutpostConditionList = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outposts", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Outposts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserArchiveUnlocks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TargetId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserArchiveUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserArchiveUnlocks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBadges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Seq = table.Column<long>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    BadgeContent = table.Column<int>(type: "INTEGER", nullable: false),
                    BadgeGuid = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBadges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBonds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    NameCode = table.Column<int>(type: "INTEGER", nullable: false),
                    Lv = table.Column<int>(type: "INTEGER", nullable: false),
                    Exp = table.Column<int>(type: "INTEGER", nullable: false),
                    CanCounselToday = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFavorites = table.Column<bool>(type: "INTEGER", nullable: false),
                    CounseledCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CompleteRewardStatus = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBonds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBonds_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaimedJukeboxRewards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TriggerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaimedJukeboxRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaimedJukeboxRewards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClearedTutorials",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TutorialGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    TutorialId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionGroup = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClearedTutorials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClearedTutorials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompletedAchievements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AchievementId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompletedAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompletedAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompletedDailyMissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MissionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompletedDailyMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompletedDailyMissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompletedScenarios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ScenarioId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompletedScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompletedScenarios_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCompletedWeeklyMissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MissionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompletedWeeklyMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompletedWeeklyMissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserContentsUnlocks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ContentsOpenId = table.Column<int>(type: "INTEGER", nullable: false),
                    ButtonAnimationPlayed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PopupAnimationPlayed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContentsUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserContentsUnlocks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCurrencies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CurrencyType = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCurrencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCurrencies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDailyCounselCounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    KeyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDailyCounselCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDailyCounselCounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDailyTowerCounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CorporationType = table.Column<int>(type: "INTEGER", nullable: false),
                    UsedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDailyTowerCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDailyTowerCounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEventMissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastDay = table.Column<int>(type: "INTEGER", nullable: false),
                    LastDate = table.Column<long>(type: "INTEGER", nullable: false),
                    AllClear = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEventMissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    Diff = table.Column<int>(type: "INTEGER", nullable: false),
                    LastStage = table.Column<int>(type: "INTEGER", nullable: false),
                    LastDay = table.Column<int>(type: "INTEGER", nullable: false),
                    FreeTicket = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEventShopBuys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductTid = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventShopBuys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEventShopBuys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFavoriteItemQuests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    QuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Clear = table.Column<bool>(type: "INTEGER", nullable: false),
                    Received = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteItemQuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFavoriteItemQuests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFavoriteItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    FavoriteItemId = table.Column<long>(type: "INTEGER", nullable: false),
                    Tid = table.Column<int>(type: "INTEGER", nullable: false),
                    Csn = table.Column<long>(type: "INTEGER", nullable: false),
                    Lv = table.Column<int>(type: "INTEGER", nullable: false),
                    Exp = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFavoriteItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGachaBannerPulls",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BannerId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxPulls = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGachaBannerPulls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGachaBannerPulls_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGachaDailyFreePulls",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GachaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGachaDailyFreePulls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGachaDailyFreePulls_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGachaPaybacks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GachaId = table.Column<int>(type: "INTEGER", nullable: false),
                    GachaCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGachaPaybacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGachaPaybacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGachaPityCounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BannerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecuteCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGachaPityCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGachaPityCounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGachaSelectupChoices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BannerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterTid = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGachaSelectupChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGachaSelectupChoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserJukeboxSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Location = table.Column<int>(type: "INTEGER", nullable: false),
                    BgmType = table.Column<int>(type: "INTEGER", nullable: false),
                    TableId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsShuffle = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserJukeboxSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserJukeboxSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLiberations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Step = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgressPoint = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLiberations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLiberations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastDay = table.Column<int>(type: "INTEGER", nullable: false),
                    LastDate = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLostSectors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SectorId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPlaying = table.Column<bool>(type: "INTEGER", nullable: false),
                    StateJson = table.Column<string>(type: "TEXT", nullable: false),
                    TeamPositionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ObtainedRewards = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedFinalReward = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedPerfectly = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLostSectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLostSectors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMainQuests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    QuestTid = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedReward = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMainQuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMainQuests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Seq = table.Column<long>(type: "INTEGER", nullable: false),
                    ConversationId = table.Column<string>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNormalShopStates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ShopCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    RenewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RenewAt = table.Column<long>(type: "INTEGER", nullable: false),
                    NextRenewAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNormalShopStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNormalShopStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserOpenLiberateTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LiberateTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOpenLiberateTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOpenLiberateTypes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPasses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    PassId = table.Column<int>(type: "INTEGER", nullable: false),
                    PassPoint = table.Column<int>(type: "INTEGER", nullable: false),
                    PassSkipCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PremiumActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastCompleteAt = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPasses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPickedMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ConversationId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtNanos = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPickedMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPickedMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileCardDecorations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ShowCharacterSpine = table.Column<bool>(type: "INTEGER", nullable: false),
                    BackgroundId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileCardDecorations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfileCardDecorations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileCardStickers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    StickerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Layer = table.Column<int>(type: "INTEGER", nullable: false),
                    NormalizedX = table.Column<float>(type: "REAL", nullable: false),
                    NormalizedY = table.Column<float>(type: "REAL", nullable: false),
                    RotationRadian = table.Column<float>(type: "REAL", nullable: false),
                    Scale = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileCardStickers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfileCardStickers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRepresentationTeamSlots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterCsn = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRepresentationTeamSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRepresentationTeamSlots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserResetStates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    WipeoutCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ClearedSimulationRoom = table.Column<bool>(type: "INTEGER", nullable: false),
                    InterceptionTickets = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyMissionPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    DispatchCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WeeklyMissionPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    SimRoomStateJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserResetStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserResetStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSideStoryStages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    StageId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsViewed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSideStoryStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSideStoryStages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTeamGroups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TeamType = table.Column<int>(type: "INTEGER", nullable: false),
                    LastContentsTeamNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTeamGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTeamGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTowerProgress",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CorporationType = table.Column<int>(type: "INTEGER", nullable: false),
                    ClearedFloor = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTowerProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTowerProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserUnlockedCosmetics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CosmeticId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUnlockedCosmetics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserUnlockedCosmetics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserViewedNoticePopups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoticePopupTableId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserViewedNoticePopups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserViewedNoticePopups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWallpaperBackgrounds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    LobbyDecoBackgroundId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWallpaperBackgrounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWallpaperBackgrounds_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWallpaperFavorites",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsShuffled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWallpaperFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWallpaperFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWallpaperJukeboxes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    JukeboxListTableId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWallpaperJukeboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWallpaperJukeboxes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWallpaperPlaylists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    PlaylistUId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsShuffled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWallpaperPlaylists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWallpaperPlaylists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWallpaperSlots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    WallpaperId = table.Column<int>(type: "INTEGER", nullable: false),
                    UseCharacterBackground = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWallpaperSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWallpaperSlots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentAwakenings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    IsNewData = table.Column<bool>(type: "INTEGER", nullable: false),
                    Option1Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Option1Lock = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOption1DisposableLock = table.Column<bool>(type: "INTEGER", nullable: false),
                    Option2Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Option2Lock = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOption2DisposableLock = table.Column<bool>(type: "INTEGER", nullable: false),
                    Option3Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Option3Lock = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOption3DisposableLock = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentAwakenings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentAwakenings_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemCharacterAssignments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    CharacterCsn = table.Column<long>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCharacterAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCharacterAssignments_Characters_UserId_CharacterCsn",
                        columns: x => new { x.UserId, x.CharacterCsn },
                        principalTable: "Characters",
                        principalColumns: new[] { "UserId", "Csn" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemCharacterAssignments_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemCharacterAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchClearItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DispatchId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchClearItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchClearItems_Outposts_UserId",
                        column: x => x.UserId,
                        principalTable: "Outposts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dispatches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Tid = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRun = table.Column<int>(type: "INTEGER", nullable: false),
                    StartAt = table.Column<long>(type: "INTEGER", nullable: false),
                    EndAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dispatches_Outposts_UserId",
                        column: x => x.UserId,
                        principalTable: "Outposts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InfraCoreRewards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    InfraCoreLevel = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfraCoreRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfraCoreRewards_Outposts_UserId",
                        column: x => x.UserId,
                        principalTable: "Outposts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutpostBuildings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SlotId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuildingId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDone = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CompleteAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutpostBuildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutpostBuildings_Outposts_UserId",
                        column: x => x.UserId,
                        principalTable: "Outposts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResearchProgress",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ResearchId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Defense = table.Column<int>(type: "INTEGER", nullable: false),
                    Hp = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchProgress_Outposts_UserId",
                        column: x => x.UserId,
                        principalTable: "Outposts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelectableDispatches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DispatchGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectTid = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectSlotId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRun = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartAt = table.Column<long>(type: "INTEGER", nullable: false),
                    EndAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectableDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectableDispatches_Outposts_UserId",
                        column: x => x.UserId,
                        principalTable: "Outposts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SynchroSlots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterSerialNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    AvailableAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynchroSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SynchroSlots_Outposts_UserId",
                        column: x => x.UserId,
                        principalTable: "Outposts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBondProgressItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BondId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBondProgressItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBondProgressItems_UserBonds_BondId",
                        column: x => x.BondId,
                        principalTable: "UserBonds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBondProgressItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEventMissionEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserEventMissionId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    IsDaily = table.Column<bool>(type: "INTEGER", nullable: false),
                    MissionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventMissionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEventMissionEntries_UserEventMissions_UserEventMissionId",
                        column: x => x.UserEventMissionId,
                        principalTable: "UserEventMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEventMissionEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEventClearedStages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserEventId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    StageId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventClearedStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEventClearedStages_UserEvents_UserEventId",
                        column: x => x.UserEventId,
                        principalTable: "UserEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEventClearedStages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEventCompletedScenarios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserEventId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    ScenarioId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventCompletedScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEventCompletedScenarios_UserEvents_UserEventId",
                        column: x => x.UserEventId,
                        principalTable: "UserEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEventCompletedScenarios_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGachaPaybackRewards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    PaybackId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    StepId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGachaPaybackRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGachaPaybackRewards_UserGachaPaybacks_PaybackId",
                        column: x => x.PaybackId,
                        principalTable: "UserGachaPaybacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGachaPaybackRewards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLiberationMissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LiberationId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    MissionId = table.Column<long>(type: "INTEGER", nullable: false),
                    MissionTid = table.Column<int>(type: "INTEGER", nullable: false),
                    LiberateCharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    MissionState = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    TriggerStartAt = table.Column<long>(type: "INTEGER", nullable: false),
                    TriggerEndAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ReceivedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLiberationMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLiberationMissions_UserLiberations_LiberationId",
                        column: x => x.LiberationId,
                        principalTable: "UserLiberations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLiberationMissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginEventDays",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserLoginEventId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    Day = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginEventDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginEventDays_UserLoginEvents_UserLoginEventId",
                        column: x => x.UserLoginEventId,
                        principalTable: "UserLoginEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLoginEventDays_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLostSectorClearedStages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LostSectorId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    StageKey = table.Column<string>(type: "TEXT", nullable: false),
                    ClearState = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLostSectorClearedStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLostSectorClearedStages_UserLostSectors_LostSectorId",
                        column: x => x.LostSectorId,
                        principalTable: "UserLostSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLostSectorClearedStages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLostSectorObjects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LostSectorId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    ObjectKey = table.Column<string>(type: "TEXT", nullable: false),
                    ActionAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    PositionId = table.Column<string>(type: "TEXT", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false),
                    TeamPositionsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLostSectorObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLostSectorObjects_UserLostSectors_LostSectorId",
                        column: x => x.LostSectorId,
                        principalTable: "UserLostSectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLostSectorObjects_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNormalShopOrders",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ShopStateId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNormalShopOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNormalShopOrders_UserNormalShopStates_ShopStateId",
                        column: x => x.ShopStateId,
                        principalTable: "UserNormalShopStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNormalShopOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNormalShopPinnedBuys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ShopStateId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNormalShopPinnedBuys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNormalShopPinnedBuys_UserNormalShopStates_ShopStateId",
                        column: x => x.ShopStateId,
                        principalTable: "UserNormalShopStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNormalShopPinnedBuys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPassMissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserPassId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    PassMissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsComplete = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPassMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPassMissions_UserPasses_UserPassId",
                        column: x => x.UserPassId,
                        principalTable: "UserPasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPassMissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPassRanks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserPassId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    PassRank = table.Column<int>(type: "INTEGER", nullable: false),
                    IsNormalRewarded = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPremiumRewarded = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPassRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPassRanks_UserPasses_UserPassId",
                        column: x => x.UserPassId,
                        principalTable: "UserPasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPassRanks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTeamSlots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TeamGroupId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    TeamNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: false),
                    ValueType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTeamSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTeamSlots_UserTeamGroups_TeamGroupId",
                        column: x => x.TeamGroupId,
                        principalTable: "UserTeamGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTeamSlots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_Csn",
                table: "Characters",
                columns: new[] { "UserId", "Csn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchClearItems_UserId_DispatchId",
                table: "DispatchClearItems",
                columns: new[] { "UserId", "DispatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_UserId_Tid",
                table: "Dispatches",
                columns: new[] { "UserId", "Tid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAwakenings_ItemId",
                table: "EquipmentAwakenings",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InfraCoreRewards_UserId_InfraCoreLevel",
                table: "InfraCoreRewards",
                columns: new[] { "UserId", "InfraCoreLevel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCharacterAssignments_ItemId_CharacterCsn",
                table: "ItemCharacterAssignments",
                columns: new[] { "ItemId", "CharacterCsn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCharacterAssignments_ItemId_Position",
                table: "ItemCharacterAssignments",
                columns: new[] { "ItemId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCharacterAssignments_UserId_CharacterCsn",
                table: "ItemCharacterAssignments",
                columns: new[] { "UserId", "CharacterCsn" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_Isn",
                table: "Items",
                columns: new[] { "UserId", "Isn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_ItemType",
                table: "Items",
                columns: new[] { "UserId", "ItemType" });

            migrationBuilder.CreateIndex(
                name: "IX_OutpostBuildings_UserId_SlotId",
                table: "OutpostBuildings",
                columns: new[] { "UserId", "SlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchProgress_UserId_ResearchId",
                table: "ResearchProgress",
                columns: new[] { "UserId", "ResearchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelectableDispatches_UserId_SelectTid",
                table: "SelectableDispatches",
                columns: new[] { "UserId", "SelectTid" });

            migrationBuilder.CreateIndex(
                name: "IX_SynchroSlots_UserId_Slot",
                table: "SynchroSlots",
                columns: new[] { "UserId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserArchiveUnlocks_UserId_Kind_TargetId",
                table: "UserArchiveUnlocks",
                columns: new[] { "UserId", "Kind", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId_Seq",
                table: "UserBadges",
                columns: new[] { "UserId", "Seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBondProgressItems_BondId_Kind_ItemId",
                table: "UserBondProgressItems",
                columns: new[] { "BondId", "Kind", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBondProgressItems_UserId",
                table: "UserBondProgressItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBonds_UserId_NameCode",
                table: "UserBonds",
                columns: new[] { "UserId", "NameCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaimedJukeboxRewards_UserId_TriggerId",
                table: "UserClaimedJukeboxRewards",
                columns: new[] { "UserId", "TriggerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClearedTutorials_UserId_TutorialGroupId",
                table: "UserClearedTutorials",
                columns: new[] { "UserId", "TutorialGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCompletedAchievements_UserId_AchievementId",
                table: "UserCompletedAchievements",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCompletedDailyMissions_UserId_MissionId",
                table: "UserCompletedDailyMissions",
                columns: new[] { "UserId", "MissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCompletedScenarios_UserId_ScenarioId",
                table: "UserCompletedScenarios",
                columns: new[] { "UserId", "ScenarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCompletedWeeklyMissions_UserId_MissionId",
                table: "UserCompletedWeeklyMissions",
                columns: new[] { "UserId", "MissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserContentsUnlocks_UserId_ContentsOpenId",
                table: "UserContentsUnlocks",
                columns: new[] { "UserId", "ContentsOpenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCurrencies_UserId_CurrencyType",
                table: "UserCurrencies",
                columns: new[] { "UserId", "CurrencyType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyCounselCounts_UserId_KeyId",
                table: "UserDailyCounselCounts",
                columns: new[] { "UserId", "KeyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDailyTowerCounts_UserId_CorporationType",
                table: "UserDailyTowerCounts",
                columns: new[] { "UserId", "CorporationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEventClearedStages_UserEventId_StageId",
                table: "UserEventClearedStages",
                columns: new[] { "UserEventId", "StageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEventClearedStages_UserId",
                table: "UserEventClearedStages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEventCompletedScenarios_UserEventId_ScenarioId",
                table: "UserEventCompletedScenarios",
                columns: new[] { "UserEventId", "ScenarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEventCompletedScenarios_UserId",
                table: "UserEventCompletedScenarios",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEventMissionEntries_UserEventMissionId_IsDaily_MissionId",
                table: "UserEventMissionEntries",
                columns: new[] { "UserEventMissionId", "IsDaily", "MissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEventMissionEntries_UserId",
                table: "UserEventMissionEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEventMissions_UserId_EventId",
                table: "UserEventMissions",
                columns: new[] { "UserId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEvents_UserId_EventId",
                table: "UserEvents",
                columns: new[] { "UserId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEventShopBuys_UserId_EventId_ProductTid",
                table: "UserEventShopBuys",
                columns: new[] { "UserId", "EventId", "ProductTid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteItemQuests_UserId_QuestId",
                table: "UserFavoriteItemQuests",
                columns: new[] { "UserId", "QuestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteItems_UserId_FavoriteItemId",
                table: "UserFavoriteItems",
                columns: new[] { "UserId", "FavoriteItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGachaBannerPulls_UserId_BannerId",
                table: "UserGachaBannerPulls",
                columns: new[] { "UserId", "BannerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGachaDailyFreePulls_UserId_GachaId",
                table: "UserGachaDailyFreePulls",
                columns: new[] { "UserId", "GachaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGachaPaybackRewards_PaybackId_StepId",
                table: "UserGachaPaybackRewards",
                columns: new[] { "PaybackId", "StepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGachaPaybackRewards_UserId",
                table: "UserGachaPaybackRewards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGachaPaybacks_UserId_GachaId",
                table: "UserGachaPaybacks",
                columns: new[] { "UserId", "GachaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGachaPityCounts_UserId_BannerId",
                table: "UserGachaPityCounts",
                columns: new[] { "UserId", "BannerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGachaSelectupChoices_UserId_BannerId",
                table: "UserGachaSelectupChoices",
                columns: new[] { "UserId", "BannerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserJukeboxSettings_UserId_Location",
                table: "UserJukeboxSettings",
                columns: new[] { "UserId", "Location" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLiberationMissions_LiberationId_MissionId",
                table: "UserLiberationMissions",
                columns: new[] { "LiberationId", "MissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLiberationMissions_UserId",
                table: "UserLiberationMissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLiberations_UserId_CharacterId",
                table: "UserLiberations",
                columns: new[] { "UserId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEventDays_UserId",
                table: "UserLoginEventDays",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEventDays_UserLoginEventId_Day",
                table: "UserLoginEventDays",
                columns: new[] { "UserLoginEventId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginEvents_UserId_EventId",
                table: "UserLoginEvents",
                columns: new[] { "UserId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLostSectorClearedStages_LostSectorId_StageKey",
                table: "UserLostSectorClearedStages",
                columns: new[] { "LostSectorId", "StageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLostSectorClearedStages_UserId",
                table: "UserLostSectorClearedStages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLostSectorObjects_LostSectorId_ObjectKey",
                table: "UserLostSectorObjects",
                columns: new[] { "LostSectorId", "ObjectKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLostSectorObjects_UserId",
                table: "UserLostSectorObjects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLostSectors_UserId_SectorId",
                table: "UserLostSectors",
                columns: new[] { "UserId", "SectorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMainQuests_UserId_QuestTid",
                table: "UserMainQuests",
                columns: new[] { "UserId", "QuestTid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMessages_UserId_Seq",
                table: "UserMessages",
                columns: new[] { "UserId", "Seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNormalShopOrders_ShopStateId_OrderIndex",
                table: "UserNormalShopOrders",
                columns: new[] { "ShopStateId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNormalShopOrders_UserId",
                table: "UserNormalShopOrders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNormalShopPinnedBuys_ShopStateId_ProductId",
                table: "UserNormalShopPinnedBuys",
                columns: new[] { "ShopStateId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNormalShopPinnedBuys_UserId",
                table: "UserNormalShopPinnedBuys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNormalShopStates_UserId_ShopCategory",
                table: "UserNormalShopStates",
                columns: new[] { "UserId", "ShopCategory" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOpenLiberateTypes_UserId_LiberateTypeId",
                table: "UserOpenLiberateTypes",
                columns: new[] { "UserId", "LiberateTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPasses_UserId_PassId",
                table: "UserPasses",
                columns: new[] { "UserId", "PassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPassMissions_UserId",
                table: "UserPassMissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPassMissions_UserPassId_PassMissionId",
                table: "UserPassMissions",
                columns: new[] { "UserPassId", "PassMissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPassRanks_UserId",
                table: "UserPassRanks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPassRanks_UserPassId_PassRank",
                table: "UserPassRanks",
                columns: new[] { "UserPassId", "PassRank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPickedMessages_UserId_ConversationId",
                table: "UserPickedMessages",
                columns: new[] { "UserId", "ConversationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileCardDecorations_UserId",
                table: "UserProfileCardDecorations",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileCardStickers_UserId_Position",
                table: "UserProfileCardStickers",
                columns: new[] { "UserId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRepresentationTeamSlots_UserId_Slot",
                table: "UserRepresentationTeamSlots",
                columns: new[] { "UserId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserResetStates_UserId",
                table: "UserResetStates",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSideStoryStages_UserId_StageId",
                table: "UserSideStoryStages",
                columns: new[] { "UserId", "StageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTeamGroups_UserId_TeamType",
                table: "UserTeamGroups",
                columns: new[] { "UserId", "TeamType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTeamSlots_TeamGroupId_TeamNumber_Slot",
                table: "UserTeamSlots",
                columns: new[] { "TeamGroupId", "TeamNumber", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTeamSlots_UserId",
                table: "UserTeamSlots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTowerProgress_UserId_CorporationType",
                table: "UserTowerProgress",
                columns: new[] { "UserId", "CorporationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserUnlockedCosmetics_UserId_Category_CosmeticId",
                table: "UserUnlockedCosmetics",
                columns: new[] { "UserId", "Category", "CosmeticId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserViewedNoticePopups_UserId_CategoryId_NoticePopupTableId",
                table: "UserViewedNoticePopups",
                columns: new[] { "UserId", "CategoryId", "NoticePopupTableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWallpaperBackgrounds_UserId_OrderIndex",
                table: "UserWallpaperBackgrounds",
                columns: new[] { "UserId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWallpaperFavorites_UserId_OrderIndex",
                table: "UserWallpaperFavorites",
                columns: new[] { "UserId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWallpaperJukeboxes_UserId_OrderIndex",
                table: "UserWallpaperJukeboxes",
                columns: new[] { "UserId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWallpaperPlaylists_UserId_OrderIndex",
                table: "UserWallpaperPlaylists",
                columns: new[] { "UserId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWallpaperSlots_UserId_OrderIndex",
                table: "UserWallpaperSlots",
                columns: new[] { "UserId", "OrderIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchClearItems");

            migrationBuilder.DropTable(
                name: "Dispatches");

            migrationBuilder.DropTable(
                name: "EquipmentAwakenings");

            migrationBuilder.DropTable(
                name: "InfraCoreRewards");

            migrationBuilder.DropTable(
                name: "ItemCharacterAssignments");

            migrationBuilder.DropTable(
                name: "OutpostBuildings");

            migrationBuilder.DropTable(
                name: "ResearchProgress");

            migrationBuilder.DropTable(
                name: "SelectableDispatches");

            migrationBuilder.DropTable(
                name: "SynchroSlots");

            migrationBuilder.DropTable(
                name: "UserArchiveUnlocks");

            migrationBuilder.DropTable(
                name: "UserBadges");

            migrationBuilder.DropTable(
                name: "UserBondProgressItems");

            migrationBuilder.DropTable(
                name: "UserClaimedJukeboxRewards");

            migrationBuilder.DropTable(
                name: "UserClearedTutorials");

            migrationBuilder.DropTable(
                name: "UserCompletedAchievements");

            migrationBuilder.DropTable(
                name: "UserCompletedDailyMissions");

            migrationBuilder.DropTable(
                name: "UserCompletedScenarios");

            migrationBuilder.DropTable(
                name: "UserCompletedWeeklyMissions");

            migrationBuilder.DropTable(
                name: "UserContentsUnlocks");

            migrationBuilder.DropTable(
                name: "UserCurrencies");

            migrationBuilder.DropTable(
                name: "UserDailyCounselCounts");

            migrationBuilder.DropTable(
                name: "UserDailyTowerCounts");

            migrationBuilder.DropTable(
                name: "UserEventClearedStages");

            migrationBuilder.DropTable(
                name: "UserEventCompletedScenarios");

            migrationBuilder.DropTable(
                name: "UserEventMissionEntries");

            migrationBuilder.DropTable(
                name: "UserEventShopBuys");

            migrationBuilder.DropTable(
                name: "UserFavoriteItemQuests");

            migrationBuilder.DropTable(
                name: "UserFavoriteItems");

            migrationBuilder.DropTable(
                name: "UserGachaBannerPulls");

            migrationBuilder.DropTable(
                name: "UserGachaDailyFreePulls");

            migrationBuilder.DropTable(
                name: "UserGachaPaybackRewards");

            migrationBuilder.DropTable(
                name: "UserGachaPityCounts");

            migrationBuilder.DropTable(
                name: "UserGachaSelectupChoices");

            migrationBuilder.DropTable(
                name: "UserJukeboxSettings");

            migrationBuilder.DropTable(
                name: "UserLiberationMissions");

            migrationBuilder.DropTable(
                name: "UserLoginEventDays");

            migrationBuilder.DropTable(
                name: "UserLostSectorClearedStages");

            migrationBuilder.DropTable(
                name: "UserLostSectorObjects");

            migrationBuilder.DropTable(
                name: "UserMainQuests");

            migrationBuilder.DropTable(
                name: "UserMessages");

            migrationBuilder.DropTable(
                name: "UserNormalShopOrders");

            migrationBuilder.DropTable(
                name: "UserNormalShopPinnedBuys");

            migrationBuilder.DropTable(
                name: "UserOpenLiberateTypes");

            migrationBuilder.DropTable(
                name: "UserPassMissions");

            migrationBuilder.DropTable(
                name: "UserPassRanks");

            migrationBuilder.DropTable(
                name: "UserPickedMessages");

            migrationBuilder.DropTable(
                name: "UserProfileCardDecorations");

            migrationBuilder.DropTable(
                name: "UserProfileCardStickers");

            migrationBuilder.DropTable(
                name: "UserRepresentationTeamSlots");

            migrationBuilder.DropTable(
                name: "UserResetStates");

            migrationBuilder.DropTable(
                name: "UserSideStoryStages");

            migrationBuilder.DropTable(
                name: "UserTeamSlots");

            migrationBuilder.DropTable(
                name: "UserTowerProgress");

            migrationBuilder.DropTable(
                name: "UserUnlockedCosmetics");

            migrationBuilder.DropTable(
                name: "UserViewedNoticePopups");

            migrationBuilder.DropTable(
                name: "UserWallpaperBackgrounds");

            migrationBuilder.DropTable(
                name: "UserWallpaperFavorites");

            migrationBuilder.DropTable(
                name: "UserWallpaperJukeboxes");

            migrationBuilder.DropTable(
                name: "UserWallpaperPlaylists");

            migrationBuilder.DropTable(
                name: "UserWallpaperSlots");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Outposts");

            migrationBuilder.DropTable(
                name: "UserBonds");

            migrationBuilder.DropTable(
                name: "UserEvents");

            migrationBuilder.DropTable(
                name: "UserEventMissions");

            migrationBuilder.DropTable(
                name: "UserGachaPaybacks");

            migrationBuilder.DropTable(
                name: "UserLiberations");

            migrationBuilder.DropTable(
                name: "UserLoginEvents");

            migrationBuilder.DropTable(
                name: "UserLostSectors");

            migrationBuilder.DropTable(
                name: "UserNormalShopStates");

            migrationBuilder.DropTable(
                name: "UserPasses");

            migrationBuilder.DropTable(
                name: "UserTeamGroups");
        }
    }
}
