using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.IntegrationTests;

public class StockWriteTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public StockWriteTests()
    {
        _factory = new TestWebApplicationFactoryNoStock();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));

    private async Task<StokEntitas> RegisterGudangAsync(
        IServiceProvider sp,
        ClaimsPrincipal actor,
        Wilayah wilayah = Wilayah.PapuaTengah,
        decimal stok = 100,
        decimal dot = 10)
    {
        var write = sp.GetRequiredService<IStockWriteService>();
        var entity = await write.RegisterAsync(actor, new RegisterStokRequest
        {
            Wilayah = wilayah,
            Produk = Produk.MinyakTanah,
            Tier = Tier.GudangWilayah,
            TanggalStokAwal = new DateTime(2026, 8, 5),
            Stok = stok,
            DOT = dot,
        });
        entity.Id.Should().BeGreaterThan(0);
        return entity;
    }

    [Fact]
    public async Task Register_ByOperator_Allowed()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), wilayah: Wilayah.PapuaTengah);
        entity.Stok.Should().Be(100);
    }

    [Fact]
    public async Task Register_ByTamu_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var act = () => write.RegisterAsync(Principal("Tamu"), new RegisterStokRequest
        {
            Wilayah = Wilayah.PapuaTengah,
            Produk = Produk.MinyakTanah,
            Tier = Tier.GudangWilayah,
            TanggalStokAwal = DateTime.Today,
            Stok = 10,
            DOT = 1,
        });
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Register_Duplicate_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), wilayah: Wilayah.PapuaTengah);

        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var act = () => write.RegisterAsync(Principal("Operator"), new RegisterStokRequest
        {
            Wilayah = Wilayah.PapuaTengah,
            Produk = Produk.MinyakTanah,
            Tier = Tier.GudangWilayah,
            TanggalStokAwal = DateTime.Today,
            Stok = 10,
            DOT = 1,
        });
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Adjust_BySupervisi_ChangesStok()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), stok: 100);

        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var updated = await write.TransactAsync(Principal("Supervisi"), entity.Id, StockTransactionType.Adjust, 50);

        updated.Stok.Should().Be(150);
    }

    [Fact]
    public async Task Adjust_Overdraft_Rejected_AndStokUnchanged()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), stok: 100);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var act = () => write.TransactAsync(Principal("Supervisi"), entity.Id, StockTransactionType.Adjust, -200);

        await act.Should().ThrowAsync<InvalidOperationException>();
        var reloaded = await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == entity.Id);
        reloaded.Stok.Should().Be(100);
    }

    [Fact]
    public async Task Adjust_NegativeOpname_Allowed_WhenSufficientStock()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), stok: 100);
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var updated = await write.TransactAsync(Principal("Supervisi"), entity.Id, StockTransactionType.Adjust, -30);
        updated.Stok.Should().Be(70);
    }

    [Fact]
    public async Task Issue_BySupervisi_DecreasesStokAndAutoTerjual()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), stok: 100);
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var updated = await write.TransactAsync(Principal("Supervisi"), entity.Id, StockTransactionType.Issue, 25);
        updated.Stok.Should().Be(75);
        updated.StokHabisTerjual.Should().Be(25);
    }

    [Fact]
    public async Task Issue_Overdraft_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), stok: 100);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var act = () => write.TransactAsync(Principal("Supervisi"), entity.Id, StockTransactionType.Issue, 200);
        await act.Should().ThrowAsync<InvalidOperationException>();
        var reloaded = await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == entity.Id);
        reloaded.Stok.Should().Be(100);
    }

    [Fact]
    public async Task Transfer_DebitSumber_KreditTujuan_Conserved()
    {
        using var scope = _factory.Services.CreateScope();
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var actor = Principal("Operator");

        var gudang = await write.RegisterAsync(actor, new RegisterStokRequest
        {
            Wilayah = Wilayah.PapuaTengah,
            Produk = Produk.MinyakTanah,
            Tier = Tier.GudangWilayah,
            TanggalStokAwal = new DateTime(2026, 8, 5),
            Stok = 100,
            DOT = 10,
        });
        var outlet = await write.RegisterAsync(actor, new RegisterStokRequest
        {
            Wilayah = Wilayah.PapuaTengah,
            Produk = Produk.MinyakTanah,
            Tier = Tier.Outlet,
            TanggalStokAwal = new DateTime(2026, 8, 5),
            Stok = 50,
            DOT = 5,
        });

        // transfer 30 dari Gudang ke Outlet → total wilayah tetap 150 (konservasi)
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await write.TransactAsync(Principal("Supervisi"), gudang.Id, StockTransactionType.Transfer, 30, tujuanId: outlet.Id);

        var gudangAfter = await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == gudang.Id);
        var outletAfter = await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == outlet.Id);
        gudangAfter.Stok.Should().Be(70);
        outletAfter.Stok.Should().Be(80);
        (gudangAfter.Stok + outletAfter.Stok).Should().Be(150);
    }

    [Fact]
    public async Task Transfer_CrossWilayah_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();
        var actor = Principal("Operator");

        var gudang = await write.RegisterAsync(actor, new RegisterStokRequest
        {
            Wilayah = Wilayah.PapuaTengah,
            Produk = Produk.MinyakTanah,
            Tier = Tier.GudangWilayah,
            TanggalStokAwal = new DateTime(2026, 8, 5),
            Stok = 100,
            DOT = 10,
        });
        var outlet = await write.RegisterAsync(actor, new RegisterStokRequest
        {
            Wilayah = Wilayah.PapuaBarat,
            Produk = Produk.MinyakTanah,
            Tier = Tier.Outlet,
            TanggalStokAwal = new DateTime(2026, 8, 5),
            Stok = 50,
            DOT = 5,
        });

        var act = () => write.TransactAsync(Principal("Supervisi"), gudang.Id, StockTransactionType.Transfer, 30, tujuanId: outlet.Id);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_BySuperadmin_SoftDeletes_AndHiddenFromDashboard()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), wilayah: Wilayah.PapuaBarat);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();

        await write.DeleteAsync(Principal("Superadmin"), entity.Id);

        var reloaded = await db.StokEntitas.AsNoTracking().FirstAsync(e => e.Id == entity.Id);
        reloaded.IsDeleted.Should().BeTrue();

        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();
        var cards = await dashboard.GetSalesAreaCardsAsync(DashboardFilter.MinyakTanah);
        cards.Should().NotContain(c => c.Wilayah == Wilayah.PapuaBarat && c.Produk == Produk.MinyakTanah);
    }

    [Fact]
    public async Task Delete_ByOperator_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), wilayah: Wilayah.PapuaBarat);
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();

        var act = () => write.DeleteAsync(Principal("Operator"), entity.Id);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Transactions_AreRecorded_ForAudit()
    {
        using var scope = _factory.Services.CreateScope();
        var entity = await RegisterGudangAsync(scope.ServiceProvider, Principal("Operator"), stok: 100);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var write = scope.ServiceProvider.GetRequiredService<IStockWriteService>();

        await write.TransactAsync(Principal("Supervisi"), entity.Id, StockTransactionType.Receive, 25, catatan: "Isi ulang");

        var records = await db.StockTransactions.AsNoTracking().Where(t => t.StokEntitasId == entity.Id).ToListAsync();
        records.Should().ContainSingle(t => t.Type == StockTransactionType.Receive && t.Kuantitas == 25);
        records[^1].StokSumberSebelum.Should().Be(100);
        records[^1].StokSumberSesudah.Should().Be(125);
    }
}
