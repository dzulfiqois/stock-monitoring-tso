namespace StockMonitorTso.Domain.Entities;

public enum Wilayah
{
    Maluku,
    PapuaBarat,
    PapuaBaratDaya,
    MalukuUtara,
    PapuaTengah,
    PapuaSelatanPegunungan,
    Papua,
}

public static class WilayahInfo
{
    public static readonly Wilayah[] All =
    [
        Wilayah.Maluku,
        Wilayah.PapuaBarat,
        Wilayah.PapuaBaratDaya,
        Wilayah.MalukuUtara,
        Wilayah.PapuaTengah,
        Wilayah.PapuaSelatanPegunungan,
        Wilayah.Papua,
    ];

    public static string DisplayName(this Wilayah wilayah) => wilayah switch
    {
        Wilayah.Maluku => "Maluku",
        Wilayah.PapuaBarat => "Papua Barat",
        Wilayah.PapuaBaratDaya => "Papua Barat Daya",
        Wilayah.MalukuUtara => "Maluku Utara",
        Wilayah.PapuaTengah => "Papua Tengah",
        Wilayah.PapuaSelatanPegunungan => "Papua Selatan-Pegunungan",
        Wilayah.Papua => "Papua",
        _ => wilayah.ToString(),
    };
}
