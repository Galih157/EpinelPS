using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EpinelPS.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondaryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExperiencePoint",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GuildId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GuildLeaveAt",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserLevel",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ArcadeScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Score = table.Column<long>(type: "INTEGER", nullable: false),
                    ArcadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArcadeScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MiniGameLeaderboards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    MinigameType = table.Column<string>(type: "TEXT", nullable: false),
                    ArcadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModeId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    Score = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiniGameLeaderboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MiniGameLeaderboards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidMuseumStages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    StageId = table.Column<int>(type: "INTEGER", nullable: false),
                    StageMode = table.Column<int>(type: "INTEGER", nullable: false),
                    IsNoLimitUnlocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    DebugDamageMultiplier = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidMuseumStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidMuseumStages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaids",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RaidId = table.Column<int>(type: "INTEGER", nullable: false),
                    RaidOpenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TrialCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastDateDay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaids_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TtsSongScores",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    SongId = table.Column<int>(type: "INTEGER", nullable: false),
                    RankType = table.Column<int>(type: "INTEGER", nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateTime = table.Column<long>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TtsSongScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TtsTotalScores",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    RankType = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TtsTotalScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCharacterWishlists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BannerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCharacterWishlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCharacterWishlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClearedOutpostScenarios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ScenarioId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClearedOutpostScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClearedOutpostScenarios_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFieldMaps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MapId = table.Column<string>(type: "TEXT", nullable: false),
                    DataJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFieldMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFieldMaps_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserJukeboxPlaylists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    PlaylistUid = table.Column<long>(type: "INTEGER", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    SongsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserJukeboxPlaylists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserJukeboxPlaylists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLobbyPrivateBanners",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BannerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLobbyPrivateBanners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLobbyPrivateBanners_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Msn = table.Column<long>(type: "INTEGER", nullable: false),
                    Sender = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleJson = table.Column<string>(type: "TEXT", nullable: false),
                    TextJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Nickname = table.Column<string>(type: "TEXT", nullable: false),
                    AppNoticeId = table.Column<string>(type: "TEXT", nullable: false),
                    HasReward = table.Column<bool>(type: "INTEGER", nullable: false),
                    ItemsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Period = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMails_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMapStates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MapId = table.Column<string>(type: "TEXT", nullable: false),
                    StateJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMapStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMapStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMessengerRepairHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AuditId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    MessageJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessengerRepairHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMessengerRepairHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMinigames",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MinigameType = table.Column<string>(type: "TEXT", nullable: false),
                    ArcadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataJson = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMinigames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMinigames_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStageClearHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    TeamCombat = table.Column<int>(type: "INTEGER", nullable: false),
                    ClearedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UserJson = table.Column<string>(type: "TEXT", nullable: false),
                    SlotsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStageClearHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStageClearHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSubQuests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SubQuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedReward = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubQuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubQuests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTacticAcademyLessons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    LessonId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTacticAcademyLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTacticAcademyLessons_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidMuseumMissionClaims",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidMuseumStageId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    ModeType = table.Column<int>(type: "INTEGER", nullable: false),
                    MissionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidMuseumMissionClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidMuseumMissionClaims_SoloRaidMuseumStages_SoloRaidMuseumStageId",
                        column: x => x.SoloRaidMuseumStageId,
                        principalTable: "SoloRaidMuseumStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidMuseumModes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidMuseumStageId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    ModeType = table.Column<int>(type: "INTEGER", nullable: false),
                    StageJoinCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDamage = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalStep = table.Column<int>(type: "INTEGER", nullable: false),
                    BestDamage = table.Column<long>(type: "INTEGER", nullable: false),
                    BestStep = table.Column<int>(type: "INTEGER", nullable: false),
                    IsInProgress = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidMuseumModes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidMuseumModes_SoloRaidMuseumStages_SoloRaidMuseumStageId",
                        column: x => x.SoloRaidMuseumStageId,
                        principalTable: "SoloRaidMuseumStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidMuseumTeams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidMuseumStageId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    TeamNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidMuseumTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidMuseumTeams_SoloRaidMuseumStages_SoloRaidMuseumStageId",
                        column: x => x.SoloRaidMuseumStageId,
                        principalTable: "SoloRaidMuseumStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidLevels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    RaidLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    RaidJoinCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Hp = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalDamage = table.Column<long>(type: "INTEGER", nullable: false),
                    IsClear = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOpen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidLevels_SoloRaids_SoloRaidId",
                        column: x => x.SoloRaidId,
                        principalTable: "SoloRaids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidMuseumLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidMuseumModeId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    LogSet = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    TeamNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage = table.Column<long>(type: "INTEGER", nullable: false),
                    Step = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidMuseumLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidMuseumLogs_SoloRaidMuseumModes_SoloRaidMuseumModeId",
                        column: x => x.SoloRaidMuseumModeId,
                        principalTable: "SoloRaidMuseumModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidMuseumOpenTeams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidMuseumModeId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    TeamNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidMuseumOpenTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidMuseumOpenTeams_SoloRaidMuseumModes_SoloRaidMuseumModeId",
                        column: x => x.SoloRaidMuseumModeId,
                        principalTable: "SoloRaidMuseumModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidMuseumTeamSlots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidMuseumTeamId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: false),
                    ValueType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidMuseumTeamSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidMuseumTeamSlots_SoloRaidMuseumTeams_SoloRaidMuseumTeamId",
                        column: x => x.SoloRaidMuseumTeamId,
                        principalTable: "SoloRaidMuseumTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidLevelId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage = table.Column<long>(type: "INTEGER", nullable: false),
                    Kill = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidLogs_SoloRaidLevels_SoloRaidLevelId",
                        column: x => x.SoloRaidLevelId,
                        principalTable: "SoloRaidLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidMuseumLogCharacters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidMuseumLogId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Csn = table.Column<long>(type: "INTEGER", nullable: false),
                    Tid = table.Column<int>(type: "INTEGER", nullable: false),
                    Lv = table.Column<int>(type: "INTEGER", nullable: false),
                    Combat = table.Column<int>(type: "INTEGER", nullable: false),
                    CostumeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidMuseumLogCharacters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidMuseumLogCharacters_SoloRaidMuseumLogs_SoloRaidMuseumLogId",
                        column: x => x.SoloRaidMuseumLogId,
                        principalTable: "SoloRaidMuseumLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoloRaidLogCharacters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    SoloRaidLogId = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Csn = table.Column<long>(type: "INTEGER", nullable: false),
                    Tid = table.Column<int>(type: "INTEGER", nullable: false),
                    Lv = table.Column<int>(type: "INTEGER", nullable: false),
                    Combat = table.Column<int>(type: "INTEGER", nullable: false),
                    CostumeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoloRaidLogCharacters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoloRaidLogCharacters_SoloRaidLogs_SoloRaidLogId",
                        column: x => x.SoloRaidLogId,
                        principalTable: "SoloRaidLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeScores_ArcadeId_ModeId_Score",
                table: "ArcadeScores",
                columns: new[] { "ArcadeId", "ModeId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_ArcadeScores_UserId",
                table: "ArcadeScores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MiniGameLeaderboards_MinigameType_ArcadeId_ModeId_Score",
                table: "MiniGameLeaderboards",
                columns: new[] { "MinigameType", "ArcadeId", "ModeId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_MiniGameLeaderboards_MinigameType_GuildId_ArcadeId_Score",
                table: "MiniGameLeaderboards",
                columns: new[] { "MinigameType", "GuildId", "ArcadeId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_MiniGameLeaderboards_UserId",
                table: "MiniGameLeaderboards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidLevels_SoloRaidId_RaidLevel_Type_IsOpen",
                table: "SoloRaidLevels",
                columns: new[] { "SoloRaidId", "RaidLevel", "Type", "IsOpen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidLogCharacters_SoloRaidLogId_Slot",
                table: "SoloRaidLogCharacters",
                columns: new[] { "SoloRaidLogId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidLogs_SoloRaidLevelId_Sequence",
                table: "SoloRaidLogs",
                columns: new[] { "SoloRaidLevelId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumLogCharacters_SoloRaidMuseumLogId_Slot",
                table: "SoloRaidMuseumLogCharacters",
                columns: new[] { "SoloRaidMuseumLogId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumLogs_SoloRaidMuseumModeId_LogSet_Sequence",
                table: "SoloRaidMuseumLogs",
                columns: new[] { "SoloRaidMuseumModeId", "LogSet", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumLogs_SoloRaidMuseumModeId_LogSet_TeamNumber",
                table: "SoloRaidMuseumLogs",
                columns: new[] { "SoloRaidMuseumModeId", "LogSet", "TeamNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumMissionClaims_SoloRaidMuseumStageId_ModeType_MissionId",
                table: "SoloRaidMuseumMissionClaims",
                columns: new[] { "SoloRaidMuseumStageId", "ModeType", "MissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumModes_SoloRaidMuseumStageId_ModeType",
                table: "SoloRaidMuseumModes",
                columns: new[] { "SoloRaidMuseumStageId", "ModeType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumOpenTeams_SoloRaidMuseumModeId_Sequence",
                table: "SoloRaidMuseumOpenTeams",
                columns: new[] { "SoloRaidMuseumModeId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumOpenTeams_SoloRaidMuseumModeId_TeamNumber",
                table: "SoloRaidMuseumOpenTeams",
                columns: new[] { "SoloRaidMuseumModeId", "TeamNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumStages_UserId_StageId",
                table: "SoloRaidMuseumStages",
                columns: new[] { "UserId", "StageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumTeams_SoloRaidMuseumStageId_TeamNumber",
                table: "SoloRaidMuseumTeams",
                columns: new[] { "SoloRaidMuseumStageId", "TeamNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaidMuseumTeamSlots_SoloRaidMuseumTeamId_Slot",
                table: "SoloRaidMuseumTeamSlots",
                columns: new[] { "SoloRaidMuseumTeamId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoloRaids_UserId_RaidId",
                table: "SoloRaids",
                columns: new[] { "UserId", "RaidId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TtsSongScores_SongId_RankType_Difficulty_Score",
                table: "TtsSongScores",
                columns: new[] { "SongId", "RankType", "Difficulty", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_TtsSongScores_UserId_SongId_RankType_Difficulty",
                table: "TtsSongScores",
                columns: new[] { "UserId", "SongId", "RankType", "Difficulty" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TtsTotalScores_RankType_Score",
                table: "TtsTotalScores",
                columns: new[] { "RankType", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_TtsTotalScores_UserId_RankType",
                table: "TtsTotalScores",
                columns: new[] { "UserId", "RankType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCharacterWishlists_UserId_BannerId_CharacterId",
                table: "UserCharacterWishlists",
                columns: new[] { "UserId", "BannerId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClearedOutpostScenarios_UserId_ScenarioId",
                table: "UserClearedOutpostScenarios",
                columns: new[] { "UserId", "ScenarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFieldMaps_UserId_MapId",
                table: "UserFieldMaps",
                columns: new[] { "UserId", "MapId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserJukeboxPlaylists_UserId_IsFavorite_PlaylistUid",
                table: "UserJukeboxPlaylists",
                columns: new[] { "UserId", "IsFavorite", "PlaylistUid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLobbyPrivateBanners_UserId_BannerId",
                table: "UserLobbyPrivateBanners",
                columns: new[] { "UserId", "BannerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMails_UserId_Msn",
                table: "UserMails",
                columns: new[] { "UserId", "Msn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMapStates_UserId_MapId",
                table: "UserMapStates",
                columns: new[] { "UserId", "MapId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMessengerRepairHistory_UserId_AuditId",
                table: "UserMessengerRepairHistory",
                columns: new[] { "UserId", "AuditId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMinigames_UserId_MinigameType_ArcadeId",
                table: "UserMinigames",
                columns: new[] { "UserId", "MinigameType", "ArcadeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStageClearHistory_UserId_ClearedAt",
                table: "UserStageClearHistory",
                columns: new[] { "UserId", "ClearedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubQuests_UserId_SubQuestId",
                table: "UserSubQuests",
                columns: new[] { "UserId", "SubQuestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTacticAcademyLessons_UserId_LessonId",
                table: "UserTacticAcademyLessons",
                columns: new[] { "UserId", "LessonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArcadeScores");

            migrationBuilder.DropTable(
                name: "MiniGameLeaderboards");

            migrationBuilder.DropTable(
                name: "SoloRaidLogCharacters");

            migrationBuilder.DropTable(
                name: "SoloRaidMuseumLogCharacters");

            migrationBuilder.DropTable(
                name: "SoloRaidMuseumMissionClaims");

            migrationBuilder.DropTable(
                name: "SoloRaidMuseumOpenTeams");

            migrationBuilder.DropTable(
                name: "SoloRaidMuseumTeamSlots");

            migrationBuilder.DropTable(
                name: "TtsSongScores");

            migrationBuilder.DropTable(
                name: "TtsTotalScores");

            migrationBuilder.DropTable(
                name: "UserCharacterWishlists");

            migrationBuilder.DropTable(
                name: "UserClearedOutpostScenarios");

            migrationBuilder.DropTable(
                name: "UserFieldMaps");

            migrationBuilder.DropTable(
                name: "UserJukeboxPlaylists");

            migrationBuilder.DropTable(
                name: "UserLobbyPrivateBanners");

            migrationBuilder.DropTable(
                name: "UserMails");

            migrationBuilder.DropTable(
                name: "UserMapStates");

            migrationBuilder.DropTable(
                name: "UserMessengerRepairHistory");

            migrationBuilder.DropTable(
                name: "UserMinigames");

            migrationBuilder.DropTable(
                name: "UserStageClearHistory");

            migrationBuilder.DropTable(
                name: "UserSubQuests");

            migrationBuilder.DropTable(
                name: "UserTacticAcademyLessons");

            migrationBuilder.DropTable(
                name: "SoloRaidLogs");

            migrationBuilder.DropTable(
                name: "SoloRaidMuseumLogs");

            migrationBuilder.DropTable(
                name: "SoloRaidMuseumTeams");

            migrationBuilder.DropTable(
                name: "SoloRaidLevels");

            migrationBuilder.DropTable(
                name: "SoloRaidMuseumModes");

            migrationBuilder.DropTable(
                name: "SoloRaids");

            migrationBuilder.DropTable(
                name: "SoloRaidMuseumStages");

            migrationBuilder.DropColumn(
                name: "ExperiencePoint",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GuildLeaveAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserLevel",
                table: "Users");
        }
    }
}
