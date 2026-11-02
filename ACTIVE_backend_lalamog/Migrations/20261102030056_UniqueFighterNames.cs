using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueFighterNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FighterName",
                table: "Profiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedFighterName",
                table: "Profiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Preserve every existing nickname as the reusable alias. Backfill a
            // collision-free public name from the immutable user ID before indexing.
            migrationBuilder.Sql(@"
                UPDATE [Profiles]
                SET [FighterName] = 'fighter-' + LOWER(REPLACE(CONVERT(varchar(36), [UserId]), '-', '')),
                    [NormalizedFighterName] = 'FIGHTER-' + UPPER(REPLACE(CONVERT(varchar(36), [UserId]), '-', ''));");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_NormalizedFighterName",
                table: "Profiles",
                column: "NormalizedFighterName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profiles_NormalizedFighterName",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "FighterName",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "NormalizedFighterName",
                table: "Profiles");
        }
    }
}
