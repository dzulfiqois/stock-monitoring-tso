# Plan — Aplikasi Stock Monitor dan TSO (Architectural Reconstruction)

> Workplan and guard-rails for agents and contributors. This document is self-contained:
> every rule needed to build correctly is stated here and in `AGENTS.md`, the
> `architecture/` documents (English) and `arsitektur/` documents (Bahasa).

## 1. Goal

One application, two modules sharing one shell (login + dashboard):

- **Monitoring Stok** (minyak tanah + LPG) — hirarki `Pusat → Gudang Wilayah → Agen (2–3)
  → Outlet (2 per Agen)`, granularitas `(Wilayah × Produk × Tier)` + `(Agen × Produk)` +
  `(Outlet × Produk)`, invarian konservasi stok, Rencana Kedatangan (maks 3 slot),
  RBAC 4 role.
- **Transport Shipping Order (TSO)** — order `Pusat → Gudang Wilayah` dari master Mitra
  TSO (multi-product order lines, per-product tariff snapshots, optional distance),
  commit di Submit, dampak stok saat keberangkatan, Draft Invoice PDF idempoten.

### Locked decisions (2026-09 reconstruction)

| Decision | Choice |
|---|---|
| Topology | `browser → nginx → { frontend, backend } → db` |
| Frontend | **TanStack Start (SSR, Node runtime)** + TypeScript + Tailwind (ported `sm-*` Material-3 tokens) |
| Auth | **JWT bearer** (stateless API); ASP.NET Core Identity retained as user store (hash, lockout, roles); 15-minute idle via access-token lifetime + activity-based refresh; role switch = re-issued token |
| Migration style | **Backend-first, phased (R0–R5)**; Blazor stays functional during transition, deleted at R5 |
| Deployment | **One container per service**, orchestrated by docker compose; everything in this **monorepo** |
| Database | **PostgreSQL** (Npgsql provider), fresh migrations |
| Branches | `main` = main repository (full docs + code) · `apps` = production update point (app + deploy artifacts only) |

## 2. Monorepo layout

```
/
├── frontend/                        ← TanStack Start app (own Dockerfile: node build → node runtime)
├── src/
│   ├── StockMonitorTso.Api          ← backend host: Program.cs, JWT, REST endpoints (own Dockerfile)
│   ├── StockMonitorTso.Domain       ← entities, enums, pure calculation (no framework deps)
│   ├── StockMonitorTso.Infrastructure ← DbContext, migrations (Postgres), seeds, services, audit, invoice
│   └── StockMonitorTso.Web          ← Blazor — retired at R5
├── tests/
│   ├── StockMonitorTso.UnitTests
│   └── StockMonitorTso.IntegrationTests  ← WebApplicationFactory + ephemeral Postgres
├── deploy/
│   ├── nginx/nginx.conf             ← SPA/SSR proxy: / → frontend, /api + /health → api, TLS
│   └── postgres/init/               ← db init script
├── docker-compose.yml               ← 4 services: nginx · frontend · api · postgres
├── seeds/mitra-tso.json             ← Mitra TSO master (3 mitra, per-product tariffs)
├── architecture/ (EN) · arsitektur/ (ID) · docs/ · PLAN.md · AGENTS.md   ← documentation (main only)
└── .gitignore
```

## 3. Domain guard-rails (unchanged — apply to every phase)

**Konservasi stok:** `Stok` is never edited directly. All changes via atomic transactions
in the service layer: `Receive` (+), `Issue` (−, auto `StokHabisTerjual`), `Adjust`
(opname ±, never 0), `Transfer` (debit source + credit destination, same Wilayah,
atomic per SKU). Overdraft rejected ("stok tidak mencukupi"). Every mutation →
`StockTransactions` + `AuditLogs`.

**Rumus:** `CD = Stok ÷ DOT` (N/A bila DOT 0) · `Exhaust = TanggalStokAwal + CD` ·
Status: Kritis `CD < 3`, Warning `3 ≤ CD < 7`, Aman `CD ≥ 7` ·
`CD_n = (sisa stok saat ETA_n + Next Supply_n) ÷ DOT` — **never** the Excel formula
(`Next Supply ÷ Σ CD`, wrong dimension) · `MT = Tabung × kg ÷ 1000` (LPG only).

**Satuan kanonik:** Tabung (LPG) vs Kiloliter (minyak tanah); cross-units rejected.

**RBAC matrix (enforced in the service layer, mirrored per endpoint):**

| Aksi | Superadmin | Operator | Supervisi | Tamu |
|---|---|---|---|---|
| Baca | ✅ | ✅ | ✅ | ✅ |
| Register Sales Area / Create stok | ✅ | ✅ | ❌ | ❌ |
| Update detail stok / transaksi stok / transfer | ✅ | ❌ | ✅ | ❌ |
| Identitas Agen & Outlet: Create/Update | ✅ | ❌ | ✅ | ❌ |
| Delete (stok, agen, outlet, order) | ✅ | ❌ | ❌ | ❌ |
| TSO Create | ✅ | ✅ | ❌ | ❌ |
| TSO Update | ✅ | ❌ | ✅ | ❌ |
| Mitra admin, Manajemen user, assign role/password | ✅ | ❌ | ❌ | ❌ |

**TSO rules:** mitra must be active + cover the destination wilayah; every product line
qty > 0; departure date ≥ today; duplicate submit within 1 minute returns the existing
order; ETA = departure + 7 days; per-product tariff/cost snapshots; Rencana Kedatangan
per product line (maks 3 slot each); failure → `FlagTertunda` + resync; invoice
regeneration byte-identical.

## 4. Phased roadmap (backend-first)

### R0 — Groundwork
- EF provider SQLite → **Npgsql**; regenerate **all** migrations for PostgreSQL
  (partial unique-index quoting; replace `RowVersion byte[]` with `xmin` concurrency
  token or a version column).
- JWT auth endpoints: `POST /api/auth/login`, `POST /api/auth/refresh`,
  `POST /api/auth/logout`, `GET /api/auth/me`, `POST /api/auth/switch-role`.
- Compose skeleton: **api + postgres** (pgdata volume, init script) + nginx config.
- Integration tests: ephemeral Postgres (Testcontainers) + bearer auth helpers.
- Seed: users/roles always; Mitra from JSON; stock mock behind `Seed:SkipStock`
  (the workbook is absent from this repo).
- **Gate:** api boots on Postgres in compose; login → me → switch-role round-trips via
  curl; `dotnet test` green.

### R1 — REST surface completion
- New endpoint groups mirroring every Blazor flow: `/api/dashboard`,
  `/api/stock` (register/update/transact/delete), `/api/agen` (+transfer),
  `/api/outlet` (+transfer), `/api/users` (Superadmin) — alongside existing
  `/api/tso`, `/api/mitra`.
- ProblemDetails (RFC 7807) on every error path; 400 for validation, 403 for RBAC.
- **Gate:** every Blazor flow has a REST equivalent + integration tests.

### R2 — React shell
- `frontend/` scaffold: TanStack Start + TypeScript + Tailwind (ported `sm-*` tokens).
- Pages: login (+ active-role pick), dashboard (Ringkasan Operasional, Sektor KPI, chart).
- Query client + auth interceptor: Bearer header; 401 → refresh once → retry; else
  redirect `/login`; 403 → notice.
- **Gate:** auth journey works end-to-end through nginx in compose.

### R3 — Monitoring module pages
- Gudang Wilayah cards (LPG 1 card/wilayah with 3 size chips), Detail Sales Area
  (+ Update Data Harian, + Kirim ke Agen modals), Register Sales Area (branching),
  Daftar/Detail Agen (+ Kirim ke Outlet), Daftar/Detail Outlet.
- **Gate:** parity walkthrough; konservasi + overdraft verified through the UI.

### R4 — TSO + Mitra + Admin pages
- TSO wizard 4 langkah (multi-product lines, distance for per-km tariffs), list,
  preview, invoice download (blob); Mitra admin (Superadmin); User management
  (Superadmin).
- **Gate:** TSO flow green through the UI; hidden/403 RBAC behavior verified.

### R5 — Cutover + hardening
- ✅ **R5a**: `StockMonitorTso.Web` deleted; entire integration suite migrated to the Api
  host (factory swap + bearer; idle-timeout tested via token expiry); legacy
  single-container deploy retired.
- ✅ **R5b**: Serilog structured (JSON in Production, request logging + RequestId); `/ready`
  (database check) + `/health` liveness; nginx TLS template (`nginx-tls.conf.template` +
  `gen-dev-cert.sh`); api image 461MB → 227MB (alpine + musl publish + fontconfig for QuestPDF).
- **R5c**: sync `main` → `apps` (app + deploy artifacts only, no documentation).
- **R5d**: final documentation sync.
- **Gate:** ✅ 4 containers healthy + smoke via nginx; 132/132 tests; build 0/0; format clean.

## 5. Container & deployment rules

- One container per service: **nginx** (stock image + repo config), **frontend**
  (TanStack Start SSR on Node, non-root), **api** (multi-stage sdk→aspnet, non-root),
  **postgres** (stock image + init script + `pgdata` volume).
- Compose builds custom images from in-repo Dockerfiles (monorepo, one versioned
  pipeline); `depends_on` + healthchecks wired; JWT secrets via env/user-secrets, never
  in source.
- During R0–R4 production keeps the current single-container deploy; 4-container goes
  live at R5 cutover.

## 6. Auth model (JWT parity with the old session rules)

- Access token: 15-minute expiry; claims: `sub`, email, **active role** (user picks at
  login; default = first role). Permissions follow the active role only.
- Refresh on activity restores the sliding 15-minute idle; logout revokes the refresh.
- Switch role = `POST /api/auth/switch-role` (membership validated server-side) → new
  token; never client-side only.
- No auth cookies for the API → antiforgery dropped for API calls.

## 7. Data strategy

- Schema: fresh Npgsql migrations; never reuse SQLite migration files.
- Dev data: reseed (recommended over SQLite→Postgres data migration).
- Seeds at api startup: roles + 5 accounts → stock (LPG from `seeds/lpg-stok.json`;
  minyak tanah from the hardcoded sample; `Seed:SkipStock` skips both) →
  agen/outlet 50% split (audited as `Transfer`) → Mitra upsert from JSON.
- The Excel workbook is **no longer a runtime dependency** — it was converted once
  (via the original parser, byte-faithful: 42 rows) into `seeds/lpg-stok.json`.

## 8. Verification commands

```bash
dotnet build StockMonitorTso.sln -warnaserror
dotnet test StockMonitorTso.sln
dotnet format StockMonitorTso.sln --verify-no-changes
cd frontend && npm run build && npm run lint && npm test
docker compose up -d && curl -f http://localhost/health
```

## 9. Risks & mitigations

| Risk | Mitigation |
|---|---|
| JWT idle/role-switch parity bugs | Token rules centralized in one auth service + tests for refresh/switch/logout |
| Concurrency token migration | `xmin` mapping tested (409 on stale write) |
| SQLite→Postgres migration drift | Regenerate all migrations; CI green on ephemeral Postgres |
| Seed without workbook | `Seed:SkipStock` + JSON mitra; optional workbook path |
| XSS / token storage | Refresh-token storage decision tracked (open question); no secrets in localStorage |
| SSR Node ops | Non-root image, healthcheck, restart policy |
| CORS in dev | Vite/TanStack dev proxy → api; same origin in prod via nginx |

## 10. Open questions

- Refresh-token storage: memory vs httpOnly cookie?
- PostgreSQL major version pin (default 16)?
- E2E scope (Playwright) — minimal smoke vs full journey?
- Keep 2FA/manage scaffold endpoints from the old Identity pages? (default: drop)
- ~~Does the stock workbook return to the repo, or stay optional forever?~~ **Resolved
  2026-09**: workbook converted to `seeds/lpg-stok.json` (tracked); xlsx machinery deleted.

## 11. Definition of Done (per task)

- [ ] `dotnet build -warnaserror` clean
- [ ] Tests added/updated; coverage does not drop
- [ ] Audit log for every mutation
- [ ] No new `// TODO`
- [ ] `npm` gates clean when frontend touched
- [ ] Docs updated when architecture/flows change (this set: PLAN, AGENTS, architecture/, arsitektur/, docs/)

## 12. References

- `AGENTS.md` — persona, runbook, traps, gates
- `architecture/01–05` (English) and `arsitektur/01–05` (Bahasa) — topology, DFD 0/1/2, flowchart
- `docs/PHASE_CHECKLIST.md`, `docs/SESSION_HANDOFF.md`, `docs/CHECKLIST_DOD.md` — progress tracking
