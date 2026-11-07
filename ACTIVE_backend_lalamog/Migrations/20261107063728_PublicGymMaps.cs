using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Migrations
{
    /// <inheritdoc />
    public partial class PublicGymMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MapId",
                table: "Fights",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MapAreas",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParentCode = table.Column<string>(type: "nvarchar(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapAreas", x => x.Code);
                    table.ForeignKey(
                        name: "FK_MapAreas_MapAreas_ParentCode",
                        column: x => x.ParentCode,
                        principalTable: "MapAreas",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    AreaCode = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    IsDemo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                    table.CheckConstraint("CK_Map_Coordinates", "[Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180");
                    table.ForeignKey(
                        name: "FK_Maps_MapAreas_AreaCode",
                        column: x => x.AreaCode,
                        principalTable: "MapAreas",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fights_MapId",
                table: "Fights",
                column: "MapId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Fight_SharedVenue",
                table: "Fights",
                sql: "[MapId] IS NULL OR ([Location] = '' AND [Address] = '' AND [RegionCode] IS NULL AND [ProvinceCode] IS NULL AND [CityCode] IS NULL AND [BarangayCode] IS NULL AND [PsgcVersion] IS NULL AND [Latitude] IS NULL AND [Longitude] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_MapAreas_ParentCode",
                table: "MapAreas",
                column: "ParentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_AreaCode",
                table: "Maps",
                column: "AreaCode");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_Slug",
                table: "Maps",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Fights_Maps_MapId",
                table: "Fights",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the public fight's venue snapshot when reverting to the old schema.
            migrationBuilder.DropCheckConstraint(name: "CK_Fight_SharedVenue", table: "Fights");
            migrationBuilder.Sql(@"
                UPDATE f SET [Location] = m.[Name], [Address] = m.[Address],
                    [Latitude] = m.[Latitude], [Longitude] = m.[Longitude], [MapId] = NULL
                FROM [Fights] f INNER JOIN [Maps] m ON f.[MapId] = m.[Id];");
            migrationBuilder.DropForeignKey(
                name: "FK_Fights_Maps_MapId",
                table: "Fights");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropTable(
                name: "MapAreas");

            migrationBuilder.DropIndex(
                name: "IX_Fights_MapId",
                table: "Fights");

            migrationBuilder.DropColumn(
                name: "MapId",
                table: "Fights");
        }
    }
}
