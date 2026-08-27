# Plan — Aplikasi Stock Monitor dan TSO

> Workplan dan guard-rails untuk agent dan kontributor. Dibaca bersama
> `STOCK_MONITORING_SPEC.md`, `TRANSPORT_SHIPPING_ORDER_SPEC.md`,
> `Monitoring Tabung RPM(1).xlsx`, dan `seeds/mitra-tso.json`.

## 1. Goal

Membangun satu aplikasi web .NET 8 — **Aplikasi Stock Monitor dan TSO** —
dengan dua modul yang berbagi shell (login + dashboard):

- **Modul Monitoring Stok** (minyak tanah + LPG): tabel stok + kartu ringkasan;
  hirarki entitas `Gudang Agen (Warehouse) → Agen → Outlet` (spec §2);
  CD/Exhaust/Status/MT dihitung per `(Wilayah × Produk × Tier)` untuk Gudang/Outlet
  dan `(Agen × Produk)` untuk agen bernama (2–3 per Gudang, spec §3.c); invarian
  konservasi stok (debit-kredit atomic saat keberangkatan, same-day intra-region);
  Rencana Kedatangan hingga 3 slot; RBAC 4-role (Superadmin/Operator/Supervisi/Tamu).
- **Modul Transport Shipping Order (TSO)**: order `Pusat → Gudang Wilayah`
  dari Mitra TSO (seed); commit di Submit; dampak stok otomatis saat
  keberangkatan; Preview (read-only) + Generate Draft Invoice (PDF, idempotent);
  Update/Delete order.

Lintas modul: Identity, multi-role **switchable active role**, **idle timeout 15 menit**,
manajemen role & password **hanya Superadmin**, audit log append-only.

## 2. Architecture (MVP)

```
┌─────────────────────────┐
│  Blazor Server (UI)     │  login, dashboard, form branching, modal, unduh PDF
│  + Minimal API /api/*   │  JSON endpoints (data) + UI interaktif
└───────────┬─────────────┘
            │ EF Core (SQLite, MVP)
            ▼
     ┌────────────┐         ┌──────────────┐
     │  SQLite     │ ◀────── │ Seeds loader │  Monitoring Tabung RPM(1).xlsx
     │             │         │              │  + seeds/mitra-tso.json
     └────────────┘         └──────────────┘
            ▲
            │ audit log (append-only), transaksi konservasi (atomic)
```

- **Satu deployable** ASP.NET Core hosts Blazor Server + Minimal API via Generic Host.
- **Auth**: ASP.NET Core Identity (cookie, role-based). Multi-role via join many-to-many
  + **switchable active role**: claim role-aktif scoped ke sesi; dipilih saat login,
  bisa switch in-sesi. Idle timeout 15m (sliding cookie). Policy Superadmin-only di service layer.
- **Konservasi stok**: service transaksional atomic debit-kredit — angka stok tidak pernah diedit langsung.
- **PDF**: QuestPDF, generasi deterministik (Draft Invoice idempoten).
- **DB**: SQLite-only MVP lewat abstraksi provider EF Core; cukup ganti
  connection string + provider package untuk PostgreSQL di hardening.

## 3. Solution Layout

```
/src
  /StockMonitorTso.Domain          ← entities, enums (Wilayah, Produk, Tier, Status), value objects, domain services (konservasi, perhitungan CD)
  /StockMonitorTso.Infrastructure  ← EF DbContext, migrasi, repos, audit logger, seed loader, QuestPDF invoice generator
  /StockMonitorTso.Api             ← Minimal API (MapGroup per resource) + DI composition root
  /StockMonitorTso.Web             ← Blazor Server UI (login, dashboard, form, modal, unduh PDF)
/tests
  /StockMonitorTso.UnitTests       ← domain logic (formula, konservasi, RBAC) ≥80%
  /StockMonitorTso.IntegrationTests← WebApplicationFactory + SQLite in-memory
/seeds
  mitra-tso.json
```

Penamaan: `StockMonitorTso.<Layer>`; namespace mengikuti folder; TANPA prefix
`Monitoring.` (PLAN lama obsolete — produk uptime-monitor, jangan ditiru).

## 4. Phased Roadmap (LLM-driven)

Tiap fase berakhir dengan artifact runnable + gerbang verifikasi. Jangan mulai
fase n sebelum fase n−1 hijau. Klarifikasi open question sebelum slice terdampak.

### Phase 0 — Skeleton
- `git init` + `.gitignore` .NET 8; `dotnet new sln` + proyek di §3.
- DI wired, `/health` 200, EF empty context auto-migrate (file SQLite tercipta).
- Blazor Server default render; scaffold ASP.NET Core Identity (halaman login).
- **Done**: `dotnet run` boot, `curl /health` → 200, halaman login render, DB file ada.

### Phase 1 — Auth, RBAC & Sesi
- Identity: `User`, `Role`, `UserRole` (many-to-many).
- **Switchable active role**: claim role-aktif scoped sesi; pilih saat login, switch in-sesi.
- Idle timeout 15m (cookie sliding) + logout eksplisit.
- Policy: assign role & ganti password **Superadmin-only** (service layer, bukan UI saja).
- `AuditLog` entity + service.
- **Done**: integration test — login multi-role, switch role, idle expiry,
  assign-role oleh Superadmin only; non-Superadmin ditolak.

### Phase 2 — Monitoring Stok (read + compute)
- Entitas: `Wilayah` (7 enum canon), `Produk` (LPG 5.5/12/50 + Minyak Tanah),
  `Tier` (GudangWilayah, Agen, Outlet) — `GudangWilayah` = `Gudang Agen`/Warehouse (spec §2),
  `StokEntitas` (Wilayah×Produk×Tier untuk Gudang/Outlet; Agen×Produk untuk agen bernama via `AgenId`),
  `Agen` (entitas bernama, 2–3 per Gudang, spec §3.c),
  `RencanaKedatangan` (Next Supply, ETA, CD_n, ExhaustDate_n) hingga 3 slot.
- Domain services:
  - `CD = Stok / DOT` (per Wilayah×Produk×Tier)
  - `ExhaustDate = Tanggal Stok Awal + CD`
  - `Status`: Kritis <3, Warning 3–7, Aman ≥7
  - `MT = Tabung × berat ukuran / 1000` (LPG), `Total MT = Σ MT` per wilayah
  - `CD_n = (sisa stok saat ETA_n + Next Supply_n) / DOT`  ← **konseptual; JANGAN tiru rumus CD_n Excel** (`Next Supply ÷ Σ CD` — salah dimensi)
- Dashboard: tabel minyak tanah, tabel LPG (per ukuran), kartu (total, produk kritis, exhaust terdekat). Read-only.
- Seed loader: parse `Monitoring Tabung RPM(1).xlsx` (sheet Agen & Outlet) untuk seed awal.
- **Done**: unit test formula ≥80%; dashboard menampilkan CD/Status benar dari seed.

### Phase 3 — CRUD Sales Area + Konservasi
- Register form (branching objek stok: minyak tanah vs LPG; field sesuai `STOCK §5.c`).
- Identitas **Agen** (2–3 per Gudang, `AgenId`) & **Outlet** (2 per Agen, `OutletId`, one-to-many tanpa limit) — entitas bernama, stok per `(Agen×Produk)` / `(Outlet×Produk)` (spec §2+§3.c).
- Update (Superadmin + Supervisi), Delete (Superadmin, soft delete, modal konfirmasi).
- Invarian konservasi: perubahan stok via service transaksional atomic debit-kredit
  saat keberangkatan (same-day intra-region); tolak overdraft (G3); audit log tiap mutasi.
- Guardrails G1–G11 & Fallbacks F1–F14 (DOT=0 → N/A, overdraft rejected, ETA lampau,
  snapshot usang, optimistic concurrency, dll).
- Transfer Gudang→Agen (§5.f) & Agen→Outlet (§5.g): modal "Kirim ke Agen/Outlet" (Superadmin+Supervisi), pilih tujuan + qty per SKU, `Transfer` atomic per SKU.
- **Done**: integration test — create/update/delete per role sesuai §4; konservasi terjaga;
  overdraft ditolak; recompute otomatis CD/Exhaust/Status.

### Phase 4 — Modul TSO
- `MitraTso` dari `seeds/mitra-tso.json` (master, via seed loader).
- TSO form (Mitra, Jenis Material, Kuantitas, Tanggal Keberangkatan). **Commit di Submit**.
  Dampak stok saat keberangkatan diproses → debit sumber + `RencanaKedatangan`
  (Next Supply + ETA) di Gudang Wilayah tujuan (T5).
- Preview (read-only) + Generate Draft Invoice (QuestPDF, **idempoten**, 8 kolom sesuai `TSO §4.d`).
- Update order (Superadmin + Supervisi), Delete order (Superadmin).
- Guardrails T1–T11 & Fallbacks F1–F12 (mitra tak terdaftar 400, kuantitas kosong, tanggal
  lampau, gagal generate retry, koneksi monitoring putus → flag "dampak stok tertunda" + sync
  ulang, submit ganda idempoten, idle session, aksi tanpa wewenang).
- **Done**: integration test — TSO→departure→stok debit + Rencana Kedatangan tercatat;
  PDF idempoten (regenerate identical); role check; simulasi F7 (monitoring putus).

### Phase 5 — Hardening
- Serilog structured JSON + correlation id + redaksi secret.
- ProblemDetails (RFC 7807) di semua error API; 400 untuk validasi, bukan 500.
- `/ready`; `/metrics` (prometheus-net) — opsional MVP.
- Dockerfile multi-stage non-root; `docker compose up` smoke.
- `dotnet format` di gerbang verifikasi/CI.
- Roadmap: switch DB ke PostgreSQL (ganti connection string + provider package).
- **Done**: smoke pass; image < 200MB; semua guardrails hijau; format clean.

## 5. Coding Guard-Rails (wajib)

### 5.1 General
- C# 12, nullable on, `TreatWarningsAsErrors=true`; `dotnet format` di verifikasi.
- No comments kecuali intent non-obvious; no secrets in source (user-secrets/env).
- Async di hot path (handler, service I/O); no blocking sync I/O.

### 5.2 Persistence
- Semua perubahan skema via EF migrations (checked-in `Migrations/`); `Down` harus work.
- Tidak pernah `EnsureCreated` di prod path — migrations only.
- Query terparameterisasi (default EF); no string-concat LINQ.

### 5.3 Domain (khusus Stock Monitor + TSO)
- Konservasi stok via transaksi atomic service layer — **tidak pernah edit angka stok langsung**.
- `CD_n` pakai rumus konseptual; **dilarang** meniru `CD_n` Excel (`Next Supply ÷ Σ CD`).
- Satuan kanonik: **Tabung** untuk LPG, **Kiloliter** untuk minyak tanah; tolak satuan silang.
- Generasi invoice **deterministik** (idempoten; no random/timestamp di konten).
- Audit log wajib tiap aksi mutasi (pihak, role aktif, waktu, nilai sebelum/sesudah).
- `Tier` = `GudangWilayah` (= `Gudang Agen`/Warehouse, spec §2), `Agen`, `Outlet`; `Agen` bernama 2–3 per Gudang (spec §3.c).

### 5.4 API
- `MapGroup` + extension per resource under `/api/*`;
- `ProblemDetails` (RFC 7807) untuk error; 400 untuk validasi, bukan 500.
- Idempoten where feasible.

### 5.5 Testing
- xUnit + FluentAssertions + NSubstitute; domain logic ≥80%.
- Integration: `WebApplicationFactory`; SQLite in-memory; snapshot Excel hanya untuk seed parse.

### 5.6 Security (persona Izi)
- RBAC per endpoint/aksi; policy Superadmin-only di service layer (bukan UI saja).
- Parameterized; no secrets; log redaksi; role check di tiap mutasi.

### 5.7 Dependencies (approved)
- `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.AspNetCore.Identity`,
  `Serilog.AspNetCore` + sink Console, `QuestPDF`, `FluentValidation.AspNetCore`,
  `Swashbuckle.AspNetCore`.
- Test: `xunit`, `FluentAssertions`, `NSubstitute`, `Microsoft.AspNetCore.Mvc.Testing`.
- Paket baru wajib dijelaskan di deskripsi PR.

## 6. Definition of Done (per task)
- [ ] `dotnet build -warnaserror` bersih.
- [ ] Unit test ditambah/diperbarui; coverage tidak turun.
- [ ] Integration test untuk interaksi eksternal baru.
- [ ] Audit log ada untuk tiap mutasi.
- [ ] Tidak ada `// TODO` baru; cross-check dengan SPEC terkait.
- [ ] Jika mengubah data/dashboard: update SPEC bila perlu.

## 7. Verification Commands
```bash
dotnet restore
dotnet build -warnaserror
dotnet test
dotnet format --verify
dotnet run --project src/StockMonitorTso.Web --urls http://0.0.0.0:8080
```

## 8. Risks & Mitigations
| Risk | Mitigation |
| --- | --- |
| Konservasi stok drift | Transaksi atomic + audit + integration test overdraft |
| Salah formula `CD_n` | Rumus konseptual; spec eksplisit larangan Excel; unit test |
| Multi-role switch bug | Claim session-scoped; test switch + idle |
| PDF non-idempoten | QuestPDF deterministik; test regenerate identical |
| Lead time variabel Papua/Maluku | ETA estimasi; flag "Terlambat" (F5) |
| Seed Excel drift | Parse sekali saat seed; formula lock di `STOCK §3.c` |

## 9. Open Questions
- Switch PostgreSQL: Phase 5 (default) vs lebih awal?
- Apakah melacak stok Gudang Pusat untuk cek overdraft TSO Pusat→Wilayah? Default **tidak** (cuma `Kuantitas > 0`).
- Admin CRUD Mitra TSO di app (Phase 5+) vs seed-only (Phase 4)? Default seed-only Phase 4.
- Export/import seed Excel dari app? Default **tidak**.

## 10. References
- `STOCK_MONITORING_SPEC.md` — spec Monitoring Stok.
- `TRANSPORT_SHIPPING_ORDER_SPEC.md` — spec TSO.
- `Monitoring Tabung RPM(1).xlsx` — sumber kebenaran komputasi (sheet Agen & Outlet).
- `seeds/mitra-tso.json` — seed master Mitra TSO.
- .NET 8: https://learn.microsoft.com/dotnet
- EF Core migrations: https://learn.microsoft.com/ef/core/managing-schemas/migrations
- QuestPDF: https://www.questpdf.net
- ASP.NET Core Identity: https://learn.microsoft.com/aspnet/core/security/authentication/identity
