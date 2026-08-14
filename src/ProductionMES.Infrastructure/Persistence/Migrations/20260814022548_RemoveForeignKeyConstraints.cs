using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMES.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BreakWindow_Line_LineId",
                table: "BreakWindow");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionPlan_Line_LineId",
                table: "ProductionPlan");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionPlanStage_Line_LineId",
                table: "ProductionPlanStage");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionPlanStage_ProductionPlan_ProductionPlanId",
                table: "ProductionPlanStage");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionPlanStage_Stage_StageId",
                table: "ProductionPlanStage");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshToken_User_UserId",
                table: "RefreshToken");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermission_Permission_PermissionId",
                table: "RolePermission");

            migrationBuilder.DropForeignKey(
                name: "FK_Scan_Line_LineId",
                table: "Scan");

            migrationBuilder.DropForeignKey(
                name: "FK_Scan_ProductionPlan_ProductionPlanId",
                table: "Scan");

            migrationBuilder.DropForeignKey(
                name: "FK_Scan_Stage_StageId",
                table: "Scan");

            migrationBuilder.DropForeignKey(
                name: "FK_Scan_WorkStation_WorkStationId",
                table: "Scan");

            migrationBuilder.DropForeignKey(
                name: "FK_StationApiKey_WorkStation_WorkStationId",
                table: "StationApiKey");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkStation_Line_LineId",
                table: "WorkStation");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkStation_Stage_StageId",
                table: "WorkStation");

            migrationBuilder.DropIndex(
                name: "IX_WorkStation_LineId",
                table: "WorkStation");

            migrationBuilder.DropIndex(
                name: "IX_WorkStation_StageId",
                table: "WorkStation");

            migrationBuilder.DropIndex(
                name: "IX_Scan_LineId",
                table: "Scan");

            migrationBuilder.DropIndex(
                name: "IX_Scan_ProductionPlanId",
                table: "Scan");

            migrationBuilder.DropIndex(
                name: "IX_Scan_StageId",
                table: "Scan");

            migrationBuilder.DropIndex(
                name: "IX_Scan_WorkStationId",
                table: "Scan");

            migrationBuilder.DropIndex(
                name: "IX_RolePermission_PermissionId",
                table: "RolePermission");

            migrationBuilder.DropIndex(
                name: "IX_ProductionPlanStage_StageId",
                table: "ProductionPlanStage");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_ProductionPlanId_StageId",
                table: "Scan",
                columns: new[] { "ProductionPlanId", "StageId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scan_ProductionPlanId_StageId",
                table: "Scan");

            migrationBuilder.CreateIndex(
                name: "IX_WorkStation_LineId",
                table: "WorkStation",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkStation_StageId",
                table: "WorkStation",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_LineId",
                table: "Scan",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_ProductionPlanId",
                table: "Scan",
                column: "ProductionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_StageId",
                table: "Scan",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_Scan_WorkStationId",
                table: "Scan",
                column: "WorkStationId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPlanStage_StageId",
                table: "ProductionPlanStage",
                column: "StageId");

            migrationBuilder.AddForeignKey(
                name: "FK_BreakWindow_Line_LineId",
                table: "BreakWindow",
                column: "LineId",
                principalTable: "Line",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlan_Line_LineId",
                table: "ProductionPlan",
                column: "LineId",
                principalTable: "Line",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlanStage_Line_LineId",
                table: "ProductionPlanStage",
                column: "LineId",
                principalTable: "Line",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlanStage_ProductionPlan_ProductionPlanId",
                table: "ProductionPlanStage",
                column: "ProductionPlanId",
                principalTable: "ProductionPlan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionPlanStage_Stage_StageId",
                table: "ProductionPlanStage",
                column: "StageId",
                principalTable: "Stage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshToken_User_UserId",
                table: "RefreshToken",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermission_Permission_PermissionId",
                table: "RolePermission",
                column: "PermissionId",
                principalTable: "Permission",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Scan_Line_LineId",
                table: "Scan",
                column: "LineId",
                principalTable: "Line",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scan_ProductionPlan_ProductionPlanId",
                table: "Scan",
                column: "ProductionPlanId",
                principalTable: "ProductionPlan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scan_Stage_StageId",
                table: "Scan",
                column: "StageId",
                principalTable: "Stage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scan_WorkStation_WorkStationId",
                table: "Scan",
                column: "WorkStationId",
                principalTable: "WorkStation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StationApiKey_WorkStation_WorkStationId",
                table: "StationApiKey",
                column: "WorkStationId",
                principalTable: "WorkStation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkStation_Line_LineId",
                table: "WorkStation",
                column: "LineId",
                principalTable: "Line",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkStation_Stage_StageId",
                table: "WorkStation",
                column: "StageId",
                principalTable: "Stage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
