using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMES.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLineStageSequence_RemoveProductionPlanStageSequenceNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionPlanStage_ProductionPlanId_SequenceNumber",
                table: "ProductionPlanStage");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "ProductionPlanStage");

            migrationBuilder.CreateTable(
                name: "LineStageSequence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineStageSequence", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LineStageSequence_LineId_SequenceNumber",
                table: "LineStageSequence",
                columns: new[] { "LineId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineStageSequence_LineId_StageId",
                table: "LineStageSequence",
                columns: new[] { "LineId", "StageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineStageSequence");

            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "ProductionPlanStage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlanStage_ProductionPlanId_SequenceNumber",
                table: "ProductionPlanStage",
                columns: new[] { "ProductionPlanId", "SequenceNumber" },
                unique: true);
        }
    }
}
