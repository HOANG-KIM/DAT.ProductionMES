using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMES.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackingBoxAndStagePackingFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPackingStage",
                table: "Stage",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PackingBox",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductionPlanId = table.Column<int>(type: "int", nullable: false),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    WorkStationId = table.Column<int>(type: "int", nullable: false),
                    BoxNo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetQuantity = table.Column<int>(type: "int", nullable: false),
                    ScannedQuantity = table.Column<int>(type: "int", nullable: false),
                    PackingModelConfigId = table.Column<int>(type: "int", nullable: false),
                    ModelSnapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartNameSnapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ManufacturerSnapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GrossWeightSnapshot = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingBox", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PackingDuplicateScanConfirmation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TagCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    ScanId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedByUserName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingDuplicateScanConfirmation", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PackingBox_ProductionPlanId_StageId_Status",
                table: "PackingBox",
                columns: new[] { "ProductionPlanId", "StageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PackingDuplicateScanConfirmation_TagCode_StageId",
                table: "PackingDuplicateScanConfirmation",
                columns: new[] { "TagCode", "StageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackingBox");

            migrationBuilder.DropTable(
                name: "PackingDuplicateScanConfirmation");

            migrationBuilder.DropColumn(
                name: "IsPackingStage",
                table: "Stage");
        }
    }
}
