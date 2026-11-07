using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Migrations
{
    /// <inheritdoc />
    public partial class AvatarBlobNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarKey",
                table: "Profiles",
                newName: "AvatarBlobName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarBlobName",
                table: "Profiles",
                newName: "AvatarKey");
        }
    }
}
