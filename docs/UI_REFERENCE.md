# UI Reference — Aplikasi Stock Monitor dan TSO

> Sumber: `DESIGN_REFERENCES.html` (export Google Stitch, 9 layar).
> Status: **referensi style** untuk domain saat ini (7 wilayah Papua/Maluku).
> Dibaca bersama `STOCK_MONITORING_SPEC.md`, `TRANSPORT_SHIPPING_ORDER_SPEC.md`, `AGENTS.md`.

## 1. Status & nota pemisahan (penting — baca dulu)

- **Yang diadopsi**: token desain (warna, font, radius, spacing) dan pola komponen UI (sidebar, kartu, badge, stepper, KPI bento, modal, bar chart).
- **Yang TIDAK diadopsi** (data/scope desain, bukan style):
  - Gudang regional nasional: `Terminal BBM Plumpang`, `TBBM Tanjung Gerem`, `TBBM Balongan`, `TBBM Semarang` — **bukan** scope kita.
  - Filter wilayah grup: `Jawa & Bali`, `Sumatera`, `Kalimantan`, `Sulawesi & Timur` — **bukan** 7 wilayah kanonik kita (Maluku, Papua Barat, Papua Barat Daya, Maluku Utara, Papua Tengah, Papua Selatan-Pegunungan, Papua).
  - Angka contoh (5.2M Ton, 1,450 KL, Rp 12,450,000, dst) — hanya mockup.
- **Brand**: desain memakai 2 nama berbeda ("Industrial Flow" di layar TSO, "FlowMonitor" di layar gudang) — keduanya **tidak dipakai**; identitas app kita "Stock Monitor dan TSO".
- **Keputusan scope**: desain = style saja (keputusan user, 2026-08). Setiap fitur baru yang desain tampilkan (TSO wizard 4 langkah, menu Gudang Wilayah/Monitoring Agen, dst) adalah **open question** — lihat §6; tidak dieksekusi tanpa persetujuan.

## 2. Katalog layar (9)

| # | Layar | Isi | Pemetaan page |
|---|-------|-----|---------------|
| A | TSO – Pilih Transporter | Stepper 3 (Order Detail ✓ → Pilih Transporter → Konfirmasi); search "Cari mitra transporter…" | Step Transporter (TSO) |
| B | TSO – Langkah 1 "Tujuan & Obyek" | Stepper 4; Lokasi Asal (dropdown Gudang Regional); Rincian Obyek (kartu radio Minyak Tanah / Gas LPG); Kuantitas Muatan (stepper, min 8 max 120 step 8, KL); peta; "Lanjutkan ke Transporter" | Step 1 TSO |
| C | TSO – Rute & Jadwal | Stepper (Info Dasar ✓, Muatan ✓, Rute & Jadwal aktif, Ringkasan); Definisi Rute (Asal/Tujuan dropdown); Penjadwalan (Estimasi Keberangkatan/Kedatangan datetime, estimasi tempuh); peta | Step Rute & Jadwal TSO |
| D | TSO – Ringkasan Proposal | Stepper 4 ✓; Ringkasan Konfigurasi (kartu: Sumber Gudang, Vendor Transporter, Rute & Tujuan, Jadwal Pengiriman — masing-masing tombol edit); Proyeksi Dampak Stok (bar chart Stok Awal vs Proyeksi Baru, H-2..H+2); Draft Proposal sidebar (Draft ID `SO-…`, Vol Total, Est. Biaya, Generate Proposal / Diskusi / Simpan) | Step Ringkasan TSO (Preview + Draft Invoice) |
| E | Gudang Wilayah – Minyak Tanah | Filter Wilayah + Obyek; "Tambah Gudang"; 3 metrik (Total Kapasitas, Peringatan Stok Rendah, Armada In-Transit); kartu Sales Area (Realisasi Tanggal, Sisa Agen/Outlet, Habis Terjual, In-Transit, Keterangan) | `GudangWilayah` (baru) |
| F | Detail Monitoring Minyak Tanah | Breadcrumb; "Update Data Harian"; 4 KPI (Total Sisa Stok, Terjual, Intransit, Status Area); Log Distribusi Harian table (No, Nama Sales Area, Realisasi Tanggal, Sisa Agen, Sisa Outlet, Terjual, Intransit, Keterangan, Aksi/Edit); modal Update (segmented Isi Ulang Stok / Update Stok Harian) | `DetailSalesArea` + modal (baru) |
| G | Ringkasan Operasional (Dashboard) | Live Sync badge; "Ekspor Laporan"; KPI Sektor Gas (Total Stok 82%, Outlet Kritis) & Sektor Minyak Tanah (72%, Kritis); chart tren per-region (Harian/Mingguan/Bulanan, region kritis merah); Metrik Minyak Tanah table | `Home.razor` (redesign) |
| H | Gudang Wilayah – Gas Tabung (Card) | Sama dgn E, obyek Gas; kartu Papua dgn chip per-ukuran (5.5kg pink / 12kg biru / 50kg oranye); Total Stok; Status Aman | `GudangWilayah` (baru) |
| I | Detail Monitoring Gas Tabung | 4 KPI (Total Stok, Daily Obj. Throughput, Covered Days, Status Area); tabel Data Inventory & Distribusi Agen (legend warna ukuran; kolom persis spec LPG: Stok Gudang Agen per ukuran, 5.5/12/50 Total Wilayah, Total Stok, DOT, CD, Status, Next Supply); modal Update (versi Tabung) | `DetailSalesArea` + modal (baru) |

## 3. Design tokens (untuk ditiru di Blazor)

### Warna (Material 3, dari tailwind-config)

| Token | Hex | Penggunaan |
|-------|-----|------------|
| `primary` | `#00355f` | Tombol utama, stepper aktif, aksen |
| `primary-container` | `#0f4c81` | Aktif nav, chip terpilih, icon |
| `on-primary` | `#ffffff` | Teks di atas primary |
| `background` | `#f7f9fb` | Latar halaman |
| `surface-container-lowest` | `#ffffff` | Kartu, sidebar |
| `surface-container-low` | `#f2f4f6` | Input, chip netral |
| `surface-container` | `#eceef0` | Baris header tabel, segmen |
| `surface-container-high` | `#e6e8ea` | Hover, icon container |
| `error` | `#ba1a1a` | Status Kritis, peringatan |
| `error-container` | `#ffdad6` | Kartu peringatan (bg) |
| `on-error-container` | `#93000a` | Teks di kartu error |
| `secondary` | `#48626e` | Aksen minyak tanah, info |
| `secondary-container` | `#cbe7f5` | Icon/vendor |
| `tertiary` | `#532800` | Aksen rute/dokumen |
| `tertiary-container` | `#743b00` | Icon |
| `on-surface` | `#191c1e` | Teks utama |
| `on-surface-variant` | `#42474f` | Teks sekunder, label |
| `outline` | `#727780` | Border kuat |
| `outline-variant` | `#c2c7d1` | Border halus, divider |
| Sukses | `#16a34a` (green-600) | Status Aman, live dot |
| Chip 5.5kg | `#f472b6` (pink-400) | Per-ukuran |
| Chip 12kg | `#3b82f6` (blue-500) | Per-ukuran |
| Chip 50kg | `#f97316` (orange-500) | Per-ukuran |

### Tipografi
- **Inter** (wght 100–900) — teks UI.
- **Material Symbols Outlined** — icon (propane, oil_barrel, warehouse, local_shipping, route, calendar_today, dashboard, factory, dashboard, sync, edit, add, arrow_forward, dll).

### Radius & Spacing
- Radius: `DEFAULT 0.125rem`, `lg 0.25rem`, `xl 0.5rem`, `full 0.75rem`.
- Spacing: `base 4px`, `gutter 16px`, `stack-sm 8px`, `stack-md 16px`, `stack-lg 32px`, `container-padding 24px`.
- Sidebar: `w-72` (288px). Konten: `max-w-[1440px] mx-auto`, `px-container-padding`.

## 4. Pola komponen (referensi implementasi)

- **Sidebar**: fixed kiri 288px, bg `surface-container-lowest`, logo + nama brand, nav item = icon + label; aktif = `bg-primary-container text-on-primary-container rounded-xl shadow-md`.
- **Kartu Sales Area**: `bg-surface-container-lowest rounded-xl shadow-sm border border-outline-variant/30 p-stack-md`; header (nama + badge kategori), body data, footer status.
- **Badge status**: `inline-flex items-center px-2.5 py-1 rounded-full` + pasangan `bg-X/10 text-X`; Aman=green, Kritis=error, (Warning=amber bila muncul).
- **KPI bento**: kartu dengan `absolute -right-* -top-*` blur-circle dekoratif + label caps + angka besar (`font-display-lg`) + ikon kanan-atas + footer delta.
- **Stepper wizard**: lingkaran `rounded-full`; aktif `bg-primary text-on-primary shadow`; selesai = ikon `check`; pending = `bg-surface-container-high`; dihubungkan garis `h-[2px] bg-primary/30`.
- **Modal**: `fixed inset-0 bg-on-surface/40 backdrop-blur-sm`; card `max-w-md rounded-xl`; header + body + footer (Batal / Simpan).
- **Segmented control** (mode form): `flex bg-surface-container-low p-1 rounded-lg`; aktif `bg-primary text-on-primary`; tombol `flex-1 py-2`.
- **Bottom action bar**: `fixed bottom-0 bg-surface-container-lowest/80 backdrop-blur-xl border-t`.
- **Bar chart**: inline (SVG/div) dengan grid y 100/75/50/25% + bar per kelompok (target vs stok saat ini; bar region kritis merah).
- **Stepper qty input**: tombol − / + di tepi input angka `step`/`min`/`max`.

## 5. Konvensi unit & status (tetap ikut spec, bukan desain)

- Minyak tanah: **Kiloliter (KL)** — truk 16–32 KL.
- LPG: **Tabung** — ukuran 5.5 / 12 / 50 kg.
- Dashboard agregasi: **Ton** untuk LPG (MT = Tabung × kg ÷ 1000).
- Status: **Aman** (CD ≥ 7) = green, **Warning** (3 ≤ CD < 7) = amber, **Kritis** (CD < 3) = red. Threshold dari `STOCK_MONITORING_SPEC.md` §2.c.
- Next Supply / Rencana Kedatangan: hingga 3 slot per entitas.

## 6. Fitur baru yang desain tampilkan (open questions — butuh persetujuan, bukan style)

Fitur berikut ada di desain tapi **belum** menjadi keputusan spec. Jangan implement tanpa persetujuan:

| Fitur | Ada di desain | Status |
|-------|---------------|--------|
| TSO wizard 4 langkah (Tujuan & Obyek → Transporter → Rute & Jadwal → Ringkasan/Proposal) + Rute & Jadwal (asal/tujuan + estimasi) | B, C, D | Open — memperluas `TRANSPORT_SHIPPING_ORDER_SPEC.md` |
| Draft Proposal sidebar (Draft ID `SO-…`, Vol Total, Est. Biaya, Generate Proposal) | D | Open — terkait Draft Invoice (F20) |
| Proyeksi Dampak Stok (chart Stok Awal vs Proyeksi Baru) | D | Open |
| Halaman Gudang Wilayah (card view + filter 7 wilayah/obyek + Tambah Gudang) | E, H | Open — Tambah Gudang = master baru |
| Menu Monitoring Agen | E-H (sidebar) | Open — bagian "Daftar Agen"/"Detail Agen" per gudang wilayah diimplementasi pada Phase 3 (menu utama belum) |
| Update Data Harian modal (2 mode: Isi Ulang Stok / Update Stok Harian) | F, I | Menguatkan §4.d — pola modal bisa dipakai |
| Ekspor Laporan, Live Sync badge | G | Open |
| KPI Sektor (Gas/Minyak) + chart tren + metrik table | G | Open — redesign `Home.razor` |

## 7. Urutan penerapan (saat disetujui)

1. **Phase 3**: pola kartu sales area + badge status + modal Update Data Harian (segmented) + halaman `GudangWilayah`/`DetailSalesArea` (style dari desain, data 7 wilayah).
2. **Phase 4**: TSO wizard 4 langkah + Draft Proposal (konfirmasi §6 dulu).
3. **Phase 5**: dashboard redesain (Sektor KPI + chart tren + Ekspor Laporan).
