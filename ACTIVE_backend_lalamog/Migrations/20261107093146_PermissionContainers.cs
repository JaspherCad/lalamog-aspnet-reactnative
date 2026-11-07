using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Migrations
{
    /// <inheritdoc />
    public partial class PermissionContainers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'FIGHTER' WHERE [Name] = 'Fighter';");
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'ADMIN' WHERE [Name] = 'Admin';");
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'GYM_OWNER' WHERE [Name] = 'GymOwner';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'Fighter' WHERE [Name] = 'FIGHTER';");
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'Admin' WHERE [Name] = 'ADMIN';");
            migrationBuilder.Sql("UPDATE [Roles] SET [Name] = 'GymOwner' WHERE [Name] = 'GYM_OWNER';");
        }
    }
}
