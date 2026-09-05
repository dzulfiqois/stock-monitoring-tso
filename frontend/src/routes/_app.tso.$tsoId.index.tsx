import { useQuery } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { RoleGate } from '../components/RoleGate'
import { produkDisplay, wilayahDisplay } from '../lib/data'
import { tsoApi } from '../lib/tso'

export const Route = createFileRoute('/_app/tso/$tsoId/')({
  component: TsoPreviewPage,
})

function TsoPreviewPage() {
  const { tsoId } = Route.useParams()
  const tsoIdNumber = Number(tsoId)
  const [message, setMessage] = useState<string | null>(null)
  const [generating, setGenerating] = useState(false)

  const order = useQuery({
    queryKey: ['tso', tsoIdNumber],
    queryFn: () => tsoApi.get(tsoIdNumber),
  })

  async function generateInvoice() {
    setMessage(null)
    setGenerating(true)
    try {
      const { blob } = await tsoApi.invoice(tsoIdNumber)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `DraftInvoice-${order.data?.orderNo ?? tsoIdNumber}.pdf`
      a.click()
      URL.revokeObjectURL(url)
    } catch (ex) {
      setMessage(ex instanceof Error ? ex.message : 'Gagal generate invoice.')
    } finally {
      setGenerating(false)
    }
  }

  async function resync() {
    setMessage(null)
    try {
      await tsoApi.resync(tsoIdNumber)
      await order.refetch()
    } catch (ex) {
      setMessage(ex instanceof Error ? ex.message : 'Gagal resync.')
    }
  }

  if (order.isPending) {
    return <div className="sm-card" style={{ padding: 24 }}>Memuat...</div>
  }

  if (order.error || !order.data) {
    return <div className="sm-card" style={{ padding: 24 }}>Order tidak ditemukan.</div>
  }

  const o = order.data
  const fmtDate = (value: string) =>
    new Date(value).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })

  return (
    <div>
      <div className="sm-breadcrumb" style={{ marginBottom: 12 }}>
        <Link to="/tso">Transport Shipping Order</Link>
        <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
        <span style={{ color: 'var(--sm-on-surface)' }}>{o.orderNo}</span>
      </div>
      <h1 className="sm-headline-md">Preview Invoice Pengiriman</h1>
      <p className="sm-body-md" style={{ margin: '4px 0 20px' }}>Ringkasan order yang telah ter-commit (read-only).</p>

      <div className="sm-card" style={{ padding: 16, marginBottom: 16 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <div><span className="sm-label-caps">Mitra TSO</span><div className="sm-title-sm">{o.mitraNamaSnapshot} ({o.mitraId})</div></div>
          <div><span className="sm-label-caps">Gudang Wilayah Tujuan</span><div className="sm-title-sm">{o.ruteTujuan}</div></div>
          <div><span className="sm-label-caps">Jenis Material</span><div className="sm-title-sm">{produkDisplay(o.produk)}</div></div>
          <div><span className="sm-label-caps">Kuantitas + Satuan</span><div className="sm-title-sm">{o.kuantitas.toFixed(2).replace(/\.?0+$/, '')} {o.satuan}</div></div>
          <div><span className="sm-label-caps">Tanggal Keberangkatan</span><div className="sm-title-sm">{fmtDate(o.tanggalKeberangkatan)}</div></div>
          <div><span className="sm-label-caps">ETA Estimasi</span><div className="sm-title-sm">{fmtDate(o.eta)}</div></div>
          <div><span className="sm-label-caps">Nomor Order</span><div className="sm-title-sm">{o.orderNo}</div></div>
          <div><span className="sm-label-caps">Timestamp Generate</span><div className="sm-title-sm">{new Date(o.createdAt).toISOString().slice(0, 16).replace('T', ' ')}</div></div>
        </div>
        <div style={{ marginTop: 12, display: 'flex', gap: 10, alignItems: 'center' }}>
          <span className="sm-pill sm-pill-neutral">Status: {o.status}</span>
          {o.status === 'FlagTertunda' ? (
            <span className="sm-pill sm-pill-warning">
              Dampak stok tertunda —{' '}
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => void resync()}>
                Resync
              </button>
            </span>
          ) : null}
          <span className="sm-body-md" style={{ fontSize: 12 }}>
            {wilayahDisplay(o.wilayahTujuan)} · {o.ruteAsal} → {o.ruteTujuan}
            {o.jarakKm !== null ? ` (${o.jarakKm} km)` : ''}
          </span>
        </div>
      </div>

      <div style={{ display: 'flex', gap: 12 }}>
        <button className="sm-btn sm-btn-primary" type="button" disabled={generating} onClick={() => void generateInvoice()}>
          <span className="material-symbols-outlined" style={{ fontSize: 18 }}>picture_as_pdf</span>{' '}
          {generating ? 'Menyiapkan…' : 'Generate Draft Invoice'}
        </button>
        <Link className="sm-btn sm-btn-outline" to="/tso">Kembali ke Daftar</Link>
        <RoleGate roles={['Superadmin', 'Supervisi']}>
          <Link className="sm-btn sm-btn-outline" to="/tso/$tsoId/edit" params={{ tsoId: String(o.id) }}>
            Update Order
          </Link>
        </RoleGate>
      </div>
      {message ? (
        <div style={{ marginTop: 12, padding: 10, borderRadius: 'var(--sm-radius)', background: 'var(--sm-error-container)', color: 'var(--sm-on-error-container)' }}>
          {message}
        </div>
      ) : null}
    </div>
  )
}
