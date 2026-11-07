using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExplicitMapGeography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maps_MapAreas_AreaCode",
                table: "Maps");


            migrationBuilder.DropIndex(
                name: "IX_Maps_AreaCode",
                table: "Maps");


            migrationBuilder.AddColumn<int>(
                name: "BarangayId",
                table: "Maps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Maps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Provinces_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProvinceId = table.Column<int>(type: "int", nullable: true),
                    RegionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.CheckConstraint("CK_City_Parent", "([ProvinceId] IS NOT NULL AND [RegionId] IS NULL) OR ([ProvinceId] IS NULL AND [RegionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Cities_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cities_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Barangays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barangays", x => x.Id);
                    table.UniqueConstraint("AK_Barangays_Id_CityId", x => new { x.Id, x.CityId });
                    table.ForeignKey(
                        name: "FK_Barangays_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Maps_BarangayId_CityId",
                table: "Maps",
                columns: new[] { "BarangayId", "CityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Maps_CityId",
                table: "Maps",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Barangays_CityId",
                table: "Barangays",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Barangays_Code",
                table: "Barangays",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Code",
                table: "Cities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_ProvinceId",
                table: "Cities",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_RegionId",
                table: "Cities",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_Code",
                table: "Provinces",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_RegionId",
                table: "Provinces",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_Code",
                table: "Regions",
                column: "Code",
                unique: true);

            migrationBuilder.Sql(@"
INSERT INTO Regions (Code,Name) SELECT Code,Name FROM MapAreas WHERE Level='region';
INSERT INTO Provinces (Code,Name,RegionId) SELECT a.Code,a.Name,r.Id FROM MapAreas a JOIN Regions r ON r.Code=a.ParentCode WHERE a.Level='province';
INSERT INTO Cities (Code,Name,ProvinceId,RegionId)
SELECT a.Code,a.Name,p.Id,CASE WHEN p.Id IS NULL THEN r.Id END
FROM MapAreas a LEFT JOIN Provinces p ON p.Code=a.ParentCode LEFT JOIN Regions r ON r.Code=a.ParentCode WHERE a.Level='city';
INSERT INTO Barangays (Code,Name,CityId)
SELECT a.Code,a.Name,c.Id FROM MapAreas a LEFT JOIN MapAreas parent ON parent.Code=a.ParentCode
JOIN Cities c ON c.Code=CASE WHEN parent.Level='city' THEN parent.Code ELSE parent.ParentCode END WHERE a.Level='barangay';
UPDATE m SET CityId=COALESCE(c.Id,b.CityId),BarangayId=b.Id FROM Maps m
LEFT JOIN Cities c ON c.Code=m.AreaCode LEFT JOIN Barangays b ON b.Code=m.AreaCode;
IF EXISTS(SELECT 1 FROM Maps WHERE CityId=0) THROW 51010,'A saved venue has no recognized city. Migration stopped without deleting its location.',1;
");
            migrationBuilder.DropTable(
                name: "MapAreas");
            migrationBuilder.DropColumn(
                name: "AreaCode",
                table: "Maps");
            migrationBuilder.AddForeignKey(
                name: "FK_Maps_Barangays_BarangayId_CityId",
                table: "Maps",
                columns: new[] { "BarangayId", "CityId" },
                principalTable: "Barangays",
                principalColumns: new[] { "Id", "CityId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_Cities_CityId",
                table: "Maps",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maps_Barangays_BarangayId_CityId",
                table: "Maps");

            migrationBuilder.DropForeignKey(
                name: "FK_Maps_Cities_CityId",
                table: "Maps");





            migrationBuilder.DropIndex(
                name: "IX_Maps_BarangayId_CityId",
                table: "Maps");

            migrationBuilder.DropIndex(
                name: "IX_Maps_CityId",
                table: "Maps");



            migrationBuilder.AddColumn<string>(
                name: "AreaCode",
                table: "Maps",
                type: "nvarchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MapAreas",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ParentCode = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
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

            migrationBuilder.Sql(@"
INSERT INTO MapAreas (Code,Name,Level,ParentCode) SELECT Code,Name,'region',NULL FROM Regions;
INSERT INTO MapAreas (Code,Name,Level,ParentCode) SELECT p.Code,p.Name,'province',r.Code FROM Provinces p JOIN Regions r ON r.Id=p.RegionId;
INSERT INTO MapAreas (Code,Name,Level,ParentCode) SELECT c.Code,c.Name,'city',COALESCE(p.Code,r.Code) FROM Cities c LEFT JOIN Provinces p ON p.Id=c.ProvinceId LEFT JOIN Regions r ON r.Id=c.RegionId;
INSERT INTO MapAreas (Code,Name,Level,ParentCode) SELECT b.Code,b.Name,'barangay',c.Code FROM Barangays b JOIN Cities c ON c.Id=b.CityId;
UPDATE m SET AreaCode=COALESCE(b.Code,c.Code) FROM Maps m JOIN Cities c ON c.Id=m.CityId LEFT JOIN Barangays b ON b.Id=m.BarangayId;
");
            migrationBuilder.DropTable(
                name: "Barangays");
            migrationBuilder.DropTable(
                name: "Cities");
            migrationBuilder.DropTable(
                name: "Provinces");
            migrationBuilder.DropTable(
                name: "Regions");
            migrationBuilder.DropColumn(
                name: "BarangayId",
                table: "Maps");
            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Maps");
            migrationBuilder.CreateIndex(
                name: "IX_Maps_AreaCode",
                table: "Maps",
                column: "AreaCode");

            migrationBuilder.CreateIndex(
                name: "IX_MapAreas_ParentCode",
                table: "MapAreas",
                column: "ParentCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_MapAreas_AreaCode",
                table: "Maps",
                column: "AreaCode",
                principalTable: "MapAreas",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
