using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialWebMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fights_MatchId",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "MediaKey",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "ResultNotes",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "ResultRecordedAt",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "User1Confirmed",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "User2Confirmed",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "WinnerId",
                table: "Fights");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarKey",
                table: "Profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Fights",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Fights",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Fights",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Fights",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "FightConfirmations",
                columns: table => new
                {
                    FightId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FightConfirmations", x => new { x.FightId, x.UserId });
                    table.ForeignKey(
                        name: "FK_FightConfirmations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FightConfirmations_Fights_FightId",
                        column: x => x.FightId,
                        principalTable: "Fights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Swipe_Direction",
                table: "Swipes",
                sql: "[Direction] IN ('like', 'pass')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Swipe_NoSelf",
                table: "Swipes",
                sql: "[SwiperId] <> [TargetId]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Profile_Experience",
                table: "Profiles",
                sql: "[ExperienceLevel] BETWEEN 1 AND 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Match_NoSelf",
                table: "Matches",
                sql: "[User1Id] <> [User2Id]");

            migrationBuilder.CreateIndex(
                name: "IX_Fights_MatchId",
                table: "Fights",
                column: "MatchId",
                unique: true,
                filter: "[Status] IN ('scheduled', 'confirmed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Fight_Status",
                table: "Fights",
                sql: "[Status] IN ('scheduled', 'confirmed', 'canceled', 'completed')");

            migrationBuilder.CreateIndex(
                name: "IX_FightConfirmations_UserId",
                table: "FightConfirmations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FightConfirmations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Swipe_Direction",
                table: "Swipes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Swipe_NoSelf",
                table: "Swipes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Profile_Experience",
                table: "Profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Match_NoSelf",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Fights_MatchId",
                table: "Fights");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Fight_Status",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Fights");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarKey",
                table: "Profiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Profiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MediaKey",
                table: "Fights",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "Fights",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultNotes",
                table: "Fights",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResultRecordedAt",
                table: "Fights",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "User1Confirmed",
                table: "Fights",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "User2Confirmed",
                table: "Fights",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "WinnerId",
                table: "Fights",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fights_MatchId",
                table: "Fights",
                column: "MatchId");
        }
    }
}
