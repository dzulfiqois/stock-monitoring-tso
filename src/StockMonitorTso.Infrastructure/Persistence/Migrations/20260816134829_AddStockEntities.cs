using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMonitorTso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StokEntitas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Wilayah = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Produk = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Tier = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    TanggalStokAwal = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Stok = table.Column<decimal>(type: "TEXT", nullable: false),
                    DOT = table.Column<decimal>(type: "TEXT", nullable: false),
                    StokHabisTerjual = table.Column<decimal>(type: "TEXT", nullable: true),
                    StokIntransit = table.Column<decimal>(type: "TEXT", nullable: true),
                    Keterangan = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokEntitas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RencanaKedatangan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StokEntitasId = table.Column<int>(type: "INTEGER", nullable: false),
                    Urutan = table.Column<int>(type: "INTEGER", nullable: false),
                    NextSupply = table.Column<decimal>(type: "TEXT", nullable: false),
                    ETA = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RencanaKedatangan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RencanaKedatangan_StokEntitas_StokEntitasId",
                        column: x => x.StokEntitasId,
                        principalTable: "StokEntitas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RencanaKedatangan_StokEntitasId_Urutan",
                table: "RencanaKedatangan",
                columns: new[] { "StokEntitasId", "Urutan" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "Wilayah", "Produk", "Tier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RencanaKedatangan");

            migrationBuilder.DropTable(
                name: "StokEntitas");
        }
    }
}
