namespace StockMonitorTso.Domain.Entities;

public sealed class MitraTso
{
    public string Id { get; set; } = "";

    public string Nama { get; set; } = "";

    public string JenisKendaraan { get; set; } = "";

    public decimal KapasitasMax { get; set; }

    public string SatuanKapasitas { get; set; } = "";

    public string[] Rute { get; set; } = Array.Empty<string>();

    public string[] AreaCoverage { get; set; } = Array.Empty<string>();

    public string Kontak { get; set; } = "";

    public string Pic { get; set; } = "";

    public bool Active { get; set; }

    /// <summary>Tarif legacy (single). Tetap untuk kompatibilitas, tapi per-jenis ada di <see cref="Tarifs"/>.</summary>
    public decimal Tarif { get; set; }

    public string SatuanTarif { get; set; } = "";

    public ICollection<MitraTarif> Tarifs { get; set; } = new List<MitraTarif>();
}
