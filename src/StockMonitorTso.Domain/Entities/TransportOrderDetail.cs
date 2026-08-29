namespace StockMonitorTso.Domain.Entities;

public sealed class TransportOrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public TransportOrder Order { get; set; } = null!;

    public Produk Produk { get; set; }

    public decimal Kuantitas { get; set; }

    public decimal TarifSnapshot { get; set; }

    public string SatuanTarifSnapshot { get; set; } = "";

    public decimal EstimasiBiayaSnapshot { get; set; }
}
