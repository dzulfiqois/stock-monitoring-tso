import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { Modal } from '../components/Modal'
import { mitraApi } from '../lib/tso'
import type { MitraTsoView } from '../lib/tso'
import { WILAYAH_ALL, produkDisplay, wilayahDisplay } from '../lib/data'
import { QtyInput } from '../components/QtyInput'

export const Route = createFileRoute('/_app/mitra')({
  component: MitraListPage,
})

interface MitraForm {
  id: string
  nama: string
  jenisKendaraan: string
  kapasitasMax: number
  satuanKapasitas: string
  kontak: string
  pic: string
  rute: string
  active: boolean
  area: string[]
  tarifs: { produk: import('../lib/tso').TsoProduk; tarif: number; satuanTarif: string }[]
}

function emptyForm(): MitraForm {
  return {
    id: '', nama: '', jenisKendaraan: '', kapasitasMax: 0, satuanKapasitas: 'Tabung',
    kontak: '', pic: '', rute: '', active: true, area: [],
    tarifs: [
      { produk: 'MinyakTanah', tarif: 0, satuanTarif: 'per_kiloliter' },
      { produk: 'Lpg5_5Kg', tarif: 0, satuanTarif: 'per_tabung' },
      { produk: 'Lpg12Kg', tarif: 0, satuanTarif: 'per_tabung' },
      { produk: 'Lpg50Kg', tarif: 0, satuanTarif: 'per_tabung' },
    ],
  }
}

function MitraListPage() {
  const queryClient = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<MitraForm>(emptyForm)
  const [formMessage, setFormMessage] = useState<string | null>(null)

  const mitras = useQuery({ queryKey: ['mitra'], queryFn: mitraApi.list })

  const createMutation = useMutation({
    mutationFn: mitraApi.create,
    onSuccess: () => {
      setShowForm(false)
      queryClient.invalidateQueries({ queryKey: ['mitra'] })
    },
    onError: (error: Error) => setFormMessage(error.message),
  })
  const updateMutation = useMutation({
    mutationFn: async (args: { id: string; body: Parameters<typeof mitraApi.update>[1]; tarifs: MitraForm['tarifs'] }) => {
      await mitraApi.update(args.id, args.body)
      for (const t of args.tarifs.filter((x) => x.tarif > 0)) {
        await mitraApi.updateTarif(args.id, t)
      }
    },
    onSuccess: () => {
      setShowForm(false)
      queryClient.invalidateQueries({ queryKey: ['mitra'] })
    },
    onError: (error: Error) => setFormMessage(error.message),
  })

  function openCreate() {
    setEditingId(null)
    setForm(emptyForm())
    setFormMessage(null)
    setShowForm(true)
  }

  function openEdit(m: MitraTsoView) {
    setEditingId(m.id)
    setForm({
      id: m.id,
      nama: m.nama,
      jenisKendaraan: m.jenisKendaraan,
      kapasitasMax: m.kapasitasMax,
      satuanKapasitas: m.satuanKapasitas,
      kontak: m.kontak,
      pic: m.pic,
      rute: m.rute.join(', '),
      active: m.active,
      area: [...m.areaCoverage],
      tarifs: [
        { produk: 'MinyakTanah', tarif: m.tarifs.find((t) => t.produk === 'MinyakTanah')?.tarif ?? m.tarif, satuanTarif: m.tarifs.find((t) => t.produk === 'MinyakTanah')?.satuanTarif ?? 'per_kiloliter' },
        { produk: 'Lpg5_5Kg', tarif: m.tarifs.find((t) => t.produk === 'Lpg5_5Kg')?.tarif ?? 0, satuanTarif: m.tarifs.find((t) => t.produk === 'Lpg5_5Kg')?.satuanTarif ?? 'per_tabung' },
        { produk: 'Lpg12Kg', tarif: m.tarifs.find((t) => t.produk === 'Lpg12Kg')?.tarif ?? 0, satuanTarif: m.tarifs.find((t) => t.produk === 'Lpg12Kg')?.satuanTarif ?? 'per_tabung' },
        { produk: 'Lpg50Kg', tarif: m.tarifs.find((t) => t.produk === 'Lpg50Kg')?.tarif ?? 0, satuanTarif: m.tarifs.find((t) => t.produk === 'Lpg50Kg')?.satuanTarif ?? 'per_tabung' },
      ],
    })
    setFormMessage(null)
    setShowForm(true)
  }

  function save() {
    const area = form.area
    const tarifs = form.tarifs.filter((t) => t.tarif > 0)
    if (tarifs.length === 0) {
      setFormMessage('Isi minimal 1 tarif per jenis.')
      return
    }
    const rute = form.rute.split(',').map((r) => r.trim()).filter((r) => r !== '')
    if (editingId === null) {
      createMutation.mutate({
        id: form.id.trim(), nama: form.nama, jenisKendaraan: form.jenisKendaraan,
        kapasitasMax: form.kapasitasMax, satuanKapasitas: form.satuanKapasitas,
        rute, areaCoverage: area, kontak: form.kontak, pic: form.pic,
        active: form.active, tarifs,
      })
    } else {
      updateMutation.mutate({
        id: editingId,
        body: {
          nama: form.nama, jenisKendaraan: form.jenisKendaraan,
          kapasitasMax: form.kapasitasMax, satuanKapasitas: form.satuanKapasitas,
          rute, areaCoverage: area, kontak: form.kontak, pic: form.pic, active: form.active,
        },
        tarifs,
      })
    }
  }

  const tarifLabels: Record<string, string> = {
    MinyakTanah: 'Minyak Tanah',
    Lpg5_5Kg: '5.5 kg',
    Lpg12Kg: '12 kg',
    Lpg50Kg: '50 kg',
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <div>
          <h1 className="sm-display-lg">Mitra TSO</h1>
          <p className="sm-body-md" style={{ maxWidth: 660, margin: '4px 0 0' }}>
            Kelola Mitra transporter (full 12 field) dan tarif per jenis tabung.
          </p>
        </div>
        <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" onClick={openCreate}>
          <span className="material-symbols-outlined" style={{ fontSize: 18 }}>add</span> Tambah Mitra
        </button>
      </div>

      {mitras.isPending ? (
        <div className="sm-body-md" style={{ padding: '40px 0' }}>Memuat...</div>
      ) : (
        <div className="sm-table-wrap">
          <div style={{ overflowX: 'auto' }}>
            <table className="sm-table">
              <thead>
                <tr>
                  <th>Id</th>
                  <th>Nama</th>
                  <th>Jenis</th>
                  <th>Kapasitas</th>
                  <th>Area</th>
                  <th>Tarif per-jenis</th>
                  <th>Aktif</th>
                  <th>Aksi</th>
                </tr>
              </thead>
              <tbody>
                {(mitras.data ?? []).map((m) => (
                  <tr key={m.id}>
                    <td>{m.id}</td>
                    <td style={{ fontWeight: 600 }}>{m.nama}</td>
                    <td>{m.jenisKendaraan}</td>
                    <td>{m.kapasitasMax} {m.satuanKapasitas}</td>
                    <td>{m.areaCoverage.join(', ')}</td>
                    <td>
                      {m.tarifs.length === 0 ? (
                        <span className="sm-body-md" style={{ fontSize: 12 }}>{m.tarif.toLocaleString('id-ID')}/{m.satuanTarif}</span>
                      ) : (
                        m.tarifs.map((t) => (
                          <div className="sm-body-md" style={{ fontSize: 12 }} key={t.produk}>
                            {produkDisplay(t.produk)}: {t.tarif.toLocaleString('id-ID')}/{t.satuanTarif}
                          </div>
                        ))
                      )}
                    </td>
                    <td>{m.active ? 'Ya' : 'Tidak'}</td>
                    <td>
                      <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => openEdit(m)}>Edit</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {showForm ? (
        <Modal
          title={editingId === null ? 'Tambah Mitra' : 'Edit Mitra'}
          maxWidth={720}
          onClose={() => { setShowForm(false); setFormMessage(null) }}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => { setShowForm(false); setFormMessage(null) }}>Batal</button>
              <button
                className="sm-btn sm-btn-primary sm-btn-sm"
                type="button"
                disabled={createMutation.isPending || updateMutation.isPending}
                onClick={save}
              >
                Simpan
              </button>
            </>
          }
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <div>
              <label className="sm-label-caps">Id</label>
              <input type="text" className="sm-frame" style={{ width: '100%', padding: 8 }} disabled={editingId !== null} value={form.id} onChange={(e) => setForm((f) => ({ ...f, id: e.target.value }))} />
            </div>
            <div>
              <label className="sm-label-caps">Nama</label>
              <input type="text" className="sm-frame" style={{ width: '100%', padding: 8 }} value={form.nama} onChange={(e) => setForm((f) => ({ ...f, nama: e.target.value }))} />
            </div>
            <div>
              <label className="sm-label-caps">Jenis Kendaraan</label>
              <input type="text" className="sm-frame" style={{ width: '100%', padding: 8 }} value={form.jenisKendaraan} onChange={(e) => setForm((f) => ({ ...f, jenisKendaraan: e.target.value }))} />
            </div>
            <div>
              <label className="sm-label-caps">Kapasitas Max</label>
              <QtyInput className="sm-frame" min={1} step={1} style={{ width: '100%', padding: 8 }} value={form.kapasitasMax} onChange={(next) => setForm((f) => ({ ...f, kapasitasMax: next }))} />
            </div>
            <div>
              <label className="sm-label-caps">Satuan Kapasitas</label>
              <select className="sm-filter" style={{ width: '100%' }} value={form.satuanKapasitas} onChange={(e) => setForm((f) => ({ ...f, satuanKapasitas: e.target.value }))}>
                <option>Tabung</option>
                <option>Kiloliter</option>
              </select>
            </div>
            <div>
              <label className="sm-label-caps">Aktif</label>
              <select className="sm-filter" style={{ width: '100%' }} value={String(form.active)} onChange={(e) => setForm((f) => ({ ...f, active: e.target.value === 'true' }))}>
                <option value="true">Ya</option>
                <option value="false">Tidak</option>
              </select>
            </div>
            <div>
              <label className="sm-label-caps">Kontak</label>
              <input type="text" className="sm-frame" style={{ width: '100%', padding: 8 }} value={form.kontak} onChange={(e) => setForm((f) => ({ ...f, kontak: e.target.value }))} />
            </div>
            <div>
              <label className="sm-label-caps">PIC</label>
              <input type="text" className="sm-frame" style={{ width: '100%', padding: 8 }} value={form.pic} onChange={(e) => setForm((f) => ({ ...f, pic: e.target.value }))} />
            </div>
            <div style={{ gridColumn: '1 / -1' }}>
              <label className="sm-label-caps">Rute (pisah koma)</label>
              <input type="text" className="sm-frame" placeholder="Pusat -> Gudang Wilayah Papua, Pusat -> Maluku" style={{ width: '100%', padding: 8 }} value={form.rute} onChange={(e) => setForm((f) => ({ ...f, rute: e.target.value }))} />
            </div>
            <div style={{ gridColumn: '1 / -1' }}>
              <label className="sm-label-caps">Area Coverage (Wilayah enum, checklist)</label>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 6 }}>
                {WILAYAH_ALL.map((w) => (
                  <label key={w} style={{ display: 'flex', alignItems: 'center', gap: 4, cursor: 'pointer' }}>
                    <input
                      type="checkbox"
                      checked={form.area.includes(wilayahDisplay(w))}
                      onChange={(e) =>
                        setForm((f) => ({
                          ...f,
                          area: e.target.checked
                            ? [...f.area, wilayahDisplay(w)]
                            : f.area.filter((a) => a !== wilayahDisplay(w)),
                        }))
                      }
                    />{' '}
                    {wilayahDisplay(w)}
                  </label>
                ))}
              </div>
            </div>
          </div>

          <div className="sm-label-caps" style={{ margin: '12px 0 6px' }}>Tarif per Jenis (allowlist)</div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {form.tarifs.map((t, index) => {
              const allowlist = t.produk === 'MinyakTanah' ? ['per_kiloliter', 'per_kilometer'] : ['per_tabung', 'per_kilometer']
              return (
                <div key={t.produk} style={{ display: 'grid', gridTemplateColumns: '100px 1fr 140px', gap: 8, alignItems: 'center' }}>
                  <span className="sm-body-md">{tarifLabels[t.produk]}</span>
                  <QtyInput min={0} step={1000} placeholder="Tarif" style={{ padding: 8, borderRadius: 'var(--sm-radius)', border: '1px solid var(--sm-outline-variant)' }} value={t.tarif} onChange={(next) => setForm((f) => ({ ...f, tarifs: f.tarifs.map((x, i) => (i === index ? { ...x, tarif: next } : x)) }))} />
                  <select className="sm-filter" value={t.satuanTarif} onChange={(e) => setForm((f) => ({ ...f, tarifs: f.tarifs.map((x, i) => (i === index ? { ...x, satuanTarif: e.target.value } : x)) }))}>
                    {allowlist.map((s) => (
                      <option key={s} value={s}>{s}</option>
                    ))}
                  </select>
                </div>
              )
            })}
          </div>

          {formMessage ? (
            <div style={{ marginTop: 12, padding: 10, borderRadius: 'var(--sm-radius)', background: 'var(--sm-error-container)', color: 'var(--sm-on-error-container)' }}>
              {formMessage}
            </div>
          ) : null}
        </Modal>
      ) : null}
    </div>
  )
}
