using System.Security.Claims;
using StockMonitorTso.Domain.Entities;

namespace StockMonitorTso.Infrastructure.Services;

public sealed record RegisterStokRequest
{
    public required Wilayah Wilayah { get; init; }

    public required Produk Produk { get; init; }

    public required Tier Tier { get; init; }

    public DateTime TanggalStokAwal { get; init; }

    public decimal Stok { get; init; }

    public decimal DOT { get; init; }

    public decimal? StokHabisTerjual { get; init; }

    public decimal? StokIntransit { get; init; }

    public string? Keterangan { get; init; }
}

public sealed record UpdateDetailRequest
{
    public decimal DOT { get; init; }

    public DateTime TanggalStokAwal { get; init; }

    public decimal? StokHabisTerjual { get; init; }

    public decimal? StokIntransit { get; init; }

    public string? Keterangan { get; init; }
}

public interface IStockWriteService
{
    /// <summary>Create — Superadmin + Operator.</summary>
    Task<StokEntitas> RegisterAsync(ClaimsPrincipal actor, RegisterStokRequest request, CancellationToken ct = default);

    /// <summary>Update field non-stok (DOT, tanggal, keterangan) — Superadmin + Supervisi.</summary>
    Task<StokEntitas> UpdateDetailAsync(ClaimsPrincipal actor, int entitasId, UpdateDetailRequest request, CancellationToken ct = default);

    /// <summary>Transaksi stok atomic (Receive/Adjust/Transfer) — Superadmin + Supervisi. Menolak overdraft (stok &lt; 0).</summary>
    Task<StokEntitas> TransactAsync(
        ClaimsPrincipal actor,
        int entitasId,
        StockTransactionType type,
        decimal kuantitas,
        int? tujuanId = null,
        string? catatan = null,
        CancellationToken ct = default);

    /// <summary>Delete (soft delete) — Superadmin only.</summary>
    Task DeleteAsync(ClaimsPrincipal actor, int entitasId, CancellationToken ct = default);
}
