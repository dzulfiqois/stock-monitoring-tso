namespace StockMonitorTso.Domain.Entities;

/// <summary>
/// Jejak transaksi stok (audit konservasi). Satu baris per transaksi; transfer antar-tier
/// mencatat debit sumber &amp; kredit tujuan dalam satu baris.
/// </summary>
public sealed class StockTransactionRecord
{
    public int Id { get; set; }

    public int StokEntitasId { get; set; }

    public StokEntitas StokEntitas { get; set; } = null!;

    public int? StokEntitasTujuanId { get; set; }

    public StockTransactionType Type { get; set; }

    public decimal Kuantitas { get; set; }

    public DateTime Tanggal { get; set; }

    public string? Catatan { get; set; }

    public decimal StokSumberSebelum { get; set; }

    public decimal StokSumberSesudah { get; set; }

    public decimal? StokTujuanSebelum { get; set; }

    public decimal? StokTujuanSesudah { get; set; }
}
