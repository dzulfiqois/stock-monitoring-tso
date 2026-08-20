# Checklist Fase Pengembangan — Stock Monitor dan TSO

> Checklist ringkas per fase (`PLAN.md §4`). Dibaca bersama `PLAN.md`, `SESSION_HANDOFF.md`,
> `STOCK_MONITORING_SPEC.md`, `TRANSPORT_SHIPPING_ORDER_SPEC.md`.
> Status per 2026-08: **Phase 0–3 hijau**, Phase 4 berikutnya, Phase 5 belum.
> Verifikasi: `dotnet build -warnaserror` · `dotnet test` · `dotnet format --verify` · smoke.

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
- [x] `AuditLog` entity + `AuditLogService`

**Keterangan:**
- Gerbang: login multi-role, switch role, idle expiry, assign-role oleh Superadmin; non-Superadmin ditolak.
- Kebijakan **harus** di service layer (claim `Role` aktif), jangan hanya disembunyikan di UI.
- Akun seed: 5 akun (Superadmin/Operator/Supervisi/Tamu/Multi-role) — lihat `SeedData.cs`.

---

## Phase 2 — Monitoring Stok (read + compute)

**Deskripsi:** Entitas stok per `(Wilayah × Produk × Tier)`, penghitungan CD/Exhaust/Status/MT/CD_n, dashboard read-only, seed dari Excel.

**Checklist:**
- [x] Entitas `Wilayah` (7 canon), `Produk` (LPG 5.5/12/50 + Minyak Tanah), `Tier` (Agen/Outlet)
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
- [x] **Inventarisasi Tier Agen** (lanjutan 2026-08): entitas `Agen` bernama (2–3 per gudang wilayah), baris stok per (Agen × Produk), migrasi data `Tier.Agen`→`GudangWilayah`, mock 50% stok gudang dibagi rata + DOT rata (dengan audit Transfer), identitas agen Create/Update = Superadmin+Supervisi, halaman Daftar Agen + Detail Agen + Update Data Harian
- [x] **Transfer Gudang Wilayah → Agen** (lanjutan 2026-08): modal "Kirim ke Agen" (Superadmin+Supervisi), pilih 1 agen + kuantitas per SKU (3 SKU LPG sekaligus), loop `Transfer` atomic per SKU, overdraft ditolak (fix bug guard overdraft transfer), konservasi terjaga
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

## Phase 4 — Modul TSO (BERIKUTNYA — belum ada kode)

**Deskripsi:** Order Transport Shipping Order Pusat → Gudang Wilayah dari master Mitra TSO; commit di Submit; dampak stok saat keberangkatan; Preview read-only + Draft Invoice PDF idempoten; Update/Delete order.

**Checklist:**
- [ ] `MitraTso` dimuat dari `seeds/mitra-tso.json` (master, via seed loader)
- [ ] Halaman form TSO: Mitra (dari master, bukan teks bebas), Jenis Material (enum sama dgn Monitoring), Kuantitas (>0, satuan sesuai material), Tanggal Keberangkatan (tidak lampau)
- [ ] **Commit di Submit** (T11); submit ganda dicegah (T1/F9)
- [ ] Dampak stok saat keberangkatan: debit sumber + `RencanaKedatangan` (Next Supply + ETA) di Gudang Wilayah tujuan (T5)
- [ ] Halaman Preview read-only (tidak mengubah data)
- [ ] Generate Draft Invoice PDF (QuestPDF, idempoten — regenerate identik, T9) dengan kolom minimal per spec §4.d
- [ ] Update order (Superadmin+Supervisi), Delete order (Superadmin, soft delete + modal)
- [ ] Audit log tiap aksi order; role check per mutasi (T7/T8)
- [ ] Fallback: tak terdaftar 400 (F3), tanggal lampau (F5), gagal PDF (F6), monitoring putus → flag "dampak stok tertunda" + resync (F7/F10), idle (F11), tanpa wewenang (F12)

**Keterangan:**
- Gerbang: TSO → departure → stok debit + Rencana Kedatangan tercatat; PDF idempoten; role check; simulasi F7.
- Satuan kanonik material mengikuti modul Monitoring (Tabung / Kiloliter); tolak satuan silang.
- Konservasi stok: dampak saat **keberangkatan**, bukan saat preview/generate.
- Open question terkait: TSO wizard 4 langkah + Draft Proposal sidebar (Draft ID `SO-…`, Est. Biaya) — fitur desain, **butuh persetujuan** sebelum dikerjakan (`UI_REFERENCE.md §6`).
- Hilangkan blocker §"Stok Terjual" (Phase 3 open) jika TSO bergantung pada jalur debit—sebaiknya putuskan mekanisme pengurangan stok **sebelum** Phase 4.

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

- [ ] Mekanisme pengurangan stok / "Stok Terjual": opsi **A / B / C / D** (`REVISION_NOTE_stock_reduction.md`)
- [ ] TSO wizard 4 langkah, Monitoring Agen, Ekspor Laporan, Live Sync, Proyeksi Dampak Stok (fitur desain, `UI_REFERENCE.md §6`)
- [ ] Rename solution `StockMonitorTso.*` vs tetap
- [ ] Excel acuan: pakai baru vs cabut (kasus perombakan total domain — belum aktif)
- [ ] Tracker stok Gudang Pusat untuk cek overdraft TSO (default: **tidak**, hanya `Kuantitas > 0`)
- [ ] Admin CRUD Mitra TSO di app (Phase 5+) vs seed-only (Phase 4, default seed-only)
- [ ] Export/import seed Excel dari app (default: **tidak**)