# Revision Note — Mekanisme Pengurangan Stok (Update Stok Harian)

> Mode: **resolved — Opsi C (`Issue` eksplisit) dipilih 2026-08**. `Receive` = masuk (+), `Issue` = keluar/terjual (−, auto `StokHabisTerjual`), `Adjust` = opname ±, `Transfer` = pair.

## 1. Lokasi problem

**Konsep spec:**
- `STOCK_MONITORING_SPEC.md` §4.c — form Minyak Tanah & LPG, field **"Stok Habis Terjual"**;
  §5.2 F4 (overdraft ditolak), F8 (stockout).
- Desain UI `DESIGN_REFERENCES.html` — layar **F** (Detail Monitoring Minyak Tanah) & **I**
  (Detail Monitoring Gas Tabung): modal **"Update Data Harian" → tab "Update Stok Harian" →
  input "Stok Terjual"** dan "Stok Intransit".

**Kode yang jadi conflict:**
- `src/StockMonitorTso.Infrastructure/Services/StockWriteService.cs:98` — `if (kuantitas <= 0) throw`
- `Adjust`: `entity.Stok += kuantitas` → hanya menambah; **tidak pernah mengurangi**
- `src/StockMonitorTso.Web/Components/Pages/DetailSalesArea.razor` — mode "daily" memanggil
  `TransactAsync(StockTransactionType.Adjust, -soldQty, ...)` dengan **kuantitas NEGATIF**
  → ditolak di validasi, sehingga "Stok Terjual" tidak berfungsi.

## 2. Akar design gap

1. **`StockTransactionType` tidak mencakup arah keluar.** `Receive` = masuk (+), `Transfer` = pasangan
   debit/kredit antar-tier, `Adjust` ambigu dan dipaksa selalu `kuantitas > 0` → **tidak ada cara sah
   untuk stok turun** akibat penjualan/outtake.
2. **Diskrepansi UI vs service**: UI menginstruksi pengurangan via `Adjust` negatif, service menolaknya.
3. **Kebutuhan konservasi tetap**: debit/kredit berpasangan, stok ≥ 0 (G3/F4), audit tercatat (G1).

## 3. Defect yang sudah terdeteksi (test gagal Phase 3)

- `StockWriteTests.Adjust_Overdraft_Rejected_AndStokUnchanged` — adjust `-200` melempar
  `ArgumentOutOfRangeException` (kuantitas>0) — bukan overdraft.
- `StockWriteTests.Delete_SoftDeletes_AndHiddenFromDashboard` — **test salah** (assertion tak
  memfilter data seed Maluku yang ikut muncul); mekanisme delete/soft-hidden sudah benar.
  (Perbaikan assertion terpisah, bukan bagian desain ini.)

## 4. Opsi desain (tentukan salah satu)

| Opsi | Deskripsi | Konservasi |
|------|-----------|------------|
| **A. Pisahkan `Debit`/`Kredit`** | `StockTransactionType.Debit` (keluar −), `Kredit` (masuk +); `Transfer` = pasangan keduanya; `Adjust` jadi opname ± (bisa naik/turun dengan guard). Paling jelas domain. | ✔ atomic pair |
| **B. `Adjust` terima ±** | Izinkan kuantitas negatif di `Adjust`; validasi akhir cuma "stok ≥ 0". Sederhana; `Adjust` overloaded (koreksi vs terjual). | ✔ bila cek stok tetap |
| **C. `Issue`/`Usage` eksplisit** | Tambah `Issue` (pengeluaran/penjualan, kuantitas positif yang MENURUNKAN stok); `Adjust` khusus opname. Mirip inventory mgmt (receipt/issue). | ✔ debit-only |
| **D. "Stok Terjual" bukan mutasi stok** | Catat sebagai metrik terpisah, jangan menurunkan `Stok`; CD dihitung dari snapshot lain. Hindari konflik, menggeser makna CD. | ± |

## 5. Keputusan desain (terkunci 2026-08 — Opsi C)

- [x] Cara **sah** stok berkurang: **Opsi C — `Issue` eksplisit** (penjualan/outtake via `Issue`, Qty>0; `Adjust` khusus opname)
- [x] **"Stok Terjual" menurunkan stok**: YA — `Issue` menurunkan `Stok` + auto `StokHabisTerjual += Qty`
- [x] **Naming transaksi**: `Receive / Issue / Adjust / Transfer` (audit clarity)
- [x] Guardrail overdraft: YA, tetap tolak stok < 0 (G3/F4) — `Adjust` dan `Issue` cek `Stok - qty < 0`

## 6. Dampak implementasi (setelah desain dipilih)

- `Domain/Entities/StockTransaction.cs` — tambah/ubah enum + semantik.
- `Infrastructure/Services/StockWriteService.cs` — `TransactAsync`: pisah jalur masuk/keluar,
  validasi stok ≥ 0, pair debit-kredit.
- `Web/Components/Pages/DetailSalesArea.razor` — selaraskan pemanggilan "(daily)" dengan opsi.
- `tests/.../StockWriteTests.cs` — perbarui test overdraft + tambah konservasi keluar.
- `docs/UI_REFERENCE.md`, `STOCK_MONITORING_SPEC.md` §2.c/§4.c/§5.2 — sinkron terminologi.

## 7. Status
- [x] Titik revisi diidentifikasi & dicatat
- [x] Arah desain dipilih — **Opsi C `Issue` eksplisit** (2026-08)
- [x] Kode + test disesuaikan — `StockTransaction.Issue` + `StockWriteService` (Issue/Adjust±/auto Terjual) + modal perukuran
- [x] UI selaras — `DetailSalesArea`/`DetailAgen` perukuran (Terjual, [☑]Opname±, Intransit, Keterangan)
- [ ] Guardrail audit: `Adjust` opname + `Issue` penjualan terpisah di `Log Transaksi` (investigate tiap opname)
