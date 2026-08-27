using System.Security.Claims;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Services;

public sealed record CreateOutletRequest
{
    public required string Nama { get; init; }

    public required int AgenId { get; init; }

    public string? Keterangan { get; init; }
}

public sealed record UpdateOutletRequest
{
    public required string Nama { get; init; }

    public string? Keterangan { get; init; }
}

public interface IOutletService
{
    /// <summary>Create identitas Outlet di bawah Agen (Superadmin + Supervisi). Auto-create baris stok per produk (0).</summary>
    Task<Outlet> CreateAsync(ClaimsPrincipal actor, CreateOutletRequest request, CancellationToken ct = default);

    /// <summary>Update identitas Outlet (nama/keterangan) — Superadmin + Supervisi.</summary>
    Task<Outlet> UpdateAsync(ClaimsPrincipal actor, int outletId, UpdateOutletRequest request, CancellationToken ct = default);

    /// <summary>Delete identitas Outlet (soft delete + baris stok) — Superadmin only.</summary>
    Task DeleteAsync(ClaimsPrincipal actor, int outletId, CancellationToken ct = default);

    /// <summary>
    /// Transfer stok dari Agen ke Outlet (Superadmin + Supervisi). qtyPerProduk: Qty&gt;0 per SKU.
    /// </summary>
    Task TransferFromAgenAsync(
        ClaimsPrincipal actor,
        int agenId,
        int outletId,
        IReadOnlyDictionary<Produk, decimal> qtyPerProduk,
        string? catatan = null,
        CancellationToken ct = default);
}
