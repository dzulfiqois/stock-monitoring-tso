using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Seed;

/// <summary>
/// Generator identitas agen + stok awal (mock). Aturan (keputusan user 2026-08):
/// - Setiap Gudang Wilayah memayungi 2–3 agen.
/// - Total stok awal seluruh agen per (Wilayah × Produk) = 50% stok Gudang Wilayah,
///   dibagi rata ke tiap agen; DOT gudang dibagi rata juga (sisa ke agen terakhir).
/// - Deterministik agar dapat diuji; nama agen = "Agen {n} {wilayah}".
/// </summary>
public static class AgenMockSeeder
{
    /// <summary>Jumlah agen per wilayah (2–3, sesuai permintaan user).</summary>
    public static int AgenCount(Wilayah wilayah) => wilayah switch
    {
        Wilayah.Maluku => 3,
        Wilayah.PapuaBarat => 2,
        Wilayah.PapuaBaratDaya => 3,
        Wilayah.MalukuUtara => 2,
        Wilayah.PapuaTengah => 3,
        Wilayah.PapuaSelatanPegunungan => 2,
        Wilayah.Papua => 3,
        _ => 2,
    };

    /// <summary>
    /// Bagi total ke n bagian rata; sisa pembagian diberikan ke bagian terakhir sehingga
    /// jumlah seluruh bagian = total. Untuk total bulat (tabung LPG) hasilnya bulat;
    /// untuk desimal (Kiloliter) dibulatkan ke 2 angka desimal.
    /// </summary>
    public static IReadOnlyList<decimal> SplitEqual(decimal total, int n)
    {
        if (n <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), "Jumlah agen harus > 0.");
        }

        if (total <= 0)
        {
            return Enumerable.Repeat(0m, n).ToList();
        }

        if (total == Math.Truncate(total))
        {
            var baseInt = (long)Math.Floor(total / n);
            var sisa = (long)total - baseInt * n;
            var list = Enumerable.Repeat((decimal)baseInt, n).ToList();
            list[n - 1] = baseInt + sisa;
            return list;
        }

        var baseQty = Math.Floor(total / n * 100m) / 100m;
        var remainder = total - baseQty * n;
        var result = Enumerable.Repeat(baseQty, n).ToList();
        result[n - 1] = baseQty + remainder;
        return result;
    }

    public static string AgenName(Wilayah wilayah, int urutan) => $"Agen {urutan} {wilayah.DisplayName()}";
}
