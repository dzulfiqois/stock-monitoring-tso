using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMonitorTso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTsoModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MitraTso",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Nama = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JenisKendaraan = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    KapasitasMax = table.Column<decimal>(type: "TEXT", nullable: false),
                    SatuanKapasitas = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Rute = table.Column<string>(type: "TEXT", nullable: false),
                    AreaCoverage = table.Column<string>(type: "TEXT", nullable: false),
                    Kontak = table.Column<string>(type: "TEXT", nullable: false),
                    Pic = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tarif = table.Column<decimal>(type: "TEXT", nullable: false),
                    SatuanTarif = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitraTso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNo = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    MitraId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    MitraNamaSnapshot = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TarifSnapshot = table.Column<decimal>(type: "TEXT", nullable: false),
                    SatuanTarifSnapshot = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EstimasiBiayaSnapshot = table.Column<decimal>(type: "TEXT", nullable: false),
                    WilayahTujuan = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RuteAsal = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RuteTujuan = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Produk = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Kuantitas = table.Column<decimal>(type: "TEXT", nullable: false),
                    Satuan = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TanggalKeberangkatan = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Eta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    InvoiceGeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InvoiceNo = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransportOrders_MitraId_WilayahTujuan_Produk_Kuantitas_TanggalKeberangkatan",
                table: "TransportOrders",
                columns: new[] { "MitraId", "WilayahTujuan", "Produk", "Kuantitas", "TanggalKeberangkatan" });

            migrationBuilder.CreateIndex(
                name: "IX_TransportOrders_OrderNo",
                table: "TransportOrders",
                column: "OrderNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MitraTso");

            migrationBuilder.DropTable(
                name: "TransportOrders");
        }
    }
}
