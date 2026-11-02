using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Migrations
{
    /// <inheritdoc />
    public partial class PhilippineMapLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BarangayCode",
                table: "Profiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityCode",
                table: "Profiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Profiles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Profiles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvinceCode",
                table: "Profiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PsgcVersion",
                table: "Profiles",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionCode",
                table: "Profiles",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BarangayCode",
                table: "Fights",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityCode",
                table: "Fights",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Fights",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Fights",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvinceCode",
                table: "Fights",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PsgcVersion",
                table: "Fights",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionCode",
                table: "Fights",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Profile_Coordinates",
                table: "Profiles",
                sql: "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Fight_Coordinates",
                table: "Fights",
                sql: "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Profile_Coordinates",
                table: "Profiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Fight_Coordinates",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "BarangayCode",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "CityCode",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "ProvinceCode",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "PsgcVersion",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "RegionCode",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "BarangayCode",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "CityCode",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "ProvinceCode",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "PsgcVersion",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "RegionCode",
                table: "Fights");
        }
    }
}
