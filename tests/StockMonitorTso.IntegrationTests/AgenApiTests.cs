using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace StockMonitorTso.IntegrationTests;

public class AgenApiTests : IClassFixture<TestApiWebApplicationFactory>
{
    private readonly TestApiWebApplicationFactory _factory;

    public AgenApiTests(TestApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_BySupervisi_ThenTransfer_ConservationHolds()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");
        var supervisi = await TestHttp.ClientAsync(_factory, "supervisi@stockmonitor.local", "Supervisi!2345");

        var register = await superadmin.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "Papua",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 100m,
            dot = 10m,
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var create = await supervisi.PostAsJsonAsync("/api/agen", new { nama = "Agen API Papua", wilayah = "Papua" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var agen = await create.Content.ReadFromJsonAsync<AgenDto>();
        agen!.Id.Should().BeGreaterThan(0);

        var transfer = await supervisi.PostAsJsonAsync($"/api/agen/{agen.Id}/transfer-from-warehouse", new
        {
            wilayah = "Papua",
            quantities = new Dictionary<string, decimal> { ["MinyakTanah"] = 40m },
            catatan = "transfer via api",
        });
        transfer.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await supervisi.GetAsync($"/api/agen/{agen.Id}");
        var body = await detail.Content.ReadFromJsonAsync<AgenDetailDto>();
        body!.TotalStok.Should().Be(40m);
    }

    [Fact]
    public async Task Create_DuplicateName_Rejected400()
    {
        var supervisi = await TestHttp.ClientAsync(_factory, "supervisi@stockmonitor.local", "Supervisi!2345");

        (await supervisi.PostAsJsonAsync("/api/agen", new { nama = "Agen Duplikat", wilayah = "Maluku" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await supervisi.PostAsJsonAsync("/api/agen", new { nama = "Agen Duplikat", wilayah = "Maluku" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transfer_Overdraft_Rejected400()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");
        var supervisi = await TestHttp.ClientAsync(_factory, "supervisi@stockmonitor.local", "Supervisi!2345");

        await superadmin.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "PapuaBarat",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 20m,
            dot = 2m,
        });
        var agen = await (await supervisi.PostAsJsonAsync("/api/agen", new { nama = "Agen Overdraft", wilayah = "PapuaBarat" }))
            .Content.ReadFromJsonAsync<AgenDto>();

        var transfer = await supervisi.PostAsJsonAsync($"/api/agen/{agen!.Id}/transfer-from-warehouse", new
        {
            wilayah = "PapuaBarat",
            quantities = new Dictionary<string, decimal> { ["MinyakTanah"] = 500m },
        });
        transfer.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ByOperator_Rejected403()
    {
        var operatorClient = await TestHttp.ClientAsync(_factory, "operator@stockmonitor.local", "Operator!2345");

        var response = await operatorClient.PostAsJsonAsync("/api/agen", new { nama = "Agen Operator", wilayah = "Papua" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_BySuperadmin_Ok()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");

        var agen = await (await superadmin.PostAsJsonAsync("/api/agen", new { nama = "Agen Hapus", wilayah = "PapuaTengah" }))
            .Content.ReadFromJsonAsync<AgenDto>();

        var delete = await superadmin.DeleteAsync($"/api/agen/{agen!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await superadmin.GetAsync($"/api/agen/{agen.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    internal sealed record AgenDto(int Id, string Nama, string Wilayah);

    internal sealed record AgenDetailDto(int AgenId, string Nama, decimal TotalStok);
}
