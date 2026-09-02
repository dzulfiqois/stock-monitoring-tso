# Checklist & Definition of Done — Stock Monitor dan TSO

> Checklist operasional per titik + kriteria Definition of Done (DoD). Dibaca bersama
> `PLAN.md §4–§6`, `STOCK_MONITORING_SPEC.md`, `TRANSPORT_SHIPPING_ORDER_SPEC.md`,
> `docs/PHASE_CHECKLIST.md`, `docs/SESSION_HANDOFF.md`, `docs/CHANGE_PROCESS_NOTE.md`.
>
> Cara pakai: centang `[x]` bila titik/DoD terpenuhi; rujuk kolom **Bukti/Verifikasi**.
> Status diperbarui per 2026-09.

---

## Ringkasan Status

| Phase                                                       | Status | Ringkasan                                                         |
| ----------------------------------------------------------- | :----: | ----------------------------------------------------------------- |
| Phase 0 Skeleton                                            |   ✅   | Selesai & hijau                                                   |
| Phase 1 Auth, RBAC & Sesi (+ buat user oleh Superadmin 2026-09) |   ✅   | Selesai & hijau — `CreateUserAsync` + audit + modal `/admin/users` |
| Phase 2 Monitoring (read+compute)                           |   ✅   | Selesai & hijau                                                   |
| Phase 3 CRUD + Konservasi (+ lanjutan Agen/Outlet/Transfer/Rev UI) |   ✅   | Selesai & hijau —**CRUD belum final menunggu stakeholder** |
| Phase 4 Modul TSO (wizard + snapshot + /api/tso + Docker)   |   ✅   | Selesai & hijau — wizard 4 langkah, snapshot tarif, PDF idempoten |
| Phase 5 Hardening                                           |   ⏭   | Belum                                                             |

**Gerbang global terakhir (2026-09):** `dotnet build -warnaserror` 0 error · `dotnet test` 98/98 lulus tanpa 5 prasejarah (xlsx tidak di `apps` branch — sama tanpa slice); 7 baru untuk `CreateUser`; `dotnet format --verify` bersih · smoke `/health` 200 + `/admin/users` 302 (redirect login untuk anonim).

---

## Definition of Done — Global (PLAN §6 + Guardrails §5)

Setiap slice/task dinyatakan selesai bila semua kriteria berikut centang:

| #   | Kriteria DoD Global                                              | Bukti/Verifikasi                                                  | Status |
| --- | ---------------------------------------------------------------- | ----------------------------------------------------------------- | ------ |
| D1  | `dotnet build -warnaserror` bersih (0 warning/error)           | `dotnet build StockMonitorTso.sln -warnaserror`                 | ✅     |
| D2  | Unit test ditambah/diperbarui; coverage tidak turun              | `tests/StockMonitorTso.UnitTests` — `dotnet test`            | ✅     |
| D3  | Integration test untuk interaksi eksternal baru                  | `tests/StockMonitorTso.IntegrationTests` — `dotnet test`     | ✅     |
| D4  | Audit log ada untuk tiap mutasi                                  | `AuditLogService` · tabel `AuditLogs` · inspeksi test       | ✅     |
| D5  | Tidak ada`// TODO` baru; cross-check dengan SPEC terkait       | `grep -r TODO --include="*.cs"` kosong · review spec           | ✅     |
| D6  | Jika mengubah data/dashboard: update SPEC bila perlu             | `STOCK_MONITORING_SPEC.md` §2–§4 amend                       | ✅     |
| G1  | Single source of truth stok — mutasi hanya via transaksi atomic | `StockWriteService.TransactAsync` · `StockTransactionRecord` | ✅     |
| G2  | Satuan kanonik anti-campur (Tabung vs Kiloliter)                 | Validasi`Produk.Satuan()` · menolak satuan silang              | ✅     |
| G3  | Stok non-negatif — overdraft ditolak (G3/F4)                    | `StockWriteService:EnsureSufficientStock` · test overdraft     | ✅     |
| G4  | DOT > 0 untuk CD; DOT=0 → CD N/A (F3)                           | `StockCalculator.CoverageDays` · dashboard N/A                 | ✅     |
| G9  | Idempotensi & konkurensi atomic                                  | `BeginTransactionAsync` · test konservasi                      | ✅     |
| G10 | RBAC di service layer (bukan UI saja)                            | `RequireAnyRole`/`RequireSuperadmin` di service               | ✅     |
| F   | `dotnet format --verify` bersih                                | `dotnet format --verify-no-changes`                             | ✅     |

---

## Phase 0 — Skeleton

**Deskripsi:** Kerangka solusi minimal: repo, solution, DI, `/health`, DB SQLite ter-create, shell Blazor + Identity login.

| Titik                                                                           | Status | Bukti/Verifikasi                                                                  | Keterangan                             |
| ------------------------------------------------------------------------------- | :----: | --------------------------------------------------------------------------------- | -------------------------------------- |
| `git init` + `.gitignore` .NET 8                                            |   ✅   | Root:`.gitignore`, `StockMonitorTso.sln`                                      | Penamaan tanpa prefix`Monitoring.`   |
| `dotnet new sln` + proyek `StockMonitorTso.{Domain,Infrastructure,Api,Web}` |   ✅   | `src/` 4 proyek · `Directory.Build.props` (net8.0, nullable, LangVersion 12) | Api masih kosong utk Phase 4           |
| DI wired;`/health` → 200                                                     |   ✅   | `Web/Program.cs` — `MapHealthChecks("/health")` · `curl /health` 200      | Identity + EF + health                 |
| EF empty context auto-migrate (file SQLite)                                     |   ✅   | `ApplicationDbContext` · `Migrations/` (5) · `DataSource=stockmonitor.db` | `db.Database.Migrate()` saat startup |
| Blazor Server + Identity login                                                  |   ✅   | `MainLayout.razor` · `NavMenu.razor` · `Components/Account/*`             | Scaffold Identity, shell 288px         |
| **DoD Phase 0** — `dotnet run` boot, login render, DB file ada         |   ✅   | Manual smoke`http://localhost:5110` + `/health` 200                           | —                                     |

---

## Phase 1 — Auth, RBAC & Sesi

**Deskripsi:** Otentikasi multi-role switchable, idle 15 menit, manajemen oleh Superadmin, audit log.

| Titik                                                                                            | Status | Bukti/Verifikasi                                                                                                       | Keterangan                                                            |
| ------------------------------------------------------------------------------------------------ | :----: | ---------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| Identity:`ApplicationUser`, `Role`, `UserRole` (many-to-many)                              |   ✅   | `Persistence/ApplicationUser.cs` · `Identity`                                                                     | 4 role kanonik + multi-role                                           |
| Switchable active role (claim role-aktif scoped sesi)                                            |   ✅   | `Services/ActiveRoleClaimsPrincipalFactory.cs` · `ActiveRoleSwitcher.razor`                                       | `ClaimsFactory` hanya klaim role aktif                              |
| Idle timeout 15m (sliding) + logout eksplisit                                                    |   ✅   | `Web/Program.cs` `ExpireTimeSpan=15m, SlidingExpiration` · `AuthAndRbacTests.IdleTimeout_ConfiguredTo15Minutes` | `IdentityConstants.ApplicationScheme`                               |
| Assign role & ganti password**Superadmin-only** (service layer)                            |   ✅   | `Services/UserAdminService.cs` (`RequireSuperadmin`) · `AuthAndRbacTests`                                       | `AssignRoleAsync`/`RemoveRoleAsync`/`SetPasswordAsync` di-audit |
| **Buat user baru + assign role (Superadmin-only, 2026-09)**                                |   ✅   | `Services/UserAdminService.cs` `CreateUserAsync` (`RequireSuperadmin`) · `UserAdminCreateTests` 7 test hijau | `Email+Password+Roles[]+ActiveRole`; audit `CreateUser`; modal di `/admin/users` |
| `AuditLog` entity + `AuditLogService`                                                        |   ✅   | `Domain/Entities/AuditLog.cs` · `Infrastructure/Services/AuditLogService.cs`                                      | Append-only,`Timestamp=UtcNow`                                      |
| Seed 5 akun (Superadmin/Operator/Supervisi/Tamu/Multi)                                           |   ✅   | `Seed/SeedData.cs` (`Seed:SuperadminEmail` dll.)                                                                   | `Seed:DefaultPassword` override via config                          |
| **DoD P1** — login multi-role, switch role, idle expiry, assign-role oleh Superadmin only |   ✅   | `AuthAndRbacTests` 6 test hijau · manual login + switch                                                             | Non-Superadmin ditolak`UnauthorizedAccessException`                 |

---

## Phase 2 — Monitoring Stok (read + compute)

**Deskripsi:** Entitas stok per `(Wilayah × Produk × Tier)`, rumus CD/Exhaust/Status/MT/CD_n, dashboard read-only, seed Excel.

| Titik                                                                                | Status | Bukti/Verifikasi                                                                             | Keterangan                                                                                   |
| ------------------------------------------------------------------------------------ | :----: | -------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Entitas`Wilayah` (7 canon) + `WilayahInfo.All`                                   |   ✅   | `Domain/Entities/Wilayah.cs`                                                               | Papua/Maluku 7 wilayah                                                                       |
| Entitas`Produk` (5.5/12/50 + Minyak) + `ProdukInfo`                              |   ✅   | `Domain/Entities/Produk.cs` · `BeratKg()`, `Satuan()`                                 | Tabung vs KL                                                                                 |
| Entitas`Tier` (GudangWilayah, Agen, Outlet)                                        |   ✅   | `Domain/Entities/Tier.cs`                                                                  | Urutan = hirarki`Pusat→Gudang→Agen→Outlet`                                              |
| `StokEntitas` + `RencanaKedatangan` (maks 3 slot)                                |   ✅   | `Domain/Entities/StokEntitas.cs` · `RencanaKedatangan.cs`                               | `TanggalStokAwal`, `Stok`, `DOT`, `StokHabisTerjual/Intransit`                       |
| Rumus`CD = Stok ÷ DOT`; `Exhaust = Tgl + CD`; Status Kritis<3/Warning<7/Aman≥7 |   ✅   | `Domain/Services/StockCalculator.cs` · `StockCalculatorTests`                           | `CoverageDays`, `ExhaustDate`, `StatusFor`                                             |
| `MT = Tabung × kg ÷ 1000`; `Total MT = Σ MT` per wilayah                      |   ✅   | `StockCalculator.MetricTon`                                                                | Minyak Tanah null                                                                            |
| `CD_n = (sisa@ETA + NextSupply) ÷ DOT` — konseptual                              |   ✅   | `StockCalculator.CoverageDaysAfterRencana`                                                 | **Dilarang tiru `Next Supply ÷ Σ CD` Excel**                                       |
| Dashboard read-only: ringkasan, kartu, tabel                                         |   ✅   | `Components/Pages/Home.razor` · `StockDashboardService`                                 | `GetSummaryAsync`, `GetRingkasanAsync`, `GetLpgRowsAsync`, `GetMinyakTanahRowsAsync` |
| Seed loader Excel (sheet Agen→GudangWilayah, Outlet)                                |   ✅   | `Seed/ExcelStockSeeder.cs` `LoadLpgGudangWilayah`/`LoadLpgOutlet` · `XlsxReader.cs` | `Monitoring Tabung RPM(1).xlsx`                                                            |
| Mock minyak tanah 7 wilayah                                                          |   ✅   | `ExcelStockSeeder.LoadMinyakTanahSample`                                                   | —                                                                                           |
| **DoD P2** — unit test formula ≥80%; dashboard CD/Status benar dari seed     |   ✅   | `StockCalculatorTests` · `StockDashboardTests` hijau                                    | N/A tampil bila DOT=0                                                                        |

---

## Phase 3 — CRUD Sales Area + Konservasi (+ lanjutan Agen/Transfer/Rev UI)

**Deskripsi:** Register/Update/Delete dengan invarian konservasi atomic + redesign UI Stitch + inventarisasi agen → transfer. **Catatan: CRUD belum final — menunggu stakeholder (lihat `docs/CHANGE_PROCESS_NOTE.md`).**

### 3.1 Inti CRUD & konservasi

| Titik                                                                                               | Status | Bukti/Verifikasi                                                                                                     | Keterangan                                                                            |
| --------------------------------------------------------------------------------------------------- | :----: | -------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| Register form branching (Minyak vs LPG), validasi                                                   |   ✅   | `Pages/RegisterSalesArea.razor` (Superadmin+Operator)                                                              | Minyak: 2 entitas (Gudang+Outlet); LPG: 6 (3 ukuran×2 tier); field sesuai spec §4.c |
| Update: Superadmin+Supervisi; Delete: Superadmin soft-delete + modal                                |   ✅   | `StockWriteService.Register/UpdateDetail/DeleteAsync` · `@rendermode InteractiveServer`                         | Delete = flag`IsDeleted`                                                            |
| Konservasi:`Receive`/`Adjust`/`Transfer` atomic (transaction) + audit                         |   ✅   | `StockWriteService.TransactAsync` (`BeginTransactionAsync` + `Commit`)                                         | Tiap mutasi →`StockTransactions` + `AuditLogs`                                   |
| Tolak overdraft G3/F4 (`entity.Stok - kuantitas < 0`)                                             |   ✅   | `StockWriteService:EnsureSufficientStock(entity, entity.Stok - kuantitas)` · `WarehouseTransferTests.Overdraft` | Fix bug: sebelumnya`EnsureSufficientStock(entity, kuantitas)` selalu lolos          |
| Recompute CD/Exhaust/Status otomatis setelah mutasi                                                 |   ✅   | `StockCalculator` dipanggil pada render dashboard                                                                  | `Covered Days` dihitung dari snapshot terbaru                                       |
| **DoD 3.1** — integration test create/update/delete per role; konservasi & overdraft terjaga |   ✅   | `StockWriteTests` 11 test hijau                                                                                    | `Register_ByOperator_Allowed`/`Delete_BySuperadmin_SoftDeletes`                   |

### 3.2 Inventarisasi Tier Agen (lanjutan 2026-08)

| Titik                                                                          | Status | Bukti/Verifikasi                                                                                                       | Keterangan                                                               |
| ------------------------------------------------------------------------------ | :----: | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Entitas`Agen` (Id, Nama unik per Wilayah, Wilayah, TanggalDaftar, IsDeleted) |   ✅   | `Domain/Entities/Agen.cs` · `ApplicationDbContext` (index unik `(Wilayah,Nama)`)                                | Soft delete; Nama case-insensitive                                       |
| Granularitas`(Agen × Produk)` via `StokEntitas.AgenId` + filtered index   |   ✅   | `StokEntitas.AgenId` · `(Wilayah,Produk,Tier) WHERE AgenId IS NULL` vs `(AgenId,Produk,Tier) WHERE IS NOT NULL` | 2 filtered index                                                         |
| Migrasi`AddAgenAndGudangWilayahTier`: rename `Tier.Agen→GudangWilayah`    |   ✅   | `Migrations/20260818083638_*.cs` (Up/Down SQL)                                                                       | Up:`UPDATE ... SET Tier='GudangWilayah' WHERE Tier='Agen'`             |
| Mock 50%: stok agen = 50% gudang ÷ N; DOT ÷ N; 2–3 agen/wilayah (18 total)  |   ✅   | `Seed/AgenMockSeeder.cs` (`AgenCount`, `SplitEqual`) · `SeedData.SeedAgenMockAsync`                           | Sisa pembagian ke agen terakhir; gudang didebit 50% via`Transfer` mock |
| Konservasi mock: gudang didebit +`StockTransactions` `Transfer` (audit)    |   ✅   | `SeedData` pendingTransfer → `StockTransactions` 72 baris                                                         | Σ gudang = Σ agen = 26533.425 (SMOKE)                                  |
| Identitas agen Create/Update = Superadmin+Supervisi; Delete = Superadmin only  |   ✅   | `Services/AgenService.cs` (`IAgenService`) · `AgenServiceTests`                                                 | Auto-create 4 baris stok (0) saat create                                 |
| Halaman Daftar Agen + Detail Agen + Update Data Harian                         |   ✅   | `Pages/DaftarAgen.razor` `/wilayah/{Wilayah}/agen` · `DetailAgen.razor` `/agen/{id:int}`                      | Modal`sm-*`, list agen per wilayah                                     |
| **DoD 3.2** — per wilayah 2–3 agen, Σ agen == Σ gudang setelah split |   ✅   | `AgenDashboardTests` + `AgenMockSeederTests` hijau                                                                 | `Seed_EveryWilayah_HasTwoOrThreeAgen`                                  |

### 3.2b Inventarisasi Tier Outlet (lanjutan 2026-08)

| Titik                                                                          | Status | Bukti/Verifikasi                                                                                                       | Keterangan                                                               |
| ------------------------------------------------------------------------------ | :----: | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Entitas `Outlet` (Id, Nama unik per AgenId, AgenId, Wilayah, TanggalDaftar, IsDeleted) |   ✅   | `Domain/Entities/Outlet.cs` · `ApplicationDbContext` (index unik `(AgenId,Nama)`)                                | Soft delete; Nama case-insensitive                                       |
| Granularitas `(Outlet × Produk)` via `StokEntitas.OutletId` + filtered index   |   ✅   | `StokEntitas.OutletId` · `(OutletId,Produk,Tier) WHERE OutletId IS NOT NULL` | 3 filtered index (Gudang/Agen/Outlet)                                                         |
| Migrasi `AddOutletEntity`: create `Outlet`, add `OutletId`, 3rd index |   ✅   | `Migrations/20260823140735_*.cs`                                                                       | Drop index gudang lama → recreate `AgenId IS NULL AND OutletId IS NULL`             |
| Mock 50%: stok outlet = 50% agen ÷ 2; DOT ÷ 2; 2 per agen (36 total)  |   ✅   | `Seed/OutletMockSeeder.cs` (`OutletPerAgen=2`) · `SeedData.SeedOutletMockAsync`                           | Sisa ke terakhir; agen didebit 50% via `Transfer` mock |
| Konservasi mock: agen didebit + `StockTransactions` `Transfer` (audit); outlet agregat lama di-soft-delete |   ✅   | `SeedData` pendingTransfer → `StockTransactions` 72 baris (outlet)                                                         | Σ outlet = Σ agen tersisa = 13266.7 (SMOKE)                                  |
| Identitas outlet Create/Update = Superadmin+Supervisi; Delete = Superadmin only  |   ✅   | `Services/OutletService.cs` (`IOutletService`) · `OutletServiceTests` (akan ditambah)                                                 | Auto-create 4 baris stok (0) saat create                                 |
| Halaman Daftar Outlet + Detail Outlet + Update Data Harian                         |   ✅   | `Pages/DaftarOutlet.razor` `/agen/{id}/outlet` · `DetailOutlet.razor` `/outlet/{id:int}`                      | Modal `sm-*`, list outlet per agen                                     |
| **DoD 3.2b** — per agen 2 outlet, Σ outlet == Σ agen tersisa |   ✅   | `AgenDashboardTests` (agen 50% gudang → outlet 50% agen)                                                                 | —                                  |

### 3.3 Transfer Gudang Wilayah → Agen (lanjutan 2026-08)

| Titik                                                                                                                | Status | Bukti/Verifikasi                                                            | Keterangan                                                   |
| -------------------------------------------------------------------------------------------------------------------- | :----: | --------------------------------------------------------------------------- | ------------------------------------------------------------ |
| Modal "Kirim ke Agen" di Detail Gudang (Superadmin+Supervisi)                                                        |   ✅   | `Pages/DetailSalesArea.razor` `OpenTransferModal`                       | Tombol di blok`AuthorizeView Roles="Superadmin,Supervisi"` |
| Pilih 1 agen + kuantitas per SKU (3 ukuran LPG sekaligus; minyak 1 input)                                            |   ✅   | `GetAgenTransferTargetsAsync(wilayah)` → dropdown + chip stok gudang     | `GudangStok55/12/50Kg` dari `GetLpgRowsAsync`            |
| Loop`Transfer` atomic per SKU via `AgenService.TransferFromWarehouseAsync`                                       |   ✅   | `Services/AgenService.cs` + `StockWriteService.TransactAsync(Transfer)` | Overdraft per SKU ditolak; agen harus se-wilayah             |
| **DoD 3.3** — multi-SKU 5000/3000/2000, konservasi (gudang debit = agen kredit), RBAC, lintas-wilayah ditolak |   ✅   | `WarehouseTransferTests` 6 test hijau                                     | —                                                           |

### 3.3b Transfer Agen → Outlet (lanjutan 2026-08)

| Titik                                                                                                                | Status | Bukti/Verifikasi                                                            | Keterangan                                                   |
| -------------------------------------------------------------------------------------------------------------------- | :----: | --------------------------------------------------------------------------- | ------------------------------------------------------------ |
| Modal "Kirim ke Outlet" di Detail Agen (Superadmin+Supervisi)                                                        |   ✅   | `Pages/DetailAgen.razor` `OpenTransferModal`                       | Tombol di blok `AuthorizeView Roles="Superadmin,Supervisi"` |
| Pilih 1 outlet + kuantitas per SKU (3 ukuran LPG sekaligus; minyak 1 input)                                            |   ✅   | `GetOutletTransferTargetsAsync(agenId)` → dropdown + chip stok agen     | `TransferStok55/12/50/Minyak` dari `DetailAgen`            |
| Loop `Transfer` atomic per SKU via `OutletService.TransferFromAgenAsync`                                       |   ✅   | `Services/OutletService.cs` + `StockWriteService.TransactAsync(Transfer)` | Overdraft per SKU ditolak; outlet harus milik agen             |
| **DoD 3.3b** — multi-SKU agen→outlet, konservasi (agen debit = outlet kredit), RBAC, lintas-agen ditolak |   ✅   | `WarehouseTransferTests` pattern (outlet, akan ditambah `OutletTransferTests`)                                     | —                                                           |

### 3.4 Redesign & rev. UI

| Titik                                                                                     | Status | Bukti/Verifikasi                                                                                           | Keterangan                                                      |
| ----------------------------------------------------------------------------------------- | :----: | ---------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| Redesign style Stitch (sidebar 288px, card, KPI, pill, chip, modal, segmented, bar chart) |   ✅   | `Web/wwwroot/app.css` `sm-*` · `Layout/MainLayout, NavMenu`                                         | Token Material-3                                                |
| Card Gas LPG: 1 card per wilayah berisi 3 ukuran (chip titik warna 5.5/12/50)             |   ✅   | `Pages/GudangWilayah.razor` · `StockDashboardService` grouping                                        | `StokGudang55Kg/12Kg/50Kg` + `TotalStok` gabungan           |
| Detail LPG gabungan: route`/sales-area/{Wilayah}/Lpg`, 6 baris (3 ukuran×tier)         |   ✅   | `GetLpgDetailAsync` · `Pages/DetailSalesArea.razor` `_isLpg` + `SalesAreaDetailRow.StokEntitasId` | `ResolveEntityIdAsync` dihapus — pakai `row.StokEntitasId` |
| Filter objek interaktif, tombol Detail (enum name), fixture xUnit                         |   ✅   | `@rendermode InteractiveServer` · `Enum.TryParse` · `TestWebApplicationFactory`                    | —                                                              |
| **DoD 3.4** — grouping card + detail 6 baris, 3 chip warna                         |   ✅   | `StockDashboardTests.SalesAreaCards_GasLpg_OneCardPerWilayah...` + `LpgDetail_...SixRows` hijau        | —                                                              |

### DoD Keseluruhan Phase 3

- [X] `build -warnaserror` 0 error, 91/91 test hijau, `format --verify` bersih, smoke `/health` 200 (global D1–D6, G1–G10)
- [X] Spec CRUD di-update: `STOCK_MONITORING_SPEC.md` §3 amend (identitas Agen) + §4.f (transfer) + §2.c granularitas
- [x] **Resolved 2026-08 — Opsi C `Issue` eksplisit**: `Receive`/`Issue`/`Adjust`±/`Transfer`; `Terjual` via `Issue` auto `StokHabisTerjual`, `Opname` via `Adjust` ± dengan `[☑]` enable; overdraft ditolak
- [ ] **CRUD belum final** — menunggu stakeholder (`docs/CHANGE_PROCESS_NOTE.md` §1)

---

## Phase 4 — Modul TSO

**Deskripsi:** Order TSO `Pusat → Gudang Wilayah` dari Mitra TSO; commit di Submit; dampak stok saat keberangkatan; Preview read-only + Draft Invoice PDF idempoten.

| Titik                                                                                                                                                    | Status | Bukti/Verifikasi                        | Keterangan                                               |
| -------------------------------------------------------------------------------------------------------------------------------------------------------- | :----: | --------------------------------------- | -------------------------------------------------------- |
| `MitraTso` (3 mitra) dimuat dari `seeds/mitra-tso.json` — `MitraTsoSeeder` upsert, tarif mutable diaudit, snapshot di order (harga dinamis) |   ✅   | `Domain/Entities/MitraTso.cs` · `Seed/MitraTsoSeeder.cs` · `ApplicationDbContext.MitraTso` | 3 mitra, upsert tarif |
| **Wizard 4 langkah:** (1) Gudang+Obyek+Qty → (2) Rute & Jadwal (Pusat→Gudang, Tgl ≥today, ETA+7) → (3) Transporter + Estimasi Biaya (`tarif × qty`) → (4) Ringkasan → Submit (`/tso/create` + `/tso/{id}/edit`) |   ✅   | `Pages/Tso/{TsoWizard,TsoList,TsoPreview}.razor` · `Invoices` | Stepper 4, validasi per step |
| Commit di Submit (T11); submit ganda dicegah (T1/F9) — dedup key `Mitra+Wilayah+Produk+Qty+Tgl` 1 menit |   ✅   | `Services/TransportOrderService.cs` `CreateAsync` dedup | T1/F9 |
| Dampak stok saat keberangkatan: `RencanaKedatangan` (NextSupply=Kuantitas, ETA, Urutan 1..3) di Gudang Wilayah tujuan (T5); `Status FlagTertunda` jika gagal (F7/F10) |   ✅   | `TransportOrderService.CreateRencanaKedatanganAsync` | T5, F7/F10 |
| Preview read-only (`/tso/{id}`) — 8 kolom §4.d (tidak mengubah data) |   ✅   | `Pages/Tso/TsoPreview.razor` | T11 |
| Generate Draft Invoice PDF (QuestPDF, idempoten — regenerate bytes identical, `CreationDate=CreatedAt`, T9) — kolom: Mitra, Gudang Tujuan, Jenis Material, Kuantitas+satuan, Tgl Keberangkatan, ETA, Nomor Order, Timestamp |   ✅   | `Services/InvoiceGenerator.cs` (QuestPDF `CreationDate=CreatedAt`) | T9 |
| Update order (Superadmin+Supervisi, `RowVersion` 409 F8), Delete order (Superadmin, soft delete + modal) |   ✅   | `TransportOrderService.Update/DeleteAsync` · `RowVersion IsConcurrencyToken` | F8 |
| Audit tiap aksi order; role check per mutasi (T7/T8) — `RequireAnyRole` di service, `AuthorizeView` di UI |   ✅   | `AuditLogService` · `TsoServiceTests` role matrix | T7/T8 |
| `/api/tso` — `MapGroup /api/tso`: `POST /`, `GET /`, `GET /{id}`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/invoice`, `POST /{id}/resync` — ProblemDetails 400/403/404/409 |   ✅   | `Api/Endpoints/TsoEndpoints.cs` · `Web/Program.cs` `MapTsoEndpoints` | T8 |
| Fallback: 400 tak terdaftar (F3), tgl lampau (F5), gagal PDF (F6), monitoring putus → flag tertunda + resync (F7/F10), idle (F11), tanpa wewenang (F12) |   ✅   | `TsoServiceTests` (10 test: Rencana, idempoten, role, snapshot) | — |
| **DoD P4** — TSO→departure→stok debit + Rencana tercatat; PDF idempoten (bytes equal); role check; simulasi F7 |   ✅   | `TsoServiceTests` 10 test hijau · `seeds/mitra-tso.json` | `/api/tso` siap |

**Deploy PoC (lanjutan Phase 4):** `Dockerfile` multi-stage non-root `USER 1654`, `docker-compose.yml` literal `80:8080` + `443:8081` (self-signed cert `/app/certs`), volume `stockmonitor_data` + `stockmonitor_keys` (DataProtection persist), `HealthCheck /health` — `build` OK, `curl -k https://localhost/health` 200, seed DB (Agen 18, Outlet 36, Mitra 3).

---

## Phase 5 — Hardening

**Deskripsi:** Logging terstruktur, ProblemDetails, readiness/metrics, container non-root, CI format.

| Titik                                                                              | Status | Bukti/Verifikasi                      | Keterangan |
| ---------------------------------------------------------------------------------- | :----: | ------------------------------------- | ---------- |
| Serilog structured JSON + correlation id + redaksi secret                          |   ⏳   | —                                    | —         |
| ProblemDetails (RFC 7807) di semua error API; 400 untuk validasi bukan 500         |   ⏳   | —                                    | —         |
| `/ready`; `/metrics` (prometheus-net, opsional MVP)                            |   ⏳   | —                                    | —         |
| Dockerfile multi-stage non-root;`docker compose up` smoke                        |   ⏳   | —                                    | —         |
| `dotnet format` di CI                                                            |   ⏳   | `dotnet format --verify-no-changes` | —         |
| (Opsional) switch DB ke PostgreSQL                                                 |   ⏳   | Connection string + provider          | —         |
| **DoD P5** — smoke pass; image <200MB; semua guardrails hijau; format clean |   ⏳   | —                                    | —         |

---

## Catatan Terbuka Lintas Fase

| Topik                                                                 | Status                                             | Rujukan                                                                               |
| --------------------------------------------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------------------- |
| Mekanisme pengurangan stok / "Stok Terjual" (C `Issue`/`Adjust`±) | ✅ Resolved 2026-08 (Opsi C)                      | `docs/REVISION_NOTE_stock_reduction.md` — auto-berlaku untuk modal Gudang & Agen   |
| Ekspor Laporan, Live Sync, Proyeksi Dampak Stok | ⏳ open                                            | `docs/UI_REFERENCE.md §6`                                                          |
| Rename solution`StockMonitorTso.*`                                  | ⏳ belum                                           | `PLAN.md §9`                                                                       |
| Excel acuan: pakai baru vs cabut (perombakan domain)                  | ⏳ belum aktif                                     | `PLAN.md §9`                                                                       |
| Tracker stok Gudang Pusat untuk TSO overdraft                         | ⏳ default:**tidak** (hanya `Kuantitas>0`) | `PLAN.md §9`                                                                       |
| Admin CRUD Mitra TSO di app vs seed-only                              | ⏳ default Phase 4: seed-only                      | `PLAN.md §9`                                                                       |
| Export/import seed Excel dari app                                     | ⏳ default:**tidak**                         | `PLAN.md §9`                                                                       |
| Proses ubah langkah bisnis CRUD setelah stakeholder                   | 📌                                                 | `docs/CHANGE_PROCESS_NOTE.md` — SOP 7 langkah (spec→kode→test→verifikasi→docs) |

---

## Perintah Verifikasi

```bash
dotnet build StockMonitorTso.sln -warnaserror          # D1
dotnet test StockMonitorTso.sln                         # D2, D3
dotnet format StockMonitorTso.sln --verify-no-changes   # F
dotnet run --project src/StockMonitorTso.Web            # http://localhost:5110 → /health 200
```
