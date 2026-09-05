import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Modal } from '../components/Modal'
import { RoleGate } from '../components/RoleGate'
import { StatusPill, statusText } from '../components/StatusPill'
import { data, outlet, produkDisplay, stock } from '../lib/data'
import type { Produk } from '../lib/data'
import { QtyInput } from '../components/QtyInput'

export const Route = createFileRoute('/_app/agen/$agenId/')({
  component: DetailAgenPage,
})

const ENTRIES: { produk: Produk; key: string }[] = [
  { produk: 'Lpg5_5Kg', key: '55' },
  { produk: 'Lpg12Kg', key: '12' },
  { produk: 'Lpg50Kg', key: '50' },
  { produk: 'MinyakTanah', key: 'Minyak' },
]

function DailyEntry({
  produk,
  label,
  values,
  onChange,
}: {
  produk: Produk
  label: string
  values: Record<string, { terjual: number; opnameEnabled: boolean; opname: number; intransit: number }>
  onChange: (key: string, patch: Partial<{ terjual: number; opnameEnabled: boolean; opname: number; intransit: number }>) => void
}) {
  const key = produk === 'Lpg5_5Kg' ? '55' : produk === 'Lpg12Kg' ? '12' : produk === 'Lpg50Kg' ? '50' : 'Minyak'
  const v = values[key]
  const chipClass = produk === 'Lpg5_5Kg' ? 'chip-55' : produk === 'Lpg12Kg' ? 'chip-12' : produk === 'Lpg50Kg' ? 'chip-50' : ''
  const unit = produk === 'MinyakTanah' ? '(Kiloliter)' : '(Tabung)'
  if (!v) return null
  return (
    <div style={{ border: '1px solid var(--sm-outline-variant)', borderRadius: 'var(--sm-radius)', padding: 10 }}>
      <div className="sm-label-caps" style={{ marginBottom: 8 }}>
        <span className={`sm-chip ${chipClass}`}>
          <span className="dot" />
          {label}
        </span>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
        <div>
          <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Stok Terjual {unit}</label>
          <QtyInput className="sm-frame" min={0} step={produk === 'MinyakTanah' ? 0.1 : 1} style={{ width: '100%', padding: 8 }} value={v.terjual} onChange={(next) => onChange(key, { terjual: next })} />
        </div>
        <div>
          <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Stok Intransit {unit}</label>
          <QtyInput className="sm-frame" min={0} step={produk === 'MinyakTanah' ? 0.1 : 1} style={{ width: '100%', padding: 8 }} value={v.intransit} onChange={(next) => onChange(key, { intransit: next })} />
        </div>
      </div>
      <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 8, cursor: 'pointer' }}>
        <input type="checkbox" checked={v.opnameEnabled} onChange={(e) => onChange(key, { opnameEnabled: e.target.checked })} />{' '}
        <span className="sm-label-caps">Stok Opname {produk === 'MinyakTanah' ? '(± Kiloliter)' : ''}</span>
      </label>
      <QtyInput
        className="sm-frame"
        step={produk === 'MinyakTanah' ? 0.1 : 1}
        disabled={!v.opnameEnabled}
        style={{ width: '100%', padding: 8, marginTop: 4, background: v.opnameEnabled ? 'var(--sm-surface-bright)' : 'var(--sm-surface-container)' }}
        value={v.opname}
        onChange={(next) => onChange(key, { opname: next })}
      />
    </div>
  )
}

function DetailAgenPage() {
  const { agenId } = Route.useParams()
  const agenIdNumber = Number(agenId)
  const queryClient = useQueryClient()

  const [showUpdateModal, setShowUpdateModal] = useState(false)
  const [updateMode, setUpdateMode] = useState<'restock' | 'daily'>('restock')
  const [selectedProdukIndex, setSelectedProdukIndex] = useState(0)
  const [restockQty, setRestockQty] = useState(0)
  const [restockDate, setRestockDate] = useState(() => new Date().toISOString().split('T')[0])
  const [daily, setDaily] = useState<Record<string, { terjual: number; opnameEnabled: boolean; opname: number; intransit: number }>>(() =>
    Object.fromEntries(
      ENTRIES.map((e) => [e.key, { terjual: 0, opnameEnabled: false, opname: 0, intransit: 0 }]),
    ),
  )
  const [dailyKeterangan, setDailyKeterangan] = useState('')
  const [formMessage, setFormMessage] = useState<string | null>(null)

  const [showTransferModal, setShowTransferModal] = useState(false)
  const [outletTargets, setOutletTargets] = useState<Awaited<ReturnType<typeof data.outletTransferTargets>>>([])
  const [selectedOutletIndex, setSelectedOutletIndex] = useState(0)
  const [transferQty, setTransferQty] = useState({ q55: 0, q12: 0, q50: 0, qMinyak: 0 })
  const [transferMessage, setTransferMessage] = useState<string | null>(null)

  const detail = useQuery({
    queryKey: ['agen-detail', agenIdNumber],
    queryFn: () => data.agenDetail(agenIdNumber),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['agen-detail', agenIdNumber] })
    queryClient.invalidateQueries({ queryKey: ['agen-inventaris'] })
    queryClient.invalidateQueries({ queryKey: ['cards'] })
    queryClient.invalidateQueries({ queryKey: ['summary'] })
  }

  const transactMutation = useMutation({
    mutationFn: (args: { id: number; type: 'Receive' | 'Issue' | 'Adjust'; kuantitas: number; catatan?: string }) =>
      stock.transact(args.id, args.type, args.kuantitas, undefined, args.catatan),
  })
  const transferMutation = useMutation({
    mutationFn: (args: { outletId: number; quantities: Record<string, number>; catatan: string }) =>
      outlet.transferFromAgen(agenIdNumber, args.outletId, args.quantities, args.catatan),
    onSuccess: () => {
      setShowTransferModal(false)
      invalidate()
    },
    onError: (error: Error) => setTransferMessage(error.message),
  })

  const detailData = detail.data

  function openUpdateModal() {
    setDaily(Object.fromEntries(ENTRIES.map((e) => [e.key, { terjual: 0, opnameEnabled: false, opname: 0, intransit: 0 }])))
    setDailyKeterangan('')
    setFormMessage(null)
    setShowUpdateModal(true)
  }

  async function openTransferModal() {
    const targets = await data.outletTransferTargets(agenIdNumber)
    setOutletTargets(targets)
    if (targets.length === 0) {
      setTransferMessage('Belum ada outlet terdaftar di agen ini.')
      setShowTransferModal(true)
      return
    }
    setSelectedOutletIndex(0)
    setTransferQty({ q55: 0, q12: 0, q50: 0, qMinyak: 0 })
    setTransferMessage(null)
    setShowTransferModal(true)
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

  function confirmTransfer() {
    if (outletTargets.length === 0 || !detailData) return
    const target = outletTargets[selectedOutletIndex]
    const quantities: Record<string, number> = {}
    if (transferQty.q55 > 0) quantities.Lpg5_5Kg = transferQty.q55
    if (transferQty.q12 > 0) quantities.Lpg12Kg = transferQty.q12
    if (transferQty.q50 > 0) quantities.Lpg50Kg = transferQty.q50
    if (transferQty.qMinyak > 0) quantities.MinyakTanah = transferQty.qMinyak

    transferMutation.mutate(
      {
        outletId: target.outletId,
        quantities,
        catatan: `Transfer dari Agen ${detailData.nama} ke ${target.nama}`,
      },
    )
  }

  const transferStok = (produk: Produk): number =>
    detailData?.rows.find((r) => r.produk === produk)?.stok ?? 0

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
            <Link to="/wilayah/$wilayah/agen" params={{ wilayah: detailData.wilayah }}>
              Agen {detailData.wilayah}
            </Link>
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
            <span style={{ color: 'var(--sm-on-surface)' }}>{detailData.nama}</span>
          </div>
          <h1 className="sm-headline-md" style={{ marginTop: 6 }}>Inventori Agen — {detailData.nama}</h1>
          <p className="sm-body-md" style={{ margin: '4px 0 0' }}>
            Sales Area {detailData.wilayah} — Realisasi dan pemantauan stok harian.
          </p>
        </div>
        <RoleGate roles={['Superadmin', 'Supervisi']}>
          <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
            <Link className="sm-btn sm-btn-outline" to="/agen/$agenId/outlet" params={{ agenId: String(agenIdNumber) }}>
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>store</span> Outlet — Lihat
            </Link>
            <button className="sm-btn sm-btn-outline" type="button" onClick={() => void openTransferModal()}>
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>local_shipping</span> Kirim ke Outlet
            </button>
            <button className="sm-btn sm-btn-primary" type="button" onClick={openUpdateModal}>
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>sync</span> Update Data Harian
            </button>
          </div>
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
                <th>Asal / Tujuan</th>
                <th>Catatan</th>
                <th className="num">Stok Sesudah</th>
              </tr>
            </thead>
            <tbody>
              {detailData.transactions.length === 0 ? (
                <tr><td colSpan={6} style={{ textAlign: 'center', color: 'var(--sm-on-surface-variant)' }}>Belum ada transaksi.</td></tr>
              ) : (
                detailData.transactions.map((t, index) => (
                  <tr key={index}>
                    <td>{new Date(t.tanggal).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                    <td>{t.type}</td>
                    <td className="num">{t.kuantitas.toFixed(3).replace(/\.?0+$/, '')}</td>
                    <td>{t.tujuan ?? '-'}</td>
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
              {ENTRIES.some((e) => e.produk !== 'MinyakTanah' && detailData.rows.some((r) => r.produk === e.produk)) ? (
                <>
                  <div className="sm-label-caps" style={{ marginBottom: 8 }}>LPG — per ukuran tabung</div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginBottom: 14 }}>
                    <DailyEntry produk="Lpg5_5Kg" label="5.5 kg" values={daily} onChange={(key, patch) => setDaily((prev) => ({ ...prev, [key]: { ...prev[key], ...patch } }))} />
                    <DailyEntry produk="Lpg12Kg" label="12 kg" values={daily} onChange={(key, patch) => setDaily((prev) => ({ ...prev, [key]: { ...prev[key], ...patch } }))} />
                    <DailyEntry produk="Lpg50Kg" label="50 kg" values={daily} onChange={(key, patch) => setDaily((prev) => ({ ...prev, [key]: { ...prev[key], ...patch } }))} />
                  </div>
                </>
              ) : null}
              {detailData.rows.some((r) => r.produk === 'MinyakTanah') ? (
                <>
                  <div className="sm-label-caps" style={{ marginBottom: 8 }}>Minyak Tanah</div>
                  <DailyEntry produk="MinyakTanah" label="Minyak Tanah" values={daily} onChange={(key, patch) => setDaily((prev) => ({ ...prev, [key]: { ...prev[key], ...patch } }))} />
                </>
              ) : null}
              <div style={{ marginBottom: 12, marginTop: 12 }}>
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

      {showTransferModal ? (
        <Modal
          title="Kirim ke Outlet"
          maxWidth={520}
          onClose={() => { setShowTransferModal(false); setTransferMessage(null) }}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => { setShowTransferModal(false); setTransferMessage(null) }}>Batal</button>
              <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" disabled={transferMutation.isPending} onClick={confirmTransfer}>Kirim</button>
            </>
          }
        >
          <div style={{ marginBottom: 12 }}>
            <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Outlet tujuan</label>
            <select className="sm-filter" style={{ width: '100%' }} value={selectedOutletIndex} onChange={(e) => setSelectedOutletIndex(Number(e.target.value))}>
              {outletTargets.map((target, index) => (
                <option key={target.outletId} value={index}>{target.nama}</option>
              ))}
            </select>
          </div>
          <div className="sm-label-caps" style={{ marginBottom: 8 }}>Kuantitas per jenis material</div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            {([
              ['LPG 5.5 kg', 'q55', transferStok('Lpg5_5Kg'), 1],
              ['LPG 12 kg', 'q12', transferStok('Lpg12Kg'), 1],
              ['LPG 50 kg', 'q50', transferStok('Lpg50Kg'), 1],
              ['Minyak Tanah', 'qMinyak', transferStok('MinyakTanah'), 0.1],
            ] as const).map(([label, key, stokValue, step]) => (
              <div key={key} style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 10, alignItems: 'center' }}>
                <span className="sm-body-md">
                  {label} <span style={{ color: 'var(--sm-on-surface-variant)' }}>(stok agen: {stokValue.toLocaleString('id-ID')}{key === 'qMinyak' ? ' KL' : ''})</span>
                </span>
                <QtyInput
                  min={0}
                  step={step}
                  style={{ width: 120, padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)' }}
                  value={transferQty[key]}
                  onChange={(next) => setTransferQty((prev) => ({ ...prev, [key]: next }))}
                />
              </div>
            ))}
          </div>
          {transferMessage ? (
            <div style={{ margin: '14px 0 0', padding: 10, borderRadius: 'var(--sm-radius)', background: 'var(--sm-error-container)', color: 'var(--sm-on-error-container)' }}>
              {transferMessage}
            </div>
          ) : null}
        </Modal>
      ) : null}
    </div>
  )
}
