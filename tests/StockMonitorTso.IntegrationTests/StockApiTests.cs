using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace StockMonitorTso.IntegrationTests;

public class StockApiTests : IClassFixture<TestApiWebApplicationFactory>
{
    private readonly TestApiWebApplicationFactory _factory;

    public StockApiTests(TestApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ByOperator_CreatesRow()
    {
        var client = await TestHttp.ClientAsync(_factory, "operator@stockmonitor.local", "Operator!2345");

        var response = await client.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "Papua",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 100m,
            dot = 10m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<StockDto>();
        body!.Id.Should().BeGreaterThan(0);
        body.Stok.Should().Be(100m);
    }

    [Fact]
    public async Task Register_ByTamu_Rejected403()
    {
        var client = await TestHttp.ClientAsync(_factory, "tamu@stockmonitor.local", "Tamu!2345");

        var response = await client.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "Maluku",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 50m,
            dot = 5m,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Transact_ReceiveAndIssue_UpdatesStokAndTerjual_OverdraftRejected()
    {
        var operatorClient = await TestHttp.ClientAsync(_factory, "operator@stockmonitor.local", "Operator!2345");
        var register = await operatorClient.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "PapuaBarat",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 100m,
            dot = 10m,
        });
        var id = (await register.Content.ReadFromJsonAsync<StockDto>())!.Id;

        var supervisi = await TestHttp.ClientAsync(_factory, "supervisi@stockmonitor.local", "Supervisi!2345");
        var receive = await supervisi.PostAsJsonAsync($"/api/stock/{id}/transact", new { type = "Receive", kuantitas = 50m });
        receive.StatusCode.Should().Be(HttpStatusCode.OK);
        (await receive.Content.ReadFromJsonAsync<StockDto>())!.Stok.Should().Be(150m);

        var issue = await supervisi.PostAsJsonAsync($"/api/stock/{id}/transact", new { type = "Issue", kuantitas = 30m });
        issue.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await issue.Content.ReadFromJsonAsync<StockDto>();
        after!.Stok.Should().Be(120m);
        after.StokHabisTerjual.Should().Be(30m);

        var overdraft = await supervisi.PostAsJsonAsync($"/api/stock/{id}/transact", new { type = "Issue", kuantitas = 999m });
        overdraft.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateDetail_BySupervisi_Ok_ByOperator_Rejected()
    {
        var operatorRegister = await TestHttp.ClientAsync(_factory, "operator@stockmonitor.local", "Operator!2345");
        var register = await operatorRegister.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "Maluku",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 10m,
            dot = 1m,
        });
        var id = (await register.Content.ReadFromJsonAsync<StockDto>())!.Id;

        var supervisi = await TestHttp.ClientAsync(_factory, "supervisi@stockmonitor.local", "Supervisi!2345");
        var update = await supervisi.PutAsJsonAsync($"/api/stock/{id}", new
        {
            dot = 2m,
            tanggalStokAwal = "2026-09-02",
            keterangan = "update via api",
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var operatorClient = await TestHttp.ClientAsync(_factory, "operator@stockmonitor.local", "Operator!2345");
        var rejected = await operatorClient.PutAsJsonAsync($"/api/stock/{id}", new
        {
            dot = 3m,
            tanggalStokAwal = "2026-09-02",
        });
        rejected.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_BySuperadmin_Ok_ByOperator_Rejected()
    {
        var superadmin = await TestHttp.ClientAsync(_factory, "superadmin@stockmonitor.local", "Superadmin!2345");
        var register = await superadmin.PostAsJsonAsync("/api/stock", new
        {
            wilayah = "PapuaTengah",
            produk = "MinyakTanah",
            tier = "GudangWilayah",
            tanggalStokAwal = "2026-09-01",
            stok = 10m,
            dot = 1m,
        });
        var id = (await register.Content.ReadFromJsonAsync<StockDto>())!.Id;

        var operatorClient = await TestHttp.ClientAsync(_factory, "operator@stockmonitor.local", "Operator!2345");
        (await operatorClient.DeleteAsync($"/api/stock/{id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await superadmin.DeleteAsync($"/api/stock/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await superadmin.GetAsync($"/api/dashboard/sales-area/PapuaTengah/MinyakTanah")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    internal sealed record StockDto(int Id, decimal Stok, decimal? StokHabisTerjual);
}
