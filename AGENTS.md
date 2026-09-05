# AGENTS — Repo Guide (Stock Monitor dan TSO)

Guide for any agent or developer working in this repository. Read `PLAN.md` for the
reconstruction roadmap; `architecture/` (EN) / `arsitektur/` (ID) for topology, DFD and
flowcharts; `docs/` for progress tracking.

## Persona

Senior engineer mindset: design the optimal flow for every problem, question the
security of every line of code. Spec-first, code second, test always, docs in sync.

## Stack (post-reconstruction)

| Layer | Tech |
|---|---|
| Frontend | TanStack Start (SSR, Node), TypeScript, TanStack Router/Query/Form, Tailwind (ported `sm-*` Material-3 tokens) |
| Backend | .NET 8 REST API (Minimal API endpoints), JWT bearer, ASP.NET Core Identity as user store |
| Database | PostgreSQL via Npgsql + EF Core migrations |
| Deploy | nginx (TLS, reverse proxy) + frontend + api + postgres — one container per service, docker compose, monorepo |
| Tests | xUnit + FluentAssertions + NSubstitute + WebApplicationFactory (ephemeral Postgres) · frontend: Vitest |

## Dev runbook

```bash
# 1. database
docker compose up -d postgres

# 2. backend (JWT auth lives here)
dotnet run --project src/StockMonitorTso.Api     # http://localhost:8080

# 3. frontend (dev server proxies /api → localhost:8080)
cd frontend && npm install && npm run dev        # http://localhost:3000
```

Full stack (as in production): `docker compose up -d` → nginx on `:80`/`:443`.

**Seed accounts** (auto-created at api startup; password length minimum 8):

| Akun | Email | Password | Role |
|---|---|---|---|
| Superadmin | `superadmin@stockmonitor.local` | `Superadmin!2345` | Superadmin |
| Operator | `operator@stockmonitor.local` | `Operator!2345` | Operator |
| Supervisi | `supervisi@stockmonitor.local` | `Supervisi!2345` | Supervisi |
| Tamu | `tamu@stockmonitor.local` | `Tamu!2345` | Tamu |
| Multi-role | `multi@stockmonitor.local` | `MultiRole!2345` | Operator + Supervisi + Tamu |

## Hard rules

- **Konservasi stok:** never write `Stok` directly — only via the transaction service
  (`Receive` / `Issue` / `Adjust±` / `Transfer`, atomic, overdraft rejected, audited).
- **`CD_n` is conceptual:** `(sisa stok saat ETA_n + Next Supply_n) ÷ DOT`. Never copy
  the Excel formula (`Next Supply ÷ Σ CD`) — it is dimensionally wrong.
- **Satuan kanonik:** Tabung (LPG) vs Kiloliter (minyak tanah); reject cross-units.
- **RBAC at the service layer** (`RequireAnyRole` / `RequireSuperadmin`), mirrored per
  endpoint — hiding UI controls is not enforcement. The active-role claim governs.
- **Audit log for every mutation** (actor, active role, time, before/after).
- Domain terms stay Indonesian: Pihak, Sales Area, Gudang Wilayah, Agen, Outlet, DOT,
  CD, Aman/Warning/Kritis, Rencana Kedatangan, Konservasi Stok.
- No comments unless intent is non-obvious; no secrets in source; `TreatWarningsAsErrors`
  means zero warnings.
- All stock-changing logic lives in Infrastructure services; the frontend and endpoints
  are thin layers over them.

## Traps (learned the hard way)

- **JWT:** on 401 → refresh once → retry → else redirect `/login`. Switching active role
  MUST call `/api/auth/switch-role` (server re-issues the token) — client-side role
  state alone changes nothing. Idle 15 min = access-token expiry; activity refreshes.
- **Behind a proxy:** set `APP_BASE_URL` (→ `App:BaseUrl`) to pin scheme/host, or ensure
  the proxy sends `X-Forwarded-Proto` — otherwise post-auth redirects/cookies downgrade
  to http. `/api/debug/request` (dev-only) shows the effective scheme/host.
- **PostgreSQL migrations:** regenerate with the Npgsql provider; never port SQLite
  migration files. Partial unique indexes need Postgres quoting; concurrency uses
  `xmin` (409 on stale write).
- **Seed flags:** LPG seeds from `seeds/lpg-stok.json` (converted from the old workbook —
  don't hand-edit casually; regenerate via the parser if the source changes); Mitra seeds
  from `seeds/mitra-tso.json`; `Seed:SkipStock=true` skips stock seeding for tests/empty
  runs. Stock seeds only into an empty `StokEntitas` table — reseed with
  `docker compose down -v && up -d`.
- **TanStack Start SSR:** server-only code must stay out of the browser bundle
  (import guards). Query keys must include role-scoping and route params. After adding
  a route file, run `npm run generate-routes` (tsr CLI) — `vite build` does not
  regenerate the route tree. SSR output is gzip/binary-ish to grep — use `grep -a`.
  **No `window`/`localStorage` outside `typeof window` guards** — the server runtime
  has no window; unguarded accessors crash SSR prefetch (auth storage lives in
  `lib/api.ts`+`lib/auth.ts` behind guards).
- **Tailwind tokens:** use the ported `sm-*` design tokens; never hardcode hex colors.
- **Idempotency:** duplicate TSO submit within 1 minute returns the existing order;
  invoice regeneration is byte-identical; preview/generate never mutate data.
- **Rebuild:** frontend and backend code changes need rebuild/restart (or watch mode);
  data changes do not.

## Branch strategy

- `main` = **main repository**: full documentation set (`PLAN.md`, `AGENTS.md`,
  `architecture/`, `arsitektur/`, `docs/`) + all code.
- `apps` = **production update point**: app + deploy artifacts only (`src/`, `tests/`,
  `frontend/`, `deploy/`, `seeds/`, Docker files). Never commit documentation there.

## Verification gate (before every commit)

```bash
dotnet build StockMonitorTso.sln -warnaserror
dotnet test StockMonitorTso.sln          # prasyarat: docker compose up -d postgres
dotnet format StockMonitorTso.sln --verify-no-changes
cd frontend && npm run build && npm run lint && npm test
docker compose up -d && curl -f http://localhost/health
```
