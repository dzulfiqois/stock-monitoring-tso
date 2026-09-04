# 05 — Application Flowchart (User Journey)

One diagram for the whole click-path: login, dashboard, every sidebar branch, every
modal, every guard. Diamonds reject and loop back; nothing is lost silently.

```mermaid
flowchart TB
    L1["Login page"]
    L2{"credentials valid and complete?"}
    L3["Pick active role (multi-role users)"]
    DASH["Dashboard - Ringkasan Operasional (sector KPIs, chart, critical outlets)"]

    L1 --> L2
    L2 -- "no - notice" --> L1
    L2 -- "yes" --> L3 --> DASH

    DASH --> GW["Gudang Wilayah - filter obyek, sales-area cards"]
    DASH --> REG["Register Sales Area"]
    DASH --> TSO["TSO order list"]
    DASH --> ADM["Manajemen User (Superadmin)"]
    DASH --> SW["Switch active role"]
    DASH --> OUT["Logout"]
    IDLE["15 min idle"] --> L1
    DASH -. "no activity" .-> IDLE
    SW --> DASH
    OUT --> L1

    GW --> CARD["Pick sales-area card"]
    CARD --> DET["Detail Sales Area - KPIs, tier table, transaction log"]
    DET --> UPD["Modal Update Data Harian"]
    DET --> KAG["Modal Kirim ke Agen - pick 1 agen, qty per SKU"]
    DET --> LA["Daftar Agen"]
    UPD --> DET
    KAG --> DET

    REG --> BR{"minyak tanah or LPG?"}
    BR -->|"minyak - 2 rows"| SUBMIT1["Submit"]
    BR -->|"LPG - 6 rows"| SUBMIT1
    SUBMIT1 --> DASH

    LA --> CA["Modal Tambah or Edit Agen"]
    LA --> DA["Detail Agen - KPIs, product table, log"]
    CA --> LA
    DA --> UPA["Modal Update per ukuran"]
    DA --> KAO["Modal Kirim ke Outlet - pick 1 outlet, qty per SKU"]
    DA --> LO["Daftar Outlet"]
    UPA --> DA
    KAO --> DA
    LO --> CO["Modal Tambah or Edit Outlet"]
    LO --> DOU["Detail Outlet - KPIs, product table, log"]
    CO --> LO
    DOU --> UPO["Modal Update per ukuran"]
    UPO --> DOU

    TSO --> WIZ["Wizard step 1 - Tujuan and Obyek (destination + product + qty)"]
    WIZ --> W2["Wizard step 2 - Rute and Jadwal (Pusat to Gudang, departure date)"]
    W2 --> W3["Wizard step 3 - Transporter + Estimasi Biaya (tariff x qty)"]
    W3 --> W4["Wizard step 4 - Ringkasan"]
    W4 --> SUBMIT2{"Submit - guards pass?"}
    SUBMIT2 -- "no - validation message" --> WIZ
    SUBMIT2 -- "yes - order committed" --> PREV["Preview (read-only)"]
    PREV --> GEN["Generate Draft Invoice (PDF download)"]
    GEN --> PREV
    TSO --> EDIT["Edit order (pre-filled wizard)"]
    EDIT --> SUBMIT2

    ADM --> CU["Modal Tambah User (email + password + roles + active role)"]
    CU --> ADM
```

## Branch notes

- **Gudang Wilayah:** the LPG card groups all three sizes of one wilayah (pink 5.5kg,
  blue 12kg, orange 50kg chips); the minyak card shows Agen remainder, Outlet
  remainder, sold, in-transit, and notes. Delete is Superadmin-only behind a confirm
  modal and only flags the row deleted — the audit trail stays.
- **Update Data Harian:** one row per size (three for LPG, one for minyak):
  `Terjual` records a sale, ticked `Opname` records a physical-count correction
  (plus or minus), `Intransit` and `Keterangan` are metadata.
- **Kirim ke Agen / Kirim ke Outlet:** runs one atomic transfer per SKU with qty
  above zero. Any SKU exceeding the source balance rejects the whole transfer.
- **TSO wizard:** product choice is a single SKU (one LPG size or minyak tanah).
  Step 3 shows `Estimasi Biaya = tariff × quantity` from the Mitra master.
  Submit commits the order; Preview and Generate never change data.
- **Manajemen User:** Superadmin creates the account with an initial password and
  assigns at least one role plus the starting active role.

## Guard points (diamonds in the diagram)

| Point | Rejection |
|---|---|
| Login | Wrong credentials or empty fields loop back with a notice. |
| Register / Update | Duplicate (Wilayah × Produk × Tier), unknown wilayah/product (400-style error), snapshot date in the future, ETA before snapshot date. |
| Transfers and daily updates | Overdraft ("stok tidak mencukupi"), cross-wilayah or cross-agen destination, non-positive qty (opname must not be 0). |
| TSO Submit | Unregistered mitra, mitra not covering the destination, qty not above zero, departure before today, duplicate submit within 1 minute (returns the existing order instead), arrival slots already full (3), stale edit (concurrency conflict asks for reload). |
| Session | Actions without the required active role are refused with a notice. |
