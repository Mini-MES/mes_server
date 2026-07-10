using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mes_server.Migrations
{
    /// <inheritdoc />
    public partial class UnifyProductsAndModifyBOM : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SafetyStock",
                table: "ProductMasters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "INSERT INTO ProductMasters (ProductID, ProductName, StockQty, SafetyStock, ItemType) " +
                "SELECT MaterialID, MaterialName, StockQty, SafetyStock, 2 FROM RawMaterials WHERE MaterialID NOT IN (SELECT ProductID FROM ProductMasters)");

            migrationBuilder.DropTable(
                name: "RawMaterials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BOMs",
                table: "BOMs");

            migrationBuilder.AddColumn<string>(
                name: "ChildProductID",
                table: "BOMs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE BOMs SET ChildProductID = MaterialID");

            migrationBuilder.DropColumn(
                name: "MaterialID",
                table: "BOMs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BOMs",
                table: "BOMs",
                columns: new[] { "ProductID", "ChildProductID", "ProcessID" });

            migrationBuilder.CreateIndex(
                name: "IX_BOMs_ChildProductID",
                table: "BOMs",
                column: "ChildProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_BOMs_ProductMasters_ChildProductID",
                table: "BOMs",
                column: "ChildProductID",
                principalTable: "ProductMasters",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BOMs_ProductMasters_ChildProductID",
                table: "BOMs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BOMs",
                table: "BOMs");

            migrationBuilder.DropIndex(
                name: "IX_BOMs_ChildProductID",
                table: "BOMs");

            migrationBuilder.DropColumn(
                name: "SafetyStock",
                table: "ProductMasters");

            migrationBuilder.DropColumn(
                name: "ChildProductID",
                table: "BOMs");

            migrationBuilder.AddColumn<string>(
                name: "MaterialID",
                table: "BOMs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BOMs",
                table: "BOMs",
                columns: new[] { "ProductID", "MaterialID", "ProcessID" });

            migrationBuilder.CreateTable(
                name: "RawMaterials",
                columns: table => new
                {
                    MaterialID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SafetyStock = table.Column<int>(type: "int", nullable: false),
                    StockQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawMaterials", x => x.MaterialID);
                });
        }
    }
}
