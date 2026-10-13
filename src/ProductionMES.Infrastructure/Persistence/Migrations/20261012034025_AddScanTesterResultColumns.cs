using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMES.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScanTesterResultColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                table: "Scan",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GoldenCrc",
                table: "Scan",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "TestedAtUtc",
                table: "Scan",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TesterBootId",
                table: "Scan",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TesterSeq",
                table: "Scan",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scan_WorkStationId_TesterBootId_TesterSeq",
                table: "Scan",
                columns: new[] { "WorkStationId", "TesterBootId", "TesterSeq" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scan_WorkStationId_TesterBootId_TesterSeq",
                table: "Scan");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                table: "Scan");

            migrationBuilder.DropColumn(
                name: "GoldenCrc",
                table: "Scan");

            migrationBuilder.DropColumn(
                name: "TestedAtUtc",
                table: "Scan");

            migrationBuilder.DropColumn(
                name: "TesterBootId",
                table: "Scan");

            migrationBuilder.DropColumn(
                name: "TesterSeq",
                table: "Scan");
        }
    }
}
