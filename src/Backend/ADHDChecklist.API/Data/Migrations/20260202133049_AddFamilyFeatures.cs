using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHDChecklist.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CLEANUP GHOST SCHEMA (Idempotent Fix)
            migrationBuilder.Sql(@"
                DECLARE @sql nvarchar(max) = N'';
                
                -- 1. Drop FKs referencing Families
                SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + 
                    ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
                FROM sys.foreign_keys
                WHERE referenced_object_id = OBJECT_ID('Families');
                EXEC sp_executesql @sql;

                -- 2. Drop Indexes
                DROP INDEX IF EXISTS IX_Tasks_AssignedUserId ON Tasks;
                DROP INDEX IF EXISTS IX_Tasks_FamilyId ON Tasks;
                DROP INDEX IF EXISTS IX_Habits_FamilyId ON Habits;
                
                -- 3. Drop specific FKs on Tasks
                SET @sql = N'';
                SELECT @sql += N'ALTER TABLE Tasks DROP CONSTRAINT ' + QUOTENAME(name) + ';'
                FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID('Tasks') AND name LIKE '%AssignedUserId%';
                EXEC sp_executesql @sql;

                -- 4. Drop Default Constraints (for IsShared, etc)
                SET @sql = N'';
                SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
                    ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
                FROM sys.default_constraints
                WHERE parent_object_id = OBJECT_ID('Tasks') AND parent_column_id IN (
                    SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('Tasks') AND name IN ('IsShared', 'AssignedUserId', 'FamilyId')
                );
                EXEC sp_executesql @sql;

                SET @sql = N'';
                SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
                    ' DROP CONSTRAINT ' + QUOTENAME(name) + ';'
                FROM sys.default_constraints
                WHERE parent_object_id = OBJECT_ID('Habits') AND parent_column_id IN (
                    SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('Habits') AND name IN ('IsShared', 'FamilyId')
                );
                EXEC sp_executesql @sql;

                -- 5. Drop Tables
                DROP TABLE IF EXISTS FamilyMembers;
                DROP TABLE IF EXISTS FamilyInvitations;
                DROP TABLE IF EXISTS Families;
                
                -- 6. Drop Columns
                ALTER TABLE Tasks DROP COLUMN IF EXISTS AssignedUserId;
                ALTER TABLE Tasks DROP COLUMN IF EXISTS FamilyId;
                ALTER TABLE Tasks DROP COLUMN IF EXISTS IsShared;
                ALTER TABLE Habits DROP COLUMN IF EXISTS FamilyId;
                ALTER TABLE Habits DROP COLUMN IF EXISTS IsShared;
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "Habits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "Habits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlan = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Families_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FamilyInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyInvitations_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nickname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedUserId",
                table: "Tasks",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_FamilyId",
                table: "Tasks",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Habits_FamilyId",
                table: "Habits",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Families_OwnerId",
                table: "Families",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyInvitations_FamilyId",
                table: "FamilyInvitations",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_FamilyId",
                table: "FamilyMembers",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_UserId",
                table: "FamilyMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Habits_Families_FamilyId",
                table: "Habits",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Families_FamilyId",
                table: "Tasks",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_AssignedUserId",
                table: "Tasks",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Habits_Families_FamilyId",
                table: "Habits");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Families_FamilyId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_AssignedUserId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "FamilyInvitations");

            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.DropTable(
                name: "Families");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_AssignedUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_FamilyId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Habits_FamilyId",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "Habits");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "Habits");
        }
    }
}
