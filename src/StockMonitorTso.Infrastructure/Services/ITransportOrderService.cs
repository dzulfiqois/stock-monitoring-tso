using System.Security.Claims;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Services;

public sealed record CreateTransportOrderRequest
{
    public required string MitraId { get; init; }
    public required Wilayah WilayahTujuan { get; init; }
    public required Produk Produk { get; init; }
    public required decimal Kuantitas { get; init; }
    public required DateTime TanggalKeberangkatan { get; init; }
    public string? RuteAsal { get; init; }
    public string? RuteTujuan { get; init; }
}

public sealed record UpdateTransportOrderRequest
{
    public required string MitraId { get; init; }
    public required Wilayah WilayahTujuan { get; init; }
    public required Produk Produk { get; init; }
    public required decimal Kuantitas { get; init; }
    public required DateTime TanggalKeberangkatan { get; init; }
    public string? RuteAsal { get; init; }
    public string? RuteTujuan { get; init; }
    public required byte[] RowVersion { get; init; }
}

public interface ITransportOrderService
{
    Task<TransportOrder> CreateAsync(ClaimsPrincipal actor, CreateTransportOrderRequest request, CancellationToken ct = default);
    Task<TransportOrder?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TransportOrder>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MitraTso>> ListMitraAsync(CancellationToken ct = default);
    Task<TransportOrder> UpdateAsync(ClaimsPrincipal actor, int id, UpdateTransportOrderRequest request, CancellationToken ct = default);
    Task DeleteAsync(ClaimsPrincipal actor, int id, CancellationToken ct = default);
    Task ResyncStockImpactAsync(int id, CancellationToken ct = default);
    Task<byte[]> GenerateInvoiceAsync(int id, CancellationToken ct = default);
}
