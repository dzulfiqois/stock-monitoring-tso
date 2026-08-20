# DOKUMEN SPESIFIKASI FITUR MONITORING STOK MINYAK TANAH DAN LPG (LIQUEFIED PETROLEUM GAS) PADA APLIKASI STOCK MONITOR DAN TSO

## 1. Latar Belakang

**Panggih** adalah seorang **leader** dari tim marketing pada perusahaan **oil and gas**, sejenis dengan PT. PERTAMINA (Persero). Panggih bertanggung jawab atas distribusi stok minyak tanah dan LPG untuk daerah Indonesia Timur. Pada kesehariannya, dia harus bisa mengetahui angka dan status ketersediaan stok LPG dan minyak tanah, baik saat pengiriman stok dari pusat menuju gudang daerahnya, maupun distribusi dari gudang daerah menuju tiap agen, dan outlet yang tersebar di tiap daerah di Papua. Namun, Panggih seringkali mengalami kendala untuk mengerti angka pasti dari ketersediaan stok pada tiap agen dan outlet, yang menyulitkan dia untuk menentukan keputusan apakah dia perlu melakukan permintaan pesanan kepada pusat.

Oleh karena itu, dia membentuk sebuah tim monitoring agent yang bertugas untuk memberikan informasi aktual tentang ketersediaan stok LPG dan minyak tanah pada daerah wilayah kerjanya. Nantinya, informasi dari tiap monitoring agent akan dikompilasi menjadi sebuah **dashboard** yang bisa menyelesaikan permasalahan Panggih dan memudahkannya untuk menentukan strategi serta memberikan arahan pada tim marketingnya.

## 2. Definisi Fitur

Dashboard tersebut akan menampilkan informasi mengenai dua objek stok, yakni LPG dan minyak tanah. Setiap objek memiliki metode matrikulasi yang sedikit berbeda. Berikut adalah contoh tabel yang akan ditampilkan pada dashboard:

### a. Tabel Informasi Minyak Tanah

| No. | Nama Sales Area | Realisasi Tanggal               | Sisa Stok Agen | Sisa Stok di Outlet/Pangkalan | Stok Habis Terjual | Stok Intransit | Keterangan |
| --- | --------------- | ------------------------------- | -------------- | ----------------------------- | ------------------ | -------------- | ---------- |
| 1.  | Papua           | 5 Agustus 2026 - 5 Agustus 2026 | 0.5 Kiloliter  | 0.2 Kiloliter                 | 0.3 Kiloliter      | 0 Kiloliter    |            |

> **Realisasi Tanggal (same-day):** notasi "5 Agustus 2026 - 5 Agustus 2026" berarti tanggal permintaan pasokan = tanggal kedatangan pasokan. Ketika agen meminta pasokan pada tanggal X, pasokan tersebut harus dan pasti sampai pada tanggal X juga. Kolom ini setara dengan **Tanggal Stok Awal** pada §2.c (ditulis sebagai tanggal tunggal di form, lihat §4.c).

Hirarki distribusi minyak tanah dibagi menjadi empat bagian transit, yakni Pusat, Gudang Wilayah, Agen, dan Outlet/Pangkalan. **Gudang Wilayah** adalah warehouse/depo utama per region (misalnya region Papua memiliki gudang wilayah sebagai depo utamanya; disebut juga "Warehouse" pada dokumen sebelumnya). Pada tanggal 5 Agustus 2026, Sales Area Papua memiliki supply minyak tanah 1 Kiloliter.

### b. Tabel Informasi LPG

| No. | Nama Sales Area | Stok LPG di Gudang Agen                                   | LPG ukuran 5.5kg | LPG ukuran 12kg | LPG ukuran 50kg | Total Stok  | Daily Objective Throughput                                            | Covered Days | Status Stok<br />(Aman/Warning/Kritis) | Next Supply     |
| --- | --------------- | --------------------------------------------------------- | ---------------- | --------------- | --------------- | ----------- | --------------------------------------------------------------------- | ------------ | -------------------------------------- | --------------- |
| 1.  | Papua           | 200 Tabung 5.5kg<br />150 Tabung 12kg<br />50 Tabung 50kg | 300 Tabung       | 250 Tabung      | 50 Tabung       | 1000 Tabung | 20 Tabung 5.5kg/hari<br />15 Tabung 12kg/hari<br />5 Tabung 50kg/hari | 10 Hari      | Aman                                   | 15 Agustus 2026 |

Hirarki distribusi LPG dibagi menjadi empat bagian transit, yakni Pusat, Gudang Wilayah, Agen, dan Outlet. **Gudang Wilayah** merujuk pada tier yang sama dengan "Warehouse" pada minyak tanah (lihat §2.a), yaitu warehouse/depo utama per region. Pada tanggal 5 Agustus 2026, Gudang Agen Papua memiliki 400 tabung LPG dengan rincian: 200 tabung ukuran 5.5kg, 150 tabung ukuran 12kg, dan 50 tabung ukuran 50kg, sementara stok di tier Outlet berjumlah 600 tabung dengan rincian: 300 tabung ukuran 5.5kg, 250 tabung ukuran 12kg, dan 50 tabung ukuran 50kg, sehingga Total Stok wilayah Papua adalah 1000 tabung. Daya beli masyarakat daerah tersebut mencapai: 20 tabung 5.5kg per hari, 15 tabung 12kg per hari, dan 5 tabung 50kg per hari, sehingga dari stok LPG di Gudang Agen, Covered Days (Ketahanan Stok) dihitung **per ukuran** dan untuk tiap ukuran berada di angka 10 hari (200/20, 150/15, dan 50/5). Setelah 10 hari dari 5 Agustus 2026, Next Supply (Supply LPG berikutnya dari pusat) sudah harus sampai di Gudang Wilayah Papua. Tiap hari, angka pada tabel bisa berubah, tergantung dinamika penjualan di daerah tersebut. **CD, Status, Exhaust Date, dan Next Supply ditampilkan per ukuran (5.5kg/12kg/50kg), tidak digabung menjadi satu angka** (lihat §2.c).

### c. Model Perhitungan

Model perhitungan berlaku untuk LPG maupun minyak tanah, dan mengacu pada dokumen operasional `Monitoring Tabung RPM(1).xlsx` (sheet "Stok Tabung", "Agen", dan "Outlet") sebagai sumber kebenaran komputasi.

#### Granularitas Data

- Setiap baris data stok merepresentasikan satu entitas **(Wilayah × Produk × Tier)**.
- **Hirarki distribusi kanonik**: `Pusat → Gudang Wilayah → Agen → Outlet`. "Gudang Wilayah" adalah warehouse/depo utama per region (sinonim: "Warehouse" pada minyak tanah, "Gudang Sales Area" pada LPG — ketiganya merujuk tier yang sama).
- **Distribusi intra-region (Gudang Wilayah → Agen → Outlet) berlaku same-day**: ketika agen meminta pasokan pada tanggal X, pasokan harus dan pasti sampai pada tanggal X juga (lihat §2.a). Leg Pusat → Gudang Wilayah (TSO) tetap memiliki lead time variabel (lihat §5 G8).
- Wilayah kanonik (7): **Maluku, Papua Barat, Papua Barat Daya, Maluku Utara, Papua Tengah, Papua Selatan-Pegunungan, Papua**.
- Produk:
  - **LPG**: 3 SKU — Tabung 5.5kg, 12kg, dan 50kg. Satuan: Tabung (Pcs).
  - **Minyak tanah**: 1 SKU. Satuan kanonik: Kiloliter (1 Kiloliter = 1000 Liter).
- Tier **Agen** dan **Outlet** adalah entitas stok yang terpisah; masing-masing memiliki snapshot, DOT, CD, dan Rencana Kedatangan sendiri. CD/Status/Next Supply tidak dilipat menjadi satu angka per wilayah, melainkan dihitung per entitas.
- **Agen adalah entitas bernama** (identitas: `Agen {Id, Nama, Wilayah}`). Satu Gudang Wilayah memayungi **2–3 agen**; stok agen dilacak pada granularitas **(Agen × Produk)** (baris `Tier.Agen` + `AgenId`), sedangkan Gudang Wilayah/Outlet pada granularitas **(Wilayah × Produk × Tier)**. Identitas agen dibuat/diubah oleh Superadmin+Supervisi (lihat §3 amend).
- **Mock data 2026-08**: stok awal seluruh agen per (Wilayah × Produk) = **50% stok Gudang Wilayah**, dibagi rata ke tiap agen (sisa ke agen terakhir); DOT gudang dibagi rata juga. Stok gudang di-debit sebesar jumlah yang dialihkan (konservasi terjaga), tiap pengalihan dicatat sebagai transaksi `Transfer`.

#### Invarian Konservasi Stok

Jumlah stok yang tersedia pada tiap tier tidak boleh memiliki selisih sedikitpun antar kolom maupun antar tier dalam satu wilayah. Setiap perpindahan stok men-debit sumber dan meng-kredit tujuan secara otomatis **saat proses keberangkatan/muat dimulai**, bukan saat barang tiba. Contoh: gudang wilayah memiliki 1 Kiloliter minyak tanah pada pukul 08.00; pukul 09.00 agen meminta pasokan 0.5 Kiloliter; saat stok diberangkatkan, sisa stok gudang otomatis menjadi 0.5 Kiloliter.

#### Rumus

| Metrik | Rumus | Keterangan |
| --- | --- | --- |
| CD / Coverage Days (hari) | `Stok ÷ DOT` | Dihitung per (Wilayah × Produk × Tier): stok tier Agen untuk view Agen, stok tier Outlet untuk view Outlet. Menyatakan berapa hari stok saat ini mampu memenuhi kebutuhan wilayah sebelum habis. |
| Exhaust Date | `Tanggal Stok Awal + CD` | Tanggal ketika stok diperkirakan habis. |
| CD setelah Rencana Kedatangan ke-n (CD_n) | `(Sisa stok saat ETA_n + Next Supply_n) ÷ DOT` | Sisa stok saat ETA_n = `Stok − DOT × (ETA_n − Tanggal Stok Awal)`. **Jangan** meniru rumus CD_n pada Excel acuan (`Next Supply ÷ Σ CD`) — rumus tersebut salah secara dimensi dan tidak konsisten antar barisnya. |
| Exhaust Date ke-n | `ETA_n + CD_n` | Mengikuti pola Excel acuan (bagian ini valid). |
| MT (khusus LPG) | `Tabung × berat ukuran (5.5 / 12 / 50 kg) ÷ 1000` | Konversi tabung ke metrik ton. |
| Total (MT) per Wilayah | `Σ MT seluruh produk wilayah` | Ditampilkan pada baris pertama tiap wilayah. |

#### Status Stok

Status diturunkan otomatis dari CD dengan ambang berikut (berlaku untuk LPG dan minyak tanah):

| Status | Kondisi CD |
| --- | --- |
| Kritis | CD < 3 hari |
| Warning | 3 ≤ CD < 7 hari |
| Aman | CD ≥ 7 hari |

#### Rencana Kedatangan

Setiap entitas dapat memiliki hingga **3 slot Rencana Kedatangan** (pasokan berikutnya dari pusat), masing-masing terdiri dari:

1. **Next Supply** (Tabung untuk LPG; Kiloliter untuk minyak tanah) — kuantitas yang akan dikirim.
2. **ETA** — tanggal perkiraan tiba.
3. **CD_n** — dihitung otomatis (rumus di atas).
4. **Exhaust Date_n** — dihitung otomatis.

#### Ringkasan Dashboard

Bagian atas dashboard menampilkan kartu ringkasan:

- Total stok seluruh wilayah (Σ stok semua entitas).
- Jumlah produk kritis (entitas dengan CD < 3).
- Exhaust terdekat (`MIN(Exhaust Date)` lintas seluruh entitas).

## 3. Definisi Role

Dari dua tabel yang berada di dashboard ini, ada empat pihak yang memiliki otoritas untuk melakukan transaksi data:

| Pihak      | Otoritas                                 | Keterangan                                                 |
| ---------- | ---------------------------------------- | ---------------------------------------------------------- |
| Superadmin | Create<br />Read<br />Update<br />Delete | Webmaster                                                  |
| Operator   | Create<br />Read                         | Melakukan pengisian data setiap hari                       |
| Supervisi  | Read<br />Update                         | Melakukan pengecekan dan penggantian data jika diperlukan |
| Tamu       | Read                                     | Melakukan pemantauan data                                  |

> **Amend 2026-08 (identitas Agen):** pembuatan (**Create**) dan perubahan (**Update**) identitas **Agen**
> (party/agency bernama, lihat §2.c) dilakukan oleh **Superadmin + Supervisi**; penghapusan tetap
> **Superadmin only** (konsisten aturan Delete global). Pembuatan identitas Agen otomatis membuat baris
> stok per produk (stok 0, DOT 0); angka stok tetap diisi lewat transaksi stok (Create stok = Superadmin + Operator).

## 4. Alur Interaksi pada Aplikasi Monitoring Stok Minyak Tanah dan LPG

Pada pendefinisian alur interaksi, kedepannya, aktor akan disebut sebagai **pihak**. Alur interaksi dalam pengoperasian dashboard ini adalah sebagai berikut:

#### a. Halaman Login (Login Page)

Pihak memasukkan alamat aplikasi pada browser, kemudian tiba pada halaman login page. Pihak kemudian memasukkan kredensial yang dimiliki.

#### b. Halaman Dashboard Monitoring Stok

Setelah proses login sukses, pihak akan tiba pada Halaman Monitoring Stok. Disini:

1. Semua pihak bisa melihat tabel informasi stok minyak tanah dan LPG.
2. Pihak Superadmin dan Operator bisa melihat serta mengakses tombol Register Sales Area.
3. Tombol Update pada tiap baris Sales Area dapat diakses oleh Superadmin dan Supervisi (lihat §4.d); Operator (Create/Read) dan Tamu (Read) tidak melihat tombol Update. Supervisi tidak melihat tombol Register Sales Area.

#### c. Halaman Register Sales Area

Halaman ini hanya bisa diakses oleh Superadmin dan Operator. Pada halaman ini, terdapat form yang harus diisi agar proses registrasi Sales Area berhasil, yang meliputi:

1. Pihak Superadmin dan Operator melakukan pemilihan dropdown objek stok.

Pihak Superadmin ataupun Operator harus memilih satu dari dua jenis objek stok, antara minyak tanah atau LPG.

Setelah dropdown terisi, akan muncul dua alur yang mengikuti dari kolom pada tabel masing-masing objek stok.

- i. Jika memilih minyak tanah

Nantinya, form Registrasi Sales Area akan meliputi:

| Nama Field                       | Tipe Data | Keterangan                                                                                        | Contoh             |
| -------------------------------- | --------- | ------------------------------------------------------------------------------------------------- | ------------------ |
| Nama Sales Area                  | Enum      | Menentukan nama sales area (7 wilayah kanonik, lihat §2.c)                                        | Maluku             |
| Realisasi Tanggal (Tanggal Stok Awal) | Date | Menentukan tanggal tunggal snapshot stok; permintaan pasokan = tanggal kedatangan (same-day, lihat §2.a) | 5 Agustus 2026 |
| Sisa Stok Agen                   | Decimal   | Menentukan jumlah sisa stok minyak tanah yang dimiliki agen (satuan: Kiloliter)                   | 0.5 Kiloliter      |
| Sisa Stok di Outlet/Pangkalan    | Decimal   | Menentukan jumlah sisa stok minyak tanah yang dimiliki outlet/pangkalan (satuan: Kiloliter)       | 0.2 Kiloliter      |
| Stok Habis Terjual               | Decimal   | Menentukan jumlah stok minyak tanah yang telah terjual (satuan: Kiloliter)                        | 0.3 Kiloliter      |
| Stok Intransit                   | Decimal   | Menentukan jumlah stok minyak tanah yang sedang dikirim menuju Gudang Wilayah (satuan: Kiloliter) | 0 Kiloliter     |
| DOT (Daily Objective Throughput) | Decimal   | Menentukan rata-rata penjualan harian (Kiloliter/hari)                                            | 0.1 Kiloliter/hari |
| Keterangan                       | Text      | Menentukan keterangan dari informasi pada tiap situasi                                            |                    |

Field berikut **dihitung otomatis oleh sistem** (tidak diinput manual), mengikuti Model Perhitungan pada §2.c:

- **CD (Coverage Days)** = Sisa Stok Agen ÷ DOT.
- **Exhaust Date** = Tanggal Stok Awal + CD.
- **Status** = Kritis / Warning / Aman, diturunkan dari CD.
- **Rencana Kedatangan** (hingga 3 slot): masing-masing berisi Next Supply (Kiloliter), ETA, CD_n, dan Exhaust Date_n.

- ii. Jika memilih LPG

Nantinya, form Registrasi Sales Area akan meliputi:

| Nama Field                 | Tipe Data | Keterangan                                                    | Contoh         |
| -------------------------- | --------- | ------------------------------------------------------------- | -------------- |
| Nama Sales Area            | Enum      | Menentukan nama sales area (7 wilayah kanonik, lihat §2.c)    | Maluku         |
| Realisasi Tanggal (Tanggal Stok Awal) | Date | Menentukan tanggal tunggal snapshot stok; permintaan pasokan = tanggal kedatangan (same-day, lihat §2.a) | 5 Agustus 2026 |
| Stok Agen — Tabung 5.5kg   | Integer   | Menentukan jumlah stok LPG 5.5kg di tier Agen                 | 200 Tabung     |
| Stok Agen — Tabung 12kg    | Integer   | Menentukan jumlah stok LPG 12kg di tier Agen                  | 150 Tabung     |
| Stok Agen — Tabung 50kg    | Integer   | Menentukan jumlah stok LPG 50kg di tier Agen                  | 50 Tabung      |
| Stok Outlet — Tabung 5.5kg | Integer   | Menentukan jumlah stok LPG 5.5kg di tier Outlet               | 300 Tabung     |
| Stok Outlet — Tabung 12kg  | Integer   | Menentukan jumlah stok LPG 12kg di tier Outlet                | 250 Tabung     |
| Stok Outlet — Tabung 50kg  | Integer   | Menentukan jumlah stok LPG 50kg di tier Outlet                | 50 Tabung      |
| DOT — Tabung 5.5kg         | Integer   | Menentukan rata-rata penjualan harian LPG 5.5kg (Tabung/hari) | 20 Tabung/hari |
| DOT — Tabung 12kg          | Integer   | Menentukan rata-rata penjualan harian LPG 12kg (Tabung/hari)  | 15 Tabung/hari |
| DOT — Tabung 50kg          | Integer   | Menentukan rata-rata penjualan harian LPG 50kg (Tabung/hari)  | 5 Tabung/hari  |

Field berikut **dihitung otomatis oleh sistem** (tidak diinput manual), per (Wilayah × Ukuran × Tier), mengikuti Model Perhitungan pada §2.c:

- **Total Stok** = Σ stok seluruh ukuran dan tier.
- **Total (MT)** = Σ MT seluruh ukuran (konversi: Tabung × berat ukuran ÷ 1000).
- **CD (Coverage Days)** = Stok Agen ÷ DOT, per ukuran.
- **Exhaust Date** = Tanggal Stok Awal + CD.
- **Status** = Kritis / Warning / Aman, diturunkan dari CD (Kritis: CD < 3; Warning: 3 ≤ CD < 7; Aman: CD ≥ 7).
- **Rencana Kedatangan** (hingga 3 slot): masing-masing berisi Next Supply (Tabung), ETA, CD_n, dan Exhaust Date_n.

2. Pihak Superadmin dan Operator menekan tombol submit.
3. Pihak Superadmin dan Operator kembali ke halaman dashboard dengan data Sales Area baru yang sudah muncul pada tabel.

#### d. Halaman Update Sales Area

Halaman ini hanya bisa diakses oleh **Superadmin** dan **Supervisi** (role dengan otoritas Update, lihat §3). Operator (Create/Read) dan Tamu (Read) tidak melihat tombol Update.

1. Pihak membuka tombol Update pada baris entitas (Wilayah × Produk × Tier) yang dituju.
2. Form menampilkan field yang sama dengan Halaman Register Sales Area (§4.c) sesuai objek stok, dengan nilai terisi (pre-filled) dari entitas yang dituju.
3. Pihak mengubah data, lalu menekan tombol submit.
4. Sistem menghitung ulang CD, Exhaust Date, Status, dan Rencana Kedatangan secara otomatis.
5. Perubahan angka stok tunduk pada Invarian Konservasi Stok (§2.c); koreksi stok dicatat sebagai transaksi di audit log (pihak, waktu, role, nilai sebelum/sesudah).

#### e. Mekanisme Hapus Sales AreaPenghapusan data hanya dapat dilakukan oleh **Superadmin** (satu-satunya role dengan otoritas Delete, lihat §3).

1. Superadmin menekan tombol hapus pada baris entitas yang dituju.
2. Muncul modal konfirmasi berisi ringkasan entitas (Wilayah, Produk, Tier, dan nilai stok terkini).
3. Setelah konfirmasi, entitas dan snapshot terkait dihapus (soft delete).
4. Riwayat transaksi stok entitas tersebut tetap tersimpan di audit log untuk keperluan pelacakan.
5. Role selain Superadmin tidak melihat tombol hapus.

#### f. Transfer Gudang Wilayah → Agen (2026-08)

Tersedia bagi **Superadmin + Supervisi** (sama dengan otoritas Update, lihat §3).

1. Pihak membuka tombol **"Kirim ke Agen"** pada halaman Detail Gudang Wilayah.
2. Memilih **satu agen** tujuan (daftar agen aktif di wilayah tersebut).
3. Mengisi **kuantitas per jenis material dalam satu modal**: untuk LPG sekaligus ketiga ukuran (Tabung 5.5kg, 12kg, 50kg); untuk Minyak Tanah satu input (Kiloliter).
4. Menekan Kirim → untuk tiap SKU dengan kuantitas > 0, sistem menjalankan transaksi `Transfer` atomic: **stok Gudang Wilayah ter-debit, stok agen ter-kredit** (Invarian Konservasi §2.c), tiap pengalihan tercatat di log transaksi & audit log.
5. **Overdraft ditolak** (G3/F4): bila kuantitas suatu SKU melebihi stok gudang SKU tsb, transaksi ditolak dengan "stok tidak mencukupi"; stok tidak berubah.
6. Agen tujuan harus berada di Gudang Wilayah asal (transfer lintas wilayah ditolak).

## 5. Guardrails dan Fallbacks

Bagian ini merangkum aturan pencegahan (guardrails) dan perilaku reaktif (fallback) untuk menjaga kebenaran data stok, dengan mengacu pada praktik inventory management (safety stock, reorder point, dan pencegahan stockout) serta invarian pada §2.c.

### 5.1 Guardrails (Pencegahan)

| No. | Guardrail | Ketentuan |
| --- | --- | --- |
| G1 | Single source of truth untuk stok | Angka stok hanya boleh berubah melalui transaksi stok (penerimaan, pengeluaran, transfer antar-tier), bukan pengeditan langsung. Setiap perubahan tercatat di audit log (pihak, waktu, role, nilai sebelum/sesudah). |
| G2 | Satuan kanonik anti-campur | LPG selalu dalam Tabung (Pcs); minyak tanah selalu dalam Kiloliter. Validasi input menolak satuan silang. |
| G3 | Stok non-negatif | Sistem menolak transaksi yang akan membuat stok suatu tier menjadi negatif (overdraft), dengan notifikasi "stok tidak mencukupi". |
| G4 | DOT wajib > 0 | CD hanya dihitung bila DOT > 0. Bila DOT = 0, CD tidak dihitung (lihat F2). |
| G5 | Ambang status sebagai reorder point | Ambang CD (Kritis < 3, Warning < 7, Aman ≥ 7) berfungsi sebagai reorder point: saat CD < 3, sistem menandai entitas wajib segera membuat Rencana Kedatangan / permintaan pasokan. |
| G6 | CD dihitung dari stok aktual | CD dihitung dari stok snapshot aktual, bukan proyeksi. DOT adalah laju (rate) yang dapat diperbarui berkala; CD dihitung ulang setiap ada snapshot/transaksi baru. |
| G7 | Identifikasi produk kanonik | Produk dibatasi pada enum master: LPG (Tabung 5.5kg, 12kg, 50kg) dan Minyak Tanah. Pelacakan selalu pada granularitas (Wilayah × Produk × Tier). |
| G8 | Lead time diperlakukan sebagai estimasi | ETA Rencana Kedatangan adalah estimasi karena lead time Pusat → Wilayah (laut/udara ke Papua/Maluku) sangat variabel. ETA ditampilkan sebagai perkiraan, bukan jaminan. |
| G9 | Idempotensi dan konkurensi | Submit ganda dicegah; penulisan stok bersifat atomic. Konflik edit simultan ditangani dengan optimistic concurrency (lihat F9). |
| G10 | Otorisasi berbasis role | Setiap aksi mutasi data memeriksa role pihak (Superadmin/Operator/Supervisi/Tamu) sesuai §3. Tidak ada rahasia di kode sumber; query selalu terparameterisasi. |
| G11 | Validasi tanggal | Tanggal Stok Awal tidak boleh di masa depan; ETA tidak boleh mendahului Tanggal Stok Awal. |

### 5.2 Fallbacks (Skenario Reaktif)

- ### F1 — Invalid Credentials

  Ketika ada pihak yang hendak melakukan proses login, namun tidak memberikan kredensial yang benar, sebuah notifikasi yang menginformasikan bahwa kredensial yang dimasukkan salah akan muncul.
- ### F2 — Improper Credentials

  Ketika ada pihak yang melakukan proses login, namun tidak mengisikan kredensial secara lengkap, maka sebuah notifikasi peringatan (form validation) akan muncul pada form kredensial yang bermasalah.
- ### F3 — DOT bernilai 0

  Bila DOT = 0, CD dan Status tidak dihitung; kolom CD menampilkan "N/A" dan Status menampilkan "Tidak Dihitung". Sistem tidak mengalami error.
- ### F4 — Transaksi melebihi stok tersedia

  Bila suatu transaksi pengeluaran/transfer akan membuat stok tier menjadi negatif, transaksi ditolak dan muncul notifikasi "stok tidak mencukupi"; angka stok tetap pada nilai sebelumnya.
- ### F5 — Rencana Kedatangan terlambat (ETA terlewati)

  Bila ETA sudah lewat namun pasokan belum tercatat tiba, sistem menghitung ulang CD tanpa mengasumsikan pasokan tersebut, menandai entitas "Terlambat", dan melakukan eskalasi peringatan.
- ### F6 — Snapshot usang

  Bila Tanggal Stok Awal terakhir lebih tua dari ambang tertentu (mis. > N hari), dashboard menampilkan indikator "data usang" beserta tanggal snapshot terakhir.
- ### F7 — Data tidak konsisten setelah Exhaust Date

  Bila Exhaust Date sudah terlewati namun stok masih tercatat ada, sistem menandai "data tidak konsisten / perlu diperbarui".
- ### F8 — Stockout aktual

  Bila stok suatu entitas mencapai 0, Status otomatis menjadi "Kritis"/"Habis" dan sistem menyarankan pembuatan permintaan pasokan.
- ### F9 — Konflik edit simultan

  Bila dua pihak mengubah entitas yang sama secara bersamaan, penulisan kedua ditolak dengan pesan "data telah diperbarui pihak lain, muat ulang" (optimistic concurrency).
- ### F10 — Kegagalan di tengah transfer

  Bila terjadi kegagalan di tengah proses transfer antar-tier, transaksi di-rollback secara atomic sehingga invarian konservasi stok (§2.c) tetap terjaga.
- ### F11 — Konversi MT LPG gagal

  Bila berat ukuran LPG tidak terdefinisi, kolom MT tidak ditampilkan (nilai Pcs tetap ditampilkan) tanpa menyebabkan error.
- ### F12 — Wilayah atau produk tidak terdaftar

  Bila input merujuk Wilayah/Produk di luar enum master, sistem menolak dengan respons 400 (ProblemDetails), bukan error server.
- ### F13 — Gagal memuat dashboard

  Bila layanan data tidak tersedia, dashboard menampilkan snapshot terakhir yang tersimpan dengan indikator "mode offline".
- ### F14 — Lead time sangat variabel

  Bila lead time sangat tidak menentu, ETA ditampilkan sebagai rentang (paling awal/paling lambat) atau dengan penanda "estimasi".

## 6. Otentikasi, Otorisasi, dan Sesi

Bagian ini berlaku lintas modul (Monitoring Stok dan Transport Shipping Order) pada aplikasi Stock Monitor dan TSO.

### 6.1 Login

1. Pihak memasukkan alamat aplikasi pada browser dan tiba pada halaman login.
2. Pihak memasukkan kredensial yang dimiliki (lihat F1 dan F2 pada §5.2 untuk penanganan kredensial salah/lengkap).
3. Bila user memiliki lebih dari satu role, pihak memilih **satu role aktif** saat login sebelum masuk ke dashboard.

### 6.2 Multi-role (switchable active role)

1. Satu user dapat memiliki lebih dari satu role; penetapan dan perubahan role hanya dapat dilakukan oleh **Superadmin**.
2. Saat login, user memilih satu role aktif. Hak akses yang berlaku adalah hak akses dari **role aktif tersebut** (bukan gabungan seluruh role).
3. User dapat berpindah role aktif selama sesi berlangsung; hak akses mengikuti role yang sedang aktif.
4. Hak akses per modul tetap mengikuti Definisi Role masing-masing modul (§3 pada dokumen ini untuk Monitoring Stok; §3 pada `TRANSPORT_SHIPPING_ORDER_SPEC.md` untuk TSO).

### 6.3 Manajemen role dan password

1. **Assign role** (menetapkan/mengubah role user, termasuk multi-role) hanya dapat dilakukan oleh **Superadmin**.
2. **Ganti password** (termasuk password user lain maupun password sendiri) hanya dapat dilakukan oleh **Superadmin**. Tidak ada mekanisme ganti password mandiri (self-service) bagi role lain.
3. Seluruh aksi manajemen role/password tercatat di audit log.

### 6.4 Logout

1. Terdapat mekanisme **logout eksplisit** pada aplikasi (tombol logout di dashboard).
2. Logout menutup sesi pengguna dan membatalkan seluruh hak akses yang sedang aktif; user diarahkan kembali ke halaman login.

### 6.5 Session expiry

1. Sesi berakhir secara otomatis setelah **15 menit tanpa aktivitas** (idle timeout).
2. Setiap permintaan yang sah dari user mereset timer idle.
3. Setelah sesi hangus, user diarahkan kembali ke halaman login; data yang belum tersimpan pada sesi tersebut hilang.

---
