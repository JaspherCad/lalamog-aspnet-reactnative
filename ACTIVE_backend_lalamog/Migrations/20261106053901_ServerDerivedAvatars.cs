using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Migrations
{
    /// <inheritdoc />
    public partial class ServerDerivedAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Profiles WHERE AvatarBlobName IS NOT NULL)
                    THROW 51000, 'Avatar transfer required: start the updated API with the existing storage before removing avatar references.', 1;
                """);
            migrationBuilder.DropColumn(
                name: "AvatarBlobName",
                table: "Profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarBlobName",
                table: "Profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
