using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Seed;

/// <summary>
/// Generator identitas outlet per agen (mock). Paralel dengan <see cref="AgenMockSeeder"/>.
/// - Inisiasi 2 outlet per agen (one-to-many tanpa limit).
/// - Stok tiap outlet = 50% stok agen ÷ 2 (sisa ke outlet terakhir), DOT = agen DOT ÷ 2.
/// - Konservasi: stok agen didebit 50% via Transfer.
/// </summary>
public static class OutletMockSeeder
{
    public const int OutletPerAgen = 2;

    public static string OutletName(Agen agen, int urutan) => $"Outlet {urutan} {agen.Nama}";
}
