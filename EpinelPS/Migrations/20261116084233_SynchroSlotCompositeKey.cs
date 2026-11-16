using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EpinelPS.Migrations
{
    /// <inheritdoc />
    public partial class SynchroSlotCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SynchroSlots",
                table: "SynchroSlots");

            migrationBuilder.DropIndex(
                name: "IX_SynchroSlots_UserId_Slot",
                table: "SynchroSlots");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "SynchroSlots");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SynchroSlots",
                table: "SynchroSlots",
                columns: new[] { "UserId", "Slot" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SynchroSlots",
                table: "SynchroSlots");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "SynchroSlots",
                type: "TEXT",
                maxLength: 26,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SynchroSlots",
                table: "SynchroSlots",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SynchroSlots_UserId_Slot",
                table: "SynchroSlots",
                columns: new[] { "UserId", "Slot" },
                unique: true);
        }
    }
}
