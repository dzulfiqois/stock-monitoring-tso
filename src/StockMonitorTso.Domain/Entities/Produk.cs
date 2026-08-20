namespace StockMonitorTso.Domain.Entities;

public enum Produk
{
    Lpg5_5Kg,
    Lpg12Kg,
    Lpg50Kg,
    MinyakTanah,
}

public static class ProdukInfo
{
    public static readonly Produk[] All =
    [
        Produk.Lpg5_5Kg,
        Produk.Lpg12Kg,
        Produk.Lpg50Kg,
        Produk.MinyakTanah,
    ];

    /// <summary>Berat per tabung (kg) untuk SKU LPG; null untuk minyak tanah.</summary>
    public static decimal? BeratKg(this Produk produk) => produk switch
    {
        Produk.Lpg5_5Kg => 5.5m,
        Produk.Lpg12Kg => 12m,
        Produk.Lpg50Kg => 50m,
        Produk.MinyakTanah => null,
        _ => null,
    };

    public static string DisplayName(this Produk produk) => produk switch
    {
        Produk.Lpg5_5Kg => "LPG 5.5 kg",
        Produk.Lpg12Kg => "LPG 12 kg",
        Produk.Lpg50Kg => "LPG 50 kg",
        Produk.MinyakTanah => "Minyak Tanah",
        _ => produk.ToString(),
    };

    /// <summary>Satuan kanonik: Tabung untuk LPG, Kiloliter untuk minyak tanah.</summary>
    public static string Satuan(this Produk produk) =>
        produk == Produk.MinyakTanah ? "Kiloliter" : "Tabung";
}
