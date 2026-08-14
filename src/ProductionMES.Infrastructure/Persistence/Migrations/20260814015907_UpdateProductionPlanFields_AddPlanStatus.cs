using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMES.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductionPlanFields_AddPlanStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlan_LineId",
                table: "ProductionPlan",
                column: "LineId");

            migrationBuilder.DropIndex(
                name: "IX_ProductionPlan_LineId_IsActive",
                table: "ProductionPlan");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductionPlan");

            migrationBuilder.DropColumn(
                name: "Shift",
                table: "ProductionPlan");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "ProductionPlan",
                newName: "Model");

            migrationBuilder.RenameColumn(
                name: "ProductCode",
                table: "ProductionPlan",
                newName: "Lot");

            migrationBuilder.RenameColumn(
                name: "EffectiveDate",
                table: "ProductionPlan",
                newName: "StartTime");

            migrationBuilder.AddColumn<int>(
                name: "LineId",
                table: "ProductionPlanStage",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PlanStatus",
                table: "ProductionPlanStage",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Customer",
                table: "ProductionPlan",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OperatorNames",
                table: "ProductionPlan",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Revision",
                table: "ProductionPlan",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlanStage_LineId_StageId_PlanStatus",
                table: "ProductionPlanStage",
                columns: new[] { "LineId", "StageId", "PlanStatus" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlanStage_Line_LineId",
                table: "ProductionPlanStage",
                column: "LineId",
                principalTable: "Line",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionPlanStage_Line_LineId",
                table: "ProductionPlanStage");

            migrationBuilder.DropIndex(
                name: "IX_ProductionPlanStage_LineId_StageId_PlanStatus",
                table: "ProductionPlanStage");

            migrationBuilder.DropIndex(
                name: "IX_ProductionPlan_LineId",
                table: "ProductionPlan");

            migrationBuilder.DropColumn(
                name: "LineId",
                table: "ProductionPlanStage");

            migrationBuilder.DropColumn(
                name: "PlanStatus",
                table: "ProductionPlanStage");

            migrationBuilder.DropColumn(
                name: "Customer",
                table: "ProductionPlan");

            migrationBuilder.DropColumn(
                name: "OperatorNames",
                table: "ProductionPlan");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "ProductionPlan");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "ProductionPlan",
                newName: "EffectiveDate");

            migrationBuilder.RenameColumn(
                name: "Model",
                table: "ProductionPlan",
                newName: "ProductName");

            migrationBuilder.RenameColumn(
                name: "Lot",
                table: "ProductionPlan",
                newName: "ProductCode");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductionPlan",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Shift",
                table: "ProductionPlan",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlan_LineId_IsActive",
                table: "ProductionPlan",
                columns: new[] { "LineId", "IsActive" });
        }
    }
}
