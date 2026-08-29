using System.Text.Json;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Seed;

public static class MitraTsoSeeder
{
    public static IReadOnlyList<MitraTso> Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var raw = JsonSerializer.Deserialize<List<RawMitra>>(json, options) ?? new List<RawMitra>();
        return raw.Select(r =>
        {
            var mitra = new MitraTso
            {
                Id = r.id,
                Nama = r.nama,
                JenisKendaraan = r.jenis_kendaraan,
                KapasitasMax = r.kapasitas_max,
                SatuanKapasitas = r.satuan_kapasitas,
                Rute = r.rute ?? Array.Empty<string>(),
                AreaCoverage = r.area_coverage ?? Array.Empty<string>(),
                Kontak = r.kontak ?? "",
                Pic = r.pic ?? "",
                Active = r.active,
                Tarif = r.tarif,
                SatuanTarif = r.satuan_tarif ?? "",
            };
            var tarifs = new List<MitraTarif>();
            var isKilo = r.satuan_kapasitas?.Contains("Kiloliter", StringComparison.OrdinalIgnoreCase) == true
                || r.satuan_tarif?.Contains("kiloliter", StringComparison.OrdinalIgnoreCase) == true;
            var isTabung = r.satuan_kapasitas?.Contains("Tabung", StringComparison.OrdinalIgnoreCase) == true
                || r.satuan_tarif?.Contains("tabung", StringComparison.OrdinalIgnoreCase) == true;
            if (isKilo)
            {
                tarifs.Add(new MitraTarif { MitraId = r.id, Produk = Produk.MinyakTanah, Tarif = r.tarif, SatuanTarif = r.satuan_tarif ?? "" });
            }
            if (isTabung)
            {
                foreach (var p in new[] { Produk.Lpg5_5Kg, Produk.Lpg12Kg, Produk.Lpg50Kg })
                {
                    tarifs.Add(new MitraTarif { MitraId = r.id, Produk = p, Tarif = r.tarif, SatuanTarif = r.satuan_tarif ?? "" });
                }
            }
            if (tarifs.Count == 0)
            {
                foreach (var p in ProdukInfo.All)
                {
                    tarifs.Add(new MitraTarif { MitraId = r.id, Produk = p, Tarif = r.tarif, SatuanTarif = r.satuan_tarif ?? "" });
                }
            }
            mitra.Tarifs = tarifs;
            return mitra;
        }).ToList();
    }

    private sealed class RawMitra
    {
        public string id { get; set; } = "";
        public string nama { get; set; } = "";
        public string jenis_kendaraan { get; set; } = "";
        public decimal kapasitas_max { get; set; }
        public string satuan_kapasitas { get; set; } = "";
        public string[]? rute { get; set; }
        public string[]? area_coverage { get; set; }
        public string? kontak { get; set; }
        public string? pic { get; set; }
        public bool active { get; set; }
        public decimal tarif { get; set; }
        public string? satuan_tarif { get; set; }
    }
}
