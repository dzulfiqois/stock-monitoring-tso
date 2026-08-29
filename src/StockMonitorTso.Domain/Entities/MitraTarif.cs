namespace StockMonitorTso.Domain.Entities;

public sealed class MitraTarif
{
    public int Id { get; set; }

    public string MitraId { get; set; } = "";

    public MitraTso Mitra { get; set; } = null!;

    public Produk Produk { get; set; }

    public decimal Tarif { get; set; }

    public string SatuanTarif { get; set; } = "";
}
