using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EpinelPS.Migrations
{
    /// <inheritdoc />
    public partial class AddServerConfigUserProfileAndBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BattleTime",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CurrentLiberateCharacterId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DailyDiscountUsed",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "LastBadgeSeq",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "LastClearedDifficulty",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastHardStageCleared",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "LastMessageId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "LastNormalStageCleared",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReset",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "LastStoryStageCleared",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastTriggerId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWeeklyReset",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ProfileFrame",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ProfileIconId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30100);

            migrationBuilder.AddColumn<bool>(
                name: "ProfileIconIsPrism",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SickPullsEnabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TitleId",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "EventBanners",
                columns: table => new
                {
                    BannerId = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventBanners", x => x.BannerId);
                });

            migrationBuilder.CreateTable(
                name: "ServerSecrets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    LauncherTokenKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptionTokenKey = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSecrets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    LogLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxInterceptionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ResetHourUtcTime = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserBans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 26, nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsBanned = table.Column<bool>(type: "INTEGER", nullable: false),
                    BanStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BanEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BanId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SdkUsers_Email",
                table: "SdkUsers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UserBans_UserId",
                table: "UserBans",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventBanners");

            migrationBuilder.DropTable(
                name: "ServerSecrets");

            migrationBuilder.DropTable(
                name: "ServerSettings");

            migrationBuilder.DropTable(
                name: "UserBans");

            migrationBuilder.DropIndex(
                name: "IX_SdkUsers_Email",
                table: "SdkUsers");

            migrationBuilder.DropColumn(name: "BattleTime", table: "Users");
            migrationBuilder.DropColumn(name: "CurrentLiberateCharacterId", table: "Users");
            migrationBuilder.DropColumn(name: "DailyDiscountUsed", table: "Users");
            migrationBuilder.DropColumn(name: "LastBadgeSeq", table: "Users");
            migrationBuilder.DropColumn(name: "LastClearedDifficulty", table: "Users");
            migrationBuilder.DropColumn(name: "LastHardStageCleared", table: "Users");
            migrationBuilder.DropColumn(name: "LastMessageId", table: "Users");
            migrationBuilder.DropColumn(name: "LastNormalStageCleared", table: "Users");
            migrationBuilder.DropColumn(name: "LastReset", table: "Users");
            migrationBuilder.DropColumn(name: "LastStoryStageCleared", table: "Users");
            migrationBuilder.DropColumn(name: "LastTriggerId", table: "Users");
            migrationBuilder.DropColumn(name: "LastWeeklyReset", table: "Users");
            migrationBuilder.DropColumn(name: "ProfileFrame", table: "Users");
            migrationBuilder.DropColumn(name: "ProfileIconId", table: "Users");
            migrationBuilder.DropColumn(name: "ProfileIconIsPrism", table: "Users");
            migrationBuilder.DropColumn(name: "SickPullsEnabled", table: "Users");
            migrationBuilder.DropColumn(name: "TitleId", table: "Users");
        }
    }
}
