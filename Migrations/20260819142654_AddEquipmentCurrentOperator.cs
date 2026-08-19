using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mes_server.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentCurrentOperator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentOperatorId",
                table: "Equipments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_CurrentOperatorId",
                table: "Equipments",
                column: "CurrentOperatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Users_CurrentOperatorId",
                table: "Equipments",
                column: "CurrentOperatorId",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Users_CurrentOperatorId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_CurrentOperatorId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CurrentOperatorId",
                table: "Equipments");

        }
    }
}
