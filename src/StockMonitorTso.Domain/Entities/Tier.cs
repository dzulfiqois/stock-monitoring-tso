namespace StockMonitorTso.Domain.Entities;

/// <summary>
/// Hirarki distribusi kanonik: Pusat → Gudang Wilayah → Agen → Outlet (STOCK §2.c).
/// Urutan enum mengikuti hirarki agar sortir default naik dari gudang ke outlet.
/// </summary>
public enum Tier
{
    GudangWilayah,
    Agen,
    Outlet,
}

public static class TierInfo
{
    public static string DisplayName(this Tier tier) => tier switch
    {
        Tier.GudangWilayah => "Gudang Wilayah",
        Tier.Agen => "Agen",
        Tier.Outlet => "Outlet",
        _ => tier.ToString(),
    };
}

public enum Status
{
    Aman,
    Warning,
    Kritis,
}

public static class StatusInfo
{
    public static string DisplayName(this Status status) => status switch
    {
        Status.Aman => "Aman",
        Status.Warning => "Warning",
        Status.Kritis => "Kritis",
        _ => status.ToString(),
    };
}
