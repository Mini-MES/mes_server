using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mes_server.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyEquipmentOee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyEquipmentOees",
                columns: table => new
                {
                    DailyEquipmentOeeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedProductionMinutes = table.Column<int>(type: "int", nullable: false),
                    OperatingMinutes = table.Column<int>(type: "int", nullable: false),
                    DowntimeMinutes = table.Column<int>(type: "int", nullable: false),
                    TargetQty = table.Column<int>(type: "int", nullable: false),
                    TotalProducedQty = table.Column<int>(type: "int", nullable: false),
                    GoodQty = table.Column<int>(type: "int", nullable: false),
                    DefectQty = table.Column<int>(type: "int", nullable: false),
                    AvailabilityRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PerformanceRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    QualityRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OeePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ImportBatchID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyEquipmentOees", x => x.DailyEquipmentOeeID);
                    table.ForeignKey(
                        name: "FK_DailyEquipmentOees_Equipments_EquipmentID",
                        column: x => x.EquipmentID,
                        principalTable: "Equipments",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyEquipmentOees_EquipmentID_WorkDate",
                table: "DailyEquipmentOees",
                columns: new[] { "EquipmentID", "WorkDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyEquipmentOees");
        }
    }
}
