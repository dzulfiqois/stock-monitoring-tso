# 05 — Diagram Alir Aplikasi (Perjalanan Pengguna)

Satu diagram untuk seluruh jalur klik: login, dashboard, setiap cabang sidebar, setiap
modal, setiap penjaga. Belah ketupat menolak dan kembali; tidak ada yang hilang diam-diam.

```mermaid
flowchart TB
    L1["Halaman login"]
    L2{"kredensial valid dan lengkap?"}
    L3["Pilih role aktif (pengguna multi-role)"]
    DASH["Dashboard - Ringkasan Operasional (KPI sektor, chart, outlet kritis)"]

    L1 --> L2
    L2 -- "tidak - notifikasi" --> L1
    L2 -- "ya" --> L3 --> DASH

    DASH --> GW["Gudang Wilayah - filter obyek, kartu sales area"]
    DASH --> REG["Register Sales Area"]
    DASH --> TSO["Daftar order TSO"]
    DASH --> ADM["Manajemen User (Superadmin)"]
    DASH --> SW["Ganti role aktif"]
    DASH --> OUT["Logout"]
    IDLE["Idle 15 menit"] --> L1
    DASH -. "tanpa aktivitas" .-> IDLE
    SW --> DASH
    OUT --> L1

    GW --> CARD["Pilih kartu sales area"]
    CARD --> DET["Detail Sales Area - KPI, tabel tier, log transaksi"]
    DET --> UPD["Modal Update Data Harian"]
    DET --> KAG["Modal Kirim ke Agen - pilih 1 agen, qty per SKU"]
    DET --> LA["Daftar Agen"]
    UPD --> DET
    KAG --> DET

    REG --> BR{"minyak tanah atau LPG?"}
    BR -->|"minyak - 2 baris"| SUBMIT1["Submit"]
    BR -->|"LPG - 6 baris"| SUBMIT1
    SUBMIT1 --> DASH

    LA --> CA["Modal Tambah atau Edit Agen"]
    LA --> DA["Detail Agen - KPI, tabel produk, log"]
    CA --> LA
    DA --> UPA["Modal Update per ukuran"]
    DA --> KAO["Modal Kirim ke Outlet - pilih 1 outlet, qty per SKU"]
    DA --> LO["Daftar Outlet"]
    UPA --> DA
    KAO --> DA
    LO --> CO["Modal Tambah atau Edit Outlet"]
    LO --> DOU["Detail Outlet - KPI, tabel produk, log"]
    CO --> LO
    DOU --> UPO["Modal Update per ukuran"]
    UPO --> DOU

    TSO --> WIZ["Wizard langkah 1 - Tujuan dan Obyek (tujuan + produk + qty)"]
    WIZ --> W2["Wizard langkah 2 - Rute dan Jadwal (Pusat ke Gudang, tanggal berangkat)"]
    W2 --> W3["Wizard langkah 3 - Transporter + Estimasi Biaya (tarif x qty)"]
    W3 --> W4["Wizard langkah 4 - Ringkasan"]
    W4 --> SUBMIT2{"Submit - lolos penjaga?"}
    SUBMIT2 -- "tidak - pesan validasi" --> WIZ
    SUBMIT2 -- "ya - order ter-commit" --> PREV["Preview (read-only)"]
    PREV --> GEN["Generate Draft Invoice (unduh PDF)"]
    GEN --> PREV
    TSO --> EDIT["Edit order (wizard pre-filled)"]
    EDIT --> SUBMIT2

    ADM --> CU["Modal Tambah User (email + password + role + role aktif)"]
    CU --> ADM
```

## Catatan cabang

- **Gudang Wilayah:** kartu LPG mengelompokkan ketiga ukuran satu wilayah (chip pink
  5.5kg, biru 12kg, oranye 50kg); kartu minyak menampilkan sisa Agen, sisa Outlet,
  terjual, intransit, dan keterangan. Hapus hanya Superadmin di balik modal konfirmasi
  dan hanya menandai baris terhapus — jejak audit tetap ada.
- **Update Data Harian:** satu baris per ukuran (tiga untuk LPG, satu untuk minyak):
  `Terjual` mencatat penjualan, `Opname` yang dicentang mencatat koreksi hitung fisik
  (plus atau minus), `Intransit` dan `Keterangan` disimpan sebagai metadata.
- **Kirim ke Agen / Kirim ke Outlet:** menjalankan satu transfer atomik per SKU dengan
  qty di atas nol. SKU mana pun yang melebihi saldo sumber menggagalkan seluruh transfer.
- **Wizard TSO:** pilihan produk adalah satu SKU (satu ukuran LPG atau minyak tanah).
  Langkah 3 menampilkan `Estimasi Biaya = tarif × kuantitas` dari master Mitra.
  Submit meng-commit order; Preview dan Generate tidak mengubah data.
- **Manajemen User:** Superadmin membuat akun dengan password awal serta minimal satu
  role plus role aktif awal.

## Titik penjaga (belah ketupat pada diagram)

| Titik | Penolakan |
|---|---|
| Login | Kredensial salah atau field kosong kembali dengan notifikasi. |
| Registrasi / Update | Duplikat (Wilayah × Produk × Tier), wilayah/produk tak dikenal (error 400), tanggal snapshot di masa depan, ETA mendahului tanggal snapshot. |
| Transfer dan update harian | Overdraft ("stok tidak mencukupi"), tujuan lintas wilayah atau lintas agen, qty tidak positif (opname tidak boleh 0). |
| Submit TSO | Mitra tak terdaftar, mitra tidak meng-cover tujuan, qty tidak di atas nol, berangkat mendahului hari ini, submit ganda dalam 1 menit (mengembalikan order yang sudah ada), slot kedatangan sudah penuh (3), edit basi (konflik konkurensi diminta reload). |
| Sesi | Aksi tanpa role aktif yang berwenang ditolak dengan notifikasi. |
