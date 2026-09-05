import { useQuery } from '@tanstack/react-query'
import { createFileRoute } from '@tanstack/react-router'
import { apiFetch } from '../lib/api'
import { wilayahDisplay } from '../lib/data'
import type { ChartPointRow, RingkasanResponse } from '../lib/data'

export const Route = createFileRoute('/_app/')({
  component: DashboardPage,
})

function ringkasanMax(points: ChartPointRow[]): number {
  const max = points.reduce((acc, p) => Math.max(acc, p.agen, p.outlet), 0)
  return max <= 0 ? 1 : max
}

function pct(value: number, max: number): number {
  return Math.max(2, Math.round((value / max) * 100))
}

function formatKl(value: number | null): string {
  return value === null ? '-' : value.toFixed(3).replace(/\.?0+$/, '')
}

function ChartCard({
  title,
  iconColor,
  points,
  agenColor,
}: {
  title: string
  iconColor: string
  points: ChartPointRow[]
  agenColor: string
}) {
  const max = ringkasanMax(points)
  return (
    <div className="sm-card">
      <div className="sm-title-sm" style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 12 }}>
        <span className="material-symbols-outlined" style={{ color: iconColor }}>
          bar_chart
        </span>{' '}
        {title}
      </div>
      {points.length === 0 ? (
        <div className="sm-body-md" style={{ textAlign: 'center', padding: 20 }}>
          Belum ada data.
        </div>
      ) : (
        <>
          <div className="sm-chart">
            {points.map((p) => (
              <div className="sm-chart-col" key={p.label}>
                <div className="sm-bars">
                  <div
                    className={`sm-bar sm-bar-current ${p.critical ? 'sm-crit' : ''}`}
                    style={{ height: `${pct(p.agen, max)}%` }}
                  />
                  <div className="sm-bar sm-bar-target" style={{ height: `${pct(p.outlet, max)}%` }} />
                </div>
                <div className={`sm-chart-label ${p.critical ? 'sm-crit-label' : ''}`}>{p.label}</div>
              </div>
            ))}
          </div>
          <div style={{ display: 'flex', justifyContent: 'center', gap: 16, marginTop: 12 }}>
            <span className="sm-body-md" style={{ fontSize: 12 }}>
              <span
                className="sm-bar"
                style={{
                  display: 'inline-block',
                  width: 14,
                  height: 14,
                  borderRadius: 3,
                  background: agenColor,
                  verticalAlign: 'middle',
                }}
              />{' '}
              Stok Agen
            </span>
            <span className="sm-body-md" style={{ fontSize: 12 }}>
              <span
                className="sm-bar"
                style={{
                  display: 'inline-block',
                  width: 14,
                  height: 14,
                  borderRadius: 3,
                  background: 'var(--sm-surface-container-highest)',
                  verticalAlign: 'middle',
                }}
              />{' '}
              Stok Outlet
            </span>
          </div>
        </>
      )}
    </div>
  )
}

function DashboardPage() {
  const ringkasan = useQuery({
    queryKey: ['ringkasan'],
    queryFn: () => apiFetch<RingkasanResponse>('/api/dashboard/ringkasan'),
  })

  return (
    <div>
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'flex-end',
          justifyContent: 'space-between',
          gap: 16,
          marginBottom: 20,
        }}
      >
        <div>
          <h1 className="sm-display-lg">Ringkasan Operasional</h1>
          <p className="sm-body-md" style={{ maxWidth: 660, margin: '4px 0 0' }}>
            Visibilitas tingkat stok bahan bakar per Sales Area dan status distribusi kritis.
          </p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span className="sm-pill sm-pill-neutral" style={{ gap: 8 }}>
            <span
              style={{
                width: 9,
                height: 9,
                borderRadius: '50%',
                background: 'var(--sm-success)',
                boxShadow: '0 0 8px var(--sm-success)',
              }}
            />
            Live Sync
          </span>
          <a className="sm-btn sm-btn-primary sm-btn-sm" href="/gudang-wilayah">
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>
              dashboard
            </span>{' '}
            Buka Gudang Wilayah
          </a>
        </div>
      </div>

      {ringkasan.isPending ? (
        <div className="sm-body-md" style={{ padding: '40px 0' }}>
          Memuat...
        </div>
      ) : ringkasan.error ? (
        <div className="sm-alert sm-alert-error">{ringkasan.error.message}</div>
      ) : (
        ringkasan.data && (
          <>
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit,minmax(280px,1fr))',
                gap: 16,
                marginBottom: 20,
              }}
            >
              {[
                { sektor: ringkasan.data.gas, icon: 'propane', color: 'var(--sm-primary)', bg: 'var(--sm-surface-container-highest)' },
                { sektor: ringkasan.data.minyak, icon: 'oil_barrel', color: 'var(--sm-secondary)', bg: 'var(--sm-surface-container-highest)' },
              ].map(({ sektor, icon, color }) => (
                <div className="sm-card" key={sektor.nama} style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <div>
                      <div className="sm-label-caps" style={{ opacity: 0.8 }}>
                        Sektor
                      </div>
                      <div className="sm-title-sm" style={{ margin: '6px 0 2px' }}>
                        {sektor.nama}
                      </div>
                    </div>
                    <div className="sm-kpi-icon" style={{ color, background: 'var(--sm-surface-container-highest)' }}>
                      <span className="material-symbols-outlined">{icon}</span>
                    </div>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'baseline', gap: 8 }}>
                    <span style={{ fontSize: 40, fontWeight: 700, color }}>
                      {sektor.totalStok.toLocaleString('id-ID')}
                    </span>
                    <span className="sm-body-md">{sektor.unit}</span>
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span className="sm-body-md" style={{ fontSize: 12 }}>
                      {sektor.outletKritis} Outlet Defisit Stok (CD &lt; 3)
                    </span>
                    <span
                      className={`sm-pill ${
                        sektor.statusSektor === 'Kritis'
                          ? 'sm-pill-danger'
                          : sektor.statusSektor === 'Warning'
                            ? 'sm-pill-warning'
                            : 'sm-pill-success'
                      }`}
                    >
                      {sektor.statusSektor ?? '-'}
                    </span>
                  </div>
                </div>
              ))}
            </div>

            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit,minmax(380px,1fr))',
                gap: 16,
                marginBottom: 20,
              }}
            >
              <ChartCard
                title="Tren Stok Gas Tabung"
                iconColor="var(--sm-primary)"
                points={ringkasan.data.gasChart}
                agenColor="var(--sm-primary)"
              />
              <ChartCard
                title="Tren Stok Minyak Tanah"
                iconColor="var(--sm-secondary)"
                points={ringkasan.data.minyakChart}
                agenColor="var(--sm-secondary)"
              />
            </div>

            <div className="sm-table-wrap">
              <div
                className="sm-label-caps"
                style={{
                  padding: '14px var(--sm-pad)',
                  borderBottom: '1px solid var(--sm-outline-variant)',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <span style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                  <span className="material-symbols-outlined" style={{ color: 'var(--sm-secondary)' }}>
                    oil_barrel
                  </span>{' '}
                  Metrik Minyak Tanah
                </span>
                <a href="/gudang-wilayah" style={{ color: 'var(--sm-primary)', textTransform: 'none', letterSpacing: 0 }}>
                  Lihat Semua
                </a>
              </div>
              <div style={{ overflowX: 'auto' }}>
                <table className="sm-table">
                  <thead>
                    <tr>
                      <th>No.</th>
                      <th>Nama Sales Area</th>
                      <th>Realisasi Tanggal</th>
                      <th className="num">Sisa Stok Agen</th>
                      <th className="num">Sisa Outlet</th>
                      <th className="num">Terjual</th>
                      <th className="num">Intransit</th>
                      <th>Status</th>
                      <th>Keterangan</th>
                    </tr>
                  </thead>
                  <tbody>
                    {ringkasan.data.metrikMinyak.map((row, index) => {
                      const worst =
                        row.statusAgen === 'Kritis' || row.statusOutlet === 'Kritis'
                          ? 'Kritis'
                          : row.statusAgen === 'Warning' || row.statusOutlet === 'Warning'
                            ? 'Warning'
                            : row.statusAgen ?? row.statusOutlet ?? null
                      const worstClass =
                        worst === 'Kritis'
                          ? 'sm-pill-danger'
                          : worst === 'Warning'
                            ? 'sm-pill-warning'
                            : 'sm-pill-success'
                      return (
                        <tr key={row.wilayah}>
                          <td>{index + 1}</td>
                          <td>{wilayahDisplay(row.wilayah)}</td>
                          <td>{new Date(row.tanggal).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                          <td className="num">{formatKl(row.stokAgen)}</td>
                          <td className="num">{formatKl(row.stokOutlet)}</td>
                          <td className="num">{formatKl(row.stokHabisTerjual)}</td>
                          <td className="num">{formatKl(row.stokIntransit)}</td>
                          <td>
                            <span className={`sm-pill ${worstClass}`}>{worst ?? '-'}</span>
                          </td>
                          <td>{row.keterangan}</td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        )
      )}
    </div>
  )
}
