using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mes_server.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DowntimeReasonMasters",
                columns: table => new
                {
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReasonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DowntimeReasonMasters", x => x.ReasonCode);
                });

            migrationBuilder.CreateTable(
                name: "Equipments",
                columns: table => new
                {
                    EquipmentID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentLotId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalRunningSeconds = table.Column<long>(type: "bigint", nullable: false),
                    TotalDowntimeSeconds = table.Column<long>(type: "bigint", nullable: false),
                    LastStatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipments", x => x.EquipmentID);
                });

            migrationBuilder.CreateTable(
                name: "DowntimeLogs",
                columns: table => new
                {
                    DowntimeLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperatorMemo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DowntimeLogs", x => x.DowntimeLogID);
                    table.ForeignKey(
                        name: "FK_DowntimeLogs_DowntimeReasonMasters_ReasonCode",
                        column: x => x.ReasonCode,
                        principalTable: "DowntimeReasonMasters",
                        principalColumn: "ReasonCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DowntimeLogs_Equipments_EquipmentID",
                        column: x => x.EquipmentID,
                        principalTable: "Equipments",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DowntimeLogs_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeLogs_EquipmentID",
                table: "DowntimeLogs",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeLogs_ReasonCode",
                table: "DowntimeLogs",
                column: "ReasonCode");

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeLogs_UserID",
                table: "DowntimeLogs",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DowntimeLogs");

            migrationBuilder.DropTable(
                name: "DowntimeReasonMasters");

            migrationBuilder.DropTable(
                name: "Equipments");
        }
    }
}
