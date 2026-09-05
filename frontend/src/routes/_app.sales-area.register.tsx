import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useState } from 'react'
import { stock } from '../lib/data'
import type { Wilayah } from '../lib/data'
import { WILAYAH_ALL, wilayahDisplay } from '../lib/data'
import { QtyInput } from '../components/QtyInput'

export const Route = createFileRoute('/_app/sales-area/register')({
  component: RegisterSalesAreaPage,
})

const inputStyle = { width: '100%', padding: 10 }

function RegisterSalesAreaPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [objek, setObjek] = useState<'MinyakTanah' | 'GasLpg'>('MinyakTanah')
  const [wilayah, setWilayah] = useState<Wilayah>('Papua')
  const [tanggal, setTanggal] = useState(() => new Date().toISOString().split('T')[0])

  const [stokAgen, setStokAgen] = useState(0)
  const [dotAgen, setDotAgen] = useState(0)
  const [stokOutlet, setStokOutlet] = useState(0)
  const [dotOutlet, setDotOutlet] = useState(0)
  const [terjual, setTerjual] = useState(0)
  const [intransit, setIntransit] = useState(0)
  const [keterangan, setKeterangan] = useState('')

  const [stokAgen55, setStokAgen55] = useState(0)
  const [stokAgen12, setStokAgen12] = useState(0)
  const [stokAgen50, setStokAgen50] = useState(0)
  const [stokOutlet55, setStokOutlet55] = useState(0)
  const [stokOutlet12, setStokOutlet12] = useState(0)
  const [stokOutlet50, setStokOutlet50] = useState(0)
  const [dot55, setDot55] = useState(0)
  const [dot12, setDot12] = useState(0)
  const [dot50, setDot50] = useState(0)

  const [message, setMessage] = useState<string | null>(null)
  const [isError, setIsError] = useState(false)

  const mutation = useMutation({
    mutationFn: async () => {
      if (objek === 'MinyakTanah') {
        await stock.register({
          wilayah, produk: 'MinyakTanah', tier: 'GudangWilayah',
          tanggalStokAwal: tanggal, stok: stokAgen, dot: dotAgen,
          stokHabisTerjual: terjual, stokIntransit: intransit, keterangan,
        })
        await stock.register({
          wilayah, produk: 'MinyakTanah', tier: 'Outlet',
          tanggalStokAwal: tanggal, stok: stokOutlet, dot: dotOutlet,
        })
      } else {
        for (const [produk, stokValue, dotValue] of [
          ['Lpg5_5Kg', stokAgen55, dot55],
          ['Lpg12Kg', stokAgen12, dot12],
          ['Lpg50Kg', stokAgen50, dot50],
        ] as const) {
          await stock.register({
            wilayah, produk, tier: 'GudangWilayah',
            tanggalStokAwal: tanggal, stok: stokValue, dot: dotValue,
          })
        }
        for (const [produk, stokValue, dotValue] of [
          ['Lpg5_5Kg', stokOutlet55, dot55],
          ['Lpg12Kg', stokOutlet12, dot12],
          ['Lpg50Kg', stokOutlet50, dot50],
        ] as const) {
          await stock.register({
            wilayah, produk, tier: 'Outlet',
            tanggalStokAwal: tanggal, stok: stokValue, dot: dotValue,
          })
        }
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cards'] })
      queryClient.invalidateQueries({ queryKey: ['summary'] })
      setMessage('Sales Area berhasil didaftarkan.')
      setIsError(false)
      setTimeout(() => navigate({ to: '/gudang-wilayah' }), 400)
    },
    onError: (error: Error) => {
      setMessage(error.message)
      setIsError(true)
    },
  })

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <h1 className="sm-display-lg">Register Sales Area</h1>
        <a className="sm-btn sm-btn-outline sm-btn-sm" href="/gudang-wilayah">Kembali</a>
      </div>

      <div className="sm-card" style={{ maxWidth: 680 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
          <div>
            <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Objek Stok</label>
            <select className="sm-filter" style={{ width: '100%' }} value={objek} onChange={(e) => setObjek(e.target.value as 'MinyakTanah' | 'GasLpg')}>
              <option value="MinyakTanah">Minyak Tanah</option>
              <option value="GasLpg">Gas LPG</option>
            </select>
          </div>
          <div>
            <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Wilayah</label>
            <select className="sm-filter" style={{ width: '100%' }} value={wilayah} onChange={(e) => setWilayah(e.target.value as Wilayah)}>
              {WILAYAH_ALL.map((w) => (
                <option key={w} value={w}>{wilayahDisplay(w)}</option>
              ))}
            </select>
          </div>
        </div>

        <div style={{ marginBottom: 16 }}>
          <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Realisasi Tanggal</label>
          <input type="date" className="sm-frame" style={inputStyle} value={tanggal} onChange={(e) => setTanggal(e.target.value)} />
        </div>

        {objek === 'MinyakTanah' ? (
          <>
            <div style={{ marginBottom: 12 }}>
              <h3 className="sm-label-caps" style={{ color: 'var(--sm-primary)' }}>Stok Minyak Tanah (KL)</h3>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Stok Agen</label>
                <QtyInput step={0.1} min={0} value={stokAgen} onChange={(setStokAgen)} className="sm-frame" style={inputStyle} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>DOT Agen (KL/hari)</label>
                <QtyInput step={0.1} min={0} value={dotAgen} onChange={(setDotAgen)} className="sm-frame" style={inputStyle} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Stok Outlet</label>
                <QtyInput step={0.1} min={0} value={stokOutlet} onChange={(setStokOutlet)} className="sm-frame" style={inputStyle} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>DOT Outlet (KL/hari)</label>
                <QtyInput step={0.1} min={0} value={dotOutlet} onChange={(setDotOutlet)} className="sm-frame" style={inputStyle} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Stok Habis Terjual</label>
                <QtyInput step={0.1} min={0} value={terjual} onChange={(setTerjual)} className="sm-frame" style={inputStyle} />
              </div>
              <div>
                <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Stok Intransit</label>
                <QtyInput step={0.1} min={0} value={intransit} onChange={(setIntransit)} className="sm-frame" style={inputStyle} />
              </div>
            </div>
            <div style={{ margin: '14px 0' }}>
              <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Keterangan</label>
              <input type="text" value={keterangan} onChange={(e) => setKeterangan(e.target.value)} className="sm-frame" style={inputStyle} />
            </div>
          </>
        ) : (
          <>
            <h3 className="sm-label-caps" style={{ color: 'var(--sm-primary)', marginBottom: 12 }}>Stok LPG (Tabung)</h3>
            <div className="sm-label-caps" style={{ color: 'var(--sm-on-surface-variant)', marginBottom: 6 }}>Stok Agen</div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 14, marginBottom: 12 }}>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>5.5 kg</label><QtyInput min={0} value={stokAgen55} onChange={setStokAgen55} className="sm-frame" style={inputStyle} /></div>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>12 kg</label><QtyInput min={0} value={stokAgen12} onChange={setStokAgen12} className="sm-frame" style={inputStyle} /></div>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>50 kg</label><QtyInput min={0} value={stokAgen50} onChange={setStokAgen50} className="sm-frame" style={inputStyle} /></div>
            </div>
            <div className="sm-label-caps" style={{ color: 'var(--sm-on-surface-variant)', marginBottom: 6 }}>Stok Outlet</div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 14, marginBottom: 12 }}>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>5.5 kg</label><QtyInput min={0} value={stokOutlet55} onChange={setStokOutlet55} className="sm-frame" style={inputStyle} /></div>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>12 kg</label><QtyInput min={0} value={stokOutlet12} onChange={setStokOutlet12} className="sm-frame" style={inputStyle} /></div>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>50 kg</label><QtyInput min={0} value={stokOutlet50} onChange={setStokOutlet50} className="sm-frame" style={inputStyle} /></div>
            </div>
            <div className="sm-label-caps" style={{ color: 'var(--sm-on-surface-variant)', marginBottom: 6 }}>DOT (Tabung/hari)</div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 14 }}>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>5.5 kg</label><QtyInput min={0} value={dot55} onChange={setDot55} className="sm-frame" style={inputStyle} /></div>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>12 kg</label><QtyInput min={0} value={dot12} onChange={setDot12} className="sm-frame" style={inputStyle} /></div>
              <div><label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>50 kg</label><QtyInput min={0} value={dot50} onChange={setDot50} className="sm-frame" style={inputStyle} /></div>
            </div>
          </>
        )}

        {message ? (
          <div
            style={{
              margin: '16px 0 0',
              padding: 12,
              borderRadius: 'var(--sm-radius)',
              background: isError ? 'var(--sm-error-container)' : 'rgba(22,163,74,.12)',
              color: isError ? 'var(--sm-on-error-container)' : 'var(--sm-success)',
            }}
          >
            {message}
          </div>
        ) : null}

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, marginTop: 20, borderTop: '1px solid var(--sm-outline-variant)', paddingTop: 16 }}>
          <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => navigate({ to: '/gudang-wilayah' })}>
            Batal
          </button>
          <button className="sm-btn sm-btn-primary" type="button" disabled={mutation.isPending} onClick={() => mutation.mutate()}>
            Simpan
          </button>
        </div>
      </div>
    </div>
  )
}
