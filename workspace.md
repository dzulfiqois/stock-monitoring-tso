```mermaid
graph TD
A[Halaman Login] -->|Proses Login| B[Halaman Dashboard]
B -->|Sidebar: Transport Shipping Order| C[Halaman Transport Shipping Order]
C --> D[Mengisi Mitra TSO]
D --> E[Memilih Jenis Material]
E --> F[Mengisi Kuantitas Material yang akan Dikirim]
F --> G[Mengisi Tanggal Keberangkatan Pengiriman]
G --> |Submit|H[Halaman Preview Invoice Pengiriman]
H --> |Tombol: Generate Draft Invoice|I[Menggenerate Draft Invoice Pengiriman]
```
