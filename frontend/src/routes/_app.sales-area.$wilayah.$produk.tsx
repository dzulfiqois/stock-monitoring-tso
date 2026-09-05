import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Modal } from '../components/Modal'
import { RoleGate } from '../components/RoleGate'
import { StatusPill, statusText } from '../components/StatusPill'
import { agen, data, produkDisplay, stock, tierDisplay } from '../lib/data'
import type { Produk, SalesAreaDetailRow, Tier } from '../lib/data'
import { QtyInput } from '../components/QtyInput'

export const Route = createFileRoute('/_app/sales-area/$wilayah/$produk')({
  component: DetailSalesAreaPage,
})

const TODAY = () => new Date().toISOString().split('T')[0]

function FormatKl(value: number): string {
  return value.toFixed(3).replace(/\.?0+$/, '')
}

function FormatStok(value: number): string {
  return value.toLocaleString('id-ID')
}

function DailyUkuranRow({
  chipClass,
  label,
  terjual,
  setTerjual,
  intransit,
  setIntransit,
  opnameEnabled,
  setOpnameEnabled,
  opname,
  setOpname,
}: {
  chipClass: string
  label: string
  terjual: number
  setTerjual: (v: number) => void
  intransit: number
  setIntransit: (v: number) => void
  opnameEnabled: boolean
  setOpnameEnabled: (v: boolean) => void
  opname: number
  setOpname: (v: number) => void
}) {
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
          <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>
            Stok Terjual (Tabung)
          </label>
          <QtyInput className="sm-frame" min={0} step={1} style={{ width: '100%', padding: 8 }} value={terjual} onChange={(setTerjual)} />
        </div>
        <div>
          <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>
            Stok Intransit
          </label>
          <QtyInput className="sm-frame" min={0} step={1} style={{ width: '100%', padding: 8 }} value={intransit} onChange={(setIntransit)} />
        </div>
      </div>
      <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 8, cursor: 'pointer' }}>
        <input type="checkbox" checked={opnameEnabled} onChange={(e) => setOpnameEnabled(e.target.checked)} />{' '}
        <span className="sm-label-caps">Stok Opname</span>
      </label>
              <QtyInput
          className="sm-frame"
          step={1}
          disabled={!opnameEnabled}
          style={{ width: '100%', padding: 8, marginTop: 4, background: opnameEnabled ? 'var(--sm-surface-bright)' : 'var(--sm-surface-container)' }}
          value={opname}
          onChange={setOpname}
        />
    </div>
  )
}

function DetailSalesAreaPage() {
  const { wilayah, produk } = Route.useParams()
  const queryClient = useQueryClient()
  const isLpg = produk === 'Lpg'
  const produkEnum: Produk | null = isLpg
    ? 'Lpg5_5Kg'
    : (produk as Produk)

  const detail = useQuery({
    queryKey: ['sales-area', wilayah, produk],
    queryFn: () => (isLpg ? data.lpgDetail(wilayah as never) : data.salesAreaDetail(wilayah as never, produkEnum!)),
  })

  // Update Data Harian modal state
  const [showUpdateModal, setShowUpdateModal] = useState(false)
  const [updateMode, setUpdateMode] = useState<'restock' | 'daily'>('restock')
  const [selectedEntityIndex, setSelectedEntityIndex] = useState(0)
  const [restockQty, setRestockQty] = useState(0)
  const [restockDate, setRestockDate] = useState(TODAY())
  const [dailyTier, setDailyTier] = useState<Tier>('GudangWilayah')
  const [dailyTerjual55, setDailyTerjual55] = useState(0)
  const [dailyOpnameEnabled55, setDailyOpnameEnabled55] = useState(false)
  const [dailyOpname55, setDailyOpname55] = useState(0)
  const [dailyIntransit55, setDailyIntransit55] = useState(0)
  const [dailyTerjual12, setDailyTerjual12] = useState(0)
  const [dailyOpnameEnabled12, setDailyOpnameEnabled12] = useState(false)
  const [dailyOpname12, setDailyOpname12] = useState(0)
  const [dailyIntransit12, setDailyIntransit12] = useState(0)
  const [dailyTerjual50, setDailyTerjual50] = useState(0)
  const [dailyOpnameEnabled50, setDailyOpnameEnabled50] = useState(false)
  const [dailyOpname50, setDailyOpname50] = useState(0)
  const [dailyIntransit50, setDailyIntransit50] = useState(0)
  const [dailyTerjualMinyak, setDailyTerjualMinyak] = useState(0)
  const [dailyOpnameEnabledMinyak, setDailyOpnameEnabledMinyak] = useState(false)
  const [dailyOpnameMinyak, setDailyOpnameMinyak] = useState(0)
  const [dailyIntransitMinyak, setDailyIntransitMinyak] = useState(0)
  const [dailyKeterangan, setDailyKeterangan] = useState('')
  const [dailyFormMessage, setDailyFormMessage] = useState<string | null>(null)

  // Kirim ke Agen modal state
  const [showTransferModal, setShowTransferModal] = useState(false)
  const [agenTargets, setAgenTargets] = useState<Awaited<ReturnType<typeof data.agenTransferTargets>>>([])
  const [selectedAgenIndex, setSelectedAgenIndex] = useState(0)
  const [qty55, setQty55] = useState(0)
  const [qty12, setQty12] = useState(0)
  const [qty50, setQty50] = useState(0)
  const [qtyMinyak, setQtyMinyak] = useState(0)
  const [transferMessage, setTransferMessage] = useState<string | null>(null)
  const [gudangStokState, setGudangStokState] = useState({ stok55: 0, stok12: 0, stok50: 0, stokMinyak: 0 })

  const invalidateAll = () => {
    queryClient.invalidateQueries({ queryKey: ['sales-area'] })
    queryClient.invalidateQueries({ queryKey: ['cards'] })
    queryClient.invalidateQueries({ queryKey: ['summary'] })
    queryClient.invalidateQueries({ queryKey: ['lpg-rows'] })
  }

  const transactMutation = useMutation({
    mutationFn: (args: { id: number; type: 'Receive' | 'Issue' | 'Adjust'; kuantitas: number; catatan?: string }) =>
      stock.transact(args.id, args.type, args.kuantitas, undefined, args.catatan),
    onSuccess: invalidateAll,
  })

  const transferMutation = useMutation({
    mutationFn: (args: { agenId: number; quantities: Record<string, number>; catatan: string }) =>
      agen.transferFromWarehouse(args.agenId, wilayah as never, args.quantities, args.catatan),
    onSuccess: () => {
      setShowTransferModal(false)
      invalidateAll()
    },
    onError: (error: Error) => setTransferMessage(error.message),
  })

  const detailData = detail.data

  function openUpdateModal() {
    setDailyTier(detailData?.rows.map((r) => r.tier).find((t) => t) ?? 'GudangWilayah')
    setDailyTerjual55(0); setDailyOpnameEnabled55(false); setDailyOpname55(0); setDailyIntransit55(0)
    setDailyTerjual12(0); setDailyOpnameEnabled12(false); setDailyOpname12(0); setDailyIntransit12(0)
    setDailyTerjual50(0); setDailyOpnameEnabled50(false); setDailyOpname50(0); setDailyIntransit50(0)
    setDailyTerjualMinyak(0); setDailyOpnameEnabledMinyak(false); setDailyOpnameMinyak(0); setDailyIntransitMinyak(0)
    setDailyKeterangan(''); setDailyFormMessage(null)
    setShowUpdateModal(true)
  }

  async function openTransferModal() {
    try {
      if (isLpg) {
        const rows = await data.lpgRows()
        setGudangStokState({
          stok55: rows.find((r) => r.produk === 'Lpg5_5Kg')?.stokGudang ?? 0,
          stok12: rows.find((r) => r.produk === 'Lpg12Kg')?.stokGudang ?? 0,
          stok50: rows.find((r) => r.produk === 'Lpg50Kg')?.stokGudang ?? 0,
          stokMinyak: 0,
        })
      } else {
        const rows = await data.minyakRows()
        setGudangStokState({
          stok55: 0, stok12: 0, stok50: 0,
          stokMinyak: rows.find((r) => r.wilayah === wilayah)?.stokGudang ?? 0,
        })
      }
    } catch {
      setGudangStokState({ stok55: 0, stok12: 0, stok50: 0, stokMinyak: 0 })
    }

    const targets = await data.agenTransferTargets(wilayah as never)
    setAgenTargets(targets)
    if (targets.length === 0) {
      setTransferMessage('Belum ada agen terdaftar di wilayah ini.')
      setShowTransferModal(true)
      return
    }

    setSelectedAgenIndex(0)
    setQty55(0); setQty12(0); setQty50(0); setQtyMinyak(0)
    setTransferMessage(null)
    setShowTransferModal(true)
  }

  function saveUpdate() {
    if (!detailData || !produkEnum) return
    const keterangan = dailyKeterangan || undefined

    if (updateMode === 'restock') {
      const row = detailData.rows[selectedEntityIndex]
      if (restockQty <= 0) return
      transactMutation.mutate(
        { id: row.stokEntitasId, type: 'Receive', kuantitas: restockQty, catatan: 'Isi ulang stok' },
        {
          onError: (error: Error) => setDailyFormMessage(error.message),
          onSuccess: () => { setShowUpdateModal(false); invalidateAll() },
        },
      )
      return
    }

    const entries: { produk: Produk; terjual: number; opEnabled: boolean; opname: number; intransit: number }[] = isLpg
      ? [
          { produk: 'Lpg5_5Kg', terjual: dailyTerjual55, opEnabled: dailyOpnameEnabled55, opname: dailyOpname55, intransit: dailyIntransit55 },
          { produk: 'Lpg12Kg', terjual: dailyTerjual12, opEnabled: dailyOpnameEnabled12, opname: dailyOpname12, intransit: dailyIntransit12 },
          { produk: 'Lpg50Kg', terjual: dailyTerjual50, opEnabled: dailyOpnameEnabled50, opname: dailyOpname50, intransit: dailyIntransit50 },
        ]
      : [
          { produk: 'MinyakTanah', terjual: dailyTerjualMinyak, opEnabled: dailyOpnameEnabledMinyak, opname: dailyOpnameMinyak, intransit: dailyIntransitMinyak },
        ]

    void (async () => {
      try {
        for (const entry of entries) {
          const row = detailData.rows.find((r) => r.tier === dailyTier && r.produk === entry.produk)
          if (!row) continue
          const perluIntransit = entry.intransit !== (row.stokIntransit ?? 0)
          const perluKeterangan = keterangan !== undefined && keterangan !== ''
          if (perluIntransit || perluKeterangan) {
            await stock.updateDetail(row.stokEntitasId, {
              dot: row.dot,
              tanggalStokAwal: row.tanggalStokAwal.split('T')[0],
              stokHabisTerjual: row.stokHabisTerjual,
              stokIntransit: entry.intransit,
              keterangan: dailyKeterangan,
            })
          }
          if (entry.terjual > 0) {
            await stock.transact(row.stokEntitasId, 'Issue', entry.terjual, undefined, dailyKeterangan || 'Stok terjual harian')
          }
          if (entry.opEnabled && entry.opname !== 0) {
            await stock.transact(row.stokEntitasId, 'Adjust', entry.opname, undefined, `Opname ${dailyKeterangan ?? ''}`.trim())
          }
        }
        setShowUpdateModal(false)
        invalidateAll()
      } catch (error) {
        setDailyFormMessage(error instanceof Error ? error.message : 'Gagal menyimpan.')
      }
    })()
  }

  function confirmTransfer() {
    const targets = agenTargets
    if (targets.length === 0) return
    const target = targets[selectedAgenIndex]
    const quantities: Record<string, number> = {}
    if (isLpg) {
      if (qty55 > 0) quantities.Lpg5_5Kg = qty55
      if (qty12 > 0) quantities.Lpg12Kg = qty12
      if (qty50 > 0) quantities.Lpg50Kg = qty50
    } else if (qtyMinyak > 0) {
      quantities.MinyakTanah = qtyMinyak
    }

    transferMutation.mutate(
      {
        agenId: target.agenId,
        quantities,
        catatan: `Transfer dari Gudang Wilayah ${wilayah} ke ${target.nama}`,
      },
    )
  }

  if (detail.isPending) {
    return <div className="sm-card" style={{ padding: 24 }}>Memuat...</div>
  }

  if (!detailData) {
    return <div className="sm-card" style={{ padding: 24 }}>Data tidak ditemukan.</div>
  }

  const produkLabel = isLpg ? 'Gas Tabung' : produkDisplay(produk)
  const satuan = isLpg ? 'Tabung' : 'Kiloliter'
  const tiers = Array.from(new Set(detailData.rows.map((r) => r.tier)))

  return (
    <div>
      <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 16, marginBottom: 20 }}>
        <div>
          <div className="sm-breadcrumb">
            <Link to="/gudang-wilayah">Gudang Wilayah</Link>
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
            <span style={{ color: 'var(--sm-on-surface)' }}>{wilayah} ({produkLabel})</span>
          </div>
          <h1 className="sm-headline-md" style={{ marginTop: 6 }}>Detail Monitoring {produkLabel}</h1>
          <p className="sm-body-md" style={{ margin: '4px 0 0' }}>
            Sales Area {wilayah} — Realisasi dan pemantauan stok harian.
          </p>
        </div>
        <RoleGate roles={['Superadmin', 'Supervisi']}>
          <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
            <button className="sm-btn sm-btn-outline" type="button" onClick={() => void openTransferModal()}>
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>local_shipping</span> Kirim ke Agen
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
          <div>
            <span className="value">{detailData.totalStok.toLocaleString('id-ID')}</span> <span className="unit">{satuan}</span>
          </div>
        </div>
        {isLpg ? (
          <>
            <div className="sm-kpi">
              <span className="sm-label-caps">Daily Obj. Throughput</span>
              <div>
                <span className="value" style={{ fontSize: 26 }}>{detailData.rows.reduce((a, r) => a + r.dot, 0).toFixed(1)}</span>{' '}
                <span className="unit">{satuan}/hari</span>
              </div>
            </div>
            <div className="sm-kpi">
              <span className="sm-label-caps">Covered Days Estimasi</span>
              <div>
                <span className="value" style={{ fontSize: 26 }}>{detailData.cdTerburuk?.toFixed(2) ?? '-'}</span>{' '}
                <span className="unit">hari</span>
              </div>
            </div>
          </>
        ) : (
          <>
            <div className="sm-kpi">
              <span className="sm-label-caps">Terjual</span>
              <div>
                <span className="value" style={{ fontSize: 26 }}>
                  {FormatKl(detailData.rows.reduce((a, r) => a + (r.stokHabisTerjual ?? 0), 0))}
                </span>{' '}
                <span className="unit">{satuan}</span>
              </div>
            </div>
            <div className="sm-kpi">
              <span className="sm-label-caps">Intransit</span>
              <div>
                <span className="value" style={{ fontSize: 26 }}>
                  {FormatKl(detailData.rows.reduce((a, r) => a + (r.stokIntransit ?? 0), 0))}
                </span>{' '}
                <span className="unit">{satuan}</span>
              </div>
            </div>
          </>
        )}
        <div className={`sm-kpi ${detailData.statusArea === 'Kritis' ? 'sm-kpi-kritis' : ''}`}>
          <span className="sm-label-caps">Status Area</span>
          <div>
            <span className="value" style={{ fontSize: 26 }}>{statusText(detailData.statusArea)}</span>
          </div>
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
          <span>Data Inventory per Tier</span>
          <span style={{ textTransform: 'none', letterSpacing: 0 }}>
            Realisasi:{' '}
            {detailData.rows.length > 0 && detailData.rows[0].exhaustDate
              ? new Date(detailData.rows[0].exhaustDate).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })
              : '-'}
          </span>
        </div>
        <div style={{ overflowX: 'auto' }}>
          <table className="sm-table">
            <thead>
              <tr>
                <th>Tier</th>
                <th>Produk</th>
                <th className="num">Stok</th>
                <th className="num">DOT</th>
                <th className="num">Covered Days</th>
                <th>Status</th>
                <th>Exhaust</th>
              </tr>
            </thead>
            <tbody>
              {detailData.rows.map((row: SalesAreaDetailRow) => (
                <tr key={row.stokEntitasId}>
                  <td>{tierDisplay(row.tier)}</td>
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
                <th>Tujuan</th>
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
          onClose={() => { setShowUpdateModal(false); setDailyFormMessage(null) }}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => { setShowUpdateModal(false); setDailyFormMessage(null) }}>
                Batal
              </button>
              <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" onClick={saveUpdate}>
                Simpan Perubahan
              </button>
            </>
          }
        >
          <div className="sm-segmented" style={{ marginBottom: 16 }}>
            <button type="button" className={updateMode === 'restock' ? 'active' : ''} onClick={() => setUpdateMode('restock')}>
              Isi Ulang Stok
            </button>
            <button type="button" className={updateMode === 'daily' ? 'active' : ''} onClick={() => setUpdateMode('daily')}>
              Update Stok Harian
            </button>
          </div>

          {updateMode === 'restock' ? (
            <>
              <div style={{ marginBottom: 12 }}>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Tier tujuan</label>
                <select className="sm-filter" style={{ width: '100%' }} value={selectedEntityIndex} onChange={(e) => setSelectedEntityIndex(Number(e.target.value))}>
                  {detailData.rows.map((row, index) => (
                    <option key={row.stokEntitasId} value={index}>
                      {tierDisplay(row.tier)} ({produkDisplay(row.produk)})
                    </option>
                  ))}
                </select>
              </div>
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
              <div style={{ marginBottom: 12 }}>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Tier tujuan</label>
                <select className="sm-filter" style={{ width: '100%' }} value={dailyTier} onChange={(e) => setDailyTier(e.target.value as Tier)}>
                  {tiers.map((tier) => (
                    <option key={tier} value={tier}>{tierDisplay(tier)}</option>
                  ))}
                </select>
              </div>

              {isLpg ? (
                <div className="sm-label-caps" style={{ marginBottom: 8 }}>Per ukuran tabung</div>
              ) : null}
              {isLpg ? (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                  <DailyUkuranRow chipClass="chip-55" label="5.5 kg" terjual={dailyTerjual55} setTerjual={setDailyTerjual55} intransit={dailyIntransit55} setIntransit={setDailyIntransit55} opnameEnabled={dailyOpnameEnabled55} setOpnameEnabled={setDailyOpnameEnabled55} opname={dailyOpname55} setOpname={setDailyOpname55} />
                  <DailyUkuranRow chipClass="chip-12" label="12 kg" terjual={dailyTerjual12} setTerjual={setDailyTerjual12} intransit={dailyIntransit12} setIntransit={setDailyIntransit12} opnameEnabled={dailyOpnameEnabled12} setOpnameEnabled={setDailyOpnameEnabled12} opname={dailyOpname12} setOpname={setDailyOpname12} />
                  <DailyUkuranRow chipClass="chip-50" label="50 kg" terjual={dailyTerjual50} setTerjual={setDailyTerjual50} intransit={dailyIntransit50} setIntransit={setDailyIntransit50} opnameEnabled={dailyOpnameEnabled50} setOpnameEnabled={setDailyOpnameEnabled50} opname={dailyOpname50} setOpname={setDailyOpname50} />
                </div>
              ) : (
                <>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, marginBottom: 12 }}>
                    <div>
                      <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Stok Terjual (Kiloliter)</label>
                      <QtyInput className="sm-frame" min={0} step={0.1} style={{ width: '100%', padding: 8 }} value={dailyTerjualMinyak} onChange={(setDailyTerjualMinyak)} />
                    </div>
                    <div>
                      <label className="sm-label-caps" style={{ display: 'block', marginBottom: 4 }}>Stok Intransit (Kiloliter)</label>
                      <QtyInput className="sm-frame" min={0} step={0.1} style={{ width: '100%', padding: 8 }} value={dailyIntransitMinyak} onChange={(setDailyIntransitMinyak)} />
                    </div>
                  </div>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 12, cursor: 'pointer' }}>
                    <input type="checkbox" checked={dailyOpnameEnabledMinyak} onChange={(e) => setDailyOpnameEnabledMinyak(e.target.checked)} />{' '}
                    <span className="sm-label-caps">Stok Opname (± Kiloliter)</span>
                  </label>
                                      <QtyInput
                      className="sm-frame"
                      step={0.1}
                      disabled={!dailyOpnameEnabledMinyak}
                      style={{ width: '100%', padding: 8, marginBottom: 12, background: dailyOpnameEnabledMinyak ? 'var(--sm-surface-bright)' : 'var(--sm-surface-container)' }}
                      value={dailyOpnameMinyak}
                      onChange={setDailyOpnameMinyak}
                    />
                </>
              )}

              <div style={{ marginBottom: 12 }}>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Keterangan</label>
                <input type="text" className="sm-frame" style={{ width: '100%' }} value={dailyKeterangan} onChange={(e) => setDailyKeterangan(e.target.value)} />
              </div>
              {dailyFormMessage ? (
                <div style={{ padding: 10, borderRadius: 'var(--sm-radius)', background: 'var(--sm-error-container)', color: 'var(--sm-on-error-container)' }}>
                  {dailyFormMessage}
                </div>
              ) : null}
            </>
          )}
        </Modal>
      ) : null}

      {showTransferModal ? (
        <Modal
          title="Kirim ke Agen"
          maxWidth={520}
          onClose={() => { setShowTransferModal(false); setTransferMessage(null) }}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => { setShowTransferModal(false); setTransferMessage(null) }}>
                Batal
              </button>
              <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" disabled={transferMutation.isPending} onClick={confirmTransfer}>
                Kirim
              </button>
            </>
          }
        >
          <div style={{ marginBottom: 12 }}>
            <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Agen tujuan</label>
            <select className="sm-filter" style={{ width: '100%' }} value={selectedAgenIndex} onChange={(e) => setSelectedAgenIndex(Number(e.target.value))}>
              {agenTargets.map((target, index) => (
                <option key={target.agenId} value={index}>{target.nama}</option>
              ))}
            </select>
          </div>

          <div className="sm-label-caps" style={{ marginBottom: 8 }}>Kuantitas per jenis material</div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            {isLpg ? (
              <>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 10, alignItems: 'center' }}>
                  <span className="sm-body-md">LPG 5.5 kg <span style={{ color: 'var(--sm-on-surface-variant)' }}>(stok gudang: {FormatStok(gudangStokState.stok55)})</span></span>
                  <QtyInput min={0} step={1} style={{ width: 120, padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)' }} value={qty55} onChange={setQty55} />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 10, alignItems: 'center' }}>
                  <span className="sm-body-md">LPG 12 kg <span style={{ color: 'var(--sm-on-surface-variant)' }}>(stok gudang: {FormatStok(gudangStokState.stok12)})</span></span>
                  <QtyInput min={0} step={1} style={{ width: 120, padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)' }} value={qty12} onChange={setQty12} />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 10, alignItems: 'center' }}>
                  <span className="sm-body-md">LPG 50 kg <span style={{ color: 'var(--sm-on-surface-variant)' }}>(stok gudang: {FormatStok(gudangStokState.stok50)})</span></span>
                  <QtyInput min={0} step={1} style={{ width: 120, padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)' }} value={qty50} onChange={setQty50} />
                </div>
              </>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 10, alignItems: 'center' }}>
                <span className="sm-body-md">Minyak Tanah <span style={{ color: 'var(--sm-on-surface-variant)' }}>(stok gudang: {FormatStok(gudangStokState.stokMinyak)} KL)</span></span>
                <QtyInput min={0} step={0.1} style={{ width: 120, padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)' }} value={qtyMinyak} onChange={(setQtyMinyak)} />
              </div>
            )}
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
