using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class FightSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FightSchedule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    ScheduledDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LocationName = table.Column<string>(type: "text", nullable: false),
                    LocationAddress = table.Column<string>(type: "text", nullable: false),
                    LocationCoordinates = table.Column<Point>(type: "geography", nullable: false),
                    IsSafetyWaiverAcceptedByUser1 = table.Column<bool>(type: "boolean", nullable: false),
                    IsSafetyWaiverAcceptedByUser2 = table.Column<bool>(type: "boolean", nullable: false),
                    User1SkillLevelAtTimeOfScheduling = table.Column<int>(type: "integer", nullable: false),
                    User2SkillLevelAtTimeOfScheduling = table.Column<int>(type: "integer", nullable: false),
                    User1EmergencyContactName = table.Column<string>(type: "text", nullable: false),
                    User1EmergencyContactPhone = table.Column<string>(type: "text", nullable: false),
                    User2EmergencyContactName = table.Column<string>(type: "text", nullable: false),
                    User2EmergencyContactPhone = table.Column<string>(type: "text", nullable: false),
                    CancellationReason = table.Column<string>(type: "text", nullable: false),
                    CanceledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    User1Rating = table.Column<int>(type: "integer", nullable: true),
                    User2Rating = table.Column<int>(type: "integer", nullable: true),
                    User1Feedback = table.Column<string>(type: "text", nullable: true),
                    User2Feedback = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FightSchedule", x => x.Id);
                    table.CheckConstraint("CK_FightSchedule_FutureTime", "\"ScheduledDateTime\" > NOW()");
                    table.CheckConstraint("CK_FightSchedule_SkillLevel", "\"User1SkillLevelAtTimeOfScheduling\" BETWEEN 1 AND 5 AND \"User2SkillLevelAtTimeOfScheduling\" BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_FightSchedule_Status", "\"Status\" IN ('scheduled', 'confirmed', 'in-progress', 'completed', 'canceled')");
                    table.ForeignKey(
                        name: "FK_FightSchedule_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FightSchedule_MatchId",
                table: "FightSchedule",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FightSchedule");
        }
    }
}
