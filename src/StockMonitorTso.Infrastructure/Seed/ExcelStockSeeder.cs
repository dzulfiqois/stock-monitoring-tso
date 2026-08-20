using System.Globalization;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Excel;

namespace StockMonitorTso.Infrastructure.Seed;

/// <summary>
/// Memuat seed stok LPG dari `Monitoring Tabung RPM(1).xlsx` (sheet Agen & Outlet).
/// Tanggal serial Excel dikonversi via OADate. Baris sheet "Agen" (gudang-agen depo regional)
/// kini dipetakan ke tier Gudang Wilayah; baris sheet "Outlet" → tier Outlet.
/// Data di-map ke (Wilayah × Produk × Tier) beserta Rencana Kedatangan (hingga 3 slot).
/// Minyak tanah di-seed dari contoh spec (Excel hanya berisi LPG).
/// </summary>
public static class ExcelStockSeeder
{
    public const string AgenSheet = "Agen 16.06.26";
    public const string OutletSheet = "Outlet 16.06.2026";

    public static IReadOnlyList<SeedStokRow> LoadLpgGudangWilayah(string filePath)
        => LoadSheet(filePath, AgenSheet, Tier.GudangWilayah, hasRencana2: true, hasRencana3: true);

    public static IReadOnlyList<SeedStokRow> LoadLpgOutlet(string filePath)
        => LoadSheet(filePath, OutletSheet, Tier.Outlet, hasRencana2: false, hasRencana3: false);

    private static IReadOnlyList<SeedStokRow> LoadSheet(
        string filePath,
        string sheetName,
        Tier tier,
        bool hasRencana2,
        bool hasRencana3)
    {
        var cells = XlsxReader.ReadSheet(filePath, sheetName);
        var rows = new List<SeedStokRow>();
        Wilayah? currentWilayah = null;

        foreach (var row in EnumerateDataRows(cells))
        {
            if (row.WilayahRaw is not null && row.WilayahRaw.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (row.WilayahRaw is not null)
            {
                currentWilayah = ParseWilayah(row.WilayahRaw);
            }

            if (currentWilayah is null || row.Produk is null)
            {
                continue;
            }

            var produk = ParseProduk(row.Produk);
            var rencana = new List<SeedRencana>();
            AddRencana(rencana, 1, row.Rencana1Next, row.Rencana1Eta);
            if (hasRencana2)
            {
                AddRencana(rencana, 2, row.Rencana2Next, row.Rencana2Eta);
            }

            if (hasRencana3)
            {
                AddRencana(rencana, 3, row.Rencana3Next, row.Rencana3Eta);
            }

            rows.Add(new SeedStokRow
            {
                Wilayah = currentWilayah.Value,
                Produk = produk,
                Tier = tier,
                TanggalStokAwal = ParseDate(row.TanggalStokAwal!),
                Stok = ParseDecimal(row.Stok!),
                Dot = ParseDecimal(row.Dot!),
                RencanaKedatangan = rencana,
            });
        }

        return rows;
    }

    public static IReadOnlyList<SeedStokRow> LoadMinyakTanahSample()
    {
        // Contoh dari STOCK_MONITORING_SPEC.md §2.a + mock untuk 7 wilayah kanonik
        // (minyak tanah hanya LPG di Excel; di-seed mock agar tabel tampil lengkap).
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

    private static IEnumerable<RawRow> EnumerateDataRows(IReadOnlyDictionary<string, string> cells)
    {
        // Data dimulai dari baris 3 (baris 1-2 = header dua tingkat).
        for (var r = 3; r <= 60; r++)
        {
            var get = (string col) => cells.GetValueOrDefault($"{col}{r}");
            var wilayah = get("A");
            if (wilayah is null && r > 3)
            {
                // pastikan setidaknya ada sel di baris; jika kolom A dan B kosong, hentikan
                if (get("B") is null && get("C") is null)
                {
                    break;
                }
            }

            yield return new RawRow
            {
                WilayahRaw = wilayah,
                Produk = get("B"),
                Stok = get("C"),
                Dot = get("F"),
                TanggalStokAwal = get("H"),
                Rencana1Next = get("J"),
                Rencana1Eta = get("K"),
                Rencana2Next = get("N"),
                Rencana2Eta = get("O"),
                Rencana3Next = get("R"),
                Rencana3Eta = get("S"),
            };
        }
    }

    private static void AddRencana(List<SeedRencana> list, int urutan, string? next, string? eta)
    {
        if (next is null || eta is null)
        {
            return;
        }

        list.Add(new SeedRencana { Urutan = urutan, NextSupply = ParseDecimal(next), Eta = ParseDate(eta) });
    }

    private static Wilayah ParseWilayah(string raw) => raw.Trim().ToUpperInvariant() switch
    {
        "MALUKU" => Wilayah.Maluku,
        "PAPUA BARAT" => Wilayah.PapuaBarat,
        "PAPUA BARAT DAYA" => Wilayah.PapuaBaratDaya,
        "MALUKU UTARA" => Wilayah.MalukuUtara,
        "PAPUA TENGAH" => Wilayah.PapuaTengah,
        "PAPUA SELATAN-PEGUNUNGAN" => Wilayah.PapuaSelatanPegunungan,
        "PAPUA" => Wilayah.Papua,
        _ => throw new InvalidOperationException($"Wilayah tidak dikenal: '{raw}'"),
    };

    private static Produk ParseProduk(string raw) => raw.Trim() switch
    {
        "5.5 kg" => Produk.Lpg5_5Kg,
        "12 kg" => Produk.Lpg12Kg,
        "50 kg" => Produk.Lpg50Kg,
        _ => throw new InvalidOperationException($"Produk tidak dikenal: '{raw}'"),
    };

    private static decimal ParseDecimal(string raw)
        => decimal.Parse(raw, CultureInfo.InvariantCulture);

    private static DateTime ParseDate(string raw)
    {
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
        {
            return DateTime.FromOADate(serial);
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Tanggal tidak dikenal: '{raw}'");
    }

    private sealed class RawRow
    {
        public string? WilayahRaw { get; init; }

        public string? Produk { get; init; }

        public string? Stok { get; init; }

        public string? Dot { get; init; }

        public string? TanggalStokAwal { get; init; }

        public string? Rencana1Next { get; init; }

        public string? Rencana1Eta { get; init; }

        public string? Rencana2Next { get; init; }

        public string? Rencana2Eta { get; init; }

        public string? Rencana3Next { get; init; }

        public string? Rencana3Eta { get; init; }
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
