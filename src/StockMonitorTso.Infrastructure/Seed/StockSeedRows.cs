using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Seed;

/// <summary>
/// Baris seed stok + contoh minyak tanah (hardcoded per spec — workbook tidak dipakai lagi;
/// LPG di-seed dari `seeds/lpg-stok.json` via <see cref="LpgStokSeeder"/>).
/// </summary>
public static class StockSeedRows
{
    public static IReadOnlyList<SeedStokRow> LoadMinyakTanahSample()
    {
        // Contoh dari spec §2.a + mock untuk 7 wilayah kanonik
        // (minyak tanah tidak ada di seed LPG; di-seed mock agar tabel tampil lengkap).
        // DOT & stok diatur agar status bervariasi (Aman/Warning/Kritis).
        var rows = new List<SeedStokRow>();
        foreach (var (wilayah, agen, outlet, dot, terjual, intransit, ket) in new[]
        {
            (Wilayah.Papua, 0.5m, 0.2m, 0.1m, 0.3m, 0m, "Contoh seed dari spec."),
            (Wilayah.Maluku, 0.8m, 0.4m, 0.15m, 0.3m, 0.1m, (string?)null),
            (Wilayah.PapuaBarat, 0.9m, 0.45m, 0.14m, 0.4m, 0.1m, (string?)null),
            (Wilayah.PapuaBaratDaya, 0.3m, 0.15m, 0.12m, 0.2m, 0m, (string?)null),
            (Wilayah.MalukuUtara, 0.05m, 0.1m, 0.13m, 0.2m, 0.2m, "Mock: stok rendah."),
            (Wilayah.PapuaTengah, 0.7m, 0.35m, 0.12m, 0.3m, 0.1m, (string?)null),
            (Wilayah.PapuaSelatanPegunungan, 0.6m, 0.3m, 0.11m, 0.25m, 0m, (string?)null),
        })
        {
            rows.Add(new SeedStokRow
            {
                Wilayah = wilayah,
                Produk = Produk.MinyakTanah,
                Tier = Tier.GudangWilayah,
                TanggalStokAwal = new DateTime(2026, 8, 5),
                Stok = agen,
                Dot = dot,
                StokHabisTerjual = terjual,
                StokIntransit = intransit,
                Keterangan = ket,
            });
            rows.Add(new SeedStokRow
            {
                Wilayah = wilayah,
                Produk = Produk.MinyakTanah,
                Tier = Tier.Outlet,
                TanggalStokAwal = new DateTime(2026, 8, 5),
                Stok = outlet,
                Dot = dot,
            });
        }

        return rows;
    }
}

public sealed class SeedStokRow
{
    public Wilayah Wilayah { get; init; }

    public Produk Produk { get; init; }

    public Tier Tier { get; init; }

    public DateTime TanggalStokAwal { get; init; }

    public decimal Stok { get; init; }

    public decimal Dot { get; init; }

    public decimal? StokHabisTerjual { get; init; }

    public decimal? StokIntransit { get; init; }

    public string? Keterangan { get; init; }

    public IReadOnlyList<SeedRencana> RencanaKedatangan { get; init; } = new List<SeedRencana>();
}

public sealed class SeedRencana
{
    public int Urutan { get; init; }

    public decimal NextSupply { get; init; }

    public DateTime Eta { get; init; }
}
