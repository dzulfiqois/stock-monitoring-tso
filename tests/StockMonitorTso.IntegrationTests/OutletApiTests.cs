using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace StockMonitorTso.IntegrationTests;

public class OutletApiTests : IClassFixture<TestApiWebApplicationFactory>
{
    private readonly TestApiWebApplicationFactory _factory;

    public OutletApiTests(TestApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<int> CreateAgenAsync(HttpClient client, string nama, string wilayah)
    {
        var response = await client.PostAsJsonAsync("/api/agen", new { nama, wilayah });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AgenApiTests.AgenDto>())!.Id;
    }

    [Fact]
    public async Task CreateAndTransfer_AgenToOutlet_ConservationHolds()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");
        var supervisi = await TestHttp.ClientAsync(_factory, "supervisi@stockmonitor.local", "Supervisi!2345");

        await superadmin.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "Papua",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 200m,
            dot = 20m,
        });
        var agenId = await CreateAgenAsync(supervisi, "Agen Outlet Test", "Papua");

        var transferIn = await supervisi.PostAsJsonAsync($"/api/agen/{agenId}/transfer-from-warehouse", new
        {
            wilayah = "Papua",
            quantities = new Dictionary<string, decimal> { ["MinyakTanah"] = 80m },
        });
        transferIn.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var create = await supervisi.PostAsJsonAsync("/api/outlet", new { nama = "Outlet API", agenId });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var outlet = await create.Content.ReadFromJsonAsync<OutletDto>();

        var transfer = await supervisi.PostAsJsonAsync("/api/outlet/transfer-from-agen", new
        {
            agenId,
            outletId = outlet!.Id,
            quantities = new Dictionary<string, decimal> { ["MinyakTanah"] = 30m },
        });
        transfer.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await supervisi.GetAsync($"/api/outlet/{outlet.Id}");
        var body = await detail.Content.ReadFromJsonAsync<OutletDetailDto>();
        body!.TotalStok.Should().Be(30m);
        body.AgenId.Should().Be(agenId);
    }

    [Fact]
    public async Task Transfer_OutletFromWrongAgen_Rejected400()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");
        var supervisi = await TestHttp.ClientAsync(_factory, "supervisi@stockmonitor.local", "Supervisi!2345");

        await superadmin.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "Maluku",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 50m,
            dot = 5m,
        });
        var agenA = await CreateAgenAsync(supervisi, "Agen A Outlet", "Maluku");
        var agenB = await CreateAgenAsync(supervisi, "Agen B Outlet", "Maluku");

        var outlet = await (await supervisi.PostAsJsonAsync("/api/outlet", new { nama = "Outlet Salah Agen", agenId = agenB }))
            .Content.ReadFromJsonAsync<OutletDto>();

        var transfer = await supervisi.PostAsJsonAsync("/api/outlet/transfer-from-agen", new
        {
            agenId = agenA,
            outletId = outlet!.Id,
            quantities = new Dictionary<string, decimal> { ["MinyakTanah"] = 10m },
        });
        transfer.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ByTamu_Rejected403()
    {
        var tamu = await TestHttp.ClientAsync(_factory, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await tamu.PostAsJsonAsync("/api/outlet", new { nama = "Outlet Tamu", agenId = 1 });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    internal sealed record OutletDto(int Id, string Nama, int AgenId, string Wilayah);

    internal sealed record OutletDetailDto(int OutletId, string Nama, int AgenId, decimal TotalStok);
}
