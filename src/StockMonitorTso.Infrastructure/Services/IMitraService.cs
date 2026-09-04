using System.Security.Claims;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Services;

public sealed record CreateMitraRequest
{
    public required string Id { get; init; }
    public required string Nama { get; init; }
    public required string JenisKendaraan { get; init; }
    public required decimal KapasitasMax { get; init; }
    public required string SatuanKapasitas { get; init; }
    public required string[] Rute { get; init; }
    public required string[] AreaCoverage { get; init; }
    public required string Kontak { get; init; }
    public required string Pic { get; init; }
    public required bool Active { get; init; }
    public required IReadOnlyList<MitraTarifDto> Tarifs { get; init; }
}

public sealed record UpdateMitraRequest
{
    public required string Nama { get; init; }
    public required string JenisKendaraan { get; init; }
    public required decimal KapasitasMax { get; init; }
    public required string SatuanKapasitas { get; init; }
    public required string[] Rute { get; init; }
    public required string[] AreaCoverage { get; init; }
    public required string Kontak { get; init; }
    public required string Pic { get; init; }
    public required bool Active { get; init; }
}

public sealed record UpdateMitraTarifRequest
{
    public required Produk Produk { get; init; }
    public required decimal Tarif { get; init; }
    public required string SatuanTarif { get; init; }
}

public sealed record MitraTarifDto
{
    public required Produk Produk { get; init; }
    public required decimal Tarif { get; init; }
    public required string SatuanTarif { get; init; }
}

public interface IMitraService
{
    Task<MitraTso> CreateAsync(ClaimsPrincipal actor, CreateMitraRequest request, CancellationToken ct = default);
    Task<MitraTso> UpdateAsync(ClaimsPrincipal actor, string mitraId, UpdateMitraRequest request, CancellationToken ct = default);
    Task<MitraTso> UpdateTarifAsync(ClaimsPrincipal actor, string mitraId, UpdateMitraTarifRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<MitraTso>> ListAsync(CancellationToken ct = default);
    Task<MitraTso?> GetAsync(string mitraId, CancellationToken ct = default);
}
