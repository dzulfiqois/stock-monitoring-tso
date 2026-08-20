using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMonitorTso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgenAndGudangWilayahTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas");

            migrationBuilder.AddColumn<int>(
                name: "AgenId",
                table: "StokEntitas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Agen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nama = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Wilayah = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TanggalDaftar = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Keterangan = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agen", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_AgenId_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "AgenId", "Produk", "Tier" },
                unique: true,
                filter: "[AgenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "Wilayah", "Produk", "Tier" },
                unique: true,
                filter: "[AgenId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Agen_Wilayah_Nama",
                table: "Agen",
                columns: new[] { "Wilayah", "Nama" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StokEntitas_Agen_AgenId",
                table: "StokEntitas",
                column: "AgenId",
                principalTable: "Agen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Data migration: baris agregat Agen lama (granularitas Wilayah×Produk) kini
            // merepresentasikan tier Gudang Wilayah pada hirarki Pusat→Gudang Wilayah→Agen→Outlet.
            migrationBuilder.Sql("UPDATE \"StokEntitas\" SET \"Tier\" = 'GudangWilayah' WHERE \"Tier\" = 'Agen'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StokEntitas_Agen_AgenId",
                table: "StokEntitas");

            migrationBuilder.DropTable(
                name: "Agen");

            migrationBuilder.DropIndex(
                name: "IX_StokEntitas_AgenId_Produk_Tier",
                table: "StokEntitas");

            migrationBuilder.DropIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas");

            migrationBuilder.DropColumn(
                name: "AgenId",
                table: "StokEntitas");

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "Wilayah", "Produk", "Tier" },
                unique: true);

            // Rollback data: kembalikan baris Gudang Wilayah menjadi tier Agen lama.
            migrationBuilder.Sql("UPDATE \"StokEntitas\" SET \"Tier\" = 'Agen' WHERE \"Tier\" = 'GudangWilayah'");
        }
    }
}
