using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.IntegrationTests;

public class WarehouseTransferTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public WarehouseTransferTests()
    {
        _factory = new TestWebApplicationFactoryNoStock();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));

    private static async Task<StokEntitas> RegisterGudangAsync(
        IServiceProvider sp, ClaimsPrincipal actor, Wilayah wilayah, Produk produk, decimal stok)
    {
        var write = sp.GetRequiredService<IStockWriteService>();
        return await write.RegisterAsync(actor, new RegisterStokRequest
        {
            Wilayah = wilayah,
            Produk = produk,
            Tier = Tier.GudangWilayah,
            TanggalStokAwal = new DateTime(2026, 8, 5),
            Stok = stok,
            DOT = 100,
        });
    }

    private static async Task<Agen> CreateAgenAsync(IServiceProvider sp, ClaimsPrincipal actor, Wilayah wilayah, string nama)
    {
        var agenService = sp.GetRequiredService<IAgenService>();
        return await agenService.CreateAsync(actor, new CreateAgenRequest { Nama = nama, Wilayah = wilayah });
    }

    private static async Task<StokEntitas> GetAgenRowAsync(IServiceProvider sp, int agenId, Produk produk)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        return await db.StokEntitas.AsNoTracking().FirstAsync(e => e.AgenId == agenId && e.Produk == produk);
    }

    [Fact]
    public async Task Transfer_SingleSku_Supervisi_DebitGudangCreditAgen_Conserved()
    {
        using var scope = _factory.Services.CreateScope();
        var actor = Principal("Operator");
        var gudang = await RegisterGudangAsync(scope.ServiceProvider, actor, Wilayah.Papua, Produk.Lpg5_5Kg, 5000);
        var agen = await CreateAgenAsync(scope.ServiceProvider, Principal("Supervisi"), Wilayah.Papua, "Agen Konservasi");

        var agenService = scope.ServiceProvider.GetRequiredService<IAgenService>();
        await agenService.TransferFromWarehouseAsync(
            Principal("Supervisi"), Wilayah.Papua, agen.Id,
            new Dictionary<Produk, decimal> { [Produk.Lpg5_5Kg] = 5000m });

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var gudangAfter = await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == gudang.Id);
        var agenAfter = await GetAgenRowAsync(scope.ServiceProvider, agen.Id, Produk.Lpg5_5Kg);
        gudangAfter.Stok.Should().Be(0);
        agenAfter.Stok.Should().Be(5000);
        (gudangAfter.Stok + agenAfter.Stok).Should().Be(5000);
    }

    [Fact]
    public async Task Transfer_MultiSku_AllDebitedAndCredited_Simultaneously()
    {
        using var scope = _factory.Services.CreateScope();
        var actor = Principal("Operator");
        var gudang5 = await RegisterGudangAsync(scope.ServiceProvider, actor, Wilayah.Papua, Produk.Lpg5_5Kg, 5000);
        var gudang12 = await RegisterGudangAsync(scope.ServiceProvider, actor, Wilayah.Papua, Produk.Lpg12Kg, 3000);
        var gudang50 = await RegisterGudangAsync(scope.ServiceProvider, actor, Wilayah.Papua, Produk.Lpg50Kg, 2000);
        var agen = await CreateAgenAsync(scope.ServiceProvider, Principal("Supervisi"), Wilayah.Papua, "Agen Multi");

        var agenService = scope.ServiceProvider.GetRequiredService<IAgenService>();
        await agenService.TransferFromWarehouseAsync(
            Principal("Supervisi"), Wilayah.Papua, agen.Id,
            new Dictionary<Produk, decimal>
            {
                [Produk.Lpg5_5Kg] = 5000m,
                [Produk.Lpg12Kg] = 3000m,
                [Produk.Lpg50Kg] = 2000m,
            });

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == gudang5.Id)).Stok.Should().Be(0);
        (await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == gudang12.Id)).Stok.Should().Be(0);
        (await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == gudang50.Id)).Stok.Should().Be(0);
        (await GetAgenRowAsync(scope.ServiceProvider, agen.Id, Produk.Lpg5_5Kg)).Stok.Should().Be(5000);
        (await GetAgenRowAsync(scope.ServiceProvider, agen.Id, Produk.Lpg12Kg)).Stok.Should().Be(3000);
        (await GetAgenRowAsync(scope.ServiceProvider, agen.Id, Produk.Lpg50Kg)).Stok.Should().Be(2000);
    }

    [Fact]
    public async Task Transfer_Overdraft_Rejected_AndGudangUnchanged()
    {
        using var scope = _factory.Services.CreateScope();
        var gudang = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), Wilayah.Papua, Produk.Lpg5_5Kg, 100);
        var agen = await CreateAgenAsync(scope.ServiceProvider, Principal("Supervisi"), Wilayah.Papua, "Agen Overdraft");

        var agenService = scope.ServiceProvider.GetRequiredService<IAgenService>();
        var act = () => agenService.TransferFromWarehouseAsync(
            Principal("Supervisi"), Wilayah.Papua, agen.Id,
            new Dictionary<Produk, decimal> { [Produk.Lpg5_5Kg] = 200m });

        await act.Should().ThrowAsync<InvalidOperationException>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == gudang.Id)).Stok.Should().Be(100);
        (await GetAgenRowAsync(scope.ServiceProvider, agen.Id, Produk.Lpg5_5Kg)).Stok.Should().Be(0);
    }

    [Fact]
    public async Task Transfer_ByOperator_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var agen = await CreateAgenAsync(scope.ServiceProvider, Principal("Supervisi"), Wilayah.Papua, "Agen RBAC");

        var agenService = scope.ServiceProvider.GetRequiredService<IAgenService>();
        var act = () => agenService.TransferFromWarehouseAsync(
            Principal("Operator"), Wilayah.Papua, agen.Id,
            new Dictionary<Produk, decimal> { [Produk.Lpg5_5Kg] = 100m });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Transfer_AgenDifferentWilayah_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), Wilayah.Papua, Produk.Lpg5_5Kg, 5000);
        var agenMaluku = await CreateAgenAsync(scope.ServiceProvider, Principal("Supervisi"), Wilayah.Maluku, "Agen Maluku");

        var agenService = scope.ServiceProvider.GetRequiredService<IAgenService>();
        var act = () => agenService.TransferFromWarehouseAsync(
            Principal("Supervisi"), Wilayah.Papua, agenMaluku.Id,
            new Dictionary<Produk, decimal> { [Produk.Lpg5_5Kg] = 100m });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Transfer_NoPositiveQuantity_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var agen = await CreateAgenAsync(scope.ServiceProvider, Principal("Supervisi"), Wilayah.Papua, "Agen Kosong");

        var agenService = scope.ServiceProvider.GetRequiredService<IAgenService>();
        var act = () => agenService.TransferFromWarehouseAsync(
            Principal("Supervisi"), Wilayah.Papua, agen.Id,
            new Dictionary<Produk, decimal> { [Produk.Lpg5_5Kg] = 0m });

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
