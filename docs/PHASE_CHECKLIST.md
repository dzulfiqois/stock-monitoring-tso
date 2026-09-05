# Checklist Fase Pengembangan — Stock Monitor dan TSO

> Checklist ringkas per fase. Dibaca bersama `PLAN.md` (roadmap & guardrails) dan
> `AGENTS.md` (runbook & traps).
> Status per 2026-09: **Phase 0–4 hijau** (era Blazor) · **Phase 5 digabung ke R5** ·
> **Phase R (Rekonstruksi Arsitektur) berikutnya**.
> Verifikasi lama: `dotnet build -warnaserror` (0 error) · `dotnet test` **106/106 hijau** · `dotnet format --verify` bersih · smoke `/health` 200 + DB seed (Agen 18, Outlet 36, Stok 272).
> Verifikasi baru (post-rekonstruksi): ditambah `npm run build/lint/test` (frontend) · smoke compose 4 kontainer via `curl -f http://localhost/health`.
>
> Catatan: **5 test prasejarah sudah tidak ada** — workbook dikonversi ke `seeds/lpg-stok.json` (2026-09), xlsx machinery dihapus, seed LPG kini tracked JSON.

---

## Phase R — Rekonstruksi Arsitektur (R0–R5)

**Deskripsi:** Migrasi dari single-host Blazor Server + SQLite ke
`browser → nginx → { frontend TanStack Start SSR, backend .NET 8 JWT } → PostgreSQL`,
satu kontainer per service, monorepo.

**Keputusan terkunci (2026-09):**
- [x] Frontend: **TanStack Start (SSR, Node)** + TypeScript + Tailwind (token `sm-*` diport)
- [x] Auth: **JWT bearer** (Identity Core tetap user store); idle 15 menit via access-token + refresh aktivitas; switch-role = token terbit ulang
- [x] Migrasi: **backend-first bertahap R0–R5**; Blazor hidup selama transisi, dihapus di R5
- [x] Deploy: **satu kontainer per service** (nginx + frontend + api + postgres), docker compose, **monorepo**
- [x] Database: **PostgreSQL** (Npgsql), migrasi regenerasi penuh
- [x] Branch: `main` = main repository (dokumentasi lengkap + kode) · `apps` = production update point (app + deploy saja)

**Checklist fase:**

### R0 — Groundwork ✅ selesai 2026-09
- [x] Provider EF SQLite → Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.11); semua migrasi lama dihapus, **satu migrasi baru `InitialNpgsql`** (17 tabel, partial index quoting Postgres)
- [x] Concurrency: `RowVersion byte[]` **dipertahankan** sebagai token (`bytea`) — semantik sama dengan SQLite, nol perubahan service; `xmin` tidak diperlukan
- [x] DateTime: `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` di kedua host; kolom audit UTC (`CreatedAt`/`UpdatedAt`/`InvoiceGeneratedAt`) → `timestamp with time zone`; tanggal bisnis tetap `timestamp`
- [x] Endpoint auth JWT: `POST /api/auth/login|refresh|logout`, `GET /api/auth/me`, `POST /api/auth/switch-role` + `TokenService` (access 15 menit + refresh 7 hari, audience terpisah, `typ=refresh`)
- [x] Host API murni: `StockMonitorTso.Api` jadi `Sdk.Web` executable (Program.cs + guard `EF.IsDesignTime`, DI lengkap, seed startup); Web (Blazor) tetap hidup di atas Postgres selama transisi
- [x] Compose: `docker-compose.yml` baru (postgres + api + nginx — satu kontainer per service, volume `pgdata`, healthcheck); legacy dipindah ke `docker-compose.legacy.yml`; `deploy/nginx/nginx.conf` + `deploy/postgres/init/01-init.sql` + `src/StockMonitorTso.Api/Dockerfile` (non-root 1654)
- [x] `DataProtection:KeyPath` configurable (default `/app/keys` untuk kontainer; test override ke temp dir)
- [x] **R0.1 — `APP_BASE_URL` + forwarded headers**: `App:BaseUrl` (env `APP_BASE_URL`) pin scheme/host via `PublicBaseUrlMiddleware` (kedua host); `UseForwardedHeaders` paling awal (`X-Forwarded-For|Proto`, KnownNetworks/Proxies cleared); cookie `Secure=Always` bila BaseUrl https; endpoint diagnostik `/api/debug/request` (dev-only, 404 di Production)
- [x] Test integration: Postgres **compose** (`docker compose up -d postgres` wajib jalan) — database terpisah `sm_test_{guid}` per factory via `TestDatabase` helper; 8 test baru `AuthApiTests` + 3 test baru `PublicBaseUrlTests` (login/me/switch-role/refresh/401/scheme pinning/forwarded headers). *Catatan: Testcontainers digantikan compose-Postgres — container-start dari dalam testhost menggantung di environment ini (bukti: probe standalone 4.3s OK, testhost mang).*
- [x] Seed LPG: workbook → **`seeds/lpg-stok.json`** (42 baris, konversi via parser asli — byte-faithful); `LpgStokSeeder` + `Seed:LpgJsonPath`; xlsx machinery (`ExcelStockSeeder` parsing, `XlsxReader`) dihapus; **5 test prasejarah kembali hijau** → suite 106/106
- [x] Fix bonus: **merge-conflict markers tertinggal** di `LoginLegacy.razor` + `LoginLayout.razor` (sejak merge apps — solusi tidak pernah build); **race DbContext** di `ActiveRoleSwitcher` (query layout vs halaman pada satu connection — SQLite menyembunyikannya, Npgsql membongkarnya) → isolasi child-scope
- Gerbang: ✅ api boot di Postgres via compose; ✅ login → me → switch-role round-trip via curl **melalui nginx**; ✅ `dotnet test` 101 total — 96 hijau, 5 prasejarah xlsx (terdokumentasi); ✅ build 0/0; ✅ format verify bersih

### R1 — REST surface lengkap ✅ selesai 2026-09
- [x] `/api/dashboard` — summary, ringkasan, lpg-rows, minyak-rows, cards(+filter), sales-area/{wilayah}/{produk}, sales-area-lpg/{wilayah}, agen-inventaris, agen/{id}, agen-transfer-targets, outlet-inventaris, outlet/{id}, outlet-transfer-targets (semua read: semua role)
- [x] `/api/stock` — POST register (SA+Op), PUT {id} detail (SA+Sup), POST {id}/transact Receive/Issue/Adjust±/Transfer (SA+Sup, overdraft 400), DELETE {id} (SA)
- [x] `/api/agen` — GET by wilayah + {id}, POST/PUT/DELETE, POST {id}/transfer-from-warehouse (konservasi via API terverifikasi)
- [x] `/api/outlet` — GET by agen + {id}, POST/PUT/DELETE, POST transfer-from-agen (mismatch agen → 400)
- [x] `/api/users` — **Superadmin-only di endpoint** (`RequireRole("Superadmin")`, karena service list tidak enforce): list(+roles), create, assign/remove role, set password (old password mati terverifikasi)
- [x] `ProblemMapper` — exception service → ProblemDetails (409/403/404/400, sisanya 500); enum sebagai string global (`JsonStringEnumConverter`) untuk kontrak React
- [x] Fix: Api host menambah `.AddDefaultTokenProviders()` (set-password butuh reset token)
- [x] Test baru (20): DashboardApi ×6, StockApi ×5, AgenApi ×5, OutletApi ×4, UsersApi ×5 — pola bearer via `TestHttp`
- Gerbang: ✅ build 0/0 · ✅ `dotnet test` **134/134 hijau** (32 unit + 102 integration) · ✅ format bersih · ✅ smoke live via nginx (summary 200, tamu 403 di stock/users)

### R2 — React shell ✅ selesai 2026-09
- [x] `frontend/` scaffold **TanStack Start** (nitro 3 + vite 8 + tsr CLI, React 19, Tailwind v4) — discaffold via `npm create @tanstack/start`, lalu disesuaikan
- [x] Token `sm-*` Material-3 di-port ke Tailwind v4 `@theme` (kelas komponen `sm-card/sm-btn/sm-input/sm-pill/sm-alert`); catatan: Tailwind v4 **tidak bisa `@apply` kelas komponen sendiri** — utility di-expand
- [x] Halaman `/login` (email+password → pilih role aktif bila multi-role via `/api/auth/switch-role`) dan `/` dashboard Ringkasan Operasional (KPI Sektor Gas/Minyak + bar chart Agen vs Outlet + role switcher + logout)
- [x] Auth interceptor `apiFetch`: Bearer header; **401 → refresh sekali → ulangi** → gagal: clear session + redirect `/login`; 403 → ApiError (notice); token di localStorage (keputusan refresh-token storage tetap open question di PLAN)
- [x] TanStack Query untuk data dashboard; `beforeLoad` auth-gate di route `/`
- [x] Gates frontend: `npm run build` ✅ · `npm run lint` (eslint flat) ✅ · `npm test` (vitest, 5 unit pure-function) ✅
- [x] Deploy: `frontend/Dockerfile` (node 22 build → runtime `.output/server/index.mjs`, non-root) · compose service `frontend` (:3000) · nginx `location /` → frontend (WS upgrade headers untuk SSR), `/api`+`/health` → api
- Gerbang: ✅ perjalanan auth end-to-end lewat nginx di compose (SSR `/login` render, login → Bearer → `/api/dashboard/summary` 200 via nginx) · ✅ backend regression 134/134 tetap hijau

### R3 — Halaman Monitoring ✅ selesai 2026-09
- [x] **Shell** `_app.tsx` (pathless layout): sidebar 288px (logo Pertamina + nav dengan Material Symbols + role switcher reload-style + logout) + topbar (user pill) + `Outlet`; `beforeLoad` + `useIsClient` gate (direct URL load anonim → `/login`)
- [x] Dashboard `/` sesuai Blazor `Dashboard.razor`: Live Sync pill, CTA Buka Gudang Wilayah, KPI Sektor (Gas/Minyak + outlet defisit + status pill), 2 bar chart `sm-chart` (Agen=current merah-bila-kritis / Outlet=target) + legend, tabel Metrik Minyak Tanah (status = worst dari Agen/Outlet)
- [x] `/gudang-wilayah` — filter obyek, 3 KPI (Total Stok / Produk Kritis / Exhaust Terdekat), kartu sales area (Minyak: 3 kolom Gudang/Agen/Outlet + Terjual/Intransit; LPG: 3 chip ukuran + total Tabung), tombol Agen(n), Detail, Hapus (SA, modal konfirmasi → loop soft-delete entityIds)
- [x] `/sales-area/$wilayah/$produk` — breadcrumb, 4 KPI (LPG: Total/DOT/CD/Status; Minyak: Total/Terjual/Intransit/Status), tabel per Tier (CD N/A), Log Transaksi, **modal Update Data Harian** (segmented Isi Ulang [Receive] / Harian [Issue+auto Terjual, Adjust opname ±, UpdateDetail intransit/keterangan] per-ukuran LPG / tunggal Minyak), **modal Kirim ke Agen** (stok gudang live per SKU + target dropdown + qty>0)
- [x] `/sales-area/register` — branching Minyak (2 baris: Gudang+Outlet) / LPG (6 baris: 3 SKU × 2 tier), invalidasi cards+summary
- [x] `/wilayah/$wilayah/agen` — tabel agen + Tambah/Edit modal + Hapus modal (RBAC: CU=SA+Sup, D=SA)
- [x] `/agen/$agenId` — KPI + tabel per produk + log + Update Harian (per-produk entries) + Kirim ke Outlet (stok agen live per 4 SKU) + link Daftar Outlet
- [x] `/agen/$agenId/outlet` — tabel outlet + Tambah/Edit + Hapus
- [x] `/outlet/$outletId` — KPI + tabel per produk + log + Update Harian
- [x] Komponen bersama: `RoleGate` (mirror `AuthorizeView`), `StatusPill`, `Modal` (ESC + backdrop), `lib/data.ts` (typed API layer: data/stock/agen/outlet)
- [x] Styles: `app.css` Blazor di-port **verbatim** (alias `--sm-*` + seluruh kelas: shell/nav/kpi/pill/chip/table/modal/segmented/breadcrumb/frame/chart/fonts Inter+Material Symbols)
- [x] Stub R4: `/tso`, `/mitra`, `/admin/users` (nav lengkap, halaman menyusul)
- Gerbang: ✅ build · lint · vitest 5/5 · **tsc --noEmit bersih** · ✅ 13 route SSR 200 via nginx, 0 error frontend · backend tak tersentuh

### R4 — Halaman TSO + Mitra + Admin ✅ selesai 2026-09
- [x] `/tso` — daftar order (Order No/Mitra/Tujuan/Material/Qty/Keberangkatan/Status) + Buat TSO (SA+Op) + Preview + Update (SA+Sup) + Hapus modal (SA)
- [x] **Wizard TSO 4 langkah** (`components/TsoWizard.tsx`, dipakai `/tso/create` + `/tso/$tsoId/edit`): (1) Tujuan & Obyek — multi-SKU LPG / minyak tunggal, stepper qty; (2) Rute & Jadwal — rute + jarak km (wajib utk tarif per_kilometer) + tanggal ≥ hari ini + ETA otomatis +7; (3) Transporter — mitra ter-filter area coverage + tarif per-jenis + estimasi biaya per baris (`tarif × qty [× jarak]`); (4) Ringkasan proposal — commit di Submit → redirect preview
- [x] `/tso/$tsoId` — Preview read-only 8 kolom + status pill + Resync (FlagTertunda) + **Generate Draft Invoice** (POST → blob → download) + link Update (SA+Sup)
- [x] `/mitra` (Superadmin) — tabel mitra + modal create/edit 12 field (rute comma-list, area coverage checklist DisplayName, aktif) + tarif per-jenis dengan allowlist satuan (per_kiloliter/per_tabung/per_kilometer); edit = PUT update + loop PUT tarif
- [x] `/admin/users` (Superadmin) — daftar user + roles pill toggle (klik assign/remove) + Set Password (prompt ≥8) + modal Tambah User (email/password/konfirmasi/roles checklist/role aktif terbatas)
- [x] `lib/tso.ts` + `lib/users.ts` — typed API layer (TransportOrder + details, MitraTso + tarifs, UserView)
- [x] **Fix backend**: `ReferenceHandler.IgnoreCycles` di JSON options — `GET /api/tso/{id}` dan `GET /api/mitra` 500 karena siklus navigasi balik (`TransportOrderDetail.Order`, `MitraTarif.Mitra`) yang baru pertama terekspos via HTTP
- Gerbang: ✅ build · lint · vitest · tsc bersih · ✅ backend 134/134 tetap hijau · ✅ E2E via nginx: create order (201 → StockImpacted) → GET detail + tarif rows → invoice PDF 200 (35KB, %PDF-) → 6 route R4 SSR 200

### R5 — Cutover + Hardening
- [x] **R5a — Web dipensiunkan (2026-09)**: `StockMonitorTso.Web` dihapus dari solusi + folder; **seluruh test integration dimigrasi ke host Api** (`TestApiWebApplicationFactory[WithStock]`) — service-DI suite swap mekanis (aktor ClaimsPrincipal manual host-agnostic), cookie suite diganti bearer/service (`AuthAndRbacTests`: idle-15-menit kini diuji via expiry token JWT; `AdminPageAccessTests` dihapus — paritas di `UsersApiTests` + anon-401 baru), `ActiveRoleClaimsPrincipalFactory` kini juga terdaftar di host Api (defense-in-depth klaim role aktif), legacy single-container deploy dihapus (root `Dockerfile` + `docker-compose.legacy.yml`)
- [x] Solution kini 3 proyek: Domain · Infrastructure · Api (+ frontend/ + tests) — compose 4 kontainer tetap
- [x] **R5b — Hardening (2026-09)**: Serilog structured (bootstrap→config-driven; JSON Compact di Production, teks di dev; request logging + RequestId) · `/ready` (cek `SELECT 1` database, tag "ready"; `/health` liveness) · template TLS nginx (`nginx-tls.conf.template` + `gen-dev-cert.sh`) · image di-slim: **api 461MB → 227MB** (base alpine + publish `-r linux-musl-x64`, folder runtimes 72MB hilang, fontconfig+font-dejavu untuk QuestPDF — PDF terverifikasi byte-identical) · healthcheck api pindah ke busybox wget (alpine tanpa bash) · DataProtection keys dir dibuat+owned di image
- Catatan: frontend image 239MB (node:22-alpine + output nitro) — gate <200MB berlaku untuk image api; ukuran frontend mengikuti base node resmi
- [x] **R5c — Sinkron `apps`**: merge main → apps (app + deploy artifacts; tanpa dokumentasi)
- [x] **R5d — Dokumentasi final**
- Gerbang tercapai (R5a): ✅ build 0/0 · ✅ **132/132 test** (32 unit + 100 integration, semua di host Api) · ✅ format bersih · ✅ compose 4 kontainer healthy + smoke via nginx

---

## Phase 0 — Skeleton

**Deskripsi:** Kerangka solusi minimal: repo, solution 4 proyek (+test), DI, `/health`, DB SQLite ter-create, shell Blazor + Identity login.

**Checklist:**
- [x] `git init` + `.gitignore` .NET 8
- [x] `dotnet new sln` + proyek `StockMonitorTso.{Domain,Infrastructure,Api,Web}`
- [x] DI wired; `/health` → 200
- [x] EF empty context auto-migrate (file SQLite tercipta)
- [x] Blazor Server default render + scaffold ASP.NET Core Identity (halaman login)

**Keterangan:**
- Gerbang: `dotnet run` boot, `curl /health` 200, halaman login render, DB file ada.
- Penamaan tanpa prefix `Monitoring.` (prefix itu milik produk uptime-monitor decoy — jangan ditiru).

---

## Phase 1 — Auth, RBAC & Sesi

**Deskripsi:** Otentikasi + otorisasi multi-role switchable + idle 15 menit + manajemen oleh Superadmin + audit log.

**Checklist:**
- [x] Identity: `User`, `Role`, `UserRole` (many-to-many)
- [x] Switchable active role: claim role-aktif scoped sesi (pilih saat login, switch in-sesi)
- [x] Idle timeout 15m (cookie sliding) + logout eksplisit
- [x] Assign role & ganti password **Superadmin-only** (di service layer, bukan UI saja)
- [x] **Buat user baru + assign role (Superadmin-only, 2026-09)**: `UserAdminService.CreateUserAsync` (email + password + ≥1 role + role aktif eksplisit), audit `CreateUser`; UI modal di `/admin/users`; scaffold `/Account/Register` dihapus (berbahaya: buat user tanpa role + `SignInAsync` menimpa sesi Superadmin).
- [x] `AuditLog` entity + `AuditLogService`

**Keterangan:**
- Gerbang: login multi-role, switch role, idle expiry, assign-role oleh Superadmin; non-Superadmin ditolak.
- Kebijakan **harus** di service layer (claim `Role` aktif), jangan hanya disembunyikan di UI.
- Akun seed: 5 akun (Superadmin/Operator/Supervisi/Tamu/Multi-role) — lihat `SeedData.cs`.

---

## Phase 2 — Monitoring Stok (read + compute)

**Deskripsi:** Entitas stok per `(Wilayah × Produk × Tier)`, penghitungan CD/Exhaust/Status/MT/CD_n, dashboard read-only, seed dari Excel.

**Checklist:**
- [x] Entitas `Wilayah` (7 canon), `Produk` (LPG 5.5/12/50 + Minyak Tanah), `Tier` (GudangWilayah, Agen, Outlet) — `GudangWilayah` = `Gudang Agen` (spec §2)
- [x] Entitas `Agen` (2–3 per Gudang) & `Outlet` (2 per Agen, tanpa limit) — entitas bernama, stok per (Agen×Produk)/(Outlet×Produk) (spec §2+§3.c)
- [x] `StokEntitas` (stok, DOT) + `RencanaKedatangan` (Next Supply, ETA, CD_n, Exhaust_n, maks 3 slot)
- [x] Rumus: `CD = Stok÷DOT`; `ExhaustDate = TanggalStokAwal + CD`; `Status` Kritis<3/Warning<7/Aman≥7
- [x] `MT = Tabung × kg ÷ 1000`; `Total MT = Σ MT` per wilayah (LPG)
- [x] `CD_n = (sisa stok saat ETA_n + Next Supply_n) ÷ DOT` — **konseptual, dilarang tiru Excel**
- [x] Dashboard read-only (tabel minyak tanah + LPG per ukuran + kartu ringkasan)
- [x] Seed loader Excel (`Monitoring Tabung RPM(1).xlsx` → sheet Agen/Outlet) + mock minyak tanah 7 wilayah

**Keterangan:**
- Gerbang: unit test formula ≥80%; dashboard menampilkan CD/Status benar dari seed.
- CD/Status/Next Supply **per ukuran**, tidak dilipat jadi satu angka per wilayah.
- Trap: rumus `CD_n` Excel (`Next Supply ÷ Σ CD`) salah dimensi — jangan diikuti.
- Unit: Tabung (LPG) vs Kiloliter (minyak tanah); tolak satuan silang.

---

## Phase 3 — CRUD Sales Area + Konservasi Stok

**Deskripsi:** Register/Update/Delete entitas stok dengan invarian konservasi atomic + redesign UI style Stitch.

**Checklist:**
- [x] Register form branching objek (Minyak vs LPG), field sesuai spec, validasi
- [x] Update: Superadmin + Supervisi; Delete: Superadmin-only (soft delete + modal konfirmasi)
- [x] Konservasi: mutasi stok via service transaksional atomic (`Receive`/`Adjust`/`Transfer`), audit tiap mutasi
- [x] Tolak overdraft (G3/F4): stok tidak boleh `< 0`
- [x] Recompute otomatis CD/Exhaust/Status setelah mutasi
- [x] Redesign UI Stitch (sidebar 288px, kartu, KPI, pill, modal, segmented, bar chart)
- [x] **Inventarisasi Tier Agen** (lanjutan 2026-08): entitas `Agen` bernama (2–3 per Gudang, `AgenId`), baris stok per (Agen×Produk), migrasi data `Tier.Agen`→`GudangWilayah`, mock 50% stok gudang dibagi rata + DOT rata (dengan audit Transfer), identitas Create/Update = Superadmin+Supervisi, halaman Daftar Agen + Detail Agen + Update Data Harian perukuran (Opsi C)
- [x] **Inventarisasi Tier Outlet** (lanjutan 2026-08): entitas `Outlet` bernama (2 per Agen, tanpa limit, `OutletId`), baris stok per (Outlet×Produk), mock 50% stok agen dibagi rata (dengan audit Transfer, outlet agregat lama di-soft-delete), halaman Daftar Outlet + Detail Outlet + Update Data Harian perukuran
- [x] **Transfer Gudang Wilayah → Agen** (lanjutan 2026-08): modal "Kirim ke Agen" (Superadmin+Supervisi), pilih 1 agen + kuantitas per SKU (3 SKU LPG sekaligus), loop `Transfer` atomic per SKU, overdraft ditolak (fix bug guard overdraft transfer), konservasi terjaga
- [x] **Transfer Agen → Outlet** (lanjutan 2026-08): modal "Kirim ke Outlet" (Superadmin+Supervisi, di Detail Agen), pilih 1 outlet + qty per SKU, loop `Transfer` atomic per SKU, lintas-agen ditolak
- [x] **Rev. UI card Gas LPG** (lanjutan 2026-08): 1 card per wilayah berisi 3 ukuran (chip titik warna); detail LPG gabungan 3 ukuran per Ukuran×Tier (route `Lpg`)
- [x] Fix temuan testing: dashboard card+chart, filter objek interaktif, tombol Detail (enum name), fixture test

**Keterangan:**
- Gerbang: integration test create/update/delete per role; konservasi terjaga; overdraft ditolak.
- **Resolved 2026-08 — Opsi C `Issue` eksplisit** (`docs/REVISION_NOTE_stock_reduction.md`): `Receive` masuk (+), `Issue` terjual (−, auto `StokHabisTerjual`), `Adjust` opname ±, `Transfer` pair. Modal **Update Data Harian** kini perukuran: `Stok Terjual` (Issue), `[☑] Stok Opname` (Adjust ±), `Stok Intransit` (metadata), `Keterangan`.
- Investigasi tiap opname (minus = unwanted loss jika disengaja / risiko natural) — lihat `CHANGE_PROCESS_NOTE.md`.
- Routing Detail wajib pakai **enum name** di URL + `Enum.TryParse` (bukan `DisplayName`).
- Halaman fitur wajib `@rendermode InteractiveServer` (tanpa itu dropdown/klik mati).
- Fixture test: `TestWebApplicationFactory` parameterless; subclass `...NoStock` untuk test yang register stok sendiri (`Seed:SkipStock=true`).

---

## Phase 4 — Modul TSO

**Deskripsi:** Order Transport Shipping Order Pusat → Gudang Wilayah dari master Mitra TSO; commit di Submit; dampak stok saat keberangkatan; Preview read-only + Draft Invoice PDF idempoten; Update/Delete order.

**Checklist:**
- [x] `MitraTso` (3 mitra) dimuat dari `seeds/mitra-tso.json` — `MitraTsoSeeder` upsert, tarif mutable diaudit, snapshot di order (harga dinamis)
- [x] **Wizard 4 langkah:** (1) Gudang+Obyek+Qty → (2) Rute & Jadwal (Pusat→Gudang, Tgl Keberangkatan ≥today, ETA+7) → (3) Transporter + Estimasi Biaya (`tarif × qty` dari master) → (4) Ringkasan → Submit (`/tso/create` + `/tso/{id}/edit`)
- [x] **Commit di Submit** (T11); submit ganda dicegah (T1/F9) — dedup key `Mitra+Wilayah+Produk+Qty+Tgl` 1 menit
- [x] Dampak stok saat keberangkatan: `RencanaKedatangan` (NextSupply=Kuantitas, ETA, Urutan 1..3) di Gudang Wilayah tujuan (T5); `Status FlagTertunda` jika gagal (F7/F10)
- [x] Halaman Preview read-only (`/tso/{id}`) — 8 kolom §4.d (tidak mengubah data)
- [x] Generate Draft Invoice PDF (QuestPDF, idempoten — regenerate bytes identical, `CreationDate=CreatedAt`, T9) — kolom: Mitra, Gudang Tujuan, Jenis Material, Kuantitas+satuan, Tgl Keberangkatan, ETA, Nomor Order, Timestamp
- [x] Update order (Superadmin+Supervisi, `RowVersion` 409 F8), Delete order (Superadmin, soft delete + modal)
- [x] Audit log tiap aksi order; role check per mutasi (T7/T8) — `RequireAnyRole` di service, `AuthorizeView` di UI
- [x] `/api/tso` — `MapGroup /api/tso`: `POST /`, `GET /`, `GET /{id}`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/invoice`, `POST /{id}/resync` — ProblemDetails 400/403/404/409

**Keterangan:**
- Gerbang: TSO→departure→RencanaKedatangan tercatat; PDF idempoten (bytes equal); role (Create Operator, Update Supervisi, Delete Superadmin); F7 FlagTertunda + resync.
- Satuan kanonik material mengikuti modul Monitoring (Tabung / Kiloliter); tolak satuan silang — `Produk.Satuan()`.
- Konservasi stok: dampak saat **keberangkatan**, bukan saat preview/generate — `TransportOrderService` → `RencanaKedatangan`.
- Snapshot harga: `TarifSnapshot`/`EstimasiBiayaSnapshot` disimpan di order (order lama tak berubah saat `Mitra.tarif` naik); CRUD Mitra ditunda Phase 5 (seed-only Phase 4).
- Deploy PoC: `Dockerfile` multi-stage non-root `USER 1654`, `docker-compose.yml` literal `80:8080` + `443:8081` (self-signed cert `/app/certs`), volume `stockmonitor_data` + `stockmonitor_keys` (DataProtection persist), `HealthCheck /health`.

---

## Phase 5 — Hardening (digabung ke R5)

**Deskripsi:** Kualitas produksi: logging terstruktur, ProblemDetails, readiness/metrics, container non-root, CI format, opsi PostgreSQL. **2026-09: seluruh item fase ini dipindahkan ke Phase R5 (cutover + hardening) — PostgreSQL tidak lagi opsional.**

**Checklist:**
- [ ] Serilog structured JSON + correlation id + redaksi secret
- [ ] ProblemDetails (RFC 7807) di semua error API; 400 untuk validasi bukan 500
- [ ] `/ready`; `/metrics` (prometheus-net, opsional MVP)
- [ ] Dockerfile multi-stage non-root; `docker compose up` smoke
- [ ] `dotnet format` di gerbang verifikasi/CI
- [ ] (Opsional) switch DB ke PostgreSQL: ganti connection string + provider package

**Keterangan:**
- Gerbang: smoke pass; image < 200MB; guardrails hijau; format clean.
- Roadmap PostgreSQL: default di Phase 5 (open question — bisa lebih awal jika diminta).

---

## Open Questions Lintas Fase

- [x] Mekanisme pengurangan stok / "Stok Terjual": **Opsi C `Issue` eksplisit** (`REVISION_NOTE_stock_reduction.md`) — resolved 2026-08
- [x] TSO wizard 4 langkah — **selesai & terverifikasi** (wizard Gudang→Rute→Transporter→Ringkasan + estimate `tarif×qty`)
- [ ] Monitoring Agen, Ekspor Laporan, Live Sync, Proyeksi Dampak Stok (fitur desain, `UI_REFERENCE.md §6`)
- [ ] Rename solution `StockMonitorTso.*` vs tetap
- [ ] Excel acuan: pakai baru vs cabut (kasus perombakan total domain — belum aktif)
- [ ] Tracker stok Gudang Pusat untuk cek overdraft TSO (default: **tidak**, hanya `Kuantitas > 0`)
- [ ] Admin CRUD Mitra TSO di app (Phase 5+) vs seed-only (Phase 4, default seed-only)
- [ ] Export/import seed Excel dari app (default: **tidak**)