using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHDChecklist.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixGamificationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "FamilyRewards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "FamilyRewards",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyRewards_CreatorId",
                table: "FamilyRewards",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyRewards_Users_CreatorId",
                table: "FamilyRewards",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FamilyRewards_Users_CreatorId",
                table: "FamilyRewards");

            migrationBuilder.DropIndex(
                name: "IX_FamilyRewards_CreatorId",
                table: "FamilyRewards");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "FamilyRewards");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FamilyRewards");
        }
    }
}
