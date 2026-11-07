using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Lalamog.Api.Migrations;
public partial class GymBookingMvp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.DropForeignKey("FK_Fights_Maps_MapId", "Fights");
            migrationBuilder.DropForeignKey("FK_Maps_Cities_CityId", "Maps");
            migrationBuilder.DropForeignKey("FK_Maps_Barangays_BarangayId_CityId", "Maps");
            migrationBuilder.DropPrimaryKey("PK_Maps", "Maps");
            migrationBuilder.RenameTable("Maps", newName: "Gyms");
            migrationBuilder.AddPrimaryKey("PK_Gyms", "Gyms", "Id");
            migrationBuilder.RenameIndex(name: "IX_Maps_Slug", table: "Gyms", newName: "IX_Gyms_Slug");
            migrationBuilder.RenameIndex(name: "IX_Maps_CityId", table: "Gyms", newName: "IX_Gyms_CityId");
            migrationBuilder.RenameIndex(name: "IX_Maps_BarangayId_CityId", table: "Gyms", newName: "IX_Gyms_BarangayId_CityId");
            migrationBuilder.DropIndex("IX_Fights_MapId", "Fights");
            migrationBuilder.DropIndex("IX_Fights_MatchId", "Fights");
            migrationBuilder.DropCheckConstraint("CK_Fight_SharedVenue", "Fights");
            migrationBuilder.DropCheckConstraint("CK_Fight_Status", "Fights");
            migrationBuilder.RenameColumn("MapId", "Fights", "GymId");
            migrationBuilder.AddColumn<Guid>("OwnerUserId", "Gyms", "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<string>("Description", "Gyms", "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>("ContactInformation", "Gyms", "nvarchar(300)", maxLength: 300, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<decimal>("SessionPrice", "Gyms", "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<int>("SessionDurationMinutes", "Gyms", "int", nullable: false, defaultValue: 60);
            migrationBuilder.AddColumn<bool>("IsPublished", "Gyms", "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<byte[]>("Version", "Gyms", "rowversion", rowVersion: true, nullable: false);
            migrationBuilder.AddColumn<decimal>("QuotedPrice", "Fights", "decimal(10,2)", precision: 10, scale: 2, nullable: true);
            migrationBuilder.AddColumn<int>("SessionDurationMinutes", "Fights", "int", nullable: true);
            migrationBuilder.AddColumn<bool>("AllowPublicIdentity", "FightConfirmations", "bit", nullable: false, defaultValue: false);
            migrationBuilder.Sql("UPDATE Roles SET Name='Fighter' WHERE Name='User'; UPDATE Fights SET QuotedPrice=0,SessionDurationMinutes=60 WHERE GymId IS NOT NULL;");
            migrationBuilder.AddCheckConstraint("CK_Gym_Details", "Gyms", "[SessionPrice] >= 0 AND [SessionDurationMinutes] BETWEEN 30 AND 240 AND ([IsPublished] = 0 OR [OwnerUserId] IS NOT NULL)");
            migrationBuilder.CreateIndex("IX_Gyms_OwnerUserId", "Gyms", "OwnerUserId");
            migrationBuilder.AddForeignKey("FK_Gyms_AppUsers_OwnerUserId", "Gyms", "OwnerUserId", "AppUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.CreateIndex("IX_Fights_MatchId", "Fights", "MatchId", unique: true, filter: "[Status] IN ('pending_approval', 'scheduled', 'confirmed')");
            migrationBuilder.AddCheckConstraint("CK_Fight_Status", "Fights", "[Status] IN ('pending_approval', 'scheduled', 'confirmed', 'canceled', 'completed')");
            migrationBuilder.AddCheckConstraint("CK_Fight_SharedVenue", "Fights", "[GymId] IS NULL OR ([Location] = '' AND [Address] = '' AND [RegionCode] IS NULL AND [ProvinceCode] IS NULL AND [CityCode] IS NULL AND [BarangayCode] IS NULL AND [PsgcVersion] IS NULL AND [Latitude] IS NULL AND [Longitude] IS NULL)");
            migrationBuilder.CreateIndex("IX_Fights_GymId_ScheduledAt", "Fights", new[] { "GymId", "ScheduledAt" });
            migrationBuilder.AddForeignKey("FK_Fights_Gyms_GymId", "Fights", "GymId", "Gyms", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_Gyms_Cities_CityId", "Gyms", "CityId", "Cities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_Gyms_Barangays_BarangayId_CityId", "Gyms", new[] { "BarangayId", "CityId" }, "Barangays", principalColumns: new[] { "Id", "CityId" }, onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM Gyms WHERE OwnerUserId IS NOT NULL) OR EXISTS(SELECT 1 FROM Fights WHERE Status='pending_approval') THROW 51020,'Gym ownership or booking requests require a matching backup for rollback. No data was removed.',1;");
            migrationBuilder.DropForeignKey("FK_Fights_Gyms_GymId", "Fights");
            migrationBuilder.DropForeignKey("FK_Gyms_AppUsers_OwnerUserId", "Gyms");
            migrationBuilder.DropForeignKey("FK_Gyms_Cities_CityId", "Gyms");
            migrationBuilder.DropForeignKey("FK_Gyms_Barangays_BarangayId_CityId", "Gyms");
            migrationBuilder.DropCheckConstraint("CK_Gym_Details", "Gyms");
            migrationBuilder.DropCheckConstraint("CK_Fight_SharedVenue", "Fights");
            migrationBuilder.DropCheckConstraint("CK_Fight_Status", "Fights");
            migrationBuilder.DropIndex("IX_Fights_GymId_ScheduledAt", "Fights");
            migrationBuilder.DropIndex("IX_Fights_MatchId", "Fights");
            migrationBuilder.DropIndex("IX_Gyms_OwnerUserId", "Gyms");
            migrationBuilder.DropColumn("OwnerUserId", "Gyms");
            migrationBuilder.DropColumn("Description", "Gyms");
            migrationBuilder.DropColumn("ContactInformation", "Gyms");
            migrationBuilder.DropColumn("SessionPrice", "Gyms");
            migrationBuilder.DropColumn("SessionDurationMinutes", "Gyms");
            migrationBuilder.DropColumn("IsPublished", "Gyms");
            migrationBuilder.DropColumn("Version", "Gyms");
            migrationBuilder.DropColumn("QuotedPrice", "Fights");
            migrationBuilder.DropColumn("SessionDurationMinutes", "Fights");
            migrationBuilder.DropColumn("AllowPublicIdentity", "FightConfirmations");
            migrationBuilder.DropPrimaryKey("PK_Gyms", "Gyms");
            migrationBuilder.RenameTable("Gyms", newName: "Maps");
            migrationBuilder.AddPrimaryKey("PK_Maps", "Maps", "Id");
            migrationBuilder.RenameColumn("GymId", "Fights", "MapId");
            migrationBuilder.RenameIndex(name: "IX_Gyms_Slug", table: "Maps", newName: "IX_Maps_Slug");
            migrationBuilder.RenameIndex(name: "IX_Gyms_CityId", table: "Maps", newName: "IX_Maps_CityId");
            migrationBuilder.RenameIndex(name: "IX_Gyms_BarangayId_CityId", table: "Maps", newName: "IX_Maps_BarangayId_CityId");
            migrationBuilder.CreateIndex("IX_Fights_MatchId", "Fights", "MatchId", unique: true, filter: "[Status] IN ('scheduled', 'confirmed')");
            migrationBuilder.AddCheckConstraint("CK_Fight_Status", "Fights", "[Status] IN ('scheduled', 'confirmed', 'canceled', 'completed')");
            migrationBuilder.AddCheckConstraint("CK_Fight_SharedVenue", "Fights", "[MapId] IS NULL OR ([Location] = '' AND [Address] = '' AND [RegionCode] IS NULL AND [ProvinceCode] IS NULL AND [CityCode] IS NULL AND [BarangayCode] IS NULL AND [PsgcVersion] IS NULL AND [Latitude] IS NULL AND [Longitude] IS NULL)");
            migrationBuilder.CreateIndex("IX_Fights_MapId", "Fights", "MapId");
            migrationBuilder.AddForeignKey("FK_Fights_Maps_MapId", "Fights", "MapId", "Maps", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_Maps_Cities_CityId", "Maps", "CityId", "Cities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_Maps_Barangays_BarangayId_CityId", "Maps", new[] { "BarangayId", "CityId" }, "Barangays", principalColumns: new[] { "Id", "CityId" }, onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("UPDATE Roles SET Name='User' WHERE Name='Fighter';");
    }
}
