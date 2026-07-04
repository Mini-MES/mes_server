using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mes_server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadReasonMasters",
                columns: table => new
                {
                    ReasonCode = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    ReasonDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadReasonMasters", x => x.ReasonCode);
                });

            migrationBuilder.CreateTable(
                name: "ProcessMasters",
                columns: table => new
                {
                    ProcessID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessMasters", x => x.ProcessID);
                });

            migrationBuilder.CreateTable(
                name: "ProductMasters",
                columns: table => new
                {
                    ProductID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemType = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    StockQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMasters", x => x.ProductID);
                });

            migrationBuilder.CreateTable(
                name: "RawMaterials",
                columns: table => new
                {
                    MaterialID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StockQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawMaterials", x => x.MaterialID);
                });

            migrationBuilder.CreateTable(
                name: "Tools",
                columns: table => new
                {
                    ToolID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxUsageCount = table.Column<int>(type: "int", nullable: false),
                    CurrentUsageCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.ToolID);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetQty = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_WorkOrders_ProductMasters_ProductID",
                        column: x => x.ProductID,
                        principalTable: "ProductMasters",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BOMs",
                columns: table => new
                {
                    ProductID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaterialID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequiredQty = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOMs", x => new { x.ProductID, x.MaterialID });
                    table.ForeignKey(
                        name: "FK_BOMs_ProductMasters_ProductID",
                        column: x => x.ProductID,
                        principalTable: "ProductMasters",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BOMs_RawMaterials_MaterialID",
                        column: x => x.MaterialID,
                        principalTable: "RawMaterials",
                        principalColumn: "MaterialID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToolHistories",
                columns: table => new
                {
                    HistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToolID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeforeCount = table.Column<int>(type: "int", nullable: false),
                    Reson = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolHistories", x => x.HistoryID);
                    table.ForeignKey(
                        name: "FK_ToolHistories_Tools_ToolID",
                        column: x => x.ToolID,
                        principalTable: "Tools",
                        principalColumn: "ToolID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lots",
                columns: table => new
                {
                    LotID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    CurrentProcessID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lots", x => x.LotID);
                    table.ForeignKey(
                        name: "FK_Lots_ProcessMasters_CurrentProcessID",
                        column: x => x.CurrentProcessID,
                        principalTable: "ProcessMasters",
                        principalColumn: "ProcessID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Lots_WorkOrders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "WorkOrders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Performances",
                columns: table => new
                {
                    PerfID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcessID = table.Column<int>(type: "int", nullable: false),
                    ToolID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReasonCode = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    InputQty = table.Column<int>(type: "int", nullable: false),
                    GoodQty = table.Column<int>(type: "int", nullable: false),
                    BadQty = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Performances", x => x.PerfID);
                    table.ForeignKey(
                        name: "FK_Performances_BadReasonMasters_ReasonCode",
                        column: x => x.ReasonCode,
                        principalTable: "BadReasonMasters",
                        principalColumn: "ReasonCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Performances_Lots_LotID",
                        column: x => x.LotID,
                        principalTable: "Lots",
                        principalColumn: "LotID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Performances_ProcessMasters_ProcessID",
                        column: x => x.ProcessID,
                        principalTable: "ProcessMasters",
                        principalColumn: "ProcessID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Performances_Tools_ToolID",
                        column: x => x.ToolID,
                        principalTable: "Tools",
                        principalColumn: "ToolID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Performances_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BOMs_MaterialID",
                table: "BOMs",
                column: "MaterialID");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_CurrentProcessID",
                table: "Lots",
                column: "CurrentProcessID");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_OrderID",
                table: "Lots",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_LotID",
                table: "Performances",
                column: "LotID");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_ProcessID",
                table: "Performances",
                column: "ProcessID");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_ReasonCode",
                table: "Performances",
                column: "ReasonCode");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_ToolID",
                table: "Performances",
                column: "ToolID");

            migrationBuilder.CreateIndex(
                name: "IX_Performances_UserID",
                table: "Performances",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ToolHistories_ToolID",
                table: "ToolHistories",
                column: "ToolID");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_ProductID",
                table: "WorkOrders",
                column: "ProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BOMs");

            migrationBuilder.DropTable(
                name: "Performances");

            migrationBuilder.DropTable(
                name: "ToolHistories");

            migrationBuilder.DropTable(
                name: "RawMaterials");

            migrationBuilder.DropTable(
                name: "BadReasonMasters");

            migrationBuilder.DropTable(
                name: "Lots");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tools");

            migrationBuilder.DropTable(
                name: "ProcessMasters");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "ProductMasters");
        }
    }
}
