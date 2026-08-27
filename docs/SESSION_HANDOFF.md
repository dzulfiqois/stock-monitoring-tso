# Session Handoff — Aplikasi Stock Monitor dan TSO

> Dibuat: 2026-08 · Untuk melanjutkan di session baru. Bacaan wajib sebelum lanjut:
> `PLAN.md`, `AGENTS.md`, `docs/UI_REFERENCE.md`, `docs/REVISION_NOTE_stock_reduction.md`.

## 1. Masalah / fitur yang sedang dikerjakan

**Status: Phase 0–3 selesai & hijau. Phase 3 + redesign UI Stitch + inventarisasi Tier Agen + transfer Gudang→Agen + pengurangan stok (Opsi C) selesai & terverifikasi (lanjutan 2026-08).**

Lingkup yang baru dituntaskan:
- **Phase 3 (CRUD Sales Area + Konservasi Stok)** + **redesign UI mengikuti design Stitch** (`stitch_dashboard_monitoring_stok_migas/`).
- **Inventarisasi Tier Agen & Outlet**: entitas `Agen` (2–3 per Gudang) & `Outlet` (2 per Agen, tanpa limit) bernama, baris stok per (Agen×Produk)/(Outlet×Produk), migrasi `Tier.Agen`→`GudangWilayah` + `Outlet` + filtered index, mock 50% (gudang→agen→outlet, audit Transfer), identitas Create/Update = Superadmin+Supervisi, halaman Daftar Agen/Outlet + Detail Agen/Outlet + Update Data Harian perukuran (Opsi C).
- **Transfer Gudang Wilayah → Agen & Agen → Outlet**: modal "Kirim ke Agen" (di Detail Gudang) & "Kirim ke Outlet" (di Detail Agen) — Superadmin+Supervisi, pilih 1 tujuan + qty per SKU (3 SKU LPG sekaligus / 1 minyak), loop `Transfer` atomic per SKU via `AgenService.TransferFromWarehouseAsync` & `OutletService.TransferFromAgenAsync`. **Fix bug**: guard overdraft `EnsureSufficientStock(entity, kuantitas)` → `entity.Stok - kuantitas`.
- **Rev. UI Gudang Wilayah (2026-08)**: card Gas LPG dikelompokkan → **1 card per wilayah memuat 3 ukuran tabung sekaligus** (chip titik warna 5.5/12/50). Detail LPG jadi **gabungan 3 ukuran** (route `/sales-area/{Wilayah}/Lpg`, tabel per Ukuran×Tier = 6 baris via `GetLpgDetailAsync`); `SalesAreaDetailRow.StokEntitasId` baru → update/transfer resolve langsung, `ResolveEntityIdAsync` dihapus.
- **Pengurangan stok (Opsi C) (2026-08)**: `StockTransactionType.Issue` eksplisit (Qty>0 → `Stok-=Qty` + auto `StokHabisTerjual+=Qty`), `Adjust` opname ± (`Qty≠0`), `Receive`/`Transfer` tetap; modal **Update Data Harian** perukuran (LPG 3 baris + Minyak 1 baris: `Terjual`/`[☑]Opname`/`Intransit`/`Keterangan`).
- Perbaikan 4 temuan testing user: (1) Dashboard masih tabel → redesign card+chart; (2) filter objek tidak berfungsi → fix render mode interaktif + mock minyak tanah; (3) tombol Detail tidak berfungsi → fix routing enum + KPI per objek; (4) test integration gagal → fix fixture xUnit.

**Semua gerbang hijau terakhir:** `dotnet build -warnaserror` 0 error · `dotnet test` 81/81 (32 unit + 49 integration) · `dotnet format --verify` bersih · smoke (login superadmin + DB baru seed agen) lulus.

## 2. File yang diubah/dibuat

**Dokumen (source of truth):**
- `PLAN.md` — roadmap 6 fase produk Stock Monitor + TSO (bukan lagi uptime-monitor).
- `AGENTS.md` — persona Izi + repo guide (stack, traps, verification gate, referensi UI_REFERENCE).
- `STOCK_MONITORING_SPEC.md`, `TRANSPORT_SHIPPING_ORDER_SPEC.md` — spec ter-update (model perhitungan §2.c, guardrails, auth/sesi §6, CRUD, TSO).
- `docs/UI_REFERENCE.md` — katalog 9 layar Stitch + design token (Material-3) + pemetaan page/fase. **Style-only** (data/scope desain TIDAK dipakai).
- `docs/REVISION_NOTE_stock_reduction.md` — **resolved Opsi C `Issue` eksplisit** (auto `Terjual`).

**Kode (src/StockMonitorTso.*):**
- Deploy PoC: `Dockerfile` (multi-stage sdk→aspnet, non-root `USER 1654`, self-signed cert 8081, seed xlsx+mitra di-copy ke image) · `docker-compose.yml` (literal `80:8080` http + `443:8081` https, volume `stockmonitor_data:/app/data`, env_file `.env`) · `.dockerignore` · `.env.example`.
- `Web/Program.cs` — Identity, idle 15m, claims factory, DI services, auto-migrate+seed, `/health`, `MapRazorComponents`.
- `Web/wwwroot/app.css` — design system `sm-*` (sidebar, card, KPI, pill, chip, table, modal, segmented, chart, progress).
- `Web/Components/Layout/{MainLayout,NavMenu,ActiveRoleSwitcher}.razor` — shell sidebar 288px + topbar + nav `sm-nav-item` + role switcher.
- `Web/Components/Pages/Home.razor` — **Ringkasan Operasional** (Sektor KPI Gas/Minyak + chart bar per-wilayah + metrik table). Deklaratif, tanpa `$$"""`.
- `Web/Components/Pages/GudangWilayah.razor` — KPI overview + filter objek (`DashboardFilter`) + kartu sales area + modal hapus.
- `Web/Components/Pages/DetailSalesArea.razor` — breadcrumb + **KPI per objek** (Gas: Total/DOT/CD/Status; Minyak: Total/Terjual/Intransit/Status) + tabel per-tier + Log Transaksi + modal Update Data Harian perukuran (Isi Ulang / Terjual+ [☑]Opname± + Intransit + Keterangan, Opsi C).
- `Web/Components/Pages/RegisterSalesArea.razor` — form card branching MT/LPG.
- `Web/Components/Pages/Admin/UserManagement.razor` — Superadmin-only (assign role, set password).
- `Web/Components/Pages/DaftarAgen.razor` (`/wilayah/{wilayah}/agen`) — daftar agen + modal Tambah/Edit (Superadmin+Supervisi) + Hapus (Superadmin).
- `Web/Components/Pages/DetailAgen.razor` (`/agen/{id}`) — KPI per agen + tabel per produk + Log Transaksi + modal Update Data Harian perukuran (Opsi C).
- Domain: `Entities/{Agen,Outlet,Wilayah,Produk,Tier,StokEntitas,RencanaKedatangan,StockTransaction,StockTransactionRecord,AuditLog}.cs`, `Services/StockCalculator.cs`, `Abstractions/IAuditLogService.cs`.
- Infrastructure: `Persistence/ApplicationDbContext.cs`, `Persistence/Migrations/*` (6 migrasi — `AddOutletEntity`), `Seed/{SeedData,ExcelStockSeeder,AgenMockSeeder,OutletMockSeeder}.cs` (seed LPG dari Excel + mock minyak 7 wilayah + mock agen 18 + outlet 36), `Excel/XlsxReader.cs`, `Services/{StockWriteService,StockDashboardService,AgenService,OutletService,AuditLogService,UserAdminService,ActiveRoleClaimsPrincipalFactory}.cs`.
- Tests: `UnitTests/{StockCalculatorTests,AgenMockSeederTests}.cs`, `IntegrationTests/{StockWriteTests,StockDashboardTests,AgenServiceTests,AgenDashboardTests,AuthAndRbacTests,AdminPageAccessTests,HealthCheckTests,TestWebApplicationFactory}.cs`.

## 3. Solusi / kesepakatan teknis

- **Stack**: .NET 8, C# 12, nullable on, `TreatWarningsAsErrors`, Blazor Server (`@rendermode InteractiveServer` di halaman fitur — **harus ada** agar event handler jalan), Minimal API `/api/*`, EF Core SQLite (MVP), ASP.NET Core Identity, QuestPDF (TSO nanti), xUnit+FluentAssertions+NSubstitute+WebApplicationFactory.
- **Model domain**: `GudangWilayah/Outlet = (Wilayah×Produk×Tier)` + `Agen = (Agen×Produk)` + `Outlet = (Outlet×Produk)`; CD = Stok÷DOT; Status Kritis<3/Warning<7/Aman≥7; `CD_n = (sisa@ETA + NextSupply)÷DOT` (**konseptual, JANGAN tiru rumus Excel**); konservasi via atomic (Receive/Issue/Adjust±/Transfer), `Issue` auto `StokHabisTerjual`, tolak overdraft, audit.
- **RBAC**: multi-role switchable active role (claim role aktif saja), idle 15m, Superadmin-only assign role & password, Update=Superadmin+Supervisi, Delete=Superadmin, Register=Superadmin+Operator. **Identitas Agen**: Create/Update=Superadmin+Supervisi, Delete=Superadmin (amend STOCK §3).
- **Granularitas stok**: Gudang Wilayah/Outlet = `(Wilayah × Produk × Tier)`; **Agen = `(Agen × Produk)`** lewat `StokEntitas.AgenId`. Index unik difilter: gudang/outlet unik per (Wilayah,Produk,Tier) saat `AgenId IS NULL`; baris agen unik per (AgenId,Produk,Tier) saat `AgenId IS NOT NULL`.
- **Mock agen (2026-08)**: 2–3 agen per gudang; stok awal agen = 50% stok gudang dibagi rata, DOT gudang dibagi rata; gudang di-debit 50% via transaksi `Transfer` (audit, konservasi terjaga).
- **Routing Detail**: gunakan **enum name** di URL (`Lpg5_5Kg`, `MinyakTanah`) dan parse via `Enum.TryParse` (bukan `DisplayName`) — ini fix tombol Detail.
- **Test fixture**: `TestWebApplicationFactory` **parameterless** (xUnit `IClassFixture`); subclass `TestWebApplicationFactoryNoStock` untuk test yang meregister stok sendiri (seed stok di-skip via `Seed:SkipStock=true`).
- **Desain = style saja**: token/pola dari Stitch diadopsi; data/scope desain (gudang nasional Plumpang/TBBM, wilayah Jawa/Sumatera/Kalimantan, angka contoh) TIDAK dipakai. Brand app = "Stock Monitor dan TSO".

## 4. Langkah berikutnya (belum selesai)

0. **Inventarisasi Tier Outlet bernama** (task ini hanya agen; Outlet masih agregat per wilayah).
1. **Phase 4 — Modul TSO** (PLAN.md §4): Mitra TSO dari `seeds/mitra-tso.json`, form + commit di Submit, dampak stok saat keberangkatan, Preview read-only + Generate Draft Invoice (QuestPDF, idempoten), Update/Delete order, guardrails T1–T11 + F1–F12. (TSO spec sudah siap; belum ada kode.)
2. **Phase 5 — Hardening**: Serilog structured, ProblemDetails semua error, `/ready`, `/metrics` (opsional), Dockerfile non-root, `dotnet format` CI, switch PostgreSQL (opsional).
3. **(Resolved) mekanisme pengurangan stok** (`docs/REVISION_NOTE_stock_reduction.md`): **Opsi C `Issue` eksplisit** — `Issue` terjual (−, auto Terjual), `Adjust` opname ± (`[☑]`), `Receive`/`Transfer` tetap; modal perukuran terverifikasi.
4. **Open question (fitur baru dari desain, tercatat di `docs/UI_REFERENCE.md` §6)**: TSO wizard 4 langkah, menu Gudang Wilayah (done sebagian), Monitoring Agen, Ekspor Laporan, Live Sync, Proyeksi Dampak Stok — butuh persetujuan.
5. **Keputusan scope terdahulu yang belum dijawab**: rename solution (`StockMonitorTso.*`) vs tetap; pakai Excel acuan baru vs cabut — untuk kasus "perombakan total domain" (belum aktif).

## 5. Reminder phase per plan (`PLAN.md §4`)

- **Phase 0** Skeleton — ✅ selesai
- **Phase 1** Auth/RBAC/Sesi — ✅ selesai
- **Phase 2** Monitoring Stok (read+compute) — ✅ selesai
- **Phase 3** CRUD Sales Area + Konservasi (+ redesign UI Stitch + **inventarisasi Tier Agen**) — ✅ selesai & terverifikasi
- **Phase 4** Modul TSO — ⏭ **berikutnya**
- **Phase 5** Hardening — ⏭ belum

## 6. Cara verifikasi (gerbang per fase)

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build StockMonitorTso.sln -warnaserror
dotnet test StockMonitorTso.sln
dotnet format StockMonitorTso.sln --verify-no-changes
dotnet run --project src/StockMonitorTso.Web --launch-profile http   # http://localhost:5110
```

Akun seed: `superadmin@stockmonitor.local` / `Superadmin!2345` (Operator/Supervisi/Tamu/multi ada di `SeedData`).
