import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Modal } from '../components/Modal'
import { RoleGate } from '../components/RoleGate'
import { StatusPill } from '../components/StatusPill'
import { agen, wilayahDisplay } from '../lib/data'
import type { AgenInventarisRow, Wilayah } from '../lib/data'

export const Route = createFileRoute('/_app/wilayah/$wilayah/agen')({
  component: DaftarAgenPage,
})

function DaftarAgenPage() {
  const { wilayah } = Route.useParams()
  const queryClient = useQueryClient()

  const [showFormModal, setShowFormModal] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [namaAgen, setNamaAgen] = useState('')
  const [keteranganAgen, setKeteranganAgen] = useState('')
  const [formMessage, setFormMessage] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AgenInventarisRow | null>(null)

  const rows = useQuery({
    queryKey: ['agen-inventaris', wilayah],
    queryFn: () => agen.list(wilayah as Wilayah),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['agen-inventaris', wilayah] })

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (editingId === null) {
        await agen.create({ nama: namaAgen, wilayah: wilayah as Wilayah, keterangan: keteranganAgen || undefined })
      } else {
        await agen.update(editingId, { nama: namaAgen, keterangan: keteranganAgen || undefined })
      }
    },
    onSuccess: () => {
      setShowFormModal(false)
      void invalidate()
    },
    onError: (error: Error) => setFormMessage(error.message),
  })

  const deleteMutation = useMutation({
    mutationFn: (agenId: number) => agen.remove(agenId),
    onSuccess: () => void invalidate(),
    onError: (error: Error) => {
      setFormMessage(error.message)
      setShowFormModal(true)
    },
  })

  function openCreateModal() {
    setEditingId(null)
    setNamaAgen('')
    setKeteranganAgen('')
    setFormMessage(null)
    setShowFormModal(true)
  }

  function openEditModal(row: AgenInventarisRow) {
    setEditingId(row.agenId)
    setNamaAgen(row.nama)
    setKeteranganAgen('')
    setFormMessage(null)
    setShowFormModal(true)
  }

  const wilayahKnown = WILAYAH_KNOWN(wilayah)

  if (!wilayahKnown) {
    return <div className="sm-card" style={{ padding: 24 }}>Wilayah tidak dikenal.</div>
  }

  return (
    <div>
      <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 16, marginBottom: 20 }}>
        <div>
          <div className="sm-breadcrumb">
            <Link to="/gudang-wilayah">Gudang Wilayah</Link>
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
            <span style={{ color: 'var(--sm-on-surface)' }}>{wilayahDisplay(wilayah)}</span>
          </div>
          <h1 className="sm-headline-md" style={{ marginTop: 6 }}>Daftar Agen — {wilayahDisplay(wilayah)}</h1>
          <p className="sm-body-md" style={{ margin: '4px 0 0' }}>
            Identitas agen yang memayungi Gudang Wilayah {wilayahDisplay(wilayah)} beserta stok inventarisnya.
          </p>
        </div>
        <RoleGate roles={['Superadmin', 'Supervisi']}>
          <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" onClick={openCreateModal}>
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>add</span> Tambah Agen
          </button>
        </RoleGate>
      </div>

      {rows.isPending ? (
        <div className="sm-body-md" style={{ padding: '40px 0' }}>Memuat...</div>
      ) : !rows.data || rows.data.length === 0 ? (
        <div className="sm-card" style={{ padding: 24, textAlign: 'center' }}>
          <span className="material-symbols-outlined" style={{ fontSize: 40, color: 'var(--sm-outline)' }}>storefront</span>
          <div className="sm-body-md" style={{ marginTop: 8 }}>
            Belum ada agen terdaftar di wilayah ini. Gunakan "Tambah Agen" untuk mendaftarkan identitas agen baru.
          </div>
        </div>
      ) : (
        <div className="sm-table-wrap">
          <div style={{ overflowX: 'auto' }}>
            <table className="sm-table">
              <thead>
                <tr>
                  <th>Nama Agen</th>
                  <th>Tanggal Daftar</th>
                  <th className="num">Total Stok</th>
                  <th className="num">Produk</th>
                  <th>Status</th>
                  <th>Aksi</th>
                </tr>
              </thead>
              <tbody>
                {rows.data.map((row) => (
                  <tr key={row.agenId}>
                    <td style={{ fontWeight: 600 }}>{row.nama}</td>
                    <td>{new Date(row.tanggalDaftar).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                    <td className="num">{row.totalStok.toLocaleString('id-ID')}</td>
                    <td className="num">{row.jumlahProduk}</td>
                    <td><StatusPill status={row.statusTerburuk} /></td>
                    <td>
                      <div style={{ display: 'flex', gap: 8 }}>
                        <Link className="sm-btn sm-btn-outline sm-btn-sm" to="/agen/$agenId" params={{ agenId: String(row.agenId) }}>
                          Detail
                        </Link>
                        <RoleGate roles={['Superadmin', 'Supervisi']}>
                          <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => openEditModal(row)}>
                            Edit
                          </button>
                        </RoleGate>
                        <RoleGate roles={['Superadmin']}>
                          <button className="sm-btn sm-btn-danger sm-btn-sm" type="button" onClick={() => setDeleteTarget(row)}>
                            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>delete</span>
                          </button>
                        </RoleGate>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {showFormModal ? (
        <Modal
          title={editingId === null ? 'Tambah Agen' : 'Edit Agen'}
          onClose={() => { setShowFormModal(false); setFormMessage(null) }}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => { setShowFormModal(false); setFormMessage(null) }}>
                Batal
              </button>
              <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" disabled={saveMutation.isPending} onClick={() => saveMutation.mutate()}>
                Simpan
              </button>
            </>
          }
        >
          <div style={{ marginBottom: 12 }}>
            <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Nama Agen</label>
            <input type="text" className="sm-frame" style={{ width: '100%' }} value={namaAgen} onChange={(e) => setNamaAgen(e.target.value)} />
          </div>
          <div style={{ marginBottom: 12 }}>
            <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Keterangan</label>
            <input type="text" className="sm-frame" style={{ width: '100%' }} value={keteranganAgen} onChange={(e) => setKeteranganAgen(e.target.value)} />
          </div>
          {formMessage ? (
            <div style={{ margin: '12px 0 0', padding: 10, borderRadius: 'var(--sm-radius)', background: 'var(--sm-error-container)', color: 'var(--sm-on-error-container)' }}>
              {formMessage}
            </div>
          ) : null}
        </Modal>
      ) : null}

      {deleteTarget ? (
        <Modal
          title="Hapus Agen"
          onClose={() => setDeleteTarget(null)}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => setDeleteTarget(null)}>Batal</button>
              <button
                className="sm-btn sm-btn-danger sm-btn-sm"
                type="button"
                disabled={deleteMutation.isPending}
                onClick={() => {
                  const target = deleteTarget
                  setDeleteTarget(null)
                  if (target) deleteMutation.mutate(target.agenId)
                }}
              >
                Hapus
              </button>
            </>
          }
        >
          <p className="sm-body-md" style={{ margin: 0 }}>
            Hapus <strong style={{ color: 'var(--sm-on-surface)' }}>{deleteTarget.nama}</strong>?
            Agen dihapus (soft delete) beserta baris stoknya; riwayat transaksi tetap di audit log.
          </p>
        </Modal>
      ) : null}
    </div>
  )
}

function WILAYAH_KNOWN(w: string): boolean {
  return [
    'Maluku', 'PapuaBarat', 'PapuaBaratDaya', 'MalukuUtara', 'PapuaTengah', 'PapuaSelatanPegunungan', 'Papua',
  ].includes(w)
}
