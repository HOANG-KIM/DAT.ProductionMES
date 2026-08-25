using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMES.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScanPackingBoxId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackingBoxId",
                table: "Scan",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scan_PackingBoxId",
                table: "Scan",
                column: "PackingBoxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scan_PackingBoxId",
                table: "Scan");

            migrationBuilder.DropColumn(
                name: "PackingBoxId",
                table: "Scan");
        }
    }
}
