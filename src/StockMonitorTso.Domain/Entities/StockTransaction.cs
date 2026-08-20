namespace StockMonitorTso.Domain.Entities;

/// <summary>
/// Jenis transaksi stok. Konservasi (STOCK §2.c): perubahan stok hanya lewat transaksi
/// atomic debit-kredit; angka stok tidak pernah diedit langsung.
/// </summary>
public enum StockTransactionType
{
    /// <summary>Penerimaan stok (isi ulang) dari sumber eksternal (Pusat) — menambah stok tier (Qty&gt;0 → Stok+=Qty).</summary>
    Receive,

    /// <summary>
    /// Pengeluaran/penjualan (Qty&gt;0 → Stok−=Qty, auto StokHabisTerjual+=Qty). Untuk outtake normal (penjualan harian).
    /// </summary>
    Issue,

    /// <summary>Koreksi/opname stok fisik vs sistem — bisa +/- kuantitas (Qty≠0 → Stok+=Qty), dicatat dengan alasan.</summary>
    Adjust,

    /// <summary>Transfer antar-tier dalam satu wilayah (Gudang Wilayah → Agen → Outlet) — debit sumber = kredit tujuan, atomic.</summary>
    Transfer,
}
