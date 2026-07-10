using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mes_server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBomForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BOMs_RawMaterials_MaterialID",
                table: "BOMs");

            migrationBuilder.DropIndex(
                name: "IX_BOMs_MaterialID",
                table: "BOMs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BOMs_MaterialID",
                table: "BOMs",
                column: "MaterialID");

            migrationBuilder.AddForeignKey(
                name: "FK_BOMs_RawMaterials_MaterialID",
                table: "BOMs",
                column: "MaterialID",
                principalTable: "RawMaterials",
                principalColumn: "MaterialID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
