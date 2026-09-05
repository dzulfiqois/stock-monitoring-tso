import { apiFetch } from './api'

export type Wilayah =
  | 'Maluku'
  | 'PapuaBarat'
  | 'PapuaBaratDaya'
  | 'MalukuUtara'
  | 'PapuaTengah'
  | 'PapuaSelatanPegunungan'
  | 'Papua'

export type Produk = 'Lpg5_5Kg' | 'Lpg12Kg' | 'Lpg50Kg' | 'MinyakTanah'

export type Tier = 'GudangWilayah' | 'Agen' | 'Outlet'

export type Status = 'Aman' | 'Warning' | 'Kritis'

export type DashboardFilter = 'Semua' | 'MinyakTanah' | 'GasLpg'

export const WILAYAH_ALL: Wilayah[] = [
  'Maluku',
  'PapuaBarat',
  'PapuaBaratDaya',
  'MalukuUtara',
  'PapuaTengah',
  'PapuaSelatanPegunungan',
  'Papua',
]

export function wilayahDisplay(w: string): string {
  const names: Record<string, string> = {
    Maluku: 'Maluku',
    PapuaBarat: 'Papua Barat',
    PapuaBaratDaya: 'Papua Barat Daya',
    MalukuUtara: 'Maluku Utara',
    PapuaTengah: 'Papua Tengah',
    PapuaSelatanPegunungan: 'Papua Selatan-Pegunungan',
    Papua: 'Papua',
  }
  return names[w] ?? w
}

export function produkDisplay(p: string): string {
  const names: Record<string, string> = {
    Lpg5_5Kg: 'LPG 5.5 kg',
    Lpg12Kg: 'LPG 12 kg',
    Lpg50Kg: 'LPG 50 kg',
    MinyakTanah: 'Minyak Tanah',
  }
  return names[p] ?? p
}

export function tierDisplay(t: string): string {
  const names: Record<string, string> = {
    GudangWilayah: 'Gudang Wilayah',
    Agen: 'Agen',
    Outlet: 'Outlet',
  }
  return names[t] ?? t
}

export function satuanProduk(p: string): string {
  return p === 'MinyakTanah' ? 'Kiloliter' : 'Tabung'
}

function dateOnly(value: string): string {
  return value.split('T')[0]
}

// ---------- Dashboard ----------

export interface DashboardSummary {
  totalStok: number
  produkKritis: number
  exhaustTerdekat: string | null
}

export interface ChartPointRow {
  label: string
  agen: number
  outlet: number
  critical: boolean
}

export interface SektorRow {
  nama: string
  totalStok: number
  unit: string
  outletKritis: number
  statusSektor: Status | null
}

export interface MetrikMinyakRow {
  wilayah: Wilayah
  tanggal: string
  stokAgen: number | null
  stokOutlet: number | null
  stokHabisTerjual: number | null
  stokIntransit: number | null
  statusAgen: Status | null
  statusOutlet: Status | null
  keterangan: string | null
}

export interface RingkasanResponse {
  gas: SektorRow
  minyak: SektorRow
  gasChart: ChartPointRow[]
  minyakChart: ChartPointRow[]
  metrikMinyak: MetrikMinyakRow[]
}

export interface SalesAreaCardRow {
  wilayah: Wilayah
  produk: Produk
  tanggal: string
  stokGudang: number | null
  stokAgen: number | null
  stokOutlet: number | null
  totalStok: number
  statusTerburuk: Status | null
  stokHabisTerjual: number | null
  stokIntransit: number | null
  keterangan: string | null
  entityIds: number[]
  agenRows: { agenId: number; nama: string; totalStok: number; status: Status | null }[]
  stokGudang55Kg: number | null
  stokGudang12Kg: number | null
  stokGudang50Kg: number | null
}

export interface SalesAreaDetailRow {
  tier: Tier
  produk: Produk
  stokEntitasId: number
  tanggalStokAwal: string
  stok: number
  dot: number
  cd: number | null
  status: Status | null
  exhaustDate: string | null
  stokHabisTerjual: number | null
  stokIntransit: number | null
}

export interface StockTransactionView {
  tanggal: string
  type: string
  kuantitas: number
  tujuan: string | null
  catatan: string | null
  stokSesudah: number
}

export interface SalesAreaDetail {
  wilayah: Wilayah
  produk: Produk
  totalStok: number
  cdTerburuk: number | null
  statusArea: Status | null
  rows: SalesAreaDetailRow[]
  transactions: StockTransactionView[]
}

export interface LpgDashboardRow {
  wilayah: Wilayah
  produk: Produk
  stokGudang: number
  dotGudang: number
  cdGudang: number | null
  statusGudang: Status | null
  stokAgen: number
  dotAgen: number
  cdAgen: number | null
  statusAgen: Status | null
  exhaustAgen: string | null
  stokOutlet: number
  dotOutlet: number
  cdOutlet: number | null
  statusOutlet: Status | null
  exhaustOutlet: string | null
  nextSupplyEta: string | null
}

export interface MinyakTanahDashboardRow {
  wilayah: Wilayah
  tanggal: string
  stokGudang: number | null
  cdGudang: number | null
  statusGudang: Status | null
  stokAgen: number | null
  cdAgen: number | null
  statusAgen: Status | null
  stokOutlet: number | null
  cdOutlet: number | null
  statusOutlet: Status | null
  stokHabisTerjual: number | null
  stokIntransit: number | null
  keterangan: string | null
}

export interface AgenInventarisRow {
  agenId: number
  nama: string
  tanggalDaftar: string
  totalStok: number
  jumlahProduk: number
  statusTerburuk: Status | null
}

export interface AgenProdukRow {
  produk: Produk
  stokEntitasId: number
  tanggalStokAwal: string
  stok: number
  dot: number
  cd: number | null
  status: Status | null
  exhaustDate: string | null
  stokHabisTerjual: number | null
  stokIntransit: number | null
}

export interface AgenDetail {
  agenId: number
  nama: string
  wilayah: Wilayah
  tanggalDaftar: string
  totalStok: number
  totalDot: number
  cdTerburuk: number | null
  statusArea: Status | null
  exhaustTerdekat: string | null
  rows: AgenProdukRow[]
  transactions: StockTransactionView[]
}

export interface AgenTransferTargetRow {
  agenId: number
  nama: string
  products: { produk: Produk; stokEntitasId: number }[]
}

export interface OutletInventarisRow {
  outletId: number
  nama: string
  tanggalDaftar: string
  totalStok: number
  jumlahProduk: number
  statusTerburuk: Status | null
}

export interface OutletDetail {
  outletId: number
  nama: string
  agenId: number
  wilayah: Wilayah
  tanggalDaftar: string
  totalStok: number
  totalDot: number
  cdTerburuk: number | null
  statusArea: Status | null
  exhaustTerdekat: string | null
  rows: AgenProdukRow[]
  transactions: StockTransactionView[]
}

export interface OutletTransferTargetRow {
  outletId: number
  nama: string
  products: { produk: Produk; stokEntitasId: number }[]
}

export const data = {
  summary: () => apiFetch<DashboardSummary>('/api/dashboard/summary'),

  cards: (filter: DashboardFilter) =>
    apiFetch<SalesAreaCardRow[]>(`/api/dashboard/cards?filter=${encodeURIComponent(filter)}`),

  salesAreaDetail: (wilayah: Wilayah, produk: Produk) =>
    apiFetch<SalesAreaDetail | null>(`/api/dashboard/sales-area/${wilayah}/${produk}`),

  lpgDetail: (wilayah: Wilayah) =>
    apiFetch<SalesAreaDetail | null>(`/api/dashboard/sales-area-lpg/${wilayah}`),

  lpgRows: () => apiFetch<LpgDashboardRow[]>('/api/dashboard/lpg-rows'),

  minyakRows: () => apiFetch<MinyakTanahDashboardRow[]>('/api/dashboard/minyak-rows'),

  agenInventaris: (wilayah: Wilayah) =>
    apiFetch<AgenInventarisRow[]>(`/api/dashboard/agen-inventaris/${wilayah}`),

  agenDetail: (agenId: number) =>
    apiFetch<AgenDetail | null>(`/api/dashboard/agen/${agenId}`),

  agenTransferTargets: (wilayah: Wilayah) =>
    apiFetch<AgenTransferTargetRow[]>(`/api/dashboard/agen-transfer-targets/${wilayah}`),

  outletInventaris: (agenId: number) =>
    apiFetch<OutletInventarisRow[]>(`/api/dashboard/outlet-inventaris/${agenId}`),

  outletDetail: (outletId: number) =>
    apiFetch<OutletDetail | null>(`/api/dashboard/outlet/${outletId}`),

  outletTransferTargets: (agenId: number) =>
    apiFetch<OutletTransferTargetRow[]>(`/api/dashboard/outlet-transfer-targets/${agenId}`),
}

// ---------- Stock writes ----------

export interface RegisterStockBody {
  wilayah: Wilayah
  produk: Produk
  tier: Tier
  tanggalStokAwal: string
  stok: number
  dot: number
  stokHabisTerjual?: number
  stokIntransit?: number
  keterangan?: string
}

export interface UpdateStockDetailBody {
  dot: number
  tanggalStokAwal: string
  stokHabisTerjual?: number | null
  stokIntransit?: number | null
  keterangan?: string | null
}

export const stock = {
  register: (body: RegisterStockBody) =>
    apiFetch<{ id: number }>('/api/stock', { method: 'POST', body: JSON.stringify(body) }),

  updateDetail: (id: number, body: UpdateStockDetailBody) =>
    apiFetch<unknown>(`/api/stock/${id}`, { method: 'PUT', body: JSON.stringify(body) }),

  transact: (id: number, type: 'Receive' | 'Issue' | 'Adjust' | 'Transfer', kuantitas: number, tujuanId?: number, catatan?: string) =>
    apiFetch<{ id: number; stok: number }>(`/api/stock/${id}/transact`, {
      method: 'POST',
      body: JSON.stringify({ type, kuantitas, tujuanId, catatan }),
    }),

  delete: (id: number) => apiFetch<void>(`/api/stock/${id}`, { method: 'DELETE' }),
}

// ---------- Agen / Outlet ----------

export const agen = {
  list: (wilayah: Wilayah) => data.agenInventaris(wilayah),
  detail: (agenId: number) => data.agenDetail(agenId),
  create: (body: { nama: string; wilayah: Wilayah; keterangan?: string }) =>
    apiFetch<{ id: number }>('/api/agen', { method: 'POST', body: JSON.stringify(body) }),
  update: (agenId: number, body: { nama: string; keterangan?: string }) =>
    apiFetch<unknown>(`/api/agen/${agenId}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (agenId: number) => apiFetch<void>(`/api/agen/${agenId}`, { method: 'DELETE' }),
  transferFromWarehouse: (
    agenId: number,
    wilayah: Wilayah,
    quantities: Partial<Record<Produk, number>>,
    catatan?: string,
  ) =>
    apiFetch<void>(`/api/agen/${agenId}/transfer-from-warehouse`, {
      method: 'POST',
      body: JSON.stringify({ wilayah, quantities, catatan }),
    }),
}

export const outlet = {
  list: (agenId: number) => data.outletInventaris(agenId),
  detail: (outletId: number) => data.outletDetail(outletId),
  create: (body: { nama: string; agenId: number; keterangan?: string }) =>
    apiFetch<{ id: number }>('/api/outlet', { method: 'POST', body: JSON.stringify(body) }),
  update: (outletId: number, body: { nama: string; keterangan?: string }) =>
    apiFetch<unknown>(`/api/outlet/${outletId}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (outletId: number) => apiFetch<void>(`/api/outlet/${outletId}`, { method: 'DELETE' }),
  transferFromAgen: (
    agenId: number,
    outletId: number,
    quantities: Partial<Record<Produk, number>>,
    catatan?: string,
  ) =>
    apiFetch<void>('/api/outlet/transfer-from-agen', {
      method: 'POST',
      body: JSON.stringify({ agenId, outletId, quantities, catatan }),
    }),
}

export { dateOnly }
