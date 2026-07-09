using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mes_server.Migrations
{
    /// <inheritdoc />
    public partial class AddToolStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reson",
                table: "ToolHistories");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tools",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Reason",
                table: "ToolHistories",
                type: "int",
                maxLength: 20,
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tools");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ToolHistories");

            migrationBuilder.AddColumn<string>(
                name: "Reson",
                table: "ToolHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
