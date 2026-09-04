using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMonitorTso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMitraPerProdukTarifAndOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "JarakKm",
                table: "TransportOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MitraTarif",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MitraId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Produk = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Tarif = table.Column<decimal>(type: "TEXT", nullable: false),
                    SatuanTarif = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitraTarif", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MitraTarif_MitraTso_MitraId",
                        column: x => x.MitraId,
                        principalTable: "MitraTso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportOrderDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Produk = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Kuantitas = table.Column<decimal>(type: "TEXT", nullable: false),
                    TarifSnapshot = table.Column<decimal>(type: "TEXT", nullable: false),
                    SatuanTarifSnapshot = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EstimasiBiayaSnapshot = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportOrderDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportOrderDetail_TransportOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "TransportOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MitraTarif_MitraId_Produk",
                table: "MitraTarif",
                columns: new[] { "MitraId", "Produk" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportOrderDetail_OrderId",
                table: "TransportOrderDetail",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MitraTarif");

            migrationBuilder.DropTable(
                name: "TransportOrderDetail");

            migrationBuilder.DropColumn(
                name: "JarakKm",
                table: "TransportOrders");
        }
    }
}
