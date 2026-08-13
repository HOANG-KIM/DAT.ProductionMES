using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMES.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TagCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    WorkStationId = table.Column<int>(type: "int", nullable: false),
                    ProductionPlanId = table.Column<int>(type: "int", nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Result = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RejectionReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scan_Line_LineId",
                        column: x => x.LineId,
                        principalTable: "Line",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scan_ProductionPlan_ProductionPlanId",
                        column: x => x.ProductionPlanId,
                        principalTable: "ProductionPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scan_Stage_StageId",
                        column: x => x.StageId,
                        principalTable: "Stage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scan_WorkStation_WorkStationId",
                        column: x => x.WorkStationId,
                        principalTable: "WorkStation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_LineId",
                table: "Scan",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_ProductionPlanId",
                table: "Scan",
                column: "ProductionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_ScannedAtUtc",
                table: "Scan",
                column: "ScannedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_StageId",
                table: "Scan",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_TagCode_StageId",
                table: "Scan",
                columns: new[] { "TagCode", "StageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Scan_WorkStationId",
                table: "Scan",
                column: "WorkStationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Scan");
        }
    }
}
