using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHDChecklist.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectionReasonToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Tasks");
        }
    }
}
