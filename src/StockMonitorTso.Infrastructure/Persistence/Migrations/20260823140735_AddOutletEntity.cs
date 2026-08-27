using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMonitorTso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas");

            migrationBuilder.AddColumn<int>(
                name: "OutletId",
                table: "StokEntitas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Outlet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nama = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    AgenId = table.Column<int>(type: "INTEGER", nullable: false),
                    Wilayah = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TanggalDaftar = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Keterangan = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outlet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Outlet_Agen_AgenId",
                        column: x => x.AgenId,
                        principalTable: "Agen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_OutletId_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "OutletId", "Produk", "Tier" },
                unique: true,
                filter: "[OutletId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "Wilayah", "Produk", "Tier" },
                unique: true,
                filter: "[AgenId] IS NULL AND [OutletId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Outlet_AgenId_Nama",
                table: "Outlet",
                columns: new[] { "AgenId", "Nama" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StokEntitas_Outlet_OutletId",
                table: "StokEntitas",
                column: "OutletId",
                principalTable: "Outlet",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StokEntitas_Outlet_OutletId",
                table: "StokEntitas");

            migrationBuilder.DropTable(
                name: "Outlet");

            migrationBuilder.DropIndex(
                name: "IX_StokEntitas_OutletId_Produk_Tier",
                table: "StokEntitas");

            migrationBuilder.DropIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas");

            migrationBuilder.DropColumn(
                name: "OutletId",
                table: "StokEntitas");

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "Wilayah", "Produk", "Tier" },
                unique: true,
                filter: "[AgenId] IS NULL");
        }
    }
}
