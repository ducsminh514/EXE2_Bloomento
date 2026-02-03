using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHDChecklist.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsMandatoryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMandatory",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMandatory",
                table: "Tasks");
        }
    }
}
