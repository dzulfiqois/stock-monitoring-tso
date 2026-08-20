using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMonitorTso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StokEntitas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StokEntitasId = table.Column<int>(type: "INTEGER", nullable: false),
                    StokEntitasTujuanId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Kuantitas = table.Column<decimal>(type: "TEXT", nullable: false),
                    Tanggal = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Catatan = table.Column<string>(type: "TEXT", nullable: true),
                    StokSumberSebelum = table.Column<decimal>(type: "TEXT", nullable: false),
                    StokSumberSesudah = table.Column<decimal>(type: "TEXT", nullable: false),
                    StokTujuanSebelum = table.Column<decimal>(type: "TEXT", nullable: true),
                    StokTujuanSesudah = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_StokEntitas_StokEntitasId",
                        column: x => x.StokEntitasId,
                        principalTable: "StokEntitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StokEntitasId",
                table: "StockTransactions",
                column: "StokEntitasId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StokEntitas");
        }
    }
}
