import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Modal } from '../components/Modal'
import { RoleGate } from '../components/RoleGate'
import { StatusPill } from '../components/StatusPill'
import { outlet } from '../lib/data'
import type { OutletInventarisRow } from '../lib/data'

export const Route = createFileRoute('/_app/agen/$agenId/outlet')({
  component: DaftarOutletPage,
})

function DaftarOutletPage() {
  const { agenId } = Route.useParams()
  const agenIdNumber = Number(agenId)
  const queryClient = useQueryClient()

  const [showFormModal, setShowFormModal] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [namaOutlet, setNamaOutlet] = useState('')
  const [formMessage, setFormMessage] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<OutletInventarisRow | null>(null)

  const rows = useQuery({
    queryKey: ['outlet-inventaris', agenIdNumber],
    queryFn: () => outlet.list(agenIdNumber),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['outlet-inventaris', agenIdNumber] })

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (editingId === null) {
        await outlet.create({ nama: namaOutlet, agenId: agenIdNumber })
      } else {
        await outlet.update(editingId, { nama: namaOutlet })
      }
    },
    onSuccess: () => {
      setShowFormModal(false)
      void invalidate()
    },
    onError: (error: Error) => setFormMessage(error.message),
  })

  const deleteMutation = useMutation({
    mutationFn: (outletId: number) => outlet.remove(outletId),
    onSuccess: () => void invalidate(),
    onError: (error: Error) => {
      setFormMessage(error.message)
      setShowFormModal(true)
    },
  })

  function openCreateModal() {
    setEditingId(null)
    setNamaOutlet('')
    setFormMessage(null)
    setShowFormModal(true)
  }

  function openEditModal(row: OutletInventarisRow) {
    setEditingId(row.outletId)
    setNamaOutlet(row.nama)
    setFormMessage(null)
    setShowFormModal(true)
  }

  return (
    <div>
      <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 16, marginBottom: 20 }}>
        <div>
          <div className="sm-breadcrumb">
            <Link to="/gudang-wilayah">Gudang Wilayah</Link>
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
            <Link to="/agen/$agenId" params={{ agenId: String(agenIdNumber) }}>Agen</Link>
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
            <span style={{ color: 'var(--sm-on-surface)' }}>Outlet</span>
          </div>
          <h1 className="sm-headline-md" style={{ marginTop: 6 }}>Daftar Outlet</h1>
          <p className="sm-body-md" style={{ margin: '4px 0 0' }}>
            Outlet binaan agen beserta stok inventarisnya.
          </p>
        </div>
        <RoleGate roles={['Superadmin', 'Supervisi']}>
          <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" onClick={openCreateModal}>
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>add</span> Tambah Outlet
          </button>
        </RoleGate>
      </div>

      {rows.isPending ? (
        <div className="sm-body-md" style={{ padding: '40px 0' }}>Memuat...</div>
      ) : !rows.data || rows.data.length === 0 ? (
        <div className="sm-card" style={{ padding: 24, textAlign: 'center' }}>
          <span className="material-symbols-outlined" style={{ fontSize: 40, color: 'var(--sm-outline)' }}>store</span>
          <div className="sm-body-md" style={{ marginTop: 8 }}>Belum ada outlet. Gunakan "Tambah Outlet".</div>
        </div>
      ) : (
        <div className="sm-table-wrap">
          <div style={{ overflowX: 'auto' }}>
            <table className="sm-table">
              <thead>
                <tr>
                  <th>Nama Outlet</th>
                  <th>Tanggal Daftar</th>
                  <th className="num">Total Stok</th>
                  <th>Status</th>
                  <th>Aksi</th>
                </tr>
              </thead>
              <tbody>
                {rows.data.map((row) => (
                  <tr key={row.outletId}>
                    <td style={{ fontWeight: 600 }}>{row.nama}</td>
                    <td>{new Date(row.tanggalDaftar).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })}</td>
                    <td className="num">{row.totalStok.toLocaleString('id-ID')}</td>
                    <td><StatusPill status={row.statusTerburuk} /></td>
                    <td>
                      <div style={{ display: 'flex', gap: 8 }}>
                        <Link className="sm-btn sm-btn-outline sm-btn-sm" to="/outlet/$outletId" params={{ outletId: String(row.outletId) }}>
                          Detail
                        </Link>
                        <RoleGate roles={['Superadmin', 'Supervisi']}>
                          <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => openEditModal(row)}>Edit</button>
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
          title={editingId === null ? 'Tambah Outlet' : 'Edit Outlet'}
          onClose={() => { setShowFormModal(false); setFormMessage(null) }}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" type="button" onClick={() => { setShowFormModal(false); setFormMessage(null) }}>Batal</button>
              <button className="sm-btn sm-btn-primary sm-btn-sm" type="button" disabled={saveMutation.isPending} onClick={() => saveMutation.mutate()}>Simpan</button>
            </>
          }
        >
          <div style={{ marginBottom: 12 }}>
            <label className="sm-label-caps" style={{ display: 'block', marginBottom: 6 }}>Nama Outlet</label>
            <input type="text" className="sm-frame" style={{ width: '100%' }} value={namaOutlet} onChange={(e) => setNamaOutlet(e.target.value)} />
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
          title="Hapus Outlet"
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
                  if (target) deleteMutation.mutate(target.outletId)
                }}
              >
                Hapus
              </button>
            </>
          }
        >
          <p className="sm-body-md" style={{ margin: 0 }}>
            Hapus <strong style={{ color: 'var(--sm-on-surface)' }}>{deleteTarget.nama}</strong>?
            Outlet dihapus (soft delete) beserta baris stoknya; riwayat transaksi tetap di audit log.
          </p>
        </Modal>
      ) : null}
    </div>
  )
}
