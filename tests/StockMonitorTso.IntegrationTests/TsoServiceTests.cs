using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.IntegrationTests;

public class TsoServiceTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    public TsoServiceTests(TestWebApplicationFactory factory) => _factory = factory;

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));

    [Fact]
    public async Task Create_ByOperator_CreatesOrderAndRencana()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var order = await service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "MT-001",
            WilayahTujuan = Wilayah.Papua,
            Produk = Produk.MinyakTanah,
            Kuantitas = 100,
            TanggalKeberangkatan = DateTime.Today.AddDays(1),
        });

        order.OrderNo.Should().StartWith("TSO-");
        order.Status.Should().Be(TransportOrderStatus.StockImpacted);
        order.TarifSnapshot.Should().Be(150000);
        order.EstimasiBiayaSnapshot.Should().Be(150000 * 100);

        var rencana = await db.RencanaKedatangan.AsNoTracking()
            .FirstOrDefaultAsync(r => r.NextSupply == 100 && r.ETA == order.Eta);
        rencana.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_MitraInvalid_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var act = () => service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "NOT-EXIST",
            WilayahTujuan = Wilayah.Papua,
            Produk = Produk.MinyakTanah,
            Kuantitas = 10,
            TanggalKeberangkatan = DateTime.Today,
        });
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Create_AreaCoverageMismatch_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        // MT-002 covers Papua Barat area only, not Papua
        var act = () => service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "MT-002",
            WilayahTujuan = Wilayah.Papua,
            Produk = Produk.MinyakTanah,
            Kuantitas = 10,
            TanggalKeberangkatan = DateTime.Today,
        });
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Create_KuantitasZero_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var act = () => service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "MT-001",
            WilayahTujuan = Wilayah.Papua,
            Produk = Produk.MinyakTanah,
            Kuantitas = 0,
            TanggalKeberangkatan = DateTime.Today,
        });
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Create_TanggalLampau_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var act = () => service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "MT-001",
            WilayahTujuan = Wilayah.Papua,
            Produk = Produk.MinyakTanah,
            Kuantitas = 10,
            TanggalKeberangkatan = DateTime.Today.AddDays(-1),
        });
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Create_Idempotent_DuplicateWithinMinuteReturnsSameOrder()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var req = new CreateTransportOrderRequest
        {
            MitraId = "MT-001",
            WilayahTujuan = Wilayah.Maluku,
            Produk = Produk.Lpg5_5Kg,
            Kuantitas = 50,
            TanggalKeberangkatan = DateTime.Today.AddDays(2),
        };
        var o1 = await service.CreateAsync(Principal("Operator"), req);
        var o2 = await service.CreateAsync(Principal("Operator"), req);
        o1.Id.Should().Be(o2.Id);
        o1.OrderNo.Should().Be(o2.OrderNo);
    }

    [Fact]
    public async Task Update_BySupervisi_Allowed_ByOperatorRejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var order = await service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "MT-001",
            WilayahTujuan = Wilayah.Maluku,
            Produk = Produk.MinyakTanah,
            Kuantitas = 10,
            TanggalKeberangkatan = DateTime.Today.AddDays(1),
        });

        var updateReq = new UpdateTransportOrderRequest
        {
            MitraId = "MT-001",
            WilayahTujuan = Wilayah.Maluku,
            Produk = Produk.MinyakTanah,
            Kuantitas = 20,
            TanggalKeberangkatan = DateTime.Today.AddDays(1),
            RowVersion = order.RowVersion,
        };
        var updated = await service.UpdateAsync(Principal("Supervisi"), order.Id, updateReq);
        updated.Kuantitas.Should().Be(20);

        var act = () => service.UpdateAsync(Principal("Operator"), order.Id, updateReq);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Delete_BySuperadmin_Allowed_ByOperatorRejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var order = await service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "MT-001",
            WilayahTujuan = Wilayah.Maluku,
            Produk = Produk.MinyakTanah,
            Kuantitas = 10,
            TanggalKeberangkatan = DateTime.Today.AddDays(1),
        });

        var actOperator = () => service.DeleteAsync(Principal("Operator"), order.Id);
        await actOperator.Should().ThrowAsync<UnauthorizedAccessException>();

        await service.DeleteAsync(Principal("Superadmin"), order.Id);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var deleted = await db.TransportOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == order.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateInvoice_Idempotent_BytesEqual()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var order = await service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "MT-002",
            WilayahTujuan = Wilayah.PapuaBarat,
            Produk = Produk.Lpg12Kg,
            Kuantitas = 100,
            TanggalKeberangkatan = DateTime.Today.AddDays(1),
        });

        var pdf1 = await service.GenerateInvoiceAsync(order.Id);
        var pdf2 = await service.GenerateInvoiceAsync(order.Id);
        pdf1.Should().Equal(pdf2);
    }

    [Fact]
    public async Task Snapshot_TarifDoesNotChangeAfterMitraPriceChange()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITransportOrderService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var order = await service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
        {
            MitraId = "MT-003",
            WilayahTujuan = Wilayah.PapuaTengah,
            Produk = Produk.Lpg12Kg,
            Kuantitas = 10,
            TanggalKeberangkatan = DateTime.Today.AddDays(1),
        });
        var snapshotTarif = order.TarifSnapshot;

        // simulate price change in master
        var mitra = await db.MitraTso.FirstAsync(m => m.Id == "MT-003");
        var originalTarif = mitra.Tarif;
        mitra.Tarif = 999999;
        await db.SaveChangesAsync();

        try
        {
            var orderReload = await service.GetAsync(order.Id);
            orderReload!.TarifSnapshot.Should().Be(snapshotTarif);
            orderReload.TarifSnapshot.Should().NotBe(999999);

            // new order should pick new price
            var order2 = await service.CreateAsync(Principal("Operator"), new CreateTransportOrderRequest
            {
                MitraId = "MT-003",
                WilayahTujuan = Wilayah.PapuaTengah,
                Produk = Produk.Lpg5_5Kg,
                Kuantitas = 10,
                TanggalKeberangkatan = DateTime.Today.AddDays(1),
            });
            order2.TarifSnapshot.Should().Be(999999);
        }
        finally
        {
            mitra.Tarif = originalTarif;
            await db.SaveChangesAsync();
        }
    }
}
