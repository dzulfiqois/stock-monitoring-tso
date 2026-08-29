using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockMonitorTso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMitraTarifTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MitraTarif_MitraTso_MitraId",
                table: "MitraTarif");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportOrderDetail_TransportOrders_OrderId",
                table: "TransportOrderDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransportOrderDetail",
                table: "TransportOrderDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MitraTarif",
                table: "MitraTarif");

            migrationBuilder.RenameTable(
                name: "TransportOrderDetail",
                newName: "TransportOrderDetails");

            migrationBuilder.RenameTable(
                name: "MitraTarif",
                newName: "MitraTarifs");

            migrationBuilder.RenameIndex(
                name: "IX_TransportOrderDetail_OrderId",
                table: "TransportOrderDetails",
                newName: "IX_TransportOrderDetails_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_MitraTarif_MitraId_Produk",
                table: "MitraTarifs",
                newName: "IX_MitraTarifs_MitraId_Produk");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransportOrderDetails",
                table: "TransportOrderDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MitraTarifs",
                table: "MitraTarifs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MitraTarifs_MitraTso_MitraId",
                table: "MitraTarifs",
                column: "MitraId",
                principalTable: "MitraTso",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportOrderDetails_TransportOrders_OrderId",
                table: "TransportOrderDetails",
                column: "OrderId",
                principalTable: "TransportOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MitraTarifs_MitraTso_MitraId",
                table: "MitraTarifs");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportOrderDetails_TransportOrders_OrderId",
                table: "TransportOrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransportOrderDetails",
                table: "TransportOrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MitraTarifs",
                table: "MitraTarifs");

            migrationBuilder.RenameTable(
                name: "TransportOrderDetails",
                newName: "TransportOrderDetail");

            migrationBuilder.RenameTable(
                name: "MitraTarifs",
                newName: "MitraTarif");

            migrationBuilder.RenameIndex(
                name: "IX_TransportOrderDetails_OrderId",
                table: "TransportOrderDetail",
                newName: "IX_TransportOrderDetail_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_MitraTarifs_MitraId_Produk",
                table: "MitraTarif",
                newName: "IX_MitraTarif_MitraId_Produk");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransportOrderDetail",
                table: "TransportOrderDetail",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MitraTarif",
                table: "MitraTarif",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MitraTarif_MitraTso_MitraId",
                table: "MitraTarif",
                column: "MitraId",
                principalTable: "MitraTso",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportOrderDetail_TransportOrders_OrderId",
                table: "TransportOrderDetail",
                column: "OrderId",
                principalTable: "TransportOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
