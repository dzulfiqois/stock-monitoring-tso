import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { RoleGate } from '../components/RoleGate'
import { StatusPill } from '../components/StatusPill'
import { Modal } from '../components/Modal'
import { stock, data, produkDisplay, wilayahDisplay } from '../lib/data'
import type { DashboardFilter, SalesAreaCardRow } from '../lib/data'

export const Route = createFileRoute('/_app/gudang-wilayah')({
  component: GudangWilayahPage,
})

function Format(value: number | null | undefined): string {
  return value === null || value === undefined ? '-' : value.toFixed(3).replace(/\.?0+$/, '')
}

function Card({ card, onDelete }: { card: SalesAreaCardRow; onDelete: () => void }) {
  const isMinyak = card.produk === 'MinyakTanah'
  return (
    <div className="sm-card" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <div>
          <div className="sm-headline-md">{wilayahDisplay(card.wilayah)}</div>
          <div className="sm-label-caps" style={{ marginTop: 2 }}>
            Sales Area · {isMinyak ? 'Minyak Tanah' : 'Gas Tabung'}
          </div>
        </div>
        <StatusPill status={card.statusTerburuk} />
      </div>

      <div className="sm-label-caps">
        Realisasi: {new Date(card.tanggal).toLocaleDateString('id-ID', { day: '2-digit', month: 'short', year: 'numeric' })}
      </div>

      {isMinyak ? (
        <>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 8, textAlign: 'center' }}>
            <div>
              <div className="sm-label-caps">Gudang</div>
              <div className="sm-title-sm">{Format(card.stokGudang)}</div>
            </div>
            <div>
              <div className="sm-label-caps">Agen</div>
              <div className="sm-title-sm">{Format(card.stokAgen)}</div>
            </div>
            <div>
              <div className="sm-label-caps">Outlet</div>
              <div className="sm-title-sm">{Format(card.stokOutlet)}</div>
            </div>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span className="sm-label-caps">Total Stok</span>
            <span className="sm-title-sm" style={{ color: 'var(--sm-primary)' }}>
              {Format(card.totalStok)} KL
            </span>
          </div>
          <div className="sm-body-md" style={{ fontSize: 12 }}>
            Terjual: {Format(card.stokHabisTerjual)} KL · Intransit: {Format(card.stokIntransit)} KL
          </div>
        </>
      ) : (
        <>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            <div className="sm-label-caps">Stok Gudang Wilayah</div>
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              <span className="sm-chip chip-55"><span className="dot" /> 5.5 kg: {Format(card.stokGudang55Kg)}</span>
              <span className="sm-chip chip-12"><span className="dot" /> 12 kg: {Format(card.stokGudang12Kg)}</span>
              <span className="sm-chip chip-50"><span className="dot" /> 50 kg: {Format(card.stokGudang50Kg)}</span>
            </div>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
            <span className="sm-label-caps">Total Stok (Gas Tabung)</span>
            <span className="sm-title-sm" style={{ color: 'var(--sm-primary)' }}>
              {card.totalStok.toLocaleString('id-ID')} Tabung
            </span>
          </div>
        </>
      )}

      {card.agenRows.length > 0 ? (
        <div style={{ borderTop: '1px solid var(--sm-outline-variant)', paddingTop: 10 }}>
          <Link
            className="sm-btn sm-btn-outline sm-btn-sm"
            style={{ width: '100%', justifyContent: 'center' }}
            to="/wilayah/$wilayah/agen"
            params={{ wilayah: card.wilayah }}
          >
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>
              storefront
            </span>{' '}
            Agen ({card.agenRows.length})
          </Link>
        </div>
      ) : null}

      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          borderTop: '1px solid var(--sm-outline-variant)',
          paddingTop: 10,
          marginTop: 'auto',
        }}
      >
        <Link
          className="sm-btn sm-btn-outline sm-btn-sm"
          to="/sales-area/$wilayah/$produk"
          params={{ wilayah: card.wilayah, produk: isMinyak ? 'MinyakTanah' : 'Lpg' }}
        >
          Detail
        </Link>
        <RoleGate roles={['Superadmin']}>
          <button className="sm-btn sm-btn-danger sm-btn-sm" onClick={onDelete} type="button">
            <span className="material-symbols-outlined" style={{ fontSize: 16 }}>
              delete
            </span>{' '}
            Hapus
          </button>
        </RoleGate>
      </div>
    </div>
  )
}

function GudangWilayahPage() {
  const queryClient = useQueryClient()
  const [filterObyek, setFilterObyek] = useState<DashboardFilter>('Semua')
  const [deleteTarget, setDeleteTarget] = useState<SalesAreaCardRow | null>(null)

  const cards = useQuery({
    queryKey: ['cards', filterObyek],
    queryFn: () => data.cards(filterObyek),
  })
  const summary = useQuery({ queryKey: ['summary'], queryFn: data.summary })

  const deleteMutation = useMutation({
    mutationFn: async (target: SalesAreaCardRow) => {
      for (const id of target.entityIds) {
        await stock.delete(id)
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cards'] })
      queryClient.invalidateQueries({ queryKey: ['summary'] })
    },
  })

  return (
    <div>
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'flex-end',
          justifyContent: 'space-between',
          gap: 16,
          marginBottom: 20,
        }}
      >
        <div>
          <h1 className="sm-display-lg">Gudang Wilayah</h1>
          <p className="sm-body-md" style={{ maxWidth: 660, margin: '4px 0 0' }}>
            Pemantauan stok per Sales Area (Wilayah × Produk) di wilayah Papua-Maluku.
          </p>
        </div>
        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          <select
            className="sm-filter"
            style={{ width: 'auto' }}
            value={filterObyek}
            onChange={(e) => setFilterObyek(e.target.value as DashboardFilter)}
          >
            <option value="Semua">Semua Obyek</option>
            <option value="MinyakTanah">Minyak Tanah</option>
            <option value="GasLpg">Gas LPG</option>
          </select>
          <RoleGate roles={['Superadmin', 'Operator']}>
            <a className="sm-btn sm-btn-primary sm-btn-sm" href="/sales-area/register">
              <span className="material-symbols-outlined" style={{ fontSize: 18 }}>
                add
              </span>{' '}
              Tambah Gudang
            </a>
          </RoleGate>
        </div>
      </div>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit,minmax(220px,1fr))',
          gap: 16,
          marginBottom: 20,
        }}
      >
        <div className="sm-kpi">
          <span className="sm-label-caps">Total Stok (Wilayah-Papua-Maluku)</span>
          <div>
            <span className="value">{summary.data?.totalStok.toLocaleString('id-ID') ?? '0'}</span>{' '}
            <span className="unit">satuan</span>
          </div>
        </div>
        <div className={`sm-kpi ${(summary.data?.produkKritis ?? 0) > 0 ? 'sm-kpi-kritis' : ''}`}>
          <span className="sm-label-caps">Produk Kritis (CD &lt; 3)</span>
          <div>
            <span className="value">{summary.data?.produkKritis ?? 0}</span>{' '}
            <span className="unit">entitas</span>
          </div>
        </div>
        <div className="sm-kpi">
          <span className="sm-label-caps">Exhaust Terdekat</span>
          <div>
            <span className="value" style={{ fontSize: 22, lineHeight: '30px' }}>
              {summary.data?.exhaustTerdekat
                ? new Date(summary.data.exhaustTerdekat).toLocaleDateString('id-ID', {
                    day: '2-digit',
                    month: 'short',
                    year: 'numeric',
                  })
                : '-'}
            </span>
          </div>
        </div>
      </div>

      {cards.isPending ? (
        <div className="sm-body-md" style={{ padding: '40px 0' }}>
          Memuat...
        </div>
      ) : !cards.data || cards.data.length === 0 ? (
        <div className="sm-card" style={{ padding: 24, textAlign: 'center' }}>
          <span className="material-symbols-outlined" style={{ fontSize: 40, color: 'var(--sm-outline)' }}>
            warehouse
          </span>
          <div className="sm-body-md" style={{ marginTop: 8 }}>
            Belum ada data Sales Area. Gunakan "Tambah Gudang" untuk mendaftarkan entitas baru.
          </div>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(300px,1fr))', gap: 16 }}>
          {cards.data.map((card) => (
            <Card key={`${card.wilayah}-${card.produk}`} card={card} onDelete={() => setDeleteTarget(card)} />
          ))}
        </div>
      )}

      {deleteTarget ? (
        <Modal
          title="Hapus Sales Area"
          onClose={() => setDeleteTarget(null)}
          footer={
            <>
              <button className="sm-btn sm-btn-outline sm-btn-sm" onClick={() => setDeleteTarget(null)} type="button">
                Batal
              </button>
              <button
                className="sm-btn sm-btn-danger sm-btn-sm"
                type="button"
                disabled={deleteMutation.isPending}
                onClick={() => deleteMutation.mutate(deleteTarget)}
              >
                Hapus
              </button>
            </>
          }
        >
          <p className="sm-body-md" style={{ margin: 0 }}>
            Hapus <strong style={{ color: 'var(--sm-on-surface)' }}>{wilayahDisplay(deleteTarget.wilayah)} — {produkDisplay(deleteTarget.produk)}</strong>?
            Entitas dihapus (soft delete); riwayat transaksi tetap di audit log.
          </p>
        </Modal>
      ) : null}
    </div>
  )
}
