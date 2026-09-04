# 04 — DFD Level 2 (Uraian Proses)

Setiap proses Level 1 yang membawa logika bisnis diurai menjadi langkah bernomor.
Penjaga yang menolak request digambar sebagai belah ketupat; penolakan tidak mengubah
isi store mana pun.

## 1.0 — Autentikasi dan Sesi

```mermaid
flowchart TB
    A1("1.1 - Validasi kredensial")
    A2{"lengkap dan benar?"}
    A3("1.2 - Muat role pengguna")
    A4("1.3 - Tetapkan role aktif (dipilih saat login, atau role pertama sebagai default)")
    A5("1.4 - Terbitkan kuki, idle geser 15 menit")
    A6("1.5 - Ganti role aktif di tengah sesi (hanya role aktif yang diterbitkan sebagai klaim)")
    A7("1.6 - Idle hangus atau logout eksplisit, kembali ke login")

    A1 --> A2
    A2 -- "tidak - tampil notifikasi" --> A1
    A2 -- "ya" --> A3 --> A4 --> A5
    A5 --> A6 --> A6
    A5 --> A7
    A6 --> A7
```

## 2.0 — Manajemen User dan Role (hanya Superadmin)

```mermaid
flowchart TB
    B1("2.1 - Wajib role aktif Superadmin")
    B2("2.2 - Buat user: email unik, role dikenal, role aktif ada di dalam role terpilih")
    B3("2.3 - Assign atau hapus role (saat hapus, role aktif dialihkan ke role tersisa)")
    B4("2.4 - Ganti password user mana pun")
    B5("2.5 - Tulis baris audit")

    B1 --> B2 --> B5
    B1 --> B3 --> B5
    B1 --> B4 --> B5
```

## 5.0 — Transaksi Stok, Konservasi (Superadmin + Supervisi)

```mermaid
flowchart TB
    C1("5.1 - Wajib Superadmin atau Supervisi")
    C2("5.2 - Validasi qty (Adjust tidak boleh 0, lainnya harus di atas 0) dan satuan kanonik")
    C3("5.3 - Muat baris stok aktif (dan baris tujuan untuk Transfer)")
    C4{"Transfer lintas Wilayah?"}
    C5{"Hasilnya di bawah nol?"}
    C6("5.4 - Terapkan: Receive menambah, Issue mengurangi plus otomatis Terjual, Adjust menambah qty bertanda, Transfer mendebit sumber dan mengkredit tujuan")
    C7("5.5 - Tulis record StockTransaction")
    C8("5.6 - Commit atomik")
    C9("5.7 - Tulis baris audit")

    C1 --> C2 --> C3 --> C4
    C4 -- "ya - tolak" --> C3
    C4 -- "tidak" --> C5
    C5 -- "ya - tolak, stok tidak mencukupi" --> C3
    C5 -- "tidak" --> C6 --> C7 --> C8 --> C9
```

Kegagalan di tengah transfer me-rollback seluruh transaksi, sehingga sumber dan
tujuan tidak pernah selisih.

## 6.0 — Inventaris Agen dan Outlet

```mermaid
flowchart TB
    D1("6.1 - Wajib role: Buat dan Ubah perlu Superadmin atau Supervisi, Hapus perlu Superadmin")
    D2("6.2 - Buat identitas: nama unik dalam scope, lalu otomatis buat satu baris stok nol per produk")
    D3("6.3 - Modal transfer: pilih tepat satu tujuan dalam scope, isi qty per SKU")
    D4("6.4 - Jalankan satu Transfer atomik per SKU dengan qty di atas 0 (proses 5.0)")
    D5("6.5 - Tulis baris audit")

    D1 --> D2 --> D5
    D1 --> D3 --> D4 --> D5
```

Aturan scope: Agen harus berada di bawah Gudang Wilayah-nya sendiri; Outlet harus
milik Agen-nya sendiri — selain itu ditolak. Update Data Harian memetakan tiap baris
ke proses 5.0: `Terjual` menjadi `Issue`, `Opname` yang dicentang menjadi `Adjust`,
`Intransit` dan `Keterangan` disimpan sebagai metadata.

## 7.0 — Order TSO

Buat (Superadmin + Operator). Ubah (Superadmin + Supervisi). Hapus (Superadmin).

```mermaid
flowchart TB
    E1("7.1 - Wajib role sesuai aksi")
    E2("7.2 - Mitra harus aktif dan meng-cover Wilayah tujuan")
    E3("7.3 - Qty di atas 0, satuan kanonik, tanggal berangkat tidak mendahului hari ini")
    E4{"order sama disubmit dalam 1 menit?"}
    E5("7.4 - Kembalikan order yang sudah ada (tanpa duplikat)")
    E6("7.5 - Generate OrderNo, ETA = berangkat + 7 hari, snapshot nama mitra, tarif, dan biaya")
    E7("7.6 - Commit order sebagai Committed")
    E8("7.7 - Tulis Rencana Kedatangan (Next Supply + ETA) pada Gudang Wilayah tujuan, maks 3 slot")
    E9{"tulis rencana kedatangan gagal?"}
    E10("7.8 - Tandai StockImpacted")
    E11("7.8 - Tandai FlagTertunda (dampak stok tertunda), coba lagi belakangan via resync")
    E12("7.9 - Tulis baris audit")

    E1 --> E2 --> E3 --> E4
    E4 -- "ya" --> E5
    E4 -- "tidak" --> E6 --> E7 --> E8 --> E9
    E9 -- "tidak" --> E10 --> E12
    E9 -- "ya" --> E11 --> E12
```

Ubah juga membandingkan token konkurensi dan menolak tulisan basi ("data telah
diperbarui pihak lain, muat ulang"), lalu menghitung ulang snapshot tarif dan biaya.
Hapus adalah soft delete plus audit. Resync mengulang langkah 7.7 untuk order
`FlagTertunda`.

## 8.0 — Draft Invoice

```mermaid
flowchart TB
    F1("8.1 - Muat order (preview menampilkan Mitra, tujuan, material, qty + satuan, keberangkatan, ETA, nomor order, timestamp)")
    F2("8.2 - Render PDF deterministik (tanpa konten acak, tanpa timestamp baru di dalam)")
    F3("8.3 - Cap InvoiceGeneratedAt sekali, tidak pernah ditulis ulang")
    F4("8.4 - Kembalikan byte PDF; generate ulang order yang sama menghasilkan byte identik")

    F1 --> F2 --> F3 --> F4
```

Preview tidak pernah mengubah data; hanya Submit (proses 7.0) yang commit. Bila render
PDF gagal, order yang sudah ter-commit tidak tersentuh dan pengguna tinggal mencoba
lagi.
