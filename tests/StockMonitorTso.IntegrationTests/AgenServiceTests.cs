using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Persistence;
using StockMonitorTso.Infrastructure.Services;

namespace StockMonitorTso.IntegrationTests;

public class AgenServiceTests : IDisposable
{
    private readonly TestApiWebApplicationFactory _factory;

    public AgenServiceTests()
    {
        _factory = new TestApiWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static ClaimsPrincipal Principal(string role) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "Test"));

    [Fact]
    public async Task Create_BySupervisi_CreatesAgenWithFourStokRows()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var agen = await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest
        {
            Nama = "Agen Test Papua",
            Wilayah = Wilayah.Papua,
        });

        agen.Id.Should().BeGreaterThan(0);
        var rows = await db.StokEntitas.AsNoTracking().Where(e => e.AgenId == agen.Id).ToListAsync();
        rows.Should().HaveCount(ProdukInfo.All.Length);
        rows.Should().OnlyContain(r => r.Tier == Tier.Agen && r.Stok == 0m && r.DOT == 0m);
    }

    [Fact]
    public async Task Create_ByOperator_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();

        var act = () => service.CreateAsync(Principal("Operator"), new CreateAgenRequest
        {
            Nama = "Agen Test",
            Wilayah = Wilayah.Papua,
        });
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Create_DuplicateName_SameWilayah_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();

        await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "Agen Baru", Wilayah = Wilayah.Maluku });
        var act = () => service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "agen baru", Wilayah = Wilayah.Maluku });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Create_SameName_DifferentWilayah_Allowed()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();

        await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "Agen Baru", Wilayah = Wilayah.Maluku });
        var agen = await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "Agen Baru", Wilayah = Wilayah.Papua });

        agen.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Update_BySupervisi_Renames()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();

        var agen = await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "Agen Lama", Wilayah = Wilayah.Papua });
        var updated = await service.UpdateAsync(Principal("Supervisi"), agen.Id, new UpdateAgenRequest { Nama = "Agen Baru" });

        updated.Nama.Should().Be("Agen Baru");
    }

    [Fact]
    public async Task Update_ByOperator_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();

        var agen = await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "Agen Lama", Wilayah = Wilayah.Papua });
        var act = () => service.UpdateAsync(Principal("Operator"), agen.Id, new UpdateAgenRequest { Nama = "Agen Baru" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Delete_BySuperadmin_SoftDeletesAgenAndStokRows()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var agen = await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "Agen Hapus", Wilayah = Wilayah.Papua });
        await service.DeleteAsync(Principal("Superadmin"), agen.Id);

        var agenAfter = await db.Agen.AsNoTracking().FirstAsync(a => a.Id == agen.Id);
        agenAfter.IsDeleted.Should().BeTrue();
        var rows = await db.StokEntitas.AsNoTracking().Where(e => e.AgenId == agen.Id).ToListAsync();
        rows.Should().OnlyContain(r => r.IsDeleted);
    }

    [Fact]
    public async Task Delete_BySupervisi_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();

        var agen = await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "Agen Hapus", Wilayah = Wilayah.Papua });
        var act = () => service.DeleteAsync(Principal("Supervisi"), agen.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetAgenDetail_EmptyAgen_ReturnsFourZeroRows()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAgenService>();
        var dashboard = scope.ServiceProvider.GetRequiredService<IStockDashboardService>();

        var agen = await service.CreateAsync(Principal("Supervisi"), new CreateAgenRequest { Nama = "Agen Detail", Wilayah = Wilayah.Papua });
        var detail = await dashboard.GetAgenDetailAsync(agen.Id);

        detail.Should().NotBeNull();
        detail!.Rows.Should().HaveCount(ProdukInfo.All.Length);
        detail.Rows.Should().OnlyContain(r => r.Stok == 0m);
        detail.StatusArea.Should().BeNull();
    }
}
