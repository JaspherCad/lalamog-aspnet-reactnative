using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class FixFightScheduleConstraintForCanceled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old constraint FIRST (before updating data)
            migrationBuilder.DropCheckConstraint(
                name: "CK_FightSchedule_FutureTime",
                table: "FightSchedules");

            // Now update existing scheduled/confirmed fights with past dates
            migrationBuilder.Sql(@"
                UPDATE ""FightSchedules"" 
                SET ""Status"" = 'canceled', 
                    ""CancellationReason"" = 'Auto-canceled: past scheduled date',
                    ""CanceledAt"" = NOW(),
                    ""UpdatedAt"" = NOW()
                WHERE ""Status"" IN ('scheduled', 'confirmed') 
                AND ""ScheduledDateTime"" <= NOW();
            ");

            // Add the new constraint (allows past dates for canceled/completed)
            migrationBuilder.AddCheckConstraint(
                name: "CK_FightSchedule_FutureTime",
                table: "FightSchedules",
                sql: "(\"Status\" IN ('canceled', 'completed')) OR (\"ScheduledDateTime\" > NOW())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FightSchedule_FutureTime",
                table: "FightSchedules");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FightSchedule_FutureTime",
                table: "FightSchedules",
                sql: "\"ScheduledDateTime\" > NOW()");
        }
    }
}
