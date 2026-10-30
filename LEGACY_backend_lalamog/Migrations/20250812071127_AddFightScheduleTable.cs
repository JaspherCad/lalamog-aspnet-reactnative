using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFightScheduleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FightSchedule_Matches_MatchId",
                table: "FightSchedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FightSchedule",
                table: "FightSchedule");

            migrationBuilder.RenameTable(
                name: "FightSchedule",
                newName: "FightSchedules");

            migrationBuilder.RenameIndex(
                name: "IX_FightSchedule_MatchId",
                table: "FightSchedules",
                newName: "IX_FightSchedules_MatchId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FightSchedules",
                table: "FightSchedules",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FightSchedules_Matches_MatchId",
                table: "FightSchedules",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FightSchedules_Matches_MatchId",
                table: "FightSchedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FightSchedules",
                table: "FightSchedules");

            migrationBuilder.RenameTable(
                name: "FightSchedules",
                newName: "FightSchedule");

            migrationBuilder.RenameIndex(
                name: "IX_FightSchedules_MatchId",
                table: "FightSchedule",
                newName: "IX_FightSchedule_MatchId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FightSchedule",
                table: "FightSchedule",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FightSchedule_Matches_MatchId",
                table: "FightSchedule",
                column: "MatchId",
                principalTable: "Matches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
