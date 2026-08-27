namespace StockMonitorTso.Domain.Entities;

/// <summary>
/// Identitas outlet (pangkalan/pengecer) milik satu Agen. 2 outlet per agen saat seed,
/// one-to-many tanpa limit (tambah/kurangi bebas). Stok outlet dilacak per (Outlet × Produk)
/// lewat <see cref="StokEntitas"/> (Tier.Outlet + OutletId).
/// </summary>
public sealed class Outlet
{
    public int Id { get; set; }

    /// <summary>Nama outlet — unik case-insensitive per AgenId.</summary>
    public string Nama { get; set; } = "";

    public int AgenId { get; set; }

    public Agen Agen { get; set; } = null!;

    public Wilayah Wilayah { get; set; }

    public DateTime TanggalDaftar { get; set; }

    public string? Keterangan { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<StokEntitas> StokEntitas { get; set; } = new List<StokEntitas>();
}
