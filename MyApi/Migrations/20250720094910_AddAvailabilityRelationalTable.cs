using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MyApi.Models;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityRelationalTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Availability",
                table: "Profiles");

            migrationBuilder.CreateTable(
                name: "Availabilities",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Days = table.Column<string[]>(type: "text[]", nullable: true),
                    Time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Availabilities", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Availabilities_Profiles_UserId",
                        column: x => x.UserId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Availabilities");

            migrationBuilder.AddColumn<Availability>(
                name: "Availability",
                table: "Profiles",
                type: "jsonb",
                nullable: true);
        }
    }
}
