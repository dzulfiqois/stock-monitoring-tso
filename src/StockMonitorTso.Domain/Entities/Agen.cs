namespace StockMonitorTso.Domain.Entities;

/// <summary>
/// Identitas agen (party) di bawah satu Gudang Wilayah. Satu gudang wilayah memayungi 2–3 agen.
/// Stok agen dilacak pada granularitas (Agen × Produk) lewat <see cref="StokEntitas"/> (Tier.Agen + AgenId).
/// </summary>
public sealed class Agen
{
    public int Id { get; set; }

    /// <summary>Nama agen — unik case-insensitive per wilayah.</summary>
    public string Nama { get; set; } = "";

    public Wilayah Wilayah { get; set; }

    /// <summary>Tanggal pendaftaran identitas agen pada aplikasi.</summary>
    public DateTime TanggalDaftar { get; set; }

    public string? Keterangan { get; set; }

    /// <summary>Soft delete — agen disembunyikan, riwayat transaksi stok tetap di audit log.</summary>
    public bool IsDeleted { get; set; }

    public ICollection<StokEntitas> StokEntitas { get; set; } = new List<StokEntitas>();
}
