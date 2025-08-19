using Microsoft.EntityFrameworkCore.Migrations;
using MyApi.Models;
using NetTopologySuite.Geometries;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class MakeLocationAndAvailabilityNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Profiles",
                type: "geography",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geography");

            migrationBuilder.AlterColumn<Availability>(
                name: "Availability",
                table: "Profiles",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(Availability),
                oldType: "jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "Location",
                table: "Profiles",
                type: "geography",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography",
                oldNullable: true);

            migrationBuilder.AlterColumn<Availability>(
                name: "Availability",
                table: "Profiles",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(Availability),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
