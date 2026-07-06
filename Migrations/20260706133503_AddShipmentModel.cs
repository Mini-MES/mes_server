using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mes_server.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WorkOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SafetyStock",
                table: "RawMaterials",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkOrderID",
                table: "Performances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProcessID",
                table: "BOMs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    ShipmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkOrderID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShipmentDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.ShipmentID);
                    table.ForeignKey(
                        name: "FK_Shipments_ProductMasters_ProductID",
                        column: x => x.ProductID,
                        principalTable: "ProductMasters",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_WorkOrders_WorkOrderID",
                        column: x => x.WorkOrderID,
                        principalTable: "WorkOrders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Performances_WorkOrderID",
                table: "Performances",
                column: "WorkOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ProductID",
                table: "Shipments",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_WorkOrderID",
                table: "Shipments",
                column: "WorkOrderID");

            migrationBuilder.AddForeignKey(
                name: "FK_Performances_WorkOrders_WorkOrderID",
                table: "Performances",
                column: "WorkOrderID",
                principalTable: "WorkOrders",
                principalColumn: "OrderID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Performances_WorkOrders_WorkOrderID",
                table: "Performances");

            migrationBuilder.DropTable(
                name: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Performances_WorkOrderID",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SafetyStock",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "WorkOrderID",
                table: "Performances");

            migrationBuilder.DropColumn(
                name: "ProcessID",
                table: "BOMs");
        }
    }
}
