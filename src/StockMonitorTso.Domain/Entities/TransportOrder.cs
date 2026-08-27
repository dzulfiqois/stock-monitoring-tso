namespace StockMonitorTso.Domain.Entities;

public enum TransportOrderStatus
{
    Committed,
    StockImpacted,
    FlagTertunda,
}

public sealed class TransportOrder
{
    public int Id { get; set; }

    public string OrderNo { get; set; } = "";

    public string MitraId { get; set; } = "";

    public string MitraNamaSnapshot { get; set; } = "";

    public decimal TarifSnapshot { get; set; }

    public string SatuanTarifSnapshot { get; set; } = "";

    public decimal EstimasiBiayaSnapshot { get; set; }

    public Wilayah WilayahTujuan { get; set; }

    public string RuteAsal { get; set; } = "Pusat";

    public string RuteTujuan { get; set; } = "";

    public Produk Produk { get; set; }

    public decimal Kuantitas { get; set; }

    public string Satuan { get; set; } = "";

    public DateTime TanggalKeberangkatan { get; set; }

    public DateTime Eta { get; set; }

    public TransportOrderStatus Status { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? InvoiceGeneratedAt { get; set; }

    public string? InvoiceNo { get; set; }

    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
}
