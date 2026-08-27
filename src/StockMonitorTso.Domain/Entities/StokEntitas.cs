namespace StockMonitorTso.Domain.Entities;

/// <summary>
/// Satu entitas stok. Granularitas: (Wilayah × Produk × Tier) untuk Gudang Wilayah/Outlet;
/// (Agen × Produk × Tier) untuk baris Agen (AgenId terisi).
/// Konservasi: stok hanya berubah via transaksi atomic (lihat Invarian Konservasi Stok, STOCK §2.c).
/// </summary>
public sealed class StokEntitas
{
    public int Id { get; set; }

    public Wilayah Wilayah { get; set; }

    public Produk Produk { get; set; }

    public Tier Tier { get; set; }

    /// <summary>
    /// Agen pemilik baris stok saat <see cref="Tier"/>=Agen (granularitas Agen × Produk);
    /// null untuk baris Gudang Wilayah / Outlet (granularitas Wilayah × Produk × Tier).
    /// </summary>
    public int? AgenId { get; set; }

    public Agen? Agen { get; set; }

    public int? OutletId { get; set; }

    public Outlet? Outlet { get; set; }

    /// <summary>Realisasi Tanggal / Tanggal Stok Awal — tanggal snapshot (same-day, STOCK §2.a).</summary>
    public DateTime TanggalStokAwal { get; set; }

    /// <summary>Stok dalam satuan kanonik: Tabung (LPG) atau Kiloliter (minyak tanah).</summary>
    public decimal Stok { get; set; }

    /// <summary>Daily Objective Throughput — laju penjualan harian (satuan/hari).</summary>
    public decimal DOT { get; set; }

    /// <summary>Khusus minyak tanah: stok yang telah terjual.</summary>
    public decimal? StokHabisTerjual { get; set; }

    /// <summary>Khusus minyak tanah: stok sedang dikirim menuju Gudang Wilayah.</summary>
    public decimal? StokIntransit { get; set; }

    public string? Keterangan { get; set; }

    /// <summary>Soft delete (STOCK §4.e) — entitas disembunyikan, riwayat transaksi tetap di audit log.</summary>
    public bool IsDeleted { get; set; }

    public ICollection<RencanaKedatangan> RencanaKedatangan { get; set; } = new List<RencanaKedatangan>();
}
