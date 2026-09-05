using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StockMonitorTso.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialNpgsql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Wilayah = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TanggalDaftar = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Keterangan = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ActiveRoleName = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<string>(type: "text", nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ActorRole = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Before = table.Column<string>(type: "text", nullable: true),
                    After = table.Column<string>(type: "text", nullable: true),
                    Detail = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MitraTso",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Nama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JenisKendaraan = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KapasitasMax = table.Column<decimal>(type: "numeric", nullable: false),
                    SatuanKapasitas = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rute = table.Column<string[]>(type: "text[]", nullable: false),
                    AreaCoverage = table.Column<string[]>(type: "text[]", nullable: false),
                    Kontak = table.Column<string>(type: "text", nullable: false),
                    Pic = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Tarif = table.Column<decimal>(type: "numeric", nullable: false),
                    SatuanTarif = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitraTso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MitraId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MitraNamaSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TarifSnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    SatuanTarifSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EstimasiBiayaSnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    WilayahTujuan = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuteAsal = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuteTujuan = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    JarakKm = table.Column<decimal>(type: "numeric", nullable: true),
                    Produk = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Kuantitas = table.Column<decimal>(type: "numeric", nullable: false),
                    Satuan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TanggalKeberangkatan = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Eta = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    InvoiceGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvoiceNo = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Outlet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nama = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AgenId = table.Column<int>(type: "integer", nullable: false),
                    Wilayah = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TanggalDaftar = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Keterangan = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MitraTarifs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MitraId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Produk = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Tarif = table.Column<decimal>(type: "numeric", nullable: false),
                    SatuanTarif = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitraTarifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MitraTarifs_MitraTso_MitraId",
                        column: x => x.MitraId,
                        principalTable: "MitraTso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportOrderDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Produk = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Kuantitas = table.Column<decimal>(type: "numeric", nullable: false),
                    TarifSnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    SatuanTarifSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EstimasiBiayaSnapshot = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportOrderDetails_TransportOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "TransportOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokEntitas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Wilayah = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Produk = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Tier = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AgenId = table.Column<int>(type: "integer", nullable: true),
                    OutletId = table.Column<int>(type: "integer", nullable: true),
                    TanggalStokAwal = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Stok = table.Column<decimal>(type: "numeric", nullable: false),
                    DOT = table.Column<decimal>(type: "numeric", nullable: false),
                    StokHabisTerjual = table.Column<decimal>(type: "numeric", nullable: true),
                    StokIntransit = table.Column<decimal>(type: "numeric", nullable: true),
                    Keterangan = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokEntitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokEntitas_Agen_AgenId",
                        column: x => x.AgenId,
                        principalTable: "Agen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokEntitas_Outlet_OutletId",
                        column: x => x.OutletId,
                        principalTable: "Outlet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RencanaKedatangan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StokEntitasId = table.Column<int>(type: "integer", nullable: false),
                    Urutan = table.Column<int>(type: "integer", nullable: false),
                    NextSupply = table.Column<decimal>(type: "numeric", nullable: false),
                    ETA = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StokEntitasId = table.Column<int>(type: "integer", nullable: false),
                    StokEntitasTujuanId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Kuantitas = table.Column<decimal>(type: "numeric", nullable: false),
                    Tanggal = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Catatan = table.Column<string>(type: "text", nullable: true),
                    StokSumberSebelum = table.Column<decimal>(type: "numeric", nullable: false),
                    StokSumberSesudah = table.Column<decimal>(type: "numeric", nullable: false),
                    StokTujuanSebelum = table.Column<decimal>(type: "numeric", nullable: true),
                    StokTujuanSesudah = table.Column<decimal>(type: "numeric", nullable: true)
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
                name: "IX_Agen_Wilayah_Nama",
                table: "Agen",
                columns: new[] { "Wilayah", "Nama" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MitraTarifs_MitraId_Produk",
                table: "MitraTarifs",
                columns: new[] { "MitraId", "Produk" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outlet_AgenId_Nama",
                table: "Outlet",
                columns: new[] { "AgenId", "Nama" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RencanaKedatangan_StokEntitasId_Urutan",
                table: "RencanaKedatangan",
                columns: new[] { "StokEntitasId", "Urutan" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StokEntitasId",
                table: "StockTransactions",
                column: "StokEntitasId");

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_AgenId_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "AgenId", "Produk", "Tier" },
                unique: true,
                filter: "\"AgenId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_OutletId_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "OutletId", "Produk", "Tier" },
                unique: true,
                filter: "\"OutletId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StokEntitas_Wilayah_Produk_Tier",
                table: "StokEntitas",
                columns: new[] { "Wilayah", "Produk", "Tier" },
                unique: true,
                filter: "\"AgenId\" IS NULL AND \"OutletId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransportOrderDetails_OrderId",
                table: "TransportOrderDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportOrders_MitraId_WilayahTujuan_Produk_Kuantitas_Tang~",
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
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "MitraTarifs");

            migrationBuilder.DropTable(
                name: "RencanaKedatangan");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropTable(
                name: "TransportOrderDetails");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "MitraTso");

            migrationBuilder.DropTable(
                name: "StokEntitas");

            migrationBuilder.DropTable(
                name: "TransportOrders");

            migrationBuilder.DropTable(
                name: "Outlet");

            migrationBuilder.DropTable(
                name: "Agen");
        }
    }
}
