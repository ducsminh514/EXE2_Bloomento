using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHDChecklist.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreviousSubscriptionTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreviousSubscriptionTier",
                table: "Users",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousSubscriptionTier",
                table: "Users");
        }
    }
}
