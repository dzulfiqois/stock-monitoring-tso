import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { Modal } from '../components/Modal'
import { RoleGate } from '../components/RoleGate'
import { tsoApi } from '../lib/tso'
import type { TransportOrder } from '../lib/tso'
import { wilayahDisplay, produkDisplay } from '../lib/data'

export const Route = createFileRoute('/_app/tso/')({
  component: TsoListPage,
})

function TsoListPage() {
  const queryClient = useQueryClient()
  const [deleteTarget, setDeleteTarget] = useState<TransportOrder | null>(null)

  const orders = useQuery({ queryKey: ['tso'], queryFn: tsoApi.list })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => tsoApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tso'] }),
  })

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h1 className="sm-display-lg">Transport Shipping Order</h1>
          <p className="sm-body-md" style={{ maxWidth: 660, margin: '4px 0 0' }}>
            Daftar order pengiriman Pusat → Gudang Wilayah.
          </p>
        </div>
        <RoleGate roles={['Superadmin', 'Operator']}>
          <Link className="sm-btn sm-btn-primary" to="/tso/create">
            <span className="material-symbols-outlined" style={{ fontSize: 18 }}>add</span> Buat TSO
          </Link>
        </RoleGate>
      </div>

      {orders.isPending ? (
        <div className="sm-body-md" style={{ padding: '40px 0' }}>Memuat...</div>
      ) : !orders.data || orders.data.length === 0 ? (
        <div className="sm-card" style={{ padding: 24, textAlign: 'center' }}>
          <span className="material-symbols-outlined" style={{ fontSize: 40, color: 'var(--sm-outline)' }}>local_shipping</span>
          <div className="sm-body-md" style={{ marginTop: 8 }}>Belum ada order TSO.</div>
        </div>
      ) : (
        <div className="sm-table-wrap">
          <div style={{ overflowX: 'auto' }}>
            <table className="sm-table">
              <thead>
                <tr>
                  <th>Order No</th>
                  <th>Mitra</th>
                  <th>Tujuan</th>
                  <th>Material</th>
                  <th className="num">Kuantitas</th>
                  <th>Keberangkatan</th>
                  <th>Status</th>
                  <th>Aksi</th>
                </tr>
              </thead>
              <tbody>
                {orders.data.map((o) => (
                  <tr key={o.id}>
                    <td style={{ fontWeight: 600 }}>{o.orderNo}</td>
                    <td>{o.mitraNamaSnapshot}</td>
                    <td>{wilayahDisplay(o.wilayahTujuan)}</td>
                    <td>{produkDisplay(o.produk)}</td>
                    <td className="num">{o.kuantitas.toFixed(2).replace(/\.?0+$/, '')} {o.satuan}</td>
                    <td>
                      {new Date(o.tanggalKeberangkatan).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })}
                    </td>
                    <td><span className="sm-pill sm-pill-neutral">{o.status}</span></td>
                    <td>
                      <div style={{ display: 'flex', gap: 8 }}>
                        <Link className="sm-btn sm-btn-outline sm-btn-sm" to="/tso/$tsoId" params={{ tsoId: String(o.id) }}>
                          Preview
                        </Link>
                        <RoleGate roles={['Superadmin', 'Supervisi']}>
                          <Link className="sm-btn sm-btn-outline sm-btn-sm" to="/tso/$tsoId/edit" params={{ tsoId: String(o.id) }}>
                            Update
                          </Link>
                        </RoleGate>
                        <RoleGate roles={['Superadmin']}>
                          <button className="sm-btn sm-btn-danger sm-btn-sm" type="button" onClick={() => setDeleteTarget(o)}>
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

      {deleteTarget ? (
        <Modal
          title="Hapus Order"
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
                  if (target) deleteMutation.mutate(target.id)
                }}
              >
                Hapus
              </button>
            </>
          }
        >
          <p className="sm-body-md" style={{ margin: 0 }}>
            Hapus order <strong>{deleteTarget.orderNo}</strong> — {deleteTarget.mitraNamaSnapshot} → {wilayahDisplay(deleteTarget.wilayahTujuan)}?
          </p>
        </Modal>
      ) : null}
    </div>
  )
}
