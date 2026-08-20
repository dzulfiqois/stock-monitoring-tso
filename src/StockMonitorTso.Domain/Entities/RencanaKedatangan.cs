namespace StockMonitorTso.Domain.Entities;

/// <summary>
/// Rencana Kedatangan (pasokan berikutnya dari pusat) pada satu entitas stok.
/// Hingga 3 slot (Urutan 1..3). CD_n dan Exhaust Date_n dihitung otomatis (STOCK §2.c).
/// </summary>
public sealed class RencanaKedatangan
{
    public int Id { get; set; }

    public int StokEntitasId { get; set; }

    public StokEntitas StokEntitas { get; set; } = null!;

    /// <summary>Nomor slot: 1..3.</summary>
    public int Urutan { get; set; }

    /// <summary>Kuantitas pasokan (Tabung untuk LPG; Kiloliter untuk minyak tanah).</summary>
    public decimal NextSupply { get; set; }

    /// <summary>Estimated Time of Arrival.</summary>
    public DateTime ETA { get; set; }
}
