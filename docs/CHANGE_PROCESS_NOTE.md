# Note — Proses Perubahan Langkah Bisnis pada CRUD Monitoring Stok

> Tujuan: reminder bagi pengembang (dan Panggih/stakeholder) bahwa **CRUD Phase 3 belum final**.
> Perubahan langkah bisnis yang disepakati dalam meeting stakeholder bisa diterapkan kapan pun,
> bahkan setelah phase selanjutnya (4–7) berjalan. Bagian ini adalah SOP langkah-demi-langkah agar
> perubahan aman, teruji, dan tidak merusak modul lain.

---

## 1. Status CRUD Phase 3

- [ ] **Belum fix** — menunggu kesepakatan bisnis dengan stakeholder (meeting).
- [ ] Keputusan stakeholder ditangkap sebagai amend/update pada `STOCK_MONITORING_SPEC.md`.
- [ ] Setelah disepakati → ikuti langkah di §2, lalu tandai checklist di §5.

> Prinsip: **spec adalah kebenaran terlebih dahulu, kode mengikuti.** Tidak pernah ubah kode
> dulu baru spec.

---

## 2. Langkah-langkah perubahan (SOP)

### Langkah 0 — Tangkap keputusan bisnis
- Tuliskan hasil meeting stakeholder dengan jelas: apa yang berubah? (alur/form/role/aturan stok).
- Bedakan: **perubahan lokal** (alur CRUD, tombol, role) vs **perubahan fondasi**
  (konservasi stok, granularitas, satuan) — yang fondasi berdampak ke modul lain (TSO/dashboard).
- Buat amend kecil di `STOCK_MONITORING_SPEC.md` §3/§4 (dan spec TSO bila kena) sebelum coding.

### Langkah 1 — Update spec (source of truth)
- [ ] `STOCK_MONITORING_SPEC.md` — ubah alur §4, matriks role §3, rumus/guardrail §2.c/§5 bila perlu.
- [ ] `TRANSPORT_SHIPPING_ORDER_SPEC.md` — hanya jika perubahan menyentuh modul TSO.
- [ ] `docs/UI_REFERENCE.md`/`docs/DEVELOPER_GUIDE.md` — sinkronkan peta/alur jika terdampak.

### Langkah 2 — Identifikasi kode yang terdampak
Gunakan peta di `docs/DEVELOPER_GUIDE.md §5` sebagai acuan. Tempat yang paling sering berubah:
- `StockWriteService.cs` — alur transaksi/CRUD stok (konservasi).
- `AgenService.cs` — identitas agen (Create/Update/Delete) & transfer.
- `StockDashboardService.cs` — agregasi/card/detail.
- `AgenMockSeeder.cs` / `SeedData.cs` — mock & data awal.
- `Web/Components/Pages/*.razor` — UI/form/modal.
- `ApplicationDbContext.cs` + migrasi — bila granularitas kolom/uniqueness berubah.

### Langkah 3 — Implementasi satu slice
- Kerjakan perubahan sekecil mungkin, satu alur bisnis per commit/slice.
- Jangan menukar urutan: **spec → kode → test → verifikasi**.

### Langkah 4 — Sesuaikan & tambah test
- Update test yang terdampak (`StockWriteTests`, `AgenServiceTests`, `WarehouseTransferTests`,
  `StockDashboardTests`, `AuthAndRbacTests`).
- Tambah unit/integrasi untuk alur/matriks baru.
- Jika menyentuh konservasi: pastikan test overdraft & konservasi tetap hijau.

### Langkah 5 — Jalankan gerbang verifikasi

```bash
dotnet build StockMonitorTso.sln -warnaserror
dotnet test StockMonitorTso.sln
dotnet format StockMonitorTso.sln --verify-no-changes
```

- Harus **0 warning/error**, semua test **hijau**.
- Smoke (login + alur baru) dijalankan manual (atau `dotnet watch run`).
- Ingat: perubahan kode `.razor`/`.cs` butuh rebuild/restart app untuk dilihat.

### Langkah 6 — Dokumentasikan & tandai final
- Perbarui `docs/PHASE_CHECKLIST.md` dan `docs/SESSION_HANDOFF.md`.
- Update `docs/REVISION_NOTE_stock_reduction.md` bila perubahan menyangkut "Stok Terjual".
- Ganti status di §1 → tandai CRUD **fix** setelah disepakati & terverifikasi.

---

## 3. Bagian yang dampaknya "fondasi" (hati-hati, berdampak fase lain)

| Area | Risiko | Catatan |
|---|---|---|
| Konservasi stok (Receive/Adjust/Transfer) | Tinggi | Dipakai modul TSO & dashboard; jaga atomik + overdraft |
| Granularitas data / uniqueness | Tinggi | Ubah skema → migrasi + test konservasi |
| Satuan kanonik (Tabung/KL) | Tinggi | Tolak campur, ikut rumus |
| RBAC / matriks role | Sedang | Enforce di service layer, bukan cuma UI |
| Alur/form CRUD (tombol, modal, flow) | Rendah | Dampak lokal, paling aman diubah |

---

## 4. Pesan kunci

> **Kapan pun perubahan disepakati, terapkan lewat proses di atas — walau sudah di Phase 7.**
> Tidak ada "fase terkunci"; yang ada adalah disiplin: spec dulu, kode & test menyusul,
> verifikasi hijau, dokumentasi sinkron. Dengan begitu kembali ke Phase 3 tidak akan merusak
> modul yang sudah dibangun.

---

## 5. Checklist status (update manual)

- [ ] Keputusan stakeholder tercatat (tanggal: ____)
- [ ] Spec di-update
- [ ] Kode + test disesuaikan
- [ ] Gerbang verifikasi hijau
- [ ] Docs sinkron
- [ ] CRUD Phase 3 dinyatakan **final** (Beri tanggal: ____)
