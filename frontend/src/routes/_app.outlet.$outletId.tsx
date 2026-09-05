import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Modal } from '../components/Modal'
import { RoleGate } from '../components/RoleGate'
import { StatusPill, statusText } from '../components/StatusPill'
import { data, produkDisplay, stock } from '../lib/data'
import type { Produk } from '../lib/data'
import { QtyInput } from '../components/QtyInput'

export const Route = createFileRoute('/_app/outlet/$outletId')({
  component: DetailOutletPage,
})

const ENTRIES: { produk: Produk; key: string; label: string }[] = [
  { produk: 'Lpg5_5Kg', key: '55', label: '5.5 kg' },
  { produk: 'Lpg12Kg', key: '12', label: '12 kg' },
  { produk: 'Lpg50Kg', key: '50', label: '50 kg' },
  { produk: 'MinyakTanah', key: 'Minyak', label: 'Minyak Tanah' },
]

function DetailOutletPage() {
  const { outletId } = Route.useParams()
  const outletIdNumber = Number(outletId)
  const queryClient = useQueryClient()

  const [showUpdateModal, setShowUpdateModal] = useState(false)
  const [updateMode, setUpdateMode] = useState<'restock' | 'daily'>('restock')
  const [selectedProdukIndex, setSelectedProdukIndex] = useState(0)
  const [restockQty, setRestockQty] = useState(0)
  const [restockDate, setRestockDate] = useState(() => new Date().toISOString().split('T')[0])
  const [daily, setDaily] = useState<Record<string, { terjual: number; opnameEnabled: boolean; opname: number; intransit: number }>>(() =>
    Object.fromEntries(ENTRIES.map((e) => [e.key, { terjual: 0, opnameEnabled: false, opname: 0, intransit: 0 }])),
  )
  const [dailyKeterangan, setDailyKeterangan] = useState('')
  const [formMessage, setFormMessage] = useState<string | null>(null)

  const detail = useQuery({
    queryKey: ['outlet-detail', outletIdNumber],
    queryFn: () => data.outletDetail(outletIdNumber),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['outlet-detail', outletIdNumber] })
    queryClient.invalidateQueries({ queryKey: ['outlet-inventaris'] })
    queryClient.invalidateQueries({ queryKey: ['cards'] })
    queryClient.invalidateQueries({ queryKey: ['summary'] })
  }

  const transactMutation = useMutation({
    mutationFn: (args: { id: number; type: 'Receive' | 'Issue' | 'Adjust'; kuantitas: number; catatan?: string }) =>
      stock.transact(args.id, args.type, args.kuantitas, undefined, args.catatan),
  })

  const detailData = detail.data

  function openUpdateModal() {
    setDaily(Object.fromEntries(ENTRIES.map((e) => [e.key, { terjual: 0, opnameEnabled: false, opname: 0, intransit: 0 }])))
    setDailyKeterangan('')
    setFormMessage(null)
    setShowUpdateModal(true)
  }

  function saveUpdate() {
    if (!detailData) return

    if (updateMode === 'restock') {
      const row = detailData.rows[selectedProdukIndex]
      if (restockQty <= 0) return
      transactMutation.mutate(
        { id: row.stokEntitasId, type: 'Receive', kuantitas: restockQty, catatan: 'Isi ulang stok' },
        {
          onError: (error: Error) => setFormMessage(error.message),
          onSuccess: () => { setShowUpdateModal(false); invalidate() },
        },
      )
      return
    }

    void (async () => {
      try {
        for (const entry of ENTRIES) {
          const state = daily[entry.key]
          const row = detailData.rows.find((r) => r.produk === entry.produk)
          if (!row || !state) continue
          const perluIntransit = state.intransit !== (row.stokIntransit ?? 0)
          const perluKeterangan = dailyKeterangan !== ''
          if (perluIntransit || perluKeterangan) {
            await stock.updateDetail(row.stokEntitasId, {
              dot: row.dot,
              tanggalStokAwal: row.tanggalStokAwal.split('T')[0],
              stokHabisTerjual: row.stokHabisTerjual,
              stokIntransit: state.intransit,
              keterangan: dailyKeterangan,
            })
          }
          if (state.terjual > 0) {
            await stock.transact(row.stokEntitasId, 'Issue', state.terjual, undefined, dailyKeterangan || 'Stok terjual harian')
          }
          if (state.opnameEnabled && state.opname !== 0) {
            await stock.transact(row.stokEntitasId, 'Adjust', state.opname, undefined, `Opname ${dailyKeterangan}`.trim())
          }
        }
        setShowUpdateModal(false)
        invalidate()
      } catch (error) {
        setFormMessage(error instanceof Error ? error.message : 'Gagal menyimpan.')
      }
    })()
  }

  if (detail.isPending) {
    return <div className="sm-card" style={{ padding: 24 }}>Memuat...</div>
  }

  if (!detailData) {
    return <div className="sm-card" style={{ padding: 24 }}>Data tidak ditemukan.</div>
  }

  return (
    <div>
      <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 16, marginBottom: 20 }}>
        <div>
          <div className="sm-breadcrumb">
            <Link to="/gudang-wilayah">Gudang Wilayah</Link>
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
            <Link to="/agen/$agenId" params={{ agenId: String(detailData.agenId) }}>Agen</Link>
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
            <span style={{ color: 'var(--sm-on-surface)' }}>{detailData.nama}</span>
          </div>
          <h1 className="sm-headline-md" style={{ marginTop: 6 }}>Inventori Outlet — {detailData.nama}</h1>
          <p className="sm-body-md" style={{ margin: '4px 0 0' }}>
            Sales Area {detailData.wilayah} — Realisasi dan pemantauan stok harian.
          </p>
        </div>
        <RoleGate roles={['Superadmin', 'Supervisi']}>
          <button className="sm-btn sm-btn-primary" type="button" onClick={openUpdateModal}>
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>sync</span> Update Data Harian
          </button>
        </RoleGate>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit,minmax(200px,1fr))', gap: 16, marginBottom: 20 }}>
        <div className="sm-kpi">
          <span className="sm-label-caps">Total Sisa Stok</span>
          <div><span className="value">{detailData.totalStok.toLocaleString('id-ID')}</span> <span className="unit">satuan</span></div>
        </div>
        <div className="sm-kpi">
          <span className="sm-label-caps">Daily Obj. Throughput</span>
          <div><span className="value" style={{ fontSize: 26 }}>{detailData.totalDot.toFixed(1)}</span> <span className="unit">/hari</span></div>
        </div>
        <div className="sm-kpi">
          <span className="sm-label-caps">Covered Days Estimasi</span>
          <div><span className="value" style={{ fontSize: 26 }}>{detailData.cdTerburuk?.toFixed(2) ?? '-'}</span> <span className="unit">hari</span></div>
        </div>
        <div className={`sm-kpi ${detailData.statusArea === 'Kritis' ? 'sm-kpi-kritis' : ''}`}>
          <span className="sm-label-caps">Status Area</span>
          <div><span className="value" style={{ fontSize: 26 }}>{statusText(detailData.statusArea)}</span></div>
        </div>
      </div>

      <div className="sm-table-wrap" style={{ marginBottom: 20 }}>
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
          <span>Data Inventory per Produk</span>
          <span style={{ textTransform: 'none', letterSpacing: 0 }}>
            Exhaust terdekat:{' '}
            {detailData.exhaustTerdekat
              ? new Date(detailData.exhaustTerdekat).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })
              : '-'}
          </span>
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table className="sm-table">
            <thead>
              <tr>
                <th>Produk</th>
                <th className="num">Stok</th>
                <th className="num">DOT</th>
                <th className="num">Covered Days</th>
                <th>Status</th>
                <th>Exhaust</th>
              </tr>
            </thead>
            <tbody>
              {detailData.rows.map((row) => (
                <tr key={row.stokEntitasId}>
                  <td>{produkDisplay(row.produk)}</td>
                  <td className="num">{row.stok.toLocaleString('id-ID')}</td>
                  <td className="num">{row.dot.toFixed(2).replace(/\.?0+$/, '')}</td>
                  <td className="num">{row.cd?.toFixed(2).replace(/\.?0+$/, '') ?? 'N/A'}</td>
                  <td><StatusPill status={row.status} /></td>
                  <td>
                    {row.exhaustDate
                      ? new Date(row.exhaustDate).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })
                      : ''}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="sm-table-wrap">
        <div className="sm-label-caps" style={{ padding: '14px var(--sm-pad)', borderBottom: '1px solid var(--sm-outline-variant)' }}>
          Log Transaksi Stok
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table className="sm-table">
            <thead>
              <tr>
                <th>Tanggal</th>
                <th>Jenis</th>
                <th className="num">Kuantitas</th>
                <th>Catatan</th>
                <th className="num">Stok Sesudah</th>
              </tr>
            </thead>
            <tbody>
              {detailData.transactions.length === 0 ? (
                <tr><td colSpan={5} style={{ textAlign: 'center', color: 'var(--sm-on-surface-variant)' }}>Belum ada transaksi.</td></tr>
              ) : (
                detailData.transactions.map((t, index) => (
                  <tr key={index}>
                    <td>{new Date(t.tanggal).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                    <td>{t.type}</td>
                    <td className="num">{t.kuantitas.toFixed(3).replace(/\.?0+$/, '')}</td>
                    <td>{t.catatan ?? '-'}</td>
                    <td className="num">{t.stokSesudah.toFixed(3).replace(/\.?0+$/, '')}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {showUpdateModal ? (
        <Modal
          title="Update Data Harian"
          onClose={() => { setShowUpdateModal(false); setFormMessage(null) }}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => { setShowUpdateModal(false); setFormMessage(null) }}>Batal</button>
              <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" onClick={saveUpdate}>Simpan Perubahan</button>
            </>
          }
        >
          <div className="sm-segmented" style={{ marginBottom: 16 }}>
            <button type="button" className={updateMode === 'restock' ? 'active' : ''} onClick={() => setUpdateMode('restock')}>Isi Ulang Stok</button>
            <button type="button" className={updateMode === 'daily' ? 'active' : ''} onClick={() => setUpdateMode('daily')}>Update Stok Harian</button>
          </div>

          <div style={{ marginBottom: 12 }}>
            <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Produk tujuan</label>
            <select className="sm-filter" style={{ width: '100%' }} value={selectedProdukIndex} onChange={(e) => setSelectedProdukIndex(Number(e.target.value))}>
              {detailData.rows.map((row, index) => (
                <option key={row.stokEntitasId} value={index}>{produkDisplay(row.produk)}</option>
              ))}
            </select>
          </div>

          {updateMode === 'restock' ? (
            <>
              <div style={{ marginBottom: 12 }}>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Kuantitas tambah (isi ulang)</label>
                <QtyInput className="sm-frame" min={0} step={1} style={{ width: '100%' }} value={restockQty} onChange={(setRestockQty)} />
              </div>
              <div style={{ marginBottom: 12 }}>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Tanggal Realisasi</label>
                <input type="date" className="sm-frame" style={{ width: '100%' }} value={restockDate} onChange={(e) => setRestockDate(e.target.value)} />
              </div>
            </>
          ) : (
            <>
              {ENTRIES.map((entry) => {
                const exists = detailData.rows.some((r) => r.produk === entry.produk)
                if (!exists) return null
                const state = daily[entry.key]
                const unit = entry.produk === 'MinyakTanah' ? '(Kiloliter)' : '(Tabung)'
                const chipClass = entry.key === '55' ? 'chip-55' : entry.key === '12' ? 'chip-12' : entry.key === '50' ? 'chip-50' : ''
                return (
                  <div key={entry.key} style={{ border: '1px solid var(--sm-outline-variant)', borderRadius: 'var(--sm-radius)', padding: 10, marginBottom: 12 }}>
                    <div className="sm-label-caps" style={{ marginBottom: 8 }}>
                      <span className={`sm-chip ${chipClass}`}>
                        <span className="dot" />
                        {entry.label}
                      </span>
                    </div>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
                      <div>
                        <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Stok Terjual {unit}</label>
                        <QtyInput className="sm-frame" min={0} step={entry.produk === 'MinyakTanah' ? 0.1 : 1} style={{ width: '100%', padding: 8 }} value={state.terjual} onChange={(next) => setDaily((prev) => ({ ...prev, [entry.key]: { ...prev[entry.key], terjual: next } }))} />
                      </div>
                      <div>
                        <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Stok Intransit {unit}</label>
                        <QtyInput className="sm-frame" min={0} step={entry.produk === 'MinyakTanah' ? 0.1 : 1} style={{ width: '100%', padding: 8 }} value={state.intransit} onChange={(next) => setDaily((prev) => ({ ...prev, [entry.key]: { ...prev[entry.key], intransit: next } }))} />
                      </div>
                    </div>
                    <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 8, cursor: 'pointer' }}>
                      <input type="checkbox" checked={state.opnameEnabled} onChange={(e) => setDaily((prev) => ({ ...prev, [entry.key]: { ...prev[entry.key], opnameEnabled: e.target.checked } }))} />{' '}
                      <span className="sm-label-caps">Stok Opname {entry.produk === 'MinyakTanah' ? '(± Kiloliter)' : ''}</span>
                    </label>
                                          <QtyInput
                        className="sm-frame"
                        step={entry.produk === 'MinyakTanah' ? 0.1 : 1}
                        disabled={!state.opnameEnabled}
                        style={{ width: '100%', padding: 8, marginTop: 4, background: state.opnameEnabled ? 'var(--sm-surface-bright)' : 'var(--sm-surface-container)' }}
                        value={state.opname}
                        onChange={(next) => setDaily((prev) => ({ ...prev, [entry.key]: { ...prev[entry.key], opname: next } }))}
                      />
                  </div>
                )
              })}
              <div style={{ marginBottom: 12 }}>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Keterangan</label>
                <input type="text" className="sm-frame" style={{ width: '100%' }} value={dailyKeterangan} onChange={(e) => setDailyKeterangan(e.target.value)} />
              </div>
            </>
          )}
          {formMessage ? (
            <div style={{ margin: '12px 0 0', padding: 10, borderRadius: 'var(--sm-radius)', background: 'var(--sm-error-container)', color: 'var(--sm-on-error-container)' }}>
              {formMessage}
            </div>
          ) : null}
        </Modal>
      ) : null}
    </div>
  )
}
