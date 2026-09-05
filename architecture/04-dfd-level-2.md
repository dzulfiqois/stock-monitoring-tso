# 04 — DFD Level 2 (Process Explosions)

Each Level 1 process that carries business logic is exploded into numbered steps.
Guards that reject a request are drawn as diamonds; rejection leaves all stores
unchanged.

## 1.0 — Autentikasi dan Sesi (JWT bearer)

```mermaid
flowchart TB
    A1("1.1 - POST /api/auth/login - validate credentials via Identity Core")
    A2{"complete and correct?"}
    A3("1.2 - Load user roles")
    A4("1.3 - Set active role (chosen at login, or first role as default)")
    A5("1.4 - Issue access token (15-min expiry, active-role claim) + refresh token")
    A6("1.5 - POST /api/auth/refresh on activity - new access token, sliding 15-min idle")
    A7("1.6 - POST /api/auth/switch-role - validate membership, re-issue token with the new active-role claim")
    A8("1.7 - POST /api/auth/logout revokes the refresh; an expired access token ends access - back to login")

    A1 --> A2
    A2 -- "no - ProblemDetails" --> A1
    A2 -- "yes" --> A3 --> A4 --> A5
    A5 --> A6
    A6 --> A6
    A6 --> A7
    A7 --> A6
    A5 --> A8
    A6 --> A8
    A7 --> A8
```

## 2.0 — Manajemen User dan Role (Superadmin only)

```mermaid
flowchart TB
    B1("2.1 - Require Superadmin active role")
    B2("2.2 - Create user: email unique, known roles, active role inside chosen roles")
    B3("2.3 - Assign or remove role (on remove, fall back active role to a remaining role)")
    B4("2.4 - Set password for any user")
    B5("2.5 - Append audit row")

    B1 --> B2 --> B5
    B1 --> B3 --> B5
    B1 --> B4 --> B5
```

## 5.0 — Transaksi Stok, Konservasi (Superadmin + Supervisi)

```mermaid
flowchart TB
    C1("5.1 - Require Superadmin or Supervisi")
    C2("5.2 - Validate qty (Adjust must not be 0, others must be above 0) and canonical unit")
    C3("5.3 - Load active stock row (and destination row for Transfer)")
    C4{"Transfer crosses Wilayah?"}
    C5{"Result would go below zero?"}
    C6("5.4 - Apply: Receive adds, Issue subtracts plus auto Terjual, Adjust adds signed qty, Transfer debits source and credits destination")
    C7("5.5 - Write StockTransaction record")
    C8("5.6 - Commit atomically")
    C9("5.7 - Append audit row")

    C1 --> C2 --> C3 --> C4
    C4 -- "yes - reject" --> C3
    C4 -- "no" --> C5
    C5 -- "yes - reject, stok tidak mencukupi" --> C3
    C5 -- "no" --> C6 --> C7 --> C8 --> C9
```

Mid-transfer failure rolls the whole transaction back, so source and destination
never drift apart.

## 6.0 — Inventaris Agen dan Outlet

```mermaid
flowchart TB
    D1("6.1 - Require role: Create and Update need Superadmin or Supervisi, Delete needs Superadmin")
    D2("6.2 - Create identity: unique name in scope, then auto-create one zero stock row per product")
    D3("6.3 - Transfer modal: pick exactly one destination in scope, enter qty per SKU")
    D4("6.4 - Run one atomic Transfer per SKU with qty above 0 (process 5.0)")
    D5("6.5 - Append audit row")

    D1 --> D2 --> D5
    D1 --> D3 --> D4 --> D5
```

Scope rules: an Agen must sit under its own Gudang Wilayah; an Outlet must belong to
its own Agen — anything else is rejected. Update Data Harian maps each row to
process 5.0: `Terjual` becomes `Issue`, ticked `Opname` becomes `Adjust`,
`Intransit` and `Keterangan` are stored as metadata.

## 7.0 — Order TSO

Create (Superadmin + Operator). Update (Superadmin + Supervisi). Delete (Superadmin).

```mermaid
flowchart TB
    E1("7.1 - Require role for the action")
    E2("7.2 - Mitra must be active and cover the destination Wilayah")
    E3("7.3 - Every product line: qty above 0, canonical unit, distance given for per-kilometer tariffs, departure date not before today")
    E4{"same order submitted within 1 minute?"}
    E5("7.4 - Return the existing order (no duplicate)")
    E6("7.5 - Generate OrderNo, ETA = departure + 7 days, snapshot mitra name plus per-product tariff and cost")
    E7("7.6 - Commit order as Committed")
    E8("7.7 - Write Rencana Kedatangan (Next Supply + ETA) per product line on destination Gudang Wilayah, max 3 slots each")
    E9{"arrival-plan write failed?"}
    E10("7.8 - Mark StockImpacted")
    E11("7.8 - Mark FlagTertunda (dampak stok tertunda), retry later via resync")
    E12("7.9 - Append audit row")

    E1 --> E2 --> E3 --> E4
    E4 -- "yes" --> E5
    E4 -- "no" --> E6 --> E7 --> E8 --> E9
    E9 -- "no" --> E10 --> E12
    E9 -- "yes" --> E11 --> E12
```

Update additionally compares the concurrency token and rejects stale writes
("data telah diperbarui pihak lain, muat ulang"), then re-snapshots tariff and cost.
Delete is a soft delete plus audit. Resync re-runs step 7.7 for `FlagTertunda` orders.

## 8.0 — Draft Invoice

```mermaid
flowchart TB
    F1("8.1 - Load order (preview shows Mitra, destination, material, qty + unit, departure, ETA, order no, timestamp)")
    F2("8.2 - Render PDF deterministically (no random content, no fresh timestamps inside)")
    F3("8.3 - Stamp InvoiceGeneratedAt once, never rewrite")
    F4("8.4 - API streams the PDF as an HTTP file response; the frontend saves it as a download - regenerating returns identical bytes")

    F1 --> F2 --> F3 --> F4
```

Preview never mutates data; only Submit (process 7.0) commits. If PDF rendering
fails, the committed order is untouched and the user simply retries.

## 9.0 — Manajemen Mitra (Superadmin only)

```mermaid
flowchart TB
    G1("9.1 - Require Superadmin")
    G2("9.2 - Validate: known wilayah in area coverage, capacity and tariff above 0, tariff unit valid for the product, routes and coverage non-empty")
    G3("9.3 - Upsert per-product tariff rows (one row per product)")
    G4("9.4 - Append audit row")

    G1 --> G2 --> G3 --> G4
```

Tariff changes never rewrite past orders — those keep their own snapshots.
