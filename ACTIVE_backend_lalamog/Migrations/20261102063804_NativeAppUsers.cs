using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lalamog.Api.Migrations
{
    /// <inheritdoc />
    public partial class NativeAppUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FightConfirmations_AspNetUsers_UserId",
                table: "FightConfirmations");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_AspNetUsers_User1Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Matches_AspNetUsers_User2Id",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_AspNetUsers_SenderId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_AspNetUsers_UserId",
                table: "Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Swipes_AspNetUsers_SwiperId",
                table: "Swipes");

            migrationBuilder.DropForeignKey(
                name: "FK_Swipes_AspNetUsers_TargetId",
                table: "Swipes");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "RetiredAuthRoleClaims");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "RetiredAuthUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "RetiredAuthUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "RetiredAuthUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "RetiredAuthUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "RetiredAuthRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "RetiredAuthUsers");

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            // Keep every existing account ID/password; unused framework metadata stays archived.
            migrationBuilder.Sql("""
                INSERT INTO [AppUsers] ([Id], [Email], [NormalizedEmail], [PasswordHash], [AccessFailedCount], [LockoutEnd])
                SELECT [Id], [Email], COALESCE([NormalizedEmail], UPPER(LTRIM(RTRIM([Email])))),
                       [PasswordHash], [AccessFailedCount], [LockoutEnd]
                FROM [RetiredAuthUsers];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_NormalizedEmail",
                table: "AppUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FightConfirmations_AppUsers_UserId",
                table: "FightConfirmations",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_AppUsers_User1Id",
                table: "Matches",
                column: "User1Id",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_AppUsers_User2Id",
                table: "Matches",
                column: "User2Id",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_AppUsers_SenderId",
                table: "Messages",
                column: "SenderId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_AppUsers_UserId",
                table: "Profiles",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Swipes_AppUsers_SwiperId",
                table: "Swipes",
                column: "SwiperId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Swipes_AppUsers_TargetId",
                table: "Swipes",
                column: "TargetId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_FightConfirmations_AppUsers_UserId", table: "FightConfirmations");
            migrationBuilder.DropForeignKey(name: "FK_Matches_AppUsers_User1Id", table: "Matches");
            migrationBuilder.DropForeignKey(name: "FK_Matches_AppUsers_User2Id", table: "Matches");
            migrationBuilder.DropForeignKey(name: "FK_Messages_AppUsers_SenderId", table: "Messages");
            migrationBuilder.DropForeignKey(name: "FK_Profiles_AppUsers_UserId", table: "Profiles");
            migrationBuilder.DropForeignKey(name: "FK_Swipes_AppUsers_SwiperId", table: "Swipes");
            migrationBuilder.DropForeignKey(name: "FK_Swipes_AppUsers_TargetId", table: "Swipes");

            migrationBuilder.Sql("""
                UPDATE old
                SET old.[Email] = native.[Email], old.[NormalizedEmail] = native.[NormalizedEmail],
                    old.[UserName] = COALESCE(native.[Email], old.[UserName]),
                    old.[NormalizedUserName] = COALESCE(native.[NormalizedEmail], old.[NormalizedUserName]),
                    old.[PasswordHash] = native.[PasswordHash], old.[AccessFailedCount] = native.[AccessFailedCount],
                    old.[LockoutEnd] = native.[LockoutEnd]
                FROM [RetiredAuthUsers] old INNER JOIN [AppUsers] native ON native.[Id] = old.[Id];

                INSERT INTO [RetiredAuthUsers]
                    ([Id], [Email], [NormalizedEmail], [UserName], [NormalizedUserName], [PasswordHash],
                     [AccessFailedCount], [LockoutEnd], [EmailConfirmed], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled])
                SELECT native.[Id], native.[Email], native.[NormalizedEmail], native.[Email], native.[NormalizedEmail],
                       native.[PasswordHash], native.[AccessFailedCount], native.[LockoutEnd], 0, 0, 0, 1
                FROM [AppUsers] native
                WHERE NOT EXISTS (SELECT 1 FROM [RetiredAuthUsers] old WHERE old.[Id] = native.[Id]);
                """);

            migrationBuilder.DropTable(name: "AppUsers");
            migrationBuilder.RenameTable(name: "RetiredAuthUsers", newName: "AspNetUsers");
            migrationBuilder.RenameTable(name: "RetiredAuthRoles", newName: "AspNetRoles");
            migrationBuilder.RenameTable(name: "RetiredAuthRoleClaims", newName: "AspNetRoleClaims");
            migrationBuilder.RenameTable(name: "RetiredAuthUserClaims", newName: "AspNetUserClaims");
            migrationBuilder.RenameTable(name: "RetiredAuthUserLogins", newName: "AspNetUserLogins");
            migrationBuilder.RenameTable(name: "RetiredAuthUserRoles", newName: "AspNetUserRoles");
            migrationBuilder.RenameTable(name: "RetiredAuthUserTokens", newName: "AspNetUserTokens");
            migrationBuilder.AddForeignKey(name: "FK_FightConfirmations_AspNetUsers_UserId", table: "FightConfirmations",
                column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Matches_AspNetUsers_User1Id", table: "Matches",
                column: "User1Id", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Matches_AspNetUsers_User2Id", table: "Matches",
                column: "User2Id", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Messages_AspNetUsers_SenderId", table: "Messages",
                column: "SenderId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Profiles_AspNetUsers_UserId", table: "Profiles",
                column: "UserId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Swipes_AspNetUsers_SwiperId", table: "Swipes",
                column: "SwiperId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Swipes_AspNetUsers_TargetId", table: "Swipes",
                column: "TargetId", principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
    }
}
