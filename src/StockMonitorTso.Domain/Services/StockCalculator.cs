using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Domain.Services;

/// <summary>
/// Perhitungan stok — sumber kebenaran: STOCK_MONITORING_SPEC.md §2.c.
/// JANGAN meniru rumus CD_n pada Excel acuan (Next Supply ÷ Σ CD — salah dimensi).
/// </summary>
public static class StockCalculator
{
    /// <summary>Coverage Days = Stok ÷ DOT. Null bila DOT ≤ 0 (F3: CD tidak dihitung).</summary>
    public static decimal? CoverageDays(decimal stok, decimal dot)
        => dot <= 0 ? null : stok / dot;

    /// <summary>Exhaust Date = Tanggal Stok Awal + CD.</summary>
    public static DateTime? ExhaustDate(DateTime tanggalStokAwal, decimal? cd)
        => cd.HasValue ? tanggalStokAwal.AddDays((double)cd.Value) : null;

    /// <summary>
    /// Status dari CD: Kritis &lt; 3; Warning 3 ≤ CD &lt; 7; Aman ≥ 7. Null = tidak dihitung.
    /// </summary>
    public static Status? StatusFor(decimal? cd)
    {
        if (!cd.HasValue)
        {
            return null;
        }

        if (cd.Value < 3m)
        {
            return Status.Kritis;
        }

        if (cd.Value < 7m)
        {
            return Status.Warning;
        }

        return Status.Aman;
    }

    /// <summary>
    /// CD setelah Rencana Kedatangan ke-n = (sisa stok saat ETA_n + Next Supply_n) ÷ DOT.
    /// Sisa stok saat ETA_n = Stok − DOT × (ETA_n − Tanggal Stok Awal).
    /// </summary>
    public static decimal? CoverageDaysAfterRencana(
        decimal stok,
        decimal dot,
        DateTime tanggalStokAwal,
        decimal nextSupply,
        DateTime eta)
    {
        if (dot <= 0)
        {
            return null;
        }

        var daysUntilEta = (decimal)(eta.Date - tanggalStokAwal.Date).Days;
        var sisaStok = stok - dot * daysUntilEta;
        return (sisaStok + nextSupply) / dot;
    }

    /// <summary>Exhaust Date ke-n = ETA_n + CD_n.</summary>
    public static DateTime? ExhaustDateAfterRencana(DateTime eta, decimal? cd)
        => cd.HasValue ? eta.AddDays((double)cd.Value) : null;

    /// <summary>Konversi tabung LPG ke metrik ton: Tabung × berat ukuran (kg) ÷ 1000. Minyak tanah tidak memiliki MT.</summary>
    public static decimal? MetricTon(Produk produk, decimal stok)
    {
        var berat = produk.BeratKg();
        return berat.HasValue ? stok * berat.Value / 1000m : null;
    }
}
