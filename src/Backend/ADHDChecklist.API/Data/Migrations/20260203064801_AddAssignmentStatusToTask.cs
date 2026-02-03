using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHDChecklist.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentStatusToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentStatus",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignmentStatus",
                table: "Tasks");
        }
    }
}
