using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockMonitorTso.Infrastructure.Seed;

/// <summary>
/// Memuat seed stok LPG dari `seeds/lpg-stok.json` (konversi satu kali dari workbook
/// `Monitoring Tabung RPM(1).xlsx` via ExcelStockSeeder — 42 baris: 21 Gudang Wilayah +
/// 21 Outlet, 7 wilayah × 3 SKU). Baris Gudang Wilayah membawa Rencana Kedatangan
/// (hingga 3 slot); baris Outlet tanpa.
/// </summary>
public static class LpgStokSeeder
{
    public static IReadOnlyList<SeedStokRow> Load(string filePath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };
        var rows = JsonSerializer.Deserialize<List<SeedStokRow>>(File.ReadAllText(filePath), options)
            ?? throw new InvalidOperationException($"Seed LPG kosong: '{filePath}'.");
        return rows;
    }
}
