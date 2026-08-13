using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionMES.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStationApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StationApiKey",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WorkStationId = table.Column<int>(type: "int", nullable: false),
                    KeyHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationApiKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StationApiKey_WorkStation_WorkStationId",
                        column: x => x.WorkStationId,
                        principalTable: "WorkStation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StationApiKey_KeyHash",
                table: "StationApiKey",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StationApiKey_WorkStationId",
                table: "StationApiKey",
                column: "WorkStationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StationApiKey");
        }
    }
}
