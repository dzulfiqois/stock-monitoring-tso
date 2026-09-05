# Session Handoff — Aplikasi Stock Monitor dan TSO

> Dibuat: 2026-08 · Diperbarui: 2026-09 (slice rekonstruksi) · Untuk melanjutkan di session baru. Bacaan wajib sebelum lanjut:
> `PLAN.md` (roadmap rekonstruksi R0–R5), `AGENTS.md` (runbook & traps), `docs/PHASE_CHECKLIST.md`, `docs/CHECKLIST_DOD.md`.

## Slice 2026-09 — R5b Hardening (selesai)

**Status: R5b hijau.** Isi:

- **Serilog**: dua tahap (bootstrap → `UseSerilog` config-driven). Development = teks; Production = **CompactJsonFormatter** (`appsettings.Production.json`, commit-able, tanpa secret). Request logging terstruktur (method/path/status/duration/**RequestId**) dipasang sedini mungkin. Terverifikasi live: log JSON `@t/@mt/@l/@x` di docker logs.
- **/ready**: health check database (`SELECT 1` via `DatabaseReadyHealthCheck`, tag "ready") — `/health` liveness tanpa dependensi. Keduanya di-proxy nginx.
- **TLS template**: `deploy/nginx/nginx-tls.conf.template` (443, redirect 80→443, ssl_protocols) + `gen-dev-cert.sh` (self-signed lokal). Compose lokal tetap HTTP.
- **Image slim**: api base Debian → **alpine** + publish `-r linux-musl-x64 --self-contained false` (folder `runtimes/` 72MB hilang; `/app` 93MB→30.7MB) + `apk fontconfig font-dejavu` (QuestPDF musl — invoice PDF 35653 bytes byte-identical ✅) + `/app/keys` dibuat & owned 1654 (DataProtection error log hilang). **api 461MB → 227MB**; frontend 239MB (node base). Healthcheck api → busybox `wget` (alpine tanpa bash).
- Pelajaran: (1) `CreateBootstrapLogger` + banyak host WAF per proses = "logger is already frozen" — bootstrap pattern hanya untuk satu host per proses, suite test memakai `UseSerilog` polos; (2) `try/catch` di sekitar `app.Run()` menelan `StopTheHostException` WebApplicationFactory → "entry point exited without ever building an IHost" — jangan bungkus Run pada app yang dites WAF; (3) `dotnet publish -r <rid>` wajib restore dengan `-r` yang sama (NETSDK1047); (4) QuestPDF alpine = musl native + apk fontconfig/font-dejavu; (5) grep dotnet-test hanya menampilkan Passed!/Failed! menyembunyikan error build — selalu sertakan `error CS`/`Error` di filter.

---

## Slice 2026-09 — R5a Cutover: Web dipensiunkan, test pindah host Api (selesai)

**Status: R5a hijau — Blazor dihapus, seluruh test integration kini berjalan di host Api (JWT), 132/132.**

- **9 suite dimigrasi** (swap fixture mekanis — aktor `ClaimsPrincipal` manual host-agnostic): StockWrite(13), AgenService(9), WarehouseTransfer(6), AgenDashboard(5), StockDashboard(6), TsoService(10), UserAdminCreate(7), HealthCheck(1), AuthAndRbac(6). Mapping fixture: NoStock→`TestApiWebApplicationFactory`, full-seed→`TestApiWebApplicationFactoryWithStock`.
- **Rewrite**: `AuthAndRbacTests.IdleTimeout` (cookie-options) → `AccessToken_ExpiryIs15Minutes` (ValidTo−ValidFrom token = 15 menit + klaim role aktif). **Dihapus**: `AdminPageAccessTests` (cookie flow; paritas: UsersApiTests SA-200/Tamu-403 + test anon-401 baru). **Plus**: `ActiveRoleClaimsPrincipalFactory` kini juga terdaftar di host Api — test ClaimsFactory OnlyActiveRole **menangkap celah** (default factory memuat semua role) → defense-in-depth ditambahkan.
- **Dihapus**: `src/StockMonitorTso.Web` (sln + folder), test ProjectReference ke Web, root `Dockerfile` + `docker-compose.legacy.yml` (legacy single-container resmi pensiun).
- Solusi kini: **Domain · Infrastructure · Api** (+ `frontend/`, `tests/`). Gate: build 0/0 · 132/132 · format bersih · compose 4 kontainer healthy · smoke auth+summary via nginx.

---

## Slice 2026-09 — R4.5 Sweep final QtyInput (selesai)

Audit grep menyeluruh menemukan **10 input polos terakhir** yang lolos konversi sebelumnya (semuanya blok multiline / destructured setter): opname di DailyUkuranRow + minyak opname (sales-area), opname (outlet), dan **4 input transfer di modal Kirim ke Outlet (DetailAgen)** — termasuk satu hasil regex rusak berupa hibrida `QtyInput` dengan handler lama `Number(e.target.value)`. Semua kini `QtyInput`. Kini **nol** `<input type="number">` di luar `QtyInput.tsx`; satu-satunya pengecualian: Jarak (km) di wizard — state string, memang sah. Pelajaran: setelah konversi massal, audit dengan `grep -rn '<input type="number"'` dan jalankan `tsc --noEmit` (vite build tidak mengecek tipe), serta hati-hati replace blok multiline (tag `<input` pembuka bisa tertinggal → parse error).

---

## Slice 2026-09 — R4.4 Sweep QtyInput menyeluruh (selesai)

Audit `<input type="number">` menyeluruh: **19 input tersisa** (DailyEntry agen ×3, daily outlet ×3, transfer agen ×4, transfer gudang ×3, register LPG ×9 — total 19, plus mitra ×2 ditemukan belakangan) masih memakai `<input>` polos dengan literal `0`/nested setter yang tak terjangkau regex konversi pertama. Semua dikonversi ke `QtyInput` (onChange nested → `(next) => ...`). Hasil akhir: **nol** `<input type="number">` di luar `QtyInput.tsx` — satu-satunya pengecualian adalah input Jarak km di wizard (state string, memang bebas diketik). Verifikasi: tsc/build/lint/vitest hijau, 7 route SSR 200.

---

## Slice 2026-09 — R4.3 QtyInput final (selesai)

Revisi kedua `QtyInput`: versi `key={value}` (remount) membuat fokus input hilang tiap ketikan — manual typing mustahil, hanya spinner. **Final: string-buffer pattern tanpa effect** — `text ?? derived` (null = derive dari prop), `onBlur` menormalkan tampilan, placeholder `"0"` sebagai hint untuk nilai nol (bukan nilai literal). Semua field numerik (wizard, modal Update Harian/Kirim, Register) memakai komponen ini. Pelajaran: pola "adjust state during render" / string-buffer + onBlur, bukan `key`-remount maupun effect+setState.

---

## Slice 2026-09 — R4.2 Dua bug UI (selesai)

1. **Input angka "menempel 0"** — controlled input dengan `Number('')=0` membuat field selalu kembali ke 0 saat dikosongkan. Fix: komponen `QtyInput` (uncontrolled + `defaultValue`, remount via `key={value}` untuk perubahan dari luar seperti stepper/reset modal) — dipasang di wizard TSO, semua modal Update Harian/Kirim, dan halaman Register. Pelajaran: jangan `onChange={setX(Number(e.target.value))}` pada controlled input ber-inisial 0.
2. **"/agen/$id/outlet" dan "/tso/$id/edit" serta "/tso/create" tidak merender halaman child** — dot-nesting TanStack Router menjadikannya CHILD dari halaman parent (`agen.$agenId.tsx`, `tso.$tsoId.tsx`, `tso.tsx`) yang tidak punya `<Outlet/>` → child tak pernah tampil (URL berubah, UI diam). Fix: pola layout+index — parent jadi pass-through (`component: () => <Outlet />`), konten pindah ke `_app.agen.$agenId.index.tsx`, `_app.tso.$tsoId.index.tsx`, `_app.tso.index.tsx`. Pelajaran: di flat routes, file bertitik **selalu nested** — parent berkatan halaman harus jadi pass-through layout atau child pakai pola index.

---

## Slice 2026-09 — R4.1 Fix JSON.parse pada transfer/resync (selesai)

**Gejala:** "JSON.parse: unexpected end of data" saat CRUD gudang/agen/outlet. **Root cause:** tiga endpoint mengembalikan `Results.Ok()` **tanpa nilai** = HTTP 200 body kosong (`transfer-from-warehouse`, `transfer-from-agen`, `resync`) — client `apiFetch` menangani 204 tapi tidak 200-kosong → `res.json()` atas string kosong melempar SyntaxError. **Fix dua sisi:** (1) client `apiFetch` membaca body sebagai text — kosong → `undefined` (defensif untuk semua endpoint); (2) server: tiga `Results.Ok()` → `Results.NoContent()` (204 semantik untuk operasi void). Test R1 disesuaikan (transfer kini 204). Verifikasi live: transfer gudang→agen→outlet 204 + konservasi (0.25 → agen 0.2 → outlet 0.1) + resync 204. Pelajaran: `Results.Ok()` tanpa argumen = 200 kosong — untuk operasi void gunakan `Results.NoContent()`, dan client jangan asumsikan semua 2xx punya body JSON.

---

## Slice 2026-09 — R4 TSO + Mitra + Admin UI (selesai)

**Status: R4 hijau — tiga modul terakhir dipindah ke React; backend 134/134 tetap hijau.**

- `/tso`: list + delete modal (SA). **Wizard** (`components/TsoWizard.tsx`, create + edit share): 4 langkah persis Blazor — multi-SKU LPG / minyak, rute+jarak (wajib utk per_kilometer), mitra ter-filter coverage + estimasi per baris, ringkasan; commit di Submit → redirect `/tso/{id}`.
- `/tso/{id}`: preview 8 kolom + status + resync (FlagTertunda) + **invoice PDF via blob download** (fetch POST → blob → objectURL, bukan data-URI base64).
- `/mitra` (SA): tabel + modal 12 field + tarif per-jenis allowlist; edit = PUT update lalu loop `PUT /{id}/tarif` per jenis.
- `/admin/users` (SA): daftar + roles pill toggle (assign/remove) + Set Password prompt + modal Tambah User (password konfirmasi, roles checklist, role aktif terbatas ke terpilih).
- `lib/tso.ts`, `lib/users.ts`: typed API layer.
- **Bug backend baru yang ketahuan saat R4**: `GET /api/tso/{id}` + `GET /api/mitra` **500** — siklus serialisasi JSON (`TransportOrderDetail.Order`, `MitraTarif.Mitra` back-ref) yang baru pertama terekspos via HTTP (test integration memanggil service langsung, bukan JSON). Fix: `ReferenceHandler.IgnoreCycles` di `ConfigureHttpJsonOptions` (Api Program). Pelajaran: endpoint yang mengembalikan **entitas EF mentah** perlu perhatian navigasi balik — pertimbangkan DTO eksplisit bila respons perlu dipangkas.
- E2E via nginx: create order (operator, 201 → StockImpacted + Rencana Kedatangan) → GET detail+details → invoice PDF (35KB %PDF) → 6 route SSR 200.

---

## Slice 2026-09 — R3 Monitoring UI (selesai)

**Status: R3 hijau — 13 route React hidup via nginx, build/lint/vitest/tsc bersih, 0 error SSR.** Backend tak tersentuh (134/134 tetap).

- **Shell**: `_app.tsx` pathless layout (sidebar 288px + topbar + Outlet). Nav mengikuti Blazor `NavMenu.razor` (Ringkasan/Gudang Wilayah/TSO/Mitra[SA]/Register/Manajemen User[SA]) + role switcher (select semua role; ganti role = `switch-role` + `window.location.reload()` — mirror `ActiveRoleSwitcher` force-reload) + logout.
- **Styles**: `app.css` Blazor di-port **verbatim** ke `styles.css` (alias `:root --sm-*` → nilai token sama; shell/sidebar/nav/typography/kpi/pill/chip/btn/table/segmented/breadcrumb/filter/modal/chart). Font Inter + JetBrains Mono + Material Symbols Outlined via @import googleapis. Halaman React memakai **nama kelas yang sama persis** dengan halaman Blazor.
- **`lib/data.ts`**: typed API layer (data/stock/agen/outlet) + helper display (wilayahDisplay/produkDisplay/tierDisplay/satuanProduk).
- **Komponen**: `RoleGate` (mirror AuthorizeView — enforcement tetap API), `StatusPill`, `Modal` (ESC + backdrop close).
- **Halaman**: dashboard penuh (Sektor KPI + sm-chart + Metrik Minyak), gudang-wilayah (filter + KPI + kartu + hapus), detail sales-area (4 KPI + tabel tier + log + modal Update Harian segmented [Receive/Issue/Adjust/UpdateDetail] + modal Kirim ke Agen), register (branching), daftar agen/outlet (CRUD modal), detail agen (Kirim ke Outlet per 4 SKU + Update Harian), detail outlet.
- **R4 stub**: `/tso`, `/mitra`, `/admin/users` nav aktif dengan halaman "menyusul".
- Gotchas: Tailwind v4 tak bisa `@apply` kelas komponen sendiri (utility di-expand); params Link harus **string** (`String(id)`); `vite build` tidak menjalankan tsc — gunakan `npx tsc --noEmit` sebagai gate tambahan; SSR halaman authed = skeleton shell (localStorage tak terlihat server — konten muncul setelah hydrate).

---

## Slice 2026-09 — R2.2 Auth gate pada direct URL load (selesai)

**Masalah:** user ter-autentikasi yang mengetik `/login` di URL bar tetap melihat form login (dan sebaliknya, anonim yang mengetik `/` melihat shell dashboard). **Root cause:** `beforeLoad` berjalan di server saat direct load — server tidak melihat localStorage, guard `typeof window` membuat redirect tidak pernah dieksekusi; tidak ada evaluasi ulang client setelahnya.

**Fix:** `useSyncExternalStore`-based `useIsClient()` hook (`lib/useIsClient.ts`; server snapshot `false`, client `true`, tanpa hydration mismatch) + `useEffect` redirect per route — `/login`: ter-autentikasi → `/`; `/`: anonim → `/login` (query di-gate `enabled: isClient && !!session` agar tidak menembak 401). `beforeLoad` dipertahankan untuk SPA navigation. Catatan: pola effect+setState awal ditolak eslint `react-hooks/set-state-in-effect` — `useSyncExternalStore` adalah pola yang disetujui.

---

## Slice 2026-09 — R2.1 Login page faithfulness (selesai)

Halaman login React dibuat **menyalin struktur Blazor persis**: `LoginLayout.razor` (`sm-auth-shell` + background photo `background.jpeg` + gradient overlay → `sm-auth-card` 440px → `sm-auth-brand` logo Pertamina 28px + "Stock Monitor & TSO") + `LoginForm.razor` (StatusMessage error + `<hr>` + floating-label Email/Password dengan placeholder yang sama (`name@example.com`/`password`), autocomplete username/current-password, checkbox "Remember me", tombol full-width "Log in" ala `btn-primary btn-lg`). Aset di-copy ke `frontend/public/images/`. Multi-role picker tetap (kebutuhan JWT role aktif). Verifikasi: SSR `/login` memuat seluruh penanda struktur (grep -a), aset 200 via nginx, journey login+summary OK.

---

## Slice 2026-09 — R2 hotfix: SSR crash di auth accessors (selesai)

**Gejala:** user login gagal di browser padahal API 200 (nginx log membuktikan POST login sukses). **Root cause:** `getSession()`/`clearSession()`/`getAccessToken()`/`refreshSession()` membaca `window.localStorage` tanpa guard → `ReferenceError: window is not defined` saat SSR prefetch route `/` setelah login (frontend log membuktikan). **Fix:** guard `typeof window` di dalam accessor (satu tempat); guard `beforeLoad` dibiarkan — server skip cek (shell kosong dirender, client hydrate + enforce). Rebuild frontend → log bersih, login via browser berfungsi.

---

## Slice 2026-09 — R2 React shell (selesai)

**Status: R2 hijau — frontend TanStack Start hidup di compose, auth journey end-to-end via nginx.** Backend tak tersentuh (134/134 tetap hijau).

- Scaffold: `npm create @tanstack/start` (nitro 3 beta + vite 8 + React 19 + Tailwind v4; route generation via `@tanstack/router-cli` → `npm run generate-routes` **wajib dijalankan setelah menambah route** — vite build tidak men-generate otomatis).
- Struktur: `src/routes/{__root,login,index}.tsx` · `src/lib/{api,auth}.ts` · token `sm-*` di `styles.css` (`@theme` Tailwind v4).
- `apiFetch` interceptor: Bearer; 401 → `refreshSession()` sekali → retry; gagal → clearSession + pemanggil redirect `/login`; 403 → ApiError notice. `setRefreshAction(refreshSession)` di-wire di `router.tsx` (hindari import cycle api↔auth).
- SSR: shell dirender server (login page terbukti SSR via curl `grep -a "Masuk"` — payload hydration bikin grep menganggap file binary; pakai `-a`).
- Halaman: `/login` (email+password → role picker bila multi-role) · `/` Ringkasan Operasional (KPI Sektor + bar chart Agen/Outlet + role switcher + logout). beforeLoad gate di `/`.
- Deploy: `frontend/Dockerfile` (build → `.output/server/index.mjs`, user node) · compose `frontend` :3000 · nginx `/` → frontend (Upgrade/Connection headers), `/api`+`/health` → api. **Bind-mount nginx.conf hanya terbaca saat start — `docker compose restart nginx` setelah ubah config.**
- Gates: `npm run build` ✅ · `npm run lint` ✅ (eslint flat config) · `npm test` ✅ (vitest 5 — warning "Vite servers exiting" benign) · SSR smoke via curl ✅ · `dotnet test` 134/134 ✅.

---

## Slice 2026-09 — R1 REST surface (selesai)

**Status: R1 hijau — 134/134 test (32 unit + 102 integration).** Lima grup endpoint baru di atas service yang sudah ada (RBAC tetap di service layer; endpoint hanya `[Authorize]` + `RequireRole` untuk users-list yang di service tidak dijaga):

- `/api/dashboard` (13 read), `/api/stock` (register/update-detail/transact/delete), `/api/agen` (CRUD + transfer-from-warehouse), `/api/outlet` (CRUD + transfer-from-agen), `/api/users` (list/create/assign/remove/set-password — Superadmin via endpoint policy).
- `ProblemMapper`: satu titik mapping exception → ProblemDetails (DbUpdateConcurrency 409, UnauthorizedAccess 403, KeyNotFound 404, Argument 400, InvalidOperation 400, lainnya 500).
- `JsonStringEnumConverter` global — semua request/response API memakai nama enum string (kontrak React R2).
- Fix kecil: Api host `.AddDefaultTokenProviders()` (GeneratePasswordResetToken butuh provider "Default" — sebelumnya NotSupportedException → 500).
- Pelajaran test: **register stok itu Superadmin+Operator** (bukan Supervisi) — beberapa test pertama salah role sehingga id=0 → 404; dan grep dotnet-test menyembunyikan error kompilasi (CS0128 sempat terlihat seperti "hang" — sebenarnya build gagal instan).

Smoke live (container rebuild, tanpa wipe data): `/api/dashboard/summary` 200 (totalStok 53066.85), tamu → 403 di POST /api/stock dan GET /api/users.

---

## Slice 2026-09 — R0.1 APP_BASE_URL + forwarded headers (selesai)

**Motif:** pola bug klasik "login https → redirect http" di balik reverse-proxy (TLS terminate di proxy, Kestrel lihat http, redirect/cookie pakai Request.Scheme). Fix sebelum R5 membuatnya load-bearing.

- `App__BaseUrl` (env `APP_BASE_URL` → config `App:BaseUrl`): bila diset, `PublicBaseUrlMiddleware` (di Api, dipakai kedua host) pin `Request.Scheme`/`Host` — semua URL absolut deterministik. Bila kosong → `UseForwardedHeaders()` (For|Proto, KnownNetworks/Proxies cleared) memakai header nginx.
- Cookie Identity: `SecurePolicy=Always` bila BaseUrl https (scheme bug jadi "keras", bukan diam-diam).
- nginx sudah mengirim `X-Forwarded-Proto/For/Host` sejak R0 — tidak ada perubahan nginx.
- `/api/debug/request` (dev-only) untuk verifikasi scheme/host nyata di balik proxy; 404 di Production.
- Compose: `App__BaseUrl: ${APP_BASE_URL:-}` di service api; `.env.example` mendokumentasikan `APP_BASE_URL`.

**Insiden test suite (resolved):** setelah slice ini, `dotnet test` menggantung saat full-run (filtered hijau). Forensik `dotnet-stack`: 12 thread blocked di `CreateDatabaseAsync().GetResult()` dalam `ConfigureWebHost`, task `container.StartAsync` Testcontainers tak pernah selesai DI DALAM testhost — padahal probe console standalone start 4.3s OK, docker socket 0.01s OK. Kesimpulan: Docker I/O Testcontainers berperilaku tidak deterministik di dalam testhost environment ini. **Solusi: Testcontainers dibuang**, suite kini pakai Postgres compose yang sudah jalan (`TestDatabase` helper — `CREATE DATABASE sm_test_{guid}` per factory, drop saat dispose). Prasyarat: `docker compose up -d postgres` sebelum `dotnet test`. Hasil: **109/109 hijau (32 unit + 77 integration) dalam 38 detik**, deterministik.

---

## Slice 2026-09 — R0 Groundwork (selesai)

**Status: R0 hijau. Suite 106/106 hijau (5 test prasejarah xlsx pulih).** Komposisi: `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.11 (bukan `Microsoft.EntityFrameworkCore.Npgsql` — id itu tidak ada). Migrasi lama SQLite dihapus total, satu `InitialNpgsql` baru.

**Lampiran — seed LPG xlsx → JSON (2026-09):** workbook `Monitoring Tabung RPM(1).xlsx` (42 baris: 21 Gudang Wilayah + 21 Outlet, 7 wilayah × 3 SKU) dikonversi **via parser asli** (throwaway project memanggil `ExcelStockSeeder` — zero reimplementasi) menjadi `seeds/lpg-stok.json` (camelCase, enum string, tanggal ISO). Kini: `LpgStokSeeder` + `Seed:LpgJsonPath` + resolver generik `ResolveSeedFilePath`; `ExcelStockSeeder` parsing + `XlsxReader` dihapus (`StockSeedRows.cs` menyimpan `SeedStokRow`/`SeedRencana`/`LoadMinyakTanahSample`); `.gitignore` xlsx line dihapus. Smoke reseed (`down -v` → up): StokEntitas 272 (LPG 204 / Minyak 68), Agen 18, Outlet 36, Rencana 9, Transfer 216, konservasi agen = 50% gudang ✅. `Seed:SkipStock` dan nuansa "stock hanya seed ke tabel kosong" tetap berlaku.

Keputusan teknis R0:
- **RowVersion tetap `byte[]` + IsConcurrencyToken** (`bytea` di Postgres) — provider-agnostic, nol perubahan service; `xmin` tidak jadi.
- **DateTime**: legacy timestamp switch ON di kedua host; kolom audit UTC (`CreatedAt`/`UpdatedAt`/`InvoiceGeneratedAt` → `timestamp with time zone`), tanggal bisnis tetap `timestamp` (Local/Unspecified aman).
- **JWT**: access 15 menit (klaim `sub`+email+role aktif) + refresh 7 hari (audience `/refresh` + `typ=refresh` — refresh token otomatis ditolak JwtBearer biasa); switch-role validasi keanggotaan server-side + persist `ActiveRoleName` + token baru. Revokasi refresh masih stateless (open question di PLAN).
- **DataProtection** kini `DataProtection:KeyPath` configurable (default `/app/keys`).
- **Host baru**: `StockMonitorTso.Api` = `Sdk.Web` executable dengan `Program.cs` sendiri (guard `EF.IsDesignTime` untuk `dotnet ef`); namespace `StockMonitorTso.Api.Program` agar tak bentrok `Program` global milik Web di test.

Bug lama yang dibongkar Postgres/test:
1. **Merge-conflict markers tertinggal** di `LoginLegacy.razor` + `LoginLayout.razor` sejak commit merge apps — solusi tidak pernah build sukses; keduanya di-resolve (sisi apps menang: logo Pertamina, tanpa `@page` di LoginLegacy).
2. **Race DbContext** di `ActiveRoleSwitcher.razor`: query layout dan query halaman berpotongan di satu connection saat static SSR — SQLite (sync completion) menyembunyikannya, Npgsql (async beneran) meledak `NpgsqlOperationInProgressException`. Fix: koneksi switcher lewat child scope (`IServiceScopeFactory`) — sekaligus membenahi bug tertinggal yang lama.
3. `AuthApiTests` ordering: test jangan bergantung urutan eksekusi (SwitchRole mem-persist role aktif user multi).

Test: `dotnet test` → 32 unit hijau · integration 69/74 hijau (5 prasejarah xlsx, terdokumentasi). Smoke: compose `postgres+api+nginx` up → `/health` 200 via nginx → login → me → switch-role → me (Operator→Supervisi) via curl. `dotnet format --verify` bersih.

Cara pakai compose baru: `docker compose up -d` (JWT_KEY via `.env`, default dev-only di compose). Legacy single-container tetap tersedia di `docker-compose.legacy.yml` sampai R5. Catatan: image runtime aspnet tidak punya `wget`/`curl` — healthcheck api memakai `bash -c` + `/dev/tcp` (CMD, bukan CMD-SHELL, karena `/bin/sh`-nya dash).

---

## Slice 2026-09 — Rekonstruksi Arsitektur (dokumentasi)

**Status: keputusan terkunci & dokumentasi target selesai. Kode belum berubah.**

Keputusan arsitektur (user-confirmed):
- Topologi baru: `browser → nginx → { frontend (TanStack Start SSR), backend (.NET 8 REST, JWT bearer) } → PostgreSQL` — **satu kontainer per service** (nginx · frontend · api · postgres) dalam **monorepo** (`frontend/`, `src/`, `deploy/`, `docker-compose.yml`).
- Blazor **dipensiunkan**: selama transisi tetap hidup; **hapus `StockMonitorTso.Web` di R5**. Yang dipertahankan: Domain + Infrastructure (service, konservasi, seed, invoice) — bergeser di bawah API.
- Auth: JWT bearer; Identity Core tetap user store (hash/lockout/role); idle 15 menit = access-token + refresh aktivitas; switch-role = re-issue token via `/api/auth/switch-role`.
- Migrasi: backend-first R0–R5 (detail di `PLAN.md §4`); PostgreSQL dengan migrasi regenerasi penuh (`xmin` concurrency); seed via flag `Seed:SkipStock` + JSON mitra (workbook tidak ada di repo).
- Branch: `main` = main repository (dokumentasi lengkap + kode) · `apps` = production update point (app + deploy artifacts saja, tanpa dokumentasi).

File yang dibuat/diubah pada slice ini:
- `PLAN.md` — dibuat ulang: arsitektur target, layout monorepo, roadmap R0–R5 + gerbang, guardrails domain (tetap), auth model JWT, strategi data, risiko, open questions.
- `AGENTS.md` — dibuat ulang: runbook dev/prod 4 service, hard rules (konservasi, CD_n konseptual, satuan, RBAC service-layer, audit), traps, verification gate (dotnet + npm + compose smoke), strategi branch.
- `docs/PHASE_CHECKLIST.md` — header baru + **Phase R (R0–R5)** dengan checklist; Phase 5 digabung ke R5.
- `docs/CHECKLIST_DOD.md` — status Phase R, gerbang verifikasi baru (npm + compose), status Mitra CRUD (sudah ada).
- `docs/SESSION_HANDOFF.md` — slice ini.
- `architecture/` + `arsitektur/` — pembaruan parsial (topologi 01 baru EN+ID; 02–05 diedit parsial) — **dikecualikan dari sisa scope** atas arahan user; sisanya menyusul belakangan.

Langkah berikutnya:
1. **R0 — Groundwork**: Npgsql + regenerasi migrasi, endpoint auth JWT, compose api+postgres(+nginx), test Testcontainers.
2. Setelah R0 hijau → R1 REST surface → R2 React shell → dst. (lihat `PLAN.md §4`).

---

## 1. Masalah / fitur yang sedang dikerjakan

**Status: Phase 0–4 selesai & hijau. Phase 3 (CRUD + inventarisasi Agen/Outlet + pengurangan stok Opsi C) & Phase 4 (TSO wizard + snapshot harga + Docker PoC + rev. login) selesai & terverifikasi.**

Lingkup yang baru dituntaskan:
- **Phase 3 (CRUD Sales Area + Konservasi Stok)** + **redesign UI mengikuti design Stitch** (`stitch_dashboard_monitoring_stok_migas/`).
- **Inventarisasi Tier Agen & Outlet**: entitas `Agen` (2–3 per Gudang) & `Outlet` (2 per Agen, tanpa limit) bernama, baris stok per (Agen×Produk)/(Outlet×Produk), migrasi `Tier.Agen`→`GudangWilayah` + `Outlet` + filtered index 3 kasus, mock 50% (gudang→agen→outlet, audit Transfer), identitas Create/Update = Superadmin+Supervisi, halaman Daftar Agen/Outlet + Detail Agen/Outlet + Update Data Harian perukuran (Opsi C).
- **Transfer Gudang Wilayah → Agen & Agen → Outlet**: modal "Kirim ke Agen" (di Detail Gudang) & "Kirim ke Outlet" (di Detail Agen) — Superadmin+Supervisi, pilih 1 tujuan + qty per SKU (3 SKU LPG sekaligus / 1 minyak), loop `Transfer` atomic per SKU via `AgenService.TransferFromWarehouseAsync` & `OutletService.TransferFromAgenAsync`.
- **Rev. UI Gudang Wilayah (2026-08)**: card Gas LPG dikelompokkan → **1 card per wilayah memuat 3 ukuran tabung sekaligus** (chip titik warna 5.5/12/50). Detail LPG jadi **gabungan 3 ukuran** (route `/sales-area/{Wilayah}/Lpg`, tabel per Ukuran×Tier via `GetLpgDetailAsync`); `SalesAreaDetailRow.StokEntitasId` baru → update/transfer resolve langsung.
- **Pengurangan stok (Opsi C) (2026-08)**: `StockTransactionType.Issue` eksplisit (Qty>0 → `Stok-=Qty` + auto `StokHabisTerjual+=Qty`), `Adjust` opname ± (`Qty≠0`), `Receive`/`Transfer` tetap; modal **Update Data Harian** perukuran (LPG 3 baris + Minyak 1 baris: `Terjual`/`[☑]Opname`/`Intransit`/`Keterangan`).
- **Phase 4 — Modul TSO (2026-08)**: `MitraTso` seed dari `seeds/mitra-tso.json` (upsert, 3 mitra), `TransportOrder` (`OrderNo TSO-YYYYMMDD-XXXX`, snapshot `Tarif/SatuanTarif/EstimasiBiaya=Tarif×Qty`, `WilayahTujuan`, `RuteAsal/Tujuan`, `Produk`, `Kuantitas`, `Satuan`, `TglBerangkat`, `Eta+7`, `Status Committed/StockImpacted/FlagTertunda`, `RowVersion` concurrency, `IsDeleted`), wizard 4 langkah (Tujuan & Obyek → Rute & Jadwal → Transporter + Estimasi Biaya → Ringkasan), commit di Submit (T11) + idempotensi dedup 1 menit (T1/F9), dampak stok T5 (`RencanaKedatangan` NextSupply+ETA di Gudang Wilayah tujuan, F7 `FlagTertunda` + `/resync`), Preview read-only 8 kolom (§4.d), **Generate Draft Invoice idempoten** (QuestPDF, `CreationDate=CreatedAt`, bytes equal), Update (Supervisi) / Delete (Superadmin) + audit, `/api/tso` (MapGroup).
- **Deploy PoC**: `Dockerfile` multi-stage sdk→aspnet non-root `USER 1654`, self-signed cert 8081, `docker-compose.yml` literal `80:8080` + `443:8081`, volume `stockmonitor_data:/app/data` + `stockmonitor_keys:/app/keys`, seed xlsx+mitra di-copy ke image; fix `DataProtection` persist keys & `SeedData` `IsNullOrWhiteSpace`.
- **Rev. Login (2026-08)**: halaman login tanpa sidebar — `LoginLayout.razor` baru (tanpa `MainLayout`), `Login.razor` tanpa `ExternalLoginPicker` ("Use another service..."), `app.css` `sm-auth-*`.
- Perbaikan 4 temuan testing user: (1) Dashboard masih tabel → redesign card+chart; (2) filter objek tidak berfungsi → fix render mode interaktif + mock minyak tanah; (3) tombol Detail tidak berfungsi → fix routing enum + KPI per objek; (4) test integration gagal → fix fixture xUnit.

**Semua gerbang hijau terakhir:** `dotnet build -warnaserror` 0 error · `dotnet test` 91/91 (32 unit + 59 integration) · `dotnet format --verify` bersih · `docker compose build` OK · `curl -k https://localhost/health` 200 · smoke DB seed (Agen 18, Outlet 36, Mitra 3, Stok 272) lulus.

## 2. File yang diubah/dibuat

**Dokumen (source of truth):**
- `PLAN.md` — roadmap 6 fase produk Stock Monitor + TSO.
- `AGENTS.md` — persona Izi + repo guide (stack, traps, verification gate, referensi UI_REFERENCE).
- `STOCK_MONITORING_SPEC.md`, `TRANSPORT_SHIPPING_ORDER_SPEC.md` — spec ter-update (model §3.c, §5.f/g, guardrails, auth/sesi).
- `docs/UI_REFERENCE.md` — katalog 9 layar Stitch + design token (Material-3) + pemetaan page/fase. **Style-only** (data/scope desain TIDAK dipakai).
- `docs/REVISION_NOTE_stock_reduction.md` — **resolved Opsi C `Issue` eksplisit** (auto `Terjual`).
- `docs/DEVELOPER_GUIDE.md`, `docs/CHECKLIST_DOD.md`, `docs/CHANGE_PROCESS_NOTE.md`.

**Kode (src/StockMonitorTso.*):**
- Deploy: `Dockerfile` (multi-stage sdk→aspnet, non-root `USER 1654`, self-signed cert 8081, seed xlsx+mitra di-copy) · `docker-compose.yml` (literal `80:8080` + `443:8081`, volumes `stockmonitor_data:/app/data` + `stockmonitor_keys:/app/keys`) · `.dockerignore` · `.env.example`.
- `Web/Program.cs` — Identity, idle 15m, `AddDataProtection().PersistKeysToFileSystem("/app/keys")`, claims factory, DI services, auto-migrate+seed, `/health`, `MapRazorComponents`, `MapTsoEndpoints`.
- `Web/wwwroot/app.css` — design system `sm-*` + `sm-auth-*` (login).
- `Web/Components/Layout/{MainLayout,NavMenu,ActiveRoleSwitcher}.razor` — shell sidebar 288px + topbar + nav `sm-nav-item` + role switcher + link TSO.
- `Web/Components/Pages/Home.razor` — **Ringkasan Operasional** (Sektor KPI Gas/Minyak + chart bar per-wilayah + metrik table).
- `Web/Components/Pages/GudangWilayah.razor` — KPI overview + filter objek + kartu sales area (LPG 1 card/wilayah, 3 chip warna) + modal hapus.
- `Web/Components/Pages/DetailSalesArea.razor` — breadcrumb + KPI + tabel per-tier + Log Transaksi + modal Update Data Harian perukuran + modal Kirim ke Agen.
- `Web/Components/Pages/RegisterSalesArea.razor` — form card branching MT/LPG.
- `Web/Components/Pages/DaftarAgen.razor` (`/wilayah/{wilayah}/agen`) + `DaftarOutlet.razor` (`/agen/{id}/outlet`) — daftar + modal Tambah/Edit + Hapus.
- `Web/Components/Pages/DetailAgen.razor` (`/agen/{id}`) + `DetailOutlet.razor` (`/outlet/{id}`) — KPI + tabel per produk + Log + modal Update perukuran + modal Kirim ke Outlet.
- `Web/Components/Pages/Tso/{TsoList,TsoWizard,TsoPreview}.razor` (`/tso`, `/tso/create`, `/tso/{id}/edit`, `/tso/{id}`) — wizard 4 langkah + Preview 8 kolom + Generate Draft Invoice.
- `Web/Components/Pages/Admin/UserManagement.razor` + `Account/Shared/LoginLayout.razor` + `Account/Pages/Login.razor` (tanpa sidebar/external login).
- Domain: `Entities/{Agen,Outlet,MitraTso,TransportOrder,Wilayah,Produk,Tier,StokEntitas,RencanaKedatangan,StockTransaction,StockTransactionRecord,AuditLog}.cs`, `Services/StockCalculator.cs`, `Abstractions/IAuditLogService.cs`.
- Infrastructure: `Persistence/ApplicationDbContext.cs`, `Persistence/Migrations/*` (8 migrasi — `AddTsoModule`, `FixTsoRowVersion`), `Seed/{SeedData,ExcelStockSeeder,AgenMockSeeder,OutletMockSeeder,MitraTsoSeeder}.cs`, `Excel/XlsxReader.cs`, `Services/{StockWriteService,StockDashboardService,AgenService,OutletService,TransportOrderService,InvoiceGenerator,AuditLogService,UserAdminService,ActiveRoleClaimsPrincipalFactory}.cs`.
- Api: `Api/Endpoints/TsoEndpoints.cs` (`MapGroup /api/tso`).
- Tests: `UnitTests/{StockCalculatorTests,AgenMockSeederTests}.cs`, `IntegrationTests/{StockWriteTests,StockDashboardTests,AgenServiceTests,AgenDashboardTests,WarehouseTransferTests,TsoServiceTests,AuthAndRbacTests,AdminPageAccessTests,HealthCheckTests,TestWebApplicationFactory}.cs`.

## 3. Solusi / kesepakatan teknis

- **Stack**: .NET 8, C# 12, nullable on, `TreatWarningsAsErrors`, Blazor Server (`@rendermode InteractiveServer` di halaman fitur — **harus ada** agar event handler jalan), Minimal API `/api/*`, EF Core SQLite (MVP), ASP.NET Core Identity, QuestPDF (deterministik idempotent via `CreationDate=CreatedAt`), xUnit+FluentAssertions+NSubstitute+WebApplicationFactory.
- **Model domain**: `GudangWilayah/Outlet = (Wilayah×Produk×Tier)` + `Agen = (Agen×Produk)` + `Outlet = (Outlet×Produk)`; CD = Stok÷DOT; Status Kritis<3/Warning<7/Aman≥7; `CD_n = (sisa@ETA + NextSupply)÷DOT` (**konseptual, JANGAN tiru rumus Excel**); konservasi via atomic (Receive/Issue/Adjust±/Transfer), `Issue` auto `StokHabisTerjual`, tolak overdraft, audit.
- **TSO**: wizard 4 langkah (Tujuan&Obyek → Rute&Jadwal → Transporter+EstimasiBiaya `tarif×qty` → Ringkasan), commit di Submit (T11), idempotensi dedup 1 menit (T1/F9) + PDF bytes equal (T9), snapshot `Tarif/SatuanTarif/EstimasiBiaya` agar order lama tak berubah saat `Mitra.tarif` berubah, `RowVersion` concurrency (F8→409), `Status FlagTertunda` + `/resync` (F7), `RencanaKedatangan` (T5) Urutan 1..3.
- **RBAC**: multi-role switchable active role (claim role aktif saja), idle 15m, Superadmin-only assign role & password, Update=Superadmin+Supervisi, Delete=Superadmin, Register=Superadmin+Operator. **Identitas Agen/Outlet**: Create/Update=Superadmin+Supervisi, Delete=Superadmin (amend STOCK §3). **TSO**: Create=Superadmin+Operator, Update=Superadmin+Supervisi, Delete=Superadmin.
- **Granularitas stok**: Gudang Wilayah = `(Wilayah × Produk × Tier)` (`AgenId/OutletId IS NULL`); **Agen = `(Agen × Produk)`** via `AgenId`; **Outlet = `(Outlet × Produk)`** via `OutletId`. Index unik difilter 3 kasus. Mitra `AreaCoverage` → validasi `WilayahTujuan` (T4).
- **Mock agen/outlet (2026-08)**: 2–3 agen per gudang, 2 outlet per agen; stok awal agen = 50% gudang ÷ N, outlet = 50% agen ÷ 2; DOT dibagi rata; gudang→agen→outlet di-debit via `Transfer` audit.
- **Mock TSO**: 3 Mitra dari `seeds/mitra-tso.json` (upsert), tarif mutable diaudit.
- **Routing Detail**: gunakan **enum name** di URL (`Lpg5_5Kg`, `MinyakTanah`, `Lpg`) dan parse via `Enum.TryParse` (bukan `DisplayName`) — ini fix tombol Detail.
- **Test fixture**: `TestWebApplicationFactory` **parameterless** (xUnit `IClassFixture`); subclass `TestWebApplicationFactoryNoStock` untuk test yang meregister stok sendiri (seed stok di-skip via `Seed:SkipStock=true`).
- **Desain = style saja**: token/pola dari Stitch diadopsi; data/scope desain (gudang nasional Plumpang/TBBM, wilayah Jawa/Sumatera/Kalimantan, angka contoh) TIDAK dipakai. Brand app = "Stock Monitor dan TSO".
- **Deploy**: Docker multi-stage non-root, literal 80/443, self-signed cert 8081, volume data+keys, `DataProtection` persist `/app/keys`, seed password fix `IsNullOrWhiteSpace`.
- **Git**: branch `main` = lengkap (docs/spec/xlsx); branch `apps` = hanya aplikasi (src/tests/seeds/mitra-tso.json/Docker) — push `origin/apps` untuk deploy.

## 4. Langkah berikutnya (belum selesai)

1. **Phase 5 — Hardening**: sync balik ke repo `apps` (fix `Program.cs` AddDataProtection & `SeedData` `IsNullOrWhiteSpace` yang masih manual di VM), Serilog structured, ProblemDetails menyeluruh, `/ready`, `/metrics` (opsional), `dotnet format` CI, image <200MB, switch PostgreSQL (opsional).
2. **Open question (fitur baru dari desain, tercatat di `docs/UI_REFERENCE.md` §6)**: Proyeksi Dampak Stok chart, Ekspor Laporan, Live Sync — butuh persetujuan (TSO wizard, Gudang Wilayah, Monitoring Agen/Outlet transfer sudah done).
3. **Keputusan scope terdahulu yang belum dijawab**: rename solution (`StockMonitorTso.*`) vs tetap; pakai Excel acuan baru vs cabut — untuk kasus "perombakan total domain" (belum aktif).
4. **Mitra CRUD di app (Phase 5+) vs seed-only (Phase 4)**: harga dinamis via snapshot sudah handle; UI CRUD Mitra ditunda Phase 5.

## 5. Reminder phase per plan (`PLAN.md §4`)

- **Phase 0** Skeleton — ✅ selesai
- **Phase 1** Auth/RBAC/Sesi — ✅ selesai
- **Phase 2** Monitoring Stok (read+compute) — ✅ selesai
- **Phase 3** CRUD Sales Area + Konservasi (+ inventarisasi Agen/Outlet + pengurangan stok Opsi C + rev. card LPG) — ✅ selesai & terverifikasi
- **Phase 4** Modul TSO — ✅ selesai & terverifikasi (wizard, snapshot, PDF idempoten, /api/tso, Docker PoC, rev. login)
- **Phase 5** Hardening — ⏭ **berikutnya**

## 6. Cara verifikasi (gerbang per fase)

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
dotnet build StockMonitorTso.sln -warnaserror
dotnet test StockMonitorTso.sln
dotnet format StockMonitorTso.sln --verify-no-changes
dotnet run --project src/StockMonitorTso.Web --launch-profile http   # http://localhost:5110
docker compose build && docker compose up -d && curl -k https://localhost/health # PoC 80:8080 443:8081
```

Akun seed: `superadmin@stockmonitor.local` / `Superadmin!2345` (Operator/Supervisi/Tamu/multi ada di `SeedData`).

---

## 7. Slice 2026-09 — Buat user baru oleh Superadmin

Lingkup yang dituntaskan (di luar fase 5):

- **Service**: `IUserAdminService.CreateUserAsync(actor, email, password, roles[], activeRole)` — `RequireSuperadmin`; validasi email unik, roles terdaftar di `RoleManager`, `activeRole` anggota `roles`; `EmailConfirmed = true`; `AddToRolesAsync`; audit log `CreateUser` (`EntityType="ApplicationUser"`, `After=roles joined`).
- **UI**: `/admin/users` direstylye ke design system `sm-*` (`sm-table`, `sm-pill`, `sm-segmented`, `sm-btn`, `sm-modal`); tombol "Tambah User" → modal dengan field email/password/konfirmasi + checklist role + dropdown "Role Aktif" (terbatas ke role tercentang); refresh daftar + alert status (`sm-alert-success/error`).
- **Hapus scaffold berbahaya**: `Web/Components/Account/Pages/Register.razor` + `RegisterConfirmation.razor` dihapus — sebelumnya orphan, Superadmin-gated, dan `SignInManager.SignInAsync(user)` menimpa cookie Superadmin saat submit (bug latent). ExternalLogin tidak disentuh (sudah unreachable dari Login).
- **Tests**: `tests/StockMonitorTso.IntegrationTests/UserAdminCreateTests.cs` — 7 test: RBAC (Superadmin OK; Operator/Supervisi/Tamu ditolak & tak tercipta), duplikat email, password lemah (< 8), role tak dikenal, activeRole ∉ roles, audit tercatat (`After=roles joined`, `ActorRole=Superadmin`), sign-in-able dengan password awal.
- **Fix 2026-09 — crash circuit `/admin/users`**: `StatusMessage.razor` `HttpContext` dibuat nullable + guard `if (HttpContext is null) return;` di `OnInitialized` (sebelumnya NRE saat dirender interactive — `HttpContext` null di circuit). `<StatusMessage>` di `UserManagement.razor` diganti alert inline `sm-alert-success/error` (CSS baru di `app.css`). Root cause: halaman interactive satu-satunya yang menyentuh komponen Account static-SSR; halaman Identity static lain tidak terdampak.
- **Fix 2026-09 — modal di belakang blur**: modal "Tambah User" kini dibungkus wrapper `position:fixed;inset:0;z-index:110;display:flex;align-items:center;justify-content:center;overflow-y:auto` (pola yang sama dengan halaman lain, mis. `DaftarAgen.razor:108`) — sebelumnya `.sm-modal` tanpa wrapper berada di alur dokumen normal di bawah backdrop blur (fixed, z-index 100) sehingga tidak bisa diklik. Backdrop tetap `@onclick` close.
- **Rev. UI sidebar (2026-09)**: `.sm-sidebar-brand` jadi kolom (`flex-direction:column; align-items:flex-start`) — logo Pertamina di atas teks "Stock Monitor & TSO", rata kiri, teks tidak lagi wrap dua baris.
- **Bug tertinggal**: `ActiveRoleSwitcher` di layout `NavMenu` masih non-fungsional (layout static; render mode per-halaman tidak propagate ke parent layout — `UserManagement.razor` InteractiveServer sendiri OK). Slice terpisah.

Gerbang: `dotnet build -warnaserror` 0 error · 7 test baru hijau (61 integration lama) · 5 test prasejarah `StockDashboardTests`/`AgenDashboardTests` masih gagal (xlsx `Monitoring Tabung RPM(1).xlsx` di-gitignore di `apps`; sama tanpa slice ini) · `dotnet format --verify` bersih · smoke `/health` 200 · `/admin/users` redirect login untuk anonim.
