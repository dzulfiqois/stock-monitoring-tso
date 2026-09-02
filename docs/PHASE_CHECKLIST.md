# Checklist Fase Pengembangan — Stock Monitor dan TSO

> Checklist ringkas per fase (`PLAN.md §4`). Dibaca bersama `PLAN.md`, `SESSION_HANDOFF.md`,
> `STOCK_MONITORING_SPEC.md`, `TRANSPORT_SHIPPING_ORDER_SPEC.md`.
> Status per 2026-09: **Phase 0–4 hijau**, Phase 5 belum.
> Verifikasi: `dotnet build -warnaserror` (0 error) · `dotnet test` 98/98 lulus tanpa 5 prasejarah (32 unit) · `dotnet format --verify` bersih · smoke `/health` 200 + DB seed (Agen 18, Outlet 36, Mitra 3).
>
> Catatan: **5 test prasejarah (`StockDashboardTests` ×4, `AgenDashboardTests.GetSalesAreaCards_Papua_…` ×1)** gagal karena `Monitoring Tabung RPM(1).xlsx` di-gitignore di branch `apps` (tetap ada di `main`). Sama dengan tanpa slice ini.

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

## Phase 5 — Hardening

**Deskripsi:** Kualitas produksi: logging terstruktur, ProblemDetails, readiness/metrics, container non-root, CI format, opsi PostgreSQL.

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