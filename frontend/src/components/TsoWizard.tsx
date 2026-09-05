import { useEffect, useState } from 'react'
import { Link, useNavigate } from '@tanstack/react-router'
import { ApiError } from '../lib/api'
import { WILAYAH_ALL, produkDisplay, wilayahDisplay } from '../lib/data'
import { tsoApi } from '../lib/tso'
import type { MitraTsoView, TsoProduk } from '../lib/tso'
import { QtyInput } from './QtyInput'

const STEP_LABELS = ['', 'Tujuan & Obyek', 'Rute & Jadwal', 'Transporter', 'Ringkasan']

const WILAYAH_OPTIONS: { value: string; label: string }[] = WILAYAH_ALL.map((w) => ({
  value: w,
  label: wilayahDisplay(w),
}))

export function TsoWizard({ editId }: { editId?: number }) {
  const navigate = useNavigate()
  const isEdit = editId !== undefined

  const [step, setStep] = useState(1)
  const [wilayahTujuan, setWilayahTujuan] = useState('Papua')
  const [isMinyak, setIsMinyak] = useState(true)
  const [qtyMinyak, setQtyMinyak] = useState(100)
  const [qty55, setQty55] = useState(0)
  const [qty12, setQty12] = useState(0)
  const [qty50, setQty50] = useState(0)
  const [jarakKm, setJarakKm] = useState<string>('')
  const [ruteAsal, setRuteAsal] = useState('Pusat')
  const [ruteTujuan, setRuteTujuan] = useState('Gudang Wilayah Papua')
  const [tanggalBerangkat, setTanggalBerangkat] = useState(() => {
    const d = new Date()
    d.setDate(d.getDate() + 1)
    return d.toISOString().split('T')[0]
  })
  const [allMitra, setAllMitra] = useState<MitraTsoView[]>([])
  const [selectedMitraId, setSelectedMitraId] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [existingOrderNo, setExistingOrderNo] = useState<string | null>(null)
  const [existingRowVersion, setExistingRowVersion] = useState<string | null>(null)

  useEffect(() => {
    void (async () => {
      const mitra = await tsoApi.listMitra()
      setAllMitra(mitra)
      if (editId !== undefined) {
        const order = await tsoApi.get(editId)
        setExistingOrderNo(order.orderNo)
        setExistingRowVersion(order.rowVersion)
        setWilayahTujuan(order.wilayahTujuan)
        setJarakKm(order.jarakKm === null ? '' : String(order.jarakKm))
        setRuteAsal(order.ruteAsal)
        setRuteTujuan(order.ruteTujuan)
        setTanggalBerangkat(order.tanggalKeberangkatan.split('T')[0])
        setSelectedMitraId(order.mitraId)
        const details = order.details
        if (details.length > 0) {
          const minyakDetail = details.find((d) => d.produk === 'MinyakTanah')
          setIsMinyak(minyakDetail !== undefined)
          setQtyMinyak(minyakDetail?.kuantitas ?? 0)
          setQty55(details.find((d) => d.produk === 'Lpg5_5Kg')?.kuantitas ?? 0)
          setQty12(details.find((d) => d.produk === 'Lpg12Kg')?.kuantitas ?? 0)
          setQty50(details.find((d) => d.produk === 'Lpg50Kg')?.kuantitas ?? 0)
        } else {
          setIsMinyak(order.produk === 'MinyakTanah')
          if (order.produk === 'MinyakTanah') setQtyMinyak(order.kuantitas)
          else if (order.produk === 'Lpg5_5Kg') setQty55(order.kuantitas)
          else if (order.produk === 'Lpg12Kg') setQty12(order.kuantitas)
          else if (order.produk === 'Lpg50Kg') setQty50(order.kuantitas)
        }
      } else {
        setRuteTujuan(`Gudang Wilayah ${wilayahDisplay(wilayahTujuan)}`)
      }
    })()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [editId])

  const filteredMitra = allMitra.filter((m) =>
    m.areaCoverage.some((a) => a.toLowerCase() === wilayahDisplay(wilayahTujuan).toLowerCase()),
  )
  const selectedMitra = allMitra.find((m) => m.id === selectedMitraId) ?? null

  const detailList = (): { produk: TsoProduk; kuantitas: number }[] => {
    if (isMinyak) {
      return qtyMinyak > 0 ? [{ produk: 'MinyakTanah', kuantitas: qtyMinyak }] : []
    }
    const entries: { produk: TsoProduk; kuantitas: number }[] = []
    if (qty55 > 0) entries.push({ produk: 'Lpg5_5Kg', kuantitas: qty55 })
    if (qty12 > 0) entries.push({ produk: 'Lpg12Kg', kuantitas: qty12 })
    if (qty50 > 0) entries.push({ produk: 'Lpg50Kg', kuantitas: qty50 })
    return entries
  }

  function updateEstimasi(): { total: number; detail: string } {
    const mitra = selectedMitra
    if (!mitra) return { total: 0, detail: '' }
    let total = 0
    const parts: string[] = []
    const add = (produk: TsoProduk, qty: number) => {
      if (qty <= 0) return
      const tarifRow = mitra.tarifs.find((t) => t.produk === produk)
      const tarif = tarifRow?.tarif ?? mitra.tarif
      const satuan = tarifRow?.satuanTarif ?? mitra.satuanTarif
      const jarak = jarakKm === '' ? null : Number(jarakKm)
      const est =
        satuan.toLowerCase().includes('kilometer') && jarak !== null ? tarif * qty * jarak : tarif * qty
      total += est
      parts.push(`${produkDisplay(produk)} ${qty} × ${tarif.toLocaleString('id-ID')} = ${est.toLocaleString('id-ID')}`)
    }
    if (isMinyak) add('MinyakTanah', qtyMinyak)
    else {
      add('Lpg5_5Kg', qty55)
      add('Lpg12Kg', qty12)
      add('Lpg50Kg', qty50)
    }
    return { total, detail: parts.join(' + ') }
  }

  const estimasi = updateEstimasi()
  const totalKuantitas = isMinyak ? qtyMinyak : qty55 + qty12 + qty50
  const etaPreview = (() => {
    const d = new Date(tanggalBerangkat)
    d.setDate(d.getDate() + 7)
    return d.toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })
  })()

  function next() {
    setErrorMessage(null)
    if (step === 1) {
      if (isMinyak && qtyMinyak <= 0) {
        setErrorMessage('Kuantitas Minyak harus > 0.')
        return
      }
      if (!isMinyak && qty55 <= 0 && qty12 <= 0 && qty50 <= 0) {
        setErrorMessage('Pilih minimal 1 jenis tabung dengan kuantitas > 0.')
        return
      }
      setRuteTujuan(`Gudang Wilayah ${wilayahDisplay(wilayahTujuan)}`)
    }
    if (step === 2) {
      const today = new Date().toISOString().split('T')[0]
      if (tanggalBerangkat < today) {
        setErrorMessage('Tanggal Keberangkatan tidak boleh lampau.')
        return
      }
      const jarak = jarakKm === '' ? null : Number(jarakKm)
      if (jarak !== null && jarak <= 0) {
        setErrorMessage('Jarak harus > 0 atau kosongkan.')
        return
      }
    }
    if (step === 3) {
      if (selectedMitraId === '') {
        setErrorMessage('Pilih Mitra transporter.')
        return
      }
    }
    if (step < 4) setStep(step + 1)
  }

  function prev() {
    setErrorMessage(null)
    if (step > 1) setStep(step - 1)
  }

  async function submit() {
    setErrorMessage(null)
    const details = detailList()
    if (details.length === 0) {
      setErrorMessage('Kuantitas tidak boleh kosong.')
      return
    }
    setSubmitting(true)
    try {
      if (isEdit && editId !== undefined && existingRowVersion !== null) {
        const updated = await tsoApi.update(editId, {
          mitraId: selectedMitraId,
          wilayahTujuan,
          produk: details[0].produk,
          kuantitas: details.reduce((a, d) => a + d.kuantitas, 0),
          tanggalKeberangkatan: tanggalBerangkat,
          ruteAsal,
          ruteTujuan,
          jarakKm: jarakKm === '' ? undefined : Number(jarakKm),
          details,
          rowVersion: existingRowVersion,
        })
        await navigate({ to: '/tso/$tsoId', params: { tsoId: String(updated.id) } })
      } else {
        const order = await tsoApi.create({
          mitraId: selectedMitraId,
          wilayahTujuan,
          produk: details[0].produk,
          kuantitas: details.reduce((a, d) => a + d.kuantitas, 0),
          tanggalKeberangkatan: tanggalBerangkat,
          ruteAsal,
          ruteTujuan,
          jarakKm: jarakKm === '' ? undefined : Number(jarakKm),
          details,
        })
        await navigate({ to: '/tso/$tsoId', params: { tsoId: String(order.id) } })
      }
    } catch (ex) {
      setErrorMessage(ex instanceof ApiError ? ex.message : 'Gagal menyimpan order.')
    } finally {
      setSubmitting(false)
    }
  }

  const stepper = (
    <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 16 }}>
      {[1, 2, 3, 4].map((i) => {
        const active = step === i
        const done = step > i
        return (
          <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <div
              style={{
                width: 28,
                height: 28,
                borderRadius: '50%',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: 12,
                fontWeight: 700,
                background: active
                  ? 'var(--sm-primary)'
                  : done
                    ? 'var(--sm-success)'
                    : 'var(--sm-surface-container-high)',
                color: active || done ? 'white' : 'var(--sm-on-surface-variant)',
              }}
            >
              {done ? '✓' : i}
            </div>
            <span
              className="sm-label-caps"
              style={{ color: active ? 'var(--sm-primary)' : 'var(--sm-on-surface-variant)' }}
            >
              {STEP_LABELS[i]}
            </span>
            {i < 4 ? (
              <div style={{ flex: 1, height: 2, background: 'var(--sm-outline-variant)', margin: '0 4px' }} />
            ) : null}
          </div>
        )
      })}
    </div>
  )

  const ringkasanObyek = isMinyak
    ? `Minyak Tanah — ${qtyMinyak} Kiloliter`
    : [
        qty55 > 0 ? `5.5kg:${qty55}` : null,
        qty12 > 0 ? `12kg:${qty12}` : null,
        qty50 > 0 ? `50kg:${qty50}` : null,
      ]
        .filter((s) => s !== null)
        .join(', ')

  return (
    <div>
      <div className="sm-breadcrumb" style={{ marginBottom: 12 }}>
        <Link to="/tso">Transport Shipping Order</Link>
        <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
        <span style={{ color: 'var(--sm-on-surface)' }}>{isEdit ? 'Update' : 'Buat Baru'}</span>
      </div>

      {stepper}

      <div className="sm-card" style={{ padding: 16 }}>
        {step === 1 ? (
          <>
            <h3 className="sm-title-sm">1. Lokasi Gudang &amp; Obyek</h3>
            <div style={{ marginTop: 12, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>
                  Gudang Wilayah Tujuan
                </label>
                <select
                  className="sm-filter"
                  style={{ width: '100%' }}
                  value={wilayahTujuan}
                  onChange={(e) => {
                    setWilayahTujuan(e.target.value)
                    setRuteTujuan(`Gudang Wilayah ${wilayahDisplay(e.target.value)}`)
                  }}
                >
                  {WILAYAH_OPTIONS.map((w) => (
                    <option key={w.value} value={w.value}>
                      {w.label}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>
                  Tipe Muatan
                </label>
                <div style={{ display: 'flex', gap: 12, marginTop: 6 }}>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input type="radio" name="muatan" checked={isMinyak} onChange={() => setIsMinyak(true)} /> Minyak Tanah
                  </label>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input type="radio" name="muatan" checked={!isMinyak} onChange={() => setIsMinyak(false)} /> Gas LPG
                  </label>
                </div>
              </div>
            </div>
            {isMinyak ? (
              <div style={{ marginTop: 12 }}>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>
                  Kuantitas Minyak Tanah (Kiloliter)
                </label>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <button type="button" className="sm-btn sm-btn-outline sm-btn-sm" onClick={() => setQtyMinyak(Math.max(1, qtyMinyak - 1))}>−</button>
                  <QtyInput min={1} step={0.1} style={{ width: 120, padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)', background: 'var(--sm-surface-bright)' }} value={qtyMinyak} onChange={(setQtyMinyak)} />
                  <button type="button" className="sm-btn sm-btn-outline sm-btn-sm" onClick={() => setQtyMinyak(qtyMinyak + 1)}>+</button>
                  <span className="sm-body-md">Kiloliter</span>
                </div>
              </div>
            ) : (
              <div style={{ marginTop: 12, display: 'flex', flexDirection: 'column', gap: 10 }}>
                <div className="sm-label-caps">Gas LPG — per jenis tabung (1 trip bisa multi-jenis)</div>
                {([
                  ['chip-55', '5.5 kg', qty55, setQty55],
                  ['chip-12', '12 kg', qty12, setQty12],
                  ['chip-50', '50 kg', qty50, setQty50],
                ] as const).map(([chip, label, qty, setQty]) => (
                  <div key={chip} style={{ display: 'grid', gridTemplateColumns: '120px 1fr', gap: 10, alignItems: 'center' }}>
                    <span className="sm-body-md">
                      <span className={`sm-chip ${chip}`}>
                        <span className="dot" />
                        {label}
                      </span>
                    </span>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      <button type="button" className="sm-btn sm-btn-outline sm-btn-sm" onClick={() => setQty(Math.max(0, qty - 1))}>−</button>
                      <QtyInput min={0} step={1} style={{ width: 100, padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)', background: 'var(--sm-surface-bright)' }} value={qty} onChange={(setQty)} />
                      <button type="button" className="sm-btn sm-btn-outline sm-btn-sm" onClick={() => setQty(qty + 1)}>+</button>
                      <span className="sm-body-md">Tabung</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        ) : null}

        {step === 2 ? (
          <>
            <h3 className="sm-title-sm">2. Rute &amp; Jadwal</h3>
            <div style={{ marginTop: 12, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Rute Asal</label>
                <input type="text" className="sm-frame" style={{ width: '100%', padding: 8 }} value={ruteAsal} onChange={(e) => setRuteAsal(e.target.value)} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Rute Tujuan</label>
                <input type="text" className="sm-frame" style={{ width: '100%', padding: 8 }} value={ruteTujuan} onChange={(e) => setRuteTujuan(e.target.value)} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>
                  Jarak (km) — manual, wajib jika tarif per_kilometer
                </label>
                <input type="number" min={0} step={0.1} placeholder="Kosongkan jika per_tabung/per_kiloliter" style={{ width: '100%', padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)', background: 'var(--sm-surface-bright)' }} value={jarakKm} onChange={(e) => setJarakKm(e.target.value)} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Tanggal Keberangkatan</label>
                <input type="date" className="sm-frame" style={{ width: '100%', padding: 8 }} value={tanggalBerangkat} onChange={(e) => setTanggalBerangkat(e.target.value)} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>ETA (otomatis +7 hari)</label>
                <input type="text" className="sm-frame" disabled style={{ width: '100%', padding: 8, background: 'var(--sm-surface-container)' }} value={etaPreview} readOnly />
              </div>
            </div>
          </>
        ) : null}

        {step === 3 ? (
          <>
            <h3 className="sm-title-sm">3. Transporter</h3>
            <div style={{ marginTop: 12 }}>
              <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Mitra Transporter</label>
              <select className="sm-filter" style={{ width: '100%' }} value={selectedMitraId} onChange={(e) => setSelectedMitraId(e.target.value)}>
                <option value="">— Pilih Mitra —</option>
                {filteredMitra.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.nama} — {m.jenisKendaraan} (Kapasitas {m.kapasitasMax} {m.satuanKapasitas})
                  </option>
                ))}
              </select>
            </div>
            {selectedMitra ? (
              <div className="sm-card" style={{ marginTop: 12, padding: 12, background: 'var(--sm-surface-container-low)' }}>
                <div className="sm-label-caps">
                  Mitra: {selectedMitra.nama} ({selectedMitra.jenisKendaraan})
                </div>
                <div className="sm-body-md" style={{ fontSize: 12, color: 'var(--sm-on-surface-variant)' }}>
                  Area: {selectedMitra.areaCoverage.join(', ')} | Tarif per-jenis:
                </div>
                {selectedMitra.tarifs.map((t) => (
                  <div className="sm-body-md" style={{ fontSize: 12 }} key={t.produk}>
                    {produkDisplay(t.produk)}: Rp {t.tarif.toLocaleString('id-ID')} / {t.satuanTarif}
                  </div>
                ))}
                <div className="sm-label-caps" style={{ marginTop: 8 }}>Estimasi Biaya Total</div>
                <div className="sm-title-sm" style={{ color: 'var(--sm-primary)' }}>
                  Rp {estimasi.total.toLocaleString('id-ID')}
                </div>
                <div className="sm-body-md" style={{ fontSize: 12, color: 'var(--sm-on-surface-variant)' }}>
                  {estimasi.detail}
                </div>
              </div>
            ) : null}
          </>
        ) : null}

        {step === 4 ? (
          <>
            <h3 className="sm-title-sm">4. Ringkasan Proposal</h3>
            <div style={{ marginTop: 12, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <div className="sm-card" style={{ padding: 12 }}>
                <span className="sm-label-caps">Gudang Tujuan</span>
                <div className="sm-title-sm">{wilayahDisplay(wilayahTujuan)}</div>
                <span className="sm-label-caps">Rute</span>
                <div className="sm-body-md">
                  {ruteAsal} → {ruteTujuan} {jarakKm !== '' ? `(${jarakKm} km)` : ''}
                </div>
              </div>
              <div className="sm-card" style={{ padding: 12 }}>
                <span className="sm-label-caps">Obyek</span>
                <div className="sm-body-md">{ringkasanObyek}</div>
                <span className="sm-label-caps">Jadwal</span>
                <div className="sm-body-md">
                  {new Date(tanggalBerangkat).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })} → ETA {etaPreview}
                </div>
              </div>
              <div className="sm-card" style={{ padding: 12 }}>
                <span className="sm-label-caps">Transporter</span>
                <div className="sm-title-sm">{selectedMitra?.nama ?? '-'} ({selectedMitra?.jenisKendaraan ?? '-'})</div>
                <span className="sm-label-caps">Estimasi Biaya</span>
                <div className="sm-title-sm">Rp {estimasi.total.toLocaleString('id-ID')}</div>
              </div>
              <div className="sm-card" style={{ padding: 12, background: 'var(--sm-surface-container-low)' }}>
                <span className="sm-label-caps">Draft Proposal</span>
                <div className="sm-body-md">Order No: {isEdit ? (existingOrderNo ?? '-') : 'akan digenerate saat Submit'}</div>
                <div className="sm-body-md">Vol Total: {totalKuantitas.toFixed(2).replace(/\.?0+$/, '')} {isMinyak ? 'Kiloliter' : 'Tabung'}</div>
              </div>
            </div>
          </>
        ) : null}

        {errorMessage ? (
          <div style={{ marginTop: 12, padding: 10, borderRadius: 'var(--sm-radius)', background: 'var(--sm-error-container)', color: 'var(--sm-on-error-container)' }}>
            {errorMessage}
          </div>
        ) : null}

        <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 16 }}>
          <button type="button" className="sm-btn sm-btn-outline sm-btn-sm" disabled={step === 1} onClick={prev}>
            Kembali
          </button>
          {step < 4 ? (
            <button type="button" className="sm-btn sm-btn-primary sm-btn-sm" onClick={next}>
              Lanjutkan
            </button>
          ) : (
            <button type="button" className="sm-btn sm-btn-primary" disabled={submitting} onClick={() => void submit()}>
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>
                {isEdit ? 'save' : 'check'}
              </span>{' '}
              {isEdit ? 'Update' : 'Submit & Preview'}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
