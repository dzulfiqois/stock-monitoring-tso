import { apiFetch } from './api'

export type TsoProduk = 'Lpg5_5Kg' | 'Lpg12Kg' | 'Lpg50Kg' | 'MinyakTanah'

export interface TransportOrderDetail {
  id: number
  orderId: number
  produk: TsoProduk
  kuantitas: number
  tarifSnapshot: number
  satuanTarifSnapshot: string
  estimasiBiayaSnapshot: number
}

export interface TransportOrder {
  id: number
  orderNo: string
  mitraId: string
  mitraNamaSnapshot: string
  tarifSnapshot: number
  satuanTarifSnapshot: string
  estimasiBiayaSnapshot: number
  wilayahTujuan: string
  ruteAsal: string
  ruteTujuan: string
  jarakKm: number | null
  produk: TsoProduk
  kuantitas: number
  satuan: string
  details: TransportOrderDetail[]
  tanggalKeberangkatan: string
  eta: string
  status: 'Committed' | 'StockImpacted' | 'FlagTertunda'
  isDeleted: boolean
  createdAt: string
  createdBy: string | null
  updatedAt: string | null
  updatedBy: string | null
  invoiceGeneratedAt: string | null
  invoiceNo: string | null
  rowVersion: string
}

export interface MitraTarif {
  id: number
  mitraId: string
  produk: TsoProduk
  tarif: number
  satuanTarif: string
}

export interface MitraTsoView {
  id: string
  nama: string
  jenisKendaraan: string
  kapasitasMax: number
  satuanKapasitas: string
  rute: string[]
  areaCoverage: string[]
  kontak: string
  pic: string
  active: boolean
  tarif: number
  satuanTarif: string
  tarifs: MitraTarif[]
}

export interface TsoDetailBody {
  produk: TsoProduk
  kuantitas: number
}

export interface CreateTsoBody {
  mitraId: string
  wilayahTujuan: string
  produk: TsoProduk
  kuantitas: number
  tanggalKeberangkatan: string
  ruteAsal?: string
  ruteTujuan?: string
  jarakKm?: number
  details: TsoDetailBody[]
}

export interface UpdateTsoBody extends CreateTsoBody {
  rowVersion: string
}

export const tsoApi = {
  list: () => apiFetch<TransportOrder[]>('/api/tso'),
  get: (id: number) => apiFetch<TransportOrder>(`/api/tso/${id}`),
  create: (body: CreateTsoBody) =>
    apiFetch<TransportOrder>('/api/tso', { method: 'POST', body: JSON.stringify(body) }),
  update: (id: number, body: UpdateTsoBody) =>
    apiFetch<TransportOrder>(`/api/tso/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  remove: (id: number) => apiFetch<void>(`/api/tso/${id}`, { method: 'DELETE' }),
  resync: (id: number) => apiFetch<void>(`/api/tso/${id}/resync`, { method: 'POST' }),

  listMitra: () => apiFetch<MitraTsoView[]>('/api/mitra'),

  invoice: async (id: number): Promise<{ blob: Blob; orderNo: string }> => {
    const res = await fetch(`/api/tso/${id}/invoice`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${localStorage.getItem('sm_access') ?? ''}` },
    })
    if (!res.ok) {
      let detail = `Gagal generate invoice (HTTP ${res.status}).`
      try {
        const body = (await res.json()) as { detail?: string }
        detail = body.detail ?? detail
      } catch {
        // bukan JSON
      }
      throw new Error(detail)
    }
    return { blob: await res.blob(), orderNo: res.headers.get('Content-Disposition') ?? `invoice-${id}` }
  },
}

export interface MitraTarifBody {
  produk: TsoProduk
  tarif: number
  satuanTarif: string
}

export const mitraApi = {
  list: () => apiFetch<MitraTsoView[]>('/api/mitra'),
  create: (body: {
    id: string
    nama: string
    jenisKendaraan: string
    kapasitasMax: number
    satuanKapasitas: string
    rute: string[]
    areaCoverage: string[]
    kontak: string
    pic: string
    active: boolean
    tarifs: MitraTarifBody[]
  }) => apiFetch<MitraTsoView>('/api/mitra', { method: 'POST', body: JSON.stringify(body) }),
  update: (
    id: string,
    body: {
      nama: string
      jenisKendaraan: string
      kapasitasMax: number
      satuanKapasitas: string
      rute: string[]
      areaCoverage: string[]
      kontak: string
      pic: string
      active: boolean
    },
  ) => apiFetch<MitraTsoView>(`/api/mitra/${id}`, { method: 'PUT', body: JSON.stringify(body) }),
  updateTarif: (id: string, body: MitraTarifBody) =>
    apiFetch<MitraTsoView>(`/api/mitra/${id}/tarif`, { method: 'PUT', body: JSON.stringify(body) }),
}
