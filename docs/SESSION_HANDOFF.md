# Session Handoff — Aplikasi Stock Monitor dan TSO

> Dibuat: 2026-08 · Untuk melanjutkan di session baru. Bacaan wajib sebelum lanjut:
> `PLAN.md`, `AGENTS.md`, `docs/UI_REFERENCE.md`, `docs/REVISION_NOTE_stock_reduction.md`.

## 1. Masalah / fitur yang sedang dikerjakan

**Status: Phase 0–4 selesai & hijau. Phase 3 (CRUD + inventarisasi Agen/Outlet + pengurangan stok Opsi C) & Phase 4 (TSO wizard + snapshot harga + Docker PoC + rev. login) selesai & terverifikasi.**

Lingkup yang baru dituntaskan:
- **Phase 3 (CRUD Sales Area + Konservasi Stok)** + **redesign UI mengikuti design Stitch** (`stitch_dashboard_monitoring_stok_migas/`).
- **Inventarisasi Tier Agen & Outlet**: entitas `Agen` (2–3 per Gudang) & `Outlet` (2 per Agen, tanpa limit) bernama, baris stok per (Agen×Produk)/(Outlet×Produk), migrasi `Tier.Agen`→`GudangWilayah` + `Outlet` + filtered index 3 kasus, mock 50% (gudang→agen→outlet, audit Transfer), identitas Create/Update = Superadmin+Supervisi, halaman Daftar Agen/Outlet + Detail Agen/Outlet + Update Data Harian perukuran (Opsi C).
- **Transfer Gudang Wilayah → Agen & Agen → Outlet**: modal "Kirim ke Agen" (di Detail Gudang) & "Kirim ke Outlet" (di Detail Agen) — Superadmin+Supervisi, pilih 1 tujuan + qty per SKU (3 SKU LPG sekaligus / 1 minyak), loop `Transfer` atomic per SKU via `AgenService.TransferFromWarehouseAsync` & `OutletService.TransferFromAgenAsync`.
- **Rev. UI Gudang Wilayah (2026-08)**: card Gas LPG dikelompokkan → **1 card per wilayah memuat 3 ukuran tabung sekaligus** (chip titik warna 5.5/12/50). Detail LPG jadi **gabungan 3 ukuran** (route `/sales-area/{Wilayah}/Lpg`, tabel per Ukuran×Tier via `GetLpgDetailAsync`); `SalesAreaDetailRow.StokEntitasId` baru → update/transfer resolve langsung.
- **Pengurangan stok (Opsi C) (2026-08)**: `StockTransactionType.Issue` eksplisit (Qty>0 → `Stok-=Qty` + auto `StokHabisTerjual+=Qty`), `Adjust` opname ± (`Qty≠0`), `Receive`/`Transfer` tetap; modal **Update Data Harian** perukuran (LPG 3 baris + Minyak 1 baris: `Terjual`/`[☑]Opname`/`Intransit`/`Keterangan`).
- **Phase 4 — Modul TSO (2026-08)**: `MitraTso` seed dari `seeds/mitra-tso.json` (upsert, 3 mitra), `TransportOrder` (`OrderNo TSO-YYYYMMDD-XXXX`, snapshot `Tarif/SatuanTarif/EstimasiBiaya=Tarif×Qty`, `WilayahTujuan`, `RuteAsal/Tujuan`, `Produk`, `Kuantitas`, `Satuan`, `TglBerangkat`, `Eta+7`, `Status Committed/StockImpacted/FlagTertunda`, `RowVersion` concurrency, `IsDeleted`), wizard 4 langkah (Tujuan & Obyek → Rute & Jadwal → Transporter + Estimasi Biaya → Ringkasan), commit di Submit (T11) + idempotensi dedup 1 menit (T1/F9), dampak stok T5 (`RencanaKedatangan` NextSupply+ETA di Gudang Wilayah tujuan, F7 `FlagTertunda` + `/resync`), Preview read-only 8 kolom (§4.d), **Generate Draft Invoice idempoten** (QuestPDF, `CreationDate=CreatedAt`, bytes equal), Update (Supervisi) / Delete (Superadmin) + audit, `/api/tso` (MapGroup).
- **Deploy PoC**: `Dockerfile` multi-stage sdk→aspnet non-root `USER 1654`, self-signed cert 8081, `docker-compose.yml` literal `80:8080` + `443:8081`, volume `stockmonitor_data:/app/data` + `stockmonitor_keys:/app/keys`, seed xlsx+mitra di-copy ke image; fix `DataProtection` persist keys & `SeedData` `IsNullOrWhiteSpace`.
- **Rev. Login (2026-08)**: halaman login tanpa sidebar — `LoginLayout.razor` baru (tanpa `MainLayout`), `Login.razor` tanpa `ExternalLoginPicker` ("Use another service..."), `app.css` `sm-auth-*`.
- Perbaikan 4 temuan testing user: (1) Dashboard masih tabel → redesign card+chart; (2) filter objek tidak berfungsi → fix render mode interaktif + mock minyak tanah; (3) tombol Detail tidak berfungsi → fix routing enum + KPI per objek; (4) test integration gagal → fix fixture xUnit.

**Semua gerbang hijau terakhir:** `dotnet build -warnaserror` 0 error · `dotnet test` 91/91 (32 unit + 59 integration) · `dotnet format --verify` bersih · `docker compose build` OK · `curl -k https://localhost/health` 200 · smoke DB seed (Agen 18, Outlet 36, Mitra 3, Stok 272) lulus.

## 2. File yang diubah/dibuat

**Dokumen (source of truth):**
- `PLAN.md` — roadmap 6 fase produk Stock Monitor + TSO.
- `AGENTS.md` — persona Izi + repo guide (stack, traps, verification gate, referensi UI_REFERENCE).
- `STOCK_MONITORING_SPEC.md`, `TRANSPORT_SHIPPING_ORDER_SPEC.md` — spec ter-update (model §3.c, §5.f/g, guardrails, auth/sesi).
- `docs/UI_REFERENCE.md` — katalog 9 layar Stitch + design token (Material-3) + pemetaan page/fase. **Style-only** (data/scope desain TIDAK dipakai).
- `docs/REVISION_NOTE_stock_reduction.md` — **resolved Opsi C `Issue` eksplisit** (auto `Terjual`).
- `docs/DEVELOPER_GUIDE.md`, `docs/CHECKLIST_DOD.md`, `docs/CHANGE_PROCESS_NOTE.md`.

**Kode (src/StockMonitorTso.*):**
- Deploy: `Dockerfile` (multi-stage sdk→aspnet, non-root `USER 1654`, self-signed cert 8081, seed xlsx+mitra di-copy) · `docker-compose.yml` (literal `80:8080` + `443:8081`, volumes `stockmonitor_data:/app/data` + `stockmonitor_keys:/app/keys`) · `.dockerignore` · `.env.example`.
- `Web/Program.cs` — Identity, idle 15m, `AddDataProtection().PersistKeysToFileSystem("/app/keys")`, claims factory, DI services, auto-migrate+seed, `/health`, `MapRazorComponents`, `MapTsoEndpoints`.
- `Web/wwwroot/app.css` — design system `sm-*` + `sm-auth-*` (login).
- `Web/Components/Layout/{MainLayout,NavMenu,ActiveRoleSwitcher}.razor` — shell sidebar 288px + topbar + nav `sm-nav-item` + role switcher + link TSO.
- `Web/Components/Pages/Home.razor` — **Ringkasan Operasional** (Sektor KPI Gas/Minyak + chart bar per-wilayah + metrik table).
- `Web/Components/Pages/GudangWilayah.razor` — KPI overview + filter objek + kartu sales area (LPG 1 card/wilayah, 3 chip warna) + modal hapus.
- `Web/Components/Pages/DetailSalesArea.razor` — breadcrumb + KPI + tabel per-tier + Log Transaksi + modal Update Data Harian perukuran + modal Kirim ke Agen.
- `Web/Components/Pages/RegisterSalesArea.razor` — form card branching MT/LPG.
- `Web/Components/Pages/DaftarAgen.razor` (`/wilayah/{wilayah}/agen`) + `DaftarOutlet.razor` (`/agen/{id}/outlet`) — daftar + modal Tambah/Edit + Hapus.
- `Web/Components/Pages/DetailAgen.razor` (`/agen/{id}`) + `DetailOutlet.razor` (`/outlet/{id}`) — KPI + tabel per produk + Log + modal Update perukuran + modal Kirim ke Outlet.
- `Web/Components/Pages/Tso/{TsoList,TsoWizard,TsoPreview}.razor` (`/tso`, `/tso/create`, `/tso/{id}/edit`, `/tso/{id}`) — wizard 4 langkah + Preview 8 kolom + Generate Draft Invoice.
- `Web/Components/Pages/Admin/UserManagement.razor` + `Account/Shared/LoginLayout.razor` + `Account/Pages/Login.razor` (tanpa sidebar/external login).
- Domain: `Entities/{Agen,Outlet,MitraTso,TransportOrder,Wilayah,Produk,Tier,StokEntitas,RencanaKedatangan,StockTransaction,StockTransactionRecord,AuditLog}.cs`, `Services/StockCalculator.cs`, `Abstractions/IAuditLogService.cs`.
- Infrastructure: `Persistence/ApplicationDbContext.cs`, `Persistence/Migrations/*` (8 migrasi — `AddTsoModule`, `FixTsoRowVersion`), `Seed/{SeedData,ExcelStockSeeder,AgenMockSeeder,OutletMockSeeder,MitraTsoSeeder}.cs`, `Excel/XlsxReader.cs`, `Services/{StockWriteService,StockDashboardService,AgenService,OutletService,TransportOrderService,InvoiceGenerator,AuditLogService,UserAdminService,ActiveRoleClaimsPrincipalFactory}.cs`.
- Api: `Api/Endpoints/TsoEndpoints.cs` (`MapGroup /api/tso`).
- Tests: `UnitTests/{StockCalculatorTests,AgenMockSeederTests}.cs`, `IntegrationTests/{StockWriteTests,StockDashboardTests,AgenServiceTests,AgenDashboardTests,WarehouseTransferTests,TsoServiceTests,AuthAndRbacTests,AdminPageAccessTests,HealthCheckTests,TestWebApplicationFactory}.cs`.

## 3. Solusi / kesepakatan teknis

- **Stack**: .NET 8, C# 12, nullable on, `TreatWarningsAsErrors`, Blazor Server (`@rendermode InteractiveServer` di halaman fitur — **harus ada** agar event handler jalan), Minimal API `/api/*`, EF Core SQLite (MVP), ASP.NET Core Identity, QuestPDF (deterministik idempotent via `CreationDate=CreatedAt`), xUnit+FluentAssertions+NSubstitute+WebApplicationFactory.
- **Model domain**: `GudangWilayah/Outlet = (Wilayah×Produk×Tier)` + `Agen = (Agen×Produk)` + `Outlet = (Outlet×Produk)`; CD = Stok÷DOT; Status Kritis<3/Warning<7/Aman≥7; `CD_n = (sisa@ETA + NextSupply)÷DOT` (**konseptual, JANGAN tiru rumus Excel**); konservasi via atomic (Receive/Issue/Adjust±/Transfer), `Issue` auto `StokHabisTerjual`, tolak overdraft, audit.
- **TSO**: wizard 4 langkah (Tujuan&Obyek → Rute&Jadwal → Transporter+EstimasiBiaya `tarif×qty` → Ringkasan), commit di Submit (T11), idempotensi dedup 1 menit (T1/F9) + PDF bytes equal (T9), snapshot `Tarif/SatuanTarif/EstimasiBiaya` agar order lama tak berubah saat `Mitra.tarif` berubah, `RowVersion` concurrency (F8→409), `Status FlagTertunda` + `/resync` (F7), `RencanaKedatangan` (T5) Urutan 1..3.
- **RBAC**: multi-role switchable active role (claim role aktif saja), idle 15m, Superadmin-only assign role & password, Update=Superadmin+Supervisi, Delete=Superadmin, Register=Superadmin+Operator. **Identitas Agen/Outlet**: Create/Update=Superadmin+Supervisi, Delete=Superadmin (amend STOCK §3). **TSO**: Create=Superadmin+Operator, Update=Superadmin+Supervisi, Delete=Superadmin.
- **Granularitas stok**: Gudang Wilayah = `(Wilayah × Produk × Tier)` (`AgenId/OutletId IS NULL`); **Agen = `(Agen × Produk)`** via `AgenId`; **Outlet = `(Outlet × Produk)`** via `OutletId`. Index unik difilter 3 kasus. Mitra `AreaCoverage` → validasi `WilayahTujuan` (T4).
- **Mock agen/outlet (2026-08)**: 2–3 agen per gudang, 2 outlet per agen; stok awal agen = 50% gudang ÷ N, outlet = 50% agen ÷ 2; DOT dibagi rata; gudang→agen→outlet di-debit via `Transfer` audit.
- **Mock TSO**: 3 Mitra dari `seeds/mitra-tso.json` (upsert), tarif mutable diaudit.
- **Routing Detail**: gunakan **enum name** di URL (`Lpg5_5Kg`, `MinyakTanah`, `Lpg`) dan parse via `Enum.TryParse` (bukan `DisplayName`) — ini fix tombol Detail.
- **Test fixture**: `TestWebApplicationFactory` **parameterless** (xUnit `IClassFixture`); subclass `TestWebApplicationFactoryNoStock` untuk test yang meregister stok sendiri (seed stok di-skip via `Seed:SkipStock=true`).
- **Desain = style saja**: token/pola dari Stitch diadopsi; data/scope desain (gudang nasional Plumpang/TBBM, wilayah Jawa/Sumatera/Kalimantan, angka contoh) TIDAK dipakai. Brand app = "Stock Monitor dan TSO".
- **Deploy**: Docker multi-stage non-root, literal 80/443, self-signed cert 8081, volume data+keys, `DataProtection` persist `/app/keys`, seed password fix `IsNullOrWhiteSpace`.
- **Git**: branch `main` = lengkap (docs/spec/xlsx); branch `apps` = hanya aplikasi (src/tests/seeds/mitra-tso.json/Docker) — push `origin/apps` untuk deploy.

## 4. Langkah berikutnya (belum selesai)

1. **Phase 5 — Hardening**: sync balik ke repo `apps` (fix `Program.cs` AddDataProtection & `SeedData` `IsNullOrWhiteSpace` yang masih manual di VM), Serilog structured, ProblemDetails menyeluruh, `/ready`, `/metrics` (opsional), `dotnet format` CI, image <200MB, switch PostgreSQL (opsional).
2. **Open question (fitur baru dari desain, tercatat di `docs/UI_REFERENCE.md` §6)**: Proyeksi Dampak Stok chart, Ekspor Laporan, Live Sync — butuh persetujuan (TSO wizard, Gudang Wilayah, Monitoring Agen/Outlet transfer sudah done).
3. **Keputusan scope terdahulu yang belum dijawab**: rename solution (`StockMonitorTso.*`) vs tetap; pakai Excel acuan baru vs cabut — untuk kasus "perombakan total domain" (belum aktif).
4. **Mitra CRUD di app (Phase 5+) vs seed-only (Phase 4)**: harga dinamis via snapshot sudah handle; UI CRUD Mitra ditunda Phase 5.

## 5. Reminder phase per plan (`PLAN.md §4`)

- **Phase 0** Skeleton — ✅ selesai
- **Phase 1** Auth/RBAC/Sesi — ✅ selesai
- **Phase 2** Monitoring Stok (read+compute) — ✅ selesai
- **Phase 3** CRUD Sales Area + Konservasi (+ inventarisasi Agen/Outlet + pengurangan stok Opsi C + rev. card LPG) — ✅ selesai & terverifikasi
- **Phase 4** Modul TSO — ✅ selesai & terverifikasi (wizard, snapshot, PDF idempoten, /api/tso, Docker PoC, rev. login)
- **Phase 5** Hardening — ⏭ **berikutnya**

## 6. Cara verifikasi (gerbang per fase)

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build StockMonitorTso.sln -warnaserror
dotnet test StockMonitorTso.sln
dotnet format StockMonitorTso.sln --verify-no-changes
dotnet run --project src/StockMonitorTso.Web --launch-profile http   # http://localhost:5110
docker compose build && docker compose up -d && curl -k https://localhost/health # PoC 80:8080 443:8081
```

Akun seed: `superadmin@stockmonitor.local` / `Superadmin!2345` (Operator/Supervisi/Tamu/multi ada di `SeedData`).

---

## 7. Slice 2026-09 — Buat user baru oleh Superadmin

Lingkup yang dituntaskan (di luar fase 5):

- **Service**: `IUserAdminService.CreateUserAsync(actor, email, password, roles[], activeRole)` — `RequireSuperadmin`; validasi email unik, roles terdaftar di `RoleManager`, `activeRole` anggota `roles`; `EmailConfirmed = true`; `AddToRolesAsync`; audit log `CreateUser` (`EntityType="ApplicationUser"`, `After=roles joined`).
- **UI**: `/admin/users` direstylye ke design system `sm-*` (`sm-table`, `sm-pill`, `sm-segmented`, `sm-btn`, `sm-modal`); tombol "Tambah User" → modal dengan field email/password/konfirmasi + checklist role + dropdown "Role Aktif" (terbatas ke role tercentang); refresh daftar + alert status (`sm-alert-success/error`).
- **Hapus scaffold berbahaya**: `Web/Components/Account/Pages/Register.razor` + `RegisterConfirmation.razor` dihapus — sebelumnya orphan, Superadmin-gated, dan `SignInManager.SignInAsync(user)` menimpa cookie Superadmin saat submit (bug latent). ExternalLogin tidak disentuh (sudah unreachable dari Login).
- **Tests**: `tests/StockMonitorTso.IntegrationTests/UserAdminCreateTests.cs` — 7 test: RBAC (Superadmin OK; Operator/Supervisi/Tamu ditolak & tak tercipta), duplikat email, password lemah (< 8), role tak dikenal, activeRole ∉ roles, audit tercatat (`After=roles joined`, `ActorRole=Superadmin`), sign-in-able dengan password awal.
- **Fix 2026-09 — crash circuit `/admin/users`**: `StatusMessage.razor` `HttpContext` dibuat nullable + guard `if (HttpContext is null) return;` di `OnInitialized` (sebelumnya NRE saat dirender interactive — `HttpContext` null di circuit). `<StatusMessage>` di `UserManagement.razor` diganti alert inline `sm-alert-success/error` (CSS baru di `app.css`). Root cause: halaman interactive satu-satunya yang menyentuh komponen Account static-SSR; halaman Identity static lain tidak terdampak.
- **Fix 2026-09 — modal di belakang blur**: modal "Tambah User" kini dibungkus wrapper `position:fixed;inset:0;z-index:110;display:flex;align-items:center;justify-content:center;overflow-y:auto` (pola yang sama dengan halaman lain, mis. `DaftarAgen.razor:108`) — sebelumnya `.sm-modal` tanpa wrapper berada di alur dokumen normal di bawah backdrop blur (fixed, z-index 100) sehingga tidak bisa diklik. Backdrop tetap `@onclick` close.
- **Rev. UI sidebar (2026-09)**: `.sm-sidebar-brand` jadi kolom (`flex-direction:column; align-items:flex-start`) — logo Pertamina di atas teks "Stock Monitor & TSO", rata kiri, teks tidak lagi wrap dua baris.
- **Bug tertinggal**: `ActiveRoleSwitcher` di layout `NavMenu` masih non-fungsional (layout static; render mode per-halaman tidak propagate ke parent layout — `UserManagement.razor` InteractiveServer sendiri OK). Slice terpisah.

Gerbang: `dotnet build -warnaserror` 0 error · 7 test baru hijau (61 integration lama) · 5 test prasejarah `StockDashboardTests`/`AgenDashboardTests` masih gagal (xlsx `Monitoring Tabung RPM(1).xlsx` di-gitignore di `apps`; sama tanpa slice ini) · `dotnet format --verify` bersih · smoke `/health` 200 · `/admin/users` redirect login untuk anonim.
